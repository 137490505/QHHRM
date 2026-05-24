using System.Security.Cryptography;
using System.Text;

namespace TodoCenter.API.Services;

public sealed class InternalCallbackOptions
{
    public string SharedSecret { get; set; } = "internal-callback-secret";
    public int AllowedClockSkewSeconds { get; set; } = 300;
}

public static class CallbackSecurityHeaders
{
    public const string EventId = "X-Callback-Event-Id";
    public const string Timestamp = "X-Callback-Timestamp";
    public const string Signature = "X-Callback-Signature";
}

public static class CallbackSecurityService
{
    public static string CreateSignature(string sharedSecret, string eventId, string timestamp, string path, string payloadJson)
    {
        var secret = Encoding.UTF8.GetBytes(sharedSecret);
        using var hmac = new HMACSHA256(secret);
        var message = $"{eventId}\n{timestamp}\n{path}\n{payloadJson}";
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(message));
        return Convert.ToHexString(hash);
    }
}
