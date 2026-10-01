import { Pressable, Text, View } from "react-native";
import { useColors } from "./ui";

type TabRoute = { key: string; name: string };
type TabBarProps = {
  state: { index: number; routes: TabRoute[] };
  descriptors: Record<string, { options: { title?: string } }>;
  navigation: { navigate: (name: string) => void };
};

export function BottomNavigation({ state, descriptors, navigation }: TabBarProps) {
  const colors = useColors();
  return (
    <View style={{ flexDirection: "row", backgroundColor: colors.surface, borderTopWidth: 1, borderTopColor: colors.line, paddingBottom: 10, paddingTop: 8 }}>
      {state.routes.map((route, index) => {
        const focused = state.index === index;
        const label = descriptors[route.key]?.options.title ?? route.name;
        return (
          <Pressable
            key={route.key}
            accessibilityRole="tab"
            accessibilityState={{ selected: focused }}
            accessibilityLabel={label}
            onPress={() => navigation.navigate(route.name)}
            style={{ flex: 1, minHeight: 48, alignItems: "center", justifyContent: "center" }}
          >
            <Text style={{ fontFamily: "JakartaSemi", fontSize: 12, color: focused ? colors.primary : colors.muted }}>{label}</Text>
          </Pressable>
        );
      })}
    </View>
  );
}
