import { zodResolver } from "@hookform/resolvers/zod";
import { router } from "expo-router";
import { useState } from "react";
import { Controller, useForm } from "react-hook-form";
import { z } from "zod";
import { ApiError, register } from "../../src/api/client";
import { useSession } from "../../src/lib/session";
import { Screen } from "../../src/components/Screen";
import { TevsBrand } from "../../src/components/TevsBrand";
import { AppHeader, ErrorState, PrimaryButton, SearchInput } from "../../src/components/ui";

const schema = z.object({
  fullName: z.string().min(2, "Add your name"),
  email: z.string().email("Enter a valid email"),
  password: z.string().min(8).regex(/[A-Z]/).regex(/[a-z]/).regex(/[0-9]/),
});

export default function RegisterScreen() {
  const [error, setError] = useState<string | null>(null);
  const form = useForm({ resolver: zodResolver(schema), defaultValues: { fullName: "", email: "", password: "" } });

  async function onSubmit(values: z.infer<typeof schema>) {
    setError(null);
    try {
      const user = await register(values.fullName.trim(), values.email.trim(), values.password, Intl.DateTimeFormat().resolvedOptions().timeZone || "Asia/Kolkata");
      useSession.getState().setUser(user);
      router.replace("/(app)/onboarding");
    } catch (caught) {
      setError(caught instanceof ApiError ? caught.message : "The account could not be created.");
    }
  }

  return (
    <Screen>
      <AppHeader eyebrow="Start" title="Create your account" subtitle="Password needs 8 characters, with upper case, lower case and a number." />
      <Controller control={form.control} name="fullName" render={({ field }) => <SearchInput value={field.value} onChangeText={field.onChange} placeholder="Name" />} />
      <Controller control={form.control} name="email" render={({ field }) => <SearchInput value={field.value} onChangeText={field.onChange} placeholder="Email" />} />
      <Controller control={form.control} name="password" render={({ field }) => <SearchInput secure value={field.value} onChangeText={field.onChange} placeholder="Password" />} />
      {error ? <ErrorState body={error} /> : null}
      <PrimaryButton label="Continue" onPress={form.handleSubmit(onSubmit)} disabled={form.formState.isSubmitting} />
      <TevsBrand />
    </Screen>
  );
}
