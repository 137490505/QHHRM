namespace HRMS.Domain.Entities;

public class SysConfigParam
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Category { get; set; } = string.Empty;
    public string ParamKey { get; set; } = string.Empty;
    public string ParamValue { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid? OrgUnitId { get; set; }
    public bool IsGlobal { get; set; } = true;
    public bool IsActive { get; set; } = true;
    public bool TakeEffectImmediately { get; set; } = true;
    public DateTime? ScheduledTakeEffectDate { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public string? ChangeReason { get; set; }
}
