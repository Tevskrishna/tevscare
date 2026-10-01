import AsyncStorage from "@react-native-async-storage/async-storage";
import * as ImagePicker from "expo-image-picker";

export async function loadLocalPhoto(key: string) {
  return AsyncStorage.getItem(`tevscare.photo.${key}`);
}

export async function pickLocalPhoto(key: string) {
  const permission = await ImagePicker.requestMediaLibraryPermissionsAsync();
  if (!permission.granted) return null;
  const result = await ImagePicker.launchImageLibraryAsync({
    mediaTypes: ["images"],
    quality: 0.6,
  });
  if (result.canceled || !result.assets[0]) return null;
  const uri = result.assets[0].uri;
  await AsyncStorage.setItem(`tevscare.photo.${key}`, uri);
  return uri;
}
