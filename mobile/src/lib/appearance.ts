import AsyncStorage from "@react-native-async-storage/async-storage";
import { create } from "zustand";

export type AppearanceMode = "system" | "light" | "dark";

type AppearanceState = {
  mode: AppearanceMode;
  setMode: (mode: AppearanceMode) => void;
  hydrate: () => Promise<void>;
};

export const useAppearance = create<AppearanceState>((set) => ({
  mode: "system",
  setMode: (mode) => {
    set({ mode });
    AsyncStorage.setItem("tevscare.appearance", mode).catch(() => undefined);
  },
  hydrate: async () => {
    const stored = await AsyncStorage.getItem("tevscare.appearance");
    if (stored === "light" || stored === "dark" || stored === "system") set({ mode: stored });
  },
}));
