namespace TodoCenter.API.Models;

public class TodoTaskEntity
{
    public long Id { get; set; }
    public string TaskNo { get; set; } = string.Empty;
    public string TaskTypeCode { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string BusinessSystem { get; set; } = string.Empty;
    public string BusinessType { get; set; } = string.Empty;
    public string BusinessId { get; set; } = string.Empty;
    public string? ProcessInstanceId { get; set; }
    public string? ProcessNodeId { get; set; }
    public string AssigneeId { get; set; } = string.Empty;
    public string AssigneeName { get; set; } = string.Empty;
    public string TaskCategory { get; set; } = "Action";
    public string? OwnerDeptId { get; set; }
    public int Priority { get; set; }
    public string Status { get; set; } = "InProgress";
    public string? Result { get; set; }
    public DateTime? DueTime { get; set; }
    public DateTime? CompletedTime { get; set; }
    public DateTime? ReadTime { get; set; }
    public long? OriginalTaskId { get; set; }
    public string? SourcePayload { get; set; }
    public string? ExtData { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime CreatedTime { get; set; } = DateTime.UtcNow;
    public string? UpdatedBy { get; set; }
    public DateTime UpdatedTime { get; set; } = DateTime.UtcNow;

    public List<TodoTaskLogEntity> Logs { get; set; } = [];
    public List<TodoTaskNotifyLogEntity> NotifyLogs { get; set; } = [];
}

public class AgentSettingEntity
{
    public long Id { get; set; }
    public string PrincipalUserId { get; set; } = string.Empty;
    public string AgentUserId { get; set; } = string.Empty;
    public string ScopeType { get; set; } = "All";
    public string? TaskTypeCode { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public string Status { get; set; } = "Active";
    public DateTime CreatedTime { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedTime { get; set; } = DateTime.UtcNow;
}

public class TodoTaskLogEntity
{
    public long Id { get; set; }
    public long TaskId { get; set; }
    public TodoTaskEntity? Task { get; set; }
    public string Action { get; set; } = string.Empty;
    public string? OperatorId { get; set; }
    public string? OperatorName { get; set; }
    public string? ActionResult { get; set; }
    public string? Comment { get; set; }
    public string? BeforeStatus { get; set; }
    public string? AfterStatus { get; set; }
    public string? ExtData { get; set; }
    public DateTime CreatedTime { get; set; } = DateTime.UtcNow;
}

public class TodoTaskNotifyLogEntity
{
    public long Id { get; set; }
    public long TaskId { get; set; }
    public TodoTaskEntity? Task { get; set; }
    public string NotifyType { get; set; } = string.Empty;
    public string ReceiverId { get; set; } = string.Empty;
    public string ReceiverName { get; set; } = string.Empty;
    public string Channel { get; set; } = "InApp";
    public string Status { get; set; } = "Sent";
    public string Content { get; set; } = string.Empty;
    public DateTime SentTime { get; set; } = DateTime.UtcNow;
    public DateTime CreatedTime { get; set; } = DateTime.UtcNow;
}

public class ProcessedEventEntity
{
    public string EventId { get; set; } = string.Empty;
    public DateTime ProcessedTime { get; set; } = DateTime.UtcNow;
}
