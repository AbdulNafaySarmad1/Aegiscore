#!/usr/bin/env python3
"""Prompt 4 remediation: restore EF tenant filters by removing IgnoreQueryFilters().

P3B made every DbSet entity ITenantScoped, so the safest default is to let the
AegisDbContext global tenant filter apply. Intentional cross-tenant operations
must be reintroduced explicitly later with a documented justification.
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


def main() -> None:
    changed: list[dict[str, object]] = []
    removed = 0

    for source_root in SOURCE_ROOTS:
        for path in sorted(source_root.rglob("*.cs")):
            if any(part in EXCLUDED for part in path.parts):
                continue
            text = path.read_text(encoding="utf-8-sig")
            new_text, count = IGNORE.subn("", text)
            if count == 0:
                continue
            path.write_text(new_text, encoding="utf-8")
            removed += count
            changed.append({"file": path.relative_to(ROOT).as_posix(), "removed": count})
            print(f"P4_REMEDIATE {path.relative_to(ROOT).as_posix()}: removed {count} IgnoreQueryFilters bypass(es)")

    REPORT_DIR.mkdir(parents=True, exist_ok=True)
    report = {
        "strategy": "remove IgnoreQueryFilters so P3B global tenant filters apply by default",
        "ignoreQueryFiltersRemoved": removed,
        "filesChanged": changed,
        "status": "ready_for_rescan",
    }
    (REPORT_DIR / "remediation.json").write_text(json.dumps(report, indent=2) + "\n", encoding="utf-8")
    print(f"P4 remediation removed {removed} IgnoreQueryFilters bypass(es) across {len(changed)} files")


if __name__ == "__main__":
    main()
