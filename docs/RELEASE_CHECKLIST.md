# Release checklist

Do not ship until each item is actually done. None of the store items below are done.

## Must be true before a store build

- [ ] Production API URL is set with `EXPO_PUBLIC_API_URL`. Do not ship `localhost`.
- [ ] `Jwt__SigningKey` is a unique secret in the host secret store, at least 32 characters
- [ ] `Tevscare__SeedDemoData` is false
- [ ] `Tevscare__ExposeResetTokens` is false
- [ ] `Cors__Origins` lists the real app origins. Development currently allows any origin
- [ ] PostgreSQL is a managed database, not the Docker password in `.env.example`
- [ ] Password reset sends mail through a real `IEmailSender`. The current sender does not
- [ ] Privacy policy and terms are published, not the in-app placeholders
- [ ] App icons and splash are final brand assets. The current images are the Expo template
- [ ] Bundle id `app.tevscare.mobile` and Android package `app.tevscare.mobile` are owned by the publisher
- [ ] Notification permission copy is reviewed
- [ ] A device pass covers login, onboarding, home, plan, meal log, water, weight, activity, sleep, progress, shopping, budget, reminders, profile, and settings
- [ ] No `.env` file is in git

## Explicitly not in this build

- Payment collection
- Push notifications
- Apple Health or Health Connect
- Cloud photo storage
- Translated UI copy
- An admin website is available locally. It is not a hosted production desk, and it does not edit every plan day in the browser
