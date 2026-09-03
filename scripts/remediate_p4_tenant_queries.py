#!/usr/bin/env python3
"""Prompt 4 remediation for EF/global-filter bypasses.

Default behavior is to remove IgnoreQueryFilters() so P3B global tenant filters
apply. A tiny, reviewed allow-list is retained only for bootstrap/service code
that already enforces the tenant explicitly in the query and must work before
an ambient tenant context exists.
"""
from __future__ import annotations

import json
import re
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE_ROOTS = [ROOT / "AegisCore.Infrastructure", ROOT / "AegisCore.Application"]
REPORT_DIR = ROOT / "reports" / "p4"
EXCLUDED = {"bin", "obj", ".git"}
IGNORE = re.compile(r"\.IgnoreQueryFilters\s*\(\s*\)")

# These files were proven by the P4 regression suite to require pre-ambient-
# tenant lookups. Every retained query in these files carries an explicit
# TenantId == tenantId predicate (or an equivalent tenant predicate in the
# surrounding LINQ expression).
REVIEWED_EXPLICIT_TENANT_FILES = {
    "AegisCore.Infrastructure/Finance/AccountingStandardsEngine.cs",
    "AegisCore.Infrastructure/Onboarding/WizardGatewayService.cs",
    "AegisCore.Infrastructure/Onboarding/WizardSessionService.cs",
}

JUSTIFICATION = (
    "// P4 SAFE: explicit tenantId predicate scopes this query; "
    "IgnoreQueryFilters is required before ambient tenant context is available."
)


def rel(path: Path) -> str:
    return path.relative_to(ROOT).as_posix()


def has_explicit_tenant_scope(lines: list[str], index: int) -> bool:
    """Conservatively prove tenant scoping near an IgnoreQueryFilters call."""
    start = max(0, index - 2)
    end = min(len(lines), index + 12)
    context = "\n".join(lines[start:end])
    return bool(
        re.search(r"\bTenantId\s*==\s*tenantId\b", context)
        or re.search(r"\btenantId\s*==\s*[^\n;]*\bTenantId\b", context)
        or re.search(r"\bwhere\b[^\n]*(?:\.TenantId\s*==\s*tenantId|tenantId\s*==\s*[^\n]*\.TenantId)", context, re.IGNORECASE)
    )


def process_file(path: Path) -> tuple[str, int, int]:
    text = path.read_text(encoding="utf-8-sig")
    if not IGNORE.search(text):
        return text, 0, 0

    path_name = rel(path)
    lines = text.splitlines()
    out: list[str] = []
    removed = 0
    retained = 0

    for index, line in enumerate(lines):
        if not IGNORE.search(line):
            out.append(line)
            continue

        preserve = (
            path_name in REVIEWED_EXPLICIT_TENANT_FILES
            and has_explicit_tenant_scope(lines, index)
        )

        if preserve:
            # Avoid duplicating the marker if a retry runs after a previous
            # verified commit.
            if not (out and out[-1].strip().startswith("// P4 SAFE:")):
                indent = re.match(r"\s*", line).group(0)
                out.append(indent + JUSTIFICATION)
            out.append(line)
            retained += 1
        else:
            new_line, count = IGNORE.subn("", line)
            out.append(new_line)
            removed += count

    suffix = "\n" if text.endswith("\n") else ""
    return "\n".join(out) + suffix, removed, retained


def main() -> None:
    changed: list[dict[str, object]] = []
    removed_total = 0
    retained_total = 0

    for source_root in SOURCE_ROOTS:
        for path in sorted(source_root.rglob("*.cs")):
            if any(part in EXCLUDED for part in path.parts):
                continue
            original = path.read_text(encoding="utf-8-sig")
            new_text, removed, retained = process_file(path)
            if new_text == original:
                continue
            path.write_text(new_text, encoding="utf-8")
            removed_total += removed
            retained_total += retained
            changed.append({
                "file": rel(path),
                "removed": removed,
                "retainedJustified": retained,
            })
            print(
                f"P4_REMEDIATE {rel(path)}: removed {removed}, "
                f"retained-justified {retained}"
            )

    REPORT_DIR.mkdir(parents=True, exist_ok=True)
    report = {
        "strategy": (
            "remove IgnoreQueryFilters by default; retain only reviewed "
            "pre-ambient-tenant queries with explicit tenant predicates"
        ),
        "ignoreQueryFiltersRemoved": removed_total,
        "ignoreQueryFiltersRetainedJustified": retained_total,
        "reviewedFiles": sorted(REVIEWED_EXPLICIT_TENANT_FILES),
        "filesChanged": changed,
        "status": "ready_for_rescan",
    }
    (REPORT_DIR / "remediation.json").write_text(
        json.dumps(report, indent=2) + "\n", encoding="utf-8"
    )
    print(
        f"P4 remediation removed {removed_total} bypass(es), retained "
        f"{retained_total} explicitly tenant-scoped bypass(es) across "
        f"{len(changed)} changed files"
    )


if __name__ == "__main__":
    main()
