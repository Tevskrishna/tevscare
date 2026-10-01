import { create } from "zustand";
import { SessionUser } from "../api/client";

type SessionState = {
  user: SessionUser | null;
  ready: boolean;
  setUser: (user: SessionUser | null) => void;
  setReady: (ready: boolean) => void;
};

export const useSession = create<SessionState>((set) => ({
  user: null,
  ready: false,
  setUser: (user) => set({ user }),
  setReady: (ready) => set({ ready }),
}));
