using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using CocoCarbon.Domain.Entities;
using CocoCarbon.Infrastructure.Persistence;

namespace CocoCarbon.Infrastructure.Services;

public interface IAuditService
{
    Task RecordAsync(
        Guid? actorUserId,
        string actorEmail,
        string action,
        string entityType,
        string entityId,
        object? beforeState,
        object? afterState,
        object? details = null,
        CancellationToken cancellationToken = default);
}

public sealed class AuditService(CocoCarbonDbContext db) : IAuditService
{
    public async Task RecordAsync(
        Guid? actorUserId,
        string actorEmail,
        string action,
        string entityType,
        string entityId,
        object? beforeState,
        object? afterState,
        object? details = null,
        CancellationToken cancellationToken = default)
    {
        var log = new AuditLog
        {
            ActorUserId = actorUserId,
            ActorEmail = actorEmail,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            BeforeHash = beforeState is null ? null : HashObject(beforeState),
            AfterHash = afterState is null ? null : HashObject(afterState),
            DetailsJson = details is null ? "{}" : JsonSerializer.Serialize(details),
            CreatedAt = DateTimeOffset.UtcNow
        };
        db.AuditLogs.Add(log);
        await db.SaveChangesAsync(cancellationToken);
    }

    private static string HashObject(object obj)
    {
        var json = JsonSerializer.Serialize(obj);
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(json));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
