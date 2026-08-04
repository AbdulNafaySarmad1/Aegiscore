================================================================================
                    AEGISCORE UNIVERSITY ERP — DATA FORMAT TEMPLATES
                                    README
================================================================================

OVERVIEW
--------
This folder contains CSV (Comma-Separated Values) template files for bulk
data upload into the AegisCore University ERP system. Each file defines the
EXACT column layout that AegisCore expects when importing data through the
Bulk Upload module.

These templates are designed to be opened in Microsoft Excel, Google Sheets,
LibreOffice Calc, or any spreadsheet application — or edited directly in a
plain-text editor.


FILE LIST
---------
  students.csv          — Student personal and academic records
  faculty.csv           — Faculty and staff records
  courses.csv           — Course catalogue entries
  student_enrollments.csv — Course enrollment records for each student
  grades.csv            — Student grade entries per assessment
  fee_structure.csv     — Fee types and amounts per program/semester
  fee_payments.csv      — Individual payment transactions
  timetable.csv         — Weekly class schedule
  attendance.csv        — Daily attendance records
  exam_schedule.csv     — Examination dates and room assignments
  placement_records.csv — Student internship and job placements
  alumni.csv            — Graduated student records
  transcripts.csv       — Official transcript entries
  library_books.csv     — Library book inventory
  visitors.csv          — Campus visitor log


HOW TO USE EACH TEMPLATE
------------------------
  1. Open the .csv template file in your spreadsheet application.
  2. The FIRST ROW contains the column headers — DO NOT MODIFY THESE.
     They must match AegisCore's field names exactly.
  3. Delete the example data rows (rows 2 onwards) and replace them with
     your actual data.
  4. Each row below the header represents one record (student, course, etc.).
  5. Save the file as CSV (Comma Separated Values).
  6. Log into the AegisCore portal, navigate to Data Management → Bulk Upload,
     select the appropriate data category, and upload your CSV file.
  7. The system will validate your data before importing. Review any flagged
     rows, correct errors, and re-upload if needed.


ENCODING REQUIREMENTS
---------------------
  ✓ Files MUST be saved in UTF-8 encoding (without BOM).
  ✓ This ensures proper handling of accented characters, Urdu script names
    that may appear transliterated, and special symbols like the rupee sign (₨).
  ✓ In Microsoft Excel: Use "File → Save As → CSV UTF-8 (Comma delimited) (*.csv)".
  ✓ In Google Sheets: "File → Download → Comma Separated Values (.csv)"
    automatically uses UTF-8.
  ✓ NOTEPAD and most code editors also save UTF-8 by default.


DATE FORMAT REQUIREMENTS
------------------------
  ✓ ALL date fields MUST use the ISO 8601 format:  YYYY-MM-DD
  ✓ Examples:  2024-09-01      (September 1, 2024)
                2024-10-15      (October 15, 2024)
                2025-01-05      (January 5, 2025)
  ✓ Date-time fields (e.g., GradedAt, VerifiedAt, CheckInTime) use:
                YYYY-MM-DDTHH:MM:SS
    Examples:  2024-10-15T10:30:00
               2024-12-20T14:00:00
  ✗ DO NOT use: 01/09/2024, 09-01-2024, 1st September 2024, or any
    other date format. These will cause validation errors.
  ✗ DO NOT use 12-hour AM/PM notation in time fields.


NUMBER FORMAT REQUIREMENTS
--------------------------
  ✓ Use plain digits without thousand separators (commas).
    Correct:   85000           (eighty-five thousand)
    Incorrect: 85,000

  ✓ Use a period (.) as the decimal separator, not a comma.
    Correct:   3.62            (three point six two)
    Incorrect: 3,62

  ✓ Percentages should be entered as whole numbers:
    Correct:   5               (means 5%)
    Incorrect: 0.05 or 5%


SPECIAL CHARACTERS TO AVOID IN TEXT FIELDS
------------------------------------------
  ✗ DO NOT use double quotes (") inside text values.
    Use single quotes (') instead if needed.

  ✗ DO NOT use commas (,) within text field values — they will break
    the CSV column structure. If a comma is absolutely necessary, use
    a semicolon (;) or pipe (|) instead.

  ✗ DO NOT use line breaks (Enter key) inside a single cell value.

  ✗ DO NOT use the following characters as they may cause parsing issues:
    - Backslash (\)
    - Leading or trailing spaces (they will be trimmed automatically)
    - Tab characters

  ✓ The hyphen (-) and underscore (_) are safe to use.
  ✓ The at sign (@), hash (#), and forward slash (/) are safe.
  ✓ The ampersand (&) is safe in most fields but avoid it in ID fields.


MAXIMUM FILE SIZE LIMITS
------------------------
  ✓ Maximum file size per upload: 25 MB
  ✓ If your data exceeds this limit, split it into multiple CSV files
    and upload them separately.
  ✓ Maximum rows per file: 10,000
  ✓ Files exceeding the row limit will be rejected. Split large datasets
    into batches.


ERROR HANDLING — WHAT HAPPENS TO BAD ROWS
-----------------------------------------
  ✓ AegisCore validates EVERY row before importing ANY data.
  ✓ The import is ALL-OR-NOTHING. If any row has an error, the ENTIRE
    batch is rejected by default. No partial imports occur.

  ✓ Error reports show:
      - The EXACT row number(s) with problems
      - The column(s) that failed validation
      - A human-readable description of what went wrong
        (e.g., "Row 15: DateOfBirth must be in YYYY-MM-DD format")

  ✓ Correct the flagged rows and re-upload. Rows are NOT silently dropped.
    The system never skips bad rows and continues — you will always be
    notified of every issue.

  ✓ Common errors to watch for:
      - Missing values in required columns (check each template's example
        for which columns can be left blank)
      - Duplicate IDs (StudentId, FacultyId, CourseCode must be unique)
      - References to IDs that don't exist yet (e.g., enrolling a student
        in a course code that hasn't been uploaded)
      - Invalid enum values (e.g., using "Passed" instead of "Completed")


NEED HELP?
----------
  ✓ If your data doesn't fit any of the provided templates, contact the
    AegisCore support team at:  support@aegiscore.edu.pk

  ✓ Include in your email:
      - Your institution name
      - A brief description of the data you want to upload
      - A sample of your data (a few rows, no sensitive info)

  ✓ For urgent issues, call:  +92-42-111-AEGIS (23447  Ext. 5)

  ✓ Documentation portal:  https://docs.aegiscore.edu.pk/bulk-upload
  ✓ Training videos:       https://training.aegiscore.edu.pk


================================================================================
  Version: 1.0  |  Last Updated: August 2025  |  AegisCore University ERP
================================================================================
