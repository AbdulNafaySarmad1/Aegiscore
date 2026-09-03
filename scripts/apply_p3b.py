#!/usr/bin/env python3
"""Apply Prompt 3B tenant-isolation fixes from the authoritative P3A report.

The script is deterministic and fail-closed. It patches only entities reported by
P3A, refuses to absorb unrelated EF model drift, writes one no-default migration
per List-2 entity, and leaves an auditable completion manifest/checklist.
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
VERIFY_MIGRATION_NAME = "__P3B_VerifyTenantSnapshot"


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
    # Some recovered files put [Table(...)] and the class declaration on one line.
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
    patched = text[: match.start()] + new_decl + match.group("brace") + text[match.end():]

    property_inserted = "false"
    if add_tenant_id:
        rematch = class_pattern(entity_name).search(patched)
        if rematch is None:
            fail(f"could not re-locate {entity_name} after interface patch")
        indent = rematch.group("indent")
        line_start = patched.rfind("\n", 0, rematch.start()) + 1
        physical_prefix = patched[line_start:rematch.start()]
        line_indent_match = re.match(r"[ \t]*", physical_prefix)
        if line_indent_match:
            indent = line_indent_match.group(0)
        insert_at = rematch.end("brace")
        patched = patched[:insert_at] + f"\n{indent}    public Guid TenantId {{ get; set; }}" + patched[insert_at:]
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


def extract_csharp_method_body(content: str, signature: str) -> str:
    start = content.find(signature)
    if start < 0:
        fail(f"could not find migration method signature: {signature}")
    brace = content.find("{", start + len(signature))
    if brace < 0:
        fail(f"could not find opening brace for migration method: {signature}")

    depth = 0
    in_string = False
    in_char = False
    escape = False
    for index in range(brace, len(content)):
        ch = content[index]
        if in_string:
            if escape:
                escape = False
            elif ch == "\\":
                escape = True
            elif ch == '"':
                in_string = False
            continue
        if in_char:
            if escape:
                escape = False
            elif ch == "\\":
                escape = True
            elif ch == "'":
                in_char = False
            continue
        if ch == '"':
            in_string = True
            continue
        if ch == "'":
            in_char = True
            continue
        if ch == "{":
            depth += 1
        elif ch == "}":
            depth -= 1
            if depth == 0:
                return content[brace + 1 : index]
    fail(f"migration method body did not terminate: {signature}")
    raise AssertionError("unreachable")


def migration_operations(content: str) -> tuple[str, list[str]]:
    body = extract_csharp_method_body(
        content,
        "protected override void Up(MigrationBuilder migrationBuilder)",
    )
    calls = re.findall(r"migrationBuilder\.([A-Za-z0-9_]+)\s*(?:<[^>]+>)?\s*\(", body)
    return body, sorted(set(calls))


def count_tenant_add_columns(body: str) -> int:
    return len(
        re.findall(
            r'migrationBuilder\.AddColumn<[^>]+>\s*\(\s*name:\s*"TenantId"',
            body,
            re.DOTALL,
        )
    )


def tenant_property_chain(block: str, full_type_name: str) -> str:
    marker = 'b.Property<Guid>("TenantId")'
    pos = block.find(marker)
    if pos < 0:
        fail(f"new snapshot has no Guid TenantId property for {full_type_name}")
    line_start = block.rfind("\n", 0, pos) + 1
    semicolon = block.find(";", pos)
    if semicolon < 0:
        fail(f"TenantId snapshot property did not terminate for {full_type_name}")
    return block[line_start : semicolon + 1]


def inject_tenant_property(old_block: str, new_block: str, full_type_name: str) -> str:
    if 'b.Property<Guid>("TenantId")' in old_block:
        return old_block

    chain = tenant_property_chain(new_block, full_type_name)
    insertion_candidates = [
        old_block.find("b.HasKey("),
        old_block.find("b.HasIndex("),
        old_block.find("b.ToTable("),
    ]
    positions = [pos for pos in insertion_candidates if pos >= 0]
    if not positions:
        fail(f"could not find safe TenantId insertion point in snapshot for {full_type_name}")
    pos = min(positions)
    line_start = old_block.rfind("\n", 0, pos) + 1
    return old_block[:line_start] + chain + "\n\n" + old_block[line_start:]


def patch_snapshot_tenant_only(original: str, generated: str, list2: list[dict]) -> tuple[str, list[dict]]:
    sanitized = original
    mappings: list[dict] = []

    for entry in list2:
        full_type_name = f'{entry["Namespace"]}.{entry["EntityName"]}'

        # Fail closed if P3B is entangled with a pre-existing CreateTable drift.
        old_block = extract_entity_block(sanitized, full_type_name)
        new_block = extract_entity_block(generated, full_type_name)

        old_mapping = table_mapping(original, entry["Namespace"], entry["EntityName"])
        new_mapping = table_mapping(generated, entry["Namespace"], entry["EntityName"])
        if old_mapping != new_mapping:
            fail(
                f"table mapping drift for {full_type_name}: old={old_mapping}, new={new_mapping}; "
                "refusing to mix unrelated mapping changes into P3B"
            )

        patched_block = inject_tenant_property(old_block, new_block, full_type_name)
        if patched_block == old_block:
            fail(f"original snapshot already contains TenantId for List-2 entity {full_type_name}")
        sanitized = sanitized.replace(old_block, patched_block, 1)

        mappings.append({
            **entry,
            "Table": old_mapping[0],
            "Schema": old_mapping[1],
        })

    return sanitized, mappings


def migration_content(migration_id: str, class_name: str, table: str, schema: str | None) -> str:
    schema_arg = f',\n                schema: "{schema}"' if schema else ""
    index_name = f'IX_{table}_TenantId'

    return f'''using Microsoft.EntityFrameworkCore.Infrastructure;\nusing Microsoft.EntityFrameworkCore.Migrations;\n\n#nullable disable\n\nnamespace AegisCore.Infrastructure.Migrations;\n\n[DbContext(typeof(AegisDbContext))]\n[Migration("{migration_id}")]\npublic partial class {class_name} : Migration\n{{\n    protected override void Up(MigrationBuilder migrationBuilder)\n    {{\n        // DATA MIGRATION REQUIRED — populate TenantId from the known tenant before this migration runs in production.\n        migrationBuilder.AddColumn<Guid>(\n            name: "TenantId",\n            table: "{table}"{schema_arg},\n            type: "uuid",\n            nullable: false);\n\n        migrationBuilder.CreateIndex(\n            name: "{index_name}",\n            table: "{table}",\n            column: "TenantId"{schema_arg});\n    }}\n\n    protected override void Down(MigrationBuilder migrationBuilder)\n    {{\n        migrationBuilder.DropIndex(\n            name: "{index_name}",\n            table: "{table}"{schema_arg});\n\n        migrationBuilder.DropColumn(\n            name: "TenantId",\n            table: "{table}"{schema_arg});\n    }}\n}}\n'''


def scaffold(name: str) -> tuple[Path, Path]:
    before = {p.name for p in MIGRATIONS.glob("*.cs")}
    run(
        "dotnet",
        "ef",
        "migrations",
        "add",
        name,
        "--project",
        "AegisCore.Infrastructure/AegisCore.Infrastructure.csproj",
        "--startup-project",
        "AegisCore.API/AegisCore.API.csproj",
        "--context",
        "AegisDbContext",
        "--output-dir",
        "Migrations",
    )
    after = {p.name for p in MIGRATIONS.glob("*.cs")}
    created = sorted(after - before)
    main_files = [n for n in created if n.endswith(f"_{name}.cs") and not n.endswith(".Designer.cs")]
    designers = [n for n in created if n.endswith(f"_{name}.Designer.cs")]
    if len(main_files) != 1 or len(designers) != 1:
        fail(f"expected one migration pair for {name}, got: {created}")
    return MIGRATIONS / main_files[0], MIGRATIONS / designers[0]


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
    original_snapshot = SNAPSHOT.read_text(encoding="utf-8")
    diffs: list[dict[str, str]] = []
    changed_files: set[str] = set()

    # Preflight: every List-2 entity must already exist in the migration snapshot.
    # Otherwise TenantId is entangled with unrelated CreateTable drift.
    for entry in list2:
        extract_entity_block(original_snapshot, f'{entry["Namespace"]}.{entry["EntityName"]}')

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
        fail("AegisDbContext.cs changed during apply_p3b.py; only the separate outlier relocation is allowed")

    # Scaffold against the current model to learn the exact TenantId representation,
    # but never commit the aggregate migration or its unrelated stale-model drift.
    temp_main, temp_designer = scaffold(TEMP_MIGRATION_NAME)
    temp_content = temp_main.read_text(encoding="utf-8")
    temp_body, initial_operations = migration_operations(temp_content)
    tenant_adds = count_tenant_add_columns(temp_body)
    if tenant_adds != len(list2):
        fail(
            f"aggregate migration contains {tenant_adds} TenantId AddColumn operations; "
            f"expected exactly {len(list2)}"
        )

    generated_snapshot = SNAPSHOT.read_text(encoding="utf-8")
    sanitized_snapshot, mappings = patch_snapshot_tenant_only(original_snapshot, generated_snapshot, list2)

    temp_main.unlink()
    temp_designer.unlink()
    SNAPSHOT.write_text(sanitized_snapshot, encoding="utf-8")

    # Verify the tenant delta is fully represented while pre-existing unrelated
    # drift remains unabsorbed. EF may still report that unrelated drift; it must
    # report zero remaining TenantId AddColumn operations.
    verify_main, verify_designer = scaffold(VERIFY_MIGRATION_NAME)
    verify_content = verify_main.read_text(encoding="utf-8")
    verify_body, verification_operations = migration_operations(verify_content)
    remaining_tenant_adds = count_tenant_add_columns(verify_body)
    if remaining_tenant_adds != 0:
        fail(
            f"sanitized snapshot still produces {remaining_tenant_adds} TenantId AddColumn operations; "
            "tenant delta is not fully captured"
        )

    verify_main.unlink()
    verify_designer.unlink()
    # EF verification advances the snapshot to the full current model. Restore the
    # sanitized tenant-only snapshot so unrelated drift is not committed by P3B.
    SNAPSHOT.write_text(sanitized_snapshot, encoding="utf-8")

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
    (P3B_DIR / "model-drift-diagnostic.json").write_text(
        json.dumps(
            {
                "initialOperations": initial_operations,
                "verificationOperations": verification_operations,
                "initialTenantAddColumns": tenant_adds,
                "remainingTenantAddColumnsAfterSanitizedSnapshot": remaining_tenant_adds,
                "unrelatedDriftWasNotCommitted": True,
            },
            indent=2,
        ),
        encoding="utf-8",
    )

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
        "verificationMigrationRemoved": True,
        "remainingTenantAddColumnsAfterVerification": remaining_tenant_adds,
        "unrelatedModelDriftAbsorbed": False,
        "aegisDbContextUntouchedDuringApply": FORBIDDEN.read_bytes() == before_forbidden,
        "notes": [
            "List 1: ITenantScoped added only.",
            "List 2: TenantId + ITenantScoped added.",
            "Each List-2 migration adds a non-nullable Guid TenantId with no default and creates a TenantId index.",
            "Production execution requires a tenant backfill before each List-2 migration, as marked in every migration file.",
            "Pre-existing unrelated EF model drift remains intentionally unabsorbed.",
        ],
    }
    (P3B_DIR / "complete.json").write_text(json.dumps(manifest, indent=2), encoding="utf-8")

    print(
        f"P3B prepared successfully: {len(list1)} List-1 + {len(list2)} List-2 = "
        f"{len(list1) + len(list2)} entities; {len(generated_migrations)} migrations; "
        f"remaining TenantId delta={remaining_tenant_adds}."
    )


if __name__ == "__main__":
    main()
