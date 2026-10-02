import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useEffect, useState } from "react";
import { ApiError, api } from "../../src/api/client";
import { postOrQueue } from "../../src/api/cache";
import { WeightTracker } from "../../src/components/health";
import { Screen } from "../../src/components/Screen";
import { AppHeader, ErrorState, LoadingState, PrimaryButton, SearchInput, SecondaryButton, useColors } from "../../src/components/ui";
import { track } from "../../src/lib/analytics";
import { Image, Text } from "react-native";
import { loadLocalPhoto, pickLocalPhoto } from "../../src/lib/photos";

type History = { currentKg?: number | null; targetKg?: number | null; weeklyChangeKg?: number | null; monthlyChangeKg?: number | null; sevenDayAverageKg?: number | null; entries: { localDate: string; weightKg: number }[] };

export default function WeightScreen() {
  const colors = useColors();
  const queryClient = useQueryClient();
  const [value, setValue] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [photo, setPhoto] = useState<string | null>(null);
  useEffect(() => {
    loadLocalPhoto("weight-today").then(setPhoto);
  }, []);
  const history = useQuery({ queryKey: ["weight"], queryFn: () => api<History>("/api/weight?days=90") });
  const save = useMutation({
    mutationFn: () => postOrQueue("/api/weight", { weightKg: Number(value), isMorning: true }),
    onSuccess: () => {
      track("weight_logged");
      setValue("");
      queryClient.invalidateQueries({ queryKey: ["weight"] });
      queryClient.invalidateQueries({ queryKey: ["dashboard"] });
    },
    onError: (caught) => setError(caught instanceof ApiError ? caught.message : "Weight was not saved."),
  });

  if (history.isLoading) return <Screen><LoadingState /></Screen>;
  if (history.isError || !history.data) return <Screen><ErrorState body="Weight history could not be loaded." onRetry={() => history.refetch()} /></Screen>;
  const data = history.data;
  const change = data.weeklyChangeKg;
  return (
    <Screen>
      <AppHeader title="Weight" subtitle="Morning weight is enough. The chart describes change and trend. It does not grade you." />
      <WeightTracker current={data.currentKg} target={data.targetKg} change={change} />
      <Text style={{ fontFamily: "Jakarta", color: colors.muted }}>7-day average {data.sevenDayAverageKg ?? "—"} kg · this month {data.monthlyChangeKg ?? 0} kg change</Text>
      {photo ? <Image source={{ uri: photo }} accessibilityLabel="Morning weight photo" style={{ width: "100%", height: 180, borderRadius: 16 }} /> : null}
      <SecondaryButton label="Add a photo" onPress={async () => setPhoto(await pickLocalPhoto("weight-today"))} />
      <SearchInput value={value} onChangeText={setValue} placeholder="Today's weight in kg" />
      <PrimaryButton label="Save morning weight" onPress={() => save.mutate()} />
      {error ? <ErrorState body={error} /> : null}
      {data.entries.map((entry) => (
        <Text key={entry.localDate} style={{ fontFamily: "Jakarta", color: colors.ink }}>{entry.localDate} · {entry.weightKg} kg</Text>
      ))}
    </Screen>
  );
}
