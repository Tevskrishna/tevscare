import { useMutation, useQueryClient } from "@tanstack/react-query";
import { useState } from "react";
import { ApiError } from "../../src/api/client";
import { postOrQueue } from "../../src/api/cache";
import { Screen } from "../../src/components/Screen";
import { AppHeader, ErrorState, PrimaryButton, SearchInput } from "../../src/components/ui";

export default function SleepScreen() {
  const queryClient = useQueryClient();
  const [minutes, setMinutes] = useState("480");
  const [bedtime, setBedtime] = useState("22:30");
  const [wake, setWake] = useState("06:30");
  const [error, setError] = useState<string | null>(null);
  const save = useMutation({
    mutationFn: () => postOrQueue("/api/sleep", { durationMinutes: Number(minutes), bedtime, wakeTime: wake }),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ["dashboard"] }),
    onError: (caught) => setError(caught instanceof ApiError ? caught.message : "Sleep was not saved."),
  });
  return (
    <Screen>
      <AppHeader title="Sleep" subtitle="Record last night in minutes. 480 is eight hours. This is a log, not a diagnosis." />
      <SearchInput value={bedtime} onChangeText={setBedtime} placeholder="Bedtime, 22:30" />
      <SearchInput value={wake} onChangeText={setWake} placeholder="Wake time, 06:30" />
      <SearchInput value={minutes} onChangeText={setMinutes} placeholder="Minutes slept" />
      <PrimaryButton label="Save sleep" onPress={() => save.mutate()} />
      {error ? <ErrorState body={error} /> : null}
    </Screen>
  );
}
