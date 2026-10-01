import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useState } from "react";
import { View } from "react-native";
import { ApiError, api } from "../../src/api/client";
import { postOrQueue } from "../../src/api/cache";
import { WaterTracker } from "../../src/components/health";
import { Screen } from "../../src/components/Screen";
import { AppHeader, ErrorState, LoadingState, PrimaryButton, ProgressBar, SearchInput, SecondaryButton } from "../../src/components/ui";
import { track } from "../../src/lib/analytics";

type WaterDay = { summary: { goalMl: number; consumedMl: number; remainingMl: number; percent: number }; guidance?: string | null; entries: { id: string; amountMl: number }[] };

export default function WaterScreen() {
  const queryClient = useQueryClient();
  const [custom, setCustom] = useState("300");
  const [error, setError] = useState<string | null>(null);
  const day = useQuery({ queryKey: ["water"], queryFn: () => api<WaterDay>("/api/water/today") });
  const add = useMutation({
    mutationFn: (amountMl: number) => postOrQueue("/api/water", { amountMl }),
    onSuccess: () => {
      track("water_logged");
      setError(null);
      queryClient.invalidateQueries({ queryKey: ["water"] });
      queryClient.invalidateQueries({ queryKey: ["dashboard"] });
    },
    onError: (caught) => setError(caught instanceof ApiError ? caught.message : "Water was not saved."),
  });

  if (day.isLoading) return <Screen><LoadingState label="Loading water" /></Screen>;
  if (day.isError || !day.data) return <Screen><ErrorState body="Water tracking could not be loaded." onRetry={() => day.refetch()} /></Screen>;
  const summary = day.data.summary;
  return (
    <Screen>
      <AppHeader title="Water" subtitle={day.data.guidance ?? "Your goal is personal. The app will not raise it above a safe daily range."} />
      <WaterTracker consumed={summary.consumedMl} goal={summary.goalMl} percent={summary.percent} />
      <ProgressBar value={summary.percent} />
      <View style={{ flexDirection: "row", flexWrap: "wrap", gap: 8 }}>
        {[250, 500, 750, 1000].map((amount) => (
          <View key={amount} style={{ width: "48%" }}>
            <SecondaryButton label={`+${amount} ml`} onPress={() => add.mutate(amount)} />
          </View>
        ))}
      </View>
      <SearchInput value={custom} onChangeText={setCustom} placeholder="Custom ml" />
      <PrimaryButton label="Add custom amount" onPress={() => add.mutate(Number(custom))} />
      {error ? <ErrorState body={error} /> : null}
      {day.data.entries.map((entry) => (
        <SecondaryButton key={entry.id} label={`Remove ${entry.amountMl} ml`} onPress={() => api(`/api/water/${entry.id}`, { method: "DELETE" }).then(() => day.refetch())} />
      ))}
    </Screen>
  );
}
