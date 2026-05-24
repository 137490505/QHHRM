namespace HRMS.Application.DTOs;

public class LoginSecuritySettingsDto
{
    public bool CaptchaEnabled { get; set; }
}

public class UpdateLoginSecuritySettingsDto
{
    public bool CaptchaEnabled { get; set; }
}

public class SelectOptionItemDto
{
    public object? Value { get; set; }
    public string Label { get; set; } = string.Empty;
    public string? LabelEn { get; set; }
    public int SortOrder { get; set; }
}

public class UpdateSelectOptionsDto
{
    public List<SelectOptionItemDto> Options { get; set; } = [];
}
