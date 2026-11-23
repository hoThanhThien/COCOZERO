using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CocoCarbon.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AuditLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ActorUserId = table.Column<Guid>(type: "TEXT", nullable: true),
                    ActorEmail = table.Column<string>(type: "TEXT", nullable: false),
                    Action = table.Column<string>(type: "TEXT", nullable: false),
                    EntityType = table.Column<string>(type: "TEXT", nullable: false),
                    EntityId = table.Column<string>(type: "TEXT", nullable: false),
                    BeforeHash = table.Column<string>(type: "TEXT", nullable: true),
                    AfterHash = table.Column<string>(type: "TEXT", nullable: true),
                    DetailsJson = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ResearchSources",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Code = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    Title = table.Column<string>(type: "TEXT", nullable: false),
                    Authors = table.Column<string>(type: "TEXT", nullable: false),
                    Year = table.Column<int>(type: "INTEGER", nullable: false),
                    Publication = table.Column<string>(type: "TEXT", nullable: false),
                    ReferenceUrl = table.Column<string>(type: "TEXT", nullable: false),
                    StudyLocation = table.Column<string>(type: "TEXT", nullable: false),
                    SampleDesignSummary = table.Column<string>(type: "TEXT", nullable: false),
                    Limitations = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ResearchSources", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Roles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Code = table.Column<int>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Roles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    FirebaseUid = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    Email = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    DisplayName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Phone = table.Column<string>(type: "TEXT", nullable: true),
                    Status = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<long>(type: "INTEGER", nullable: false),
                    UpdatedAt = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CarbonResearchFactors",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Code = table.Column<string>(type: "TEXT", nullable: false),
                    Variety = table.Column<int>(type: "INTEGER", nullable: false),
                    VarietyDisplayName = table.Column<string>(type: "TEXT", nullable: false),
                    TreeAgeYears = table.Column<int>(type: "INTEGER", nullable: false),
                    ValueTCO2PerHa = table.Column<decimal>(type: "TEXT", precision: 18, scale: 4, nullable: false),
                    Unit = table.Column<string>(type: "TEXT", nullable: false),
                    SourceId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Version = table.Column<string>(type: "TEXT", nullable: false),
                    ValidFrom = table.Column<long>(type: "INTEGER", nullable: false),
                    ApprovedBy = table.Column<string>(type: "TEXT", nullable: false),
                    ApplicabilityNotes = table.Column<string>(type: "TEXT", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CarbonResearchFactors", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CarbonResearchFactors_ResearchSources_SourceId",
                        column: x => x.SourceId,
                        principalTable: "ResearchSources",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ConsentRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ConsentType = table.Column<int>(type: "INTEGER", nullable: false),
                    Granted = table.Column<bool>(type: "INTEGER", nullable: false),
                    PolicyVersion = table.Column<string>(type: "TEXT", nullable: false),
                    TextShown = table.Column<string>(type: "TEXT", nullable: false),
                    RecordedAt = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConsentRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ConsentRecords_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Cooperatives",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Code = table.Column<string>(type: "TEXT", nullable: false),
                    Province = table.Column<string>(type: "TEXT", nullable: false),
                    Commune = table.Column<string>(type: "TEXT", nullable: false),
                    HistoricalRegion = table.Column<string>(type: "TEXT", nullable: false),
                    ManagerUserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Status = table.Column<string>(type: "TEXT", nullable: false),
                    DataOriginTag = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Cooperatives", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Cooperatives_Users_ManagerUserId",
                        column: x => x.ManagerUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "UserRoles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    RoleId = table.Column<Guid>(type: "TEXT", nullable: false),
                    RoleCode = table.Column<int>(type: "INTEGER", nullable: false),
                    ScopeType = table.Column<string>(type: "TEXT", nullable: false),
                    ScopeId = table.Column<Guid>(type: "TEXT", nullable: true),
                    ApprovalStatus = table.Column<string>(type: "TEXT", nullable: false),
                    ApprovedByUserId = table.Column<Guid>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserRoles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserRoles_Roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserRoles_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Farms",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    OwnerUserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CooperativeId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Province = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    Commune = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    HistoricalRegion = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    AreaHa = table.Column<decimal>(type: "TEXT", precision: 18, scale: 4, nullable: false),
                    TreeCount = table.Column<int>(type: "INTEGER", nullable: false),
                    Variety = table.Column<int>(type: "INTEGER", nullable: false),
                    AgeYears = table.Column<int>(type: "INTEGER", nullable: false),
                    Latitude = table.Column<double>(type: "REAL", nullable: true),
                    Longitude = table.Column<double>(type: "REAL", nullable: true),
                    LocationGeoJson = table.Column<string>(type: "TEXT", nullable: true),
                    CareHistoryJson = table.Column<string>(type: "TEXT", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    DataOriginTag = table.Column<int>(type: "INTEGER", nullable: false),
                    HasLocationConsent = table.Column<bool>(type: "INTEGER", nullable: false),
                    HasPhotoConsent = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<long>(type: "INTEGER", nullable: false),
                    UpdatedAt = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Farms", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Farms_Cooperatives_CooperativeId",
                        column: x => x.CooperativeId,
                        principalTable: "Cooperatives",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Farms_Users_OwnerUserId",
                        column: x => x.OwnerUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Projects",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    DeveloperId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CooperativeId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Code = table.Column<string>(type: "TEXT", nullable: false),
                    ProjectType = table.Column<string>(type: "TEXT", nullable: false),
                    Province = table.Column<string>(type: "TEXT", nullable: false),
                    HistoricalRegion = table.Column<string>(type: "TEXT", nullable: false),
                    DeclaredTotalAreaHa = table.Column<decimal>(type: "TEXT", precision: 18, scale: 4, nullable: false),
                    IntendedMethodology = table.Column<string>(type: "TEXT", nullable: false),
                    MethodologyNotes = table.Column<string>(type: "TEXT", nullable: false),
                    BaselineSummary = table.Column<string>(type: "TEXT", nullable: false),
                    MonitoringPlanSummary = table.Column<string>(type: "TEXT", nullable: false),
                    ReadinessScore = table.Column<decimal>(type: "TEXT", precision: 5, scale: 2, nullable: false),
                    ReadinessLabel = table.Column<string>(type: "TEXT", nullable: false),
                    VerificationStatus = table.Column<string>(type: "TEXT", nullable: false),
                    ProjectStatus = table.Column<int>(type: "INTEGER", nullable: false),
                    DataOriginTag = table.Column<int>(type: "INTEGER", nullable: false),
                    Disclaimer = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<long>(type: "INTEGER", nullable: false),
                    UpdatedAt = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Projects", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Projects_Cooperatives_CooperativeId",
                        column: x => x.CooperativeId,
                        principalTable: "Cooperatives",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Projects_Users_DeveloperId",
                        column: x => x.DeveloperId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CarbonCalculations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    FarmId = table.Column<Guid>(type: "TEXT", nullable: true),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    InputSnapshotJson = table.Column<string>(type: "TEXT", nullable: false),
                    FactorId = table.Column<Guid>(type: "TEXT", nullable: false),
                    FactorVersion = table.Column<string>(type: "TEXT", nullable: false),
                    FormulaVersion = table.Column<string>(type: "TEXT", nullable: false),
                    EstimatedCO2EquivalentT = table.Column<decimal>(type: "TEXT", precision: 18, scale: 4, nullable: false),
                    ObservedTreeDensityPerHa = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    DensityContextNote = table.Column<string>(type: "TEXT", nullable: false),
                    OptionalArithmeticAllocationKgPerTree = table.Column<decimal>(type: "TEXT", precision: 18, scale: 4, nullable: true),
                    ArithmeticAllocationLabel = table.Column<string>(type: "TEXT", nullable: true),
                    SourceCitation = table.Column<string>(type: "TEXT", nullable: false),
                    Disclaimer = table.Column<string>(type: "TEXT", nullable: false),
                    DataOriginTag = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CarbonCalculations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CarbonCalculations_CarbonResearchFactors_FactorId",
                        column: x => x.FactorId,
                        principalTable: "CarbonResearchFactors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CarbonCalculations_Farms_FarmId",
                        column: x => x.FarmId,
                        principalTable: "Farms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_CarbonCalculations_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CooperativeMembers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    CooperativeId = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    FarmId = table.Column<Guid>(type: "TEXT", nullable: true),
                    IsDataAuthorized = table.Column<bool>(type: "INTEGER", nullable: false),
                    ParticipationStatus = table.Column<int>(type: "INTEGER", nullable: false),
                    JoinedAt = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CooperativeMembers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CooperativeMembers_Cooperatives_CooperativeId",
                        column: x => x.CooperativeId,
                        principalTable: "Cooperatives",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CooperativeMembers_Farms_FarmId",
                        column: x => x.FarmId,
                        principalTable: "Farms",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_CooperativeMembers_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FarmFiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    FarmId = table.Column<Guid>(type: "TEXT", nullable: false),
                    UploadedByUserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Category = table.Column<int>(type: "INTEGER", nullable: false),
                    FileKey = table.Column<string>(type: "TEXT", nullable: false),
                    FileName = table.Column<string>(type: "TEXT", maxLength: 260, nullable: false),
                    MimeType = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    SizeBytes = table.Column<long>(type: "INTEGER", nullable: false),
                    Sha256Hash = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    Version = table.Column<int>(type: "INTEGER", nullable: false),
                    Visibility = table.Column<int>(type: "INTEGER", nullable: false),
                    DataOriginTag = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FarmFiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FarmFiles_Farms_FarmId",
                        column: x => x.FarmId,
                        principalTable: "Farms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FarmObservations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    FarmId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ObservedAt = table.Column<long>(type: "INTEGER", nullable: false),
                    TreeAgeYears = table.Column<int>(type: "INTEGER", nullable: false),
                    DbhCm = table.Column<decimal>(type: "TEXT", precision: 10, scale: 2, nullable: true),
                    HeightM = table.Column<decimal>(type: "TEXT", precision: 10, scale: 2, nullable: true),
                    HealthStatus = table.Column<string>(type: "TEXT", nullable: false),
                    PracticeDataJson = table.Column<string>(type: "TEXT", nullable: false),
                    RecordedByUserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    DataOriginTag = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FarmObservations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FarmObservations_Farms_FarmId",
                        column: x => x.FarmId,
                        principalTable: "Farms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FarmPlots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    FarmId = table.Column<Guid>(type: "TEXT", nullable: false),
                    PolygonGeoJson = table.Column<string>(type: "TEXT", nullable: false),
                    AreaHa = table.Column<decimal>(type: "TEXT", precision: 18, scale: 4, nullable: false),
                    LandUse = table.Column<string>(type: "TEXT", nullable: false),
                    BoundarySource = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FarmPlots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FarmPlots_Farms_FarmId",
                        column: x => x.FarmId,
                        principalTable: "Farms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CreditLots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ProjectId = table.Column<Guid>(type: "TEXT", nullable: false),
                    RegistryName = table.Column<string>(type: "TEXT", nullable: false),
                    SerialRange = table.Column<string>(type: "TEXT", nullable: true),
                    VintageYear = table.Column<int>(type: "INTEGER", nullable: true),
                    QuantityTCO2e = table.Column<decimal>(type: "TEXT", precision: 18, scale: 4, nullable: false),
                    IssuanceStatus = table.Column<string>(type: "TEXT", nullable: false),
                    VerificationEvidenceRef = table.Column<string>(type: "TEXT", nullable: true),
                    HasOfficialRegistryEvidence = table.Column<bool>(type: "INTEGER", nullable: false),
                    DataOriginTag = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CreditLots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CreditLots_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Listings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ProjectId = table.Column<Guid>(type: "TEXT", nullable: false),
                    SellerUserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Title = table.Column<string>(type: "TEXT", nullable: false),
                    Summary = table.Column<string>(type: "TEXT", nullable: false),
                    StageNote = table.Column<string>(type: "TEXT", nullable: false),
                    IsPublic = table.Column<bool>(type: "INTEGER", nullable: false),
                    DataOriginTag = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Listings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Listings_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProjectFarms",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ProjectId = table.Column<Guid>(type: "TEXT", nullable: false),
                    FarmId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ParticipationStatus = table.Column<int>(type: "INTEGER", nullable: false),
                    HasFarmerConsent = table.Column<bool>(type: "INTEGER", nullable: false),
                    JoinedAt = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectFarms", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProjectFarms_Farms_FarmId",
                        column: x => x.FarmId,
                        principalTable: "Farms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProjectFarms_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Rfqs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ProjectId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ListingId = table.Column<Guid>(type: "TEXT", nullable: true),
                    BuyerUserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    SellerUserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    BuyerOrganization = table.Column<string>(type: "TEXT", nullable: false),
                    Message = table.Column<string>(type: "TEXT", nullable: false),
                    InquiryScope = table.Column<string>(type: "TEXT", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    SellerResponse = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<long>(type: "INTEGER", nullable: false),
                    UpdatedAt = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Rfqs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Rfqs_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Rfqs_Users_BuyerUserId",
                        column: x => x.BuyerUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AiAnalyses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    FileId = table.Column<Guid>(type: "TEXT", nullable: true),
                    FarmId = table.Column<Guid>(type: "TEXT", nullable: true),
                    ProjectId = table.Column<Guid>(type: "TEXT", nullable: true),
                    RequestedByUserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Provider = table.Column<string>(type: "TEXT", nullable: false),
                    ModelName = table.Column<string>(type: "TEXT", nullable: false),
                    PromptVersion = table.Column<string>(type: "TEXT", nullable: false),
                    Status = table.Column<string>(type: "TEXT", nullable: false),
                    IsFallbackResult = table.Column<bool>(type: "INTEGER", nullable: false),
                    StructuredOutputJson = table.Column<string>(type: "TEXT", nullable: false),
                    EvidenceJson = table.Column<string>(type: "TEXT", nullable: false),
                    ReadinessRubricJson = table.Column<string>(type: "TEXT", nullable: false),
                    ReviewerRequired = table.Column<bool>(type: "INTEGER", nullable: false),
                    ReviewStatus = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AiAnalyses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AiAnalyses_FarmFiles_FileId",
                        column: x => x.FileId,
                        principalTable: "FarmFiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "ReviewerDecisions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    AiAnalysisId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ReviewerUserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Decision = table.Column<int>(type: "INTEGER", nullable: false),
                    EditedOutputJson = table.Column<string>(type: "TEXT", nullable: true),
                    Comment = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReviewerDecisions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReviewerDecisions_AiAnalyses_AiAnalysisId",
                        column: x => x.AiAnalysisId,
                        principalTable: "AiAnalyses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ReviewerDecisions_Users_ReviewerUserId",
                        column: x => x.ReviewerUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AiAnalyses_FileId",
                table: "AiAnalyses",
                column: "FileId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_CreatedAt",
                table: "AuditLogs",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_EntityType_EntityId",
                table: "AuditLogs",
                columns: new[] { "EntityType", "EntityId" });

            migrationBuilder.CreateIndex(
                name: "IX_CarbonCalculations_FactorId",
                table: "CarbonCalculations",
                column: "FactorId");

            migrationBuilder.CreateIndex(
                name: "IX_CarbonCalculations_FarmId",
                table: "CarbonCalculations",
                column: "FarmId");

            migrationBuilder.CreateIndex(
                name: "IX_CarbonCalculations_UserId",
                table: "CarbonCalculations",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_CarbonResearchFactors_Code_Version",
                table: "CarbonResearchFactors",
                columns: new[] { "Code", "Version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CarbonResearchFactors_SourceId",
                table: "CarbonResearchFactors",
                column: "SourceId");

            migrationBuilder.CreateIndex(
                name: "IX_ConsentRecords_UserId",
                table: "ConsentRecords",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_CooperativeMembers_CooperativeId",
                table: "CooperativeMembers",
                column: "CooperativeId");

            migrationBuilder.CreateIndex(
                name: "IX_CooperativeMembers_FarmId",
                table: "CooperativeMembers",
                column: "FarmId");

            migrationBuilder.CreateIndex(
                name: "IX_CooperativeMembers_UserId",
                table: "CooperativeMembers",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Cooperatives_Code",
                table: "Cooperatives",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Cooperatives_ManagerUserId",
                table: "Cooperatives",
                column: "ManagerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_CreditLots_ProjectId",
                table: "CreditLots",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_FarmFiles_FarmId",
                table: "FarmFiles",
                column: "FarmId");

            migrationBuilder.CreateIndex(
                name: "IX_FarmFiles_FileKey",
                table: "FarmFiles",
                column: "FileKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FarmFiles_Sha256Hash",
                table: "FarmFiles",
                column: "Sha256Hash");

            migrationBuilder.CreateIndex(
                name: "IX_FarmObservations_FarmId",
                table: "FarmObservations",
                column: "FarmId");

            migrationBuilder.CreateIndex(
                name: "IX_FarmPlots_FarmId",
                table: "FarmPlots",
                column: "FarmId");

            migrationBuilder.CreateIndex(
                name: "IX_Farms_CooperativeId",
                table: "Farms",
                column: "CooperativeId");

            migrationBuilder.CreateIndex(
                name: "IX_Farms_OwnerUserId",
                table: "Farms",
                column: "OwnerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Listings_ProjectId",
                table: "Listings",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectFarms_FarmId",
                table: "ProjectFarms",
                column: "FarmId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectFarms_ProjectId_FarmId",
                table: "ProjectFarms",
                columns: new[] { "ProjectId", "FarmId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Projects_Code",
                table: "Projects",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Projects_CooperativeId",
                table: "Projects",
                column: "CooperativeId");

            migrationBuilder.CreateIndex(
                name: "IX_Projects_DeveloperId",
                table: "Projects",
                column: "DeveloperId");

            migrationBuilder.CreateIndex(
                name: "IX_ResearchSources_Code",
                table: "ResearchSources",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReviewerDecisions_AiAnalysisId",
                table: "ReviewerDecisions",
                column: "AiAnalysisId");

            migrationBuilder.CreateIndex(
                name: "IX_ReviewerDecisions_ReviewerUserId",
                table: "ReviewerDecisions",
                column: "ReviewerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Rfqs_BuyerUserId",
                table: "Rfqs",
                column: "BuyerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Rfqs_ProjectId",
                table: "Rfqs",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_Roles_Code",
                table: "Roles",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserRoles_RoleId",
                table: "UserRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_UserRoles_UserId_RoleCode_ScopeType_ScopeId",
                table: "UserRoles",
                columns: new[] { "UserId", "RoleCode", "ScopeType", "ScopeId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email");

            migrationBuilder.CreateIndex(
                name: "IX_Users_FirebaseUid",
                table: "Users",
                column: "FirebaseUid",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuditLogs");

            migrationBuilder.DropTable(
                name: "CarbonCalculations");

            migrationBuilder.DropTable(
                name: "ConsentRecords");

            migrationBuilder.DropTable(
                name: "CooperativeMembers");

            migrationBuilder.DropTable(
                name: "CreditLots");

            migrationBuilder.DropTable(
                name: "FarmObservations");

            migrationBuilder.DropTable(
                name: "FarmPlots");

            migrationBuilder.DropTable(
                name: "Listings");

            migrationBuilder.DropTable(
                name: "ProjectFarms");

            migrationBuilder.DropTable(
                name: "ReviewerDecisions");

            migrationBuilder.DropTable(
                name: "Rfqs");

            migrationBuilder.DropTable(
                name: "UserRoles");

            migrationBuilder.DropTable(
                name: "CarbonResearchFactors");

            migrationBuilder.DropTable(
                name: "AiAnalyses");

            migrationBuilder.DropTable(
                name: "Projects");

            migrationBuilder.DropTable(
                name: "Roles");

            migrationBuilder.DropTable(
                name: "ResearchSources");

            migrationBuilder.DropTable(
                name: "FarmFiles");

            migrationBuilder.DropTable(
                name: "Farms");

            migrationBuilder.DropTable(
                name: "Cooperatives");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
