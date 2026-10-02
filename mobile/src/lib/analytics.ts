type AnalyticsEvent =
  | "app_opened"
  | "onboarding_completed"
  | "meal_viewed"
  | "meal_completed"
  | "meal_logged"
  | "water_logged"
  | "weight_logged"
  | "activity_logged"
  | "plan_completed"
  | "plan_opened"
  | "notification_opened"
  | "shopping_list_opened"
  | "budget_viewed"
  | "checkin_completed"
  | "reminder_enabled";

type Sink = (name: AnalyticsEvent, properties?: Record<string, string>) => void;

const sinks: Sink[] = [];

export function addAnalyticsSink(sink: Sink) {
  sinks.push(sink);
}

export function track(name: AnalyticsEvent, properties?: Record<string, string>) {
  if (__DEV__) console.info("analytics", name);
  sinks.forEach((sink) => sink(name, properties));
}
