import { Text } from "react-native";
import { Screen } from "../../src/components/Screen";
import { AppHeader, Disclaimer, useColors } from "../../src/components/ui";

export default function AboutScreen() {
  const colors = useColors();
  return (
    <Screen>
      <AppHeader title="About TEVSCARE" subtitle="A meal plan, a daily log, and a food budget." />
      <Text style={{ fontFamily: "Jakarta", color: colors.ink, lineHeight: 22 }}>
        The starter plan is a 15-day breakfast rotation with almonds, walnuts and a protein choice, plus lunch, snacks, dinner, water, activity and sleep notes. A nutritionist can replace that content through the API. The free plan is the current entitlement. Paid plans are represented, but no payment is collected in this version.
      </Text>
      <Disclaimer />
    </Screen>
  );
}
