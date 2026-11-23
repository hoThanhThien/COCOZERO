using CocoCarbon.Domain.Enums;

namespace CocoCarbon.Domain.Entities;

public class User
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string FirebaseUid { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string Status { get; set; } = "Active";
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
    public ICollection<ConsentRecord> ConsentRecords { get; set; } = new List<ConsentRecord>();
}

public class Role
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public RoleCode Code { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

public class UserRole
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public User? User { get; set; }
    public Guid RoleId { get; set; }
    public Role? Role { get; set; }
    public RoleCode RoleCode { get; set; }
    public string ScopeType { get; set; } = "Global";
    public Guid? ScopeId { get; set; }
    public string ApprovalStatus { get; set; } = "Approved";
    public Guid? ApprovedByUserId { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public class ConsentRecord
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public User? User { get; set; }
    public ConsentType ConsentType { get; set; }
    public bool Granted { get; set; }
    public string PolicyVersion { get; set; } = "v1.0-2026-11";
    public string TextShown { get; set; } = string.Empty;
    public DateTimeOffset RecordedAt { get; set; } = DateTimeOffset.UtcNow;
}
