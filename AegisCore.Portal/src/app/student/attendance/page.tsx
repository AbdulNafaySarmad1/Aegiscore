"use client";

import { useEffect, useState } from "react";
import { AppShell } from "@/components/layout/AppShell";
import { apiGet, apiPost } from "@/lib/api-client";
import { ClipboardCheck, CheckCircle, MapPin, XCircle } from "lucide-react";

type CourseAttendance = {
  courseId: string; courseTitle: string; totalSessions: number;
  presentSessions: number; absentSessions: number; percentage: number;
  recent: { date: string; status: string }[];
};

type Coordinates = { latitude: number; longitude: number };

function getCurrentPosition(): Promise<Coordinates> {
  return new Promise((resolve, reject) => {
    if (!("geolocation" in navigator)) {
      reject(new Error("Geolocation is not supported by this browser."));
      return;
    }

    navigator.geolocation.getCurrentPosition(
      position => resolve({
        latitude: position.coords.latitude,
        longitude: position.coords.longitude,
      }),
      error => {
        const message = error.code === error.PERMISSION_DENIED
          ? "Location permission is required to mark attendance."
          : error.code === error.TIMEOUT
            ? "Could not determine your location in time. Please try again."
            : "Could not determine your location. Please try again.";
        reject(new Error(message));
      },
      { enableHighAccuracy: true, timeout: 12_000, maximumAge: 30_000 }
    );
  });
}

export default function StudentAttendance() {
  const [courses, setCourses] = useState<CourseAttendance[]>([]);
  const [loading, setLoading] = useState(true);
  const [checkInMsg, setCheckInMsg] = useState("");
  const [checkingCourseId, setCheckingCourseId] = useState<string | null>(null);

  const refresh = () => apiGet<{ courses: CourseAttendance[] }>("/api/student-attendance/history")
    .then(d => setCourses(d.courses ?? []));

  useEffect(() => {
    refresh()
      .catch(() => setCheckInMsg("Failed to load attendance history."))
      .finally(() => setLoading(false));
  }, []);

  const handleCheckIn = async (courseId: string) => {
    setCheckingCourseId(courseId);
    setCheckInMsg("");
    try {
      const coords = await getCurrentPosition();
      const res = await apiPost<{ success: boolean; message: string; distanceMeters?: number }>(
        "/api/student-attendance/check-in",
        { courseId, ...coords }
      );
      setCheckInMsg(res.distanceMeters == null ? res.message : `${res.message} (${Math.round(res.distanceMeters)} m from campus reference point)`);
      await refresh();
    } catch (err) {
      setCheckInMsg(err instanceof Error ? err.message : "Check-in failed.");
    } finally {
      setCheckingCourseId(null);
    }
  };

  const overallPct = courses.length > 0
    ? Math.round(courses.reduce((s, c) => s + c.presentSessions, 0) / Math.max(1, courses.reduce((s, c) => s + c.totalSessions, 0)) * 100)
    : 0;

  return (
    <AppShell>
      <div className="content-page">
        <div className="page-header">
          <div>
            <div className="page-header-breadcrumb"><span>Academic</span><span>/</span><span>Attendance</span></div>
            <h1>My Attendance</h1>
            <p>Overall: {overallPct}% · {courses.length} courses · on-campus location required for check-in</p>
          </div>
        </div>

        {loading ? <div className="skeleton" style={{ height: 120 }} /> : courses.map(c => (
          <div key={c.courseId} className="card" style={{ marginBottom: 12 }}>
            <div className="card-header" style={{ display: "flex", justifyContent: "space-between", alignItems: "center" }}>
              <div>
                <h3 style={{ fontSize: 14, margin: 0 }}>{c.courseTitle}</h3>
                <span style={{ fontSize: 12, color: "var(--muted)" }}>{c.presentSessions}/{c.totalSessions} sessions ({c.percentage}%)</span>
              </div>
              <button
                className="btn btn-primary btn-sm"
                onClick={() => void handleCheckIn(c.courseId)}
                disabled={checkingCourseId !== null}
                title="Uses your current location to verify that you are inside the configured campus attendance zone"
              >
                <MapPin size={14} /> {checkingCourseId === c.courseId ? "Verifying location..." : "Mark Attendance"}
              </button>
            </div>
            <div className="card-body" style={{ padding: 0 }}>
              {c.recent.map((r, i) => (
                <div key={i} style={{ display: "flex", justifyContent: "space-between", padding: "8px 16px", borderBottom: i < c.recent.length - 1 ? "1px solid var(--border)" : "none", fontSize: 13 }}>
                  <span>{r.date}</span>
                  <span className={r.status === "Present" ? "badge badge-success" : "badge badge-danger"}>
                    {r.status === "Present" ? <CheckCircle size={12} /> : <XCircle size={12} />} {r.status}
                  </span>
                </div>
              ))}
              {c.recent.length === 0 && <p style={{ padding: 16, color: "var(--muted)", fontSize: 13 }}>No attendance records yet.</p>}
            </div>
          </div>
        ))}
        {!loading && courses.length === 0 && <p style={{ color: "var(--muted)" }}>No attendance records found.</p>}
        {checkInMsg && <div className="error-state" style={{ marginTop: 12 }}><ClipboardCheck size={20} /><p>{checkInMsg}</p></div>}
      </div>
    </AppShell>
  );
}
