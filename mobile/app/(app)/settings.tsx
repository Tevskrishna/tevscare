import Constants from "expo-constants";
import { useState } from "react";
import { Pressable, Text, View } from "react-native";
import { api, clearSession, getCurrentUser, updateStoredUser } from "../../src/api/client";
import { Screen } from "../../src/components/Screen";
import { TevsBrand } from "../../src/components/TevsBrand";
import { AppHeader, ConfirmationModal, PrimaryButton, SearchInput, useColors } from "../../src/components/ui";
import { router } from "expo-router";
import { useTranslation } from "react-i18next";
import i18n from "../../src/lib/i18n";
import { AppearanceMode, useAppearance } from "../../src/lib/appearance";
import { useSession } from "../../src/lib/session";

const locales = ["en", "te", "hi", "ta", "kn", "ml", "bn", "mr"];

export default function SettingsScreen() {
  const colors = useColors();
  const { t } = useTranslation();
  const appearance = useAppearance((state) => state.mode);
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
      <Text style={{ fontFamily: "JakartaSemi", color: colors.ink }}>Appearance</Text>
      <View style={{ flexDirection: "row", gap: 8 }}>
        {(["system", "light", "dark"] as AppearanceMode[]).map((mode) => (
          <Pressable key={mode} accessibilityRole="button" accessibilityState={{ selected: appearance === mode }} onPress={() => useAppearance.getState().setMode(mode)} style={{ minHeight: 44, paddingHorizontal: 12, justifyContent: "center" }}>
            <Text style={{ fontFamily: "JakartaSemi", color: appearance === mode ? colors.primary : colors.ink }}>{mode}</Text>
          </Pressable>
        ))}
      </View>
      <Text style={{ fontFamily: "Jakarta", color: colors.muted }}>Security: the session token stays in secure storage on this phone. Sign out or delete the account to remove it.</Text>
      <Text style={{ fontFamily: "Jakarta", color: colors.muted }}>Reminders stay on this phone. A remote push service is not connected.</Text>
      <Text style={{ fontFamily: "Jakarta", color: colors.muted }}>Version {Constants.expoConfig?.version ?? "1.0.0"}</Text>
      <TevsBrand />
      <View style={{ flexDirection: "row", flexWrap: "wrap", gap: 8 }}>
        {locales.map((locale) => (
          <Pressable key={locale} accessibilityRole="button" onPress={async () => { await i18n.changeLanguage(locale); const profile = await api<Record<string, unknown>>("/api/profile"); await api("/api/profile", { method: "PUT", body: JSON.stringify({ ...profile, preferredLanguage: locale }) }); }} style={{ minHeight: 44, paddingHorizontal: 12, justifyContent: "center" }}>
            <Text style={{ fontFamily: "JakartaSemi", color: colors.primary }}>{locale}</Text>
          </Pressable>
        ))}
      </View>
      <PrimaryButton label="Delete account" onPress={() => setConfirm(true)} />
      <ConfirmationModal visible={confirm} title="Delete this account?" body="Logs, plan assignment and the login are removed. This cannot be undone from the app." confirmLabel="Delete" onClose={() => setConfirm(false)} onConfirm={async () => { await api("/api/profile", { method: "DELETE" }); await clearSession(); useSession.getState().setUser(null); router.replace("/(auth)/welcome"); }} />
    </Screen>
  );
}
