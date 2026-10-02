import i18n from "i18next";
import { initReactI18next } from "react-i18next";

const english = {
  settings: "Settings",
  home: "Home",
  plan: "Plan",
  track: "Track",
  progress: "Progress",
  profile: "Profile",
  greeting: "Hello",
  loadingToday: "Preparing today",
  offlinePlan: "Connection unavailable. Please try again.",
  planWindowDone: "This plan window is complete.",
  planDay: "Day {{day}} of {{total}}",
  finishSetup: "Finish setup to see a plan.",
  logged: "logged",
  nextMeal: "Next meal",
  planned: "Planned",
  estimated: "estimated",
  logMeal: "Log this meal",
  markComplete: "Mark complete",
  water: "Water",
  activity: "Activity",
  sleep: "Sleep",
  budget: "Budget",
  weight: "Weight",
  goal: "Goal",
  spent: "spent",
  change: "change",
  notLogged: "Not logged",
  estimate: "Estimate",
  quickActions: "Quick actions",
  logWeight: "Log weight",
  addWater: "Add water",
  addActivity: "Add activity",
  checkIn: "Check in",
};

const resources = Object.fromEntries(
  ["en", "te", "hi", "ta", "kn", "ml", "bn", "mr"].map((locale) => [locale, { translation: english }]),
);

i18n.use(initReactI18next).init({
  resources,
  lng: "en",
  fallbackLng: "en",
  interpolation: { escapeValue: false },
});

export default i18n;
