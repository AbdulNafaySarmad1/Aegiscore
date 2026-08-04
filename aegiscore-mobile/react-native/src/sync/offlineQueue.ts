import AsyncStorage from '@react-native-async-storage/async-storage';
import { apiClient } from '../api/client';

const ATTENDANCE_QUEUE = 'aegiscore.attendance.queue';
const BUFFER_MS = 30 * 60 * 1000;

export type AttendanceDraft = {
  id: string;
  courseId: string;
  studentId: string;
  status: 'present' | 'absent' | 'leave';
  wasOfflineAt: number;
};

export async function queueAttendance(draft: Omit<AttendanceDraft, 'wasOfflineAt'>) {
  const queued = await getAttendanceQueue();
  const next = { ...draft, wasOfflineAt: Date.now() };
  const deduped = queued.filter((item) => item.id !== next.id);
  await AsyncStorage.setItem(ATTENDANCE_QUEUE, JSON.stringify([...deduped, next]));
}

export async function getAttendanceQueue(): Promise<AttendanceDraft[]> {
  const raw = await AsyncStorage.getItem(ATTENDANCE_QUEUE);
  if (!raw) return [];
  const parsed: unknown = JSON.parse(raw);
  if (!Array.isArray(parsed)) throw new Error('Attendance queue is invalid.');
  return parsed as AttendanceDraft[];
}

export async function flushAttendanceQueue() {
  const queued = await getAttendanceQueue();
  const fresh = queued.filter((item) => Date.now() - item.wasOfflineAt <= BUFFER_MS);
  if (fresh.length === 0) {
    await AsyncStorage.removeItem(ATTENDANCE_QUEUE);
    return 0;
  }

  await apiClient.post('/mobile/attendance/batch', { events: fresh });
  await AsyncStorage.removeItem(ATTENDANCE_QUEUE);
  return fresh.length;
}
