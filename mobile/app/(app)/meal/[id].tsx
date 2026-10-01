import { useMutation, useQueryClient } from "@tanstack/react-query";
import { router, useLocalSearchParams } from "expo-router";
import { useEffect, useState } from "react";
import { Image, Text } from "react-native";
import { loadLocalPhoto, pickLocalPhoto } from "../../../src/lib/photos";
import { ApiError } from "../../../src/api/client";
import { postOrQueue } from "../../../src/api/cache";
import { Meal } from "../../../src/components/health";
import { Screen } from "../../../src/components/Screen";
import { AppHeader, ErrorState, FoodQuantityInput, PrimaryButton, SecondaryButton, useColors } from "../../../src/components/ui";
import { formatInr, mealLabel } from "../../../src/lib/planning";
import { track } from "../../../src/lib/analytics";

export default function MealScreen() {
  const colors = useColors();
  const params = useLocalSearchParams<{ meal?: string }>();
  const meal = params.meal ? (JSON.parse(params.meal) as Meal) : null;
  const [quantity, setQuantity] = useState(1);
  const [error, setError] = useState<string | null>(null);
  const [photo, setPhoto] = useState<string | null>(null);
  useEffect(() => {
    if (meal) loadLocalPhoto(meal.id).then(setPhoto);
  }, [meal?.id]);
  const queryClient = useQueryClient();
  const log = useMutation({
    mutationFn: (status: string) => {
      if (!meal) throw new Error("Meal missing");
      const base = meal.items.filter((item) => !item.alternativeGroup || item.isDefaultAlternative);
      return postOrQueue("/api/meals/log", {
        mealType: meal.mealType,
        status,
        mealId: meal.id,
        items: status === "Skipped" ? [] : base.map((item) => ({ foodId: item.foodId ?? null, name: item.name, quantity: item.quantity * quantity, unit: item.unit })),
      });
    },
    onSuccess: (_data, status) => {
      if (status === "Completed") track("meal_completed");
      queryClient.invalidateQueries({ queryKey: ["dashboard"] });
      router.back();
    },
    onError: (caught) => setError(caught instanceof ApiError ? caught.message : "The meal was not saved."),
  });

  if (!meal) return <Screen><ErrorState body="This meal could not be opened." /></Screen>;
  return (
    <Screen>
      <AppHeader eyebrow={mealLabel(meal.mealType)} title={meal.title} subtitle={meal.notes ?? "Planned meal. What you log is stored separately."} />
      {meal.items.map((item) => (
        <Text key={item.id} style={{ fontFamily: "Jakarta", color: colors.ink }}>
          {item.quantity} {item.unit} {item.name}{item.localName ? ` · ${item.localName}` : ""}{item.alternativeGroup && !item.isDefaultAlternative ? " (alternative)" : ""}
        </Text>
      ))}
      <Text style={{ fontFamily: "JakartaSemi", color: colors.ink }}>{formatInr(meal.estimatedCost)} estimated</Text>
      {photo ? <Image source={{ uri: photo }} accessibilityLabel="Meal photo" style={{ width: "100%", height: 180, borderRadius: 16 }} /> : null}
      <SecondaryButton label="Add a photo" onPress={async () => setPhoto(await pickLocalPhoto(meal.id))} />
      <FoodQuantityInput value={quantity} onChange={setQuantity} />
      {error ? <ErrorState body={error} /> : null}
      <PrimaryButton label="Log this amount" onPress={() => log.mutate(quantity === 1 ? "Completed" : "Partial")} />
      <SecondaryButton label="Mark skipped" onPress={() => log.mutate("Skipped")} />
    </Screen>
  );
}
