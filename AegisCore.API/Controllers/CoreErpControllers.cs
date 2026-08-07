using AegisCore.Application.Common.Interfaces;
using AegisCore.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AegisCore.API.Controllers;

// GAP 03 — Research Management
[ApiController][Route("api/research")]
[Authorize]
public class ResearchController : ControllerBase
{
    private readonly IRepository<ResearchProject> _projectRepo;
    private readonly IRepository<ResearchGrant> _grantRepo;
    private readonly IRepository<ResearchPublication> _pubRepo;
    public ResearchController(IRepository<ResearchProject> p, IRepository<ResearchGrant> g, IRepository<ResearchPublication> pb) { _projectRepo = p; _grantRepo = g; _pubRepo = pb; }
    [HttpGet("projects")] public async Task<IActionResult> Projects(CancellationToken ct) { var items = await _projectRepo.GetAllAsync(ct); return Ok(new { total = items.Count, projects = items }); }
    [HttpPost("projects")] public async Task<IActionResult> CreateProject([FromBody] ResearchProject p, CancellationToken ct) { await _projectRepo.AddAsync(p, ct); return Ok(p); }
    [HttpGet("grants")] public async Task<IActionResult> Grants(CancellationToken ct) { var items = await _grantRepo.GetAllAsync(ct); return Ok(new { total = items.Count, grants = items }); }
    [HttpPost("grants")] public async Task<IActionResult> CreateGrant([FromBody] ResearchGrant g, CancellationToken ct) { await _grantRepo.AddAsync(g, ct); return Ok(g); }
    [HttpGet("publications")] public async Task<IActionResult> Publications(CancellationToken ct) { var items = await _pubRepo.GetAllAsync(ct); return Ok(new { total = items.Count, publications = items }); }
    [HttpPost("publications")] public async Task<IActionResult> CreatePublication([FromBody] ResearchPublication p, CancellationToken ct) { await _pubRepo.AddAsync(p, ct); return Ok(p); }
}

// GAP 04 — OBE
[ApiController][Route("api/obe")]
[Authorize]
public class OBEController : ControllerBase
{
    private readonly IRepository<ProgramOutcome> _poRepo;
    private readonly IRepository<CourseLearningOutcome> _cloRepo;
    private readonly IRepository<Rubric> _rubricRepo;
    public OBEController(IRepository<ProgramOutcome> po, IRepository<CourseLearningOutcome> clo, IRepository<Rubric> r) { _poRepo = po; _cloRepo = clo; _rubricRepo = r; }
    [HttpGet("program-outcomes")] public async Task<IActionResult> POs(CancellationToken ct) { var items = await _poRepo.GetAllAsync(ct); return Ok(new { total = items.Count, outcomes = items }); }
    [HttpPost("program-outcomes")] public async Task<IActionResult> CreatePO([FromBody] ProgramOutcome po, CancellationToken ct) { await _poRepo.AddAsync(po, ct); return Ok(po); }
    [HttpGet("clos")] public async Task<IActionResult> CLOs(CancellationToken ct) { var items = await _cloRepo.GetAllAsync(ct); return Ok(new { total = items.Count, outcomes = items }); }
    [HttpPost("clos")] public async Task<IActionResult> CreateCLO([FromBody] CourseLearningOutcome clo, CancellationToken ct) { await _cloRepo.AddAsync(clo, ct); return Ok(clo); }
}

// GAP 05 — Accreditation
[ApiController][Route("api/accreditation")]
[Authorize]
public class AccreditationController : ControllerBase
{
    private readonly IRepository<AccreditationBody> _bodyRepo;
    private readonly IRepository<AccreditationCycle> _cycleRepo;
    private readonly IRepository<SelfStudyReport> _ssrRepo;
    public AccreditationController(IRepository<AccreditationBody> b, IRepository<AccreditationCycle> c, IRepository<SelfStudyReport> s) { _bodyRepo = b; _cycleRepo = c; _ssrRepo = s; }
    [HttpGet("bodies")] public async Task<IActionResult> Bodies(CancellationToken ct) { var items = await _bodyRepo.GetAllAsync(ct); return Ok(new { total = items.Count, bodies = items }); }
    [HttpPost("bodies")] public async Task<IActionResult> CreateBody([FromBody] AccreditationBody b, CancellationToken ct) { await _bodyRepo.AddAsync(b, ct); return Ok(b); }
    [HttpGet("cycles")] public async Task<IActionResult> Cycles(CancellationToken ct) { var items = await _cycleRepo.GetAllAsync(ct); return Ok(new { total = items.Count, cycles = items }); }
    [HttpPost("cycles")] public async Task<IActionResult> CreateCycle([FromBody] AccreditationCycle c, CancellationToken ct) { await _cycleRepo.AddAsync(c, ct); return Ok(c); }
    [HttpGet("ssr")] public async Task<IActionResult> SSR(CancellationToken ct) { var items = await _ssrRepo.GetAllAsync(ct); return Ok(new { total = items.Count, reports = items }); }
}

