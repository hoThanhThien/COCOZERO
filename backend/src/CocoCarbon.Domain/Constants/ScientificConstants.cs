namespace CocoCarbon.Domain.Constants;

public static class ScientificConstants
{
    public const string FormulaVersion = "CTU-2015-AREA-V1";
    public const string DefaultFactorVersion = "CTU-2015-v1.0";

    public const string MandatoryCalculatorDisclaimer =
        "Đây là ước tính tham khảo theo nghiên cứu sinh khối cây dừa tại Giồng Trôm, Bến Tre (2015), " +
        "áp dụng theo cấp tuổi và nhóm giống khảo sát. Kết quả không đại diện cho phép đo độc lập tại vườn của bạn, " +
        "không phải lượng hấp thụ CO2 hằng năm, không phải tín chỉ carbon đã được phát hành và không phải doanh thu được đảm bảo.";

    public const string MethodologyLimitationsSummary =
        "Nghiên cứu CTU (2015) khảo sát 4 ô tiêu chuẩn 10m x 10m (12 cây mẫu) tại huyện Giồng Trôm, Bến Tre; " +
        "sinh khối rễ ước tính qua R/S = 1/5 (không đào trực tiếp), sử dụng hệ số carbon 0.47-0.50 (IPCC 2003) đã tính sẵn vào tCO2/ha. " +
        "Nghiên cứu không theo dõi tăng trưởng sinh khối trong 1 năm nên không biểu thị tCO2/ha/năm.";

    public const string ArithmeticAllocationLabel =
        "Phân bổ số học bình quân theo số cây khai báo - không phải phép đo trực tiếp từng cây.";

    public const string UnsupportedFactorMessage =
        "Chưa có hệ số nghiên cứu phù hợp trong bộ hệ số CTU 2015 (chỉ hỗ trợ Dừa Ta / Tall và Dừa Xiêm xanh / Short ở cấp tuổi 4 năm hoặc 10 năm; không tự động nội suy hoặc ngoại suy).";

    public const decimal BenTre2024SurveyMinDensity = 160m;
    public const decimal BenTre2024SurveyMaxDensity = 240m;
    public const int BenTre2024SurveyHouseholdCount = 150;

    public const string CurrentProvinceDefault = "Vĩnh Long";
    public const string HistoricalRegionBenTre = "Vùng dừa Bến Tre";

    public static readonly int[] SupportedAgeClasses = [4, 10];
}
