using CocoCarbon.Domain.Constants;
using CocoCarbon.Domain.Enums;

namespace CocoCarbon.Domain.Entities;

public class Farm
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid OwnerUserId { get; set; }
    public User? OwnerUser { get; set; }
    public Guid? CooperativeId { get; set; }
    public Cooperative? Cooperative { get; set; }

    public string Name { get; set; } = string.Empty;
    public string Province { get; set; } = ScientificConstants.CurrentProvinceDefault;
    public string Commune { get; set; } = string.Empty;
    public string HistoricalRegion { get; set; } = ScientificConstants.HistoricalRegionBenTre;

    public decimal AreaHa { get; set; }
    public int TreeCount { get; set; }
    public CoconutVariety Variety { get; set; }
    public int AgeYears { get; set; }

    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public string? LocationGeoJson { get; set; }
    public string CareHistoryJson { get; set; } = "{}";

    public FarmStatus Status { get; set; } = FarmStatus.Draft;
    public DataOriginTag DataOriginTag { get; set; } = DataOriginTag.UserDeclared;
    public bool HasLocationConsent { get; set; }
    public bool HasPhotoConsent { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    public ICollection<FarmPlot> Plots { get; set; } = new List<FarmPlot>();
    public ICollection<FarmObservation> Observations { get; set; } = new List<FarmObservation>();
    public ICollection<FarmFile> Files { get; set; } = new List<FarmFile>();
    public ICollection<CarbonCalculation> Calculations { get; set; } = new List<CarbonCalculation>();
}

public class FarmPlot
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid FarmId { get; set; }
    public Farm? Farm { get; set; }
    public string PolygonGeoJson { get; set; } = string.Empty;
    public decimal AreaHa { get; set; }
    public string LandUse { get; set; } = "Coconut orchard";
    public string BoundarySource { get; set; } = "UserDeclared";
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public class FarmObservation
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid FarmId { get; set; }
    public Farm? Farm { get; set; }
    public DateTimeOffset ObservedAt { get; set; } = DateTimeOffset.UtcNow;
    public int TreeAgeYears { get; set; }
    public decimal? DbhCm { get; set; }
    public decimal? HeightM { get; set; }
    public string HealthStatus { get; set; } = "Healthy";
    public string PracticeDataJson { get; set; } = "{}";
    public Guid RecordedByUserId { get; set; }
    public DataOriginTag DataOriginTag { get; set; } = DataOriginTag.UserDeclared;
}

public class FarmFile
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid FarmId { get; set; }
    public Farm? Farm { get; set; }
    public Guid UploadedByUserId { get; set; }
    public FileCategory Category { get; set; } = FileCategory.LandDocument;
    public string FileKey { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string MimeType { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public string Sha256Hash { get; set; } = string.Empty;
    public int Version { get; set; } = 1;
    public FileVisibility Visibility { get; set; } = FileVisibility.Private;
    public DataOriginTag DataOriginTag { get; set; } = DataOriginTag.UserDeclared;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
