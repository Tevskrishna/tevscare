import { useMutation } from "@tanstack/react-query";
import { router } from "expo-router";
import { useState } from "react";
import { Pressable, Text, View } from "react-native";
import { ApiError, api, getCurrentUser, updateStoredUser } from "../../src/api/client";
import { Screen } from "../../src/components/Screen";
import { AppHeader, ErrorState, PrimaryButton, SearchInput, useColors } from "../../src/components/ui";
import { track } from "../../src/lib/analytics";
import { useSession } from "../../src/lib/session";

const diets = ["Vegetarian", "Eggetarian", "NonVegetarian", "Vegan"];
const activities = ["Sedentary", "Light", "Moderate", "Active", "VeryActive"];

export default function OnboardingScreen() {
  const colors = useColors();
  const user = getCurrentUser();
  const [fullName, setFullName] = useState(user?.fullName ?? "");
  const [age, setAge] = useState("30");
  const [heightCm, setHeightCm] = useState("165");
  const [currentWeightKg, setCurrentWeightKg] = useState("70");
  const [targetWeightKg, setTargetWeightKg] = useState("65");
  const [dietaryPreference, setDietaryPreference] = useState("Vegetarian");
  const [activityLevel, setActivityLevel] = useState("Moderate");
  const [allergies, setAllergies] = useState("");
  const [preferences, setPreferences] = useState("");
  const [waterGoalMl, setWaterGoalMl] = useState("2500");
  const [sleepGoalMinutes, setSleepGoalMinutes] = useState("480");
  const [activityGoalMinutes, setActivityGoalMinutes] = useState("30");
  const [error, setError] = useState<string | null>(null);

  const save = useMutation({
    mutationFn: () => api("/api/profile", {
      method: "PUT",
      body: JSON.stringify({
        fullName: fullName.trim(),
        age: Number(age),
        heightCm: Number(heightCm),
        currentWeightKg: Number(currentWeightKg),
        targetWeightKg: Number(targetWeightKg),
        dietaryPreference,
        activityLevel,
        waterGoalMl: Number(waterGoalMl),
        sleepGoalMinutes: Number(sleepGoalMinutes),
        activityGoalMinutes: Number(activityGoalMinutes),
        timezone: user?.timezone || "Asia/Kolkata",
        allergies: allergies.split(",").map((item) => item.trim()).filter(Boolean),
        foodPreferences: preferences.split(",").map((item) => item.trim()).filter(Boolean).map((name) => ({ name, kind: "Like" })),
        wakeTime: "07:00",
        breakfastTime: "08:00",
        lunchTime: "13:00",
        dinnerTime: "20:00",
        sleepTime: "22:30",
      }),
    }),
    onSuccess: async () => {
      if (user) {
        const next = { ...user, fullName: fullName.trim(), onboardingCompleted: true };
        await updateStoredUser(next);
        useSession.getState().setUser(next);
      }
      track("onboarding_completed");
      router.replace("/(app)/(tabs)");
    },
    onError: (caught) => setError(caught instanceof ApiError ? caught.message : "Profile could not be saved."),
  });

  return (
    <Screen>
      <AppHeader title="Set up your plan" subtitle="These details personalise meals, water and reminders. You can change them later." />
      <SearchInput value={fullName} onChangeText={setFullName} placeholder="Name" />
      <SearchInput value={age} onChangeText={setAge} placeholder="Age" />
      <SearchInput value={heightCm} onChangeText={setHeightCm} placeholder="Height in cm" />
      <SearchInput value={currentWeightKg} onChangeText={setCurrentWeightKg} placeholder="Current weight in kg" />
      <SearchInput value={targetWeightKg} onChangeText={setTargetWeightKg} placeholder="Target weight in kg" />
      <Text style={{ fontFamily: "JakartaSemi", color: colors.ink }}>Dietary preference</Text>
      <View style={{ flexDirection: "row", flexWrap: "wrap", gap: 8 }}>
        {diets.map((item) => (
          <Pressable key={item} accessibilityRole="button" onPress={() => setDietaryPreference(item)} style={{ minHeight: 44, paddingHorizontal: 12, borderRadius: 14, justifyContent: "center", backgroundColor: dietaryPreference === item ? colors.primary : colors.surface }}>
            <Text style={{ fontFamily: "JakartaSemi", color: dietaryPreference === item ? colors.onPrimary : colors.ink }}>{item}</Text>
          </Pressable>
        ))}
      </View>
      <Text style={{ fontFamily: "JakartaSemi", color: colors.ink }}>Activity level</Text>
      <View style={{ flexDirection: "row", flexWrap: "wrap", gap: 8 }}>
        {activities.map((item) => (
          <Pressable key={item} accessibilityRole="button" onPress={() => setActivityLevel(item)} style={{ minHeight: 44, paddingHorizontal: 12, borderRadius: 14, justifyContent: "center", backgroundColor: activityLevel === item ? colors.primary : colors.surface }}>
            <Text style={{ fontFamily: "JakartaSemi", color: activityLevel === item ? colors.onPrimary : colors.ink }}>{item}</Text>
          </Pressable>
        ))}
      </View>
      <SearchInput value={allergies} onChangeText={setAllergies} placeholder="Allergies, separated by commas" />
      <SearchInput value={preferences} onChangeText={setPreferences} placeholder="Foods you prefer, separated by commas" />
      <SearchInput value={waterGoalMl} onChangeText={setWaterGoalMl} placeholder="Daily water goal in ml" />
      <SearchInput value={sleepGoalMinutes} onChangeText={setSleepGoalMinutes} placeholder="Sleep goal in minutes" />
      <SearchInput value={activityGoalMinutes} onChangeText={setActivityGoalMinutes} placeholder="Activity goal in minutes" />
      {error ? <ErrorState body={error} /> : null}
      <PrimaryButton label={save.isPending ? "Saving" : "Save and continue"} disabled={save.isPending} onPress={() => save.mutate()} />
    </Screen>
  );
}
