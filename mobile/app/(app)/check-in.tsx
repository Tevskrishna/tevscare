import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useState } from "react";
import { Text } from "react-native";
import { api } from "../../src/api/client";
import { HabitTracker } from "../../src/components/health";
import { Screen } from "../../src/components/Screen";
import { AppHeader, LoadingState, PrimaryButton, ProgressRing, SearchInput, useColors } from "../../src/components/ui";

type CheckIn = { adherence: { score: number; explanation: string }; habits: { code: string; title: string; completed: boolean }[] };

export default function CheckInScreen() {
  const colors = useColors();
  const queryClient = useQueryClient();
  const [done, setDone] = useState<string[]>([]);
  const [mood, setMood] = useState("");
  const checkIn = useQuery({ queryKey: ["check-in"], queryFn: () => api<CheckIn>("/api/check-in") });
  const save = useMutation({
    mutationFn: () => api("/api/check-in", { method: "POST", body: JSON.stringify({ mood: mood ? Number(mood) : null, energy: null, completedHabitCodes: done }) }),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ["check-in"] }),
  });
  if (checkIn.isLoading || !checkIn.data) return <Screen><LoadingState label="Loading today's progress" /></Screen>;
  return (
    <Screen>
      <AppHeader title="Today's progress" subtitle="Mood is optional and self-reported. It is not a clinical score." />
      <ProgressRing value={checkIn.data.adherence.score} label="logged" />
      <Text style={{ fontFamily: "Jakarta", color: colors.muted }}>{checkIn.data.adherence.explanation}</Text>
      {checkIn.data.habits.map((habit) => (
        <HabitTracker key={habit.code} title={habit.title} done={done.includes(habit.code) || habit.completed} onToggle={() => setDone((current) => current.includes(habit.code) ? current.filter((code) => code !== habit.code) : [...current, habit.code])} />
      ))}
      <SearchInput value={mood} onChangeText={setMood} placeholder="Mood 1–5, optional" />
      <PrimaryButton label="Save check-in" onPress={() => save.mutate()} />
    </Screen>
  );
}
