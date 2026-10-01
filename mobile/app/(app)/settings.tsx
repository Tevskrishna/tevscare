import { useState } from "react";
import { Pressable, Text, View } from "react-native";
import { api, clearSession, getCurrentUser, updateStoredUser } from "../../src/api/client";
import { Screen } from "../../src/components/Screen";
import { AppHeader, ConfirmationModal, PrimaryButton, SearchInput, useColors } from "../../src/components/ui";
import { router } from "expo-router";
import { useTranslation } from "react-i18next";
import i18n from "../../src/lib/i18n";
import { useSession } from "../../src/lib/session";

const locales = ["en", "te", "hi", "ta", "kn", "ml", "bn", "mr"];

export default function SettingsScreen() {
  const colors = useColors();
  const { t } = useTranslation();
  const user = getCurrentUser();
  const [timezone, setTimezone] = useState(user?.timezone ?? "Asia/Kolkata");
  const [confirm, setConfirm] = useState(false);
  return (
    <Screen>
      <AppHeader title={t("settings")} subtitle="Timezone controls meal days, reminders and daily summaries. English is the only completed language. Other locales fall back to English until translations are added." />
      <SearchInput value={timezone} onChangeText={setTimezone} placeholder="Timezone, for example Asia/Kolkata" />
      <PrimaryButton label="Save timezone" onPress={async () => {
        const profile = await api<{ fullName: string; age: number; heightCm: number; currentWeightKg: number; targetWeightKg: number; dietaryPreference: string; activityLevel: string; waterGoalMl: number; sleepGoalMinutes: number; activityGoalMinutes: number; allergies: string[]; foodPreferences: { name: string; kind: string }[]; wakeTime: string; breakfastTime: string; lunchTime: string; dinnerTime: string; sleepTime: string }>("/api/profile");
        await api("/api/profile", { method: "PUT", body: JSON.stringify({ ...profile, timezone }) });
        if (user) await updateStoredUser({ ...user, timezone });
      }} />
      <View style={{ flexDirection: "row", flexWrap: "wrap", gap: 8 }}>
        {locales.map((locale) => (
          <Pressable key={locale} accessibilityRole="button" onPress={() => i18n.changeLanguage(locale)} style={{ minHeight: 44, paddingHorizontal: 12, justifyContent: "center" }}>
            <Text style={{ fontFamily: "JakartaSemi", color: colors.primary }}>{locale}</Text>
          </Pressable>
        ))}
      </View>
      <PrimaryButton label="Delete account" onPress={() => setConfirm(true)} />
      <ConfirmationModal visible={confirm} title="Delete this account?" body="Logs, plan assignment and the login are removed. This cannot be undone from the app." confirmLabel="Delete" onClose={() => setConfirm(false)} onConfirm={async () => { await api("/api/profile", { method: "DELETE" }); await clearSession(); useSession.getState().setUser(null); router.replace("/(auth)/welcome"); }} />
    </Screen>
  );
}
