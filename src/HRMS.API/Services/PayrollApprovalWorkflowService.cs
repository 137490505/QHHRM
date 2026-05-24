using HRMS.Application.DTOs;
using HRMS.Application.Services;

namespace HRMS.API.Services;

public class PayrollApprovalWorkflowService
{
    private readonly PayrollCalculationService _payrollCalculationService;
    private readonly IProcessCenterClient _processCenterClient;
    private readonly CurrentUserAccessor _currentUserAccessor;

    public PayrollApprovalWorkflowService(
        PayrollCalculationService payrollCalculationService,
        IProcessCenterClient processCenterClient,
        CurrentUserAccessor currentUserAccessor)
    {
        _payrollCalculationService = payrollCalculationService;
        _processCenterClient = processCenterClient;
        _currentUserAccessor = currentUserAccessor;
    }

    public async Task<PayrollApprovalSubmissionResultDto?> SubmitAsync(
        Guid runId,
        string? remark,
        CancellationToken cancellationToken = default)
    {
        var run = await _payrollCalculationService.GetRunAsync(runId);
        if (run == null)
        {
            return null;
        }

        if (!string.Equals(run.Status, "Calculated", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("只有已核算批次才能提交审批流程");
        }

        if (!string.IsNullOrWhiteSpace(run.ApprovalProcessInstanceId))
        {
            throw new InvalidOperationException("该薪资批次已提交审批流程");
        }

        var starterId = (await _currentUserAccessor.GetUserIdAsync())?.ToString() ?? "System";
        var starterName = await _currentUserAccessor.GetUserDisplayNameAsync() ?? starterId;
        var submittedAt = DateTime.UtcNow;
        var processResult = await _processCenterClient.StartPayrollApprovalAsync(
            new PayrollApprovalStartRequest
            {
                Run = run,
                StarterId = starterId,
                StarterName = starterName,
                SourcePayload = new PayrollApprovalSourcePayload
                {
                    RunId = run.Id,
                    RunNo = run.RunNo,
                    YearMonth = run.YearMonth,
                    RunType = run.RunType,
                    Status = run.Status,
                    PayrollCount = run.PayrollCount,
                    TotalGross = run.TotalGross,
                    TotalNetSalary = run.TotalNetSalary,
                    Remark = remark,
                    StarterId = starterId,
                    StarterName = starterName,
                    SubmittedAt = submittedAt
                }
            },
            cancellationToken);

        var updatedRun = await _payrollCalculationService.MarkApprovalSubmittedAsync(
            runId,
            processResult.ProcessCode,
            processResult.ProcessInstanceId,
            processResult.RequestId,
            starterId,
            remark);

        if (updatedRun == null)
        {
            return null;
        }

        return new PayrollApprovalSubmissionResultDto
        {
            Run = updatedRun,
            ProcessCode = processResult.ProcessCode,
            ProcessInstanceId = processResult.ProcessInstanceId,
            CurrentNodeId = processResult.CurrentNodeId,
            CurrentNodeName = processResult.CurrentNodeName,
            CurrentAssigneeId = processResult.CurrentAssigneeId,
            CurrentAssigneeName = processResult.CurrentAssigneeName
        };
    }
}
