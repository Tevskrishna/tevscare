import { ReactNode } from "react";
import {
  ActivityIndicator,
  Modal,
  Pressable,
  StyleSheet,
  Text,
  TextInput,
  View,
  useColorScheme,
} from "react-native";
import Svg, { Circle } from "react-native-svg";
import { dark, light, ThemeColors } from "../theme/tokens";

export function useColors(): ThemeColors {
  return useColorScheme() === "dark" ? dark : light;
}

export function AppHeader({ eyebrow, title, subtitle }: { eyebrow?: string; title: string; subtitle?: string }) {
  const colors = useColors();
  return (
    <View style={styles.header} accessibilityRole="header">
      {eyebrow ? <Text style={[styles.eyebrow, { color: colors.primary }]}>{eyebrow}</Text> : null}
      <Text style={[styles.title, { color: colors.ink }]}>{title}</Text>
      {subtitle ? <Text style={[styles.subtitle, { color: colors.muted }]}>{subtitle}</Text> : null}
    </View>
  );
}

export function SectionHeader({ title, action, onAction }: { title: string; action?: string; onAction?: () => void }) {
  const colors = useColors();
  return (
    <View style={styles.section}>
      <Text style={[styles.sectionTitle, { color: colors.ink }]}>{title}</Text>
      {action ? (
        <Pressable accessibilityRole="button" onPress={onAction} hitSlop={8}>
          <Text style={[styles.action, { color: colors.primary }]}>{action}</Text>
        </Pressable>
      ) : null}
    </View>
  );
}

export function Card({ children, onPress }: { children: ReactNode; onPress?: () => void }) {
  const colors = useColors();
  const body = <View style={[styles.card, { backgroundColor: colors.surface, borderColor: colors.line }]}>{children}</View>;
  if (!onPress) return body;
  return (
    <Pressable accessibilityRole="button" onPress={onPress}>
      {body}
    </Pressable>
  );
}

export function PrimaryButton({ label, onPress, disabled }: { label: string; onPress: () => void; disabled?: boolean }) {
  const colors = useColors();
  return (
    <Pressable
      accessibilityRole="button"
      accessibilityLabel={label}
      disabled={disabled}
      onPress={onPress}
      style={[styles.button, { backgroundColor: disabled ? colors.line : colors.primary }]}
    >
      <Text style={[styles.buttonText, { color: colors.onPrimary }]}>{label}</Text>
    </Pressable>
  );
}

export function SecondaryButton({ label, onPress }: { label: string; onPress: () => void }) {
  const colors = useColors();
  return (
    <Pressable accessibilityRole="button" accessibilityLabel={label} onPress={onPress} style={[styles.button, styles.secondary, { borderColor: colors.line }]}>
      <Text style={[styles.buttonText, { color: colors.ink }]}>{label}</Text>
    </Pressable>
  );
}

export function StatCard({ label, value, hint }: { label: string; value: string; hint?: string }) {
  const colors = useColors();
  return (
    <View style={[styles.stat, { backgroundColor: colors.surface, borderColor: colors.line }]}>
      <Text style={[styles.statLabel, { color: colors.muted }]}>{label}</Text>
      <Text style={[styles.statValue, { color: colors.ink }]}>{value}</Text>
      {hint ? <Text style={[styles.hint, { color: colors.muted }]}>{hint}</Text> : null}
    </View>
  );
}

export function ProgressBar({ value }: { value: number }) {
  const colors = useColors();
  const width = `${Math.max(0, Math.min(100, value))}%` as const;
  return (
    <View accessibilityRole="progressbar" accessibilityValue={{ min: 0, max: 100, now: value }} style={[styles.track, { backgroundColor: colors.soft }]}>
      <View style={[styles.fill, { width, backgroundColor: colors.primary }]} />
    </View>
  );
}

export function ProgressRing({ value, label }: { value: number; label: string }) {
  const colors = useColors();
  const size = 92;
  const stroke = 8;
  const radius = (size - stroke) / 2;
  const circumference = 2 * Math.PI * radius;
  const offset = circumference - (Math.max(0, Math.min(100, value)) / 100) * circumference;
  return (
    <View accessibilityLabel={`${label} ${value} percent`} style={styles.ringWrap}>
      <Svg width={size} height={size}>
        <Circle cx={size / 2} cy={size / 2} r={radius} stroke={colors.soft} strokeWidth={stroke} fill="none" />
        <Circle
          cx={size / 2}
          cy={size / 2}
          r={radius}
          stroke={colors.primary}
          strokeWidth={stroke}
          fill="none"
          strokeDasharray={`${circumference} ${circumference}`}
          strokeDashoffset={offset}
          strokeLinecap="round"
          rotation="-90"
          origin={`${size / 2}, ${size / 2}`}
        />
      </Svg>
      <View style={styles.ringLabel}>
        <Text style={[styles.ringValue, { color: colors.ink }]}>{value}</Text>
        <Text style={[styles.hint, { color: colors.muted }]}>{label}</Text>
      </View>
    </View>
  );
}

