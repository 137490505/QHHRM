using Microsoft.AspNetCore.Mvc;

namespace HRMS.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class CaptchaController : ControllerBase
{
    private static readonly Random _random = new Random();
    private static readonly string[] Fonts = { "Arial", "Verdana", "Times New Roman", "Georgia", "Courier New" };

    [HttpGet]
    public IActionResult Get()
    {
        string code = GenerateCode(4);
        HttpContext.Session.SetString("CaptchaCode", code);

        string svg = GenerateSvg(code);
        return Content(svg, "image/svg+xml");
    }

    private string GenerateCode(int length)
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        char[] code = new char[length];
        for (int i = 0; i < length; i++)
        {
            code[i] = chars[_random.Next(chars.Length)];
        }
        return new string(code);
    }

    private string GenerateSvg(string code)
    {
        int width = 120;
        int height = 40;

        var colors = new[]
        {
            "#667eea", "#764ba2", "#f093fb", "#f5576c",
            "#4facfe", "#00f2fe", "#43e97b", "#38f9d7"
        };
        string bgColor = "#f8f9fa";
        string textColor = colors[_random.Next(colors.Length)];

        var paths = new List<string>();
        for (int i = 0; i < 3; i++)
        {
            int x1 = _random.Next(width);
            int y1 = _random.Next(height);
            int x2 = _random.Next(width);
            int y2 = _random.Next(height);
            string lineColor = colors[_random.Next(colors.Length)];
            double opacity = _random.NextDouble() * 0.3 + 0.1;
            paths.Add($"<line x1=\"{x1}\" y1=\"{y1}\" x2=\"{x2}\" y2=\"{y2}\" stroke=\"{lineColor}\" stroke-width=\"1\" opacity=\"{opacity}\" />");
        }

        string lines = string.Join("", paths);

        return $@"<svg xmlns=""http://www.w3.org/2000/svg"" width=""{width}"" height=""{height}"" viewBox=""0 0 {width} {height}"">
            <rect width=""{width}"" height=""{height}"" fill=""{bgColor}""/>
            {lines}
            <text x=""{width / 2}"" y=""{(height + 20) / 2}"" font-family=""{Fonts[_random.Next(Fonts.Length)]}"" font-size=""24"" font-weight=""bold"" fill=""{textColor}"" text-anchor=""middle"">{code}</text>
            <circle cx=""{_random.Next(width)}"" cy=""{_random.Next(height)}"" r=""2"" fill=""{colors[_random.Next(colors.Length)]}"" opacity=""0.5""/>
            <circle cx=""{_random.Next(width)}"" cy=""{_random.Next(height)}"" r=""1.5"" fill=""{colors[_random.Next(colors.Length)]}"" opacity=""0.4""/>
            <circle cx=""{_random.Next(width)}"" cy=""{_random.Next(height)}"" r=""2"" fill=""{colors[_random.Next(colors.Length)]}"" opacity=""0.3""/>
        </svg>";
    }
}