import { router } from "expo-router";
import { Screen } from "../../../src/components/Screen";
import { AppHeader, Card, SectionHeader } from "../../../src/components/ui";

const links = [
  ["Water", "/(app)/water", "Add a glass and see what is left today."],
  ["Weight", "/(app)/weight", "Record a morning weight. The chart shows change, not a grade."],
  ["Meals", "/(app)/log-meal", "Save what you actually ate, including a different quantity."],
  ["Activity", "/(app)/activity", "Log today's walk or other movement."],
  ["Sleep", "/(app)/sleep", "Record how long you slept."],
  ["Check-in", "/(app)/check-in", "A short end-of-day review, with optional mood."],
] as const;

export default function TrackScreen() {
  return (
    <Screen>
      <AppHeader title="Track" subtitle="Log the day in one place. Planned meals and actual meals stay separate." />
      <SectionHeader title="Today" />
      {links.map(([title, href, body]) => (
        <Card key={title} onPress={() => router.push(href)}>
          <AppHeader title={title} subtitle={body} />
        </Card>
      ))}
    </Screen>
  );
}