// GAP 06 — Examination Office
[ApiController][Route("api/exam")]
[Authorize]
public class ExaminationController : ControllerBase
{
    private readonly IRepository<QuestionBank> _qbRepo;
    private readonly IRepository<ExamPaper> _paperRepo;
    public ExaminationController(IRepository<QuestionBank> qb, IRepository<ExamPaper> ep) { _qbRepo = qb; _paperRepo = ep; }
    [HttpGet("question-bank")] public async Task<IActionResult> QuestionBank(CancellationToken ct) { var items = await _qbRepo.GetAllAsync(ct); return Ok(new { total = items.Count, questions = items }); }
    [HttpPost("question-bank")] public async Task<IActionResult> AddQuestion([FromBody] QuestionBank q, CancellationToken ct) { await _qbRepo.AddAsync(q, ct); return Ok(q); }
    [HttpGet("papers")] public async Task<IActionResult> Papers(CancellationToken ct) { var items = await _paperRepo.GetAllAsync(ct); return Ok(new { total = items.Count, papers = items }); }
    [HttpPost("papers")] public async Task<IActionResult> CreatePaper([FromBody] ExamPaper p, CancellationToken ct) { await _paperRepo.AddAsync(p, ct); return Ok(p); }
}

// GAP 07 — Library ERP
[ApiController][Route("api/library")]
[Authorize]
public class LibraryController : ControllerBase
{
    private readonly IRepository<LibraryBook> _bookRepo;
    private readonly IRepository<LibraryMember> _memberRepo;
    private readonly IRepository<BookIssue> _issueRepo;
    public LibraryController(IRepository<LibraryBook> b, IRepository<LibraryMember> m, IRepository<BookIssue> i) { _bookRepo = b; _memberRepo = m; _issueRepo = i; }
    [HttpGet("books")] public async Task<IActionResult> Books(CancellationToken ct) { var items = await _bookRepo.GetAllAsync(ct); return Ok(new { total = items.Count, books = items }); }
    [HttpGet("members")] public async Task<IActionResult> Members(CancellationToken ct) { var items = await _memberRepo.GetAllAsync(ct); return Ok(new { total = items.Count, members = items }); }
    [HttpGet("issues")] public async Task<IActionResult> Issues(CancellationToken ct) { var items = await _issueRepo.GetAllAsync(ct); return Ok(new { total = items.Count, issues = items }); }
    [HttpPost("issues")] public async Task<IActionResult> IssueBook([FromBody] BookIssue issue, CancellationToken ct) { await _issueRepo.AddAsync(issue, ct); return Ok(issue); }
}

// GAP 09 — Campus Asset ERP
[ApiController][Route("api/assets")]
[Authorize]
public class AssetController : ControllerBase
{
    private readonly IRepository<Asset> _assetRepo;
    private readonly IRepository<AssetTransfer> _transferRepo;
    public AssetController(IRepository<Asset> a, IRepository<AssetTransfer> t) { _assetRepo = a; _transferRepo = t; }
    [HttpGet] public async Task<IActionResult> List(CancellationToken ct) { var items = await _assetRepo.GetAllAsync(ct); return Ok(new { total = items.Count, assets = items }); }
    [HttpPost] public async Task<IActionResult> Create([FromBody] Asset a, CancellationToken ct) { await _assetRepo.AddAsync(a, ct); return Ok(a); }
}

// GAP 12 — Transportation
[ApiController][Route("api/transport")]
[Authorize]
public class TransportController : ControllerBase
{
    private readonly IRepository<TransportRoute> _routeRepo;
    private readonly IRepository<TransportVehicle> _vehicleRepo;
    private readonly IRepository<TransportDriver> _driverRepo;
    public TransportController(IRepository<TransportRoute> r, IRepository<TransportVehicle> v, IRepository<TransportDriver> d) { _routeRepo = r; _vehicleRepo = v; _driverRepo = d; }
    [HttpGet("routes")] public async Task<IActionResult> Routes(CancellationToken ct) { var items = await _routeRepo.GetAllAsync(ct); return Ok(new { total = items.Count, routes = items }); }
    [HttpGet("vehicles")] public async Task<IActionResult> Vehicles(CancellationToken ct) { var items = await _vehicleRepo.GetAllAsync(ct); return Ok(new { total = items.Count, vehicles = items }); }
    [HttpGet("drivers")] public async Task<IActionResult> Drivers(CancellationToken ct) { var items = await _driverRepo.GetAllAsync(ct); return Ok(new { total = items.Count, drivers = items }); }
}

