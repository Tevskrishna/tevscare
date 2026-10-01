import { useQuery } from "@tanstack/react-query";
import { useState } from "react";
import { api } from "../../src/api/client";
import { FoodCard } from "../../src/components/health";
import { Screen } from "../../src/components/Screen";
import { AppHeader, EmptyState, LoadingState, SearchInput } from "../../src/components/ui";

type Food = { id: string; name: string; localName?: string | null; category: string; calories: number; proteinG: number; referencePriceInr?: number | null; yourPriceInr?: number | null; providerNote?: string | null };

export default function FoodsScreen() {
  const [query, setQuery] = useState("");
  const foods = useQuery({ queryKey: ["foods", query], queryFn: () => api<Food[]>(`/api/foods?query=${encodeURIComponent(query)}&pageSize=40`) });
  return (
    <Screen>
      <AppHeader title="Foods" subtitle="Nutrition supports the plan. It is not the main screen. Provider notes are plan guidance, not medical rules." />
      <SearchInput value={query} onChangeText={setQuery} placeholder="Search foods" />
      {foods.isLoading ? <LoadingState /> : null}
      {foods.data?.length === 0 ? <EmptyState title="No matches" body="Try a shorter name, such as dosa or almond." /> : null}
      {foods.data?.map((food) => (
        <FoodCard key={food.id} name={food.name} localName={food.localName} detail={`${food.category} · ${food.calories} kcal · ${food.proteinG} g protein · ₹${food.yourPriceInr ?? food.referencePriceInr ?? "—"}`} />
      ))}
    </Screen>
  );
}
