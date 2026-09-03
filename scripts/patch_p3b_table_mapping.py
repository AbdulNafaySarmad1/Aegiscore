#!/usr/bin/env python3
"""Broaden P3B's EF snapshot ToTable parser for overloads with callbacks.

EF emits forms such as:
    b.ToTable("CurriculumPrerequisites", t => { ... });
The P3B applicator only needs the table name and optional schema, so parsing the
first one or two string arguments is sufficient and avoids guessing mappings.
"""
from pathlib import Path

path = Path(__file__).resolve().with_name("apply_p3b.py")
text = path.read_text(encoding="utf-8")
old = "    match = re.search(r'b\\.ToTable\\(\"([^\"]+)\"(?:,\\s*\"([^\"]+)\")?\\);', block)\n"
new = "    match = re.search(r'b\\.ToTable\\(\"([^\"]+)\"(?:,\\s*\"([^\"]+)\")?', block)\n"

if old not in text:
    raise SystemExit("P3B ToTable parser signature was not found; refusing a blind patch")

path.write_text(text.replace(old, new, 1), encoding="utf-8")
print("Patched P3B ToTable parser to support callback overloads.")
