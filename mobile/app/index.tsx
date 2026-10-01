import { Redirect } from "expo-router";
import { useEffect, useState } from "react";
import { hydrateSession } from "../src/api/client";
import { flushQueue } from "../src/api/cache";
import { LoadingState } from "../src/components/ui";
import { Screen } from "../src/components/Screen";
import { useSession } from "../src/lib/session";

export default function Index() {
  const [ready, setReady] = useState(false);
  const user = useSession((state) => state.user);

  useEffect(() => {
    hydrateSession()
      .then(async (hydrated) => {
        useSession.getState().setUser(hydrated);
        if (hydrated) await flushQueue();
      })
      .finally(() => setReady(true));
  }, []);

  if (!ready) {
    return (
      <Screen>
        <LoadingState label="Opening TEVSCARE" />
      </Screen>
    );
  }
  if (!user) return <Redirect href="/(auth)/welcome" />;
  if (!user.onboardingCompleted) return <Redirect href="/(app)/onboarding" />;
  return <Redirect href="/(app)/(tabs)" />;
}
