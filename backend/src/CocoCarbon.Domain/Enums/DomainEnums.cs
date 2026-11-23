namespace CocoCarbon.Domain.Enums;

public enum RoleCode
{
    Farmer = 1,
    CooperativeManager = 2,
    ProjectDeveloper = 3,
    Buyer = 4,
    Reviewer = 5,
    Admin = 6
}

public enum CoconutVariety
{
    TallDuaTa = 1,
    ShortXiemXanh = 2,
    OtherUnsupported = 99
}

public enum DataOriginTag
{
    Demo = 1,
    Synthetic = 2,
    UserDeclared = 3,
    AiExtracted = 4,
    Verified = 5
}

public enum FarmStatus
{
    Draft = 1,
    Submitted = 2,
    NeedsRevision = 3,
    ReadyForReview = 4,
    Reviewed = 5
}

public enum ProjectStatus
{
    Concept = 1,
    DataCollection = 2,
    Feasibility = 3,
    ValidationPreparation = 4,
    RegisteredExternalEvidenceOnly = 5
}

public enum ParticipationStatus
{
    Invited = 1,
    Active = 2,
    Withdrawn = 3
}

public enum ReviewDecisionStatus
{
    PendingReview = 1,
    Accepted = 2,
    Edited = 3,
    Rejected = 4
}

public enum RfqStatus
{
    Submitted = 1,
    InReview = 2,
    Responded = 3,
    Closed = 4
}

public enum ConsentType
{
    GpsLocation = 1,
    FieldPhoto = 2,
    AiThirdPartyProcessing = 3,
    CooperativeDataSharing = 4,
    TermsPrivacy = 5
}

public enum FileCategory
{
    LandDocument = 1,
    FarmPhoto = 2,
    ProjectDescription = 3,
    MonitoringReport = 4,
    VerificationEvidence = 5,
    Other = 99
}

public enum FileVisibility
{
    Private = 1,
    CooperativeAuthorized = 2,
    ReviewerAndAdmin = 3
}
