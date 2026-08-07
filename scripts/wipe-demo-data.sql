-- ─── AegisCore Demo Data Wipe ──────────────────────────────
-- Run: psql -U aegiscore -d aegiscore -f wipe-demo-data.sql
-- Deletes all demo data in FK-safe reverse order.

BEGIN;
DELETE FROM CourseMarks WHERE EXISTS (SELECT 1 FROM Enrollments e WHERE e.Id = CourseMarks.EnrollmentId AND e.TenantId = '1bebd63c-394e-3925-b4e3-8e53f65fac9a');
DELETE FROM Enrollments WHERE TenantId = '1bebd63c-394e-3925-b4e3-8e53f65fac9a';
DELETE FROM Attendances WHERE EnrollmentId IN (SELECT Id FROM Enrollments WHERE TenantId = '1bebd63c-394e-3925-b4e3-8e53f65fac9a');
DELETE FROM FeeDemands WHERE TenantId = '1bebd63c-394e-3925-b4e3-8e53f65fac9a';
DELETE FROM FeeInstallments WHERE TenantId = '1bebd63c-394e-3925-b4e3-8e53f65fac9a';
DELETE FROM FinancePayments WHERE TenantId = '1bebd63c-394e-3925-b4e3-8e53f65fac9a';
DELETE FROM FinanceScholarshipAwards WHERE TenantId = '1bebd63c-394e-3925-b4e3-8e53f65fac9a';
DELETE FROM StudentProfiles WHERE TenantId = '1bebd63c-394e-3925-b4e3-8e53f65fac9a';
DELETE FROM FacultyWorkloads WHERE TenantId = '1bebd63c-394e-3925-b4e3-8e53f65fac9a';
DELETE FROM CourseGrades WHERE TenantId = '1bebd63c-394e-3925-b4e3-8e53f65fac9a';
DELETE FROM MarkModerations WHERE TenantId = '1bebd63c-394e-3925-b4e3-8e53f65fac9a';
DELETE FROM AssessmentSchemes WHERE TenantId = '1bebd63c-394e-3925-b4e3-8e53f65fac9a';
DELETE FROM Sections WHERE TenantId = '1bebd63c-394e-3925-b4e3-8e53f65fac9a';
DELETE FROM Courses WHERE TenantId = '1bebd63c-394e-3925-b4e3-8e53f65fac9a';
DELETE FROM DegreePrograms WHERE TenantId = '1bebd63c-394e-3925-b4e3-8e53f65fac9a';
DELETE FROM Departments WHERE TenantId = '1bebd63c-394e-3925-b4e3-8e53f65fac9a';
DELETE FROM UserRoles WHERE TenantId = '1bebd63c-394e-3925-b4e3-8e53f65fac9a';
DELETE FROM Users WHERE TenantId = '1bebd63c-394e-3925-b4e3-8e53f65fac9a';
DELETE FROM Roles WHERE TenantId = '1bebd63c-394e-3925-b4e3-8e53f65fac9a' AND "IsSystem" = false;
COMMIT;
