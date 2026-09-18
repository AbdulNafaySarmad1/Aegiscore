"use client";

import { useMemo, useState, type FormEvent, type MouseEvent } from "react";
import { apiGet, apiPost, apiPut } from "@/lib/api-client";
import { Panel, Notice, Mark } from "@/components/register";
import { ModulePage, useAction, useModuleData } from "@/components/register/module-page";

type Vertex = { lat: number; lon: number };
type CampusPolygon = { id: string; name: string; vertices: Vertex[]; createdAt: string; isActive: boolean };

/** How much ground the plotter shows, in degrees. About two kilometres across. */
const SPAN = 0.02;

/**
 * A map tile source, if this installation has one. Left unset — the default — the boundary is drawn
 * on a plain plotted grid and nothing about the campus leaves the building. The editor used to embed
 * Google Maps, which needed the internet an on-premise installation may not have and sent the
 * campus coordinates to a third party on a screen that told the reader they were seen by
 * administrators only. Set NEXT_PUBLIC_MAP_TILE_URL to an {z}/{x}/{y} template to show a basemap.
 */
const TILE_URL = process.env.NEXT_PUBLIC_MAP_TILE_URL ?? "";

/** Web-Mercator tile numbers for a latitude and longitude at a zoom level. */
function tile(lat: number, lon: number, zoom: number) {
  const n = 2 ** zoom;
  const x = Math.floor(((lon + 180) / 360) * n);
  const rad = (lat * Math.PI) / 180;
  const y = Math.floor(((1 - Math.log(Math.tan(rad) + 1 / Math.cos(rad)) / Math.PI) / 2) * n);
  return { x, y, n };
}

