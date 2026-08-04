# Native Mobile And Docker

Docker runs the AegisCore backend only. Android and iOS apps are native binaries installed on phones.

```
Phone app
  -> HTTPS API call
  -> Kubernetes load balancer
  -> Dockerized .NET AegisCore API pods
  -> PostgreSQL / Redis / object storage
```

There is no direct Docker-to-phone connection. Mobile apps use `https://api.aegiscore.<university-domain>/api/*`.
Set `AEGISCORE_API_BASE_URL` at Android build time, and set the iOS `AEGISCORE_API_BASE_URL` Info.plist value per university build.

## Local Build

Android requires Android Studio/Gradle. iOS requires Xcode on macOS. Those toolchains are not installed in this Linux workspace.
