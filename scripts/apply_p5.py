#!/usr/bin/env python3
"""Apply Prompt 5 mechanically, audit placeholder guards, and verify bindings."""
from __future__ import annotations

import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
PROGRAM = ROOT / "AegisCore.API" / "Program.cs"
TEST = ROOT / "AegisCore.Tests" / "Infrastructure" / "EnvironmentSubstitutionTest.cs"
PRODUCTION = ROOT / "AegisCore.API" / "appsettings.Production.json"
REPORT_DIR = ROOT / "reports" / "p5"

CONFIG_USING = "using AegisCore.Infrastructure.Configuration;"
WIRE_LINE = "builder.Configuration.AddEnvironmentSubstitution();"
BUILD_LINE = "var app = builder.Build();"


def fail(message: str) -> None:
    raise SystemExit(f"P5 ERROR: {message}")


def patch_program() -> None:
    text = PROGRAM.read_text(encoding="utf-8-sig")
    if CONFIG_USING not in text:
        anchor = "using AegisCore.Infrastructure;\n"
        if anchor not in text:
            fail("Program.cs Infrastructure using anchor not found")
        text = text.replace(anchor, anchor + CONFIG_USING + "\n", 1)
    if WIRE_LINE not in text:
        if text.count(BUILD_LINE) != 1:
            fail(f"expected exactly one '{BUILD_LINE}' in Program.cs")
        text = text.replace(BUILD_LINE, WIRE_LINE + "\n\n" + BUILD_LINE, 1)
    if text.index(WIRE_LINE) >= text.index(BUILD_LINE):
        fail("AddEnvironmentSubstitution() must appear before builder.Build()")
    PROGRAM.write_text(text, encoding="utf-8")


def write_test() -> None:
    TEST.parent.mkdir(parents=True, exist_ok=True)
    TEST.write_text(
        '''using AegisCore.Infrastructure.Configuration;\nusing Microsoft.Extensions.Configuration;\n\nnamespace AegisCore.Tests.Infrastructure;\n\npublic sealed class EnvironmentSubstitutionTest\n{\n    [Fact]\n    public void AddEnvironmentSubstitution_resolves_environment_variable_placeholder()\n    {\n        const string variableName = "TEST_VAR";\n        const string expected = "hello-resolved";\n        var previous = Environment.GetEnvironmentVariable(variableName);\n\n        try\n        {\n            Environment.SetEnvironmentVariable(variableName, expected);\n            var configuration = new ConfigurationBuilder()\n                .AddInMemoryCollection(new Dictionary<string, string?>\n                {\n                    ["TestKey"] = "${TEST_VAR}"\n                })\n                .AddEnvironmentSubstitution()\n                .Build();\n            Assert.Equal(expected, configuration["TestKey"]);\n        }\n        finally\n        {\n            Environment.SetEnvironmentVariable(variableName, previous);\n        }\n    }\n}\n''',
        encoding="utf-8",
    )


def scan_guards() -> list[dict[str, object]]:
    hits: list[dict[str, object]] = []
    excluded_parts = {".git", "bin", "obj", "node_modules", ".next"}
    needles = ('.StartsWith("${")', '.Contains("${")')
    for path in sorted(ROOT.rglob("*.cs")):
        if any(part in excluded_parts for part in path.parts):
            continue
        rel = path.relative_to(ROOT).as_posix()
        for line_number, line in enumerate(path.read_text(encoding="utf-8-sig").splitlines(), start=1):
            if any(needle in line for needle in needles):
                hit = {"file": rel, "line": line_number, "text": line.strip()}
                hits.append(hit)
                print(f"P5_GUARD {rel}:{line_number}: {line.strip()}")
    return hits


def get_path(data: dict, path: str):
    value = data
    for key in path.split(":"):
        if not isinstance(value, dict) or key not in value:
            return None
        value = value[key]
    return value


def verify_bindings() -> list[dict[str, object]]:
    data = json.loads(PRODUCTION.read_text(encoding="utf-8-sig"))
    # The prompt names several historical env vars. These rows record the actual
    # production bindings in this repository so no alias is silently invented.
    checks = [
        ("SMTP_HOST", "Email:SmtpHost", "SMTP_HOST"),
        ("SMTP_PORT", "Email:SmtpPort", "SMTP_PORT"),
        ("SMTP_FROM", "Email:FromAddress", "EMAIL_FROM_ADDRESS"),
        ("TWILIO_ACCOUNT_SID", "Twilio:AccountSid", "TWILIO_ACCOUNT_SID"),
        ("TWILIO_AUTH_TOKEN", "Twilio:AuthToken", "TWILIO_AUTH_TOKEN"),
        ("ELASTICSEARCH_URL", "Search:ElasticsearchUrl", "ELASTICSEARCH_URL"),
        ("OLLAMA_BASE_URL", "AI:Ollama:BaseUrl", "AI_OLLAMA_BASE_URL"),
        ("OPENAI_API_KEY", "AI:OpenAI:ApiKey", "AI_OPENAI_API_KEY"),
        ("RAAST_ENDPOINT", "Raast:BaseUrl", "RAAST_BASE_URL"),
    ]
    results = []
    for prompt_key, config_path, actual_env in checks:
        expected = "${" + actual_env + "}"
        configured = get_path(data, config_path)
        if configured != expected:
            fail(f"{config_path} expected {expected!r}, found {configured!r}")
        results.append({
            "promptKey": prompt_key,
            "configPath": config_path,
            "actualEnvironmentVariable": actual_env,
            "placeholder": configured,
            "promptNameMatchesRepository": prompt_key == actual_env,
            "resolvesThroughEnvironmentSubstitution": True,
        })
    return results


def main() -> None:
    patch_program()
    write_test()
    hits = scan_guards()
    bindings = verify_bindings()

    REPORT_DIR.mkdir(parents=True, exist_ok=True)
    report = {
        "programWiredBeforeBuild": True,
        "integrationTest": TEST.relative_to(ROOT).as_posix(),
        "guardCount": len(hits),
        "guards": hits,
        "verifiedBindings": bindings,
        "status": "guards_pending_review" if hits else "complete",
    }
    (REPORT_DIR / "guard-scan.json").write_text(json.dumps(report, indent=2) + "\n", encoding="utf-8")
    (REPORT_DIR / "key-verification.json").write_text(json.dumps(bindings, indent=2) + "\n", encoding="utf-8")

    md = [
        "# P5 Placeholder Guard Scan",
        "",
        f"Guard count: **{len(hits)}**",
        "",
        "| File | Line | Guard |",
        "|---|---:|---|",
    ]
    for hit in hits:
        escaped = str(hit["text"]).replace("|", "\\|")
        md.append(f'| `{hit["file"]}` | {hit["line"]} | `{escaped}` |')
    (REPORT_DIR / "guard-scan.md").write_text("\n".join(md) + "\n", encoding="utf-8")

    complete = REPORT_DIR / "complete.json"
    if hits:
        if complete.exists():
            complete.unlink()
    else:
        complete.write_text(json.dumps({
            "status": "complete",
            "programWiredBeforeBuild": True,
            "integrationTest": TEST.relative_to(ROOT).as_posix(),
            "deadGuardCount": 0,
            "verifiedBindings": bindings,
        }, indent=2) + "\n", encoding="utf-8")

    print(f"P5 mechanical changes prepared; dead placeholder guards found: {len(hits)}")


if __name__ == "__main__":
    main()
