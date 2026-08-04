import { appSchema, tableSchema } from '@nozbe/watermelondb';

export const schema = appSchema({
  version: 1,
  tables: [
    tableSchema({
      name: 'courses',
      columns: [
        { name: 'course_id', type: 'string', isIndexed: true },
        { name: 'course_code', type: 'string' },
        { name: 'course_name', type: 'string' },
        { name: 'credits', type: 'number' },
        { name: 'faculty_id', type: 'string', isIndexed: true },
        { name: 'enrollment_count', type: 'number' },
        { name: 'synced_at', type: 'number' }
      ]
    }),
    tableSchema({
      name: 'grades',
      columns: [
        { name: 'grade_id', type: 'string', isIndexed: true },
        { name: 'course_id', type: 'string', isIndexed: true },
        { name: 'student_id', type: 'string', isIndexed: true },
        { name: 'letter_grade', type: 'string' },
        { name: 'gpa_points', type: 'number' },
        { name: 'synced_at', type: 'number' }
      ]
    }),
    tableSchema({
      name: 'attendance_events',
      columns: [
        { name: 'event_id', type: 'string', isIndexed: true },
        { name: 'course_id', type: 'string', isIndexed: true },
        { name: 'student_id', type: 'string', isIndexed: true },
        { name: 'status', type: 'string' },
        { name: 'was_offline_at', type: 'number' },
        { name: 'synced_at', type: 'number', isOptional: true }
      ]
    })
  ]
});
