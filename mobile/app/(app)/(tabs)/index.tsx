import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { router } from "expo-router";
import { Text, View } from "react-native";
import { getCurrentUser } from "../../../src/api/client";
import { cachedGet, postOrQueue } from "../../../src/api/cache";
import { DailyTimeline, Meal, ReminderCard } from "../../../src/components/health";
import { Screen } from "../../../src/components/Screen";
import { Disclaimer, ErrorState, LoadingState, PrimaryButton, ProgressRing, SecondaryButton, SectionHeader, StatCard, useColors } from "../../../src/components/ui";
import { track } from "../../../src/lib/analytics";

type Dashboard = {
  greeting: string;
  displayDate: string;
  planName?: string | null;
  planDayNumber?: number | null;
  planDurationDays: number;
  planCompleted: boolean;
  weight: { currentKg?: number | null; targetKg?: number | null; changeKg?: number | null; progressPercent?: number | null };
  water: { goalMl: number; consumedMl: number; remainingMl: number; percent: number };
  activity: { goalMinutes: number; loggedMinutes: number; completed: boolean };
  sleep: { goalMinutes: number; loggedMinutes?: number | null; logged: boolean };
  adherence: { score: number; explanation: string };
  nextMeal?: Meal | null;
  meals: Meal[];
  reminders: { title: string; body: string; time: string }[];
};

export default function HomeScreen() {
  const colors = useColors();
  const user = getCurrentUser();
  const queryClient = useQueryClient();
  const dashboard = useQuery({ queryKey: ["dashboard"], queryFn: () => cachedGet<Dashboard>("cache.dashboard", "/api/dashboard") });
  const complete = useMutation({
    mutationFn: (meal: Meal) => postOrQueue("/api/meals/log", {
      mealType: meal.mealType,
      status: "Completed",
      mealId: meal.id,
      items: meal.items.filter((item) => !item.alternativeGroup || item.isDefaultAlternative).map((item) => ({ foodId: item.foodId ?? null, name: item.name, quantity: item.quantity, unit: item.unit })),
    }),
    onSuccess: () => {
      track("meal_completed");
      queryClient.invalidateQueries({ queryKey: ["dashboard"] });
    },
  });

  if (dashboard.isLoading) return <Screen><LoadingState label="Preparing today" /></Screen>;
  if (dashboard.isError || !dashboard.data) return <Screen><ErrorState body="Today's plan could not be loaded." onRetry={() => dashboard.refetch()} /></Screen>;
  const data = dashboard.data;
  const change = data.weight.changeKg;
  return (
    <Screen>
      <View style={{ flexDirection: "row", justifyContent: "space-between", alignItems: "center" }}>
        <View style={{ flex: 1, paddingRight: 12 }}>
          <Text style={{ fontFamily: "Jakarta", color: colors.muted }}>{data.displayDate}</Text>
          <Text style={{ fontFamily: "Fraunces", fontSize: 32, color: colors.ink }}>{data.greeting}, {user?.fullName.split(" ")[0]}</Text>
          <Text style={{ fontFamily: "Jakarta", color: colors.muted, marginTop: 4 }}>
            {data.planCompleted ? "This plan window is complete." : data.planName ? `Day ${data.planDayNumber} of ${data.planDurationDays}` : "Finish setup to see a plan."}
          </Text>
        </View>
        <ProgressRing value={data.adherence.score} label="logged" />
      </View>
      <Text style={{ fontFamily: "Jakarta", color: colors.muted }}>{data.adherence.explanation}</Text>
      {data.nextMeal ? (
        <View style={{ gap: 10 }}>
          <SectionHeader title="Next" />
          <DailyTimeline meals={[data.nextMeal]} onPress={(meal) => router.push({ pathname: "/(app)/meal/[id]", params: { id: meal.id, meal: JSON.stringify(meal) } })} />
          <PrimaryButton label="Mark meal complete" onPress={() => complete.mutate(data.nextMeal!)} />
        </View>
      ) : null}
      <View style={{ flexDirection: "row", gap: 10 }}>
        <StatCard label="Weight" value={data.weight.currentKg ? `${data.weight.currentKg} kg` : "—"} hint={change == null ? "Goal " + (data.weight.targetKg ?? "—") : `${change > 0 ? "+" : ""}${Number(change).toFixed(1)} kg change`} />
        <StatCard label="Water" value={`${data.water.percent}%`} hint={`${data.water.remainingMl} ml remaining`} />
      </View>
      <View style={{ flexDirection: "row", gap: 10 }}>
        <StatCard label="Activity" value={`${data.activity.loggedMinutes} min`} hint={`Goal ${data.activity.goalMinutes} min`} />
        <StatCard label="Sleep" value={data.sleep.logged ? `${data.sleep.loggedMinutes} min` : "Not logged"} hint={`Goal ${data.sleep.goalMinutes} min`} />
      </View>
      <SectionHeader title="Quick actions" />
      <View style={{ flexDirection: "row", flexWrap: "wrap", gap: 8 }}>
        <View style={{ width: "48%" }}><SecondaryButton label="Log weight" onPress={() => router.push("/(app)/weight")} /></View>
        <View style={{ width: "48%" }}><SecondaryButton label="Log water" onPress={() => router.push("/(app)/water")} /></View>
        <View style={{ width: "48%" }}><SecondaryButton label="Log activity" onPress={() => router.push("/(app)/activity")} /></View>
        <View style={{ width: "48%" }}><SecondaryButton label="Check in" onPress={() => router.push("/(app)/check-in")} /></View>
      </View>
      {data.reminders[0] ? <ReminderCard title={data.reminders[0].title} body={data.reminders[0].body} time={data.reminders[0].time} /> : null}
      <Disclaimer />
    </Screen>
  );
}
