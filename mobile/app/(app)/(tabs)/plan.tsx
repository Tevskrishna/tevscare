import { useQuery } from "@tanstack/react-query";
import { router } from "expo-router";
import { useState } from "react";
import { Pressable, Text, View } from "react-native";
import { cachedGet } from "../../../src/api/cache";
import { DailyTimeline, Meal } from "../../../src/components/health";
import { Screen } from "../../../src/components/Screen";
import { AppHeader, Disclaimer, EmptyState, ErrorState, LoadingState, useColors } from "../../../src/components/ui";
import { track } from "../../../src/lib/analytics";

type PlanSummary = { id: string; name: string; durationDays: number };
type Plan = PlanSummary & { currentDayNumber?: number | null; disclaimer: string };
type Day = { dayNumber: number; notes?: string | null; waterNote?: string | null; activityNote?: string | null; sleepNote?: string | null; meals: Meal[] };

export default function PlanScreen() {
  const colors = useColors();
  const plans = useQuery({ queryKey: ["plans"], queryFn: () => cachedGet<PlanSummary[]>("cache.plans", "/api/diet-plans") });
  const selectedPlan = plans.data?.[0];
  const planQuery = useQuery({
    queryKey: ["plan", selectedPlan?.id],
    enabled: Boolean(selectedPlan?.id),
    queryFn: () => cachedGet<Plan>(`cache.plan.${selectedPlan!.id}`, `/api/diet-plans/${selectedPlan!.id}`),
  });
  const plan = planQuery.data;
  const [day, setDay] = useState<number | null>(null);
  const selected = day ?? plan?.currentDayNumber ?? 1;
  const detail = useQuery({
    queryKey: ["plan-day", plan?.id, selected],
    enabled: Boolean(plan?.id),
    queryFn: () => cachedGet<Day>(`cache.day.${selected}`, `/api/diet-plans/${plan!.id}/days/${selected}`),
  });

  if (plans.isLoading || planQuery.isLoading) return <Screen><LoadingState label="Loading the plan" /></Screen>;
  if (plans.isError || planQuery.isError) return <Screen><ErrorState body="The plan could not be loaded." onRetry={() => { plans.refetch(); planQuery.refetch(); }} /></Screen>;
  if (!plan) return <Screen><EmptyState title="No plan yet" body="Complete onboarding to receive the starter plan." /></Screen>;

  return (
    <Screen>
      <AppHeader eyebrow={plan.name} title={`Day ${selected}`} subtitle="Breakfast follows the 15-day rotation. Every meal is stored as plan content, so a provider can change it without an app update." />
      <View style={{ flexDirection: "row", flexWrap: "wrap", gap: 8 }}>
        {Array.from({ length: plan.durationDays }, (_, index) => index + 1).map((number) => (
          <Pressable key={number} accessibilityRole="button" accessibilityLabel={`Day ${number}`} onPress={() => setDay(number)} style={{ minWidth: 48, minHeight: 48, borderRadius: 14, alignItems: "center", justifyContent: "center", backgroundColor: number === selected ? colors.primary : colors.surface, borderWidth: 1, borderColor: colors.line }}>
            <Text style={{ fontFamily: "JakartaSemi", color: number === selected ? colors.onPrimary : colors.ink }}>{number}</Text>
          </Pressable>
        ))}
      </View>
      {detail.isLoading ? <LoadingState /> : null}
      {detail.data ? (
        <>
          <DailyTimeline meals={detail.data.meals} onPress={(meal) => { track("meal_viewed"); router.push({ pathname: "/(app)/meal/[id]", params: { id: meal.id, meal: JSON.stringify(meal) } }); }} />
          <Text style={{ fontFamily: "Jakarta", color: colors.muted }}>{detail.data.waterNote}</Text>
          <Text style={{ fontFamily: "Jakarta", color: colors.muted }}>{detail.data.activityNote}</Text>
          <Text style={{ fontFamily: "Jakarta", color: colors.muted }}>{detail.data.sleepNote}</Text>
        </>
      ) : null}
      <Disclaimer />
    </Screen>
  );
}
