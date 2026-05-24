using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using HRMS.Infrastructure.Data;
using HRMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace HRMS.API.Services;

public sealed class InternalCallbackOptions
{
    public string SharedSecret { get; set; } = "internal-callback-secret";
    public int AllowedClockSkewSeconds { get; set; } = 300;
}

public sealed class ProcessedCallbackEventStore(HrmsDbContext dbContext)
{
    public async Task<bool> IsProcessedAsync(string eventId)
    {
        return await dbContext.SysProcessedEvents.AnyAsync(e => e.EventId == eventId);
    }

    public async Task MarkProcessedAsync(string eventId)
    {
        if (await IsProcessedAsync(eventId)) return;

        dbContext.SysProcessedEvents.Add(new SysProcessedEvent
        {
            EventId = eventId,
            ProcessedTime = DateTime.UtcNow
        });
        await dbContext.SaveChangesAsync();
    }
}

public sealed class CallbackSecurityService(IOptions<InternalCallbackOptions> options, ProcessedCallbackEventStore eventStore)
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    public async Task<CallbackValidationResult> ValidateAsync(HttpRequest request, object payload)
    {
        var eventId = request.Headers[CallbackSecurityHeaders.EventId].ToString();
        if (string.IsNullOrWhiteSpace(eventId))
        {
            return CallbackValidationResult.Fail(StatusCodes.Status401Unauthorized, "缺少回调事件 ID");
        }

        var timestampText = request.Headers[CallbackSecurityHeaders.Timestamp].ToString();
        if (string.IsNullOrWhiteSpace(timestampText))
        {
            return CallbackValidationResult.Fail(StatusCodes.Status401Unauthorized, "缺少回调时间戳");
        }

        if (!long.TryParse(timestampText, out var unixSeconds))
        {
            return CallbackValidationResult.Fail(StatusCodes.Status401Unauthorized, "回调时间戳格式无效");
        }

        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        if (Math.Abs(now - unixSeconds) > options.Value.AllowedClockSkewSeconds)
        {
            return CallbackValidationResult.Fail(StatusCodes.Status401Unauthorized, "回调时间戳已过期");
        }

        var signature = request.Headers[CallbackSecurityHeaders.Signature].ToString();
        if (string.IsNullOrWhiteSpace(signature))
        {
            return CallbackValidationResult.Fail(StatusCodes.Status401Unauthorized, "缺少回调签名");
        }

        var path = request.Path.Value ?? string.Empty;
        var payloadJson = JsonSerializer.Serialize(payload, SerializerOptions);
        var expectedSignature = CreateSignature(options.Value.SharedSecret, eventId, timestampText, path, payloadJson);
        if (!CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(signature),
                Encoding.UTF8.GetBytes(expectedSignature)))
        {
            return CallbackValidationResult.Fail(StatusCodes.Status401Unauthorized, "回调签名校验失败");
        }

        return await eventStore.IsProcessedAsync(eventId)
            ? CallbackValidationResult.Duplicate(eventId)
            : CallbackValidationResult.Success(eventId);
    }

    public async Task MarkProcessedAsync(string eventId)
    {
        if (!string.IsNullOrWhiteSpace(eventId))
        {
            await eventStore.MarkProcessedAsync(eventId);
        }
    }

    public static string CreateSignature(string sharedSecret, string eventId, string timestamp, string path, string payloadJson)
    {
        var secret = Encoding.UTF8.GetBytes(sharedSecret);
        using var hmac = new HMACSHA256(secret);
        var message = $"{eventId}\n{timestamp}\n{path}\n{payloadJson}";
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(message));
        return Convert.ToHexString(hash);
    }
}

public sealed class CallbackValidationResult
{
    public bool IsValid { get; private init; }
    public bool IsDuplicate { get; private init; }
    public int StatusCode { get; private init; }
    public string? ErrorMessage { get; private init; }
    public string? EventId { get; private init; }

    public static CallbackValidationResult Success(string eventId) => new()
    {
        IsValid = true,
        EventId = eventId
    };

    public static CallbackValidationResult Duplicate(string eventId) => new()
    {
        IsValid = true,
        IsDuplicate = true,
        EventId = eventId
    };

    public static CallbackValidationResult Fail(int statusCode, string errorMessage) => new()
    {
        StatusCode = statusCode,
        ErrorMessage = errorMessage
    };
}

public static class CallbackSecurityHeaders
{
    public const string EventId = "X-Callback-Event-Id";
    public const string Timestamp = "X-Callback-Timestamp";
    public const string Signature = "X-Callback-Signature";
}
