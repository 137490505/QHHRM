using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace TodoCenter.API.Contracts;

public class CreateTodoTaskRequest
{
    [Required]
    public string TaskTypeCode { get; set; } = string.Empty;

    [Required]
    public string Title { get; set; } = string.Empty;

    [Required]
    public string BusinessSystem { get; set; } = string.Empty;

    [Required]
    public string BusinessType { get; set; } = string.Empty;

    [Required]
    public string BusinessId { get; set; } = string.Empty;

    public string? ProcessInstanceId { get; set; }
    public string? ProcessNodeId { get; set; }

    [Required]
    public string AssigneeId { get; set; } = string.Empty;

    [Required]
    public string AssigneeName { get; set; } = string.Empty;

    public string? OwnerDeptId { get; set; }
    public int Priority { get; set; } = 2;
    public DateTime? DueTime { get; set; }
    public string? SourcePayload { get; set; }
    public string? ExtData { get; set; }
    public string? CreatedBy { get; set; }
}

public class TodoTaskActionRequest
{
    public string? OperatorId { get; set; }
    public string? OperatorName { get; set; }
    public string? Comment { get; set; }
}

public class TodoTaskTransferRequest : TodoTaskActionRequest
{
    [Required]
    public string TargetAssigneeId { get; set; } = string.Empty;

    [Required]
    public string TargetAssigneeName { get; set; } = string.Empty;
}

public class TodoTaskClaimRequest : TodoTaskActionRequest
{
    [Required]
    public string AssigneeId { get; set; } = string.Empty;

    [Required]
    public string AssigneeName { get; set; } = string.Empty;
}

public class TodoTaskCopyRequest : TodoTaskActionRequest
{
    [Required]
    [MinLength(1)]
    public List<TodoCopyReceiverRequest> Receivers { get; set; } = [];
}

public class TodoCopyReceiverRequest
{
    [Required]
    public string ReceiverId { get; set; } = string.Empty;

    [Required]
    public string ReceiverName { get; set; } = string.Empty;
}

public class TodoBatchCompleteRequest
{
    [Required]
    public List<long> TaskIds { get; set; } = [];

    public string? OperatorId { get; set; }
    public string? OperatorName { get; set; }
    public string? Comment { get; set; }
}

public class TodoBatchRejectRequest
{
    [Required]
    public List<long> TaskIds { get; set; } = [];

    public string? OperatorId { get; set; }
    public string? OperatorName { get; set; }
    public string? Comment { get; set; }
}

public class TodoBatchTransferRequest : TodoTaskActionRequest
{
    [Required]
    public List<long> TaskIds { get; set; } = [];

    [Required]
    public string TargetAssigneeId { get; set; } = string.Empty;

    [Required]
    public string TargetAssigneeName { get; set; } = string.Empty;
}

public class TodoBatchUrgeRequest : TodoTaskActionRequest
{
    [Required]
    public List<long> TaskIds { get; set; } = [];
}

public class TodoCancelByProcessNodeRequest : TodoTaskActionRequest
{
    [Required]
    public string ProcessInstanceId { get; set; } = string.Empty;

    [Required]
    public string ProcessNodeId { get; set; } = string.Empty;

    public string TargetStatus { get; set; } = "Canceled";
}

public class CreateAgentSettingRequest
{
    [Required]
    public string PrincipalUserId { get; set; } = string.Empty;

    [Required]
    public string AgentUserId { get; set; } = string.Empty;

    public string ScopeType { get; set; } = "All";
    public string? TaskTypeCode { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
}

public class UpdateAgentSettingRequest
{
    [Required]
    public string AgentUserId { get; set; } = string.Empty;

