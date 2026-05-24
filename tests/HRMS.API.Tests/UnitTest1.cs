using System.Collections;
using System.Reflection;
using HRMS.API.Services;

namespace HRMS.API.Tests;

public class RbacDataInitializerTests
{
    [Fact]
    public void MenuDefinitions_ShouldUseUniquePermissionCodes()
    {
        var definitions = GetMenuDefinitions();

        var duplicatePermissionCodes = definitions
            .GroupBy(x => x.PermissionCode, StringComparer.OrdinalIgnoreCase)
            .Where(group => !string.IsNullOrWhiteSpace(group.Key) && group.Count() > 1)
            .Select(group => $"{group.Key}: {string.Join(", ", group.Select(item => item.MenuKey))}")
            .ToList();

        Assert.True(
            duplicatePermissionCodes.Count == 0,
            $"发现重复的 PermissionCode: {string.Join("; ", duplicatePermissionCodes)}");
    }

    [Fact]
    public void OrgChartMenu_ShouldUseDedicatedPermissionCode()
    {
        var definitions = GetMenuDefinitions();

        var orgChart = Assert.Single(definitions.Where(x => x.MenuKey == "org-chart"));
        Assert.Equal("page.org.chart", orgChart.PermissionCode);
    }

    private static IReadOnlyList<MenuDefinitionSnapshot> GetMenuDefinitions()
    {
        var method = typeof(RbacDataInitializer).GetMethod("GetMenuDefinitions", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(method);

        var result = method!.Invoke(null, null);
        var definitions = Assert.IsAssignableFrom<IEnumerable>(result);

        return definitions
            .Cast<object>()
            .Select(item => new MenuDefinitionSnapshot(
                GetRequiredString(item, "MenuKey"),
                GetRequiredString(item, "PermissionCode")))
            .ToList();
    }

    private static string GetRequiredString(object instance, string propertyName)
    {
        var property = instance.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
        Assert.NotNull(property);

        var value = property!.GetValue(instance) as string;
        Assert.False(string.IsNullOrWhiteSpace(value), $"菜单定义缺少 {propertyName}");

        return value!;
    }

    private sealed record MenuDefinitionSnapshot(string MenuKey, string PermissionCode);
}
