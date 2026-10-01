import { useQuery } from "@tanstack/react-query";
import { router } from "expo-router";
import { api, clearSession, getCurrentUser } from "../../../src/api/client";
import { Screen } from "../../../src/components/Screen";
import { AppHeader, Card, ConfirmationModal, SecondaryButton, useColors } from "../../../src/components/ui";
import { useSession } from "../../../src/lib/session";
import { useState } from "react";
import { Text } from "react-native";

type Profile = { fullName: string; email: string; age?: number; heightCm?: number; currentWeightKg?: number; targetWeightKg?: number; dietaryPreference: string; timezone: string; entitlementPlan: string };

const links = [
  ["Budget", "/(app)/budget"],
  ["Shopping list", "/(app)/shopping"],
  ["Foods", "/(app)/foods"],
  ["Notifications", "/(app)/notifications"],
  ["Guidance", "/(app)/guidance"],
  ["Calendar", "/(app)/calendar"],
  ["Settings", "/(app)/settings"],
  ["Privacy", "/(app)/privacy"],
  ["About", "/(app)/about"],
] as const;

export default function ProfileScreen() {
  const colors = useColors();
  const [confirm, setConfirm] = useState(false);
  const profile = useQuery({ queryKey: ["profile"], queryFn: () => api<Profile>("/api/profile") });
  const user = profile.data;
  return (
    <Screen>
      <AppHeader title={user?.fullName ?? getCurrentUser()?.fullName ?? "Profile"} subtitle={user ? `${user.email} · ${user.timezone}` : ""} />
      {user ? <Text style={{ fontFamily: "Jakarta", color: colors.muted }}>{user.dietaryPreference} · {user.currentWeightKg ?? "—"} kg now · goal {user.targetWeightKg ?? "—"} kg · {user.entitlementPlan} plan</Text> : null}
      {links.map(([label, href]) => (
        <Card key={label} onPress={() => router.push(href)}>
          <Text style={{ fontFamily: "JakartaSemi", fontSize: 16, color: colors.ink }}>{label}</Text>
        </Card>
      ))}
      <SecondaryButton label="Edit profile" onPress={() => router.push("/(app)/onboarding")} />
      <SecondaryButton label="Sign out" onPress={() => setConfirm(true)} />
      <ConfirmationModal visible={confirm} title="Sign out?" body="You can sign in again on this device." confirmLabel="Sign out" onClose={() => setConfirm(false)} onConfirm={async () => { await clearSession(); useSession.getState().setUser(null); router.replace("/(auth)/welcome"); }} />
    </Screen>
  );
}
