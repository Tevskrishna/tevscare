import { Redirect, Stack } from "expo-router";
import { useSession } from "../../src/lib/session";

export default function AppLayout() {
  const user = useSession((state) => state.user);
  if (!user) return <Redirect href="/(auth)/welcome" />;
  return <Stack screenOptions={{ headerShown: false, animation: "slide_from_right" }} />;
}
