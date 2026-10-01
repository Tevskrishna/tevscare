# API

Base URL locally: `http://localhost:5080`. JSON is camelCase. Authenticated routes expect `Authorization: Bearer <accessToken>`.

Errors use `{ title, status, detail }`. Validation failures return 400. Unexpected errors return a generic detail in production. The Testing environment adds the exception type so API tests can diagnose failures.

## Auth

- `POST /api/auth/register` — fullName, email, password, timezone
- `POST /api/auth/login`
- `POST /api/auth/refresh` — rotates the refresh token
- `POST /api/auth/logout`
- `POST /api/auth/forgot-password` — always a generic message. Development may include `developmentResetToken`.
- `POST /api/auth/reset-password`

Auth routes are limited to 30 requests per minute per client.

## Profile and today

- `GET /api/profile` `PUT /api/profile` `DELETE /api/profile`
- `GET /api/dashboard?date=yyyy-MM-dd`
- `GET /api/diet-plans`
- `GET /api/diet-plans/{id}`
- `GET /api/diet-plans/{id}/days/{day}`
- `GET /api/meals/today`
- `POST /api/meals/log` — mealType, status (`Completed`, `Partial`, `Skipped`), mealId, items
- `GET /api/water/today` `POST /api/water` `DELETE /api/water/{id}`
- `GET /api/weight?days=90` `POST /api/weight`
- `GET /api/activity` `POST /api/activity`
- `GET /api/sleep` `POST /api/sleep`
- `GET /api/check-in` `POST /api/check-in`
- `GET /api/progress?range=7|15|30|90`
- `GET /api/calendar?month=yyyy-MM`

## Money, food, reminders

- `GET /api/budget` `POST /api/budget` `POST /api/budget/prices`
- `GET /api/shopping-list?days=1|7|15|30` `POST /api/shopping-list/toggle`
- `GET /api/foods?query=&category=&page=&pageSize=`
- `GET /api/foods/categories` `GET /api/foods/{id}`
- `GET /api/notifications/preferences` `PUT /api/notifications/preferences`
- `GET /api/guidance`
- `GET /api/entitlements/me`
- `POST /api/analytics/events` — name allow-list. Password and token properties are rejected.

## Admin

`ADMIN` or `NUTRITIONIST`, except entitlement grant which is `ADMIN` only.

- `POST /api/admin/foods`
- `POST /api/admin/plans`
- `POST /api/admin/plans/{planId}/days`
- `POST /api/admin/plans/{planId}/days/{day}/meals`
- `PUT /api/admin/meals/{mealId}`
- `POST /api/admin/plans/{planId}/publish`
- `POST /api/admin/plans/assign`
- `POST /api/admin/entitlements`

`GET /health` is anonymous.
