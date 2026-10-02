# Contributing

1. Copy `.env.example` to `.env` and set a local JWT signing key.
2. `docker compose up -d`
3. `dotnet test backend/Tevscare.sln`
4. `cd mobile; npm test; npm run typecheck`

Keep diet text in the database seed or admin API, not in React components.

After a change is reviewed and the tests pass, commit and push that commit. Do not push on every file save. Never commit `.env`, signing keys, or store credentials.

Do not commit `.env`, tokens, or store signing keys.

Prefer a small change in the existing layer over a new service. The API is one process on purpose.
