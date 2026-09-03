#!/usr/bin/env python3
"""Apply Prompt 3B tenant-isolation fixes from the authoritative P3A report.

This script is intentionally deterministic and fail-closed:
- reads reports/p3a/tenant-isolation-report.json
- patches only entities named by P3A
- never edits AegisDbContext.cs
- scaffolds one temporary EF migration to refresh the model snapshot
- rejects unrelated EF model drift
- replaces the temporary migration with one no-default migration per List-2 entity
- writes an auditable completion manifest/checklist
"""

from __future__ import annotations

import json
import re
import subprocess
import sys
from datetime import datetime, timedelta, timezone
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
REPORT = ROOT / "reports" / "p3a" / "tenant-isolation-report.json"
P3B_DIR = ROOT / "reports" / "p3b"
MIGRATIONS = ROOT / "AegisCore.Infrastructure" / "Migrations"
SNAPSHOT = MIGRATIONS / "AegisDbContextModelSnapshot.cs"
FORBIDDEN = ROOT / "AegisCore.Infrastructure" / "AegisDbContext.cs"
DOMAIN_COMMON_USING = "using AegisCore.Domain.Common;"
TEMP_MIGRATION_NAME = "__P3B_TenantIsolationAggregate"


def fail(message: str) -> None:
    print(f"P3B ERROR: {message}", file=sys.stderr)
    raise SystemExit(1)


def run(*args: str) -> subprocess.CompletedProcess[str]:
    print("+", " ".join(args))
    result = subprocess.run(args, cwd=ROOT, text=True, capture_output=True)
    if result.stdout:
        print(result.stdout)
    if result.stderr:
        print(result.stderr, file=sys.stderr)
    if result.returncode != 0:
        fail(f"command failed ({result.returncode}): {' '.join(args)}")
    return result


def ensure_common_using(text: str) -> str:
    if DOMAIN_COMMON_USING in text:
        return text
    namespace_match = re.search(r"(?m)^\s*namespace\s+", text)
    if namespace_match:
        pos = namespace_match.start()
        prefix = text[:pos]
        suffix = text[pos:]
        if prefix and not prefix.endswith("\n"):
            prefix += "\n"
        return prefix + DOMAIN_COMMON_USING + "\n" + suffix
    return DOMAIN_COMMON_USING + "\n" + text


def class_pattern(entity_name: str) -> re.Pattern[str]:
    # Some recovered entity files place [Table(...)] and the class declaration
    # on the same physical line, so declarations must not be anchored at ^.
    return re.compile(
        r"(?P<decl>"
        r"(?P<indent>[ \t]*)"
        r"(?:(?:public|internal)\s+)"
        r"(?:(?:sealed|abstract|partial)\s+)*"
        r"(?:class|record(?:\s+class)?)\s+"
        + re.escape(entity_name)
        + r"\b"
        r"(?P<tail>[^\{;]*?)"
        r")(?P<brace>\{)",
        re.DOTALL,
    )


