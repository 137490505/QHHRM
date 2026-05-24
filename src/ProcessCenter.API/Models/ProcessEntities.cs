namespace ProcessCenter.API.Models;

public class ProcessDefinitionEntity
{
    public long Id { get; set; }
    public string ProcessCode { get; set; } = string.Empty;
    public string ProcessName { get; set; } = string.Empty;
    public string BusinessType { get; set; } = string.Empty;
    public int VersionNo { get; set; }
    public string Status { get; set; } = "Draft";
    public string? CallbackConfig { get; set; } // JSON: { "ApprovedUrl": "...", "RejectedUrl": "..." }
    public DateTime CreatedTime { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedTime { get; set; } = DateTime.UtcNow;
    public List<ProcessDefinitionNodeEntity> Nodes { get; set; } = [];
}

public class ProcessDefinitionNodeEntity
{
    public long Id { get; set; }
    public long ProcessDefinitionId { get; set; }
    public string NodeId { get; set; } = string.Empty;
    public string NodeName { get; set; } = string.Empty;
    public string NodeType { get; set; } = "UserTask";
    public string AssigneeType { get; set; } = "User";
    public string AssigneeId { get; set; } = string.Empty;
    public string AssigneeName { get; set; } = string.Empty;
    public string? MultiPersonType { get; set; } // None, Any, All
    public string? NextNodeId { get; set; }
    public int SortOrder { get; set; }
    public List<ProcessNodeConditionEntity> Conditions { get; set; } = [];
}

public class ProcessNodeConditionEntity
{
    public long Id { get; set; }
    public long ProcessDefinitionNodeId { get; set; }
    public string? ConditionExpression { get; set; }
    public string TargetNodeId { get; set; } = string.Empty;
}

public class ProcessInstanceEntity
{
    public long Id { get; set; }
    public string InstanceNo { get; set; } = string.Empty;
    public string ProcessCode { get; set; } = string.Empty;
    public int VersionNo { get; set; }
    public string BusinessSystem { get; set; } = string.Empty;
    public string BusinessType { get; set; } = string.Empty;
    public string BusinessId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Status { get; set; } = "Running";
    public string? CurrentNodeId { get; set; }
    public string? CurrentNodeName { get; set; }
    public string? CurrentAssigneeId { get; set; }
    public string? CurrentAssigneeName { get; set; }
    public int CurrentNodeIndex { get; set; }
    public string StarterId { get; set; } = string.Empty;
    public string StarterName { get; set; } = string.Empty;
    public DateTime StartedTime { get; set; } = DateTime.UtcNow;
    public DateTime? FinishedTime { get; set; }
    public string? ExtData { get; set; }
    public List<ProcessNodeHistoryEntity> Histories { get; set; } = [];
}

public class ProcessNodeHistoryEntity
{
    public long Id { get; set; }
    public long ProcessInstanceId { get; set; }
    public ProcessInstanceEntity? ProcessInstance { get; set; }
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

public class ProcessedEventEntity
{
    public string EventId { get; set; } = string.Empty;
    public DateTime ProcessedTime { get; set; } = DateTime.UtcNow;
}

public class ProcessCallbackLogEntity
{
    public long Id { get; set; }
    public long ProcessInstanceId { get; set; }
    public ProcessInstanceEntity? ProcessInstance { get; set; }
    public string CallbackUrl { get; set; } = string.Empty;
    public string Payload { get; set; } = string.Empty;
    public int StatusCode { get; set; }
    public string? Response { get; set; }
    public string Status { get; set; } = "Success"; // Success, Failed
    public int RetryCount { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime CreatedTime { get; set; } = DateTime.UtcNow;
}
