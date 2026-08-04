# AegisCore — Native Mobile Architecture & Docker Integration (U13)

## Role
You are a mobile architect + DevOps engineer explaining:
1. **Kotlin for Android** (native, compiled to machine code)
2. **Swift for iOS** (native, compiled to machine code)
3. **How Docker relates to mobile apps** (spoiler: it doesn't directly)
4. **How mobile apps connect to backend** (HTTP API calls)

---

## Part A — Understanding the Architecture

### THE KEY CONCEPT: Docker ≠ Mobile App

**WRONG:**
```
"Docker image → runs on phone"
❌ This won't work. Docker is for servers, not phones.
```

**CORRECT:**
```
Phone (Kotlin/Swift App)
    ↓
    Makes HTTP request to: api.aegiscore.university.edu.pk
    ↓
Internet
    ↓
Kubernetes/Docker cluster (Azure or on-prem)
    ├─ Container 1: AegisCore API (C# .NET in Docker)
    ├─ Container 2: AegisCore API (C# .NET in Docker)
    └─ Container 3: AegisCore API (C# .NET in Docker)
    ↓
PostgreSQL database
```

### Flow Explained

```
1. Student opens AegisCore app on iPhone
   ├─ App is Swift code compiled to native iPhone binary
   └─ Binary runs on iPhone processor (ARM)

2. Student taps "Check Attendance"
   ├─ App makes HTTP request: GET /api/attendance/student/abc123
   └─ Request goes over internet to: api.aegiscore.university.edu.pk

3. Request arrives at Kubernetes cluster
   ├─ Load balancer receives request
   └─ Routes to one of 3 pods (Docker containers)

4. Inside Docker container
   ├─ C# .NET code handles request
   ├─ Queries PostgreSQL database
   └─ Returns JSON response

5. Response comes back to iPhone
   ├─ Swift app parses JSON
   ├─ Updates UI
   └─ Student sees attendance: "45/50 classes (90%)"

6. Next request uses same process
   └─ Docker just processes requests, doesn't know about the phone
```

**Key insight:** Docker doesn't "link to" the app. The app calls the API that's running inside Docker.

---

## Part B — Kotlin for Android (Native)

### Why Kotlin Over React Native?

| Feature | Kotlin (Native) | React Native |
|---------|-----------------|--------------|
| Performance | 100% native speed | ~90% (JavaScript bridge) |
| Battery | Optimized for ARM | Extra JS overhead |
| Storage | Direct access to device | Abstracted |
| UI | Native Android UI | Custom implementation |
| Offline | Perfect WatermelonDB | Requires extra setup |
| App size | 15-30 MB | 50-70 MB |
| Time to build | Longer (separate codebase) | Faster (shared code) |

**For universities:** Kotlin is better because:
- Students expect native performance
- Battery matters (all-day campus)
- Offline attendance marking needs to be snappy

### Kotlin + Android Architecture

```
AegisCore Android App (Kotlin)
├─ UI Layer
│  ├─ LoginActivity
│  ├─ DashboardActivity
│  ├─ AttendanceFragment
│  ├─ GradesFragment
│  └─ FeesFragment
│
├─ ViewModel Layer (MVVM)
│  ├─ AttendanceViewModel
│  ├─ GradesViewModel
│  └─ LoginViewModel
│
├─ Repository Layer (API calls)
│  ├─ AttendanceRepository
│  ├─ GradesRepository
│  └─ UserRepository
│
├─ Network Layer
│  └─ Retrofit HTTP client
│      └─ Calls: api.aegiscore.university.edu.pk/api/*
│
├─ Local Storage
│  ├─ Room database (offline cache)
│  ├─ SharedPreferences (auth tokens)
│  └─ Files (course materials)
│
└─ Services
   ├─ AttendanceSyncService (offline queue)
   ├─ NotificationService (FCM)
   └─ LocationService (optional: campus tracking)
```

### Example: Android Attendance Marking (Kotlin)

```kotlin
// AttendanceViewModel.kt
import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import kotlinx.coroutines.launch
import retrofit2.HttpException

class AttendanceViewModel(
    private val attendanceRepository: AttendanceRepository
) : ViewModel() {
    
    private val _attendanceState = MutableLiveData<AttendanceUiState>()
    val attendanceState: LiveData<AttendanceUiState> = _attendanceState
    
    fun markAttendance(studentId: String, status: String) {
        viewModelScope.launch {
            try {
                _attendanceState.value = AttendanceUiState.Loading
                
                // Check if online
                if (isNetworkAvailable()) {
                    // Online: send immediately to backend
                    val response = attendanceRepository.markAttendanceOnline(
                        studentId = studentId,
                        status = status
                    )
                    _attendanceState.value = AttendanceUiState.Success(response)
                } else {
                    // Offline: store locally, sync later
                    attendanceRepository.markAttendanceOffline(
                        studentId = studentId,
                        status = status
                    )
                    _attendanceState.value = AttendanceUiState.OfflineSynced(
                        message = "Marked offline, will sync when online"
                    )
                }
            } catch (e: HttpException) {
                // Network error: fall back to offline
                attendanceRepository.markAttendanceOffline(
                    studentId = studentId,
                    status = status
                )
                _attendanceState.value = AttendanceUiState.OfflineSynced(
                    message = "No internet, saved locally"
                )
            } catch (e: Exception) {
                _attendanceState.value = AttendanceUiState.Error(e.message ?: "Unknown error")
            }
        }
    }
    
    fun syncOfflineAttendance() {
        viewModelScope.launch {
            try {
                if (!isNetworkAvailable()) {
                    _attendanceState.value = AttendanceUiState.Error("No internet connection")
                    return@launch
                }
                
                _attendanceState.value = AttendanceUiState.Syncing
                
                // Get offline records
                val offlineRecords = attendanceRepository.getOfflineRecords()
                
                // Send to backend
                val syncResponse = attendanceRepository.syncOfflineRecords(offlineRecords)
                
                // Clear offline queue
                attendanceRepository.clearOfflineQueue()
                
                _attendanceState.value = AttendanceUiState.SyncSuccess(
                    message = "Synced ${syncResponse.count} records"
                )
            } catch (e: Exception) {
                _attendanceState.value = AttendanceUiState.Error("Sync failed: ${e.message}")
            }
        }
    }
    
    private fun isNetworkAvailable(): Boolean {
        val connectivityManager = context.getSystemService(Context.CONNECTIVITY_SERVICE) as ConnectivityManager
        val network = connectivityManager.activeNetwork ?: return false
        val capabilities = connectivityManager.getNetworkCapabilities(network) ?: return false
        return when {
            capabilities.hasTransport(NetworkCapabilities.TRANSPORT_WIFI) -> true
            capabilities.hasTransport(NetworkCapabilities.TRANSPORT_CELLULAR) -> true
            capabilities.hasTransport(NetworkCapabilities.TRANSPORT_ETHERNET) -> true
            else -> false
        }
    }
}

// AttendanceRepository.kt
class AttendanceRepository(
    private val attendanceApi: AttendanceApi,
    private val attendanceDao: AttendanceDao, // Room database
    private val context: Context
) {
    
    // Online API call
    suspend fun markAttendanceOnline(
        studentId: String,
        status: String
    ): MarkAttendanceResponse {
        return attendanceApi.markAttendance(
            MarkAttendanceRequest(
                studentId = studentId,
                status = status
            )
        )
    }
    
    // Offline storage
    suspend fun markAttendanceOffline(
        studentId: String,
        status: String
    ) {
        val offlineRecord = OfflineAttendanceRecord(
            id = UUID.randomUUID().toString(),
            studentId = studentId,
            status = status,
            markedAt = System.currentTimeMillis(),
            synced = false
        )
        attendanceDao.insertOfflineRecord(offlineRecord)
    }
    
    suspend fun getOfflineRecords(): List<OfflineAttendanceRecord> {
        return attendanceDao.getUnsyncedRecords()
    }
    
    suspend fun syncOfflineRecords(
        records: List<OfflineAttendanceRecord>
    ): SyncResponse {
        return attendanceApi.syncOfflineAttendance(records)
    }
    
    suspend fun clearOfflineQueue() {
        attendanceDao.deleteAllSynced()
    }
}

// AttendanceApi.kt (Retrofit interface)
interface AttendanceApi {
    @POST("api/attendance/mark")
    suspend fun markAttendance(
        @Body request: MarkAttendanceRequest
    ): MarkAttendanceResponse
    
    @POST("api/attendance/sync-offline")
    suspend fun syncOfflineAttendance(
        @Body records: List<OfflineAttendanceRecord>
    ): SyncResponse
    
    @GET("api/attendance/student/{studentId}")
    suspend fun getStudentAttendance(
        @Path("studentId") studentId: String
    ): AttendanceResponse
}

// Room Database entities
@Entity(tableName = "offline_attendance")
data class OfflineAttendanceRecord(
    @PrimaryKey val id: String,
    val studentId: String,
    val status: String, // present, absent, late
    val markedAt: Long, // timestamp
    val synced: Boolean = false
)

@Dao
interface AttendanceDao {
    @Insert(onConflict = OnConflictStrategy.REPLACE)
    suspend fun insertOfflineRecord(record: OfflineAttendanceRecord)
    
    @Query("SELECT * FROM offline_attendance WHERE synced = 0")
    suspend fun getUnsyncedRecords(): List<OfflineAttendanceRecord>
    
    @Query("DELETE FROM offline_attendance WHERE synced = 1")
    suspend fun deleteAllSynced()
}
```

### Build Locally

```bash
# Android Studio project structure
aegiscore-android/
├─ app/src/main/kotlin/
│  ├─ MainActivity.kt
│  ├─ ui/
│  │  ├─ attendance/
│  │  ├─ grades/
│  │  └─ login/
│  ├─ viewmodel/
│  ├─ repository/
│  ├─ network/
│  │  └─ AttendanceApi.kt
│  └─ database/
│     └─ AttendanceDao.kt
├─ app/src/main/res/
│  ├─ layout/
│  ├─ values/
│  └─ drawables/
├─ build.gradle.kts
└─ AndroidManifest.xml

# Build command
./gradlew build

# APK output
app/build/outputs/apk/release/app-release.apk (~30 MB)

# Or Android App Bundle (for Play Store)
./gradlew bundleRelease

# Output
app/build/outputs/bundle/release/app-release.aab (~15 MB, auto-compressed on download)
```

---

## Part C — Swift for iOS (Native)

### Swift + iOS Architecture

```
AegisCore iOS App (Swift)
├─ SwiftUI Views
│  ├─ LoginView
│  ├─ DashboardView
│  ├─ AttendanceView
│  ├─ GradesView
│  └─ FeesView
│
├─ ViewModels (ObservableObject)
│  ├─ AttendanceViewModel
│  ├─ GradesViewModel
│  └─ LoginViewModel
│
├─ Services (API layer)
│  ├─ AttendanceService
│  ├─ UserService
│  └─ NetworkService
│
├─ Networking
│  └─ URLSession HTTP client
│      └─ Calls: api.aegiscore.university.edu.pk/api/*
│
├─ Local Storage
│  ├─ Core Data (offline cache)
│  ├─ Keychain (auth tokens, secure)
│  └─ FileManager (documents, materials)
│
└─ Services
   ├─ AttendanceSyncService (offline queue)
   ├─ NotificationService (APNs)
   └─ BiometricService (Face ID, Touch ID)
```

### Example: iOS Attendance Marking (Swift)

```swift
// AttendanceViewModel.swift
import SwiftUI
import Combine

@MainActor
class AttendanceViewModel: ObservableObject {
    @Published var state: AttendanceState = .idle
    @Published var offlineSyncCount: Int = 0
    
    private let attendanceService: AttendanceService
    private let networkMonitor: NetworkMonitor
    
    init(attendanceService: AttendanceService = .shared) {
        self.attendanceService = attendanceService
        self.networkMonitor = NetworkMonitor.shared
    }
    
    func markAttendance(studentId: String, status: String) {
        Task {
            do {
                state = .loading
                
                if networkMonitor.isOnline {
                    // Online: send immediately
                    let response = try await attendanceService.markAttendanceOnline(
                        studentId: studentId,
                        status: status
                    )
                    state = .success(message: "Attendance marked")
                } else {
                    // Offline: store locally
                    try await attendanceService.markAttendanceOffline(
                        studentId: studentId,
                        status: status
                    )
                    state = .offlineSynced(message: "Marked offline, will sync when online")
                }
            } catch {
                // Network error: fall back to offline
                try? await attendanceService.markAttendanceOffline(
                    studentId: studentId,
                    status: status
                )
                state = .offlineSynced(message: "No internet, saved locally")
            }
        }
    }
    
    func syncOfflineAttendance() {
        Task {
            do {
                guard networkMonitor.isOnline else {
                    state = .error("No internet connection")
                    return
                }
                
                state = .syncing
                
                let offlineRecords = try await attendanceService.getOfflineRecords()
                offlineSyncCount = offlineRecords.count
                
                let syncResponse = try await attendanceService.syncOfflineRecords(offlineRecords)
                
                try await attendanceService.clearOfflineQueue()
                
                state = .syncSuccess(message: "Synced \(syncResponse.count) records")
            } catch {
                state = .error("Sync failed: \(error.localizedDescription)")
            }
        }
    }
}

enum AttendanceState {
    case idle
    case loading
    case success(message: String)
    case offlineSynced(message: String)
    case syncing
    case syncSuccess(message: String)
    case error(String)
}

// AttendanceService.swift
actor AttendanceService {
    static let shared = AttendanceService()
    
    private let apiClient: APIClient
    private let coreDataManager: CoreDataManager
    
    nonisolated private static let decoder: JSONDecoder = {
        let decoder = JSONDecoder()
        decoder.dateDecodingStrategy = .iso8601
        return decoder
    }()
    
    // Online API call
    func markAttendanceOnline(
        studentId: String,
        status: String
    ) async throws -> MarkAttendanceResponse {
        let request = MarkAttendanceRequest(
            studentId: studentId,
            status: status
        )
        
        return try await apiClient.post(
            endpoint: "api/attendance/mark",
            body: request
        )
    }
    
    // Offline storage (Core Data)
    func markAttendanceOffline(
        studentId: String,
        status: String
    ) async throws {
        let offlineRecord = OfflineAttendanceRecord(context: coreDataManager.context)
        offlineRecord.id = UUID()
        offlineRecord.studentId = studentId
        offlineRecord.status = status
        offlineRecord.markedAt = Date()
        offlineRecord.synced = false
        
        try coreDataManager.save()
    }
    
    func getOfflineRecords() async throws -> [OfflineAttendanceRecord] {
        let fetchRequest = OfflineAttendanceRecord.fetchRequest()
        fetchRequest.predicate = NSPredicate(format: "synced == false")
        
        return try coreDataManager.context.fetch(fetchRequest)
    }
    
    func syncOfflineRecords(
        _ records: [OfflineAttendanceRecord]
    ) async throws -> SyncResponse {
        return try await apiClient.post(
            endpoint: "api/attendance/sync-offline",
            body: records
        )
    }
    
    func clearOfflineQueue() async throws {
        let fetchRequest = OfflineAttendanceRecord.fetchRequest()
        fetchRequest.predicate = NSPredicate(format: "synced == true")
        
        let records = try coreDataManager.context.fetch(fetchRequest)
        for record in records {
            coreDataManager.context.delete(record)
        }
        
        try coreDataManager.save()
    }
}

// APIClient.swift (URLSession wrapper)
actor APIClient {
    private let baseURL: URL = {
        URL(string: "https://api.aegiscore.university.edu.pk")!
    }()
    
    func post<T: Decodable>(
        endpoint: String,
        body: Encodable
    ) async throws -> T {
        var request = URLRequest(url: baseURL.appendingPathComponent(endpoint))
        request.httpMethod = "POST"
        request.setValue("application/json", forHTTPHeaderField: "Content-Type")
        
        // Add auth token from Keychain
        if let token = KeychainManager.getToken() {
            request.setValue("Bearer \(token)", forHTTPHeaderField: "Authorization")
        }
        
        let encoder = JSONEncoder()
        encoder.dateEncodingStrategy = .iso8601
        request.httpBody = try encoder.encode(body)
        
        let (data, response) = try await URLSession.shared.data(for: request)
        
        guard let httpResponse = response as? HTTPURLResponse else {
            throw APIError.invalidResponse
        }
        
        guard (200...299).contains(httpResponse.statusCode) else {
            throw APIError.httpError(status: httpResponse.statusCode)
        }
        
        let decoder = JSONDecoder()
        decoder.dateDecodingStrategy = .iso8601
        return try decoder.decode(T.self, from: data)
    }
}

// CoreData Model (OfflineAttendanceRecord)
@objc(OfflineAttendanceRecord)
public class OfflineAttendanceRecord: NSManagedObject {
    @NSManaged public var id: UUID
    @NSManaged public var studentId: String
    @NSManaged public var status: String // present, absent, late
    @NSManaged public var markedAt: Date
    @NSManaged public var synced: Bool
}

// Keychain Helper
class KeychainManager {
    static func saveToken(_ token: String) {
        let query: [String: Any] = [
            kSecClass as String: kSecClassGenericPassword,
            kSecAttrAccount as String: "aegiscore-auth-token",
            kSecValueData as String: token.data(using: .utf8)!
        ]
        
        SecItemDelete(query as CFDictionary)
        SecItemAdd(query as CFDictionary, nil)
    }
    
    static func getToken() -> String? {
        let query: [String: Any] = [
            kSecClass as String: kSecClassGenericPassword,
            kSecAttrAccount as String: "aegiscore-auth-token",
            kSecReturnData as String: true
        ]
        
        var result: AnyObject?
        SecItemCopyMatching(query as CFDictionary, &result)
        
        guard let data = result as? Data else { return nil }
        return String(data: data, encoding: .utf8)
    }
}
```

### Build Locally

```bash
# Xcode project structure
aegiscore-ios/
├─ AegisCore.xcodeproj
├─ AegisCore/
│  ├─ AegisCoreApp.swift (SwiftUI entry point)
│  ├─ Views/
│  │  ├─ LoginView.swift
│  │  ├─ DashboardView.swift
│  │  └─ AttendanceView.swift
│  ├─ ViewModels/
│  ├─ Services/
│  ├─ Networking/
│  │  └─ APIClient.swift
│  ├─ Storage/
│  │  └─ CoreDataManager.swift
│  └─ Assets/
├─ AegisCore.xcdatamodeld (Core Data schema)
└─ Info.plist

# Build command (via Xcode)
xcodebuild -scheme AegisCore -configuration Release

# IPA output
build/Release-iphoneos/AegisCore.app

# Or via XCode Cloud
# → Automatic CI/CD, signing, App Store submission
```

---

## Part D — Docker Backend (The Piece That Connects Both)

### What Students Don't See

```
Student's Phone (Kotlin/Swift)
    ↓
    HTTPS Request
    ↓
Internet
    ↓
Kubernetes LoadBalancer (cloud or on-prem)
    ↓
Docker Container (Pod 1)
├─ Language: C# .NET 10
├─ Runtime: .NET runtime
├─ Code: Your AegisCore API
└─ Purpose: Handle requests, return JSON
    ↓
PostgreSQL Database
```

### Docker Image Build (Backend)

```dockerfile
# Dockerfile (for AegisCore API)
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

WORKDIR /app

# Copy csproj and restore dependencies
COPY ["AegisCore.csproj", "./"]
RUN dotnet restore "AegisCore.csproj"

# Copy source code
COPY . .

# Build and publish
RUN dotnet publish -c Release -o /app/publish

# Runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0

WORKDIR /app

# Copy published files from build stage
COPY --from=build /app/publish .

# Environment variables
ENV ASPNETCORE_URLS=http://+:5000
ENV ASPNETCORE_ENVIRONMENT=Production

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=40s --retries=3 \
    CMD curl -f http://localhost:5000/health || exit 1

# Expose port
EXPOSE 5000

# Run app
ENTRYPOINT ["dotnet", "AegisCore.dll"]
```

### Build & Push Docker Image

```bash
# Build Docker image
docker build -t aegiscore-api:1.0.0 .

# Tag for container registry (Azure example)
docker tag aegiscore-api:1.0.0 aegiscorepk.azurecr.io/aegiscore-api:1.0.0

# Login to registry
echo $AZURE_PASSWORD | docker login -u aegiscore --password-stdin aegiscorepk.azurecr.io

# Push to registry
docker push aegiscorepk.azurecr.io/aegiscore-api:1.0.0

# Kubernetes automatically pulls this image and runs it:
# → 3 pods, each running this Docker image
# → Each pod is an instance of your API
```

---

## Part E — How Everything Connects

### The Complete Picture

```
┌─────────────────────────────────────────────────────────────────┐
│                    AEGISCORE ECOSYSTEM                            │
└─────────────────────────────────────────────────────────────────┘

🍎 iOS Student App (Swift)        📱 Android Student App (Kotlin)
│                                  │
├─ SwiftUI Views                  ├─ Jetpack Compose UI
├─ URLSession HTTP calls          ├─ Retrofit HTTP client
├─ Core Data offline cache        ├─ Room offline database
├─ Keychain secure storage        ├─ SharedPreferences tokens
└─ BiometricKit (Face ID/Touch)   └─ BiometricPrompt (Fingerprint)
│                                  │
└──────────────────┬───────────────┘
                   │
                   │ HTTPS Requests
                   │ (REST API)
                   ↓
        api.aegiscore.university.edu.pk
                   │
        ┌──────────┴──────────┐
        │                     │
   Cloudflare           Kubernetes Cluster
   (DDoS Protection)    (Azure SG or On-Prem)
        │                     │
        │         ┌───────────┼───────────┐
        │         ↓           ↓           ↓
        │      Pod 1       Pod 2       Pod 3
        │      ┌────┐      ┌────┐      ┌────┐
        └─────→│API │      │API │      │API │
               └────┘      ┘────┘      └────┘
               ↓ ↓ ↓       ↓ ↓ ↓       ↓ ↓ ↓
               │ │ │       │ │ │       │ │ │
               Cache     Cache     Cache
               │ │ │       │ │ │       │ │ │
               └─┼─┼───────┼─┼─┼───────┼─┼─┘
                 │         │         │
                 └─────────┼─────────┘
                           ↓
                    PostgreSQL (Primary)
                           ├─ Replication (12hr lag)
                           └─ Replicas (read-only)
```

### Data Flow: Student Marks Attendance

```
1. Student opens AegisCore App (Swift)
   ├─ App is native compiled machine code
   └─ Runs on iPhone processor (ARM)

2. Student taps "Mark Present"
   ├─ Swift app checks: isNetworkAvailable?
   │  ├─ YES → Continue to step 3
   │  └─ NO → Jump to step 3b
   │
   └─ Step 3: Online path
      ├─ Create HTTP request:
      │  ├─ Method: POST
      │  ├─ URL: https://api.aegiscore.university.edu.pk/api/attendance/mark
      │  ├─ Headers: { Authorization: Bearer <jwt_token> }
      │  └─ Body: { studentId: "abc123", status: "present" }
      │
      ├─ URLSession sends request over internet
      │
      ├─ Request arrives at Kubernetes LoadBalancer
      │  └─ LoadBalancer picks a Pod (round-robin)
      │
      ├─ Pod runs Docker container (C# .NET API)
      │  ├─ Route handler: POST /api/attendance/mark
      │  ├─ Validate JWT token (is this really student abc123?)
      │  ├─ Query PostgreSQL: INSERT attendance record
      │  └─ Return JSON: { message: "Attendance marked" }
      │
      ├─ Response travels back to iPhone
      │
      └─ Swift app receives response
         ├─ Parse JSON
         ├─ Update UI: "✅ Marked Present at 2:30 PM"
         └─ Store in Core Data (for offline caching)

   └─ Step 3b: Offline path
      ├─ URLSession throws: "No internet"
      │
      ├─ Swift app catches error
      │
      ├─ Store locally in Core Data:
      │  ├─ studentId: "abc123"
      │  ├─ status: "present"
      │  ├─ markedAt: 2:30 PM
      │  └─ synced: false
      │
      └─ Show UI: "⚠️ Marked offline, will sync when online"

3. Later: Student walks to cafeteria (gets internet)
   ├─ App detects: networkMonitor.isOnline = true
   │
   ├─ App retrieves all offline records from Core Data
   │  ├─ 5 attendance records marked offline
   │  └─ 3 grade views cached offline
   │
   ├─ App creates sync request:
   │  ├─ Method: POST
   │  ├─ URL: https://api.aegiscore.university.edu.pk/api/attendance/sync-offline
   │  └─ Body: [
   │      { studentId: "abc123", status: "present", markedAt: "2:30 PM" },
   │      { studentId: "abc123", status: "present", markedAt: "3:15 PM" },
   │      ... (5 total)
   │    ]
   │
   ├─ Docker container (API) receives sync
   │  ├─ Validate: all records belong to this student
   │  ├─ Check for duplicates (prevent double-marking)
   │  ├─ Insert into PostgreSQL
   │  ├─ Mark records as synced
   │  └─ Return: { synced: 5 }
   │
   └─ Swift app updates Core Data
      ├─ Set synced = true for these records
      └─ Show: "✅ Synced 5 records"
```

---

## Part F — Key Differences: Kotlin vs Swift vs React Native

### Compilation & Execution

```
KOTLIN (Android):
  Source code (.kt files)
    ↓
  Kotlin compiler
    ↓
  Bytecode (.class files)
    ↓
  Android Runtime (ART)
    ↓
  Native machine code
    ↓
  Runs on Android device

SWIFT (iOS):
  Source code (.swift files)
    ↓
  Swift compiler (LLVM)
    ↓
  Machine code (ARM binary)
    ↓
  Runs directly on iOS device
    (No VM, pure native)

REACT NATIVE:
  Source code (JavaScript)
    ↓
  Metro bundler
    ↓
  JavaScript bundle
    ↓
  JavaScript engine (Hermes/V8)
    ↓
  Native bridge (converts JS → native calls)
    ↓
  Runs on device (hybrid)
```

### Performance Comparison

| Operation | Kotlin | Swift | React Native |
|-----------|--------|-------|--------------|
| App launch | ~200ms | ~150ms | ~500ms |
| Offline sync | ~100ms (10 records) | ~80ms | ~200ms |
| List scroll | 60fps | 60fps | 55-58fps |
| Memory usage | 80MB | 65MB | 150MB |
| Battery drain | ~1.5%/hour | ~1.2%/hour | ~2.5%/hour |
| Binary size | 25MB | 18MB | 60MB |

---

## Implementation Plan

### Phase 1: Setup (Week 1)
- [ ] Create Android Studio project (Kotlin)
- [ ] Create Xcode project (Swift)
- [ ] Set up Git repositories
- [ ] Configure CI/CD (GitHub Actions for both)

### Phase 2: Core Architecture (Weeks 2-3)
- [ ] API client (Retrofit for Android, URLSession for iOS)
- [ ] Authentication (JWT token handling)
- [ ] Offline storage (Room for Android, Core Data for iOS)
- [ ] Error handling + retry logic

### Phase 3: Features (Weeks 4-8)
- [ ] Login screen (both platforms)
- [ ] Dashboard (both)
- [ ] Attendance marking (both)
- [ ] Offline sync (both)
- [ ] Grades view (both)
- [ ] Fees payment (both)

### Phase 4: Polish (Weeks 9-10)
- [ ] UI/UX refinement
- [ ] Performance optimization
- [ ] Security audit (Keychain/SharedPreferences)
- [ ] Testing + bug fixes

### Phase 5: Submission (Weeks 11-12)
- [ ] App Store submission (iOS)
- [ ] Play Store submission (Android)
- [ ] Release notes + marketing

---

## Checklist

- [ ] Kotlin + Android project setup
- [ ] Swift + iOS project setup
- [ ] Retrofit HTTP client (Android)
- [ ] URLSession HTTP client (iOS)
- [ ] Room database (Android offline)
- [ ] Core Data (iOS offline)
- [ ] JWT authentication
- [ ] Offline sync mechanism
- [ ] Network monitoring (online/offline detection)
- [ ] Error handling + retry logic
- [ ] Biometric authentication (optional but recommended)
- [ ] Push notifications (FCM for Android, APNs for iOS)
- [ ] App Store + Play Store submission

---

**This is native mobile done right: Fast, battery-efficient, offline-capable.**

Docker runs on servers. Mobile apps make HTTP calls to Docker APIs. That's the connection. 🚀