def patch_entity(text: str, entity_name: str, add_tenant_id: bool) -> tuple[str, dict[str, str]]:
    pattern = class_pattern(entity_name)
    matches = list(pattern.finditer(text))
    if len(matches) != 1:
        fail(f"expected exactly one declaration for {entity_name}, found {len(matches)}")

    match = matches[0]
    declaration_before = match.group("decl").strip()
    tail = match.group("tail")
    tail_core = tail.rstrip()
    tail_ws = tail[len(tail_core):]

    if re.search(r"\bITenantScoped\b", tail_core):
        new_tail_core = tail_core
    elif ":" in tail_core:
        new_tail_core = tail_core + ", ITenantScoped"
    else:
        new_tail_core = tail_core + " : ITenantScoped"

    new_decl = match.group("decl")[: len(match.group("decl")) - len(tail)] + new_tail_core + tail_ws
    declaration_after = new_decl.strip()
    replacement = new_decl + match.group("brace")
    patched = text[: match.start()] + replacement + text[match.end():]

    property_inserted = "false"
    if add_tenant_id:
        rematch = class_pattern(entity_name).search(patched)
        if rematch is None:
            fail(f"could not re-locate {entity_name} after interface patch")
        indent = rematch.group("indent")
        # For inline declarations after an attribute, derive indentation from the
        # physical line instead of the one-space separator before "public".
        line_start = patched.rfind("\n", 0, rematch.start()) + 1
        physical_prefix = patched[line_start:rematch.start()]
        line_indent_match = re.match(r"[ \t]*", physical_prefix)
        if line_indent_match:
            indent = line_indent_match.group(0)
        insert_at = rematch.end("brace")
        tenant_property = f"\n{indent}    public Guid TenantId {{ get; set; }}"
        patched = patched[:insert_at] + tenant_property + patched[insert_at:]
        property_inserted = "true"

    patched = ensure_common_using(patched)
    return patched, {
        "entity": entity_name,
        "declarationBefore": declaration_before,
        "declarationAfter": declaration_after,
        "tenantIdInserted": property_inserted,
    }


def extract_entity_block(snapshot: str, full_type_name: str) -> str:
    marker = f'modelBuilder.Entity("{full_type_name}", b =>'
    start = snapshot.find(marker)
    if start < 0:
        fail(f"snapshot block not found for {full_type_name}")
    brace = snapshot.find("{", start)
    if brace < 0:
        fail(f"snapshot opening brace not found for {full_type_name}")

    depth = 0
    in_string = False
    escape = False
    for index in range(brace, len(snapshot)):
        ch = snapshot[index]
        if in_string:
            if escape:
                escape = False
            elif ch == "\\":
                escape = True
            elif ch == '"':
                in_string = False
            continue
        if ch == '"':
            in_string = True
        elif ch == "{":
            depth += 1
        elif ch == "}":
            depth -= 1
            if depth == 0:
                return snapshot[start : index + 1]
    fail(f"snapshot block did not terminate for {full_type_name}")
    raise AssertionError("unreachable")


def table_mapping(snapshot: str, namespace: str, entity_name: str) -> tuple[str, str | None]:
    block = extract_entity_block(snapshot, f"{namespace}.{entity_name}")
    match = re.search(r'b\.ToTable\("([^"]+)"(?:,\s*"([^"]+)")?\);', block)
    if not match:
        fail(f"ToTable mapping not found for {namespace}.{entity_name}")
    return match.group(1), match.group(2)


def validate_temporary_migration(content: str, expected_entities: int) -> None:
    up_match = re.search(
        r"protected override void Up\(MigrationBuilder migrationBuilder\)\s*\{(?P<body>.*?)\n\s*\}\s*\n\s*protected override void Down",
        content,
        re.DOTALL,
    )
    if not up_match:
        fail("could not parse temporary migration Up() body")
    body = up_match.group("body")

    calls = re.findall(r"migrationBuilder\.([A-Za-z0-9_]+)\s*(?:<[^>]+>)?\s*\(", body)
    unexpected = sorted({call for call in calls if call != "AddColumn"})
    if unexpected:
        fail(f"unrelated model drift detected in temporary migration: {unexpected}")

    tenant_columns = len(re.findall(r'name:\s*"TenantId"', body))
    if tenant_columns != expected_entities:
        fail(
            f"temporary migration contains {tenant_columns} TenantId columns; "
            f"expected exactly {expected_entities}. Refusing to absorb unrelated model drift."
        )


