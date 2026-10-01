import { useQuery } from "@tanstack/react-query";
import { Text } from "react-native";
import { api } from "../../src/api/client";
import { Screen } from "../../src/components/Screen";
import { AppHeader, Disclaimer, LoadingState, useColors } from "../../src/components/ui";

type Guidance = { disclaimer: string; items: { id: string; title: string; body: string; label: string; conditionKey?: string | null }[] };

export default function GuidanceScreen() {
  const colors = useColors();
  const guidance = useQuery({ queryKey: ["guidance"], queryFn: () => api<Guidance>("/api/guidance") });
  if (guidance.isLoading || !guidance.data) return <Screen><LoadingState /></Screen>;
  return (
    <Screen>
      <AppHeader title="Plan guidance" subtitle="These notes come from the plan provider. They are not universal medical rules." />
      {guidance.data.items.map((item) => (
        <Text key={item.id} style={{ fontFamily: "Jakarta", color: colors.ink }}>
          {item.label}: {item.title}. {item.body}{item.conditionKey ? ` Applies only when ${item.conditionKey} is relevant to the person.` : ""}
        </Text>
      ))}
      <Disclaimer />
    </Screen>
  );
}
