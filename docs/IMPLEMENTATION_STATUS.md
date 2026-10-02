# Implementation status

Updated after the admin desk was added to the existing local MVP. This is not a store release.

## DONE

- Account registration, login, refresh, logout, and password reset tokens
- Onboarding, profile, and account deletion
- Data-driven 15-day plan, meal log separate from the planned meal
- Water, weight, activity, sleep, check-in, progress (7/15/30/90), calendar
- Shopping lists for 1, 7, 15, and 30 days, with purchased state and an entered price
- Budget stored per user. The seed default is ₹675 and can be changed. It is not a hardcoded rule in the calculators beyond that starting value
- Food search, guidance labeled as program guidance, entitlements without a payment charge
- Local reminders. English UI strings with other languages falling back to English
- Photos stay on the phone
- JWT, refresh rotation, FluentValidation, rate limits on auth, security headers
- `IEmailSender` that records the subject and does not log the message body
- Admin and nutritionist write APIs for foods, plans, days, meals, publish, assign, and entitlement grant
- Admin web desk at `admin/` for dashboard, users, nutritionists, plans, foods, guidance, and audit
- Admin read APIs: dashboard counts, user search, user detail, plan list, guidance list, audit list
- Audit rows for plan, food, nutritionist, and lock actions. Passwords are not stored in the audit detail

## PARTIAL

- Plans can be 1–90 days. The seeded program is the 15-day starter. There is no separate published 30/60/90 program yet
- Meal edit exists on the API. The admin desk can create a draft plan and publish it. Day and meal editing is still an API call, not a full desk screen
- Localization catalogs are prepared. Only English copy is written
- Email sending is an abstraction. No SMTP or provider is configured
- Entitlements can be granted manually. There is no payment provider
- Push notifications are named as a future transport. Delivery is local only
- Nutritionist accounts can be created. Assigning a member to a nutritionist is not a separate relationship; plan assignment already exists
- Translation management for dynamic content is not a desk screen
- Reminder templates are the user’s notification preferences, not admin-authored templates

## MISSING

- Store icons, splash, bundle ownership, and a device walkthrough
- Professionally reviewed Telugu, Hindi, Tamil, Kannada, Malayalam, Bengali, and Marathi copy
- A configured email provider
- A configured push provider
- A configured payment provider
- Cloud photo storage
- Admin screens for every tracking history chart, system configuration editor, and translation editor
- Production hosting, backups, and crash monitoring

## BLOCKED

- Production email, push, and payments are blocked on external credentials. They are not faked
- A phone or emulator pass was not run in this change
- Other languages stay on the English fallback until real copy is supplied

## TESTED

- `dotnet test backend/Tevscare.sln` includes the admin authorization test: a member is rejected, an admin can read the dashboard and create a nutritionist, and that nutritionist cannot list users

## NOT TESTED HERE

- Admin web click-through against a running API
- Expo on a device or emulator
