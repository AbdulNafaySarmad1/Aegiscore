export type MobileScreen = {
  key: string;
  title: string;
  icon: string;
  summary: string;
  actions: string[];
  stats: Array<{ label: string; value: string; tone?: 'danger' | 'ok' }>;
};

export const studentScreens: MobileScreen[] = [
  { key: 'student-login', title: 'Login', icon: 'ID', summary: 'Microsoft 365 session with biometric unlock.', actions: ['Continue with Microsoft', 'Use biometrics'], stats: [{ label: 'Session', value: 'Secure', tone: 'ok' }] },
  { key: 'student-dashboard', title: 'Dashboard', icon: 'DB', summary: 'Semester overview, GPA, attendance and fee balance.', actions: ['Pay fee', 'View results', 'Hall ticket'], stats: [{ label: 'CGPA', value: '3.42' }, { label: 'Attendance', value: '82%', tone: 'ok' }, { label: 'Fee', value: 'PKR 0' }] },
  { key: 'student-attendance', title: 'Attendance', icon: 'AT', summary: 'Course-wise attendance with offline cached history.', actions: ['Refresh', 'Show shortage'], stats: [{ label: 'Algorithms', value: '78%' }, { label: 'Databases', value: '74%', tone: 'danger' }] },
  { key: 'student-grades', title: 'Grades', icon: 'GR', summary: 'Results, GPA calculator and cached transcript download.', actions: ['Download transcript', 'Calculate GPA'], stats: [{ label: 'SGPA', value: '3.55' }, { label: 'Credits', value: '16' }] },
  { key: 'student-schedule', title: 'Schedule', icon: 'TM', summary: 'Week timetable with class reminders and venue details.', actions: ['Add to calendar', 'Venue map'], stats: [{ label: 'Today', value: '4 classes' }] },
  { key: 'student-fees', title: 'Fees', icon: 'PKR', summary: 'Fee statement, payment history and receipt cache.', actions: ['JazzCash', 'EasyPaisa', 'Receipt'], stats: [{ label: 'Balance', value: 'PKR 0', tone: 'ok' }] },
  { key: 'student-notifications', title: 'Notifications', icon: 'NT', summary: 'Inbox, search and notification preferences.', actions: ['Mark all read', 'Preferences'], stats: [{ label: 'Unread', value: '3' }] },
  { key: 'student-support', title: 'Support', icon: 'SP', summary: 'Submit grievances and track resolution offline-first.', actions: ['New grievance', 'Sync replies'], stats: [{ label: 'Open', value: '1' }] },
  { key: 'student-profile', title: 'Profile', icon: 'PF', summary: 'Profile, enrolled courses and certificates.', actions: ['Edit phone', 'Download certificate'], stats: [{ label: 'Courses', value: '5' }] }
];

export const facultyScreens: MobileScreen[] = [
  { key: 'faculty-login', title: 'Login', icon: 'ID', summary: 'Microsoft 365 session with biometric unlock.', actions: ['Continue with Microsoft', 'Use biometrics'], stats: [{ label: 'Session', value: 'Secure', tone: 'ok' }] },
  { key: 'faculty-dashboard', title: 'Dashboard', icon: 'DB', summary: 'Courses, marks progress and at-risk counts.', actions: ['Open marks', 'Open attendance'], stats: [{ label: 'Courses', value: '4' }, { label: 'At risk', value: '7', tone: 'danger' }] },
  { key: 'faculty-attendance', title: 'Attendance', icon: 'AT', summary: 'Offline-first attendance marking with 30 minute buffer.', actions: ['Mark all present', 'Save draft', 'Submit'], stats: [{ label: 'Buffered', value: '12 min' }] },
  { key: 'faculty-marks', title: 'Marks', icon: 'MK', summary: 'Validated marks entry with draft and lock states.', actions: ['Save draft', 'Submit locked', 'Upload CSV'], stats: [{ label: 'Complete', value: '68%' }] },
  { key: 'faculty-materials', title: 'Materials', icon: 'MT', summary: 'Upload and browse lecture material history.', actions: ['Upload', 'Delete', 'History'], stats: [{ label: 'Files', value: '24' }] },
  { key: 'faculty-alerts', title: 'Alerts', icon: 'AL', summary: 'Students flagged by attendance and grade risk.', actions: ['Message student', 'Mark addressed'], stats: [{ label: 'Open', value: '7', tone: 'danger' }] },
  { key: 'faculty-workload', title: 'Workload', icon: 'WL', summary: 'Credits, sections and student load summary.', actions: ['Semester', 'Annual'], stats: [{ label: 'Credits', value: '12' }, { label: 'Sections', value: '4' }] },
  { key: 'faculty-messages', title: 'Messages', icon: 'MS', summary: 'Class messaging with cached history.', actions: ['Bulk message', 'Sync'], stats: [{ label: 'Unread', value: '5' }] },
  { key: 'faculty-profile', title: 'Profile', icon: 'PF', summary: 'Appointment schedule and performance profile.', actions: ['Edit profile', 'Logout'], stats: [{ label: 'Appointments', value: '2' }] }
];