/** Campus boundaries: where a student has to be for attendance inside a geofence to count. */
export default function CampusPolygonPage() {
  const { state, reload } = useModuleData<CampusPolygon[]>(signal => apiGet<CampusPolygon[]>("/api/campuses/polygon", signal));
  const action = useAction();

  const [editingId, setEditingId] = useState<string | null>(null);
  const [name, setName] = useState("");
  const [vertices, setVertices] = useState<Vertex[]>([]);
  const [centre, setCentre] = useState<Vertex>({ lat: 0, lon: 0 });
  const [manual, setManual] = useState({ lat: "", lon: "" });
  const [active, setActive] = useState(true);
  const [problem, setProblem] = useState<string | null>(null);

  const tiles = useMemo(() => {
    if (!TILE_URL || (centre.lat === 0 && centre.lon === 0)) return [];
    const zoom = 15;
    const { x, y } = tile(centre.lat, centre.lon, zoom);
    return [-1, 0, 1].flatMap(dy => [-1, 0, 1].map(dx => ({
      key: `${dx},${dy}`,
      url: TILE_URL.replace("{z}", String(zoom)).replace("{x}", String(x + dx)).replace("{y}", String(y + dy)),
      left: `${(dx + 1) * 33.34}%`, top: `${(dy + 1) * 33.34}%`,
    })));
  }, [centre]);

  function plot(event: MouseEvent<HTMLButtonElement>) {
    const bounds = event.currentTarget.getBoundingClientRect();
    const x = (event.clientX - bounds.left) / bounds.width;
    const y = (event.clientY - bounds.top) / bounds.height;
    setVertices(current => [...current, {
      lat: Number((centre.lat - (y - 0.5) * SPAN).toFixed(6)),
      lon: Number((centre.lon + (x - 0.5) * SPAN).toFixed(6)),
    }]);
    setProblem(null);
  }

  function addExact(e: FormEvent) {
    e.preventDefault();
    const lat = Number(manual.lat);
    const lon = Number(manual.lon);
    if (!Number.isFinite(lat) || lat < -90 || lat > 90 || !Number.isFinite(lon) || lon < -180 || lon > 180) {
      setProblem("A latitude is between −90 and 90, and a longitude between −180 and 180.");
      return;
    }
    setVertices(current => [...current, { lat, lon }]);
    if (vertices.length === 0 && centre.lat === 0 && centre.lon === 0) setCentre({ lat, lon });
    setManual({ lat: "", lon: "" });
    setProblem(null);
  }

  function edit(polygon: CampusPolygon) {
    setEditingId(polygon.id);
    setName(polygon.name);
    setVertices(polygon.vertices);
    setActive(polygon.isActive);
    setProblem(null);
    if (polygon.vertices.length > 0) {
      setCentre({
        lat: polygon.vertices.reduce((sum, v) => sum + v.lat, 0) / polygon.vertices.length,
        lon: polygon.vertices.reduce((sum, v) => sum + v.lon, 0) / polygon.vertices.length,
      });
    }
  }

  function clear() {
    setEditingId(null);
    setName("");
    setVertices([]);
    setActive(true);
    setProblem(null);
  }

  async function save(e: FormEvent) {
    e.preventDefault();
    await action.run("save", async () => {
      const body = { name: name.trim(), vertices, isActive: active };
      if (editingId) await apiPut(`/api/campuses/polygon/${editingId}`, body);
      else await apiPost("/api/campuses/polygon", body);
      const saved = name.trim();
      clear();
      return editingId
        ? `${saved} is updated. Attendance inside it counts from now on.`
        : `${saved} is saved with ${vertices.length} boundary points.`;
    }, reload);
  }

  const points = vertices.map(v => `${50 + ((v.lon - centre.lon) / SPAN) * 100},${50 - ((v.lat - centre.lat) / SPAN) * 100}`).join(" ");

  return (
    <ModulePage
      title="Campus boundaries"
      crumbs={[{ label: "Operations" }, { label: "Campus boundaries" }]}
      meta={<span>The ground a student must be standing on for attendance marked inside a geofence to count.</span>}
      ident={(rows: CampusPolygon[]) => <><strong>{rows.filter(p => p.isActive).length}</strong><br />in force</>}
      state={state} reload={reload} notice={action.outcome}
    >
      {polygons => (
        <div className="record">
          <div className="record-body">
            <Panel title={editingId ? "Edit a boundary" : "Draw a boundary"}>
              <form className="panel-form" onSubmit={save}>
                <label><span>Name</span><input value={name} maxLength={160} onChange={e => setName(e.target.value)} placeholder="Main campus" required /></label>
                <div className="filter-row">
                  <label><span>Centre latitude</span><input type="number" step="0.000001" value={centre.lat} onChange={e => setCentre({ ...centre, lat: Number(e.target.value) })} /></label>
                  <label><span>Centre longitude</span><input type="number" step="0.000001" value={centre.lon} onChange={e => setCentre({ ...centre, lon: Number(e.target.value) })} /></label>
                </div>

                <div className="plotter">
                  {tiles.map(t => (
                    // eslint-disable-next-line @next/next/no-img-element -- a map tile from the configured tile server, not a portal asset
                    <img key={t.key} src={t.url} alt="" aria-hidden="true" style={{ left: t.left, top: t.top }} />
                  ))}
                  <button type="button" className="plotter-surface" onClick={plot} aria-label="Add a boundary point where you click">
                    <svg viewBox="0 0 100 100" preserveAspectRatio="none" aria-hidden="true">
                      <defs>
                        <pattern id="plot-grid" width="10" height="10" patternUnits="userSpaceOnUse">
                          <path d="M 10 0 L 0 0 0 10" fill="none" stroke="var(--rule)" strokeWidth="0.3" />
                        </pattern>
                      </defs>
                      {tiles.length === 0 && <rect width="100" height="100" fill="url(#plot-grid)" />}
                      {vertices.length >= 3 && <polygon points={points} fill="var(--laurel)" fillOpacity="0.16" stroke="var(--laurel)" strokeWidth="0.5" />}
                      {vertices.length === 2 && <polyline points={points} fill="none" stroke="var(--laurel)" strokeWidth="0.5" />}
                      {points.split(" ").filter(Boolean).map((point, index) => {
                        const [cx, cy] = point.split(",");
                        return <circle key={index} cx={cx} cy={cy} r="1" fill="var(--oxblood)" />;
                      })}
                    </svg>
                  </button>
                </div>
                <p className="field-hint">
                  Click inside the square to add a point, in the order you would walk the boundary.
                  The square covers about two kilometres across, centred where you set it.
                  {vertices.length > 0 && ` ${vertices.length} point${vertices.length === 1 ? "" : "s"} so far.`}
                </p>

                {vertices.length > 0 && (
                  <ol className="vertex-list">
                    {vertices.map((v, index) => (
                      <li key={index}>
                        <span className="mono">{v.lat.toFixed(6)}, {v.lon.toFixed(6)}</span>
                        <button type="button" className="btn btn-ghost btn-sm" onClick={() => setVertices(c => c.filter((_, i) => i !== index))}>Remove</button>
                      </li>
                    ))}
                  </ol>
                )}

                <label className="check-row"><input type="checkbox" checked={active} onChange={e => setActive(e.target.checked)} /><span>This boundary is in force</span></label>
                {problem && <p className="field-hint" role="alert">{problem}</p>}
                <div className="btn-row">
                  <button type="submit" className={`btn btn-primary${action.busy === "save" ? " btn-loading" : ""}`} disabled={action.busy !== null || vertices.length < 3 || !name.trim()}>
                    {editingId ? "Update the boundary" : "Save the boundary"}
                  </button>
                  <button type="button" className="btn btn-ghost" onClick={clear}>{editingId ? "Stop editing" : "Start again"}</button>
                </div>
                {vertices.length > 0 && vertices.length < 3 && <p className="field-hint">A boundary needs at least three points to enclose any ground.</p>}
              </form>
            </Panel>

            <Panel title="Add a point by its coordinates">
              <form className="panel-form" onSubmit={addExact}>
                <div className="filter-row">
                  <label><span>Latitude</span><input type="number" step="0.000001" value={manual.lat} onChange={e => setManual({ ...manual, lat: e.target.value })} placeholder="24.860966" /></label>
                  <label><span>Longitude</span><input type="number" step="0.000001" value={manual.lon} onChange={e => setManual({ ...manual, lon: e.target.value })} placeholder="67.001137" /></label>
                </div>
                <button type="submit" className="btn btn-secondary">Add this point</button>
              </form>
            </Panel>
          </div>

          <aside className="record-margin">
            <Panel title="Boundaries on file">
              {polygons.length === 0 ? (
                <p>None yet. A geofence has no effect until a boundary is drawn and put in force.</p>
              ) : (
                <ul className="notice-list">
                  {polygons.map(p => (
                    <li key={p.id} className="goal-row">
                      <strong>{p.name}</strong> <Mark tone={p.isActive ? "laurel" : "ink"}>{p.isActive ? "In force" : "Not in force"}</Mark>
                      <p>{p.vertices.length} points</p>
                      <button type="button" className="btn btn-secondary btn-sm" onClick={() => edit(p)}>Edit</button>
                    </li>
                  ))}
                </ul>
              )}
            </Panel>

            <Notice tone="ink" title="Nothing here leaves the building.">
              {TILE_URL
                ? "The basemap is drawn from the tile server this installation is configured to use."
                : "The boundary is plotted on a plain grid, and no map service is contacted. If you want a basemap behind it, set NEXT_PUBLIC_MAP_TILE_URL to a tile server you trust."}
            </Notice>
          </aside>
        </div>
      )}
    </ModulePage>
  );
}
