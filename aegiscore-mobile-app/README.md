# AegisCore Mobile Apps

Cross-platform mobile applications for the AegisCore University ERP system, built by Nocturne Systems (Pvt.) Ltd.

## Apps Included

| Platform | Directory | Technology |
|----------|-----------|------------|
| React Native (iOS + Android) | `react-native/` | Expo SDK, TypeScript, Redux, WatermelonDB |
| Android Native | `android/` | Kotlin, Jetpack Compose, Gradle |
| iOS Native | `ios/` | Swift, SwiftUI, Swift Package Manager |

## Quick Start

### React Native (Expo)

```bash
cd react-native
npm install
npx expo start
```

### Android Native

```bash
cd android
./gradlew assembleDebug
```

### iOS Native

```bash
cd ios
swift build
```

## Key Features

- **Offline-first architecture**: WatermelonDB local database with sync queue
- **Biometric authentication**: Face ID / Touch ID / fingerprint
- **Push notifications**: Firebase Cloud Messaging + APN
- **Role switching**: Seamlessly switch between student, faculty, admin views
- **Multi-language**: English, Urdu, Arabic, Malay
- **Post-quantum safe**: PQC transport layer for all API communication

## Connecting to Backend

Update the API base URL in:
- React Native: `src/api/client.ts` → `AEGISCORE_API_URL`
- Android: `app/src/main/kotlin/.../Api.kt` → `BASE_URL`
- iOS: `AegisCore/ApiClient.swift` → `baseURL`

## Build & Deploy

- **EAS Build**: `eas build --platform ios` or `eas build --platform android`
- **Android Play Store**: GitHub Actions workflow in `android/.github/workflows/`
- **iOS App Store**: Configured via `eas.json`

## Documentation

- [Full specification](docs/specification.md)
- [Docker integration architecture](docs/docker-mobile-architecture.md)
- [Mobile manual](docs/mobile-manual.html)

## License

Proprietary — Nocturne Systems (Pvt.) Ltd. All rights reserved.
