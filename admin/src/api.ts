const configured = (import.meta.env.VITE_API_URL ?? "").replace(/\/$/, "");
const publicHttps = configured.startsWith("https://") && !/localhost|127\.0\.0\.1|192\.168\.|\b10\.\d+\.\d+\.\d+\b|172\.(1[6-9]|2\d|3[0-1])\./i.test(configured);
const devFallback = import.meta.env.DEV ? "http://localhost:5080" : "";
const base = publicHttps ? configured : import.meta.env.DEV ? (configured || devFallback) : "";

export type SessionUser = {
  id: string;
  fullName: string;
  email: string;
  roles: string[];
};

export type Session = {
  accessToken: string;
  refreshToken: string;
  user: SessionUser;
};

type ErrorBody = { title?: string; detail?: string; errors?: Record<string, string[]> };

export class ApiError extends Error {
  status: number;

  constructor(status: number, message: string) {
    super(message);
    this.status = status;
  }
}

function friendlyError(status: number, detail: string) {
  if (status === 401) return "Sign in again.";
  if (status === 403) return "You do not have access to this.";
  if (status === 0 || status >= 500) return "Connection unavailable. Please try again.";
  if (!detail || /localhost|127\.0\.0\.1|exception|stack trace/i.test(detail)) return "That did not work. Please try again.";
  return detail;
}

async function readError(response: Response) {
  const body = (await response.json().catch(() => null)) as ErrorBody | null;
  const field = body?.errors ? Object.values(body.errors).flat().join(" ") : "";
  return friendlyError(response.status, body?.detail || field || body?.title || "");
}

function requireBase() {
  if (!base) {
    throw new ApiError(0, "Connection unavailable. Please try again.");
  }
}

export async function login(email: string, password: string): Promise<Session> {
  requireBase();
  let response: Response;
  try {
    response = await fetch(`${base}/api/auth/login`, {
      method: "POST",
      headers: { "Content-Type": "application/json", Accept: "application/json" },
      body: JSON.stringify({ email, password }),
    });
  } catch {
    throw new ApiError(0, "Connection unavailable. Please try again.");
  }
  if (!response.ok) {
    throw new ApiError(response.status, await readError(response));
  }
  const body = await response.json();
  const roles = (body.user?.roles ?? []) as string[];
  if (!roles.includes("ADMIN") && !roles.includes("NUTRITIONIST")) {
    throw new ApiError(403, "This desk is for admin and nutritionist accounts.");
  }
  return {
    accessToken: body.accessToken,
    refreshToken: body.refreshToken,
    user: {
      id: body.user.id,
      fullName: body.user.fullName,
      email: body.user.email,
      roles
    }
  };
}

export async function api<T>(session: Session, path: string, init: RequestInit = {}, retry = true): Promise<T> {
  const headers = new Headers(init.headers);
  headers.set("Accept", "application/json");
  headers.set("Authorization", `Bearer ${session.accessToken}`);
  if (init.body && !headers.has("Content-Type")) {
    headers.set("Content-Type", "application/json");
  }
  requireBase();
  let response: Response;
  try {
    response = await fetch(`${base}${path}`, { ...init, headers });
  } catch {
    throw new ApiError(0, "Connection unavailable. Please try again.");
  }
  if (response.status === 401 && retry) {
    const refreshed = await fetch(`${base}/api/auth/refresh`, {
      method: "POST",
      headers: { "Content-Type": "application/json", Accept: "application/json" },
      body: JSON.stringify({ refreshToken: session.refreshToken })
    });
    if (refreshed.ok) {
      const body = await refreshed.json();
      session.accessToken = body.accessToken;
      session.refreshToken = body.refreshToken;
      return api<T>(session, path, init, false);
    }
  }
  if (response.status === 204) {
    return undefined as T;
  }
  if (!response.ok) {
    throw new ApiError(response.status, await readError(response));
  }
  return response.json() as Promise<T>;
}

export function isAdmin(session: Session) {
  return session.user.roles.includes("ADMIN");
}
