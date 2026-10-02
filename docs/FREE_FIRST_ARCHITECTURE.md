# Free-first architecture

TEVSCARE stays free until you choose a store listing or a paid host. No payment provider, paid email, or paid push service is connected.

## Channels

| Channel | Where it runs | Phone requirement |
| --- | --- | --- |
| Development | This computer, Docker PostgreSQL, Expo | Same network as the computer, or a simulator. Expo Go is optional. |
| Staging | Render free API, Neon free PostgreSQL, Cloudflare Pages admin | Any internet connection, after those accounts exist. Expo Go is not required. |
| Production | A future paid host and a store build | A standalone Android or iOS app. Not built yet. |

Development may use `http://localhost:5080` or a LAN address in `mobile/.env`. Staging and production builds refuse those addresses. They read `EXPO_PUBLIC_API_URL` from the EAS preview or production environment. That variable is not stored in git.

## What is free now

- The application code
- Local Docker PostgreSQL
- GitHub for the public repository and CI
- The selected staging services, after you create the accounts and stay inside their free limits

## What is not connected

- A public HTTPS API
- A remote database
- A custom domain. `https://api.tevscare.com` is only an example name.
- Email delivery. Password reset is stored and, in Development, the token can be returned to the app. The email body is not logged. A future SMTP or HTTPS mail provider implements `IEmailSender`.
- Remote push. Reminders are local notifications. `IPushNotificationSender` does not call a provider. A future setup needs an Expo push token or FCM/APNs credentials.
- Payments. Plans can be Free, Trial, Premium, or Expired. Granting one does not charge a card.

## Staging shape

```
Phone or admin browser
        |
        | HTTPS, any network
        v
Render free web service
        |
        v
Neon free PostgreSQL
```

The computer does not stay on. The phone does not need the home Wi-Fi. That path works only after the accounts exist and the health URL answers from the public internet. It does not answer yet.
