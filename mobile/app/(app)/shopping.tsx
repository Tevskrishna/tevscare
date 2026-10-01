import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useState } from "react";
import { Pressable, Text, View } from "react-native";
import { api } from "../../src/api/client";
import { Screen } from "../../src/components/Screen";
import { AppHeader, EmptyState, ErrorState, LoadingState, useColors } from "../../src/components/ui";
import { formatInr } from "../../src/lib/planning";

type Line = { foodId: string; name: string; category: string; quantity: number; unit: string; estimatedCost?: number | null; purchased: boolean };
type List = { days: number; totalKnownCost: number; hasMissingPrices: boolean; lines: Line[] };

export default function ShoppingScreen() {
  const colors = useColors();
  const queryClient = useQueryClient();
  const [days, setDays] = useState(15);
  const list = useQuery({ queryKey: ["shopping", days], queryFn: () => api<List>(`/api/shopping-list?days=${days}`) });
  const toggle = useMutation({
    mutationFn: (line: Line) => api("/api/shopping-list/toggle", { method: "POST", body: JSON.stringify({ foodId: line.foodId, rangeDays: days, purchased: !line.purchased }) }),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ["shopping", days] }),
  });
  return (
    <Screen>
      <AppHeader title="Shopping list" subtitle="Built from the meals still inside your plan window." />
      <View style={{ flexDirection: "row", gap: 8 }}>
        {[1, 7, 15, 30].map((item) => (
          <Pressable key={item} accessibilityRole="button" onPress={() => setDays(item)} style={{ minHeight: 44, paddingHorizontal: 14, borderRadius: 14, justifyContent: "center", backgroundColor: days === item ? colors.primary : colors.surface }}>
            <Text style={{ fontFamily: "JakartaSemi", color: days === item ? colors.onPrimary : colors.ink }}>{item}d</Text>
          </Pressable>
        ))}
      </View>
      {list.isLoading ? <LoadingState /> : null}
      {list.isError ? <ErrorState body="The list could not be built." onRetry={() => list.refetch()} /> : null}
      {list.data && list.data.lines.length === 0 ? <EmptyState title="Nothing to buy" body="Assign a plan, or choose a shorter window that still overlaps it." /> : null}
      {list.data?.lines.map((line) => (
        <Pressable key={line.foodId} accessibilityRole="checkbox" accessibilityState={{ checked: line.purchased }} onPress={() => toggle.mutate(line)} style={{ minHeight: 56, justifyContent: "center" }}>
          <Text style={{ fontFamily: "JakartaSemi", color: colors.ink, textDecorationLine: line.purchased ? "line-through" : "none" }}>{line.purchased ? "✓" : "○"} {line.name}</Text>
          <Text style={{ fontFamily: "Jakarta", color: colors.muted }}>{line.category} · {line.quantity} {line.unit} · {line.estimatedCost == null ? "price missing" : formatInr(line.estimatedCost)}</Text>
        </Pressable>
      ))}
      {list.data ? <Text style={{ fontFamily: "JakartaSemi", color: colors.ink }}>Known total {formatInr(list.data.totalKnownCost)}{list.data.hasMissingPrices ? " · some prices are missing" : ""}</Text> : null}
    </Screen>
  );
}
