# Android Cleanup Report

Date: 2026-07-26

## Current Native Android State

- Source copied to `aegiscore-android/`.
- API endpoint uses `AEGISCORE_API_BASE_URL`.
- `compileSdk` and `targetSdk` are 35.
- `minSdk` is 26.
- No generated `.gradle` or build output was copied.

## Dependency Review

Current dependencies are used by the declared app capabilities:

- Compose UI/activity/material: UI screens.
- Lifecycle viewmodel compose: MVVM state.
- Biometric: biometric auth.
- Room + KSP compiler: offline storage.
- Retrofit + Moshi converter: HTTP client.
- Firebase messaging: push notifications.

## Local Build

Not run in this workspace because the Gradle wrapper/toolchain is not present in the copied native project. Build verification remains gated on Android Studio or a checked-in Gradle wrapper.

## Standalone Repository Files

- Added `aegiscore-android/.gitignore`.
- Added `aegiscore-android/README.md`.
- Added `aegiscore-android/CONTRIBUTING.md`.
- Added `aegiscore-android/.github/workflows/android-build.yml`.
- Added `aegiscore-android/.github/workflows/android-deploy.yml`.
