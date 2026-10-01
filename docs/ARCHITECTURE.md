# Architecture

TEVSCARE is a modular monolith plus an Expo client.

```
/backend     ASP.NET Core API, domain, application, infrastructure
/mobile      Expo Router app
/docs        Setup and design notes
/tests       Pointers only. Test projects stay next to their runtime.
```

A single `/tests` runner would not compile both C# and TypeScript. Backend tests live in `backend/tests`. Mobile tests live next to the functions they cover.

## Backend layers

- **Domain** — entities, enums, roles. No EF or ASP.NET references.
- **Application** — DTOs, FluentValidation, pure calculators for water, adherence, budget, shopping, and reminder times.
- **Infrastructure** — EF Core, Identity, JWT issuing, seeding, and the service implementations.
- **Api** — HTTP, auth scheme, exception shape, rate limit on auth routes.

`ApplicationUser` is an Identity user in Infrastructure. Domain records store `UserId` as a `Guid` so the domain does not reference Identity.

## Content vs app logic

Foods, the 15-day plan, guidance, and habits are seeded rows. Screens render `/api/diet-plans` and `/api/dashboard`. Changing a meal is an admin API write, not a mobile release.

Provider notes such as “avoid potato” or “mutton is not in this plan” are guidance rows or food notes. They are not applied as universal medical rules. Thyroid and kidney notes carry a condition key and are not auto-applied.

## Time and money

Timestamps are UTC. `User.Timezone` (default `Asia/Kolkata`) decides the local day, meal window, and reminder clock.

Food prices are `Food.ReferencePriceInr` unless the user saved `UserFoodPrice`. Missing prices stay missing. The default daily budget is ₹675 and is user-editable.

## Entitlements

`Entitlement` is plan + status + source. Registration grants `Free` with source `system`. An admin can grant `Premium` or `Nutritionist` with source `manual`. There is no payment provider and no fake charge.

## Offline

The phone caches the dashboard and plan JSON. Water, weight, activity, sleep, and meal logs that fail because the network is down are queued in AsyncStorage and sent on the next launch. Validation failures are dropped.

## Notifications

The API returns a daily schedule with quiet hours already considered by the schedule builder. The phone schedules those times with `expo-notifications`. Push delivery is a later adapter. The permission prompt runs once, after the notifications screen explains why.

## Localization

`i18next` is initialized for English, Telugu, Hindi, Tamil, Kannada, Malayalam, Bengali, and Marathi. Only English copy exists. Other locales fall back to English. UI components are not copied per language.
