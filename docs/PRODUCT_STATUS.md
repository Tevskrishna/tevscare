# Product status

Updated after the local MVP polish pass. This is not an App Store or Play Store release.

## What works

- Account: register, login, refresh, logout, password reset token (email is not sent)
- Onboarding and profile, including preferred language
- 15-day starter plan from the database, including the breakfast rotation
- Meal log stored separately from the planned meal
- Water, weight, activity (type, minutes, optional steps), sleep (duration, bedtime, wake time)
- Daily check-in: habits plus optional mood, energy, hunger, and digestion
- Progress for 7, 15, 30, and 90 days, including estimated food spend
- Calendar for a month
- Budget default ₹675, editable
- Shopping lists for 1, 7, 15, and 30 days, with purchased state, quantity, and your price
- Local reminders, including afternoon snack and check-in, with quiet hours
- Provider guidance labeled as guidance
- Photos stay on the phone

## What this pass improved

- Home shows the plan day, next meal, water, activity, sleep, budget remaining, and weight change
- Plan days are marked today, past, logged, or ahead
- Shopping adjustments are saved per list window
- Appearance can follow the system, or stay light or dark
- English copy for the home screen and tabs lives in the localization catalog. Other languages fall back to English
- Notification delivery is local only. The code names that transport so a push adapter can be added later

## Phone access

Expo Go can open the app only while this computer is running and the phone shares its network. A phone on mobile data needs the free staging host in [FREE_HOSTING_OPTIONS.md](FREE_HOSTING_OPTIONS.md). That host does not exist yet, so remote phone access is not available.

## Known limitations

- No phone or emulator walkthrough was completed in this pass
- Translations for Telugu, Hindi, Tamil, Kannada, Malayalam, Bengali, and Marathi are not written
- Password reset does not send email
- Push notifications are not implemented
- Payments are not implemented
- Photos are not uploaded
- An admin website exists for the local API. It covers the dashboard, users, nutritionists, plans, foods, guidance, and audit. Day-by-day meal editing is still an API operation
- Check-in adherence still treats five meals as the planned count
- Shopping actual total only includes lines where you entered a unit price

## Release blockers

See [RELEASE_CHECKLIST.md](RELEASE_CHECKLIST.md).
