import { Tabs } from "expo-router";
import { useTranslation } from "react-i18next";
import { BottomNavigation } from "../../../src/components/BottomNavigation";

export default function TabsLayout() {
  const { t } = useTranslation();
  return (
    <Tabs tabBar={(props) => <BottomNavigation state={props.state} descriptors={props.descriptors} navigation={props.navigation} />} screenOptions={{ headerShown: false }}>
      <Tabs.Screen name="index" options={{ title: t("home") }} />
      <Tabs.Screen name="plan" options={{ title: t("plan") }} />
      <Tabs.Screen name="track" options={{ title: t("track") }} />
      <Tabs.Screen name="progress" options={{ title: t("progress") }} />
      <Tabs.Screen name="profile" options={{ title: t("profile") }} />
    </Tabs>
  );
}
