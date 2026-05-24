using HRMS.API.Services;
using HRMS.Application.DTOs;
using HRMS.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace HRMS.API.Controllers;

[ApiController]
[Route("api/v1/payroll/runs")]
public class PayrollRunController : ControllerBase
{
    private readonly PayrollCalculationService _service;
    private readonly CurrentUserAccessor _currentUserAccessor;
    private readonly PayrollApprovalWorkflowService _workflowService;
    private readonly CallbackSecurityService _callbackSecurityService;

    public PayrollRunController(
        PayrollCalculationService service,
        CurrentUserAccessor currentUserAccessor,
        PayrollApprovalWorkflowService workflowService,
        CallbackSecurityService callbackSecurityService)
    {
        _service = service;
        _currentUserAccessor = currentUserAccessor;
        _workflowService = workflowService;
        _callbackSecurityService = callbackSecurityService;
    }

    [HttpPost("trial")]
    [RequirePermission("button.salary.adjust")]
    public async Task<IActionResult> Trial([FromBody] ExecutePayrollRunDto dto)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .ToList();
            return BadRequest(new { code = 400, message = "验证失败", errors });
        }

        var result = await _service.TrialAsync(dto);
        return Ok(new { code = 200, message = "试算成功", data = result });
    }

    [HttpPost("calculate")]
    [RequirePermission("button.salary.adjust")]
    public async Task<IActionResult> Calculate([FromBody] ExecutePayrollRunDto dto)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .ToList();
            return BadRequest(new { code = 400, message = "验证失败", errors });
        }

        var result = await _service.CalculateAsync(dto);
        return Ok(new { code = 200, message = "正式核算成功", data = result });
    }

    [HttpPost("{runId:guid}/approve")]
    [RequirePermission("button.salary.adjust")]
    public async Task<IActionResult> Approve(Guid runId, [FromBody] PayrollRunActionDto? dto)
    {
        var userId = await _currentUserAccessor.GetUserIdAsync();
        var result = await _service.ApproveAsync(runId, userId?.ToString() ?? "System", dto?.Remark);
        return result == null
            ? NotFound(new { code = 404, message = "薪资批次不存在" })
            : Ok(new { code = 200, message = "薪资批次审核成功", data = result });
    }

    [HttpPost("{runId:guid}/pay")]
    [RequirePermission("button.salary.adjust")]
    public async Task<IActionResult> Pay(Guid runId, [FromBody] PayrollRunActionDto? dto)
    {
        var userId = await _currentUserAccessor.GetUserIdAsync();
        var result = await _service.PayAsync(runId, userId?.ToString() ?? "System", dto?.Remark);
        return result == null
            ? NotFound(new { code = 404, message = "薪资批次不存在" })
            : Ok(new { code = 200, message = "薪资批次发放成功", data = result });
    }

    [HttpPost("{runId:guid}/submit-approval")]
    [RequirePermission("button.salary.approve")]
    public async Task<IActionResult> SubmitApproval(Guid runId, [FromBody] SubmitPayrollApprovalDto? dto, CancellationToken cancellationToken)
    {
        var result = await _workflowService.SubmitAsync(runId, dto?.Remark, cancellationToken);
        return result == null
            ? NotFound(new { code = 404, message = "薪资批次不存在" })
            : Ok(new { code = 200, message = "薪资批次已提交审批流程", data = result });
    }

    [HttpPost("{runId:guid}/workflow-callback/approved")]
    public async Task<IActionResult> WorkflowApproved(Guid runId, [FromBody] PayrollWorkflowCallbackDto dto)
    {
        var validation = await _callbackSecurityService.ValidateAsync(Request, dto);
        if (!validation.IsValid)
        {
            return StatusCode(validation.StatusCode, new { code = validation.StatusCode, message = validation.ErrorMessage });
        }

        if (validation.IsDuplicate)
        {
            var duplicatedRun = await _service.GetRunAsync(runId);
            return duplicatedRun == null
                ? NotFound(new { code = 404, message = "薪资批次不存在" })
                : Ok(new { code = 200, message = "薪资批次审批回调已幂等处理", data = duplicatedRun });
        }

        var result = await _service.ApproveByWorkflowAsync(runId, dto.ProcessInstanceId, dto.OperatorId, dto.OperatorName, dto.Comment);
        if (result != null && validation.EventId != null)
        {
            await _callbackSecurityService.MarkProcessedAsync(validation.EventId);
        }

        return result == null
            ? NotFound(new { code = 404, message = "薪资批次不存在" })
            : Ok(new { code = 200, message = "薪资批次审批回调处理成功", data = result });
    }

    [HttpPost("{runId:guid}/workflow-callback/rejected")]
    public async Task<IActionResult> WorkflowRejected(Guid runId, [FromBody] PayrollWorkflowCallbackDto dto)
    {
        var validation = await _callbackSecurityService.ValidateAsync(Request, dto);
        if (!validation.IsValid)
        {
            return StatusCode(validation.StatusCode, new { code = validation.StatusCode, message = validation.ErrorMessage });
        }

        if (validation.IsDuplicate)
        {
            var duplicatedRun = await _service.GetRunAsync(runId);
            return duplicatedRun == null
                ? NotFound(new { code = 404, message = "薪资批次不存在" })
                : Ok(new { code = 200, message = "薪资批次驳回回调已幂等处理", data = duplicatedRun });
        }

        var result = await _service.RejectApprovalAsync(runId, dto.ProcessInstanceId, dto.OperatorId, dto.OperatorName, dto.Comment);
        if (result != null && validation.EventId != null)
        {
            await _callbackSecurityService.MarkProcessedAsync(validation.EventId);
        }

        return result == null
            ? NotFound(new { code = 404, message = "薪资批次不存在" })
            : Ok(new { code = 200, message = "薪资批次驳回回调处理成功", data = result });
    }

    [HttpPost("{runId:guid}/rollback")]
    [RequirePermission("button.salary.adjust")]
    public async Task<IActionResult> Rollback(Guid runId, [FromQuery] string? remark)
    {
        var result = await _service.RollbackAsync(runId, remark);
        return result == null
            ? NotFound(new { code = 404, message = "薪资批次不存在" })
            : Ok(new { code = 200, message = "薪资批次回滚成功", data = result });
    }

    [HttpGet]
    [RequirePermission("page.salary.list")]
    public async Task<IActionResult> GetRuns([FromQuery] string yearMonth)
    {
        var result = await _service.GetRunsAsync(yearMonth);
        return Ok(new { code = 200, message = "success", data = result });
    }

    [HttpGet("{runId:guid}")]
    [RequirePermission("page.salary.list")]
    public async Task<IActionResult> GetRun(Guid runId)
    {
        var result = await _service.GetRunAsync(runId);
        return result == null
            ? NotFound(new { code = 404, message = "薪资批次不存在" })
            : Ok(new { code = 200, message = "success", data = result });
    }

    [HttpGet("{runId:guid}/employees/{employeeId:guid}")]
    [RequirePermission("page.salary.list")]
    public async Task<IActionResult> GetRunEmployee(Guid runId, Guid employeeId)
    {
        var result = await _service.GetRunEmployeeAsync(runId, employeeId);
        return result == null
            ? NotFound(new { code = 404, message = "员工工资单不存在" })
            : Ok(new { code = 200, message = "success", data = result });
    }
}
