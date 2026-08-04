# AegisCore Mobile

Separate mobile workspace for AegisCore.

## Structure

- `react-native/`: Expo app.
- `android/`: native Kotlin app.
- `ios/`: native Swift app.

## API URL

Use one backend URL across all mobile clients:

```bash
AEGISCORE_API_BASE_URL=http://localhost:8080
EXPO_PUBLIC_API_URL=http://localhost:8080
```

## Checks

```bash
cd react-native
npm install
npm run typecheck
npm test
```

Native Android and iOS builds require Android Studio/Gradle and Xcode/Swift toolchains.
