namespace HRMS.Domain.Entities;

public class SysMenu
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string MenuKey { get; set; } = string.Empty;
    public string MenuName { get; set; } = string.Empty;
    public Guid? ParentId { get; set; }
    public SysMenu? Parent { get; set; }
    public string MenuType { get; set; } = "page";
    public int SortOrder { get; set; }
    public string? RoutePath { get; set; }
    public string? ComponentPath { get; set; }
    public string? Icon { get; set; }
    public bool IsVisible { get; set; } = true;
    public bool IsActive { get; set; } = true;
    public string? PermissionCode { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public ICollection<SysMenu> Children { get; set; } = new List<SysMenu>();
}
