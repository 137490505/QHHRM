namespace HRMS.Domain.Entities;

public class SysUser
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public Guid? EmployeeId { get; set; }
    public Employee? Employee { get; set; }
    public Guid? PostId { get; set; }
    public SysPost? Post { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsAdmin { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public ICollection<SysUserRole> UserRoles { get; set; } = new List<SysUserRole>();
}

public class SysTokenSession
{
    public Guid Id { get; set; }
    public string Token { get; set; } = string.Empty;
    public Guid UserId { get; set; }
    public bool IsAdmin { get; set; }
    public string PermissionsJson { get; set; } = "[]";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAt { get; set; }
}

public class SysProcessedEvent
{
    public string EventId { get; set; } = string.Empty;
    public DateTime ProcessedTime { get; set; } = DateTime.UtcNow;
}
