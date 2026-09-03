#!/usr/bin/env python3
"""Prompt 4 static audit for EF/global-filter bypasses.

Scans AegisCore.Infrastructure and AegisCore.Application for:
- FromSqlRaw / ExecuteSqlRaw (+ async variants)
- direct IDbConnection / SqlConnection / NpgsqlConnection usage
- high-risk AsNoTracking on named sensitive entities
- IgnoreQueryFilters with nearby justification comments

The scanner is deliberately conservative: anything whose tenant safety cannot be
proven from the local source window is marked review_required rather than guessed.
"""
from __future__ import annotations

import json
import re
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE_ROOTS = [ROOT / "AegisCore.Infrastructure", ROOT / "AegisCore.Application"]
REPORT_DIR = ROOT / "reports" / "p4"
EXCLUDED_PARTS = {"bin", "obj", ".git"}
SENSITIVE = ("Attendance", "CourseGrade", "SsoSession", "AdmissionApplication")

RAW_RE = re.compile(r"\.(FromSqlRaw|ExecuteSqlRaw|ExecuteSqlRawAsync)\s*\(")
DIRECT_RE = re.compile(r"\b(IDbConnection|SqlConnection|NpgsqlConnection)\b")
IGNORE_RE = re.compile(r"\.IgnoreQueryFilters\s*\(")
AS_NO_TRACKING_RE = re.compile(r"\.AsNoTracking\s*\(")


def source_files():
    for source_root in SOURCE_ROOTS:
        for path in sorted(source_root.rglob("*.cs")):
            if any(part in EXCLUDED_PARTS for part in path.parts):
                continue
            yield path


def rel(path: Path) -> str:
    return path.relative_to(ROOT).as_posix()


def window(lines: list[str], index: int, radius: int = 5) -> str:
    start = max(0, index - radius)
    end = min(len(lines), index + radius + 1)
    return "\n".join(lines[start:end])


def extract_sql(lines: list[str], index: int) -> str:
    text = "\n".join(lines[index:min(len(lines), index + 10)])
    # Capture the first normal or verbatim C# string literal after the method call.
    match = re.search(r"(?:FromSqlRaw|ExecuteSqlRaw(?:Async)?)\s*\(\s*@?\"((?:\\.|[^\"\\])*)\"", text, re.DOTALL)
    if match:
        return re.sub(r"\s+", " ", match.group(1)).strip()
    return "<dynamic-or-multiline-expression>"


def classify_raw(sql: str, context: str) -> tuple[str, str]:
    normalized = re.sub(r"\s+", " ", sql).strip().rstrip(";").upper()
    if normalized == "SELECT 1":
        return "safe", "Database liveness probe; no tenant-scoped table is accessed."
    if sql != "<dynamic-or-multiline-expression>":
        has_tenant_predicate = bool(re.search(r"\bTenantId\b|\btenant_id\b", sql, re.IGNORECASE))
        if has_tenant_predicate:
            return "likely_safe_review", "SQL contains an explicit tenant predicate; parameterization still requires review."
    if re.search(r"\bTenantId\b|\btenant_id\b", context, re.IGNORECASE):
        return "review_required", "Nearby code references tenant context, but tenant enforcement cannot be proven statically."
    return "review_required", "Raw SQL bypasses EF global query filters and no explicit tenant predicate was proven."


def nearby_comment(lines: list[str], index: int) -> str | None:
    comments = []
    for pos in range(max(0, index - 4), index):
        stripped = lines[pos].strip()
        if stripped.startswith("//"):
            comments.append(stripped[2:].strip())
    return " ".join(comments) if comments else None


