type AttendanceEvent = {
  clientEventId: string;
  courseId: string;
  studentId: string;
  markedBy: string;
  status: "present" | "absent" | "leave";
  wasOfflineAt: string;
};

const dbName = "nocturne-offline";
const storeName = "attendance";
const bufferMs = 30 * 60 * 1000;

export async function queueAttendance(event: Omit<AttendanceEvent, "wasOfflineAt">) {
  const db = await openDb();
  await put(db, { ...event, wasOfflineAt: new Date().toISOString() });
}

export async function syncAttendance() {
  const db = await openDb();
  const events = await all(db);
  const now = Date.now();
  const fresh = events.filter(event => now - Date.parse(event.wasOfflineAt) <= bufferMs);
  const deduped = [...new Map(fresh.map(event => [event.clientEventId, event])).values()];
  if (deduped.length === 0) return 0;

  const response = await fetch("/api/curriculum/attendance/offline-sync", {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(deduped)
  });
  if (!response.ok) throw new Error("Attendance sync failed.");

  await clear(db);
  return deduped.length;
}

export function registerOfflineAttendanceWorker() {
  if ("serviceWorker" in navigator) {
    void navigator.serviceWorker.register("/sw.js");
  }
}

function openDb(): Promise<IDBDatabase> {
  return new Promise((resolve, reject) => {
    const request = indexedDB.open(dbName, 1);
    request.onupgradeneeded = () => request.result.createObjectStore(storeName, { keyPath: "clientEventId" });
    request.onsuccess = () => resolve(request.result);
    request.onerror = () => reject(request.error);
  });
}

function put(db: IDBDatabase, event: AttendanceEvent) {
  return tx(db, "readwrite", store => store.put(event));
}

function clear(db: IDBDatabase) {
  return tx(db, "readwrite", store => store.clear());
}

function all(db: IDBDatabase): Promise<AttendanceEvent[]> {
  return tx(db, "readonly", store => store.getAll());
}

function tx<T>(db: IDBDatabase, mode: IDBTransactionMode, action: (store: IDBObjectStore) => IDBRequest<T>): Promise<T> {
  return new Promise((resolve, reject) => {
    const transaction = db.transaction(storeName, mode);
    const request = action(transaction.objectStore(storeName));
    request.onsuccess = () => resolve(request.result);
    request.onerror = () => reject(request.error);
  });
}
