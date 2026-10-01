import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import * as Linking from "expo-linking";
import { useState } from "react";
import { Switch, Text, View } from "react-native";
import { api } from "../../src/api/client";
import { Screen } from "../../src/components/Screen";
import { AppHeader, PrimaryButton, SecondaryButton, useColors } from "../../src/components/ui";
import { explainAndRequestNotifications, notificationPermissionState, syncLocalReminders } from "../../src/lib/notifications";

type Preference = { category: string; enabled: boolean; localTime?: string | null; intervalMinutes?: number | null; windowStart?: string | null; windowEnd?: string | null };
type Settings = { quietHoursEnabled: boolean; quietStart: string; quietEnd: string; preferences: Preference[] };

export default function NotificationsScreen() {
  const colors = useColors();
  const queryClient = useQueryClient();
  const [permission, setPermission] = useState("checking");
  const settings = useQuery({
    queryKey: ["notifications"],
    queryFn: async () => {
      setPermission(await notificationPermissionState());
      return api<Settings>("/api/notifications/preferences");
    },
  });
  const save = useMutation({
    mutationFn: (next: Settings) => api("/api/notifications/preferences", { method: "PUT", body: JSON.stringify(next) }),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ["notifications"] });
      await syncLocalReminders();
    },
  });

  const data = settings.data;
  return (
    <Screen>
      <AppHeader title="Reminders" subtitle="Notifications stay on this phone. They remind you to log the day. They do not give medical instructions. Permission is requested once, after this explanation." />
      <Text style={{ fontFamily: "Jakarta", color: colors.muted }}>Permission: {permission}</Text>
      {permission === "denied" ? <SecondaryButton label="Open system settings" onPress={() => Linking.openSettings()} /> : null}
      <PrimaryButton label="Allow reminders" onPress={async () => setPermission(await explainAndRequestNotifications())} />
      {data?.preferences.map((item) => (
        <View key={item.category} style={{ minHeight: 56, flexDirection: "row", alignItems: "center", justifyContent: "space-between" }}>
          <Text style={{ fontFamily: "JakartaSemi", color: colors.ink }}>{item.category}{item.localTime ? ` · ${item.localTime}` : ""}</Text>
          <Switch
            accessibilityLabel={`${item.category} reminders`}
            value={item.enabled}
            onValueChange={(enabled) => {
              if (!data) return;
              save.mutate({ ...data, preferences: data.preferences.map((preference) => preference.category === item.category ? { ...preference, enabled } : preference) });
            }}
          />
        </View>
      ))}
      {data ? <Text style={{ fontFamily: "Jakarta", color: colors.muted }}>Quiet hours {data.quietStart}–{data.quietEnd}</Text> : null}
      <SecondaryButton label="Schedule on this phone" onPress={() => syncLocalReminders().then(setPermission)} />
    </Screen>
  );
}
