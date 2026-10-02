const base = (import.meta.env.VITE_API_URL || "http://localhost:5080").replace(/\/$/, "");

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

async function readError(response: Response) {
  const body = (await response.json().catch(() => null)) as ErrorBody | null;
  const field = body?.errors ? Object.values(body.errors).flat().join(" ") : "";
  return body?.detail || field || body?.title || `Request failed (${response.status})`;
}

export async function login(email: string, password: string): Promise<Session> {
  const response = await fetch(`${base}/api/auth/login`, {
    method: "POST",
    headers: { "Content-Type": "application/json", Accept: "application/json" },
    body: JSON.stringify({ email, password })
  });
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
  const response = await fetch(`${base}${path}`, { ...init, headers });
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
