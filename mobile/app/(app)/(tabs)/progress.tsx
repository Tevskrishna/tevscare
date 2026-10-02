import { useQuery } from "@tanstack/react-query";
import { useState } from "react";
import { Pressable, Text, View } from "react-native";
import Svg, { Polyline } from "react-native-svg";
import { api } from "../../../src/api/client";
import { Screen } from "../../../src/components/Screen";
import { AppHeader, EmptyState, ErrorState, LoadingState, useColors } from "../../../src/components/ui";

type Point = { date: string; value: number };
type Progress = { range: string; weight: Point[]; adherence: Point[]; water: Point[]; meals: Point[]; activity: Point[]; sleep: Point[]; budget?: Point[] | null; weightChange?: number | null; averageAdherence: number };

function Chart({ points }: { points: Point[] }) {
  const colors = useColors();
  if (points.length < 2) return <EmptyState title="Not enough days" body="Log a few days and this chart will show the trend." />;
  const values = points.map((point) => point.value);
  const min = Math.min(...values);
  const max = Math.max(...values);
  const span = max - min || 1;
  const coords = points.map((point, index) => {
    const x = (index / (points.length - 1)) * 280;
    const y = 90 - ((point.value - min) / span) * 80;
    return `${x},${y}`;
  }).join(" ");
  return (
    <Svg width="100%" height={100} viewBox="0 0 280 100">
      <Polyline points={coords} fill="none" stroke={colors.primary} strokeWidth={3} />
    </Svg>
  );
}

export default function ProgressScreen() {
  const colors = useColors();
  const [range, setRange] = useState("7");
  const progress = useQuery({ queryKey: ["progress", range], queryFn: () => api<Progress>(`/api/progress?range=${range}`) });
  return (
    <Screen>
      <AppHeader title="Progress" subtitle="Ranges are 7, 15, 30 and 90 days. Empty days count, so the picture stays honest." />
      <View style={{ flexDirection: "row", gap: 8 }}>
        {["7", "15", "30", "90"].map((item) => (
          <Pressable key={item} accessibilityRole="button" onPress={() => setRange(item)} style={{ minHeight: 44, paddingHorizontal: 14, borderRadius: 14, justifyContent: "center", backgroundColor: range === item ? colors.primary : colors.surface }}>
            <Text style={{ fontFamily: "JakartaSemi", color: range === item ? colors.onPrimary : colors.ink }}>{item}d</Text>
          </Pressable>
        ))}
      </View>
      {progress.isLoading ? <LoadingState /> : null}
      {progress.isError ? <ErrorState body="Progress could not be loaded." onRetry={() => progress.refetch()} /> : null}
      {progress.data ? (
        <>
          <Text style={{ fontFamily: "Jakarta", color: colors.muted }}>Average logged score {progress.data.averageAdherence}. Weight change {progress.data.weightChange ?? 0} kg in this range.</Text>
          <Text style={{ fontFamily: "JakartaSemi", color: colors.ink }}>Weight</Text>
          <Chart points={progress.data.weight} />
          <Text style={{ fontFamily: "JakartaSemi", color: colors.ink }}>Water</Text>
          <Chart points={progress.data.water} />
          <Text style={{ fontFamily: "JakartaSemi", color: colors.ink }}>Meals logged</Text>
          <Chart points={progress.data.meals} />
          <Text style={{ fontFamily: "JakartaSemi", color: colors.ink }}>Activity</Text>
          <Chart points={progress.data.activity} />
          <Text style={{ fontFamily: "JakartaSemi", color: colors.ink }}>Sleep</Text>
          <Chart points={progress.data.sleep} />
          <Text style={{ fontFamily: "JakartaSemi", color: colors.ink }}>Estimated food spend</Text>
          <Chart points={progress.data.budget ?? []} />
        </>
      ) : null}
    </Screen>
  );
}
