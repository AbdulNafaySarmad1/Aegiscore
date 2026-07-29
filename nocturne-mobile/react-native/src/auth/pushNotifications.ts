import * as Notifications from 'expo-notifications';
import { registerPushToken } from '../api/client';

Notifications.setNotificationHandler({
  handleNotification: async () => ({
    shouldShowAlert: true,
    shouldShowBanner: true,
    shouldShowList: true,
    shouldPlaySound: true,
    shouldSetBadge: true
  })
});

export async function setupPushNotifications(userId: string) {
  const permission = await Notifications.getPermissionsAsync();
  if (!permission.granted) {
    const requested = await Notifications.requestPermissionsAsync();
    if (!requested.granted) return null;
  }

  const token = await Notifications.getExpoPushTokenAsync();
  await registerPushToken(userId, token.data);
  return token.data;
}
