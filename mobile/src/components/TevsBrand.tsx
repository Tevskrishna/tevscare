import { Text } from "react-native";
import { useColors } from "./ui";

export function TevsBrand() {
  const colors = useColors();
  return (
    <Text accessibilityRole="text" accessibilityLabel="Powered by TEVS" style={{ fontFamily: "Jakarta", fontSize: 13, color: colors.muted, textAlign: "center", marginTop: 16 }}>
      Powered by TEVS
    </Text>
  );
}
