import { useQuery } from "@tanstack/react-query";
import { useState } from "react";
import { Pressable, Text, View } from "react-native";
import { api } from "../../src/api/client";
import { Screen } from "../../src/components/Screen";
import { AppHeader, LoadingState, useColors } from "../../src/components/ui";

type Day = { date: string; planDayNumber?: number | null; mealsCompleted: number; mealsPlanned: number; waterMl: number; weightKg?: number | null; activityMinutes?: number | null; sleepMinutes?: number | null; adherenceScore?: number | null };

export default function CalendarScreen() {
  const colors = useColors();
  const [month, setMonth] = useState(() => new Date().toISOString().slice(0, 7));
  const [selected, setSelected] = useState<Day | null>(null);
  const days = useQuery({ queryKey: ["calendar", month], queryFn: () => api<Day[]>(`/api/calendar?month=${month}`) });
  return (
    <Screen>
      <AppHeader title="Calendar" subtitle={month} />
      <View style={{ flexDirection: "row", gap: 8 }}>
        <Pressable accessibilityRole="button" onPress={() => shift(-1)} style={{ minHeight: 44, justifyContent: "center" }}><Text style={{ fontFamily: "JakartaSemi", color: colors.primary }}>Previous</Text></Pressable>
        <Pressable accessibilityRole="button" onPress={() => shift(1)} style={{ minHeight: 44, justifyContent: "center" }}><Text style={{ fontFamily: "JakartaSemi", color: colors.primary }}>Next</Text></Pressable>
      </View>
      {days.isLoading ? <LoadingState /> : null}
      <View style={{ flexDirection: "row", flexWrap: "wrap", gap: 8 }}>
        {days.data?.map((day) => (
          <Pressable key={day.date} accessibilityLabel={day.date} onPress={() => setSelected(day)} style={{ width: 44, height: 44, borderRadius: 12, alignItems: "center", justifyContent: "center", backgroundColor: selected?.date === day.date ? colors.primary : colors.surface }}>
            <Text style={{ fontFamily: "JakartaSemi", color: selected?.date === day.date ? colors.onPrimary : colors.ink }}>{Number(day.date.slice(8))}</Text>
          </Pressable>
        ))}
      </View>
      {selected ? (
        <Text style={{ fontFamily: "Jakarta", color: colors.ink }}>
          {selected.date}{selected.planDayNumber ? ` · plan day ${selected.planDayNumber}` : ""} · meals {selected.mealsCompleted}/{selected.mealsPlanned} · water {selected.waterMl} ml · weight {selected.weightKg ?? "—"} · activity {selected.activityMinutes ?? "—"} · sleep {selected.sleepMinutes ?? "—"} · logged {selected.adherenceScore ?? "—"}
        </Text>
      ) : null}
    </Screen>
  );

  function shift(delta: number) {
    const [year, monthNumber] = month.split("-").map(Number);
    const next = new Date(Date.UTC(year, monthNumber - 1 + delta, 1));
    setMonth(next.toISOString().slice(0, 7));
    setSelected(null);
  }
}
