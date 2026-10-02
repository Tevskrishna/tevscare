import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { router } from "expo-router";
import { useTranslation } from "react-i18next";
import { Text, View } from "react-native";
import { getCurrentUser } from "../../../src/api/client";
import { cachedGet, postOrQueue } from "../../../src/api/cache";
import { Meal, ReminderCard } from "../../../src/components/health";
import { Screen } from "../../../src/components/Screen";
import { Disclaimer, ErrorState, LoadingState, PrimaryButton, ProgressBar, ProgressRing, SecondaryButton, SectionHeader, StatCard, useColors } from "../../../src/components/ui";
import { track } from "../../../src/lib/analytics";
import { formatInr, mealLabel } from "../../../src/lib/planning";

type Dashboard = {
  displayDate: string;
  planName?: string | null;
  planDayNumber?: number | null;
  planDurationDays: number;
  planCompleted: boolean;
  weight: { currentKg?: number | null; targetKg?: number | null; changeKg?: number | null };
  water: { goalMl: number; consumedMl: number; remainingMl: number; percent: number };
  activity: { goalMinutes: number; loggedMinutes: number };
  sleep: { goalMinutes: number; loggedMinutes?: number | null; logged: boolean };
  adherence: { score: number; explanation: string };
  nextMeal?: Meal | null;
  reminders: { title: string; body: string; time: string }[];
};

type Budget = { dailyBudget: number; spent: number; remaining: number };

export default function HomeScreen() {
  const colors = useColors();
  const { t } = useTranslation();
  const user = getCurrentUser();
  const queryClient = useQueryClient();
  const dashboard = useQuery({ queryKey: ["dashboard"], queryFn: () => cachedGet<Dashboard>("cache.dashboard", "/api/dashboard") });
  const budget = useQuery({ queryKey: ["budget"], queryFn: () => cachedGet<Budget>("cache.budget", "/api/budget") });
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
      queryClient.invalidateQueries({ queryKey: ["budget"] });
    },
  });

  if (dashboard.isLoading) return <Screen><LoadingState label={t("loadingToday")} /></Screen>;
  if (dashboard.isError || !dashboard.data) return <Screen><ErrorState body={t("offlinePlan")} onRetry={() => dashboard.refetch()} /></Screen>;
  const data = dashboard.data;
  const next = data.nextMeal;
  const change = data.weight.changeKg;
  return (
    <Screen>
      <Text style={{ fontFamily: "Jakarta", color: colors.muted }}>{data.displayDate}</Text>
      <Text style={{ fontFamily: "Fraunces", fontSize: 32, color: colors.ink }}>{t("greeting")}, {user?.fullName.split(" ")[0]}</Text>
      <View style={{ gap: 4, padding: 16, borderRadius: 18, backgroundColor: colors.surface, borderWidth: 1, borderColor: colors.line }}>
        <Text style={{ fontFamily: "JakartaSemi", color: colors.ink }}>Now: {next ? `${mealLabel(next.mealType)} · ${next.title}` : "No meal left to log"}</Text>
        <Text style={{ fontFamily: "Jakarta", color: colors.muted }}>Still open: {data.water.remainingMl > 0 ? `${data.water.remainingMl} ml water` : "water goal met"}{data.sleep.logged ? "" : " · sleep not logged"}</Text>
        <Text style={{ fontFamily: "Jakarta", color: colors.muted }}>Done: {data.adherence.score}% of today’s checks. {data.planCompleted ? "This plan window is complete." : `${Math.max(data.planDurationDays - (data.planDayNumber ?? 0), 0)} plan days remain.`}</Text>
      </View>
      <View style={{ flexDirection: "row", justifyContent: "space-between", alignItems: "center", gap: 12 }}>
        <View style={{ flex: 1 }}>
          <Text style={{ fontFamily: "JakartaSemi", fontSize: 18, color: colors.ink }}>
            {data.planCompleted ? t("planWindowDone") : data.planName ? t("planDay", { day: data.planDayNumber, total: data.planDurationDays }) : t("finishSetup")}
          </Text>
          <Text style={{ fontFamily: "Jakarta", color: colors.muted, marginTop: 4 }}>{data.adherence.explanation}</Text>
        </View>
        <ProgressRing value={data.adherence.score} label={t("logged")} />
      </View>
      {next ? (
        <View style={{ gap: 8, padding: 16, borderRadius: 18, backgroundColor: colors.surface, borderWidth: 1, borderColor: colors.line }}>
          <SectionHeader title={t("nextMeal")} />
          <Text style={{ fontFamily: "Jakarta", color: colors.primary }}>{mealLabel(next.mealType)} · {next.scheduledTime}</Text>
          <Text style={{ fontFamily: "Fraunces", fontSize: 24, color: colors.ink }}>{next.title}</Text>
          <Text style={{ fontFamily: "Jakarta", color: colors.muted }}>{next.logStatus ?? t("planned")} · {formatInr(next.estimatedCost)} {t("estimated")}</Text>
          <PrimaryButton label={t("logMeal")} onPress={() => router.push({ pathname: "/(app)/meal/[id]", params: { id: next.id, meal: JSON.stringify(next) } })} />
          <SecondaryButton label={t("markComplete")} onPress={() => complete.mutate(next)} />
        </View>
      ) : null}
      <View style={{ flexDirection: "row", gap: 10 }}>
        <StatCard label={t("water")} value={`${data.water.percent}%`} hint={`${data.water.consumedMl} / ${data.water.goalMl} ml`} />
        <StatCard label={t("activity")} value={`${data.activity.loggedMinutes} min`} hint={`${t("goal")} ${data.activity.goalMinutes}`} />
      </View>
      <ProgressBar value={data.water.percent} />
      <View style={{ flexDirection: "row", gap: 10 }}>
        <StatCard label={t("sleep")} value={data.sleep.logged ? `${data.sleep.loggedMinutes} min` : t("notLogged")} hint={`${t("goal")} ${data.sleep.goalMinutes}`} />
        <StatCard label={t("budget")} value={budget.data ? formatInr(budget.data.remaining) : "—"} hint={budget.data ? `${formatInr(budget.data.spent)} ${t("spent")} · ${formatInr(budget.data.dailyBudget)} ${t("goal")}` : t("estimate")} />
      </View>
      <StatCard label={t("weight")} value={data.weight.currentKg ? `${data.weight.currentKg} kg` : "—"} hint={change == null ? `${t("goal")} ${data.weight.targetKg ?? "—"} kg` : `${change > 0 ? "+" : ""}${Number(change).toFixed(1)} kg ${t("change")}`} />
      <SectionHeader title={t("quickActions")} />
      <View style={{ flexDirection: "row", flexWrap: "wrap", gap: 8 }}>
        <View style={{ width: "48%" }}><SecondaryButton label={t("logWeight")} onPress={() => router.push("/(app)/weight")} /></View>
        <View style={{ width: "48%" }}><SecondaryButton label={t("addWater")} onPress={() => router.push("/(app)/water")} /></View>
        <View style={{ width: "48%" }}><SecondaryButton label={t("addActivity")} onPress={() => router.push("/(app)/activity")} /></View>
        <View style={{ width: "48%" }}><SecondaryButton label={t("checkIn")} onPress={() => router.push("/(app)/check-in")} /></View>
      </View>
      {data.reminders[0] ? <ReminderCard title={data.reminders[0].title} body={data.reminders[0].body} time={data.reminders[0].time} /> : null}
      <Disclaimer />
    </Screen>
  );
}
