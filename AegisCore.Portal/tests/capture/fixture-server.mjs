// Local, in-memory visual fixtures. Never used by the production application.
import { createServer } from 'node:http';
const students = ['Ali Hassan', 'Sara Ahmed', 'Omar Khan', 'Ayesha Malik', 'Hamza Ali', 'Zainab Shah'].map((name, i) => ({ id: `student-${i+1}`, universityId: `BSCS-2026-00${i+1}`, firstName: name.split(' ')[0], lastName: name.split(' ')[1], name, status: 'Enrolled', enrollmentId: `enrol-${i+1}`, studentId: `student-${i+1}`, locationStatus: 'ON_CAMPUS', attendanceId: `attendance-${i+1}`, isManualOverride: false }));
const course = { id: 'course-1', code: 'CS101', title: 'Programming Fundamentals' };
const dashboard = { kpis: { revenue: 28500000, expenses: 19200000, cashPosition: 14300000 }, trend: ['Jan','Feb','Mar','Apr','May','Jun'].map((month,i) => ({month,revenue:3500000+i*480000,expenses:2400000+i*250000,postings:180+i*12})), feeFunnel:{demanded:32000000,collected:28500000,overdue:1000000,open:28,paid:640}, expenseCategories:[{category:'Academic salaries',amount:12500000},{category:'Campus operations',amount:4200000},{category:'Student services',amount:2500000}], budgetBurn:[{costCenterId:'cs',name:'Computer Science',allocated:18000000,spent:12400000,consumedPercent:69}], currencyExposure:[{currency:'PKR',foreignAmount:14300000,functionalAmount:14300000}],cashFlow:[{category:'Operating',amount:9300000}],unclassifiedCashFlow:0 };
const fixtures = {
 '/health/ready': {status:'Healthy'},
 '/api/config/portal-preferences': {locale:'en-PK',currency:'PKR',paymentGateway:'demo',accreditorName:'Demo accreditor',identityDocumentName:'Student identity',universityName:'Fictional National University',universityShortName:'FNU',sealUrl:'/aegiscore-logo.png'},
 '/api/branding/client-logo': {logoUrl:'/aegiscore-logo.png'},
 '/api/session': {userId:'demo-user',tenantId:'demo-tenant',name:'Ali Hassan',roles:['Student'],studentId:'student-1',departmentId:'cs'},
 '/api/students': {items:students,total:students.length},
 '/api/admissions/enquiries': students.map(s=>({id:s.id,name:s.name,email:`${s.firstName.toLowerCase()}@example.invalid`,status:s.id==='student-1'?'Admitted · BSCS · Entry test 87/100':'Documents verified'})),
 '/api/courses/my': [course],
 '/api/attendance/roster/course-1': {students},
 '/api/examination/schemes/course-1': {id:'scheme-1',components:[{id:'midterm',name:'Midterm',totalMarks:30,weightPercentage:30},{id:'final',name:'Final',totalMarks:70,weightPercentage:70}]},
 '/api/examination/marks': {saved:true},
 '/api/attendance/checkin': {status:'ON_CAMPUS',recordedAt:'2026-09-07T09:00:00Z',message:'Demo check-in recorded for Programming Fundamentals.'},
 '/api/finance/v2/dashboard/executive': dashboard,
 '/api/finance/v2/security-posture': {chainIntegrity:'green',lastSealTimestamp:'2026-09-07T08:00:00Z',merkleRoot:'demo-proof-not-cryptographic-evidence',signatureAlgorithm:'ML-DSA-65',keyId:'demo-key',eventCount:8240,unsignedLedgerEntries:0,keys:[],deadKeys:[],failedVerificationAlerts:[]},
 '/api/finance/v2/reports/cash-position': {report:'Cash position',asOf:'2026-09-07',currency:'PKR',cashAndBank:14300000,receivables:3500000,source:'Synthetic demo records'},
 '/api/classes/offerings': [{id:'offering-1',name:'BSCS · Fall 2026',courseCode:'CS101',courseName:course.title,targetSections:2,generatedSections:2,totalCapacity:80}],
 '/api/classes/sections': ['A','B'].map((section,i)=>({id:`section-${i}`,offeringId:'offering-1',offeringName:course.title,sectionName:`CS101-${section}`,capacity:40,enrolled:36-i*3,lmsProvider:'GoogleClassroom',lmsStatus:'Synced'})),
 '/api/classes/sync-logs': [{timestamp:'2026-09-07T08:30:00Z',membersAdded:6,membersRemoved:0,conflicts:0,status:'Success'}],
 '/api/wizard/session': {sessionId:'visual-demo',currentStep:1,licenseMode:'demo',data:{step1:{licenseMode:'demo',universityName:'Institute of Business Management',shortName:'IoBM',country:'Pakistan',city:'Karachi'},step2:{provider:'postgres',host:'demo-db.invalid',database:'university_demo'},step3:{provider:'local'},step4:{},step5:{provider:'none',connectionTested:true},step6:{skipImport:true},step7:{users:[{name:'Demo Administrator',email:'admin@example.invalid',password:'not-a-real-credential',role:'Admin'}],confirmed:true}}},
 '/api/wizard/mode': {},
 '/api/wizard/options/regions': {regions:['Sindh','Punjab','Islamabad']},
 '/api/wizard/step4/roles': {},
 '/api/wizard/step4/permissions': {},
 '/api/wizard/step4/matrix': {},
 '/api/wizard/complete': {success:true},
 '/api/data-import/connections': [],
};

