# Deployment architecture

The phone must reach a public HTTPS API. It must not depend on this computer, Expo Go, or a shared Wi-Fi network.

```
Phone or admin browser
        |
        | HTTPS
        v
Hosted ASP.NET Core API
        |
        v
Managed PostgreSQL
```

`https://api.tevscare.com` is an example name only. That domain is not registered by this repository.

## Environments

| | Development | Staging | Production |
| --- | --- | --- | --- |
| API | This computer, `http://localhost:5080` | Hosted HTTPS URL | Hosted HTTPS URL |
| Database | Docker PostgreSQL | Managed PostgreSQL | Managed PostgreSQL |
| Demo accounts | Yes, when `SeedDemoData` is true | No | No, even if the flag is set |
| Mobile URL | `EXPO_PUBLIC_API_URL` in `mobile/.env` | EAS preview env var | EAS production env var |
| Admin URL | `VITE_API_URL` or localhost | Host env var | Host env var |

Business code does not contain a production host name. The API reads connection strings and the JWT key from the environment. The mobile app reads `EXPO_PUBLIC_API_URL`. Staging and production builds refuse a URL that is not `https`.

## What stays local

`dotnet run`, Docker Compose, Expo Go, and a LAN address are development tools. A store build does not use them.

## API process

`backend/Dockerfile` publishes the existing API. The container listens on HTTP port 8080. The host in front of it terminates TLS and should send `X-Forwarded-Proto`. Production sets `Tevscare__TrustForwardedHeaders=true` for that proxy. Do not turn that on for a process exposed directly to the internet.

Startup applies EF migrations when `Tevscare__ApplyMigrations` is true, which is the default. That is safe for one API instance. It does not drop tables. For more than one instance, set the flag to false and run migrations as a release step.

The catalog seed (foods and the 15-day plan) still runs so a new database has plan content. Demo logins run only in non-production when `SeedDemoData` is true.

## Health

- `GET /health` — process is up
- `GET /health/ready` — database connection works. Returns 503 without a connection string or exception text

## Not deployed

No Render, Neon, Cloudflare, domain, or store account is attached to this repository. The public internet cannot reach this API yet. See [FREE_HOSTING_OPTIONS.md](FREE_HOSTING_OPTIONS.md).
