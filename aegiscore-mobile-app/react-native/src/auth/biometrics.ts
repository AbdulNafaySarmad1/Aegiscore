import * as LocalAuthentication from 'expo-local-authentication';
import * as SecureStore from 'expo-secure-store';

const SESSION_KEY = process.env.EXPO_PUBLIC_SESSION_KEY;

function sessionKey(): string {
  if (!SESSION_KEY) throw new Error('Mobile session key configuration is missing.');
  return SESSION_KEY;
}

export async function saveSession(token: string) {
  await SecureStore.setItemAsync(sessionKey(), token, {
    keychainAccessible: SecureStore.AFTER_FIRST_UNLOCK_THIS_DEVICE_ONLY
  });
}

export async function unlockSession() {
  const hasHardware = await LocalAuthentication.hasHardwareAsync();
  const enrolled = await LocalAuthentication.isEnrolledAsync();
  if (!hasHardware || !enrolled) return null;

  const result = await LocalAuthentication.authenticateAsync({
    promptMessage: 'Unlock AegisCore'
  });

  return result.success ? SecureStore.getItemAsync(sessionKey()) : null;
}
