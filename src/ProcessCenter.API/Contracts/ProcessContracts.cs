using System.ComponentModel.DataAnnotations;

namespace ProcessCenter.API.Contracts;

public class CreateProcessDefinitionRequest
{
    [Required]
    public string ProcessCode { get; set; } = string.Empty;

    [Required]
    public string ProcessName { get; set; } = string.Empty;

    [Required]
    public string BusinessType { get; set; } = string.Empty;

    public string? CallbackConfig { get; set; }

    [Required]
    [MinLength(1)]
    public List<ProcessDefinitionNodeRequest> Nodes { get; set; } = [];
}

public class ProcessDefinitionNodeRequest
{
    [Required]
    public string NodeId { get; set; } = string.Empty;

    [Required]
    public string NodeName { get; set; } = string.Empty;

    public string NodeType { get; set; } = "UserTask";

    public string AssigneeType { get; set; } = "User";

    [Required]
    public string AssigneeId { get; set; } = string.Empty;

    [Required]
    public string AssigneeName { get; set; } = string.Empty;

    public string? MultiPersonType { get; set; } // None, Any (或签), All (会签)
    public string? NextNodeId { get; set; }
    public List<ProcessNodeConditionRequest>? Conditions { get; set; }
}

public class ProcessNodeConditionRequest
{
    public string? ConditionExpression { get; set; } // e.g. "Amount > 1000"
    public string TargetNodeId { get; set; } = string.Empty;
}

public class CopyProcessDefinitionRequest
{
    [Required]
    public string ProcessCode { get; set; } = string.Empty;

    [Required]
    public string ProcessName { get; set; } = string.Empty;
}

public class RollbackProcessDefinitionRequest
{
    public string? ProcessName { get; set; }
}

public class StartProcessRequest
{
    [Required]
    public string ProcessCode { get; set; } = string.Empty;

    [Required]
    public string BusinessSystem { get; set; } = string.Empty;

    [Required]
    public string BusinessType { get; set; } = string.Empty;

    [Required]
    public string BusinessId { get; set; } = string.Empty;

    [Required]
    public string Title { get; set; } = string.Empty;

    [Required]
    public string StarterId { get; set; } = string.Empty;

    [Required]
    public string StarterName { get; set; } = string.Empty;

    public string? RequestId { get; set; }
    public string? ExtData { get; set; }
}

public class CompleteProcessTaskRequest
{
    [Required]
    public long ProcessInstanceId { get; set; }

    public string Action { get; set; } = "Complete";
    public string? NextNodeId { get; set; }
    public string? OperatorId { get; set; }
    public string? OperatorName { get; set; }
    public string? Comment { get; set; }
}

public class ProcessInstanceActionRequest
{
    public string? OperatorId { get; set; }
    public string? OperatorName { get; set; }
    public string? Comment { get; set; }
}

public class ProcessReturnRequest : ProcessInstanceActionRequest
{
    public string? TargetNodeId { get; set; }
}

public class ProcessTransferCurrentRequest : ProcessInstanceActionRequest
{
    [Required]
    public string TargetAssigneeId { get; set; } = string.Empty;

    [Required]
    public string TargetAssigneeName { get; set; } = string.Empty;
}

