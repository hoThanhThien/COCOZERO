using CocoCarbon.Domain.Constants;
using CocoCarbon.Domain.Enums;

namespace CocoCarbon.Domain.Entities;

public class Cooperative
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Province { get; set; } = ScientificConstants.CurrentProvinceDefault;
    public string Commune { get; set; } = string.Empty;
    public string HistoricalRegion { get; set; } = ScientificConstants.HistoricalRegionBenTre;
    public Guid ManagerUserId { get; set; }
    public User? ManagerUser { get; set; }
    public string Status { get; set; } = "Active";
    public DataOriginTag DataOriginTag { get; set; } = DataOriginTag.UserDeclared;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public ICollection<CooperativeMember> Members { get; set; } = new List<CooperativeMember>();
}

public class CooperativeMember
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CooperativeId { get; set; }
    public Cooperative? Cooperative { get; set; }
    public Guid UserId { get; set; }
    public User? User { get; set; }
    public Guid? FarmId { get; set; }
    public Farm? Farm { get; set; }
    public bool IsDataAuthorized { get; set; }
    public ParticipationStatus ParticipationStatus { get; set; } = ParticipationStatus.Active;
    public DateTimeOffset JoinedAt { get; set; } = DateTimeOffset.UtcNow;
}

public class Project
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid DeveloperId { get; set; }
    public User? Developer { get; set; }
    public Guid? CooperativeId { get; set; }
    public Cooperative? Cooperative { get; set; }

    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string ProjectType { get; set; } = "Coconut Agroforestry & Soil Management";
    public string Province { get; set; } = ScientificConstants.CurrentProvinceDefault;
    public string HistoricalRegion { get; set; } = ScientificConstants.HistoricalRegionBenTre;
    public decimal DeclaredTotalAreaHa { get; set; }

    public string IntendedMethodology { get; set; } = "Pending formal selection (IPCC AFOLU / Local Tier-2 research)";
    public string MethodologyNotes { get; set; } = string.Empty;
    public string BaselineSummary { get; set; } = string.Empty;
    public string MonitoringPlanSummary { get; set; } = string.Empty;

    public decimal ReadinessScore { get; set; }
    public string ReadinessLabel { get; set; } = "Mức độ đầy đủ hồ sơ / Project Readiness";
    public string VerificationStatus { get; set; } = "Chưa xác minh độc lập";
    public ProjectStatus ProjectStatus { get; set; } = ProjectStatus.DataCollection;
    public DataOriginTag DataOriginTag { get; set; } = DataOriginTag.UserDeclared;

    public string Disclaimer { get; set; } =
        "Thông tin dự án phục vụ chuẩn bị hồ sơ/MRV và kết nối tìm hiểu (Directory/RFQ). " +
        "Không phải chứng nhận tín chỉ đã phát hành, không phải xác nhận quyền sở hữu hay giao dịch trên sàn cấp phép.";

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    public ICollection<ProjectFarm> ProjectFarms { get; set; } = new List<ProjectFarm>();
    public ICollection<CreditLot> CreditLots { get; set; } = new List<CreditLot>();
    public ICollection<Listing> Listings { get; set; } = new List<Listing>();
}

public class ProjectFarm
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ProjectId { get; set; }
    public Project? Project { get; set; }
    public Guid FarmId { get; set; }
    public Farm? Farm { get; set; }
    public ParticipationStatus ParticipationStatus { get; set; } = ParticipationStatus.Active;
    public bool HasFarmerConsent { get; set; }
    public DateTimeOffset JoinedAt { get; set; } = DateTimeOffset.UtcNow;
}

public class CreditLot
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ProjectId { get; set; }
    public Project? Project { get; set; }
    public string RegistryName { get; set; } = "Chưa đăng ký registry chính thức";
    public string? SerialRange { get; set; }
    public int? VintageYear { get; set; }
    public decimal QuantityTCO2e { get; set; }
    public string IssuanceStatus { get; set; } = "NotIssued_Unverified";
    public string? VerificationEvidenceRef { get; set; }
    public bool HasOfficialRegistryEvidence { get; set; }
    public DataOriginTag DataOriginTag { get; set; } = DataOriginTag.UserDeclared;
}

public class Listing
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ProjectId { get; set; }
    public Project? Project { get; set; }
    public Guid SellerUserId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string StageNote { get; set; } = "Information & Pre-RFQ Only (No Financial Settlement)";
    public bool IsPublic { get; set; } = true;
    public DataOriginTag DataOriginTag { get; set; } = DataOriginTag.UserDeclared;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public class Rfq
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ProjectId { get; set; }
    public Project? Project { get; set; }
    public Guid? ListingId { get; set; }
    public Guid BuyerUserId { get; set; }
    public User? BuyerUser { get; set; }
    public Guid SellerUserId { get; set; }
    public string BuyerOrganization { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string InquiryScope { get; set; } = string.Empty;
    public RfqStatus Status { get; set; } = RfqStatus.Submitted;
    public string? SellerResponse { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}
