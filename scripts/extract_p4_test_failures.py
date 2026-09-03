#!/usr/bin/env python3
"""Extract failed .NET test results into a small machine-readable P4 report."""
from __future__ import annotations

import json
import xml.etree.ElementTree as ET
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
RESULTS = ROOT / "TestResults" / "P4"
OUTPUT = ROOT / "reports" / "p4" / "test-failures.json"


def text_at(node: ET.Element, namespace: str, path: str) -> str | None:
    found = node.find(path.format(ns=namespace))
    if found is None or found.text is None:
        return None
    value = found.text.strip()
    return value or None


def main() -> None:
    trx_files = sorted(RESULTS.rglob("*.trx"))
    if not trx_files:
        raise SystemExit(f"P4 DIAGNOSTIC ERROR: no TRX files found under {RESULTS.relative_to(ROOT)}")

    failures: list[dict[str, object]] = []
    totals = {"files": len(trx_files), "failed": 0, "passed": 0, "other": 0}

    for trx in trx_files:
        root = ET.parse(trx).getroot()
        namespace = ""
        if root.tag.startswith("{"):
            namespace = root.tag.split("}", 1)[0] + "}"

        for result in root.findall(f".//{namespace}UnitTestResult"):
            outcome = result.attrib.get("outcome", "Unknown")
            if outcome == "Passed":
                totals["passed"] += 1
                continue
            if outcome == "Failed":
                totals["failed"] += 1
            else:
                totals["other"] += 1

            failures.append({
                "testName": result.attrib.get("testName"),
                "outcome": outcome,
                "duration": result.attrib.get("duration"),
                "computerName": result.attrib.get("computerName"),
                "message": text_at(result, namespace, "{ns}Output/{ns}ErrorInfo/{ns}Message"),
                "stackTrace": text_at(result, namespace, "{ns}Output/{ns}ErrorInfo/{ns}StackTrace"),
                "stdOut": text_at(result, namespace, "{ns}Output/{ns}StdOut"),
                "trx": trx.relative_to(ROOT).as_posix(),
            })

    OUTPUT.parent.mkdir(parents=True, exist_ok=True)
    report = {
        "status": "regression_detected" if failures else "no_failures_recorded",
        "totals": totals,
        "failures": failures,
    }
    OUTPUT.write_text(json.dumps(report, indent=2) + "\n", encoding="utf-8")

    print(f"P4 diagnostic: {totals['failed']} failed, {totals['passed']} passed, {totals['other']} other")
    for failure in failures:
        print(f"P4_TEST_FAILURE {failure['testName']}: {failure['message'] or '<no message>'}")


if __name__ == "__main__":
    main()
