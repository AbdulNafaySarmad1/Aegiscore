# AegisCore Android

Official native Android app for AegisCore student and faculty workflows.

## Setup

1. Open this folder in Android Studio.
2. Set the API URL:

```bash
export AEGISCORE_API_BASE_URL=http://localhost:8080
```

3. Build:

```bash
./gradlew build
```

## Release

```bash
./gradlew bundleRelease
```

Upload the generated AAB through Google Play Console after signing credentials are configured.
