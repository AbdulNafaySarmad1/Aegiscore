"use client";

import { useState } from "react";
import { AlertTriangle, CheckCircle2, LoaderCircle, MapPin, XCircle } from "lucide-react";
import { apiPost } from "@/lib/api-client";

type CheckInResult = {
  status: "ON_CAMPUS" | "OFF_CAMPUS" | "UNVERIFIABLE";
  recordedAt: string;
  message: string;
};

export function CheckInButton({ studentId, courseId }: { studentId: string; courseId: string }) {
  const [busy, setBusy] = useState(false);
  const [result, setResult] = useState<CheckInResult | null>(null);
  const [error, setError] = useState<string | null>(null);

  async function markAttendance() {
    setBusy(true);
    setError(null);
    try {
      const coordinates = await locate();
      const response = await apiPost<CheckInResult>("/api/attendance/checkin", {
        studentId,
        courseId,
        latitude: coordinates?.latitude ?? null,
        longitude: coordinates?.longitude ?? null,
      });
      setResult(response);
    } catch (reason) {
      setError(reason instanceof Error ? reason.message : "Attendance could not be recorded. Try again.");
    } finally {
      setBusy(false);
    }
  }

  const Icon = result?.status === "ON_CAMPUS"
    ? CheckCircle2
    : result?.status === "OFF_CAMPUS"
      ? XCircle
      : AlertTriangle;
  const color = result?.status === "ON_CAMPUS"
    ? "var(--success)"
    : result?.status === "OFF_CAMPUS"
      ? "var(--danger)"
      : "var(--warning)";

  return (
    <div style={{ display: "grid", gap: 10 }}>
      <button className="btn btn-primary btn-lg" type="button" onClick={() => void markAttendance()} disabled={busy || result !== null}>
        {busy ? <><LoaderCircle className="spin" size={18} />Requesting location…</> : <><MapPin size={18} />{result ? "Attendance marked" : "Mark Attendance"}</>}
      </button>
      {result && (
        <div className="badge" role="status" style={{ justifyContent: "flex-start", whiteSpace: "normal", padding: "10px 12px", color, border: `1px solid ${color}` }}>
          <Icon size={18} aria-hidden="true" />
          <span><strong>{result.status.replaceAll("_", " ")}</strong><br />{result.message}</span>
        </div>
      )}
      {error && <p role="alert" style={{ margin: 0, color: "var(--danger)", fontSize: 13 }}>{error}</p>}
    </div>
  );
}

function locate(): Promise<GeolocationCoordinates | null> {
  if (!("geolocation" in navigator)) return Promise.resolve(null);
  return new Promise(resolve => navigator.geolocation.getCurrentPosition(
    position => resolve(position.coords),
    () => resolve(null),
    { enableHighAccuracy: true, timeout: 10_000, maximumAge: 30_000 },
  ));
}
