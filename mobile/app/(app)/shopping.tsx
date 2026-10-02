import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useState } from "react";
import { Pressable, Text, View } from "react-native";
import { api } from "../../src/api/client";
import { Screen } from "../../src/components/Screen";
import { AppHeader, EmptyState, ErrorState, LoadingState, PrimaryButton, SearchInput, useColors } from "../../src/components/ui";
import { track } from "../../src/lib/analytics";
import { formatInr } from "../../src/lib/planning";

type Line = { foodId: string; name: string; category: string; quantity: number; unit: string; estimatedCost?: number | null; purchased: boolean; plannedQuantity?: number; actualUnitPrice?: number | null; actualCost?: number | null; notes?: string | null };
type List = { days: number; totalKnownCost: number; actualKnownCost?: number; hasMissingPrices: boolean; lines: Line[] };

export default function ShoppingScreen() {
  const colors = useColors();
  const queryClient = useQueryClient();
  const [days, setDays] = useState(15);
  const [editing, setEditing] = useState<Line | null>(null);
  const [quantity, setQuantity] = useState("");
  const [price, setPrice] = useState("");
  const [notes, setNotes] = useState("");
  const list = useQuery({ queryKey: ["shopping", days], queryFn: () => { track("shopping_list_opened"); return api<List>(`/api/shopping-list?days=${days}`); } });
  const toggle = useMutation({
    mutationFn: (line: Line) => api("/api/shopping-list/toggle", { method: "POST", body: JSON.stringify({ foodId: line.foodId, rangeDays: days, purchased: !line.purchased }) }),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ["shopping", days] }),
  });
  return (
    <Screen>
      <AppHeader title="Shopping list" subtitle="Quantities come from the plan. Prices are estimates until you enter what your store charges." />
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
        <View key={line.foodId} style={{ minHeight: 56, justifyContent: "center", gap: 4 }}>
          <Pressable accessibilityRole="checkbox" accessibilityState={{ checked: line.purchased }} onPress={() => toggle.mutate(line)}>
            <Text style={{ fontFamily: "JakartaSemi", color: colors.ink, textDecorationLine: line.purchased ? "line-through" : "none" }}>{line.purchased ? "✓" : "○"} {line.name}</Text>
          </Pressable>
          <Text style={{ fontFamily: "Jakarta", color: colors.muted }}>{line.category} · plan {line.plannedQuantity ?? line.quantity} · buy {line.quantity} {line.unit} · estimate {line.estimatedCost == null ? "missing" : formatInr(line.estimatedCost)}{line.actualCost != null ? ` · actual ${formatInr(line.actualCost)}` : ""}</Text>
          <Pressable accessibilityRole="button" onPress={() => { setEditing(line); setQuantity(String(line.quantity)); setPrice(line.actualUnitPrice == null ? "" : String(line.actualUnitPrice)); setNotes(line.notes ?? ""); }}><Text style={{ fontFamily: "JakartaSemi", color: colors.primary }}>Adjust</Text></Pressable>
        </View>
      ))}
      {editing ? (
        <View style={{ gap: 8 }}>
          <SearchInput value={quantity} onChangeText={setQuantity} placeholder="Quantity to buy" />
          <SearchInput value={price} onChangeText={setPrice} placeholder="Your price per unit, optional" />
          <SearchInput value={notes} onChangeText={setNotes} placeholder="Note, optional" />
          <PrimaryButton label="Save adjustment" onPress={() => api("/api/shopping-list/toggle", { method: "POST", body: JSON.stringify({ foodId: editing.foodId, rangeDays: days, purchased: editing.purchased, quantity: Number(quantity), actualUnitPrice: price ? Number(price) : null, notes, updateDetails: true }) }).then(() => { setEditing(null); queryClient.invalidateQueries({ queryKey: ["shopping", days] }); })} />
        </View>
      ) : null}
      {list.data ? <Text style={{ fontFamily: "JakartaSemi", color: colors.ink }}>Estimated {formatInr(list.data.totalKnownCost)} · actual entered {formatInr(list.data.actualKnownCost ?? 0)}{list.data.hasMissingPrices ? " · some prices are missing" : ""}</Text> : null}
    </Screen>
  );
}
