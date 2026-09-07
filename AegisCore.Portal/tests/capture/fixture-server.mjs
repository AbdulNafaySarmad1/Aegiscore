// Local, in-memory visual fixtures. Never used by the production application.
import { createServer } from 'node:http';
const students = ['Ali Hassan', 'Sara Ahmed', 'Omar Khan', 'Ayesha Malik', 'Hamza Ali', 'Zainab Shah'].map((name, i) => ({ id: `student-${i+1}`, universityId: `BSCS-2026-00${i+1}`, firstName: name.split(' ')[0], lastName: name.split(' ')[1], name, status: 'Enrolled', enrollmentId: `enrol-${i+1}`, studentId: `student-${i+1}`, locationStatus: 'ON_CAMPUS', attendanceId: `attendance-${i+1}`, isManualOverride: false }));
const course = { id: 'course-1', code: 'CS101', title: 'Programming Fundamentals' };
const dashboard = { kpis: { revenue: 28500000, expenses: 19200000, cashPosition: 14300000 }, trend: ['Jan','Feb','Mar','Apr','May','Jun'].map((month,i) => ({month,revenue:3500000+i*480000,expenses:2400000+i*250000,postings:180+i*12})), feeFunnel:{demanded:32000000,collected:28500000,overdue:1000000,open:28,paid:640}, expenseCategories:[{category:'Academic salaries',amount:12500000},{category:'Campus operations',amount:4200000},{category:'Student services',amount:2500000}], budgetBurn:[{costCenterId:'cs',name:'Computer Science',allocated:18000000,spent:12400000,consumedPercent:69}], currencyExposure:[{currency:'PKR',foreignAmount:14300000,functionalAmount:14300000}],cashFlow:[{category:'Operating',amount:9300000}],unclassifiedCashFlow:0 };
const fixtures = {
 '/health/ready': {status:'Healthy'},
 '/api/config/portal-preferences': {locale:'en-PK',currency:'PKR',paymentGateway:'demo',accreditorName:'Demo accreditor',identityDocumentName:'Student identity'},
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
const server=createServer((req,res)=>{
 const pathname=new URL(req.url,'http://127.0.0.1').pathname;
 let value=fixtures[pathname];
 const step=pathname.match(/^\/api\/wizard\/step\/(\d+)\/save$/);
 if(step) value={nextStep:Math.min(Number(step[1])+1,7)};
 if(pathname.includes('/personality')) value={mode:'professional',personalityMode:'professional'};
 res.writeHead(value===undefined?501:200,{'Content-Type':'application/json','Cache-Control':'no-store'});
 res.end(JSON.stringify(value??{message:'This module has no synthetic demo fixture.',demo:true}));
});
server.listen(4319,'127.0.0.1',()=>console.log('Synthetic API listening on 127.0.0.1:4319'));
for(const signal of ['SIGINT','SIGTERM']) process.on(signal,()=>server.close(()=>process.exit(0)));