def migration_content(migration_id: str, class_name: str, table: str, schema: str | None) -> str:
    schema_arg = f',\n                schema: "{schema}"' if schema else ""
    index_name = f'IX_{table}_TenantId'

    return f'''using Microsoft.EntityFrameworkCore.Infrastructure;\nusing Microsoft.EntityFrameworkCore.Migrations;\n\n#nullable disable\n\nnamespace AegisCore.Infrastructure.Migrations;\n\n[DbContext(typeof(AegisDbContext))]\n[Migration("{migration_id}")]\npublic partial class {class_name} : Migration\n{{\n    protected override void Up(MigrationBuilder migrationBuilder)\n    {{\n        // DATA MIGRATION REQUIRED — populate TenantId from known tenant before running this migration in production.\n        migrationBuilder.AddColumn<Guid>(\n            name: "TenantId",\n            table: "{table}"{schema_arg},\n            type: "uuid",\n            nullable: false);\n\n        migrationBuilder.CreateIndex(\n            name: "{index_name}",\n            table: "{table}",\n            column: "TenantId"{schema_arg});\n    }}\n\n    protected override void Down(MigrationBuilder migrationBuilder)\n    {{\n        migrationBuilder.DropIndex(\n            name: "{index_name}",\n            table: "{table}"{schema_arg});\n\n        migrationBuilder.DropColumn(\n            name: "TenantId",\n            table: "{table}"{schema_arg});\n    }}\n}}\n'''


