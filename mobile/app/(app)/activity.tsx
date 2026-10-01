import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useState } from "react";
import { ApiError, api } from "../../src/api/client";
import { postOrQueue } from "../../src/api/cache";
import { Screen } from "../../src/components/Screen";
import { AppHeader, ErrorState, PrimaryButton, SearchInput } from "../../src/components/ui";

export default function ActivityScreen() {
  const queryClient = useQueryClient();
  const [minutes, setMinutes] = useState("30");
  const [error, setError] = useState<string | null>(null);
  const today = useQuery({ queryKey: ["activity"], queryFn: () => api<{ durationMinutes: number; goalMinutes: number } | null>("/api/activity") });
  const save = useMutation({
    mutationFn: () => postOrQueue("/api/activity", { activityType: "Walk", durationMinutes: Number(minutes), completed: true }),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ["activity"] }),
    onError: (caught) => setError(caught instanceof ApiError ? caught.message : "Activity was not saved."),
  });
  return (
    <Screen>
      <AppHeader title="Activity" subtitle={today.data ? `${today.data.durationMinutes} of ${today.data.goalMinutes} minutes logged today.` : "A daily walk is the default. Log the minutes you actually moved."} />
      <SearchInput value={minutes} onChangeText={setMinutes} placeholder="Minutes" />
      <PrimaryButton label="Save walk" onPress={() => save.mutate()} />
      {error ? <ErrorState body={error} /> : null}
    </Screen>
  );
}
