# AegisCore — Codebase Cleanup Prompt

## Role
You are a .NET 10 / C# 14 engineer performing a cleanup pass on the AegisCore codebase. Your job is to remove dead weight, fix code quality issues, and leave the codebase cleaner than you found it — without changing any behaviour.

## Golden Rule
**Behaviour must be identical before and after cleanup. If you are unsure whether removing something changes behaviour, leave it and flag it.**

## Cleanup Targets

### 1. Dead Code Removal
- Remove unused `using` directives (run dotnet format or Roslyn analyser first)
- Remove unused private methods, fields, and properties (confirm with IDE warnings)
- Remove commented-out code blocks older than the last meaningful commit
- Remove empty catch blocks (replace with proper logging or re-throw)
- Remove unreachable code paths

### 2. Hardcoded Value Extraction
- Find every hardcoded string that should be a config value:
  - University names
  - URLs and connection strings
  - Email addresses
  - Timeout values
  - Role names and permission strings
- Move each to `appsettings.json` under a sensible key
- Reference via `IConfiguration` or strongly-typed options pattern
- Never leave a hardcoded value that makes the system SSUET-specific or any-university-specific

### 3. TODO / FIXME / HACK Resolution
- List every TODO, FIXME, HACK comment in the codebase with file + line
- For each one, either:
  - Fix it now (if trivial and safe)
  - Create a numbered GitHub Issue for it and replace the comment with `// See Issue #N`
  - Delete it if it is no longer relevant

### 4. Naming Consistency
- Enforce C# naming conventions:
  - PascalCase for classes, methods, properties
  - camelCase for local variables and parameters
  - _camelCase for private fields
  - UPPER_SNAKE_CASE for constants
- Rename any identifiers that are misleading or abbreviated beyond readability
- Do NOT rename public API surface (controllers, DTOs) — breaking change risk

### 5. God Class Decomposition
- Identify any class exceeding 300 lines
- For each: propose a decomposition plan — do not execute without approval
- Flag for Sprint review

### 6. Logging Hygiene
- Replace all `Console.WriteLine` with proper `ILogger<T>` calls
- Ensure log levels are appropriate (Debug for trace, Info for events, Warn for degraded, Error for failures)
- Remove any logs that print sensitive data (passwords, connection strings, tokens)

### 7. Exception Handling
- Remove empty catch blocks
- Replace generic `catch (Exception e)` where a specific exception type is known
- Ensure every caught exception is either logged or re-thrown (never silently swallowed)

### 8. Test Coverage Check
- Run existing test suite and report pass/fail count
- List any test files that are empty or contain only placeholder tests
- Flag any critical business logic classes with zero test coverage

## Output
After cleanup, produce:

```
CLEANUP REPORT
--------------
Files Modified: [count and list]
Hardcoded Values Extracted: [count]
Dead Code Removed: [count of methods/classes/files]
TODOs Resolved: [count] | Issued: [count] | Deleted: [count]
Console.WriteLines Replaced: [count]
Empty Catches Fixed: [count]
God Classes Flagged (not yet decomposed): [list]
Test Suite: [X passing / Y failing / Z skipped]
Uncovered Critical Classes: [list]
Behaviour Change Risk: None — all changes are non-behavioural
```
