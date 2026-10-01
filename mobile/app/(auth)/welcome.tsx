import { router } from "expo-router";
import { Text, View } from "react-native";
import { Screen } from "../../src/components/Screen";
import { AppHeader, Disclaimer, PrimaryButton, SecondaryButton, useColors } from "../../src/components/ui";

export default function WelcomeScreen() {
  const colors = useColors();
  return (
    <Screen>
      <View style={{ flex: 1, justifyContent: "flex-end", gap: 18, minHeight: 560 }}>
        <Text style={{ fontFamily: "JakartaSemi", color: colors.primary, letterSpacing: 2 }}>TEVSCARE</Text>
        <AppHeader title="Know what to do today." subtitle="A calm place for your meal plan, water, movement, sleep and food budget. The plan is guidance you can follow and edit, not a medical promise." />
        <PrimaryButton label="Create an account" onPress={() => router.push("/(auth)/register")} />
        <SecondaryButton label="I already have an account" onPress={() => router.push("/(auth)/login")} />
        <Disclaimer />
      </View>
    </Screen>
  );
}
