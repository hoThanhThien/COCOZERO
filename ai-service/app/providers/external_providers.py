from __future__ import annotations

import json
import logging
import time
from typing import Any
import httpx

from app.providers.base import BaseAiProvider, ModelExtractionOutput
from app.providers.gemini_provider import DOCUMENT_SYSTEM_INSTRUCTION, RAG_SYSTEM_INSTRUCTION
from app.providers.heuristic_provider import HeuristicFallbackProvider
from app.services.tool_registry import TOOL_DECLARATIONS, execute_tool

logger = logging.getLogger(__name__)


def _to_openai_tools() -> list[dict[str, Any]]:
    return [
        {
            "type": "function",
            "function": {
                "name": t["name"],
                "description": t.get("description", ""),
                "parameters": t.get("parameters", {"type": "object", "properties": {}}),
            },
        }
        for t in TOOL_DECLARATIONS
    ]


def _to_anthropic_tools() -> list[dict[str, Any]]:
    return [
        {
            "name": t["name"],
            "description": t.get("description", ""),
            "input_schema": t.get("parameters", {"type": "object", "properties": {}}),
        }
        for t in TOOL_DECLARATIONS
    ]


class OpenAICompatibleProvider(BaseAiProvider):
    """Supports OpenAI, OpenRouter, Groq, DeepSeek, or any OpenAI-compatible endpoint with tool calling."""

    def __init__(
        self,
        *,
        provider_name: str,
        api_key: str,
        model_name: str,
        base_url: str,
        timeout_seconds: float = 25.0,
        max_retries: int = 1,
    ) -> None:
        self.provider_name = provider_name
        self.model_name = model_name
        self._api_key = api_key.strip()
        self._base_url = base_url.rstrip("/")
        self._timeout = timeout_seconds
        self._max_retries = max_retries
        self._fallback = HeuristicFallbackProvider(f"{provider_name} fallback after API error/unavailable")

    def _headers(self) -> dict[str, str]:
        headers = {"Content-Type": "application/json"}
        if self._api_key:
            headers["Authorization"] = f"Bearer {self._api_key}"
        return headers

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
        if not self._api_key and "localhost" not in self._base_url and "127.0.0.1" not in self._base_url:
            return self._fallback.extract_document(
                file_id=file_id,
                filename=filename,
                mime_type=mime_type,
                file_bytes=file_bytes,
                extracted_text_pages=extracted_text_pages,
                farm_context=farm_context,
                project_context=project_context,
            )

        text_preview = "\n".join(f"[Trang {p}] {txt[:1800]}" for p, txt in extracted_text_pages[:8])
        user_msg = (
            f"Trích xuất các trường JSON đúng schema {ModelExtractionOutput.model_json_schema()} từ '{filename}' (fileId={file_id}).\n"
            f"Ngữ cảnh đối chiếu: farm={json.dumps(farm_context, ensure_ascii=False)}, project={json.dumps(project_context, ensure_ascii=False)}.\n"
            f"<untrusted_document_data>\n{text_preview}\n</untrusted_document_data>"
        )
        try:
            with httpx.Client(timeout=self._timeout) as client:
                resp = client.post(
                    f"{self._base_url}/chat/completions",
                    headers=self._headers(),
                    json={
                        "model": self.model_name,
                        "response_format": {"type": "json_object"},
                        "messages": [
                            {"role": "system", "content": DOCUMENT_SYSTEM_INSTRUCTION},
                            {"role": "user", "content": user_msg},
                        ],
                    },
                )
                resp.raise_for_status()
                content = resp.json()["choices"][0]["message"]["content"]
                return ModelExtractionOutput.model_validate_json(content), False
        except Exception as exc:
            logger.warning("%s document extraction fallback (%s)", self.provider_name, type(exc).__name__)
            return self._fallback.extract_document(
                file_id=file_id,
                filename=filename,
                mime_type=mime_type,
                file_bytes=file_bytes,
                extracted_text_pages=extracted_text_pages,
                farm_context=farm_context,
                project_context=project_context,
            )

    def synthesize_rag_answer(
        self,
        *,
        role: str,
        question: str,
        context_data: dict[str, Any],
        retrieved_sources: list[dict[str, Any]],
    ) -> tuple[str, bool]:
        if not retrieved_sources:
            return self._fallback.synthesize_rag_answer(
                role=role, question=question, context_data=context_data, retrieved_sources=retrieved_sources
            )
        if not self._api_key and "localhost" not in self._base_url and "127.0.0.1" not in self._base_url:
            return self._fallback.synthesize_rag_answer(
                role=role, question=question, context_data=context_data, retrieved_sources=retrieved_sources
            )

        prompt = (
            f"Vai trò: {role}\nCâu hỏi: {question}\nNgữ cảnh: {json.dumps(context_data, ensure_ascii=False)}\n"
            f"Nguồn trích dẫn hợp lệ:\n{json.dumps(retrieved_sources, ensure_ascii=False)}"
        )
        try:
            with httpx.Client(timeout=self._timeout) as client:
                resp = client.post(
                    f"{self._base_url}/chat/completions",
                    headers=self._headers(),
                    json={
                        "model": self.model_name,
                        "messages": [
                            {"role": "system", "content": RAG_SYSTEM_INSTRUCTION},
                            {"role": "user", "content": prompt},
                        ],
                    },
                )
                resp.raise_for_status()
                text = (resp.json()["choices"][0]["message"].get("content") or "").strip()
                if text:
                    return text, False
        except Exception as exc:
            logger.warning("%s RAG synthesis fallback (%s)", self.provider_name, type(exc).__name__)
        return self._fallback.synthesize_rag_answer(
            role=role, question=question, context_data=context_data, retrieved_sources=retrieved_sources
        )

    def run_assistant_with_tools(
        self,
        *,
        role: str,
        question: str,
        intent: str,
        language: str,
        session_history: list[dict[str, Any]],
        context_data: dict[str, Any],
        retrieved_sources: list[dict[str, Any]],
        precomputed_tool_outputs: dict[str, Any] | None = None,
    ) -> tuple[str, list[dict[str, Any]], bool]:
        if not self._api_key and "localhost" not in self._base_url and "127.0.0.1" not in self._base_url:
            return self._fallback.run_assistant_with_tools(
                role=role,
                question=question,
                intent=intent,
                language=language,
                session_history=session_history,
                context_data=context_data,
                retrieved_sources=retrieved_sources,
                precomputed_tool_outputs=precomputed_tool_outputs,
            )

        payload = {
            "role": role,
            "detectedIntent": intent,
            "preferredLanguage": language,
            "recentTurns": session_history[-4:],
            "contextData": context_data,
            "precomputedToolOutputs": precomputed_tool_outputs or {},
            "retrievedSources": retrieved_sources,
            "question": question,
        }
        messages: list[dict[str, Any]] = [
            {"role": "system", "content": RAG_SYSTEM_INSTRUCTION},
            {"role": "user", "content": json.dumps(payload, ensure_ascii=False)},
        ]
        tools = _to_openai_tools()
        tool_traces: list[dict[str, Any]] = []

        for attempt in range(self._max_retries + 1):
            try:
                cur_msgs = list(messages)
                with httpx.Client(timeout=self._timeout) as client:
                    for _ in range(4):
                        resp = client.post(
                            f"{self._base_url}/chat/completions",
                            headers=self._headers(),
                            json={
                                "model": self.model_name,
                                "messages": cur_msgs,
                                "tools": tools,
                                "tool_choice": "auto",
                            },
                        )
                        resp.raise_for_status()
                        msg = resp.json()["choices"][0]["message"]
                        cur_msgs.append(msg)
                        tcalls = msg.get("tool_calls") or []
                        if not tcalls:
                            content = (msg.get("content") or "").strip()
                            if content:
                                return content, tool_traces, False
                            break
                        for tc in tcalls:
                            fn_name = tc["function"]["name"]
                            raw_args = tc["function"].get("arguments") or "{}"
                            args = json.loads(raw_args) if isinstance(raw_args, str) else dict(raw_args)
                            res = execute_tool(fn_name, args)
                            tool_traces.append({"tool": fn_name, "arguments": args, "ok": res.get("ok", False)})
                            cur_msgs.append({
                                "role": "tool",
                                "tool_call_id": tc["id"],
                                "content": json.dumps(res, ensure_ascii=False),
                            })
                    break
            except Exception as exc:
                logger.warning("%s tool-calling attempt %d failed (%s)", self.provider_name, attempt + 1, type(exc).__name__)
                if attempt < self._max_retries:
                    time.sleep(0.4)

        return self._fallback.run_assistant_with_tools(
            role=role,
            question=question,
            intent=intent,
            language=language,
            session_history=session_history,
            context_data=context_data,
            retrieved_sources=retrieved_sources,
            precomputed_tool_outputs=precomputed_tool_outputs,
        )


