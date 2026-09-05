"use client";

import { useCallback, useEffect, useMemo, useState } from "react";
import { AppShell } from "@/components/layout/AppShell";
import { apiGet, apiPost, apiPut } from "@/lib/api-client";
import { MapPin, Pencil, Plus, RotateCcw, Save } from "lucide-react";

type Vertex = { lat: number; lon: number };
type CampusPolygon = { id: string; name: string; vertices: Vertex[]; createdAt: string; isActive: boolean };

const span = 0.02;

export default function CampusPolygonPage() {
  const [polygons, setPolygons] = useState<CampusPolygon[]>([]);
  const [editingId, setEditingId] = useState<string | null>(null);
  const [name, setName] = useState("");
  const [vertices, setVertices] = useState<Vertex[]>([]);
  const [centerLat, setCenterLat] = useState(0);
  const [centerLon, setCenterLon] = useState(0);
  const [manualLat, setManualLat] = useState("");
  const [manualLon, setManualLon] = useState("");
  const [active, setActive] = useState(true);
  const [message, setMessage] = useState("");
  const [saving, setSaving] = useState(false);

  const load = useCallback(() =>
    apiGet<CampusPolygon[]>("/api/campuses/polygon").then(setPolygons).catch(() => setMessage("Campus boundaries could not be loaded.")), []);

  useEffect(() => { Promise.resolve().then(load); }, [load]);

  const mapUrl = useMemo(() =>
    `https://www.google.com/maps?q=${centerLat},${centerLon}&z=16&output=embed`, [centerLat, centerLon]);

  function addFromMap(event: React.MouseEvent<HTMLButtonElement>) {
    const bounds = event.currentTarget.getBoundingClientRect();
    const x = (event.clientX - bounds.left) / bounds.width;
    const y = (event.clientY - bounds.top) / bounds.height;
    setVertices(current => [...current, {
      lat: Number((centerLat - (y - 0.5) * span).toFixed(6)),
      lon: Number((centerLon + (x - 0.5) * span).toFixed(6)),
    }]);
  }

  function addManual() {
    const lat = Number(manualLat);
    const lon = Number(manualLon);
    if (!Number.isFinite(lat) || lat < -90 || lat > 90 || !Number.isFinite(lon) || lon < -180 || lon > 180) {
      setMessage("Enter a valid latitude and longitude.");
      return;
    }
    setVertices(current => [...current, { lat, lon }]);
    setManualLat("");
    setManualLon("");
    setMessage("");
  }

  function edit(polygon: CampusPolygon) {
    setEditingId(polygon.id);
    setName(polygon.name);
    setVertices(polygon.vertices);
    setActive(polygon.isActive);
    if (polygon.vertices.length > 0) {
      setCenterLat(polygon.vertices.reduce((sum, vertex) => sum + vertex.lat, 0) / polygon.vertices.length);
      setCenterLon(polygon.vertices.reduce((sum, vertex) => sum + vertex.lon, 0) / polygon.vertices.length);
    }
  }

  function clear(clearMessage = true) {
    setEditingId(null);
    setName("");
    setVertices([]);
    setActive(true);
    if (clearMessage) setMessage("");
  }

  async function save() {
    if (!name.trim() || vertices.length < 3) {
      setMessage("Name the campus and add at least three boundary points.");
      return;
    }
    setSaving(true);
    setMessage("");
    try {
      const body = { name: name.trim(), vertices, isActive: active };
      if (editingId) await apiPut(`/api/campuses/polygon/${editingId}`, body);
      else await apiPost("/api/campuses/polygon", body);
      const confirmation = editingId ? "Campus boundary updated." : "Campus boundary saved.";
      clear(false);
      await load();
      setMessage(confirmation);
    } catch (reason) {
      setMessage(reason instanceof Error ? reason.message : "Campus boundary could not be saved.");
    } finally {
      setSaving(false);
    }
  }

  const points = vertices.map(vertex => {
    const x = 50 + ((vertex.lon - centerLon) / span) * 100;
    const y = 50 - ((vertex.lat - centerLat) / span) * 100;
    return `${x},${y}`;
  }).join(" ");

  return (
    <AppShell>
      <div className="content-page">
        <header className="page-header"><div><div className="page-header-breadcrumb"><span>Admin</span><span>/</span><span>Campus boundaries</span></div><h1>Campus Geofences</h1><p>Draw the permitted attendance boundary. Coordinates are visible only to authorized administrators.</p></div><MapPin aria-hidden="true" /></header>
        <div className="form-grid" style={{ alignItems: "start" }}>
          <section className="card">
            <div className="card-body" style={{ display: "grid", gap: 14 }}>
              <label><span>Boundary name</span><input value={name} maxLength={160} onChange={event => setName(event.target.value)} placeholder="Main Campus" /></label>
              <div className="form-grid">
                <label><span>Map centre latitude</span><input type="number" step="0.000001" value={centerLat} onChange={event => setCenterLat(Number(event.target.value))} /></label>
                <label><span>Map centre longitude</span><input type="number" step="0.000001" value={centerLon} onChange={event => setCenterLon(Number(event.target.value))} /></label>
              </div>
              <div style={{ position: "relative", height: 380, borderRadius: 12, overflow: "hidden", border: "1px solid var(--border)" }}>
                <iframe title="Google Maps campus boundary editor" src={mapUrl} style={{ width: "100%", height: "100%", border: 0 }} />
                <button type="button" aria-label="Add a campus boundary point at this map location" onClick={addFromMap} style={{ position: "absolute", inset: 0, width: "100%", height: "100%", padding: 0, border: 0, background: "transparent", cursor: "crosshair" }}>
                  <svg aria-hidden="true" viewBox="0 0 100 100" preserveAspectRatio="none" style={{ width: "100%", height: "100%" }}>
                    {vertices.length >= 2 && <polygon points={points} fill="rgba(31,120,83,.25)" stroke="var(--success)" strokeWidth="0.6" />}
                    {points.split(" ").filter(Boolean).map((point, index) => {
                      const [cx, cy] = point.split(",");
                      return <circle key={index} cx={cx} cy={cy} r="1.2" fill="var(--danger)" />;
                    })}
                  </svg>
                </button>
              </div>
              <p style={{ margin: 0, color: "var(--muted)", fontSize: 12 }}>Tap the map to add boundary points in order. {vertices.length} point{vertices.length === 1 ? "" : "s"} added.</p>
              <div className="form-grid">
                <input aria-label="Vertex latitude" type="number" step="0.000001" value={manualLat} onChange={event => setManualLat(event.target.value)} placeholder="Latitude" />
                <input aria-label="Vertex longitude" type="number" step="0.000001" value={manualLon} onChange={event => setManualLon(event.target.value)} placeholder="Longitude" />
              </div>
              <button type="button" className="btn btn-secondary" onClick={addManual}><Plus size={16} />Add exact point</button>
              <ol>{vertices.map((vertex, index) => <li key={index}>{vertex.lat.toFixed(6)}, {vertex.lon.toFixed(6)} <button type="button" className="btn btn-ghost btn-sm" onClick={() => setVertices(current => current.filter((_, itemIndex) => itemIndex !== index))}>Remove</button></li>)}</ol>
              <label style={{ display: "flex", alignItems: "center", gap: 8 }}><input type="checkbox" checked={active} onChange={event => setActive(event.target.checked)} />Active boundary</label>
              <div style={{ display: "flex", gap: 10 }}>
                <button type="button" className="btn btn-primary" disabled={saving || vertices.length < 3 || !name.trim()} onClick={() => void save()}><Save size={16} />{saving ? "Saving boundary…" : editingId ? "Update boundary" : "Save boundary"}</button>
                <button type="button" className="btn btn-secondary" onClick={() => clear()}><RotateCcw size={16} />Reset</button>
              </div>
              {message && <p role="status">{message}</p>}
            </div>
          </section>
          <section className="card">
            <div className="card-header"><h2>Existing boundaries</h2></div>
            <div className="card-body" style={{ display: "grid", gap: 10 }}>
              {polygons.length === 0 ? <p>No campus boundaries yet. Draw the first boundary on the map.</p> : polygons.map(polygon => (
                <article key={polygon.id} style={{ padding: 12, border: "1px solid var(--border)", borderRadius: 10 }}>
                  <strong>{polygon.name}</strong><p>{polygon.vertices.length} vertices · {polygon.isActive ? "Active" : "Inactive"}</p>
                  <button type="button" className="btn btn-secondary btn-sm" onClick={() => edit(polygon)}><Pencil size={14} />Edit boundary</button>
                </article>
              ))}
            </div>
          </section>
        </div>
      </div>
    </AppShell>
  );
}
