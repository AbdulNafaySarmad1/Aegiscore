import * as LocalAuthentication from 'expo-local-authentication';
import * as SecureStore from 'expo-secure-store';

const SESSION_KEY = 'nocturne.session';

export async function saveSession(token: string) {
  await SecureStore.setItemAsync(SESSION_KEY, token, {
    keychainAccessible: SecureStore.AFTER_FIRST_UNLOCK_THIS_DEVICE_ONLY
  });
}

export async function unlockSession() {
  const hasHardware = await LocalAuthentication.hasHardwareAsync();
  const enrolled = await LocalAuthentication.isEnrolledAsync();
  if (!hasHardware || !enrolled) return null;

  const result = await LocalAuthentication.authenticateAsync({
    promptMessage: 'Unlock Nocturne'
  });

  return result.success ? SecureStore.getItemAsync(SESSION_KEY) : null;
}
