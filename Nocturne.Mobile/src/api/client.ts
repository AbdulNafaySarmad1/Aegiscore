import axios from 'axios';
import * as SecureStore from 'expo-secure-store';

export const apiClient = axios.create({
  baseURL: process.env.EXPO_PUBLIC_API_BASE_URL ?? 'https://nocturne.ssuet.edu.pk/api',
  timeout: 10000
});

apiClient.interceptors.request.use(async (config) => {
  const token = await SecureStore.getItemAsync('nocturne.session');
  if (token) config.headers.Authorization = `Bearer ${token}`;
  return config;
});

export async function registerPushToken(userId: string, token: string) {
  await apiClient.post(`/user/${encodeURIComponent(userId)}/push-token`, { token });
}
