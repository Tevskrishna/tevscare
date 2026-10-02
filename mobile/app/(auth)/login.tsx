import { zodResolver } from "@hookform/resolvers/zod";
import { router } from "expo-router";
import { useState } from "react";
import { Controller, useForm } from "react-hook-form";
import { Pressable, Text } from "react-native";
import { z } from "zod";
import { ApiError, login } from "../../src/api/client";
import { useSession } from "../../src/lib/session";
import { Screen } from "../../src/components/Screen";
import { TevsBrand } from "../../src/components/TevsBrand";
import { AppHeader, ErrorState, PrimaryButton, SearchInput, useColors } from "../../src/components/ui";

const schema = z.object({
  email: z.string().email("Enter a valid email"),
  password: z.string().min(8, "Use at least 8 characters"),
});

export default function LoginScreen() {
  const colors = useColors();
  const [error, setError] = useState<string | null>(null);
  const form = useForm({ resolver: zodResolver(schema), defaultValues: { email: "", password: "" } });

  async function onSubmit(values: z.infer<typeof schema>) {
    setError(null);
    try {
      const user = await login(values.email.trim(), values.password);
      useSession.getState().setUser(user);
      router.replace(user.onboardingCompleted ? "/(app)/(tabs)" : "/(app)/onboarding");
    } catch (caught) {
      setError(caught instanceof ApiError ? caught.message : "Sign-in did not complete.");
    }
  }

  return (
    <Screen>
      <AppHeader eyebrow="Welcome back" title="Sign in" subtitle="Your plan and logs stay on your account." />
      <Controller control={form.control} name="email" render={({ field }) => <SearchInput value={field.value} onChangeText={field.onChange} placeholder="Email" />} />
      <Controller control={form.control} name="password" render={({ field }) => <SearchInput secure value={field.value} onChangeText={field.onChange} placeholder="Password" />} />
      {error ? <ErrorState body={error} /> : null}
      <PrimaryButton label={form.formState.isSubmitting ? "Signing in" : "Sign in"} disabled={form.formState.isSubmitting} onPress={form.handleSubmit(onSubmit)} />
      <Pressable accessibilityRole="button" onPress={() => router.push("/(auth)/forgot")}>
        <Text style={{ fontFamily: "JakartaSemi", color: colors.primary }}>Forgot password</Text>
      </Pressable>
      <TevsBrand />
    </Screen>
  );
}
