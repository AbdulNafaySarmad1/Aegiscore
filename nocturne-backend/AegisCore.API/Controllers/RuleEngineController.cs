using System.Text.Json;
using AegisCore.Application.RuleEngine;
using AegisCore.Application.Common.Interfaces;
using AegisCore.Domain;
using AegisCore.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace AegisCore.API.Controllers;

[ApiController]
[Route("api/rules")]
public class RuleEngineController : ControllerBase
{
    private readonly IRuleEngineService _ruleService;
    private readonly IRuleDefinitionRepository _ruleRepo;
    private readonly IDecisionTableRepository _tableRepo;
    private readonly ILogger<RuleEngineController> _logger;

    public RuleEngineController(
        IRuleEngineService ruleService, IRuleDefinitionRepository ruleRepo,
        IDecisionTableRepository tableRepo, ILogger<RuleEngineController> logger)
    {
        _ruleService = ruleService; _ruleRepo = ruleRepo; _tableRepo = tableRepo; _logger = logger;
    }

    [HttpPost]
    public async Task<IActionResult> CreateRule([FromBody] CreateRuleRequest request, CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<RuleDomain>(request.Domain, true, out var domain))
            return BadRequest(new { error = "invalid_domain" });

        var rule = new RuleDefinition
        {
            Id = Guid.NewGuid(), Name = request.Name, Description = request.Description,
            Domain = domain, Priority = request.Priority, Status = RuleStatus.Draft
        };

        var created = await _ruleService.CreateRuleAsync(rule, request.Conditions, request.Actions, cancellationToken);
        return Ok(new RuleDto { Id = created.Id, Name = created.Name, Domain = created.Domain.ToString(), Priority = created.Priority, CurrentVersion = created.CurrentVersion });
    }

    [HttpPut("{ruleId:guid}")]
    public async Task<IActionResult> UpdateRule(Guid ruleId, [FromBody] CreateRuleRequest request, CancellationToken cancellationToken)
    {
        var updated = await _ruleService.UpdateRuleAsync(ruleId, request.Conditions, request.Actions, Guid.Empty, "Admin", cancellationToken);
        return Ok(new RuleDto { Id = updated.Id, Name = updated.Name, CurrentVersion = updated.CurrentVersion });
    }

    [HttpGet("domain/{domain}")]
    public async Task<IActionResult> ListRules(string domain, CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<RuleDomain>(domain, true, out var d))
            return BadRequest(new { error = "invalid_domain" });

        var rules = await _ruleRepo.GetByDomainAsync(d, cancellationToken);
        var response = rules.Select(r =>
        {
            var conditions = JsonSerializer.Deserialize<List<RuleCondition>>(r.ConditionsJson) ?? new();
            var actions = JsonSerializer.Deserialize<List<RuleAction>>(r.ActionsJson) ?? new();
            return new RuleDto { Id = r.Id, Name = r.Name, Description = r.Description, Domain = r.Domain.ToString(), Status = r.Status.ToString(), Priority = r.Priority, CurrentVersion = r.CurrentVersion, Conditions = conditions, Actions = actions, EffectiveFrom = r.EffectiveFrom, EffectiveTo = r.EffectiveTo };
        });

        return Ok(new { domain, total = response.Count(), rules = response });
    }

    [HttpPost("evaluate/{domain}")]
    public async Task<IActionResult> EvaluateRules(string domain, [FromBody] EvaluateFactsRequest request, CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<RuleDomain>(domain, true, out var d))
            return BadRequest(new { error = "invalid_domain" });

        var result = await _ruleService.EvaluateRulesAsync(d, request.Facts, cancellationToken);
        return Ok(result);
    }

    [HttpPost("simulate/{ruleId:guid}")]
    public async Task<IActionResult> SimulateRule(Guid ruleId, [FromBody] EvaluateFactsRequest request, CancellationToken cancellationToken)
    {
        var result = await _ruleService.SimulateRuleAsync(ruleId, request.Facts, cancellationToken);
        return Ok(result);
    }

    [HttpPost("decision-tables")]
    public async Task<IActionResult> CreateDecisionTable([FromBody] DecisionTableDto request, CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<RuleDomain>(request.Domain, true, out var domain))
            return BadRequest(new { error = "invalid_domain" });

        var table = new DecisionTable
        {
            Id = Guid.NewGuid(), Name = request.Name, Domain = domain,
            InputColumnsJson = JsonSerializer.Serialize(request.InputColumns),
            OutputColumnsJson = JsonSerializer.Serialize(request.OutputColumns),
            RowsJson = JsonSerializer.Serialize(request.Rows)
        };

        await _tableRepo.AddAsync(table, cancellationToken);
        return Ok(new { table.Id, table.Name, table.Domain });
    }

    [HttpPost("decision-tables/{tableId:guid}/evaluate")]
    public async Task<IActionResult> EvaluateDecisionTable(Guid tableId, [FromBody] EvaluateFactsRequest request, CancellationToken cancellationToken)
    {
        var result = await _ruleService.EvaluateDecisionTableAsync(tableId, request.Facts, cancellationToken);
        return Ok(result);
    }
}
