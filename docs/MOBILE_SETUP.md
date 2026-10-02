# Mobile setup

The app is Expo SDK 57, Expo Router, TypeScript, TanStack Query, Zustand, React Hook Form, and Zod.

```powershell
cd mobile
npm install
npx expo start
```

`package.json` `main` is `expo-router/entry`. Do not restore `App.tsx` as the entry.

## API address

Copy `mobile/.env.example` to `mobile/.env`. Expo reads that file, not the repository-root `.env`. `EXPO_PUBLIC_API_URL` defaults to `http://localhost:5080` for development. Expo Go is a development tool. A staging or production build reads a public `https` URL from EAS and does not use Expo Go.

- iOS simulator: localhost works.
- Android emulator: `http://10.0.2.2:5080`
- Physical device: your computer's LAN IP, and the API must allow that origin. Development CORS allows any origin.

## Screens

Splash and session restore, welcome, register, login, forgot password, onboarding, home, plan, track, progress, profile, meal detail, meal log, water, weight, activity, sleep, check-in, calendar, shopping, budget, foods, notifications, guidance, settings, privacy, about.

Tabs: Home, Plan, Track, Progress, Profile.

## Notifications

Open Notifications, read the explanation, then allow. The app asks once. If the system denies permission, the screen links to settings. Scheduling uses the daily times returned by the API and skips quiet hours on the device as well.

## What this environment cannot do

`npm test` and `npm run typecheck` do not boot a phone. A device or emulator is required to click through the UI.
