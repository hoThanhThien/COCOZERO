const API_BASE =
  (typeof window !== "undefined" ? process.env.NEXT_PUBLIC_API_BASE_URL : process.env.NEXT_PUBLIC_API_BASE_URL) ||
  "http://localhost:8080";

export function getApiBaseUrl(): string {
  return API_BASE.replace(/\/$/, "");
}

export function getStoredToken(): string | null {
  if (typeof window === "undefined") return null;
  return localStorage.getItem("cococarbon_token");
}

export function setStoredSession(token: string, mode: "firebase" | "demo"): void {
  if (typeof window === "undefined") return;
  localStorage.setItem("cococarbon_token", token);
  localStorage.setItem("cococarbon_auth_mode", mode);
}

export class ApiError extends Error {
  status: number;
  payload: unknown;
  constructor(status: number, message: string, payload?: unknown) {
    super(message);
    this.status = status;
    this.payload = payload;
  }
}

export async function apiFetch<T>(path: string, init: RequestInit = {}): Promise<T> {
  const token = getStoredToken();
  const headers = new Headers(init.headers || {});
  if (token) {
    headers.set("Authorization", `Bearer ${token}`);
  }
  if (!(init.body instanceof FormData) && !headers.has("Content-Type") && init.body) {
    headers.set("Content-Type", "application/json");
  }

  const resp = await fetch(`${getApiBaseUrl()}${path}`, {
    ...init,
    headers,
    cache: "no-store",
  });

  if (resp.status === 204) {
    return undefined as T;
  }

  const text = await resp.text();
  let json: Record<string, unknown> = {};
  try {
    json = text ? JSON.parse(text) : {};
  } catch {
    json = { message: text };
  }

  if (!resp.ok) {
    const msg =
      (typeof json.message === "string" && json.message) ||
      (typeof json.detail === "string" && json.detail) ||
      `Lỗi HTTP ${resp.status}`;
    throw new ApiError(resp.status, msg, json);
  }

  return json as T;
}
