#!/usr/bin/env python3
"""Broaden P3B's EF snapshot ToTable parser for overloads with callbacks.

EF emits forms such as:
    b.ToTable("CurriculumPrerequisites", t => { ... });
The P3B applicator only needs the table name and optional schema. This helper
locates the parser inside table_mapping() structurally rather than depending on
the exact escaped source string.
"""
from pathlib import Path

path = Path(__file__).resolve().with_name("apply_p3b.py")
text = path.read_text(encoding="utf-8")
lines = text.splitlines(keepends=True)

inside_table_mapping = False
matches = []
for index, line in enumerate(lines):
    if line.startswith("def table_mapping("):
        inside_table_mapping = True
        continue
    if inside_table_mapping and line.startswith("def "):
        break
    if inside_table_mapping and "match = re.search(" in line and "ToTable" in line:
        matches.append(index)

if len(matches) != 1:
    raise SystemExit(
        f"Expected exactly one ToTable parser inside table_mapping(), found {len(matches)}; refusing a blind patch"
    )

index = matches[0]
indent = lines[index][: len(lines[index]) - len(lines[index].lstrip())]
lines[index] = (
    indent
    + "match = re.search(r'b\\.ToTable\\(\"([^\"]+)\"(?:,\\s*\"([^\"]+)\")?', block)\n"
)
path.write_text("".join(lines), encoding="utf-8")
print("Patched P3B ToTable parser to support callback overloads.")
