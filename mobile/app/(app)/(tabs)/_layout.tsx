import { Tabs } from "expo-router";
import { BottomNavigation } from "../../../src/components/BottomNavigation";

export default function TabsLayout() {
  return (
      <Tabs tabBar={(props) => <BottomNavigation state={props.state} descriptors={props.descriptors} navigation={props.navigation} />} screenOptions={{ headerShown: false }}>
      <Tabs.Screen name="index" options={{ title: "Home" }} />
      <Tabs.Screen name="plan" options={{ title: "Plan" }} />
      <Tabs.Screen name="track" options={{ title: "Track" }} />
      <Tabs.Screen name="progress" options={{ title: "Progress" }} />
      <Tabs.Screen name="profile" options={{ title: "Profile" }} />
    </Tabs>
  );
}
