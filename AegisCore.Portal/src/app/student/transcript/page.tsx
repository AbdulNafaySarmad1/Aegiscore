"use client";
import { useEffect, useState } from "react";
import { AppShell } from "@/components/layout/AppShell";
import { apiGet, apiPost } from "@/lib/api-client";
import { CheckCircle, Download } from "lucide-react";

type Overview = { student:{id:string;name:string;rollNumber:string}; academics:{program:string;cgpa:number;earnedCredits:number;requiredCredits:number;courses:{code:string;title:string;credits:number;grade:string|null;status:string}[]} };
export default function TranscriptPage(){
  const [data,setData]=useState<Overview|null>(null); const [requested,setRequested]=useState(false); const [error,setError]=useState("");
  useEffect(()=>{apiGet<Overview>("/api/student/overview").then(setData).catch(e=>setError(e.message));},[]);
  async function request(){if(!data)return; try{await apiPost(`/api/examination/transcripts/${data.student.id}/request`,{requestedBy:data.student.id,reason:"Official student copy"});setRequested(true);}catch(e){setError(e instanceof Error?e.message:"Request failed");}}
  return <AppShell><div className="content-page"><div className="page-header"><div><div className="page-header-breadcrumb"><span>Student</span><span>/</span><span>Transcript</span></div><h1>Academic transcript</h1><p>{data?`${data.student.name} · ${data.student.rollNumber} · ${data.academics.program}`:"Loading verified records…"}</p></div><button className="btn btn-primary" disabled={!data||requested} onClick={request}>{requested?<><CheckCircle size={16}/>Requested</>:<><Download size={16}/>Request official copy</>}</button></div>
    {error&&<div role="alert" className="error-state">{error}</div>}{data&&<><div className="kpi-grid" style={{marginBottom:18}}><article className="kpi-card"><span className="kpi-label">CGPA</span><strong className="kpi-number">{data.academics.cgpa.toFixed(2)}</strong></article><article className="kpi-card"><span className="kpi-label">Earned credits</span><strong className="kpi-number">{data.academics.earnedCredits}</strong></article><article className="kpi-card"><span className="kpi-label">Program requirement</span><strong className="kpi-number">{data.academics.requiredCredits}</strong></article><article className="kpi-card"><span className="kpi-label">Completed courses</span><strong className="kpi-number">{data.academics.courses.filter(c=>c.grade).length}</strong></article></div><div className="card" style={{overflow:"hidden"}}><table className="data-table"><thead><tr><th>Code</th><th>Course</th><th>Credits</th><th>Grade</th><th>Status</th></tr></thead><tbody>{data.academics.courses.map(c=><tr key={c.code}><td>{c.code}</td><td>{c.title}</td><td>{c.credits}</td><td><span className={c.grade?"badge badge-success":"badge badge-neutral"}>{c.grade??"—"}</span></td><td>{c.status}</td></tr>)}</tbody></table></div></>}
  </div></AppShell>;
}