def main() -> None:
    if not REPORT.exists():
        fail(f"P3A report not found: {REPORT.relative_to(ROOT)}")
    if not SNAPSHOT.exists():
        fail("EF model snapshot is missing")

    report = json.loads(REPORT.read_text(encoding="utf-8"))
    list1 = report.get("List1HasTenantIdNoInterface", [])
    list2 = report.get("List2NoTenantId", [])

    if report.get("List1Count") != len(list1) or report.get("List2Count") != len(list2):
        fail("P3A report counts do not match report contents")
    if len(list1) != 353 or len(list2) != 75:
        fail(f"unexpected P3A baseline: List1={len(list1)}, List2={len(list2)}")

    before_forbidden = FORBIDDEN.read_bytes()
    diffs: list[dict[str, str]] = []
    changed_files: set[str] = set()

    by_file: dict[str, list[tuple[dict, bool]]] = {}
    for entry in list1:
        by_file.setdefault(entry["FilePath"], []).append((entry, False))
    for entry in list2:
        by_file.setdefault(entry["FilePath"], []).append((entry, True))

    for rel_path, entities in sorted(by_file.items()):
        path = ROOT / rel_path
        if not path.exists():
            fail(f"entity source file missing: {rel_path}")
        text = path.read_text(encoding="utf-8-sig")
        original = text
        for entry, add_tenant_id in sorted(entities, key=lambda pair: pair[0]["EntityName"]):
            text, diff = patch_entity(text, entry["EntityName"], add_tenant_id)
            diff["file"] = rel_path
            diff["list"] = "2" if add_tenant_id else "1"
            diffs.append(diff)
        if text != original:
            path.write_text(text, encoding="utf-8")
            changed_files.add(rel_path)

    if FORBIDDEN.read_bytes() != before_forbidden:
        fail("AegisDbContext.cs changed; P3B explicitly forbids this")

    before_migrations = {p.name for p in MIGRATIONS.glob("*.cs")}
    run(
        "dotnet",
        "ef",
        "migrations",
        "add",
        TEMP_MIGRATION_NAME,
        "--project",
        "AegisCore.Infrastructure/AegisCore.Infrastructure.csproj",
        "--startup-project",
        "AegisCore.API/AegisCore.API.csproj",
        "--context",
        "AegisDbContext",
        "--output-dir",
        "Migrations",
    )

    after_migrations = {p.name for p in MIGRATIONS.glob("*.cs")}
    created = sorted(after_migrations - before_migrations)
    temp_main = [name for name in created if name.endswith(f"_{TEMP_MIGRATION_NAME}.cs") and not name.endswith(".Designer.cs")]
    temp_designer = [name for name in created if name.endswith(f"_{TEMP_MIGRATION_NAME}.Designer.cs")]
    if len(temp_main) != 1 or len(temp_designer) != 1:
        fail(f"expected one temporary migration pair, got: {created}")

    temp_main_path = MIGRATIONS / temp_main[0]
    validate_temporary_migration(temp_main_path.read_text(encoding="utf-8"), len(list2))

    snapshot = SNAPSHOT.read_text(encoding="utf-8")
    mappings: list[dict[str, str | None]] = []
    for entry in list2:
        table, schema = table_mapping(snapshot, entry["Namespace"], entry["EntityName"])
        mappings.append({**entry, "Table": table, "Schema": schema})

    temp_main_path.unlink()
    (MIGRATIONS / temp_designer[0]).unlink()

    base_time = datetime.now(timezone.utc).replace(microsecond=0)
    generated_migrations: list[dict[str, str | None]] = []
    for offset, entry in enumerate(mappings):
        timestamp = (base_time + timedelta(seconds=offset)).strftime("%Y%m%d%H%M%S")
        class_name = f'AddTenantId_{entry["EntityName"]}'
        migration_id = f"{timestamp}_{class_name}"
        path = MIGRATIONS / f"{migration_id}.cs"
        if path.exists():
            fail(f"migration path already exists: {path.name}")
        path.write_text(
            migration_content(migration_id, class_name, entry["Table"], entry["Schema"]),
            encoding="utf-8",
        )
        generated_migrations.append({
            "EntityName": entry["EntityName"],
            "MigrationId": migration_id,
            "File": str(path.relative_to(ROOT)).replace("\\", "/"),
            "Table": entry["Table"],
            "Schema": entry["Schema"],
        })

    P3B_DIR.mkdir(parents=True, exist_ok=True)
    (P3B_DIR / "entity-diffs.json").write_text(json.dumps(diffs, indent=2), encoding="utf-8")

    checklist_lines = [
        "# P3B Tenant Isolation Completion Checklist",
        "",
        "| Entity | File | List | Fix Applied | Migration Name | Data Migration Needed |",
        "|---|---|---:|---|---|---|",
    ]
    migration_by_entity = {item["EntityName"]: item for item in generated_migrations}
    list2_names = {item["EntityName"] for item in list2}
    for entry in sorted(list1 + list2, key=lambda item: item["EntityName"]):
        is_list2 = entry["EntityName"] in list2_names
        mig = migration_by_entity.get(entry["EntityName"])
        checklist_lines.append(
            f'| {entry["EntityName"]} | `{entry["FilePath"]}` | {"2" if is_list2 else "1"} | ✅ | '
            f'{mig["MigrationId"] if mig else "—"} | {"✅" if is_list2 else "—"} |'
        )
    (P3B_DIR / "completion-checklist.md").write_text("\n".join(checklist_lines) + "\n", encoding="utf-8")

    manifest = {
        "sourceReport": "reports/p3a/tenant-isolation-report.json",
        "list1Fixed": len(list1),
        "list2Fixed": len(list2),
        "totalEntitiesFixed": len(list1) + len(list2),
        "sourceFilesChanged": sorted(changed_files),
        "migrationsGenerated": generated_migrations,
        "temporaryAggregateMigrationRemoved": True,
        "aegisDbContextUntouched": FORBIDDEN.read_bytes() == before_forbidden,
        "notes": [
            "List 1: ITenantScoped added only.",
            "List 2: TenantId + ITenantScoped added.",
            "Each List-2 migration adds a non-nullable Guid TenantId with no default and creates a TenantId index.",
            "Production execution requires a tenant backfill before each List-2 migration, as marked in every migration file.",
        ],
    }
    (P3B_DIR / "complete.json").write_text(json.dumps(manifest, indent=2), encoding="utf-8")

    print(
        f"P3B prepared successfully: {len(list1)} List-1 + {len(list2)} List-2 = "
        f"{len(list1) + len(list2)} entities; {len(generated_migrations)} migrations."
    )


if __name__ == "__main__":
    main()
