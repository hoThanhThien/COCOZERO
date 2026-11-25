from __future__ import annotations

import json
from pathlib import Path
import time
from typing import Any, Literal, Optional
from google import genai
import httpx
from pydantic import BaseModel, Field

from app.config import settings
from app.providers.base import BaseAiProvider
from app.providers.external_providers import AnthropicClaudeProvider, OpenAICompatibleProvider
from app.providers.gemini_provider import GeminiInteractionsProvider
from app.providers.heuristic_provider import HeuristicFallbackProvider

AiProviderId = Literal["gemini", "openai", "openrouter", "groq", "anthropic", "custom", "heuristic"]

_RUNTIME_CONFIG_PATH = Path(__file__).resolve().parent.parent.parent / ".runtime_ai_config.json"

PROVIDER_PRESETS: list[dict[str, Any]] = [
    {
        "id": "gemini",
        "label": "Google Gemini (Interactions API)",
        "defaultModel": "gemini-4-hydrogen",
        "suggestedModels": ["gemini-4-hydrogen", "gemini-4-argon", "gemini-4-neon", "gemini-4-carbon", "gemini-3.5-flash-lite"],
        "defaultBaseUrl": "https://generativelanguage.googleapis.com",
        "requiresKey": True,
    },
    {
        "id": "openai",
        "label": "OpenAI (ChatGPT / GPT Models)",
        "defaultModel": "gpt-4o-mini",
        "suggestedModels": ["gpt-4o-mini", "gpt-4o", "gpt-4.1-mini"],
        "defaultBaseUrl": "https://api.openai.com/v1",
        "requiresKey": True,
    },
    {
        "id": "openrouter",
        "label": "OpenRouter (Multi-Model Gateway)",
        "defaultModel": "google/gemini-2.5-flash",
        "suggestedModels": ["openai/gpt-4o-mini", "anthropic/claude-3.7-sonnet", "deepseek/deepseek-chat-v3-0324"],
        "defaultBaseUrl": "https://openrouter.ai/api/v1",
        "requiresKey": True,
    },
    {
        "id": "groq",
        "label": "Groq (Llama / Qwen Ultra-Fast)",
        "defaultModel": "llama-3.3-70b-versatile",
        "suggestedModels": ["llama-3.3-70b-versatile", "qwen/qwen3-32b"],
        "defaultBaseUrl": "https://api.groq.com/openai/v1",
        "requiresKey": True,
    },
    {
        "id": "anthropic",
        "label": "Anthropic Claude",
        "defaultModel": "claude-3-7-sonnet-latest",
        "suggestedModels": ["claude-3-7-sonnet-latest", "claude-3-5-haiku-latest"],
        "defaultBaseUrl": "https://api.anthropic.com",
        "requiresKey": True,
    },
    {
        "id": "custom",
        "label": "OpenAI-Compatible Custom / Local (DeepSeek, vLLM, Ollama)",
        "defaultModel": "deepseek-chat",
        "suggestedModels": ["deepseek-chat", "qwen2.5:14b"],
        "defaultBaseUrl": "http://localhost:11434/v1",
        "requiresKey": False,
    },
    {
        "id": "heuristic",
        "label": "Heuristic Offline Fallback (Không cần API Key)",
        "defaultModel": "heuristic-v1",
        "suggestedModels": ["heuristic-v1"],
        "defaultBaseUrl": "",
        "requiresKey": False,
    },
]


def _mask_key(raw: str) -> str:
    k = (raw or "").strip()
    if not k:
        return ""
    if len(k) <= 8:
        return "*" * len(k)
    return f"{k[:4]}...{k[-4:]}"


class UpdateAiProviderRequest(BaseModel):
    activeProvider: AiProviderId
    modelName: str = Field(default="", max_length=120)
    baseUrl: str = Field(default="", max_length=300)
    apiKey: Optional[str] = Field(default=None, description="Chỉ truyền khi muốn thêm hoặc cập nhật API key mới cho provider đã chọn.")
    clearApiKey: bool = False


class TestAiProviderRequest(BaseModel):
    provider: AiProviderId
    modelName: str = ""
    baseUrl: str = ""
    apiKey: Optional[str] = None


