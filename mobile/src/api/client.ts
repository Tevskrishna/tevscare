import * as SecureStore from "expo-secure-store";
import { isPublicHttpsApiUrl } from "../lib/apiUrl";

const ACCESS = "tevscare.access";
const REFRESH = "tevscare.refresh";
const USER = "tevscare.user";

const appEnvironment = process.env.EXPO_PUBLIC_APP_ENV ?? "development";
const configuredApiUrl = process.env.EXPO_PUBLIC_API_URL;
export const apiBaseUrl = configuredApiUrl ?? "http://localhost:5080";

if (appEnvironment !== "development" && !isPublicHttpsApiUrl(configuredApiUrl)) {
  throw new Error("Staging and production builds require EXPO_PUBLIC_API_URL to be a public https address.");
}

function friendlyError(status: number, detail: string | undefined) {
  if (status === 401) return "Sign in again.";
  if (status === 403) return "You do not have access to this.";
  if (status === 0 || status === 408 || status >= 500) return "Connection unavailable. Please try again.";
  if (!detail || /localhost|127\.0\.0\.1|192\.168\.|exception|stack trace/i.test(detail)) {
    return "That did not work. Please try again.";
  }
  return detail;
}

export class ApiError extends Error {
  status: number;
  constructor(message: string, status: number) {
    super(message);
    this.status = status;
  }
}

export type SessionUser = {
  id: string;
  fullName: string;
  email: string;
  timezone: string;
  roles: string[];
  onboardingCompleted: boolean;
  entitlementPlan: string;
};

type AuthPayload = {
  accessToken: string;
  refreshToken: string;
  user: SessionUser;
  developmentResetToken?: string | null;
};

let accessToken: string | null = null;
let refreshToken: string | null = null;
let currentUser: SessionUser | null = null;

export function getAccessToken() {
  return accessToken;
}

export function getCurrentUser() {
  return currentUser;
}

export async function hydrateSession() {
  accessToken = await SecureStore.getItemAsync(ACCESS);
  refreshToken = await SecureStore.getItemAsync(REFRESH);
  const raw = await SecureStore.getItemAsync(USER);
  currentUser = raw ? (JSON.parse(raw) as SessionUser) : null;
  return currentUser;
}

export async function saveSession(payload: AuthPayload) {
  accessToken = payload.accessToken;
  refreshToken = payload.refreshToken;
  currentUser = payload.user;
  await SecureStore.setItemAsync(ACCESS, payload.accessToken);
  await SecureStore.setItemAsync(REFRESH, payload.refreshToken);
  await SecureStore.setItemAsync(USER, JSON.stringify(payload.user));
}

export async function updateStoredUser(user: SessionUser) {
  currentUser = user;
  await SecureStore.setItemAsync(USER, JSON.stringify(user));
}

export async function clearSession() {
  const refresh = refreshToken;
  accessToken = null;
  refreshToken = null;
  currentUser = null;
  await SecureStore.deleteItemAsync(ACCESS);
  await SecureStore.deleteItemAsync(REFRESH);
  await SecureStore.deleteItemAsync(USER);
  if (refresh) {
    await fetch(`${apiBaseUrl}/api/auth/logout`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ refreshToken: refresh }),
    }).catch(() => undefined);
  }
}

async function refreshSession() {
  if (!refreshToken) return false;
  const response = await fetch(`${apiBaseUrl}/api/auth/refresh`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ refreshToken }),
  });
  if (!response.ok) {
    await clearSession();
    return false;
  }
  await saveSession((await response.json()) as AuthPayload);
  return true;
}

export async function api<T>(path: string, init: RequestInit = {}, allowRefresh = true): Promise<T> {
  const headers = new Headers(init.headers);
  if (init.body && !headers.has("Content-Type")) headers.set("Content-Type", "application/json");
  if (accessToken) headers.set("Authorization", `Bearer ${accessToken}`);
  let response: Response;
  const controller = new AbortController();
  const timer = setTimeout(() => controller.abort(), 45000);
  try {
    response = await fetch(`${apiBaseUrl}${path}`, { ...init, headers, signal: controller.signal });
  } catch {
    throw new ApiError("Connection unavailable. Please try again.", 0);
  } finally {
    clearTimeout(timer);
  }
  if (response.status === 401 && allowRefresh && refreshToken && !path.startsWith("/api/auth/")) {
    const refreshed = await refreshSession();
    if (refreshed) return api<T>(path, init, false);
  }
  if (response.status === 204) return undefined as T;
  const text = await response.text();
  let body: { detail?: string; title?: string } | null = null;
  if (text) {
    try {
      body = JSON.parse(text) as { detail?: string; title?: string };
    } catch {
      body = null;
    }
  }
  if (!response.ok) {
    throw new ApiError(friendlyError(response.status, body?.detail || body?.title), response.status);
  }
  return (body ?? undefined) as T;
}

export async function login(email: string, password: string) {
  const payload = await api<AuthPayload>("/api/auth/login", {
    method: "POST",
    body: JSON.stringify({ email, password }),
  }, false);
  await saveSession(payload);
  return payload.user;
}

export async function register(fullName: string, email: string, password: string, timezone: string) {
  const payload = await api<AuthPayload>("/api/auth/register", {
    method: "POST",
    body: JSON.stringify({ fullName, email, password, timezone }),
  }, false);
  await saveSession(payload);
  return payload.user;
}

export async function forgotPassword(email: string) {
  return api<{ message: string; developmentResetToken?: string | null }>("/api/auth/forgot-password", {
    method: "POST",
    body: JSON.stringify({ email }),
  }, false);
}

export async function resetPassword(email: string, token: string, newPassword: string) {
  return api<{ message: string }>("/api/auth/reset-password", {
    method: "POST",
    body: JSON.stringify({ email, token, newPassword }),
  }, false);
}
