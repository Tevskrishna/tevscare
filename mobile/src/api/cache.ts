import AsyncStorage from "@react-native-async-storage/async-storage";
import { ApiError, api } from "./client";

export async function cachedGet<T>(key: string, path: string): Promise<T> {
  try {
    const data = await api<T>(path);
    await AsyncStorage.setItem(key, JSON.stringify(data));
    return data;
  } catch (error) {
    if (error instanceof ApiError && error.status === 0) {
      const cached = await AsyncStorage.getItem(key);
      if (cached) return JSON.parse(cached) as T;
    }
    throw error;
  }
}

type Job = { id: string; path: string; method: string; body?: unknown };

export async function enqueue(path: string, method: string, body?: unknown) {
  const current = JSON.parse((await AsyncStorage.getItem("tevscare.queue")) ?? "[]") as Job[];
  current.push({ id: `${Date.now()}`, path, method, body });
  await AsyncStorage.setItem("tevscare.queue", JSON.stringify(current));
}

export async function flushQueue() {
  const current = JSON.parse((await AsyncStorage.getItem("tevscare.queue")) ?? "[]") as Job[];
  const remaining: Job[] = [];
  for (const job of current) {
    try {
      await api(job.path, { method: job.method, body: job.body ? JSON.stringify(job.body) : undefined });
    } catch (error) {
      if (error instanceof ApiError && error.status === 0) remaining.push(job);
    }
  }
  await AsyncStorage.setItem("tevscare.queue", JSON.stringify(remaining));
}

export async function postOrQueue<T>(path: string, body: unknown): Promise<T | null> {
  try {
    return await api<T>(path, { method: "POST", body: JSON.stringify(body) });
  } catch (error) {
    if (error instanceof ApiError && error.status === 0) {
      await enqueue(path, "POST", body);
      return null;
    }
    throw error;
  }
}
