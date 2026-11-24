using CocoCarbon.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace CocoCarbon.Infrastructure.Persistence;

public class CocoCarbonDbContext(DbContextOptions<CocoCarbonDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<ConsentRecord> ConsentRecords => Set<ConsentRecord>();
    public DbSet<Farm> Farms => Set<Farm>();
    public DbSet<FarmPlot> FarmPlots => Set<FarmPlot>();
    public DbSet<FarmObservation> FarmObservations => Set<FarmObservation>();
    public DbSet<FarmFile> FarmFiles => Set<FarmFile>();
    public DbSet<ResearchSource> ResearchSources => Set<ResearchSource>();
    public DbSet<CarbonResearchFactor> CarbonResearchFactors => Set<CarbonResearchFactor>();
    public DbSet<CarbonCalculation> CarbonCalculations => Set<CarbonCalculation>();
    public DbSet<Cooperative> Cooperatives => Set<Cooperative>();
    public DbSet<CooperativeMember> CooperativeMembers => Set<CooperativeMember>();
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<ProjectFarm> ProjectFarms => Set<ProjectFarm>();
    public DbSet<CreditLot> CreditLots => Set<CreditLot>();
    public DbSet<Listing> Listings => Set<Listing>();
    public DbSet<Rfq> Rfqs => Set<Rfq>();
    public DbSet<AiAnalysis> AiAnalyses => Set<AiAnalysis>();
    public DbSet<ReviewerDecision> ReviewerDecisions => Set<ReviewerDecision>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<FarmFinancialEntry> FarmFinancialEntries => Set<FarmFinancialEntry>();
    public DbSet<DataDeletionRequest> DataDeletionRequests => Set<DataDeletionRequest>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(e =>
        {
            e.HasIndex(x => x.FirebaseUid).IsUnique();
            e.HasIndex(x => x.Email);
            e.Property(x => x.FirebaseUid).HasMaxLength(128).IsRequired();
            e.Property(x => x.Email).HasMaxLength(256).IsRequired();
            e.Property(x => x.DisplayName).HasMaxLength(200).IsRequired();
        });

        modelBuilder.Entity<Role>(e =>
        {
            e.HasIndex(x => x.Code).IsUnique();
            e.Property(x => x.Name).HasMaxLength(100).IsRequired();
        });

        modelBuilder.Entity<UserRole>(e =>
        {
            e.HasIndex(x => new { x.UserId, x.RoleCode, x.ScopeType, x.ScopeId }).IsUnique();
            e.HasOne(x => x.User).WithMany(u => u.UserRoles).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Role).WithMany().HasForeignKey(x => x.RoleId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ConsentRecord>(e =>
        {
            e.HasOne(x => x.User).WithMany(u => u.ConsentRecords).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Farm>(e =>
        {
            e.HasIndex(x => x.OwnerUserId);
            e.HasIndex(x => x.CooperativeId);
            e.Property(x => x.AreaHa).HasPrecision(18, 4);
            e.Property(x => x.Name).HasMaxLength(200).IsRequired();
            e.Property(x => x.Province).HasMaxLength(120).IsRequired();
            e.Property(x => x.Commune).HasMaxLength(120).IsRequired();
            e.Property(x => x.HistoricalRegion).HasMaxLength(120).IsRequired();
            e.HasOne(x => x.OwnerUser).WithMany().HasForeignKey(x => x.OwnerUserId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Cooperative).WithMany().HasForeignKey(x => x.CooperativeId).OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<FarmPlot>(e =>
        {
            e.Property(x => x.AreaHa).HasPrecision(18, 4);
            e.HasOne(x => x.Farm).WithMany(f => f.Plots).HasForeignKey(x => x.FarmId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<FarmObservation>(e =>
        {
            e.Property(x => x.DbhCm).HasPrecision(10, 2);
            e.Property(x => x.HeightM).HasPrecision(10, 2);
            e.HasOne(x => x.Farm).WithMany(f => f.Observations).HasForeignKey(x => x.FarmId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<FarmFile>(e =>
        {
            e.HasIndex(x => x.Sha256Hash);
            e.HasIndex(x => x.FileKey).IsUnique();
            e.Property(x => x.FileName).HasMaxLength(260).IsRequired();
            e.Property(x => x.MimeType).HasMaxLength(100).IsRequired();
            e.Property(x => x.Sha256Hash).HasMaxLength(64).IsRequired();
            e.HasOne(x => x.Farm).WithMany(f => f.Files).HasForeignKey(x => x.FarmId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ResearchSource>(e =>
        {
            e.HasIndex(x => x.Code).IsUnique();
            e.Property(x => x.Code).HasMaxLength(32).IsRequired();
        });

        modelBuilder.Entity<CarbonResearchFactor>(e =>
        {
            e.HasIndex(x => new { x.Code, x.Version }).IsUnique();
            e.Property(x => x.ValueTCO2PerHa).HasPrecision(18, 4);
            e.HasOne(x => x.Source).WithMany().HasForeignKey(x => x.SourceId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<CarbonCalculation>(e =>
        {
            e.Property(x => x.EstimatedCO2EquivalentT).HasPrecision(18, 4);
            e.Property(x => x.ObservedTreeDensityPerHa).HasPrecision(18, 2);
            e.Property(x => x.OptionalArithmeticAllocationKgPerTree).HasPrecision(18, 4);
            e.HasOne(x => x.Farm).WithMany(f => f.Calculations).HasForeignKey(x => x.FarmId).OnDelete(DeleteBehavior.SetNull);
            e.HasOne(x => x.Factor).WithMany().HasForeignKey(x => x.FactorId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Cooperative>(e =>
        {
            e.HasIndex(x => x.Code).IsUnique();
            e.HasOne(x => x.ManagerUser).WithMany().HasForeignKey(x => x.ManagerUserId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<CooperativeMember>(e =>
        {
            e.HasOne(x => x.Cooperative).WithMany(c => c.Members).HasForeignKey(x => x.CooperativeId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Project>(e =>
        {
            e.HasIndex(x => x.Code).IsUnique();
            e.Property(x => x.DeclaredTotalAreaHa).HasPrecision(18, 4);
            e.Property(x => x.ReadinessScore).HasPrecision(5, 2);
            e.HasOne(x => x.Developer).WithMany().HasForeignKey(x => x.DeveloperId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Cooperative).WithMany().HasForeignKey(x => x.CooperativeId).OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<ProjectFarm>(e =>
        {
            e.HasIndex(x => new { x.ProjectId, x.FarmId }).IsUnique();
            e.HasOne(x => x.Project).WithMany(p => p.ProjectFarms).HasForeignKey(x => x.ProjectId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Farm).WithMany().HasForeignKey(x => x.FarmId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<CreditLot>(e =>
        {
            e.Property(x => x.QuantityTCO2e).HasPrecision(18, 4);
            e.HasOne(x => x.Project).WithMany(p => p.CreditLots).HasForeignKey(x => x.ProjectId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Listing>(e =>
        {
            e.HasOne(x => x.Project).WithMany(p => p.Listings).HasForeignKey(x => x.ProjectId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Rfq>(e =>
        {
            e.HasOne(x => x.Project).WithMany().HasForeignKey(x => x.ProjectId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.BuyerUser).WithMany().HasForeignKey(x => x.BuyerUserId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<AiAnalysis>(e =>
        {
            e.HasOne(x => x.File).WithMany().HasForeignKey(x => x.FileId).OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<ReviewerDecision>(e =>
        {
            e.HasOne(x => x.AiAnalysis).WithMany(a => a.ReviewerDecisions).HasForeignKey(x => x.AiAnalysisId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.ReviewerUser).WithMany().HasForeignKey(x => x.ReviewerUserId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<AuditLog>(e =>
        {
            e.HasIndex(x => x.CreatedAt);
            e.HasIndex(x => new { x.EntityType, x.EntityId });
        });

        modelBuilder.Entity<FarmFinancialEntry>(e =>
        {
            e.Property(x => x.AmountVnd).HasPrecision(18, 2);
            e.Property(x => x.Category).HasMaxLength(150).IsRequired();
            e.HasOne(x => x.Farm).WithMany().HasForeignKey(x => x.FarmId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<DataDeletionRequest>(e =>
        {
            e.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        if (Database.IsSqlite())
        {
            var dtoConverter = new DateTimeOffsetToBinaryConverter();
            var nullableDtoConverter = new ValueConverter<DateTimeOffset?, long?>(
                v => v.HasValue ? v.Value.UtcTicks : null,
                v => v.HasValue ? new DateTimeOffset(v.Value, TimeSpan.Zero) : null);

            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                foreach (var property in entityType.GetProperties())
                {
                    if (property.ClrType == typeof(DateTimeOffset))
                    {
                        property.SetValueConverter(dtoConverter);
                    }
                    else if (property.ClrType == typeof(DateTimeOffset?))
                    {
                        property.SetValueConverter(nullableDtoConverter);
                    }
                }
            }
        }
    }
}
