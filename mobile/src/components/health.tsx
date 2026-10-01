import { Pressable, StyleSheet, Text, View } from "react-native";
import { formatInr, mealLabel } from "../lib/planning";
import { useColors } from "./ui";

export type MealItem = {
  id: string;
  foodId?: string | null;
  name: string;
  localName?: string | null;
  quantity: number;
  unit: string;
  alternativeGroup?: string | null;
  isDefaultAlternative: boolean;
  note?: string | null;
  estimatedCost?: number | null;
};

export type Meal = {
  id: string;
  mealType: string;
  title: string;
  scheduledTime: string;
  notes?: string | null;
  estimatedCost: number;
  logStatus?: string | null;
  items: MealItem[];
};

export function MealCard({ meal, onPress }: { meal: Meal; onPress?: () => void }) {
  const colors = useColors();
  const status = meal.logStatus ?? "Planned";
  return (
    <Pressable accessibilityRole="button" accessibilityLabel={`${mealLabel(meal.mealType)} ${meal.title}`} onPress={onPress} style={[styles.card, { backgroundColor: colors.surface, borderColor: colors.line }]}>
      <View style={styles.row}>
        <Text style={[styles.kicker, { color: colors.primary }]}>{mealLabel(meal.mealType)} · {meal.scheduledTime}</Text>
        <Text style={[styles.status, { color: colors.muted }]}>{status}</Text>
      </View>
      <Text style={[styles.title, { color: colors.ink }]}>{meal.title}</Text>
      <Text style={[styles.body, { color: colors.muted }]} numberOfLines={2}>
        {meal.items.map((item) => item.name).join(" · ")}
      </Text>
      <Text style={[styles.cost, { color: colors.ink }]}>{formatInr(meal.estimatedCost)} estimated</Text>
    </Pressable>
  );
}

export function FoodCard({ name, localName, detail, onPress }: { name: string; localName?: string | null; detail: string; onPress?: () => void }) {
  const colors = useColors();
  return (
    <Pressable accessibilityRole="button" onPress={onPress} style={[styles.card, { backgroundColor: colors.surface, borderColor: colors.line }]}>
      <Text style={[styles.title, { color: colors.ink }]}>{name}</Text>
      {localName ? <Text style={[styles.body, { color: colors.primary }]}>{localName}</Text> : null}
      <Text style={[styles.body, { color: colors.muted }]}>{detail}</Text>
    </Pressable>
  );
}

export function WaterTracker({ consumed, goal, percent }: { consumed: number; goal: number; percent: number }) {
  const colors = useColors();
  return (
    <View>
      <Text style={[styles.title, { color: colors.ink }]}>{consumed} / {goal} ml</Text>
      <Text style={[styles.body, { color: colors.muted }]}>{percent}% of today's personal goal</Text>
    </View>
  );
}

export function WeightTracker({ current, target, change }: { current?: number | null; target?: number | null; change?: number | null }) {
  const colors = useColors();
  const changeText = change == null ? "No change recorded yet" : `${change > 0 ? "+" : ""}${change.toFixed(1)} kg from the start`;
  return (
    <View>
      <Text style={[styles.title, { color: colors.ink }]}>{current ?? "—"} kg</Text>
      <Text style={[styles.body, { color: colors.muted }]}>Goal {target ?? "—"} kg · {changeText}</Text>
    </View>
  );
}

export function HabitTracker({ title, done, onToggle }: { title: string; done: boolean; onToggle: () => void }) {
  const colors = useColors();
  return (
    <Pressable accessibilityRole="checkbox" accessibilityState={{ checked: done }} onPress={onToggle} style={[styles.habit, { borderColor: colors.line, backgroundColor: done ? colors.soft : colors.surface }]}>
      <Text style={[styles.mark, { color: colors.primary }]}>{done ? "✓" : "○"}</Text>
      <Text style={[styles.body, { color: colors.ink, flex: 1 }]}>{title}</Text>
    </Pressable>
  );
}

export function ReminderCard({ title, body, time }: { title: string; body: string; time: string }) {
  const colors = useColors();
  return (
    <View style={[styles.card, { backgroundColor: colors.soft, borderColor: "transparent" }]}>
      <Text style={[styles.kicker, { color: colors.primaryDark }]}>{time}</Text>
      <Text style={[styles.title, { color: colors.ink }]}>{title}</Text>
      <Text style={[styles.body, { color: colors.muted }]}>{body}</Text>
    </View>
  );
}

export function DailyTimeline({ meals, onPress }: { meals: Meal[]; onPress: (meal: Meal) => void }) {
  return (
    <View style={{ gap: 10 }}>
      {meals.map((meal) => (
        <MealCard key={meal.id} meal={meal} onPress={() => onPress(meal)} />
      ))}
    </View>
  );
}

const styles = StyleSheet.create({
  card: { borderWidth: 1, borderRadius: 20, padding: 16, gap: 6 },
  row: { flexDirection: "row", justifyContent: "space-between" },
  kicker: { fontFamily: "JakartaSemi", fontSize: 13 },
  status: { fontFamily: "Jakarta", fontSize: 13 },
  title: { fontFamily: "Fraunces", fontSize: 22 },
  body: { fontFamily: "Jakarta", fontSize: 15, lineHeight: 21 },
  cost: { fontFamily: "JakartaMedium", fontSize: 14, marginTop: 4 },
  habit: { minHeight: 56, borderWidth: 1, borderRadius: 16, paddingHorizontal: 14, flexDirection: "row", alignItems: "center", gap: 10 },
  mark: { fontFamily: "JakartaSemi", fontSize: 18, width: 24 },
});
