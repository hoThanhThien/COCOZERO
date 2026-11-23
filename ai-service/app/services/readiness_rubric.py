from typing import Any
from app.schemas import ReadinessCriterionItem, ReadinessRubric


def evaluate_project_readiness(
    *,
    farm_context: dict[str, Any],
    project_context: dict[str, Any],
    extracted_fields: list[dict[str, Any]],
    has_malformed_pdf: bool,
) -> ReadinessRubric:
    extracted_keys = {str(f.get("field", "")) for f in extracted_fields}

    criteria_specs = [
        ("Định danh vườn / dự án", bool(farm_context.get("farmName") or project_context.get("name")), "Có thông tin định danh cơ bản."),
        ("Vị trí / ranh giới địa lý", bool(farm_context.get("commune") or "regionMention" in extracted_keys), "Cần tọa độ/polygon kèm consent nếu làm dự án."),
        ("Dữ liệu diện tích & cây trồng", bool(farm_context.get("areaHa") and farm_context.get("treeCount")), "Đã có diện tích ha và số lượng cây khai báo."),
        ("Chứng từ quyền sử dụng / tài liệu không lỗi", not has_malformed_pdf and ("areaHa" in extracted_keys or bool(extracted_fields)), "Tài liệu đọc được và có bằng chứng trích xuất."),
        ("Baseline & phương pháp luận dự kiến", bool(project_context.get("intendedMethodology") and project_context.get("baselineSummary")), "Cần mô tả phương pháp (IPCC / Verra / nghiên cứu địa phương) và baseline."),
        ("Kế hoạch giám sát (Monitoring Plan)", bool(project_context.get("monitoringPlanSummary")), "Cần kế hoạch theo dõi định kỳ nếu phát triển MRV."),
    ]

    items: list[ReadinessCriterionItem] = []
    met_count = 0
    for name, ok, note in criteria_specs:
        status = "Đủ" if ok else "Thiếu"
        if ok:
            met_count += 1
        items.append(ReadinessCriterionItem(
            criterion=name,
            status=status,
            notes=note if ok else f"Cần bổ sung: {note}",
        ))

    score = round((met_count / len(criteria_specs)) * 100.0, 2)
    return ReadinessRubric(
        label="Mức độ đầy đủ hồ sơ / Project Readiness",
        score=score,
        items=items,
    )
