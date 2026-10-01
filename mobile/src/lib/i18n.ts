import i18n from "i18next";
import { initReactI18next } from "react-i18next";

const english = {
  settings: "Settings",
  home: "Home",
  plan: "Plan",
  track: "Track",
  progress: "Progress",
  profile: "Profile",
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
