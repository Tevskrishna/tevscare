import { Fraunces_600SemiBold } from "@expo-google-fonts/fraunces";
import { PlusJakartaSans_400Regular, PlusJakartaSans_500Medium, PlusJakartaSans_600SemiBold } from "@expo-google-fonts/plus-jakarta-sans";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { useFonts } from "expo-font";
import { Stack } from "expo-router";
import * as SplashScreen from "expo-splash-screen";
import { useEffect } from "react";
import { GestureHandlerRootView } from "react-native-gesture-handler";
import { SafeAreaProvider } from "react-native-safe-area-context";
import { addAnalyticsSink, track } from "../src/lib/analytics";
import "../src/lib/i18n";
import { api } from "../src/api/client";

SplashScreen.preventAutoHideAsync().catch(() => undefined);

const queryClient = new QueryClient({
  defaultOptions: { queries: { retry: 1, staleTime: 30_000 } },
});

addAnalyticsSink((name) => {
  api("/api/analytics/events", { method: "POST", body: JSON.stringify({ name }) }).catch(() => undefined);
});

export default function RootLayout() {
  const [ready] = useFonts({
    Fraunces: Fraunces_600SemiBold,
    Jakarta: PlusJakartaSans_400Regular,
    JakartaMedium: PlusJakartaSans_500Medium,
    JakartaSemi: PlusJakartaSans_600SemiBold,
  });

  useEffect(() => {
    if (ready) {
      SplashScreen.hideAsync().catch(() => undefined);
      track("app_opened");
    }
  }, [ready]);

  if (!ready) return null;

  return (
    <GestureHandlerRootView style={{ flex: 1 }}>
      <SafeAreaProvider>
        <QueryClientProvider client={queryClient}>
          <Stack screenOptions={{ headerShown: false, animation: "fade" }} />
        </QueryClientProvider>
      </SafeAreaProvider>
    </GestureHandlerRootView>
  );
}
