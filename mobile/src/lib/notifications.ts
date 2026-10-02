import AsyncStorage from "@react-native-async-storage/async-storage";
import * as Notifications from "expo-notifications";
import { Platform } from "react-native";
import { api } from "../api/client";
import { isQuietTime } from "./planning";

export const notificationTransport = "local" as const;

const ASKED = "tevscare.notification-asked";

Notifications.setNotificationHandler({
  handleNotification: async () => ({
    shouldShowBanner: true,
    shouldShowList: true,
    shouldPlaySound: true,
    shouldSetBadge: false,
  }),
});

export async function notificationPermissionState() {
  if (Platform.OS === "web") return "unavailable" as const;
  const current = await Notifications.getPermissionsAsync();
  if (current.granted) return "granted" as const;
  if (current.status === "denied" && !current.canAskAgain) return "denied" as const;
  return "undetermined" as const;
}

export async function explainAndRequestNotifications() {
  const asked = await AsyncStorage.getItem(ASKED);
  const current = await notificationPermissionState();
  if (current === "granted" || current === "unavailable" || current === "denied") return current;
  if (asked === "yes") return current;
  await AsyncStorage.setItem(ASKED, "yes");
  const next = await Notifications.requestPermissionsAsync();
  return next.granted ? "granted" as const : "denied" as const;
}

type Reminder = { title: string; body: string; time: string; enabled: boolean };

export async function syncLocalReminders() {
  const permission = await notificationPermissionState();
  if (permission !== "granted") return permission;
  const settings = await api<{ quietHoursEnabled: boolean; quietStart: string; quietEnd: string; schedule: Reminder[] }>("/api/notifications/preferences");
  await Notifications.cancelAllScheduledNotificationsAsync();
  for (const reminder of settings.schedule.filter((item) => item.enabled)) {
    const [hour, minute] = reminder.time.split(":").map(Number);
    if (isQuietTime(hour, minute, settings.quietStart, settings.quietEnd, settings.quietHoursEnabled)) continue;
    await Notifications.scheduleNotificationAsync({
      content: { title: reminder.title, body: reminder.body },
      trigger: { type: Notifications.SchedulableTriggerInputTypes.DAILY, hour, minute },
    });
  }
  return permission;
}
