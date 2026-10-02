# Notifications

Reminders are scheduled on the phone with Expo local notifications. They work without a server and without a paid push service.

Water, meals, afternoon snack, activity, sleep, and check-in are preference rows on the account. Quiet hours are stored with those preferences.

## Remote push

`IPushNotificationSender` is the future seam. The current class does not contact Expo, Firebase, or Apple. It does not send a notification.

A later provider would need:

- Android: a Firebase Cloud Messaging project, or Expo push credentials
- iOS: an Apple Developer account and an APNs key
- A device push token stored on the account

None of those accounts or keys are in this repository. Do not add a paid notification vendor for the current free staging phase.

Password-reset mail is separate. See the security notes. The reset message includes “Powered by TEVS” in the body that would be sent. The log records only the subject.
