# Native Mobile And Docker

Docker runs the Nocturne backend only. Android and iOS apps are native binaries installed on phones.

```
Phone app
  -> HTTPS API call
  -> Kubernetes load balancer
  -> Dockerized .NET Nocturne API pods
  -> PostgreSQL / Redis / object storage
```

There is no direct Docker-to-phone connection. Mobile apps use `https://api.nocturne.<university-domain>/api/*`.
Set `NOCTURNE_API_BASE_URL` at Android build time, and set the iOS `NOCTURNE_API_BASE_URL` Info.plist value per university build.

## Local Build

Android requires Android Studio/Gradle. iOS requires Xcode on macOS. Those toolchains are not installed in this Linux workspace.
