from abc import ABC, abstractmethod
from typing import Any, Optional
from pydantic import BaseModel, Field


class ModelExtractionOutput(BaseModel):
    extractedFields: list[dict[str, Any]] = Field(default_factory=list)
    documentClaims: list[str] = Field(default_factory=list)
    missingFields: list[str] = Field(default_factory=list)
    evidence: list[dict[str, Any]] = Field(default_factory=list)
    confidenceNotes: list[str] = Field(default_factory=list)


class BaseAiProvider(ABC):
    provider_name: str
    model_name: str

    @abstractmethod
    def extract_document(
        self,
        *,
        file_id: str,
        filename: str,
        mime_type: str,
        file_bytes: bytes,
        extracted_text_pages: list[tuple[int, str]],
        farm_context: dict[str, Any],
        project_context: dict[str, Any],
    ) -> tuple[ModelExtractionOutput, bool]:
        """Return (structured extraction, is_fallback)."""

    @abstractmethod
    def synthesize_rag_answer(
        self,
        *,
        role: str,
        question: str,
        context_data: dict[str, Any],
        retrieved_sources: list[dict[str, Any]],
    ) -> tuple[str, bool]:
        """Return (answer_text, is_fallback)."""
