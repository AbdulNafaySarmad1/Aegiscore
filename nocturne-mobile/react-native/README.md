# Nocturne Mobile

Expo-managed React Native shell for student and faculty mobile apps.

## Checks

```bash
npm install
npm run typecheck
```

## Build

```bash
npx eas build --profile preview --platform android
npx eas build --profile production --platform all
```

Set `EXPO_PUBLIC_API_BASE_URL` for non-production APIs. Store credentials stay in `expo-secure-store`; no secrets are committed.