export function SearchInput({ value, onChangeText, placeholder, secure }: { value: string; onChangeText: (value: string) => void; placeholder: string; secure?: boolean }) {
  const colors = useColors();
  return (
    <TextInput
      accessibilityLabel={placeholder}
      value={value}
      onChangeText={onChangeText}
      placeholder={placeholder}
      placeholderTextColor={colors.muted}
      secureTextEntry={secure}
      autoCapitalize={secure ? "none" : "sentences"}
      style={[styles.input, { color: colors.ink, borderColor: colors.line, backgroundColor: colors.surface }]}
    />
  );
}

export function FoodQuantityInput({ value, onChange }: { value: number; onChange: (value: number) => void }) {
  const colors = useColors();
  return (
    <View style={styles.qty}>
      <Pressable accessibilityLabel="Decrease quantity" onPress={() => onChange(Math.max(0.5, Math.round((value - 0.5) * 10) / 10))} style={[styles.qtyButton, { borderColor: colors.line }]}>
        <Text style={[styles.qtyText, { color: colors.ink }]}>−</Text>
      </Pressable>
      <Text style={[styles.qtyValue, { color: colors.ink }]}>{value}</Text>
      <Pressable accessibilityLabel="Increase quantity" onPress={() => onChange(Math.min(20, Math.round((value + 0.5) * 10) / 10))} style={[styles.qtyButton, { borderColor: colors.line }]}>
        <Text style={[styles.qtyText, { color: colors.ink }]}>+</Text>
      </Pressable>
    </View>
  );
}

export function EmptyState({ title, body }: { title: string; body: string }) {
  const colors = useColors();
  return (
    <View style={styles.state}>
      <Text style={[styles.stateTitle, { color: colors.ink }]}>{title}</Text>
      <Text style={[styles.subtitle, { color: colors.muted, textAlign: "center" }]}>{body}</Text>
    </View>
  );
}

export function LoadingState({ label = "Loading" }: { label?: string }) {
  const colors = useColors();
  return (
    <View style={styles.state} accessibilityLabel={label}>
      <ActivityIndicator color={colors.primary} />
      <Text style={[styles.subtitle, { color: colors.muted }]}>{label}</Text>
    </View>
  );
}

export function ErrorState({ body, onRetry }: { body: string; onRetry?: () => void }) {
  const colors = useColors();
  return (
    <View style={styles.state}>
      <Text style={[styles.stateTitle, { color: colors.ink }]}>Something needs attention</Text>
      <Text style={[styles.subtitle, { color: colors.muted, textAlign: "center" }]}>{body}</Text>
      {onRetry ? <PrimaryButton label="Try again" onPress={onRetry} /> : null}
    </View>
  );
}

export function ConfirmationModal({
  visible,
  title,
  body,
  confirmLabel,
  onConfirm,
  onClose,
}: {
  visible: boolean;
  title: string;
  body: string;
  confirmLabel: string;
  onConfirm: () => void;
  onClose: () => void;
}) {
  const colors = useColors();
  return (
    <Modal visible={visible} transparent animationType="fade" onRequestClose={onClose}>
      <View style={styles.modalBackdrop}>
        <View style={[styles.modalCard, { backgroundColor: colors.surface }]}>
          <Text style={[styles.stateTitle, { color: colors.ink }]}>{title}</Text>
          <Text style={[styles.subtitle, { color: colors.muted }]}>{body}</Text>
          <PrimaryButton label={confirmLabel} onPress={onConfirm} />
          <SecondaryButton label="Cancel" onPress={onClose} />
        </View>
      </View>
    </Modal>
  );
}

