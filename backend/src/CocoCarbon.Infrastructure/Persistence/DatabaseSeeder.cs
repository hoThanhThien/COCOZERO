using CocoCarbon.Domain.Constants;
using CocoCarbon.Domain.Entities;
using CocoCarbon.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CocoCarbon.Infrastructure.Persistence;

public static class DatabaseSeeder
{
    public static async Task SeedAsync(
        CocoCarbonDbContext db,
        bool seedDemoData = true,
        CancellationToken cancellationToken = default)
    {
        // 1. Roles
        var existingRoles = await db.Roles.ToListAsync(cancellationToken);
        var roleDefinitions = new (RoleCode Code, string Name, string Desc)[]
        {
            (RoleCode.Farmer, "Nông hộ (Farmer)", "Tạo hồ sơ vườn dừa, upload ảnh/giấy tờ, xem tính toán tham khảo."),
            (RoleCode.CooperativeManager, "Quản lý HTX (Cooperative Manager)", "Tổng hợp vùng nguyên liệu, theo dõi thành viên/vườn được ủy quyền."),
            (RoleCode.ProjectDeveloper, "Đơn vị phát triển dự án (Project Developer)", "Quản lý hồ sơ dự án, phương pháp luận, readiness và phản hồi RFQ."),
            (RoleCode.Buyer, "Doanh nghiệp tìm hiểu / Mua (Buyer)", "Tìm hiểu dự án, lọc tiêu chí và gửi yêu cầu báo giá (RFQ)."),
            (RoleCode.Reviewer, "Chuyên viên rà soát nội bộ (Reviewer)", "Rà soát hồ sơ và cảnh báo AI; không thay thế VVB độc lập."),
            (RoleCode.Admin, "Quản trị hệ thống (Admin)", "Quản lý vai trò, hệ số nghiên cứu, phiên bản công thức và audit log.")
        };

        foreach (var def in roleDefinitions)
        {
            if (existingRoles.All(r => r.Code != def.Code))
            {
                var role = new Role { Code = def.Code, Name = def.Name, Description = def.Desc };
                db.Roles.Add(role);
                existingRoles.Add(role);
            }
        }
        await db.SaveChangesAsync(cancellationToken);

        // 2. Scientific Sources ([R1]-[R8])
        var existingSources = await db.ResearchSources.ToListAsync(cancellationToken);
        ResearchSource EnsureSource(ResearchSource candidate)
        {
            var found = existingSources.FirstOrDefault(s => s.Code == candidate.Code);
            if (found is not null) return found;
            db.ResearchSources.Add(candidate);
            existingSources.Add(candidate);
            return candidate;
        }

        var r1 = EnsureSource(new ResearchSource
        {
            Code = "R1",
            Title = "Ước lượng khả năng hấp thụ CO2 của cây dừa qua sinh khối tại huyện Giồng Trôm, tỉnh Bến Tre",
            Authors = "Nguyễn Thị Thanh Trúc & Lê Anh Tuấn",
            Year = 2015,
            Publication = "Tạp chí Khoa học Đại học Cần Thơ, Số chuyên đề Môi trường và Biến đổi khí hậu, tr. 193-199",
            ReferenceUrl = "https://ctujsvn.ctu.edu.vn/index.php/ctujsvn/article/view/2264",
            StudyLocation = "Huyện Giồng Trôm, vùng dừa Bến Tre (nay thuộc tỉnh Vĩnh Long)",
            SampleDesignSummary = "4 ô tiêu chuẩn 10m x 10m; 2 nhóm giống (cao/thấp) x 2 cấp tuổi (4 và 10 năm); 3 cây/ô (12 cây mẫu); sấy mẫu ở 105°C; ước tính rễ bằng R/S = 1/5; CF = 0.47-0.50 (IPCC 2003).",
            Limitations = "Không theo dõi tăng trưởng sinh khối qua 1 năm (không phải tCO2/ha/năm); rễ không đào trực tiếp; giới hạn ở 12 cây mẫu, điều kiện và thời điểm khảo sát tại Giồng Trôm."
        });

        EnsureSource(new ResearchSource
        {
            Code = "R2",
            Title = "Thực trạng canh tác dừa tại tỉnh Bến Tre",
            Authors = "Cục Đổi mới sáng tạo, Chuyển đổi xanh và Khuyến công",
            Year = 2024,
            Publication = "Báo cáo khảo sát 02/07/2024",
            ReferenceUrl = "https://khuyennongvn.gov.vn/",
            StudyLocation = "5 huyện Châu Thành, Giồng Trôm, Ba Tri, Thạnh Phú, Bình Đại (150 hộ)",
            SampleDesignSummary = "Khảo sát 150 hộ trồng dừa; ghi nhận mật độ trồng 160-240 cây/ha và thông tin thực hành canh tác.",
            Limitations = "Dữ liệu mô tả thực trạng canh tác; không phải ngưỡng chứng nhận carbon và không dùng hiệu chỉnh ngược hệ số CTU 2015."
        });

        EnsureSource(new ResearchSource
        {
            Code = "R3",
            Title = "2006 IPCC Guidelines for National Greenhouse Gas Inventories, Vol. 4, Ch. 5 - Cropland, Table 5.3",
            Authors = "IPCC",
            Year = 2006,
            Publication = "IPCC Guidelines",
            ReferenceUrl = "https://www.ipcc-nggip.iges.or.jp/public/2006gl/vol4.html",
            StudyLocation = "Southeast Asia (Tier 1 default)",
            SampleDesignSummary = "Ghi nhận Coconut - SE Asia: 196 tonnes/ha above-ground dry biomass.",
            Limitations = "Là sinh khối khô trên mặt đất (tấn chất khô/ha), KHÔNG phải tấn CO2/ha và KHÔNG trộn với hệ số CTU 2015."
        });

        EnsureSource(new ResearchSource
        {
            Code = "R4",
            Title = "2019 Refinement to the 2006 IPCC Guidelines, Vol. 4, Ch. 5 - Cropland",
            Authors = "IPCC",
            Year = 2019,
            Publication = "IPCC Refinement",
            ReferenceUrl = "https://www.ipcc-nggip.iges.or.jp/public/2019rf/vol4.html",
            StudyLocation = "Global methodological guidance",
            SampleDesignSummary = "Đề cập dừa là perennial woody cropland; hướng dẫn gain-loss, stock-difference và Tier 1-3.",
            Limitations = "Table 5.3 cập nhật không có hàng hệ số riêng cho dừa; Tier 3 cần hiệu chuẩn và xác nhận độc lập."
        });

        EnsureSource(new ResearchSource
        {
            Code = "R5",
            Title = "VM0042 Improved Agricultural Land Management, v2.2 (with 11 June 2026 Clarifications)",
            Authors = "Verra",
            Year = 2025,
            Publication = "Verra VCS Program",
            ReferenceUrl = "https://verra.org/methodologies/vm0042-methodology-for-improved-agricultural-land-management-v2-2/",
            StudyLocation = "Global VCS scope",
            SampleDesignSummary = "Methodology cải thiện quản lý đất nông nghiệp, giảm phát thải và SOC trong phạm vi phù hợp.",
            Limitations = "Không tự động áp dụng cho sinh khối thân dừa nếu chưa kiểm tra đầy đủ scope, baseline, additionality, leakage và verification."
        });

        EnsureSource(new ResearchSource
        {
            Code = "R7",
            Title = "Soil Organic Carbon Model Requirements and Guidelines, v1.0",
            Authors = "Gold Standard",
            Year = 2025,
            Publication = "Gold Standard (08/09/2025)",
            ReferenceUrl = "https://globalgoals.goldstandard.org/",
            StudyLocation = "Global Gold Standard SOC framework",
            SampleDesignSummary = "Hướng dẫn lựa chọn, hiệu chuẩn, validation và đánh giá bất định cho mô hình carbon hữu cơ trong đất (SOC).",
            Limitations = "Chỉ áp dụng cho nhánh SOC, không thay thế phương trình sinh khối cây dừa."
        });

        EnsureSource(new ResearchSource
        {
            Code = "R8",
            Title = "Allometric models for estimating above- and belowground biomass of Cocos nucifera L. at Kisarawe and Mkuranga Districts in Tanzania",
            Authors = "Mugasha & Zahabu",
            Year = 2026,
            Publication = "Agroforestry Systems",
            ReferenceUrl = "https://link.springer.com/",
            StudyLocation = "Tanzania",
            SampleDesignSummary = "Phát triển phương trình allometric dừa theo đường kính và chiều cao tại Tanzania.",
            Limitations = "Không được bê nguyên hệ số Tanzania sang Bến Tre/Vĩnh Long nếu chưa lấy mẫu địa phương và validation độc lập."
        });

        await db.SaveChangesAsync(cancellationToken);

        // 3. Seed EXACTLY 4 approved CTU 2015 CarbonResearchFactors
        if (!await db.CarbonResearchFactors.AnyAsync(cancellationToken))
        {
            var validDate = new DateTimeOffset(2026, 11, 23, 0, 0, 0, TimeSpan.Zero);
            db.CarbonResearchFactors.AddRange(
                new CarbonResearchFactor
                {
                    Code = "CTU2015-TALL-4Y",
                    Variety = CoconutVariety.TallDuaTa,
                    VarietyDisplayName = "Dừa cao - Dừa Ta (Tall)",
                    TreeAgeYears = 4,
                    ValueTCO2PerHa = 24.5180m,
                    Unit = "tCO2/ha (ước tính tham khảo theo sinh khối cấp tuổi 4 năm)",
                    SourceId = r1.Id,
                    Version = ScientificConstants.DefaultFactorVersion,
                    ValidFrom = validDate,
                    ApprovedBy = "CTU 2015 [R1] Seed Verification",
                    ApplicabilityNotes = "Áp dụng tham khảo cấp vườn cho Dừa cao (Dừa Ta) 4 năm tuổi vùng dừa Bến Tre. Không nhân thêm CF 0.47-0.50 hoặc 44/12.",
                    IsActive = true
                },
                new CarbonResearchFactor
                {
                    Code = "CTU2015-TALL-10Y",
                    Variety = CoconutVariety.TallDuaTa,
                    VarietyDisplayName = "Dừa cao - Dừa Ta (Tall)",
                    TreeAgeYears = 10,
                    ValueTCO2PerHa = 75.2436m,
                    Unit = "tCO2/ha (ước tính tham khảo theo sinh khối cấp tuổi 10 năm)",
                    SourceId = r1.Id,
                    Version = ScientificConstants.DefaultFactorVersion,
                    ValidFrom = validDate,
                    ApprovedBy = "CTU 2015 [R1] Seed Verification",
                    ApplicabilityNotes = "Áp dụng tham khảo cấp vườn cho Dừa cao (Dừa Ta) 10 năm tuổi vùng dừa Bến Tre. Không nhân thêm CF 0.47-0.50 hoặc 44/12.",
                    IsActive = true
                },
                new CarbonResearchFactor
                {
                    Code = "CTU2015-SHORT-4Y",
                    Variety = CoconutVariety.ShortXiemXanh,
                    VarietyDisplayName = "Dừa thấp - Xiêm xanh (Short)",
                    TreeAgeYears = 4,
                    ValueTCO2PerHa = 20.4583m,
                    Unit = "tCO2/ha (ước tính tham khảo theo sinh khối cấp tuổi 4 năm)",
                    SourceId = r1.Id,
                    Version = ScientificConstants.DefaultFactorVersion,
                    ValidFrom = validDate,
                    ApprovedBy = "CTU 2015 [R1] Seed Verification",
                    ApplicabilityNotes = "Áp dụng tham khảo cấp vườn cho Dừa thấp (Xiêm xanh) 4 năm tuổi vùng dừa Bến Tre. Không nhân thêm CF 0.47-0.50 hoặc 44/12.",
                    IsActive = true
                },
                new CarbonResearchFactor
                {
                    Code = "CTU2015-SHORT-10Y",
                    Variety = CoconutVariety.ShortXiemXanh,
                    VarietyDisplayName = "Dừa thấp - Xiêm xanh (Short)",
                    TreeAgeYears = 10,
                    ValueTCO2PerHa = 69.9189m,
                    Unit = "tCO2/ha (ước tính tham khảo theo sinh khối cấp tuổi 10 năm)",
                    SourceId = r1.Id,
                    Version = ScientificConstants.DefaultFactorVersion,
                    ValidFrom = validDate,
                    ApprovedBy = "CTU 2015 [R1] Seed Verification",
                    ApplicabilityNotes = "Áp dụng tham khảo cấp vườn cho Dừa thấp (Xiêm xanh) 10 năm tuổi vùng dừa Bến Tre. Không nhân thêm CF 0.47-0.50 hoặc 44/12.",
                    IsActive = true
                }
            );
            await db.SaveChangesAsync(cancellationToken);
        }

        if (!seedDemoData || await db.Users.AnyAsync(cancellationToken))
        {
            return;
        }

        // 4. Seed clearly labeled DEMO / SYNTHETIC accounts & scenario data
        User CreateUser(string uid, string email, string name, RoleCode roleCode)
        {
            var user = new User
            {
                FirebaseUid = uid,
                Email = email,
                DisplayName = name,
                Phone = "+84-000-DEMO"
            };
            var role = existingRoles.First(r => r.Code == roleCode);
            user.UserRoles.Add(new UserRole
            {
                RoleId = role.Id,
                RoleCode = roleCode,
                ScopeType = "Global",
                ApprovalStatus = "Approved"
            });
            return user;
        }

        var farmer = CreateUser("demo-farmer-uid", "farmer.demo@cococarbon.local", "Nguyễn Văn An [DEMO Nông hộ]", RoleCode.Farmer);
        var coopMgr = CreateUser("demo-coop-uid", "coop.demo@cococarbon.local", "Trần Thị Bình [DEMO Quản lý HTX]", RoleCode.CooperativeManager);
        var devUser = CreateUser("demo-dev-uid", "developer.demo@cococarbon.local", "Mekong AgriCarbon Dev [DEMO]", RoleCode.ProjectDeveloper);
        var buyerUser = CreateUser("demo-buyer-uid", "buyer.demo@cococarbon.local", "Saigon Green ESG Buyer [DEMO]", RoleCode.Buyer);
        var reviewerUser = CreateUser("demo-reviewer-uid", "reviewer.demo@cococarbon.local", "Lê Minh Khoa [DEMO Reviewer]", RoleCode.Reviewer);
        var adminUser = CreateUser("demo-admin-uid", "admin.demo@cococarbon.local", "Quản trị CocoCarbon [DEMO Admin]", RoleCode.Admin);

        db.Users.AddRange(farmer, coopMgr, devUser, buyerUser, reviewerUser, adminUser);
        await db.SaveChangesAsync(cancellationToken);

        var coop = new Cooperative
        {
            Name = "HTX Dừa Xanh Giồng Trôm [DEMO]",
            Code = "HTX-DEMO-GT01",
            Province = ScientificConstants.CurrentProvinceDefault,
            Commune = "Xã Lương Quới (Giồng Trôm)",
            HistoricalRegion = ScientificConstants.HistoricalRegionBenTre,
            ManagerUserId = coopMgr.Id,
            DataOriginTag = DataOriginTag.Demo
        };
        db.Cooperatives.Add(coop);
        await db.SaveChangesAsync(cancellationToken);

        var farm1 = new Farm
        {
            OwnerUserId = farmer.Id,
            CooperativeId = coop.Id,
            Name = "Vườn Dừa Ta Giồng Trôm 01 [DEMO]",
            Province = ScientificConstants.CurrentProvinceDefault,
            Commune = "Xã Lương Quới (Giồng Trôm)",
            HistoricalRegion = ScientificConstants.HistoricalRegionBenTre,
            AreaHa = 1.0000m,
            TreeCount = 200,
            Variety = CoconutVariety.TallDuaTa,
            AgeYears = 10,
            Latitude = 10.1652,
            Longitude = 106.4621,
            LocationGeoJson = "{\"type\":\"Point\",\"coordinates\":[106.4621,10.1652]}",
            CareHistoryJson = "{\"organicMulch\":true,\"intercrop\":\"Cacao / Bưởi da xanh\",\"irrigation\":\"Tưới rãnh tự nhiên\"}",
            Status = FarmStatus.ReadyForReview,
            DataOriginTag = DataOriginTag.Demo,
            HasLocationConsent = true,
            HasPhotoConsent = true
        };

        var farm2 = new Farm
        {
            OwnerUserId = farmer.Id,
            CooperativeId = coop.Id,
            Name = "Vườn Dừa Xiêm Xanh Ba Tri [DEMO]",
            Province = ScientificConstants.CurrentProvinceDefault,
            Commune = "Xã An Bình Tây (Ba Tri)",
            HistoricalRegion = ScientificConstants.HistoricalRegionBenTre,
            AreaHa = 1.5000m,
            TreeCount = 330,
            Variety = CoconutVariety.ShortXiemXanh,
            AgeYears = 4,
            Latitude = 10.0448,
            Longitude = 106.5924,
            LocationGeoJson = "{\"type\":\"Point\",\"coordinates\":[106.5924,10.0448]}",
            CareHistoryJson = "{\"organicMulch\":true,\"shellComposting\":true}",
            Status = FarmStatus.Submitted,
            DataOriginTag = DataOriginTag.Demo,
            HasLocationConsent = true,
            HasPhotoConsent = true
        };

        db.Farms.AddRange(farm1, farm2);
        await db.SaveChangesAsync(cancellationToken);

        db.CooperativeMembers.AddRange(
            new CooperativeMember
            {
                CooperativeId = coop.Id,
                UserId = farmer.Id,
                FarmId = farm1.Id,
                IsDataAuthorized = true,
                ParticipationStatus = ParticipationStatus.Active
            },
            new CooperativeMember
            {
                CooperativeId = coop.Id,
                UserId = farmer.Id,
                FarmId = farm2.Id,
                IsDataAuthorized = true,
                ParticipationStatus = ParticipationStatus.Active
            }
        );

        var tall10Factor = await db.CarbonResearchFactors.FirstAsync(f => f.Code == "CTU2015-TALL-10Y", cancellationToken);
        db.CarbonCalculations.Add(new CarbonCalculation
        {
            FarmId = farm1.Id,
            UserId = farmer.Id,
            FactorId = tall10Factor.Id,
            FactorVersion = tall10Factor.Version,
            FormulaVersion = ScientificConstants.FormulaVersion,
            InputSnapshotJson = "{\"areaHa\":1.0,\"treeCount\":200,\"variety\":\"TallDuaTa\",\"treeAgeYears\":10}",
            EstimatedCO2EquivalentT = 75.2436m,
            ObservedTreeDensityPerHa = 200.00m,
            DensityContextNote = "Mật độ quan sát (200.00 cây/ha) nằm trong khoảng khảo sát canh tác Bến Tre 2024 (160-240 cây/ha, mẫu 150 hộ; chỉ mang tính bối cảnh nông học).",
            SourceCitation = "CTU 2015 [R1] - Nguyễn Thị Thanh Trúc & Lê Anh Tuấn (2015)",
            Disclaimer = ScientificConstants.MandatoryCalculatorDisclaimer,
            DataOriginTag = DataOriginTag.Demo
        });

        var project = new Project
        {
            DeveloperId = devUser.Id,
            CooperativeId = coop.Id,
            Name = "Dự án Chuẩn bị Hồ sơ Vùng Dừa Giồng Trôm - Vĩnh Long [DEMO]",
            Code = "PRJ-DEMO-VL-001",
            ProjectType = "Improved Coconut Land Management & Biomass Documentation",
            Province = ScientificConstants.CurrentProvinceDefault,
            HistoricalRegion = ScientificConstants.HistoricalRegionBenTre,
            DeclaredTotalAreaHa = 2.5000m,
            IntendedMethodology = "Đang đánh giá khả năng áp dụng IPCC AFOLU / Verra VM0042 v2.2 (SOC) - chưa đăng ký chính thức",
            MethodologyNotes = "Chỉ tiêu sinh khối vườn dừa tham khảo CTU 2015 [R1]; nếu mở rộng sang tín chỉ cần khảo sát bổ sung và thẩm định độc lập.",
            BaselineSummary = "[DEMO] Khảo sát hiện trạng 2 vườn mẫu, tổng diện tích khai báo 2.5 ha.",
            MonitoringPlanSummary = "[DEMO] Đề xuất ghi nhận ảnh hiện trường, tọa độ lô và sinh khối theo chu kỳ.",
            ReadinessScore = 62.50m,
            ProjectStatus = ProjectStatus.Feasibility,
            DataOriginTag = DataOriginTag.Demo
        };
        db.Projects.Add(project);
        await db.SaveChangesAsync(cancellationToken);

        db.ProjectFarms.AddRange(
            new ProjectFarm { ProjectId = project.Id, FarmId = farm1.Id, HasFarmerConsent = true, ParticipationStatus = ParticipationStatus.Active },
            new ProjectFarm { ProjectId = project.Id, FarmId = farm2.Id, HasFarmerConsent = true, ParticipationStatus = ParticipationStatus.Active }
        );

        var listing = new Listing
        {
            ProjectId = project.Id,
            SellerUserId = devUser.Id,
            Title = "Hồ sơ Dự án Vùng Dừa Giồng Trôm (Giai đoạn Feasibility) [DEMO]",
            Summary = "Hồ sơ kết nối tìm hiểu đầu tư & hợp tác chuỗi giá trị dừa xanh; chưa phát hành tín chỉ carbon.",
            DataOriginTag = DataOriginTag.Demo
        };
        db.Listings.Add(listing);
        await db.SaveChangesAsync(cancellationToken);

        db.Rfqs.Add(new Rfq
        {
            ProjectId = project.Id,
            ListingId = listing.Id,
            BuyerUserId = buyerUser.Id,
            SellerUserId = devUser.Id,
            BuyerOrganization = "Saigon Green ESG Buyer [DEMO]",
            InquiryScope = "Tìm hiểu tiến độ chuẩn bị hồ sơ MRV và cơ hội hợp tác bao tiêu chuỗi giá trị dừa.",
            Message = "[DEMO RFQ] Chúng tôi muốn tìm hiểu lộ trình hoàn thiện tài liệu ranh giới và baseline của dự án.",
            Status = RfqStatus.Submitted
        });

        db.AuditLogs.Add(new AuditLog
        {
            ActorUserId = adminUser.Id,
            ActorEmail = adminUser.Email,
            Action = "SYSTEM_SEED_INITIALIZED",
            EntityType = "System",
            EntityId = "SeedV1",
            DetailsJson = "{\"factorsSeeded\":4,\"demoDataSeeded\":true}"
        });

        await db.SaveChangesAsync(cancellationToken);
    }
}
