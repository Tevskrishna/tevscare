import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useState } from "react";
import { Text } from "react-native";
import { api } from "../../src/api/client";
import { HabitTracker } from "../../src/components/health";
import { Screen } from "../../src/components/Screen";
import { AppHeader, ErrorState, LoadingState, PrimaryButton, ProgressRing, SearchInput, useColors } from "../../src/components/ui";
import { track } from "../../src/lib/analytics";

type CheckIn = { adherence: { score: number; explanation: string }; habits: { code: string; title: string; completed: boolean }[] };

export default function CheckInScreen() {
  const colors = useColors();
  const queryClient = useQueryClient();
  const [done, setDone] = useState<string[]>([]);
  const [mood, setMood] = useState("");
  const [energy, setEnergy] = useState("");
  const [hunger, setHunger] = useState("");
  const [digestion, setDigestion] = useState("");
  const [sleepQuality, setSleepQuality] = useState("");
  const checkIn = useQuery({ queryKey: ["check-in"], queryFn: () => api<CheckIn>("/api/check-in") });
  const save = useMutation({
    mutationFn: () => api("/api/check-in", { method: "POST", body: JSON.stringify({ mood: numberOrNull(mood), energy: numberOrNull(energy), hunger: numberOrNull(hunger), digestion: numberOrNull(digestion), sleepQuality: numberOrNull(sleepQuality), completedHabitCodes: done }) }),
    onSuccess: () => { track("checkin_completed"); queryClient.invalidateQueries({ queryKey: ["check-in"] }); },
  });
  if (checkIn.isLoading) return <Screen><LoadingState label="Loading today's progress" /></Screen>;
  if (checkIn.isError || !checkIn.data) return <Screen><ErrorState body="Today's check-in could not be loaded." onRetry={() => checkIn.refetch()} /></Screen>;
  return (
    <Screen>
      <AppHeader title="Today's progress" subtitle="Mood is optional and self-reported. It is not a clinical score." />
      <ProgressRing value={checkIn.data.adherence.score} label="logged" />
      <Text style={{ fontFamily: "Jakarta", color: colors.muted }}>{checkIn.data.adherence.explanation}</Text>
      {checkIn.data.habits.map((habit) => (
        <HabitTracker key={habit.code} title={habit.title} done={done.includes(habit.code) || habit.completed} onToggle={() => setDone((current) => current.includes(habit.code) ? current.filter((code) => code !== habit.code) : [...current, habit.code])} />
      ))}
      <SearchInput value={mood} onChangeText={setMood} placeholder="Mood 1–5, optional" />
      <SearchInput value={energy} onChangeText={setEnergy} placeholder="Energy 1–5, optional" />
      <SearchInput value={hunger} onChangeText={setHunger} placeholder="Hunger 1–5, optional" />
      <SearchInput value={digestion} onChangeText={setDigestion} placeholder="Digestion 1–5, optional" />
      <SearchInput value={sleepQuality} onChangeText={setSleepQuality} placeholder="Sleep quality 1–5, optional" />
      <PrimaryButton label="Save check-in" onPress={() => save.mutate()} />
    </Screen>
  );
}

function numberOrNull(value: string) {
  return value.trim() ? Number(value) : null;
}
