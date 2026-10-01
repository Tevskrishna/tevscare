# Deployment

Local is Docker PostgreSQL plus `dotnet run` and Expo. A later host can be Azure App Service or any container that runs the API, with managed PostgreSQL.

## What must change outside this repo

- Set `ASPNETCORE_ENVIRONMENT=Production`.
- Set `ConnectionStrings__Default` to the managed database.
- Set `Jwt__SigningKey` to a unique secret of at least 32 characters, stored in the host's secret store.
- Set `Tevscare__SeedDemoData=false` and `Tevscare__ExposeResetTokens=false`.
- Set `Cors__Origins` to the real app origins. Development allows any origin. Production does not.
- Put a real SMTP implementation behind `IEmailSender`. The current sender logs that a reset was requested and does not log the message body.
- Build the Expo app with EAS or the native toolchains when you are ready for store binaries. This repository does not include store credentials.

## Migrations

Startup runs `MigrateAsync` against PostgreSQL. That is acceptable for a single API instance. If you run more than one instance, apply migrations as a release step instead of racing them at boot.

## Payments

Do not add a provider by hard-coding it into profile or plan code. Add a billing adapter that writes `Entitlement` rows (`source`, `status`, start, end). Cancellation is an entitlement status change. No payment code exists yet.
