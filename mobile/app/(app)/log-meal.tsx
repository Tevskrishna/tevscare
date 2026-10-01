import { useQuery } from "@tanstack/react-query";
import { router } from "expo-router";
import { api } from "../../src/api/client";
import { DailyTimeline, Meal } from "../../src/components/health";
import { Screen } from "../../src/components/Screen";
import { AppHeader, EmptyState, ErrorState, LoadingState } from "../../src/components/ui";

export default function LogMealScreen() {
  const meals = useQuery({ queryKey: ["today-meals"], queryFn: () => api<Meal[]>("/api/meals/today") });
  if (meals.isLoading) return <Screen><LoadingState /></Screen>;
  if (meals.isError) return <Screen><ErrorState body="Today's meals could not be loaded." onRetry={() => meals.refetch()} /></Screen>;
  return (
    <Screen>
      <AppHeader title="Log a meal" subtitle="Choose the planned meal, then record what you actually had." />
      {meals.data?.length ? (
        <DailyTimeline meals={meals.data} onPress={(meal) => router.push({ pathname: "/(app)/meal/[id]", params: { id: meal.id, meal: JSON.stringify(meal) } })} />
      ) : (
        <EmptyState title="No meals for today" body="A plan appears after onboarding, and only while the plan window is open." />
      )}
    </Screen>
  );
}
