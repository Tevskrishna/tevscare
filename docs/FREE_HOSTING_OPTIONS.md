# Free hosting options

Checked against provider documentation on 2 October 2026. None of these tiers is a promise that the provider will keep the same limits. No account was created from this repository, and no card was entered.

The staging choice, if you create the accounts yourself, is:

- API: Render free web service, Docker, provider HTTPS URL
- Database: Neon free PostgreSQL
- Admin: Cloudflare Pages

Render’s own free PostgreSQL expires after 30 days and then requires a paid upgrade to keep the data. Neon’s free plan does not expire on a calendar. That is why Neon is the database.

## Render web service

| | |
| --- | --- |
| Provider | Render |
| Free tier | Hobby workspace has no monthly fee. Free web services are $0. |
| Credit card | Not required to start. A card is billed only if you add one and then exceed included bandwidth or build minutes. |
| Sleep | Spins down after 15 minutes without traffic. The next request takes about a minute. |
| Database | Not this row. See Neon. Render free Postgres expires 30 days after creation, then 14 days to upgrade before deletion. |
| Bandwidth | Included monthly amount. Extra use can bill a workspace that has a payment method. |
| Build limits | Included pipeline minutes. Extra minutes can bill a workspace that has a payment method. |
| Expiration | 750 free instance hours per workspace per month. After that, free web services are suspended until the next month. |
| Risks | Cold start. 512 MB RAM may be tight for .NET. Not a commercial production host. |
| Migration | Move the same Docker image and connection string to a paid host later. |

Source: [Render free docs](https://render.com/docs/free), [Render FAQ](https://render.com/docs/faq).

## Neon PostgreSQL

| | |
| --- | --- |
| Provider | Neon |
| Free tier | $0. The free plan is not a timed trial. |
| Credit card | Not required. |
| Sleep | Compute scales to zero after 5 minutes. That cannot be turned off on the free plan. |
| Database limits | 0.5 GB storage per project. 100 CU-hours per project per month. Up to 2 CU while active. |
| Bandwidth | 5 GB public network transfer per project per month. |
| Build limits | Not a build host. |
| Expiration | Monthly compute and transfer reset. Hitting a limit suspends compute until the next month, or until you upgrade. |
| Risks | 0.5 GB is enough for this app’s early data and too small for a large production history. |
| Migration | Export with `pg_dump` and restore on another PostgreSQL host. |

Source: [Neon plans](https://neon.com/docs/introduction/plans).

## Cloudflare Pages

| | |
| --- | --- |
| Provider | Cloudflare Pages |
| Free tier | Static sites. Unlimited static requests and bandwidth on the published free limits. |
| Credit card | Not required. |
| Sleep | None for static files. |
| Database | None. The admin site calls the API. |
| Bandwidth | Unlimited on the free Pages plan as published. |
| Build limits | 500 builds per month, one build at a time, 100 projects, 20,000 files. |
| Expiration | No 30-day site expiry is published for the free plan. |
| Risks | The admin build must receive `VITE_API_URL` at build time. |
| Migration | Upload the same `admin/dist` folder anywhere else. |

Source: [Cloudflare Pages limits](https://developers.cloudflare.com/pages/platform/limits/).

## Considered and not selected

| Provider | Why it is not the staging choice |
| --- | --- |
| Koyeb | CREDIT CARD REQUIRED for the documented verification hold. Free Postgres is only 5 active hours per month. |
| Fly.io | CREDIT CARD REQUIRED after a short trial (2 VM hours or 7 days). The old free allowance is gone. |
| Oracle Cloud Always Free | CREDIT CARD REQUIRED for identity verification. PostgreSQL is not the Always Free database; the free database offer is Oracle Autonomous Database and MySQL HeatWave. |
| Render free Postgres | Expires after 30 days, then a paid upgrade is required to keep the data. |
| GitHub Pages | Free for a public repository, with a 1 GB site limit and a 100 GB soft bandwidth limit. GitHub does not allow Pages for a commercial SaaS. Fine as a temporary static preview, not the long-term admin host. |
| Railway | The published free credit is about $1 and is not enough to keep an API running. |

## What this repository did not do

No Render, Neon, Cloudflare, Fly, Oracle, or Railway account was created. The public API is not live.
