import { Text } from "react-native";
import { Screen } from "../../src/components/Screen";
import { TevsBrand } from "../../src/components/TevsBrand";
import { AppHeader, Disclaimer, useColors } from "../../src/components/ui";

export default function PrivacyScreen() {
  const colors = useColors();
  return (
    <Screen>
      <AppHeader title="Privacy" subtitle="Placeholder for the published privacy policy." />
      <Text style={{ fontFamily: "Jakarta", color: colors.ink, lineHeight: 22 }}>
        TEVSCARE stores the account, body measurements, meal logs, water, weight, activity and sleep you enter so the plan and progress screens can work. Passwords are hashed. Tokens stay in secure device storage. The app does not claim a specific regulatory certification.
      </Text>
      <Disclaimer />
      <AppHeader title="Terms" subtitle="Placeholder for the published terms of service." />
      <Text style={{ fontFamily: "Jakarta", color: colors.ink, lineHeight: 22 }}>
        The meal plan is provider guidance for planning and habit tracking. It is not medical care, and it does not promise a weight change.
      </Text>
      <TevsBrand />
    </Screen>
  );
}
