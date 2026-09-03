#!/usr/bin/env python3
"""Resolve repository-layout outliers discovered by P3A before applying P3B."""

from __future__ import annotations

import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
CONTEXT = ROOT / "AegisCore.Infrastructure" / "AegisDbContext.cs"
SCALING = ROOT / "AegisCore.Infrastructure" / "ScalingEvent.cs"
REPORT = ROOT / "reports" / "p3a" / "tenant-isolation-report.json"
P3B_DIR = ROOT / "reports" / "p3b"
MARKER = "\n/// <summary>\n/// Represents a scaling event in the system.\n/// </summary>\npublic class ScalingEvent\n"

SCALING_SOURCE = '''namespace AegisCore.Infrastructure;\n\n/// <summary>\n/// Represents a scaling event in the system.\n/// </summary>\npublic class ScalingEvent\n{\n    /// <summary>\n    /// Unique identifier.\n    /// </summary>\n    public Guid Id { get; set; } = Guid.NewGuid();\n\n    /// <summary>\n    /// Type of the scaling event.\n    /// </summary>\n    public string EventType { get; set; } = string.Empty;\n\n    /// <summary>\n    /// Optional replica name.\n    /// </summary>\n    public string? ReplicaName { get; set; }\n\n    /// <summary>\n    /// Total number of replicas after the event.\n    /// </summary>\n    public int TotalReplicas { get; set; }\n\n    /// <summary>\n    /// Timestamp of the event.\n    /// </summary>\n    public DateTime Timestamp { get; set; } = DateTime.UtcNow;\n\n    /// <summary>\n    /// Tenant identifier.\n    /// </summary>\n    public Guid TenantId { get; set; }\n}\n'''


def fail(message: str) -> None:
    raise SystemExit(f"P3B OUTLIER ERROR: {message}")


def main() -> None:
    report = json.loads(REPORT.read_text(encoding="utf-8"))
    scaling_entries = [
        entry for entry in report["List1HasTenantIdNoInterface"]
        if entry["EntityName"] == "ScalingEvent"
    ]
    if len(scaling_entries) != 1:
        fail(f"expected one ScalingEvent P3A entry, found {len(scaling_entries)}")

    context_text = CONTEXT.read_text(encoding="utf-8")
    marker_index = context_text.find(MARKER)

    if marker_index >= 0:
        tail = context_text[marker_index:]
        required_fragments = [
            "public class ScalingEvent",
            "public Guid Id { get; set; } = Guid.NewGuid();",
            "public string EventType { get; set; } = string.Empty;",
            "public string? ReplicaName { get; set; }",
            "public int TotalReplicas { get; set; }",
            "public DateTime Timestamp { get; set; } = DateTime.UtcNow;",
            "public Guid TenantId { get; set; }",
        ]
        missing = [fragment for fragment in required_fragments if fragment not in tail]
        if missing:
            fail(f"ScalingEvent shape changed; refusing relocation. Missing: {missing}")

        CONTEXT.write_text(context_text[:marker_index].rstrip() + "\n", encoding="utf-8")
        SCALING.write_text(SCALING_SOURCE, encoding="utf-8")
    elif not SCALING.exists():
        fail("ScalingEvent is neither at the known context-file location nor already relocated")

    entry = scaling_entries[0]
    entry["FilePath"] = "AegisCore.Infrastructure/ScalingEvent.cs"
    entry["Namespace"] = "AegisCore.Infrastructure"
    REPORT.write_text(json.dumps(report, indent=2), encoding="utf-8")

    P3B_DIR.mkdir(parents=True, exist_ok=True)
    (P3B_DIR / "outlier-resolution.json").write_text(
        json.dumps({
            "entity": "ScalingEvent",
            "originalLocation": "AegisCore.Infrastructure/AegisDbContext.cs",
            "newLocation": "AegisCore.Infrastructure/ScalingEvent.cs",
            "reason": (
                "P3A found a DbSet entity declared after the DbContext class in AegisDbContext.cs. "
                "A non-partial C# class cannot be made ITenantScoped from another file, so the standalone "
                "entity declaration was relocated without changing any AegisDbContext member or model configuration."
            ),
            "dbContextBehaviorChanged": False,
            "tenantIdAlreadyPresent": True,
        }, indent=2),
        encoding="utf-8",
    )

    print("Resolved P3B outlier: relocated standalone ScalingEvent declaration.")


if __name__ == "__main__":
    main()