class AiRuntimeConfigManager:
    """Thread-safe in-memory + gitignored local runtime store for Multi-AI reasoning keys (no DB schema changes)."""

    def __init__(self) -> None:
        self._active_provider: str = (settings.ai_provider or "gemini").lower()
        if self._active_provider not in {p["id"] for p in PROVIDER_PRESETS}:
            self._active_provider = "gemini"
        self._model_by_provider: dict[str, str] = {p["id"]: p["defaultModel"] for p in PROVIDER_PRESETS}
        self._model_by_provider["gemini"] = settings.gemini_model or "gemini-4-hydrogen"
        self._base_url_by_provider: dict[str, str] = {p["id"]: p["defaultBaseUrl"] for p in PROVIDER_PRESETS}
        self._keys_by_provider: dict[str, str] = {}
        if settings.gemini_api_key:
            self._keys_by_provider["gemini"] = settings.gemini_api_key.strip()
        self._load_persisted_if_exists()

    def _load_persisted_if_exists(self) -> None:
        if not _RUNTIME_CONFIG_PATH.exists():
            return
        try:
            data = json.loads(_RUNTIME_CONFIG_PATH.read_text(encoding="utf-8"))
            if isinstance(data, dict):
                if data.get("activeProvider") in {p["id"] for p in PROVIDER_PRESETS}:
                    self._active_provider = data["activeProvider"]
                for k, v in (data.get("models") or {}).items():
                    if v:
                        self._model_by_provider[k] = str(v)
                for k, v in (data.get("baseUrls") or {}).items():
                    if v is not None:
                        self._base_url_by_provider[k] = str(v)
                for k, v in (data.get("keys") or {}).items():
                    if v:
                        self._keys_by_provider[k] = str(v)
        except Exception:
            pass

    def _save_persisted(self) -> None:
        try:
            payload = {
                "activeProvider": self._active_provider,
                "models": self._model_by_provider,
                "baseUrls": self._base_url_by_provider,
                "keys": self._keys_by_provider,
            }
            _RUNTIME_CONFIG_PATH.write_text(json.dumps(payload, ensure_ascii=False, indent=2), encoding="utf-8")
        except Exception:
            pass

    def update(self, req: UpdateAiProviderRequest, *, persist: bool = True) -> dict[str, Any]:
        pid = req.activeProvider
        self._active_provider = pid
        if req.modelName.strip():
            self._model_by_provider[pid] = req.modelName.strip()
        if req.baseUrl.strip() or pid == "custom":
            self._base_url_by_provider[pid] = req.baseUrl.strip()
        if req.clearApiKey:
            self._keys_by_provider.pop(pid, None)
        elif req.apiKey is not None and req.apiKey.strip():
            self._keys_by_provider[pid] = req.apiKey.strip()

        if persist:
            self._save_persisted()
        return self.get_public_status()

    def get_public_status(self) -> dict[str, Any]:
        configured_providers = []
        for preset in PROVIDER_PRESETS:
            pid = preset["id"]
            raw_key = self._keys_by_provider.get(pid, "")
            configured_providers.append({
                "id": pid,
                "label": preset["label"],
                "active": pid == self._active_provider,
                "hasApiKey": bool(raw_key),
                "maskedApiKey": _mask_key(raw_key),
                "modelName": self._model_by_provider.get(pid, preset["defaultModel"]),
                "suggestedModels": preset["suggestedModels"],
                "baseUrl": self._base_url_by_provider.get(pid, preset["defaultBaseUrl"]),
                "requiresKey": preset["requiresKey"],
            })

        active_key_present = bool(self._keys_by_provider.get(self._active_provider))
        return {
            "activeProvider": self._active_provider,
            "activeModelName": self._model_by_provider.get(self._active_provider, "heuristic-v1"),
            "activeHasApiKey": active_key_present,
            "isUsingFallbackEngine": (self._active_provider == "heuristic") or (self._active_provider != "custom" and not active_key_present),
            "providers": configured_providers,
            "securityNote": "API key chỉ được lưu an toàn tại dịch vụ AI phía server, không bao giờ gửi key đầy đủ ngược về trình duyệt.",
        }

    def get_active_provider(self) -> BaseAiProvider:
        pid = self._active_provider
        model = self._model_by_provider.get(pid, settings.gemini_model)
        key = self._keys_by_provider.get(pid, "")
        base_url = self._base_url_by_provider.get(pid, "")

        if pid == "gemini" and key:
            return GeminiInteractionsProvider(
                api_key=key,
                model_name=model or "gemini-4-hydrogen",
                timeout_seconds=settings.ai_request_timeout_seconds,
                max_retries=settings.ai_max_retries,
            )
        if pid in ("openai", "openrouter", "groq", "custom") and (key or pid == "custom"):
            return OpenAICompatibleProvider(
                provider_name=pid,
                api_key=key,
                model_name=model or "gpt-4o-mini",
                base_url=base_url or "https://api.openai.com/v1",
                timeout_seconds=settings.ai_request_timeout_seconds,
                max_retries=min(1, settings.ai_max_retries),
            )
        if pid == "anthropic" and key:
            return AnthropicClaudeProvider(
                api_key=key,
                model_name=model or "claude-3-7-sonnet-latest",
                base_url=base_url or "https://api.anthropic.com",
                timeout_seconds=settings.ai_request_timeout_seconds,
                max_retries=min(1, settings.ai_max_retries),
            )
        return HeuristicFallbackProvider(f"Provider '{pid}' chưa cấu hình API key hoặc đang ở chế độ fallback")

    def test_connection(self, req: TestAiProviderRequest) -> dict[str, Any]:
        pid = req.provider
        model = req.modelName.strip() or self._model_by_provider.get(pid, "")
        base_url = req.baseUrl.strip() or self._base_url_by_provider.get(pid, "")
        key = (req.apiKey or "").strip() or self._keys_by_provider.get(pid, "")
        started = time.perf_counter()

        if pid == "heuristic":
            return {
                "ok": True,
                "provider": "heuristic",
                "modelName": "heuristic-v1",
                "latencyMs": 1,
                "replyPreview": "Chế độ Heuristic Fallback sẵn sàng ngoại tuyến.",
            }

        if not key and pid != "custom":
            return {
                "ok": False,
                "provider": pid,
                "modelName": model,
                "latencyMs": 0,
                "error": f"Chưa nhập API key cho provider '{pid}'.",
            }

        try:
            if pid == "gemini":
                client = genai.Client(api_key=key)
                inter = client.interactions.create(
                    model=model or "gemini-4-hydrogen",
                    input="Reply with only: OK",
                    store=False,
                    timeout=12.0,
                )
                reply = (inter.output_text or "OK").strip()[:120]
            elif pid == "anthropic":
                with httpx.Client(timeout=12.0) as client:
                    resp = client.post(
                        f"{(base_url or 'https://api.anthropic.com').rstrip('/')}/v1/messages",
                        headers={"Content-Type": "application/json", "x-api-key": key, "anthropic-version": "2023-06-01"},
                        json={"model": model or "claude-3-7-sonnet-latest", "max_tokens": 32, "messages": [{"role": "user", "content": "Reply OK"}]},
                    )
                    resp.raise_for_status()
                    reply = "".join(b["text"] for b in resp.json().get("content", []) if b.get("type") == "text").strip()[:120]
            else:
                headers = {"Content-Type": "application/json"}
                if key:
                    headers["Authorization"] = f"Bearer {key}"
                with httpx.Client(timeout=12.0) as client:
                    resp = client.post(
                        f"{(base_url or 'https://api.openai.com/v1').rstrip('/')}/chat/completions",
                        headers=headers,
                        json={"model": model or "gpt-4o-mini", "max_tokens": 32, "messages": [{"role": "user", "content": "Reply OK"}]},
                    )
                    resp.raise_for_status()
                    reply = (resp.json()["choices"][0]["message"].get("content") or "OK").strip()[:120]

            return {
                "ok": True,
                "provider": pid,
                "modelName": model,
                "latencyMs": int((time.perf_counter() - started) * 1000),
                "replyPreview": reply,
            }
        except Exception as exc:
            return {
                "ok": False,
                "provider": pid,
                "modelName": model,
                "latencyMs": int((time.perf_counter() - started) * 1000),
                "error": f"Kết nối thất bại ({type(exc).__name__}): {str(exc)[:180]}",
            }


ai_runtime_config = AiRuntimeConfigManager()
