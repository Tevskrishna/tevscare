export function waterPercent(goalMl: number, consumedMl: number) {
  if (goalMl <= 0) return 0;
  return Math.min(100, Math.round((Math.max(0, consumedMl) / goalMl) * 100));
}

export function remainingWater(goalMl: number, consumedMl: number) {
  return Math.max(0, goalMl - Math.max(0, consumedMl));
}

export function suggestWaterMl(weightKg: number) {
  if (weightKg <= 0) return 2500;
  return Math.min(3500, Math.max(2000, Math.round(weightKg * 35)));
}

export function budgetRemaining(dailyBudget: number, spent: number) {
  return Math.round((dailyBudget - spent) * 100) / 100;
}

export function isQuietTime(hour: number, minute: number, start: string, end: string, enabled: boolean) {
  if (!enabled) return false;
  const current = hour * 60 + minute;
  const [startHour, startMinute] = start.split(":").map(Number);
  const [endHour, endMinute] = end.split(":").map(Number);
  const startTotal = startHour * 60 + startMinute;
  const endTotal = endHour * 60 + endMinute;
  if (startTotal === endTotal) return false;
  if (startTotal < endTotal) return current >= startTotal && current < endTotal;
  return current >= startTotal || current < endTotal;
}

export function mealLabel(type: string) {
  const labels: Record<string, string> = {
    Breakfast: "Breakfast",
    MidMorning: "Mid-morning",
    Lunch: "Lunch",
    AfternoonSnack: "Afternoon",
    Dinner: "Dinner",
  };
  return labels[type] ?? type;
}

export function formatInr(value: number) {
  const rounded = Math.round(value);
  return `₹${rounded.toLocaleString("en-IN")}`;
}

export function adherenceTone(score: number) {
  if (score >= 80) return "strong";
  if (score >= 40) return "steady";
  return "starting";
}
