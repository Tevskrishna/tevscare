# Admin guide

The desk is `admin/`. It signs in through `POST /api/auth/login` and keeps the access token in `sessionStorage`.

```powershell
cd admin
npm install
npm run dev
```

Open `http://localhost:5173`. The API must already be running at `http://localhost:5080`, or set `VITE_API_URL`.

Development accounts, only when demo seed is on:

| Email | Password | What they can open |
| --- | --- | --- |
| admin@tevscare.app | Admin1234! | Dashboard, users, plans, foods, guidance, audit |
| nutritionist@tevscare.app | Coach1234! | Dashboard, plans, foods, guidance |

A `USER` account is turned away at sign-in.

## What the screens do

- Dashboard shows user, plan, assignment, check-in, weight, and water counts, plus database and API status. It does not show push or payment totals because those providers are not connected.
- Users searches by name or email, opens a profile and recent weight points, and can lock an account. You cannot lock yourself.
- New nutritionist creates an account with the `NUTRITIONIST` role. The password is not written to the audit log.
- Plans lists drafts and published plans. Publish does not rewrite a member’s past logs.
- Foods lists the catalog and can add a food. Leave the price empty when you do not have one.
- Guidance shows provider notes. They are program guidance, not medical advice.
- Audit lists staff actions.

Day and meal edits still use the API described in [API.md](API.md).