def main() -> None:
    raw_calls = []
    direct_connections = []
    sensitive_no_tracking = []
    ignore_calls = []

    for path in source_files():
        lines = path.read_text(encoding="utf-8-sig").splitlines()
        for index, line in enumerate(lines):
            line_no = index + 1
            raw_match = RAW_RE.search(line)
            if raw_match:
                sql = extract_sql(lines, index)
                context = window(lines, index, 6)
                status, reason = classify_raw(sql, context)
                item = {
                    "file": rel(path),
                    "line": line_no,
                    "method": raw_match.group(1),
                    "sql": sql,
                    "tenantSafety": status,
                    "reason": reason,
                    "sourceWindow": context,
                }
                raw_calls.append(item)
                print(f"P4_RAW {item['file']}:{line_no} {item['method']} {status} SQL={sql}")

            direct_match = DIRECT_RE.search(line)
            if direct_match:
                item = {
                    "file": rel(path),
                    "line": line_no,
                    "type": direct_match.group(1),
                    "source": line.strip(),
                    "sourceWindow": window(lines, index, 5),
                    "status": "review_required",
                }
                direct_connections.append(item)
                print(f"P4_DIRECT {item['file']}:{line_no} {item['type']} {line.strip()}")

            if AS_NO_TRACKING_RE.search(line):
                context = window(lines, index, 8)
                matched = [name for name in SENSITIVE if re.search(rf"\b{re.escape(name)}\b", context)]
                if matched:
                    item = {
                        "file": rel(path),
                        "line": line_no,
                        "entities": matched,
                        "source": line.strip(),
                        "sourceWindow": context,
                        "risk": "high_review",
                        "note": "AsNoTracking does not bypass global filters; flagged by Prompt 4 for manual review.",
                    }
                    sensitive_no_tracking.append(item)
                    print(f"P4_NOTRACK {item['file']}:{line_no} entities={','.join(matched)}")

            if IGNORE_RE.search(line):
                comment = nearby_comment(lines, index)
                item = {
                    "file": rel(path),
                    "line": line_no,
                    "source": line.strip(),
                    "justification": comment,
                    "status": "justified" if comment else "missing_justification",
                    "sourceWindow": window(lines, index, 5),
                }
                ignore_calls.append(item)
                print(f"P4_IGNORE {item['file']}:{line_no} {item['status']} comment={comment!r}")

    raw_review = [item for item in raw_calls if item["tenantSafety"] == "review_required"]
    direct_review = list(direct_connections)
    ignore_review = [item for item in ignore_calls if item["status"] == "missing_justification"]

    report = {
        "scope": ["AegisCore.Infrastructure/", "AegisCore.Application/"],
        "rawSqlCalls": raw_calls,
        "directConnections": direct_connections,
        "sensitiveAsNoTracking": sensitive_no_tracking,
        "ignoreQueryFilters": ignore_calls,
        "summary": {
            "rawSqlCount": len(raw_calls),
            "rawSqlReviewRequired": len(raw_review),
            "directConnectionCount": len(direct_connections),
            "sensitiveAsNoTrackingCount": len(sensitive_no_tracking),
            "ignoreQueryFiltersCount": len(ignore_calls),
            "ignoreMissingJustification": len(ignore_review),
        },
        "status": "review_required" if (raw_review or direct_review or ignore_review) else "complete",
    }

    REPORT_DIR.mkdir(parents=True, exist_ok=True)
    (REPORT_DIR / "audit.json").write_text(json.dumps(report, indent=2) + "\n", encoding="utf-8")

    md = [
        "# P4 Tenant Query Bypass Audit",
        "",
        "## Raw SQL",
        "",
        "| File | Line | Method | SQL | Tenant-safe? |",
        "|---|---:|---|---|---|",
    ]
    for item in raw_calls:
        sql = str(item["sql"]).replace("|", "\\|")
        md.append(f'| `{item["file"]}` | {item["line"]} | {item["method"]} | `{sql}` | {item["tenantSafety"]} |')
    md += ["", "## Direct connections", "", "| File | Line | Type | Fixed? |", "|---|---:|---|---|"]
    for item in direct_connections:
        md.append(f'| `{item["file"]}` | {item["line"]} | {item["type"]} | ❌ review |')
    md += ["", "## IgnoreQueryFilters", "", "| File | Line | Justification |", "|---|---:|---|"]
    for item in ignore_calls:
        justification = (item["justification"] or "MISSING").replace("|", "\\|")
        md.append(f'| `{item["file"]}` | {item["line"]} | {justification} |')
    md += ["", "## Sensitive AsNoTracking review", ""]
    for item in sensitive_no_tracking:
        md.append(f'- `{item["file"]}:{item["line"]}` — {", ".join(item["entities"])}')
    (REPORT_DIR / "audit.md").write_text("\n".join(md) + "\n", encoding="utf-8")

    if report["status"] == "complete":
        (REPORT_DIR / "complete.json").write_text(json.dumps({"status": "complete", **report["summary"]}, indent=2) + "\n", encoding="utf-8")
    else:
        complete = REPORT_DIR / "complete.json"
        if complete.exists():
            complete.unlink()

    print("P4 summary:", json.dumps(report["summary"], sort_keys=True))
    print("P4 status:", report["status"])


if __name__ == "__main__":
    main()
