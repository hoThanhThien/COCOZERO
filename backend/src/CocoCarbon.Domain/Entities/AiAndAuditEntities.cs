using CocoCarbon.Domain.Enums;

namespace CocoCarbon.Domain.Entities;

public class AiAnalysis
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? FileId { get; set; }
    public FarmFile? File { get; set; }
    public Guid? FarmId { get; set; }
    public Guid? ProjectId { get; set; }
    public Guid RequestedByUserId { get; set; }

    public string Provider { get; set; } = "gemini";
    public string ModelName { get; set; } = string.Empty;
    public string PromptVersion { get; set; } = string.Empty;
    public string Status { get; set; } = "Completed";
    public bool IsFallbackResult { get; set; }

    public string StructuredOutputJson { get; set; } = "{}";
    public string EvidenceJson { get; set; } = "[]";
    public string ReadinessRubricJson { get; set; } = "{}";
    public bool ReviewerRequired { get; set; } = true;
    public ReviewDecisionStatus ReviewStatus { get; set; } = ReviewDecisionStatus.PendingReview;

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public ICollection<ReviewerDecision> ReviewerDecisions { get; set; } = new List<ReviewerDecision>();
}

public class ReviewerDecision
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid AiAnalysisId { get; set; }
    public AiAnalysis? AiAnalysis { get; set; }
    public Guid ReviewerUserId { get; set; }
    public User? ReviewerUser { get; set; }
    public ReviewDecisionStatus Decision { get; set; }
    public string? EditedOutputJson { get; set; }
    public string Comment { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public class AuditLog
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? ActorUserId { get; set; }
    public string ActorEmail { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public string? BeforeHash { get; set; }
    public string? AfterHash { get; set; }
    public string DetailsJson { get; set; } = "{}";
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
