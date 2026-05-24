using HRMS.Application.DTOs;
using HRMS.Domain.Entities;
using HRMS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Globalization;
using System.Net.Http;
using System.Text;

namespace HRMS.API.Services;

public class SettingsService
{
    private const string SecurityCategory = "security";
    private const string CaptchaEnabledKey = "loginCaptchaEnabled";
    private const string SelectOptionsCategory = "selectOptions";
    private static readonly IReadOnlyDictionary<int, string> OrgLevelStandardLabels = new Dictionary<int, string>
    {
        [0] = "总部",
        [1] = "分公司",
        [2] = "部门",
        [3] = "产线",
        [4] = "班组",
        [5] = "供应商"
    };
    private static readonly IReadOnlyDictionary<string, int> OrgLevelLabelAliases = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
    {
        ["总部"] = 0,
        ["总公司"] = 0,
        ["分公司"] = 1,
        ["项目部"] = 1,
        ["项目"] = 1,
        ["部门"] = 2,
        ["产线"] = 3,
        ["生产线"] = 3,
        ["班组"] = 4,
        ["班"] = 4,
        ["供应商"] = 5
    };

    private static readonly Dictionary<string, List<SelectOptionItemDto>> DefaultSelectOptions =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["orgLevel"] =
            [
                CreateOption(0, "总部", sortOrder: 1),
                CreateOption(1, "分公司", sortOrder: 2),
                CreateOption(2, "部门", sortOrder: 3),
                CreateOption(3, "产线", sortOrder: 4),
                CreateOption(4, "班组", sortOrder: 5),
                CreateOption(5, "供应商", sortOrder: 6)
            ],
            ["employeeType"] =
            [
                CreateOption(0, "自主员工", sortOrder: 1),
                CreateOption(1, "第三方派遣", sortOrder: 2)
            ],
            ["salaryMode"] =
            [
                CreateOption(0, "时薪制", sortOrder: 1),
                CreateOption(1, "固薪制", sortOrder: 2),
                CreateOption(2, "计件制", sortOrder: 3),
                CreateOption(3, "混合制", sortOrder: 4)
            ],
            ["contractType"] =
            [
                CreateOption(0, "劳动合同", sortOrder: 1),
                CreateOption(1, "劳务合同", sortOrder: 2),
                CreateOption(2, "实习协议", sortOrder: 3)
            ],
            ["employeeTag"] =
            [
                CreateOption("实习生", "实习生", sortOrder: 1),
                CreateOption("试用期", "试用期", sortOrder: 2),
                CreateOption("正式员工", "正式员工", sortOrder: 3),
                CreateOption("管理层", "管理层", sortOrder: 4)
            ]
        };

    private readonly HrmsDbContext _dbContext;

    public SettingsService(HrmsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<LoginSecuritySettingsDto> GetLoginSecuritySettingsAsync()
    {
        return new LoginSecuritySettingsDto
        {
            CaptchaEnabled = await IsLoginCaptchaEnabledAsync()
        };
    }

    public async Task<LoginSecuritySettingsDto> UpdateLoginSecuritySettingsAsync(UpdateLoginSecuritySettingsDto dto)
    {
        var setting = await _dbContext.SysConfigParams
            .FirstOrDefaultAsync(x => x.Category == SecurityCategory && x.ParamKey == CaptchaEnabledKey);

        if (setting == null)
        {
            setting = new SysConfigParam
            {
                Category = SecurityCategory,
                ParamKey = CaptchaEnabledKey,
                Description = "是否启用登录验证码",
                IsGlobal = true,
                IsActive = true,
                TakeEffectImmediately = true
            };
            _dbContext.SysConfigParams.Add(setting);
        }

        setting.ParamValue = dto.CaptchaEnabled.ToString().ToLowerInvariant();
        setting.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync();

        return new LoginSecuritySettingsDto
        {
            CaptchaEnabled = dto.CaptchaEnabled
        };
    }

    public async Task<bool> IsLoginCaptchaEnabledAsync()
    {
        var paramValue = await _dbContext.SysConfigParams
            .Where(x => x.IsActive && x.Category == SecurityCategory && x.ParamKey == CaptchaEnabledKey)
            .Select(x => x.ParamValue)
            .FirstOrDefaultAsync();

        return bool.TryParse(paramValue, out var enabled) && enabled;
    }

    public async Task<List<SelectOptionItemDto>> GetSelectOptionsAsync(string category)
    {
        ValidateSelectOptionCategory(category);

        var paramValue = await _dbContext.SysConfigParams
            .Where(x => x.IsActive && x.Category == SelectOptionsCategory && x.ParamKey == category)
            .Select(x => x.ParamValue)
            .FirstOrDefaultAsync();

        if (string.IsNullOrWhiteSpace(paramValue))
        {
            var defaultResult = CloneDefaultOptions(category);
            return defaultResult;
        }

        try
        {
            var options = JsonConvert.DeserializeObject<List<SelectOptionItemDto>>(paramValue);
            if (options != null)
            {
                List<SelectOptionItemDto> result;
                if (string.Equals(category, "orgLevel", StringComparison.OrdinalIgnoreCase))
                {
                    result = NormalizeOrgLevelOptions(options);
                }
                else
                {
                    result = SanitizeOptions(category, options);
                }

                return result;
            }
        }
        catch (JsonException)
        {
        }

        return CloneDefaultOptions(category);
    }

    public async Task<List<SelectOptionItemDto>> UpdateSelectOptionsAsync(string category, UpdateSelectOptionsDto dto)
    {
        ValidateSelectOptionCategory(category);

        var normalizedOptions = NormalizeOptions(dto.Options);
        if (string.Equals(category, "orgLevel", StringComparison.OrdinalIgnoreCase))
        {
            normalizedOptions = NormalizeOrgLevelOptions(normalizedOptions);
        }

        var setting = await _dbContext.SysConfigParams
            .FirstOrDefaultAsync(x => x.Category == SelectOptionsCategory && x.ParamKey == category);

        if (setting == null)
        {
            setting = new SysConfigParam
            {
                Category = SelectOptionsCategory,
                ParamKey = category,
                Description = $"{category} 下拉选项",
                IsGlobal = true,
                IsActive = true,
                TakeEffectImmediately = true
            };
            _dbContext.SysConfigParams.Add(setting);
        }

        setting.ParamValue = JsonConvert.SerializeObject(normalizedOptions);
        setting.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();

        return SortOptions(normalizedOptions);
    }

    private static void ValidateSelectOptionCategory(string category)
    {
        if (!DefaultSelectOptions.ContainsKey(category))
        {
            throw new ArgumentException("不支持的下拉选项分类");
        }
    }

    private static List<SelectOptionItemDto> NormalizeOptions(IEnumerable<SelectOptionItemDto>? options)
    {
        if (options == null)
        {
            throw new ArgumentException("下拉选项不能为空");
        }

        var result = new List<SelectOptionItemDto>();
        var seenValues = new HashSet<string>(StringComparer.Ordinal);

        var index = 0;
        foreach (var option in options)
        {
            if (string.IsNullOrWhiteSpace(option.Label))
            {
                throw new ArgumentException("下拉选项标签不能为空");
            }

            if (option.Value == null)
            {
                throw new ArgumentException("下拉选项值不能为空");
            }

            var normalizedValue = option.Value as JToken ?? JToken.FromObject(option.Value);
            if (normalizedValue.Type == JTokenType.Null || normalizedValue.Type == JTokenType.Undefined)
            {
                throw new ArgumentException("下拉选项值不能为空");
            }

            var valueKey = normalizedValue.ToString(Formatting.None);
            if (!seenValues.Add(valueKey))
            {
                throw new ArgumentException($"下拉选项值不能重复: {valueKey}");
            }

            result.Add(new SelectOptionItemDto
            {
                Value = normalizedValue.ToObject<object>(),
                Label = option.Label.Trim(),
                LabelEn = string.IsNullOrWhiteSpace(option.LabelEn) ? null : option.LabelEn.Trim(),
                SortOrder = NormalizeSortOrder(option.SortOrder, index)
            });

            index++;
        }

        return SortOptions(result);
    }

    private static List<SelectOptionItemDto> SanitizeOptions(string category, IEnumerable<SelectOptionItemDto> options)
    {
        var defaultOptions = DefaultSelectOptions[category];
        var result = new List<SelectOptionItemDto>();
        var seenValues = new HashSet<string>(StringComparer.Ordinal);
        var index = 0;

        foreach (var option in options)
        {
            var label = option.Label?.Trim();
            var labelEn = string.IsNullOrWhiteSpace(option.LabelEn) ? null : option.LabelEn.Trim();
            var resolvedValue = ResolveOptionValue(category, option.Value, label, labelEn, index, defaultOptions);

            if (string.IsNullOrWhiteSpace(label) || resolvedValue == null)
            {
                index++;
                continue;
            }

            var valueKey = (resolvedValue as JToken ?? JToken.FromObject(resolvedValue)).ToString(Formatting.None);
            if (!seenValues.Add(valueKey))
            {
                index++;
                continue;
            }

            result.Add(new SelectOptionItemDto
            {
                Value = resolvedValue,
                Label = label,
                LabelEn = labelEn,
                SortOrder = NormalizeSortOrder(option.SortOrder, index)
            });

            index++;
        }

        return SortOptions(result);
    }

    private static List<SelectOptionItemDto> NormalizeOrgLevelOptions(IEnumerable<SelectOptionItemDto> options)
    {
        var result = new List<SelectOptionItemDto>();
        var seenValues = new HashSet<int>();
        var index = 0;

        foreach (var option in options)
        {
            var label = option.Label?.Trim();
            var labelEn = string.IsNullOrWhiteSpace(option.LabelEn) ? null : option.LabelEn.Trim();
            var parsedValue = TryParseInt(option.Value);

            int resolvedValue;
            string? resolvedLabel;

            if (!string.IsNullOrWhiteSpace(label) && OrgLevelLabelAliases.TryGetValue(label, out var aliasValue))
            {
                resolvedValue = aliasValue;
                resolvedLabel = OrgLevelStandardLabels[aliasValue];
            }
            else if (parsedValue.HasValue)
            {
                resolvedValue = parsedValue.Value;
                // Keep explicit custom labels such as "员工" instead of forcing
                // the built-in label for the same numeric value (for example 5 => "供应商").
                resolvedLabel = !string.IsNullOrWhiteSpace(label)
                    ? label
                    : OrgLevelStandardLabels.GetValueOrDefault(parsedValue.Value);
            }
            else
            {
                index++;
                continue;
            }

            if (string.IsNullOrWhiteSpace(resolvedLabel))
            {
                index++;
                continue;
            }

            if (!seenValues.Add(resolvedValue))
            {
                index++;
                continue;
            }

            result.Add(new SelectOptionItemDto
            {
                Value = resolvedValue,
                Label = resolvedLabel,
                LabelEn = labelEn,
                SortOrder = NormalizeSortOrder(option.SortOrder, index)
            });

            index++;
        }

        return SortOptions(result);
    }

    private static int NormalizeSortOrder(int sortOrder, int index)
    {
        return sortOrder > 0 ? sortOrder : index + 1;
    }

    private static List<SelectOptionItemDto> SortOptions(IEnumerable<SelectOptionItemDto> options)
    {
        return options
            .OrderBy(option => option.SortOrder)
            .ThenBy(option => option.Label, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static object? ResolveOptionValue(
        string category,
        object? currentValue,
        string? label,
        string? labelEn,
        int index,
        IReadOnlyList<SelectOptionItemDto> defaultOptions)
    {
        var normalizedValue = NormalizeOptionValue(currentValue);
        if (normalizedValue != null)
        {
            return normalizedValue;
        }

        var matchedDefault = defaultOptions.FirstOrDefault(option =>
            string.Equals(option.Label, label, StringComparison.OrdinalIgnoreCase) ||
            (!string.IsNullOrWhiteSpace(labelEn) && string.Equals(option.LabelEn, labelEn, StringComparison.OrdinalIgnoreCase)));

        if (matchedDefault?.Value != null)
        {
            return NormalizeOptionValue((matchedDefault.Value as JToken)?.DeepClone() ?? matchedDefault.Value);
        }

        if (string.Equals(category, "employeeTag", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(label))
        {
            return label;
        }

        if (index < defaultOptions.Count)
        {
            var fallbackValue = defaultOptions[index].Value;
            if (fallbackValue != null)
            {
                return NormalizeOptionValue((fallbackValue as JToken)?.DeepClone() ?? fallbackValue);
            }
        }

        return null;
    }

    private static object? NormalizeOptionValue(object? value)
    {
        if (value == null)
        {
            return null;
        }

        var token = value as JToken ?? JToken.FromObject(value);
        if (token.Type == JTokenType.Null || token.Type == JTokenType.Undefined)
        {
            return null;
        }

        if (token.Type == JTokenType.String)
        {
            var text = token.ToObject<string>()?.Trim();
            return string.IsNullOrWhiteSpace(text) ? null : text;
        }

        return token.ToObject<object>();
    }

    private static int? TryParseInt(object? value)
    {
        if (value == null)
        {
            return null;
        }

        if (value is JValue jValue)
        {
            value = jValue.Value;
        }

        return value switch
        {
            int intValue => intValue,
            long longValue when longValue is >= int.MinValue and <= int.MaxValue => (int)longValue,
            short shortValue => shortValue,
            byte byteValue => byteValue,
            _ when int.TryParse(Convert.ToString(value, CultureInfo.InvariantCulture), NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed) => parsed,
            _ => null
        };
    }

    private static List<SelectOptionItemDto> CloneDefaultOptions(string category)
    {
        return DefaultSelectOptions[category]
            .Select(option => new SelectOptionItemDto
            {
                Value = (option.Value as JToken)?.DeepClone()?.ToObject<object>(),
                Label = option.Label,
                LabelEn = option.LabelEn,
                SortOrder = option.SortOrder
            })
            .ToList();
    }

    private static SelectOptionItemDto CreateOption<TValue>(TValue value, string label, string? labelEn = null, int sortOrder = 0)
    {
        return new SelectOptionItemDto
        {
            Value = JToken.FromObject(value!),
            Label = label,
            LabelEn = labelEn,
            SortOrder = sortOrder
        };
    }
}
