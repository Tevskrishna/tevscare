import { useState } from "react";
import { Text } from "react-native";
import { ApiError, forgotPassword, resetPassword } from "../../src/api/client";
import { Screen } from "../../src/components/Screen";
import { AppHeader, ErrorState, PrimaryButton, SearchInput, useColors } from "../../src/components/ui";

export default function ForgotScreen() {
  const colors = useColors();
  const [email, setEmail] = useState("");
  const [token, setToken] = useState("");
  const [password, setPassword] = useState("");
  const [message, setMessage] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);

  async function requestToken() {
    setError(null);
    try {
      const result = await forgotPassword(email.trim());
      setMessage(result.message);
      if (result.developmentResetToken) setToken(result.developmentResetToken);
    } catch (caught) {
      setError(caught instanceof ApiError ? caught.message : "The reset could not be started.");
    }
  }

  async function reset() {
    setError(null);
    try {
      const result = await resetPassword(email.trim(), token.trim(), password);
      setMessage(result.message);
    } catch (caught) {
      setError(caught instanceof ApiError ? caught.message : "The password was not updated.");
    }
  }

  return (
    <Screen>
      <AppHeader title="Reset password" subtitle="If an account exists, a reset token is created. Email delivery is the production step. Local development can show the token here." />
      <SearchInput value={email} onChangeText={setEmail} placeholder="Email" />
      <PrimaryButton label="Send reset" onPress={requestToken} />
      <SearchInput value={token} onChangeText={setToken} placeholder="Reset token" />
      <SearchInput secure value={password} onChangeText={setPassword} placeholder="New password" />
      <PrimaryButton label="Update password" onPress={reset} />
      {message ? <Text style={{ fontFamily: "Jakarta", color: colors.ink }}>{message}</Text> : null}
      {error ? <ErrorState body={error} /> : null}
    </Screen>
  );
}
