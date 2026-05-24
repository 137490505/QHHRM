namespace HRMS.Application.DTOs;

public class OrgUnitDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int Level { get; set; }
    public Guid? ParentId { get; set; }
    public string? ParentName { get; set; }
    public string? ManagerId { get; set; }
    public string? Manager { get; set; }
    public bool IsActive { get; set; }
    public List<OrgUnitDto> Children { get; set; } = new List<OrgUnitDto>();
}

public class CreateOrgUnitDto
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int Level { get; set; }
    public Guid? ParentId { get; set; }
    public string? ManagerId { get; set; }
}

public class UpdateOrgUnitDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int Level { get; set; }
    public Guid? ParentId { get; set; }
    public string? ManagerId { get; set; }
    public bool IsActive { get; set; }
}

public class BatchUpdateOrgUnitDto
{
    public List<Guid> Ids { get; set; } = new();
    public int? Level { get; set; }
    public bool? IsActive { get; set; }
}
