import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useState } from "react";
import { Text } from "react-native";
import { api } from "../../src/api/client";
import { Screen } from "../../src/components/Screen";
import { AppHeader, ErrorState, LoadingState, PrimaryButton, SearchInput, StatCard, useColors } from "../../src/components/ui";
import { track } from "../../src/lib/analytics";
import { formatInr } from "../../src/lib/planning";

type Budget = {
  dailyBudget: number;
  spent: number;
  remaining: number;
  projectedDaily: number;
  projectedPeriod: number;
  projectedMonthly: number;
  periodDays: number;
  hasMissingPrices: boolean;
  priceNote: string;
  meals: { mealName: string; estimatedCost: number; spentCost: number }[];
};

export default function BudgetScreen() {
  const colors = useColors();
  const queryClient = useQueryClient();
  const [amount, setAmount] = useState("675");
  const budget = useQuery({ queryKey: ["budget"], queryFn: () => { track("budget_viewed"); return api<Budget>("/api/budget"); } });
  const save = useMutation({
    mutationFn: () => api("/api/budget", { method: "POST", body: JSON.stringify({ dailyBudgetAmount: Number(amount), currency: "INR" }) }),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ["budget"] }),
  });
  if (budget.isLoading) return <Screen><LoadingState /></Screen>;
  if (budget.isError || !budget.data) return <Screen><ErrorState body="Budget could not be loaded." onRetry={() => budget.refetch()} /></Screen>;
  const data = budget.data;
  return (
    <Screen>
      <AppHeader title="Food budget" subtitle="Prices are your local prices, or a reference until you replace them. They are not a national price list." />
      <StatCard label="Today" value={formatInr(data.dailyBudget)} hint={`${formatInr(data.spent)} spent · ${formatInr(data.remaining)} remaining`} />
      <Text style={{ fontFamily: "Jakarta", color: colors.muted }}>Projected daily {formatInr(data.projectedDaily)} · {data.periodDays} days {formatInr(data.projectedPeriod)} · month {formatInr(data.projectedMonthly)}</Text>
      {data.meals.map((meal) => (
        <Text key={meal.mealName} style={{ fontFamily: "Jakarta", color: colors.ink }}>{meal.mealName} · {formatInr(meal.estimatedCost)} estimated · {formatInr(meal.spentCost)} logged</Text>
      ))}
      {data.hasMissingPrices ? <Text style={{ fontFamily: "Jakarta", color: colors.muted }}>{data.priceNote}</Text> : null}
      <SearchInput value={amount} onChangeText={setAmount} placeholder="Daily budget in INR" />
      <PrimaryButton label="Save budget" onPress={() => save.mutate()} />
    </Screen>
  );
}
