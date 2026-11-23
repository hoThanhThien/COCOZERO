from pydantic_settings import BaseSettings, SettingsConfigDict


class Settings(BaseSettings):
    model_config = SettingsConfigDict(env_file=".env", extra="ignore")

    ai_provider: str = "gemini"
    gemini_api_key: str = ""
    gemini_model: str = "gemini-4-hydrogen"
    prompt_version: str = "coco-auditor-v1.0"
    ai_service_internal_key: str = ""
    ai_request_timeout_seconds: float = 25.0
    ai_max_file_bytes: int = 15 * 1024 * 1024
    ai_max_retries: int = 2


settings = Settings()