public class ProcessDefinitionDto
{
    public long Id { get; set; }
    public string ProcessCode { get; set; } = string.Empty;
    public string ProcessName { get; set; } = string.Empty;
    public string BusinessType { get; set; } = string.Empty;
    public int VersionNo { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? CallbackConfig { get; set; }
    public DateTime CreatedTime { get; set; }
    public DateTime UpdatedTime { get; set; }
    public List<ProcessDefinitionNodeDto> Nodes { get; set; } = [];
}

public class ProcessDefinitionNodeDto
{
    public string NodeId { get; set; } = string.Empty;
    public string NodeName { get; set; } = string.Empty;
    public string NodeType { get; set; } = string.Empty;
    public string AssigneeType { get; set; } = "User";
    public string AssigneeId { get; set; } = string.Empty;
    public string AssigneeName { get; set; } = string.Empty;
    public string? MultiPersonType { get; set; }
    public string? NextNodeId { get; set; }
    public int SortOrder { get; set; }
    public List<ProcessNodeConditionDto>? Conditions { get; set; }
}

public class ProcessNodeConditionDto
{
    public string? ConditionExpression { get; set; }
    public string TargetNodeId { get; set; } = string.Empty;
}

public class ProcessInstanceDto
{
    public long Id { get; set; }
    public string InstanceNo { get; set; } = string.Empty;
    public string ProcessCode { get; set; } = string.Empty;
    public int VersionNo { get; set; }
    public string BusinessSystem { get; set; } = string.Empty;
    public string BusinessType { get; set; } = string.Empty;
    public string BusinessId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? CallbackConfig { get; set; }
    public string? CurrentNodeId { get; set; }
    public string? CurrentNodeName { get; set; }
    public string? CurrentAssigneeId { get; set; }
    public string? CurrentAssigneeName { get; set; }
    public string StarterId { get; set; } = string.Empty;
    public string StarterName { get; set; } = string.Empty;
    public DateTime StartedTime { get; set; }
    public DateTime? FinishedTime { get; set; }
    public string? ExtData { get; set; }
    public ProcessTodoCommandDto? CurrentTodo { get; set; }
    public List<ProcessNodeHistoryDto> History { get; set; } = [];
}

public class ProcessTaskTransitionResultDto
{
    public ProcessInstanceDto Instance { get; set; } = new();
    public ProcessTodoCommandDto? NextTodo { get; set; }
}

public class ProcessInstanceOperateResultDto
{
    public ProcessInstanceDto Instance { get; set; } = new();
    public ProcessTodoCommandDto? CurrentTodo { get; set; }
}

public class ProcessTodoRebuildResultDto
{
    public ProcessInstanceDto Instance { get; set; } = new();
    public ProcessTodoCommandDto CurrentTodo { get; set; } = new();
    public ProcessTodoDispatchDto Task { get; set; } = new();
}

public class ProcessNodeHistoryDto
{
    public long Id { get; set; }
    public string NodeId { get; set; } = string.Empty;
    public string NodeName { get; set; } = string.Empty;
    public string NodeType { get; set; } = string.Empty;
    public string? HandlerId { get; set; }
    public string? HandlerName { get; set; }
    public string Action { get; set; } = string.Empty;
    public string ActionResult { get; set; } = string.Empty;
    public string? Comment { get; set; }
    public DateTime ArrivedTime { get; set; }
    public DateTime? HandledTime { get; set; }
    public decimal? DurationMinutes { get; set; }
}

public class ProcessTodoCommandDto
{
    public string TaskTypeCode { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string BusinessSystem { get; set; } = string.Empty;
    public string BusinessType { get; set; } = string.Empty;
    public string BusinessId { get; set; } = string.Empty;
    public string ProcessInstanceId { get; set; } = string.Empty;
    public string ProcessNodeId { get; set; } = string.Empty;
    public string AssigneeId { get; set; } = string.Empty;
    public string AssigneeName { get; set; } = string.Empty;
    public int Priority { get; set; } = 2;
    public string? SourcePayload { get; set; }
    public string? CreatedBy { get; set; }
}

public class ProcessTodoDispatchDto
{
    public long Id { get; set; }
    public string TaskNo { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string AssigneeId { get; set; } = string.Empty;
    public string AssigneeName { get; set; } = string.Empty;
}

public class ProcessInstanceSummaryDto
{
    public int TotalCount { get; set; }
    public int RunningCount { get; set; }
    public int CompletedCount { get; set; }
    public int RejectedCount { get; set; }
    public int TerminatedCount { get; set; }
    public int WithdrawnCount { get; set; }
}

public class BusinessCallbackResultDto
{
    public string CallbackUrl { get; set; } = string.Empty;
    public string Payload { get; set; } = string.Empty;
    public int StatusCode { get; set; }
    public string? Response { get; set; }
    public bool IsSuccess { get; set; }
    public string? ErrorMessage { get; set; }
}
