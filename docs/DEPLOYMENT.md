# Deployment

This prepares the existing API for a host. It does not create the host.

## Required environment

Set these on the host. Do not commit them.

| Variable | Required | Notes |
| --- | --- | --- |
| `ASPNETCORE_ENVIRONMENT` | yes | `Production` or `Staging` |
| `ConnectionStrings__Default` | yes | Managed PostgreSQL connection string |
| `Jwt__SigningKey` | yes | Unique, at least 32 characters |
| `Jwt__Issuer` | no | Default `tevscare` |
| `Jwt__Audience` | no | Default `tevscare-mobile` |
| `Tevscare__SeedDemoData` | no | Ignored in Production. Keep false in Staging |
| `Tevscare__ExposeResetTokens` | no | Must be false outside Development |
| `Tevscare__ApplyMigrations` | no | Default true. Set false if several instances start together |
| `Tevscare__TrustForwardedHeaders` | no | True only behind the platform TLS proxy |
| `Cors__Origins__0` | for admin | Public admin site origin. Native apps do not need CORS |

## Container

From the repository root, after Docker is available:

```powershell
docker build -f backend/Dockerfile -t tevscare-api .
docker run --rm -p 8080:8080 --env-file .env tevscare-api
```

Do not use the local `.env` for a real host. The image does not contain `.env`.

The free staging path is a Render web service, a Neon database, and a Cloudflare Pages admin site. `render.yaml` is a blueprint only. It has not been applied. Set the Render service port to 8080, because the container listens there. Paste the Neon connection string into `ConnectionStrings__Default`. Set `Cors__Origins__0` to the Cloudflare Pages origin after that site exists. Do not point staging at the local Docker password.

Fly.io and Koyeb ask for a credit card. They are not part of this setup.

## Mobile builds

No store build has been generated. After `EXPO_PUBLIC_API_URL` is set in the Expo preview and production environments:

```powershell
cd mobile
npx eas build --platform android --profile preview
npx eas build --platform android --profile production
npx eas build --platform ios --profile production
```

Preview is the free staging app. Production is the future store build. Expo Go remains a development tool. `eas.json` does not contain the API URL. Apple and Google signing credentials stay in EAS or the store consoles.

`app.tevscare.mobile` is the bundle identifier and Android package. It is not registered in a store from this repository.

## Admin

Build with `VITE_API_URL` set to the same public API:

```powershell
cd admin
npm ci
npm run build
```

Host the `admin/dist` files on any static host. Add that site origin to `Cors__Origins`.

## GitHub

`.github/workflows/ci.yml` builds and tests on pushes and pull requests to `master`. It does not deploy.
