# TEVSCARE

Meal planning, habit tracking, and a personal food budget. The mobile app reads plans from the API. Plan content is data, not screens.

This is a local MVP: one ASP.NET Core API, PostgreSQL, and an Expo app.

## Prerequisites

- .NET SDK 9
- Node.js 22 or newer, with npm
- Docker Desktop, for local PostgreSQL
- Expo Go, an Android emulator, or an iOS simulator

## 1. Configure environment

```powershell
Copy-Item .env.example .env
```

Replace `Jwt__SigningKey` in `.env` with a local string of at least 32 characters. The database password in the example matches `docker-compose.yml` and is only for this local database.

## 2. Start PostgreSQL

```powershell
docker compose up -d
```

## 3. Start the API

The API applies EF Core migrations and, in Development, seeds the 15-day plan plus demo accounts.

```powershell
cd backend
dotnet run --project src/Tevscare.Api
```

API: `http://localhost:5080`  
Health: `http://localhost:5080/health`  
Swagger: `http://localhost:5080/swagger`

Development accounts, created only when `SeedDemoData` is true:

| Email | Password | Role |
| --- | --- | --- |
| demo@tevscare.app | Demo1234! | USER |
| nutritionist@tevscare.app | Coach1234! | NUTRITIONIST |
| admin@tevscare.app | Admin1234! | ADMIN |

## 4. Start the mobile app

```powershell
cd mobile
npm install
npx expo start
```

Then press `a` for Android or `i` for iOS. On a physical phone, set `EXPO_PUBLIC_API_URL` to your computer's LAN address, for example `http://192.168.1.20:5080`. Android emulator often needs `http://10.0.2.2:5080`.

## 5. Admin desk

The admin site uses the same API. Sign in with the admin or nutritionist development account. A normal member account is rejected.

```powershell
cd admin
npm install
npm run dev
```

Desk: `http://localhost:5173`

A nutritionist can see the dashboard, plans, foods, and guidance. User search, locking, nutritionist creation, and the audit log are admin only.

## 6. Tests

```powershell
dotnet test backend/Tevscare.sln
cd mobile
npm test
npm run typecheck
```

API tests use SQLite and do not need Docker.

## Current limits

The app runs locally. It is not a store release. See [product status](docs/PRODUCT_STATUS.md) and the [release checklist](docs/RELEASE_CHECKLIST.md).

## Docs

- [Architecture](docs/ARCHITECTURE.md)
- [API](docs/API.md)
- [Database](docs/DATABASE.md)
- [Mobile setup](docs/MOBILE_SETUP.md)
- [Deployment](docs/DEPLOYMENT.md)
- [Security](docs/SECURITY.md)
- [Contributing](CONTRIBUTING.md)