    public string ScopeType { get; set; } = "All";
    public string? TaskTypeCode { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
}

public class TodoTaskDto
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
    public string Status { get; set; } = string.Empty;
    public string? Result { get; set; }
    public int Priority { get; set; }
    public DateTime? DueTime { get; set; }
    public DateTime? CompletedTime { get; set; }
    public DateTime? ReadTime { get; set; }
    public long? OriginalTaskId { get; set; }
    public bool IsClaimable { get; set; }
    public string? SourcePayload { get; set; }
    public JsonElement? SourcePayloadData { get; set; }
    public string? ExtData { get; set; }
    public JsonElement? ExtDataObject { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime CreatedTime { get; set; }
    public DateTime UpdatedTime { get; set; }
}

public class TodoTaskDetailDto
{
    public TodoTaskDto Task { get; set; } = new();
    public IReadOnlyList<TodoTaskLogDto> Logs { get; set; } = [];
    public IReadOnlyList<TodoTaskNotifyLogDto> NotifyLogs { get; set; } = [];
}

public class TodoTaskLogDto
{
    public long Id { get; set; }
    public string Action { get; set; } = string.Empty;
    public string? OperatorId { get; set; }
    public string? OperatorName { get; set; }
    public string? ActionResult { get; set; }
    public string? Comment { get; set; }
    public string? BeforeStatus { get; set; }
    public string? AfterStatus { get; set; }
    public string? ExtData { get; set; }
    public JsonElement? ExtDataObject { get; set; }
    public DateTime CreatedTime { get; set; }
}

public class TodoTaskNotifyLogDto
{
    public long Id { get; set; }
    public string NotifyType { get; set; } = string.Empty;
    public string ReceiverId { get; set; } = string.Empty;
    public string ReceiverName { get; set; } = string.Empty;
    public string Channel { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTime SentTime { get; set; }
    public DateTime CreatedTime { get; set; }
}

public static class TodoNotifyChannels
{
    public const string InApp = "InApp";
    public const string DingTalk = "DingTalk";
    public const string WeChat = "WeChat";
    public const string Email = "Email";
    public const string SMS = "SMS";
}

public static class TodoNotifyTypes
{
    public const string NewTask = "NewTask";
    public const string Urge = "Urge";
    public const string Timeout = "Timeout";
    public const string Completed = "Completed";
    public const string Rejected = "Rejected";
}

public class AgentSettingDto
{
    public long Id { get; set; }
    public string PrincipalUserId { get; set; } = string.Empty;
    public string AgentUserId { get; set; } = string.Empty;
    public string ScopeType { get; set; } = string.Empty;
    public string? TaskTypeCode { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedTime { get; set; }
    public DateTime UpdatedTime { get; set; }
}

public class TodoKpiSummaryDto
{
    public int TotalCount { get; set; }
    public int PendingCount { get; set; }
    public int CompletedCount { get; set; }
    public int TimeoutCount { get; set; }
    public decimal OnTimeRate { get; set; }
}

public class TodoTaskCopyBatchResultDto
{
    public int CreatedCount { get; set; }
    public IReadOnlyList<TodoTaskDto> Tasks { get; set; } = [];
}

public class TodoTaskPoolSummaryDto
{
    public int ClaimableCount { get; set; }
    public int CopiedUnreadCount { get; set; }
    public int CreatedByMeCount { get; set; }
    public int ProcessedByMeCount { get; set; }
}

public class TodoTaskViewSummaryDto
{
    public int PendingCount { get; set; }
    public int ProcessedCount { get; set; }
    public int CreatedCount { get; set; }
    public int CopiedCount { get; set; }
    public int PoolCount { get; set; }
    public int TransferredCount { get; set; }
    public int AgentPendingCount { get; set; }
}

public class TodoKpiAnalysisDto
{
    public string Dimension { get; set; } = string.Empty;
    public IReadOnlyList<TodoKpiDimensionItemDto> Items { get; set; } = [];
}

public class TodoKpiDimensionItemDto
{
    public string Key { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public int TotalCount { get; set; }
    public int PendingCount { get; set; }
    public int CompletedCount { get; set; }
    public int RejectedCount { get; set; }
    public int TimeoutCount { get; set; }
    public decimal AverageHandleMinutes { get; set; }
    public decimal OnTimeRate { get; set; }
}