export function BottomSheet({ visible, title, children, onClose }: { visible: boolean; title: string; children: ReactNode; onClose: () => void }) {
  const colors = useColors();
  return (
    <Modal visible={visible} transparent animationType="slide" onRequestClose={onClose}>
      <Pressable style={styles.sheetBackdrop} onPress={onClose}>
        <Pressable style={[styles.sheet, { backgroundColor: colors.surface }]} onPress={() => undefined}>
          <View style={[styles.handle, { backgroundColor: colors.line }]} />
          <Text style={[styles.sectionTitle, { color: colors.ink }]}>{title}</Text>
          {children}
        </Pressable>
      </Pressable>
    </Modal>
  );
}

export function Toast({ message }: { message: string | null }) {
  const colors = useColors();
  if (!message) return null;
  return (
    <View style={[styles.toast, { backgroundColor: colors.ink }]} accessibilityLiveRegion="polite">
      <Text style={{ color: colors.bg, fontFamily: "JakartaMedium" }}>{message}</Text>
    </View>
  );
}

export function Disclaimer() {
  const colors = useColors();
  return (
    <Text style={[styles.disclaimer, { color: colors.muted }]}>
      TEVSCARE provides meal-planning and habit-tracking information. It is not a substitute for medical diagnosis or treatment. Dietary restrictions should be confirmed with a qualified healthcare professional.
    </Text>
  );
}

const styles = StyleSheet.create({
  header: { gap: 6, marginBottom: 8 },
  eyebrow: { fontFamily: "JakartaSemi", letterSpacing: 1.1, textTransform: "uppercase", fontSize: 12 },
  title: { fontFamily: "Fraunces", fontSize: 32, lineHeight: 38 },
  subtitle: { fontFamily: "Jakarta", fontSize: 16, lineHeight: 23 },
  section: { flexDirection: "row", justifyContent: "space-between", alignItems: "center", marginTop: 22, marginBottom: 10 },
  sectionTitle: { fontFamily: "JakartaSemi", fontSize: 18 },
  action: { fontFamily: "JakartaSemi", fontSize: 15 },
  card: { borderWidth: 1, borderRadius: 20, padding: 16, gap: 8 },
  button: { minHeight: 52, borderRadius: 16, alignItems: "center", justifyContent: "center", paddingHorizontal: 16 },
  secondary: { backgroundColor: "transparent", borderWidth: 1 },
  buttonText: { fontFamily: "JakartaSemi", fontSize: 16 },
  stat: { flex: 1, borderWidth: 1, borderRadius: 18, padding: 14, gap: 4, minHeight: 96 },
  statLabel: { fontFamily: "Jakarta", fontSize: 13 },
  statValue: { fontFamily: "Fraunces", fontSize: 26 },
  hint: { fontFamily: "Jakarta", fontSize: 12 },
  track: { height: 8, borderRadius: 99, overflow: "hidden" },
  fill: { height: 8, borderRadius: 99 },
  ringWrap: { width: 92, height: 92, alignItems: "center", justifyContent: "center" },
  ringLabel: { position: "absolute", alignItems: "center" },
  ringValue: { fontFamily: "Fraunces", fontSize: 22 },
  input: { minHeight: 52, borderWidth: 1, borderRadius: 16, paddingHorizontal: 14, fontFamily: "Jakarta", fontSize: 16 },
  qty: { flexDirection: "row", alignItems: "center", gap: 12 },
  qtyButton: { width: 48, height: 48, borderRadius: 14, borderWidth: 1, alignItems: "center", justifyContent: "center" },
  qtyText: { fontSize: 22, fontFamily: "JakartaSemi" },
  qtyValue: { fontFamily: "Fraunces", fontSize: 24, minWidth: 36, textAlign: "center" },
  state: { alignItems: "center", gap: 12, paddingVertical: 28, paddingHorizontal: 12 },
  stateTitle: { fontFamily: "Fraunces", fontSize: 26, textAlign: "center" },
  modalBackdrop: { flex: 1, backgroundColor: "rgba(16,22,20,0.45)", justifyContent: "center", padding: 24 },
  modalCard: { borderRadius: 24, padding: 20, gap: 12 },
  sheetBackdrop: { flex: 1, justifyContent: "flex-end", backgroundColor: "rgba(16,22,20,0.35)" },
  sheet: { borderTopLeftRadius: 28, borderTopRightRadius: 28, padding: 20, gap: 12, minHeight: 240 },
  handle: { width: 48, height: 5, borderRadius: 99, alignSelf: "center" },
  toast: { position: "absolute", left: 20, right: 20, bottom: 24, borderRadius: 16, padding: 14 },
  disclaimer: { fontFamily: "Jakarta", fontSize: 12, lineHeight: 18, marginTop: 18 },
});