// GAP 13 — Hostel ERP
[ApiController][Route("api/hostel")]
[Authorize]
public class HostelController : ControllerBase
{
    private readonly IRepository<HostelBlock> _blockRepo;
    private readonly IRepository<HostelAllocation> _allocRepo;
    public HostelController(IRepository<HostelBlock> b, IRepository<HostelAllocation> a) { _blockRepo = b; _allocRepo = a; }
    [HttpGet("blocks")] public async Task<IActionResult> Blocks(CancellationToken ct) { var items = await _blockRepo.GetAllAsync(ct); return Ok(new { total = items.Count, blocks = items }); }
    [HttpGet("allocations")] public async Task<IActionResult> Allocations(CancellationToken ct) { var items = await _allocRepo.GetAllAsync(ct); return Ok(new { total = items.Count, allocations = items }); }
    [HttpPost("allocations")] public async Task<IActionResult> AllocateRoom([FromBody] HostelAllocation a, CancellationToken ct) { await _allocRepo.AddAsync(a, ct); return Ok(a); }
}

// GAP 14 — Student Success
[ApiController][Route("api/student-success")]
[Authorize]
public class StudentSuccessController : ControllerBase
{
    private readonly IRepository<StudentRiskScore> _riskRepo;
    private readonly IRepository<AdvisorAssignment> _advisorRepo;
    public StudentSuccessController(IRepository<StudentRiskScore> r, IRepository<AdvisorAssignment> a) { _riskRepo = r; _advisorRepo = a; }
    [HttpGet("risk-scores")] public async Task<IActionResult> RiskScores(CancellationToken ct) { var items = await _riskRepo.GetAllAsync(ct); return Ok(new { total = items.Count, scores = items }); }
    [HttpGet("advisors")] public async Task<IActionResult> Advisors(CancellationToken ct) { var items = await _advisorRepo.GetAllAsync(ct); return Ok(new { total = items.Count, assignments = items }); }
}

// GAP 15 — Career Center
[ApiController][Route("api/career")]
[Authorize]
public class CareerController : ControllerBase
{
    private readonly IRepository<Employer> _empRepo;
    private readonly IRepository<JobPosting> _jobRepo;
    public CareerController(IRepository<Employer> e, IRepository<JobPosting> j) { _empRepo = e; _jobRepo = j; }
    [HttpGet("employers")] public async Task<IActionResult> Employers(CancellationToken ct) { var items = await _empRepo.GetAllAsync(ct); return Ok(new { total = items.Count, employers = items }); }
    [HttpGet("jobs")] public async Task<IActionResult> Jobs(CancellationToken ct) { var items = await _jobRepo.GetAllAsync(ct); return Ok(new { total = items.Count, jobs = items }); }
}

// GAP 16 — Admissions CRM
[ApiController][Route("api/admissions-crm")]
[Authorize]
public class AdmissionsCrmController : ControllerBase
{
    private readonly IRepository<AdmissionLead> _leadRepo;
    private readonly IRepository<MarketingCampaign> _campaignRepo;
    public AdmissionsCrmController(IRepository<AdmissionLead> l, IRepository<MarketingCampaign> c) { _leadRepo = l; _campaignRepo = c; }
    [HttpGet("leads")] public async Task<IActionResult> Leads(CancellationToken ct) { var items = await _leadRepo.GetAllAsync(ct); return Ok(new { total = items.Count, leads = items }); }
    [HttpPost("leads")] public async Task<IActionResult> CreateLead([FromBody] AdmissionLead l, CancellationToken ct) { await _leadRepo.AddAsync(l, ct); return Ok(l); }
    [HttpGet("campaigns")] public async Task<IActionResult> Campaigns(CancellationToken ct) { var items = await _campaignRepo.GetAllAsync(ct); return Ok(new { total = items.Count, campaigns = items }); }
}

// GAP 17 — Business Intelligence
[ApiController][Route("api/bi")]
[Authorize]
public class BIController : ControllerBase
{
    private readonly IRepository<Dashboard> _dashboardRepo;
    private readonly IRepository<BIReport> _reportRepo;
    private readonly IRepository<ETLJob> _etlRepo;
    public BIController(IRepository<Dashboard> d, IRepository<BIReport> r, IRepository<ETLJob> e) { _dashboardRepo = d; _reportRepo = r; _etlRepo = e; }
    [HttpGet("dashboards")] public async Task<IActionResult> Dashboards(CancellationToken ct) { var items = await _dashboardRepo.GetAllAsync(ct); return Ok(new { total = items.Count, dashboards = items }); }
    [HttpGet("reports")] public async Task<IActionResult> Reports(CancellationToken ct) { var items = await _reportRepo.GetAllAsync(ct); return Ok(new { total = items.Count, reports = items }); }
    [HttpGet("etl")] public async Task<IActionResult> ETL(CancellationToken ct) { var items = await _etlRepo.GetAllAsync(ct); return Ok(new { total = items.Count, jobs = items }); }
    [HttpPost("dashboards")] public async Task<IActionResult> CreateDashboard([FromBody] Dashboard d, CancellationToken ct) { await _dashboardRepo.AddAsync(d, ct); return Ok(d); }
}
