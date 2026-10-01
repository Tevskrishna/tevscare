Tests stay with the runtime that executes them.

- Backend unit tests: `backend/tests/Tevscare.UnitTests`
- Backend API tests: `backend/tests/Tevscare.Api.Tests` (SQLite, no Docker)
- Mobile planning tests: `mobile/src/lib/planning.test.ts`

From the repository root:

```powershell
dotnet test backend/Tevscare.sln
cd mobile
npm test
npm run typecheck
```