class AnthropicClaudeProvider(BaseAiProvider):
    """Supports Anthropic Claude API (/v1/messages) with tool calling and safe fallback."""

    def __init__(
        self,
        *,
        api_key: str,
        model_name: str = "claude-3-7-sonnet-latest",
        base_url: str = "https://api.anthropic.com",
        timeout_seconds: float = 25.0,
        max_retries: int = 1,
    ) -> None:
        self.provider_name = "anthropic"
        self.model_name = model_name
        self._api_key = api_key.strip()
        self._base_url = base_url.rstrip("/")
        self._timeout = timeout_seconds
        self._max_retries = max_retries
        self._fallback = HeuristicFallbackProvider("anthropic fallback after API error/unavailable")

    def _headers(self) -> dict[str, str]:
        return {
            "Content-Type": "application/json",
            "x-api-key": self._api_key,
            "anthropic-version": "2023-06-01",
        }

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
        if not self._api_key:
            return self._fallback.extract_document(
                file_id=file_id,
                filename=filename,
                mime_type=mime_type,
                file_bytes=file_bytes,
                extracted_text_pages=extracted_text_pages,
                farm_context=farm_context,
                project_context=project_context,
            )

        text_preview = "\n".join(f"[Trang {p}] {txt[:1800]}" for p, txt in extracted_text_pages[:8])
        user_msg = (
            f"Chỉ trả về duy nhất JSON hợp lệ theo schema {ModelExtractionOutput.model_json_schema()} cho '{filename}' (fileId={file_id}).\n"
            f"<untrusted_document_data>\n{text_preview}\n</untrusted_document_data>"
        )
        try:
            with httpx.Client(timeout=self._timeout) as client:
                resp = client.post(
                    f"{self._base_url}/v1/messages",
                    headers=self._headers(),
                    json={
                        "model": self.model_name,
                        "max_tokens": 1800,
                        "system": DOCUMENT_SYSTEM_INSTRUCTION,
                        "messages": [{"role": "user", "content": user_msg}],
                    },
                )
                resp.raise_for_status()
                text_parts = [b["text"] for b in resp.json().get("content", []) if b.get("type") == "text"]
                return ModelExtractionOutput.model_validate_json("".join(text_parts)), False
        except Exception as exc:
            logger.warning("Anthropic extract_document fallback (%s)", type(exc).__name__)
            return self._fallback.extract_document(
                file_id=file_id,
                filename=filename,
                mime_type=mime_type,
                file_bytes=file_bytes,
                extracted_text_pages=extracted_text_pages,
                farm_context=farm_context,
                project_context=project_context,
            )

    def synthesize_rag_answer(
        self,
        *,
        role: str,
        question: str,
        context_data: dict[str, Any],
        retrieved_sources: list[dict[str, Any]],
    ) -> tuple[str, bool]:
        if not self._api_key or not retrieved_sources:
            return self._fallback.synthesize_rag_answer(
                role=role, question=question, context_data=context_data, retrieved_sources=retrieved_sources
            )
        prompt = (
            f"Vai trò: {role}\nCâu hỏi: {question}\nNgữ cảnh: {json.dumps(context_data, ensure_ascii=False)}\n"
            f"Nguồn:\n{json.dumps(retrieved_sources, ensure_ascii=False)}"
        )
        try:
            with httpx.Client(timeout=self._timeout) as client:
                resp = client.post(
                    f"{self._base_url}/v1/messages",
                    headers=self._headers(),
                    json={
                        "model": self.model_name,
                        "max_tokens": 1500,
                        "system": RAG_SYSTEM_INSTRUCTION,
                        "messages": [{"role": "user", "content": prompt}],
                    },
                )
                resp.raise_for_status()
                text = "\n".join(b["text"] for b in resp.json().get("content", []) if b.get("type") == "text").strip()
                if text:
                    return text, False
        except Exception as exc:
            logger.warning("Anthropic RAG synthesis fallback (%s)", type(exc).__name__)
        return self._fallback.synthesize_rag_answer(
            role=role, question=question, context_data=context_data, retrieved_sources=retrieved_sources
        )

    def run_assistant_with_tools(
        self,
        *,
        role: str,
        question: str,
        intent: str,
        language: str,
        session_history: list[dict[str, Any]],
        context_data: dict[str, Any],
        retrieved_sources: list[dict[str, Any]],
        precomputed_tool_outputs: dict[str, Any] | None = None,
    ) -> tuple[str, list[dict[str, Any]], bool]:
        if not self._api_key:
            return self._fallback.run_assistant_with_tools(
                role=role,
                question=question,
                intent=intent,
                language=language,
                session_history=session_history,
                context_data=context_data,
                retrieved_sources=retrieved_sources,
                precomputed_tool_outputs=precomputed_tool_outputs,
            )

        payload = {
            "role": role,
            "detectedIntent": intent,
            "preferredLanguage": language,
            "recentTurns": session_history[-4:],
            "contextData": context_data,
            "precomputedToolOutputs": precomputed_tool_outputs or {},
            "retrievedSources": retrieved_sources,
            "question": question,
        }
        messages: list[dict[str, Any]] = [{"role": "user", "content": json.dumps(payload, ensure_ascii=False)}]
        tools = _to_anthropic_tools()
        tool_traces: list[dict[str, Any]] = []

        for attempt in range(self._max_retries + 1):
            try:
                cur_msgs = list(messages)
                with httpx.Client(timeout=self._timeout) as client:
                    for _ in range(4):
                        resp = client.post(
                            f"{self._base_url}/v1/messages",
                            headers=self._headers(),
                            json={
                                "model": self.model_name,
                                "max_tokens": 2000,
                                "system": RAG_SYSTEM_INSTRUCTION,
                                "tools": tools,
                                "messages": cur_msgs,
                            },
                        )
                        resp.raise_for_status()
                        content_blocks = resp.json().get("content", [])
                        cur_msgs.append({"role": "assistant", "content": content_blocks})
                        tool_uses = [b for b in content_blocks if b.get("type") == "tool_use"]
                        if not tool_uses:
                            text = "\n".join(b["text"] for b in content_blocks if b.get("type") == "text").strip()
                            if text:
                                return text, tool_traces, False
                            break
                        tool_results = []
                        for tu in tool_uses:
                            res = execute_tool(tu["name"], tu.get("input") or {})
                            tool_traces.append({"tool": tu["name"], "arguments": tu.get("input") or {}, "ok": res.get("ok", False)})
                            tool_results.append({
                                "type": "tool_result",
                                "tool_use_id": tu["id"],
                                "content": json.dumps(res, ensure_ascii=False),
                            })
                        cur_msgs.append({"role": "user", "content": tool_results})
                    break
            except Exception as exc:
                logger.warning("Anthropic tool-calling attempt %d failed (%s)", attempt + 1, type(exc).__name__)
                if attempt < self._max_retries:
                    time.sleep(0.4)

        return self._fallback.run_assistant_with_tools(
            role=role,
            question=question,
            intent=intent,
            language=language,
            session_history=session_history,
            context_data=context_data,
            retrieved_sources=retrieved_sources,
            precomputed_tool_outputs=precomputed_tool_outputs,
        )