// ── Lifecycle fixtures (admissions → graduation → alumni) for visual checks ──
const state = { role: 'Student', clearances: new Set(), approved: false };
const applicantNames = ['Aisha Khan','Bilal Ahmed','Chandni Raza','Danish Iqbal','Eman Siddiqui','Farhan Malik','Gul Bano','Hassan Tariq','Iqra Shah','Junaid Qureshi','Kiran Baig','Laiba Noor'];
const applicantStatus = ['Admitted','UnderReview','TestScheduled','Submitted','Rejected','InterviewScheduled','Admitted','Draft','UnderReview','Withdrawn','Submitted','TestScheduled'];
const applicants = applicantNames.map((name,i)=>({ id:`app-${i+1}`, applicantId:`APP-2026-${String(i+17).padStart(4,'0')}`, fullName: i===7?null:name, email:`${name.split(' ')[0].toLowerCase()}.${name.split(' ')[1].toLowerCase()}@example.invalid`, programId:'bscs', intakeId:'intake-fall-2026', status: applicantStatus[i], submittedAt: i===7?null:`2026-0${(i%6)+3}-1${i%9}T09:30:00Z`, createdAt:'2026-03-01T08:00:00Z' }));
const documents = [
  { id:'doc-1', documentType:'Matric', mimeType:'application/pdf', fileSize:412000, validationStatus:'Approved', rejectionReason:null, validationConfidence:0.97, validationMethod:'OCR match', uploadedAt:'2026-04-02T10:00:00Z' },
  { id:'doc-2', documentType:'Intermediate', mimeType:'application/pdf', fileSize:388000, validationStatus:'Approved', rejectionReason:null, validationConfidence:0.94, validationMethod:'OCR match', uploadedAt:'2026-04-02T10:02:00Z' },
  { id:'doc-3', documentType:'National ID', mimeType:'image/jpeg', fileSize:1130000, validationStatus:'Approved', rejectionReason:null, validationConfidence:0.99, validationMethod:'NADRA format', uploadedAt:'2026-04-02T10:05:00Z' },
  { id:'doc-4', documentType:'Photo', mimeType:'image/jpeg', fileSize:210000, validationStatus:'Approved', rejectionReason:null, validationConfidence:null, validationMethod:null, uploadedAt:'2026-04-02T10:06:00Z' },
  { id:'doc-5', documentType:'Domicile Certificate', mimeType:'application/pdf', fileSize:265000, validationStatus:'Pending', rejectionReason:null, validationConfidence:0.61, validationMethod:'OCR match', uploadedAt:'2026-04-03T11:00:00Z' },
];
const courses = [['CS101','Programming Fundamentals',3,'A'],['CS102','Discrete Structures',3,'B+'],['MA101','Calculus I',3,'A-'],['EN101','Functional English',3,'B'],['CS201','Data Structures',4,'A'],['CS202','Object-Oriented Programming',4,'B+'],['CS301','Operating Systems',3,'A-'],['CS302','Database Systems',3,'A'],['CS401','Software Engineering',3,'B'],['CS499','Final Year Project',6,'B+'],['CS402','Computer Networks',3,null]].map(([code,title,credits,grade])=>({code,title,credits,grade,status:grade?'Completed':'In progress'}));
const overview = { student:{ id:'student-1', name:'Aisha Khan', universityId:'FNU-2026-001', rollNumber:'BSCS-26-001', institutionalEmail:'aisha.khan@fnu.edu.pk', status:'Enrolled' }, admission:{ applicationNumber:'APP-2026-0017', email:'aisha.khan@example.invalid', status:'Admitted', approvedDocuments:4, testScore:84, testPassed:true }, academics:{ program:'BS Computer Science', cgpa:3.3, earnedCredits:130, requiredCredits:130, completedCourses:10, totalCourses:11, courses }, attendance:[{course:'CS401',total:30,present:27,percentage:90},{course:'CS402',total:28,present:22,percentage:78.6},{course:'CS499',total:12,present:12,percentage:100}], finance:{ demanded:1240000, paid:1240000, outstanding:0, status:'Clear' }, timetable:[{day:'Monday',startTime:'09:00:00',endTime:'10:30:00',room:'LT-2',course:'CS402 Computer Networks'},{day:'Tuesday',startTime:'11:00:00',endTime:'12:30:00',room:'Lab 4',course:'CS499 Final Year Project'},{day:'Thursday',startTime:'09:00:00',endTime:'10:30:00',room:'LT-2',course:'CS402 Computer Networks'}] };
Object.assign(fixtures, {
  '/api/wizard/demo-status': { isDemo: true, remainingDays: 21 },
  '/api/admissions/intakes': [{ id:'intake-fall-2026', year:2026, semester:1, name:'Fall 2026', status:1 }, { id:'intake-spring-2027', year:2027, semester:2, name:'Spring 2027', status:0 }],
  '/api/admissions/applicants': applicants,
  '/api/admissions/applicants/bulk-status': { updated: true },
  '/api/student/overview': overview,
  '/api/examination/transcripts/student-1/request': { requested: true },
});
function graduation() {
  const blockers = [];
  for (const d of ['Library','Finance','Department']) if (!state.clearances.has(d)) blockers.push(`${d} clearance is required.`);
  return { studentName: overview.student.name, program: overview.academics.program, cgpa: overview.academics.cgpa, earnedCredits: overview.academics.earnedCredits, requiredCredits: overview.academics.requiredCredits, approved: state.approved, blockers };
}
function dynamicRoute(method, pathname) {
  if (pathname === '/__fixture/reset') { state.role='Student'; state.clearances.clear(); state.approved=false; return [200, {ok:true}]; }
  const roleMatch = pathname.match(/^\/__fixture\/role\/(\w+)$/); if (roleMatch) { state.role = roleMatch[1]; return [200, {role:state.role}]; }
  if (pathname === '/api/session') return [200, { userId:'demo-user', tenantId:'demo-tenant', name: state.role==='Student'?'Aisha Khan':state.role==='Admin'?'Dr. Sana Mirza':'Demo User', roles:[state.role], studentId: state.role==='Student'?'student-1':null, departmentId:'cs' }];
  if (pathname === '/api/auth/login') return [200, { accessToken:'demo-access', refreshToken:'demo-refresh', expiresIn: 900 }];
  if (pathname === '/api/auth/refresh') return [200, { accessToken:'demo-access', refreshToken:'demo-refresh', expiresIn: 900 }];
  if (pathname === '/api/auth/logout') return [200, {}];
  const detail = pathname.match(/^\/api\/admissions\/applicants\/(app-\d+)$/);
  if (detail) { const a = applicants.find(x=>x.id===detail[1]); if (!a) return [404,{error:'No such applicant'}]; return [200, { applicant:a, documents: a.id==='app-1'?documents:documents.slice(0,2), test: a.status==='Draft'?null:{ score: a.id==='app-1'?84:67, passed: a.status!=='Rejected', status:'Submitted', completedAt:'2026-05-12T10:15:00Z' } }]; }
  if (/^\/api\/admissions\/applicants\/app-\d+\/documents\/doc-\d+\/review$/.test(pathname)) return [200, {ok:true}];
  if (/^\/api\/admissions\/applicants\/app-\d+\/status$/.test(pathname)) return [200, {ok:true}];
  if (pathname === '/api/graduation/student-1') return [200, graduation()];
  const clearance = pathname.match(/^\/api\/graduation\/student-1\/clearances\/(\w+)$/);
  if (clearance) { if (state.role!=='Admin') return [403,{error:'forbidden'}]; state.clearances.add(clearance[1]); return [200, graduation()]; }
  if (pathname === '/api/graduation/student-1/approve') { if (state.role!=='Admin') return [403,{error:'forbidden'}]; if (graduation().blockers.length) return [409,{error:'Outstanding requirements must be cleared before graduation can be approved.'}]; state.approved=true; return [200, graduation()]; }
  // KPI summaries. state.readiness switches between 'none', 'stub' (score without criteria) and 'evidenced'.
  if (pathname === '/__fixture/readiness/none') { state.readiness='none'; return [200,{}]; }
  if (pathname === '/__fixture/readiness/stub') { state.readiness='stub'; return [200,{}]; }
  if (pathname === '/__fixture/readiness/evidenced') { state.readiness='evidenced'; return [200,{}]; }
  if (pathname === '/api/students/summary') { if (state.role==='Student') return [403,{error:'forbidden'}]; return [200, { enrolled: 4312, total: 5890, byStatus: { active:4312, inactive:118, graduated:1240, suspended:9, withdrawn:211 }, asOf:'2026-09-16T08:00:00Z' }]; }
  if (pathname === '/api/faculty/summary') { if (state.role==='Student') return [403,{error:'forbidden'}]; return [200, { total: 231, active: 224, teaching: 206, byDesignation: { professor:31, associateProfessor:44, assistantProfessor:78, lecturer:47, teachingAssistant:6, dean:7, registrar:1, administrator:10 }, asOf:'2026-09-16T08:00:00Z' }]; }
  if (pathname === '/api/readiness/check-freshness') return [200, { message:'Evidence freshness checked.' }];
  if (pathname === '/api/readiness/current') {
    if (state.role==='Student') return [403,{error:'forbidden'}];
    const mode = state.readiness ?? 'evidenced';
    if (mode==='none') return [200, { overall:null, frameworks:[], asOf:'2026-09-16T08:00:00Z' }];
    const criteria = mode==='stub' ? [] : [
      { id:'HEC-1', name:'Vision, mission and strategic planning', category:'Governance', score:9, maxScore:10, status:'Fully met' },
      { id:'HEC-2', name:'Faculty qualifications and workload', category:'Faculty', score:6, maxScore:10, status:'Partially met' },
      { id:'HEC-3', name:'Curriculum review cycle', category:'Academic', score:4, maxScore:10, status:'Not met' },
      { id:'HEC-4', name:'Student support services', category:'Students', score:8, maxScore:10, status:'Substantially met' },
    ];
    const fw = { scoreId:'score-1', frameworkId:'fw-hec', frameworkName:'HEC General', overallScore: mode==='stub'?78.5:67.5, status: mode==='stub'?'Good':'NeedsImprovement', generatedAt:'2026-09-01T09:30:00Z', nextAuditDate:'2027-02-15T00:00:00Z', daysUntilAudit:152, complianceVelocity:2.3, categoryScores:{Governance:90,Faculty:60,Academic:40,Students:80}, criteria,
      gaps: mode==='stub' ? [] : [
        { id:'gap-1', criterionId:'HEC-3', gapDescription:'No documented curriculum review in the last three years.', evidenceNeeded:'Minutes of the Board of Studies review and revised course files.', effort:'MediumTerm', impactOnScore:6, priorityRank:1 },
        { id:'gap-2', criterionId:'HEC-2', gapDescription:'Faculty CVs need updating.', evidenceNeeded:null, effort:'QuickWin', impactOnScore:2.5, priorityRank:2 },
      ],
      alerts: mode==='stub' ? [] : [ { id:'al-1', title:'Curriculum review overdue', message:'The three-year review window for two programmes has lapsed.', category:'CRITICAL', isAcknowledged:false, triggeredAt:'2026-09-01T09:31:00Z' } ] };
    return [200, { overall:{ score: fw.overallScore, frameworkCount:1, generatedAt: fw.generatedAt }, frameworks:[fw], asOf:'2026-09-16T08:00:00Z' }];
  }
  if (pathname === '/api/student-lifecycle/alumni/me') return state.approved ? [200, { alumniId:'ALM-2030-00017', graduationYear:2030, userId:'student-1' }] : [404, {error:'not graduated'}];
  return null;
}
const server=createServer((req,res)=>{
 const pathname=new URL(req.url,'http://127.0.0.1').pathname;
 const dyn=dynamicRoute(req.method,pathname);
 if(dyn){res.writeHead(dyn[0],{'Content-Type':'application/json','Cache-Control':'no-store'});res.end(JSON.stringify(dyn[1]));return;}
 let value=fixtures[pathname];
 const step=pathname.match(/^\/api\/wizard\/step\/(\d+)\/save$/);
 if(step) value={nextStep:Math.min(Number(step[1])+1,7)};
 if(pathname.includes('/personality')) value={mode:'professional',personalityMode:'professional'};
 res.writeHead(value===undefined?501:200,{'Content-Type':'application/json','Cache-Control':'no-store'});
 res.end(JSON.stringify(value??{message:'This module has no synthetic demo fixture.',demo:true}));
});
server.listen(4319,'127.0.0.1',()=>console.log('Synthetic API listening on 127.0.0.1:4319'));
for(const signal of ['SIGINT','SIGTERM']) process.on(signal,()=>server.close(()=>process.exit(0)));
