using System.Text.Json;
using HRMS.Infrastructure.Data;
using HRMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace HRMS.API.Services;

public record UserSessionContext(Guid UserId, bool IsAdmin, HashSet<string> Permissions);

public class TokenSessionStore(HrmsDbContext dbContext)
{
    public async Task SetAsync(string token, UserSessionContext context, TimeSpan? expiry = null)
    {
        var session = await dbContext.SysTokenSessions.FirstOrDefaultAsync(s => s.Token == token);
        if (session == null)
        {
            session = new SysTokenSession { Id = Guid.NewGuid(), Token = token };
            dbContext.SysTokenSessions.Add(session);
        }

        session.UserId = context.UserId;
        session.IsAdmin = context.IsAdmin;
        session.PermissionsJson = JsonSerializer.Serialize(context.Permissions);
        session.CreatedAt = DateTime.UtcNow;
        session.ExpiresAt = DateTime.UtcNow.Add(expiry ?? TimeSpan.FromHours(8));

        await dbContext.SaveChangesAsync();
    }

    public async Task<UserSessionContext?> GetAsync(string token)
    {
        var session = await dbContext.SysTokenSessions
            .FirstOrDefaultAsync(s => s.Token == token && s.ExpiresAt > DateTime.UtcNow);

        if (session == null) return null;

        var permissions = JsonSerializer.Deserialize<HashSet<string>>(session.PermissionsJson) ?? [];
        return new UserSessionContext(session.UserId, session.IsAdmin, permissions);
    }

    public async Task RemoveAsync(string token)
    {
        var session = await dbContext.SysTokenSessions.FirstOrDefaultAsync(s => s.Token == token);
        if (session != null)
        {
            dbContext.SysTokenSessions.Remove(session);
            await dbContext.SaveChangesAsync();
        }
    }
}
