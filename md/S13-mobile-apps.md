# Nocturne — Sprint 13: Mobile Apps (Student + Faculty Native Apps)

## Role
You are a senior .NET 10 / C# 14 engineer building native mobile apps for Nocturne. These are lightweight complements to the web portals, optimized for offline access and mobile-first UX. Use React Native for code sharing (80%+) while maintaining native excellence on iOS and Android.

---

## Stack Decision
- **Framework**: React Native (Expo-managed workflow for simplicity)
- **Language**: TypeScript
- **State Management**: Redux Toolkit
- **Navigation**: React Navigation
- **API Client**: TanStack React Query (for caching + sync)
- **Offline**: SQLite + WatermelonDB (encrypted local DB)
- **Push Notifications**: Firebase Cloud Messaging (FCM) + Apple Push Notification (APN)
- **Build & Distribution**: EAS (Expo Application Services) for iOS/Android builds

---

## Task 1 — Shared Mobile Infrastructure

### Project Structure
```
nocturne-mobile/
├── apps/
│   ├── student/          # Student app entrypoint
│   ├── faculty/          # Faculty app entrypoint
├── packages/
│   ├── shared-ui/        # Reusable UI components
│   ├── shared-api/       # API client + types
│   ├── shared-auth/      # Authentication service
│   ├── shared-db/        # WatermelonDB schema
│   ├── shared-utils/     # Helpers, formatting, validation
├── app.json              # Expo config
├── eas.json              # EAS build config
```

### Offline-First Database
```typescript
// packages/shared-db/schema.ts
import { appSchema, tableSchema } from '@nozbe/watermelondb';

const courseSchema = tableSchema({
  name: 'courses',
  columns: [
    { name: 'course_id', type: 'string', isIndexed: true },
    { name: 'course_code', type: 'string' },
    { name: 'course_name', type: 'string' },
    { name: 'credits', type: 'number' },
    { name: 'faculty_id', type: 'string', isIndexed: true },
    { name: 'enrollment_count', type: 'number' },
    { name: 'synced_at', type: 'number' }
  ]
});

const gradSchema = tableSchema({
  name: 'grades',
  columns: [
    { name: 'grade_id', type: 'string', isIndexed: true },
    { name: 'course_id', type: 'string', isIndexed: true },
    { name: 'student_id', type: 'string', isIndexed: true },
    { name: 'letter_grade', type: 'string' },
    { name: 'gpa_points', type: 'number' },
    { name: 'synced_at', type: 'number' }
  ]
});

export const schema = appSchema({
  version: 1,
  tables: [courseSchema, gradSchema, /* ... */]
});
```

### API Client + Sync Engine
```typescript
// packages/shared-api/index.ts
import { QueryClient } from '@tanstack/react-query';

export const apiClient = axios.create({
  baseURL: 'https://nocturne.ssuet.edu.pk/api',
  timeout: 10000
});

// Offline-first sync strategy
export const syncManager = {
  async syncData(userId: string) {
    const db = getDatabase();
    
    try {
      // Fetch fresh data from server
      const response = await apiClient.get(`/user/${userId}/sync`);
      
      // Merge with local DB
      const coursesCollection = db.collections.get('courses');
      await db.batch(
        ...response.data.courses.map(course =>
          coursesCollection.prepareCreateFromDirtyRaw(course)
        )
      );
      
      // Update sync timestamp
      await updateSyncTimestamp(userId);
    } catch (error) {
      console.error('Sync failed, working offline', error);
      // Gracefully degrade to cached data
    }
  }
};
```

---

## Task 2 — Student Mobile App

### Screens (iOS + Android)

#### 1. Authentication
- Sign in with Microsoft 365 (OAuth)
- Biometric unlock (Face ID / Fingerprint)
- Session persistence

#### 2. Dashboard
- Current semester overview
- GPA (current + cumulative)
- Attendance % with visual gauge
- Fee balance (red if overdue)
- Quick action buttons: "Pay Fee", "View Results", "Download Hall Ticket"

#### 3. Attendance
- Course-wise attendance breakdown (pie chart)
- Semester-wise attendance history (bar chart)
- Shortage alert (red banner if < 75%)
- Sync: works offline, syncs when online

#### 4. Grades & Results
- Course-wise grades (card view)
- GPA calculator (scroll through semesters)
- Grade distribution chart
- Download transcript (PDF, cached locally after download)

#### 5. Timetable
- Week view (Monday-Friday)
- Tap to expand course details (faculty, venue, course description)
- Add to phone calendar
- Venue map integration (if available)
- Notification: 15 mins before class

#### 6. Fee Management
- Fee statement (semester-wise breakdown)
- Payment history with receipts
- Online payment button (JazzCash / EasyPaisa)
- Receipt download (cached)
- Notification: "Fee due in 3 days"

#### 7. Notifications
- Inbox with all notifications
- Mark as read/delete
- Notification preferences (toggle by type)
- Search notifications

#### 8. Complaints / Grievances
- Submit new grievance (category + description)
- Track status (submitted → acknowledged → resolution → closed)
- Reply to grievance messages
- Auto-sync when online

#### 9. Profile
- Edit profile (phone, address)
- View enrolled courses
- Download certificate (bonafide, character)
- Logout

### Performance & UX
- Splash screen (500ms)
- Lazy loading (screens load data on demand)
- Pull-to-refresh on main screens
- Infinite scroll for lists
- Offline badge indicator (grey "Offline" tag when no internet)
- Background sync (every 30 mins when app is backgrounded, if connected)

---

## Task 3 — Faculty Mobile App

#### 1. Authentication
- Same as student (Microsoft 365 + biometric)

#### 2. Dashboard
- Current semester courses
- Student count per course
- Marks entry status (% completed)
- Attendance entry status (% completed)
- At-risk students count

#### 3. Attendance Marking
- Course selection (dropdown or list)
- Date picker (select single date or range)
- Student list with checkboxes (present/absent/leave)
- Bulk actions (mark all present / all absent)
- Save draft → submit when confident
- Sync to server when online

#### 4. Marks Entry
- Course selection
- Assessment component selection
- Student list with marks fields
- Numeric input (validated: 0 to total_marks)
- Save as draft → submit when locked
- Bulk upload: select CSV file from phone storage

#### 5. Course Materials
- Browse materials (organized by week/topic)
- Upload lecture notes / slides / handouts
- Delete materials
- View upload history

#### 6. Student At-Risk Alerts
- List of flagged students (attendance < 75% OR low grades)
- Tap to send message to student
- Mark as addressed

#### 7. Workload View
- Credits taught (semester + annual)
- Student count breakdown
- Workload score (for appraisal context)

#### 8. Messages
- In-app messaging with students
- Send bulk message to class
- Message history (cached locally)

#### 9. Profile
- Edit profile
- View appointment schedule
- View performance appraisal (if available)
- Logout

---

## Task 4 — Push Notifications & Deep Linking

### Firebase Cloud Messaging
```typescript
// shared-auth/pushNotifications.ts
import * as Notifications from 'expo-notifications';
import { initializeApp } from 'firebase/app';
import { getMessaging } from 'firebase/messaging/rn';

export async function setupPushNotifications(userId: string) {
  const messaging = getMessaging();
  
  // Request permission
  const permission = await Notifications.getPermissionsAsync();
  if (!permission.granted) {
    await Notifications.requestPermissionsAsync();
  }
  
  // Get token
  const token = await messaging.getToken();
  
  // Register token with backend
  await apiClient.post(`/user/${userId}/push-token`, { token });
  
  // Handle incoming notifications
  Notifications.setNotificationHandler({
    handleNotification: async (notification) => {
      const { data } = notification.request.content;
      
      // Deep link based on notification type
      if (data.type === 'fee_reminder') {
        navigation.navigate('Fee', { section: 'payment' });
      } else if (data.type === 'result_published') {
        navigation.navigate('Grades');
      }
      
      return {
        shouldShowAlert: true,
        shouldPlaySound: true,
        shouldSetBadge: true
      };
    }
  });
}
```

### Notification Types
- Fee due (3 days, 1 day, overdue)
- Attendance alert (weekly)
- Result published
- Hall ticket ready
- Grievance status update
- Message from faculty/admin
- New assignment / material posted

---

## Task 5 — Build & Distribution

### EAS Build Config
```json
{
  "build": {
    "production": {
      "ios": {
        "buildType": "archive"
      },
      "android": {
        "buildType": "apk",
        "gradleCommand": ":app:bundleRelease"
      }
    }
  },
  "submit": {
    "production": {
      "ios": {
        "ascAppId": "SSUET_ERP_IOS"
      },
      "android": {
        "googlePlayServiceAccountKeyPath": "./play-key.json"
      }
    }
  }
}
```

### Release Process
1. Increment version in app.json
2. Commit to main branch
3. Tag release (v1.0.0)
4. EAS builds iOS + Android simultaneously
5. EAS submits to App Store + Google Play
6. Wait for approval (48 hours iOS, 2-4 hours Android)
7. Release available on stores

### Beta Testing (Internal)
- EAS Build produces beta APK/IPA
- Distributed via TestFlight (iOS) + Google Play Beta (Android)
- Faculty/staff test on their devices
- Feedback collected via in-app form

---

## Task 6 — Performance Optimization

### Bundle Size
- Target: < 80MB (iOS), < 100MB (Android)
- Remove unused dependencies
- Code splitting per feature
- Lazy load navigation stacks

### Startup Time
- Splash screen: 500ms
- Cold start: < 3s
- Warm start (from background): < 1s

### Battery & Data Usage
- Reduce sync frequency during low battery
- Compress images before upload
- Batch network requests
- SQLite indexes for common queries

---

## Task 7 — Testing

### Unit Tests
- API client mocks
- Database queries (WatermelonDB)
- Sync logic
- Offline detection

### Integration Tests
- Auth flow (login + biometric + logout)
- Marks entry + save draft + submit
- Offline + online transitions
- Notification handling

### E2E Tests (Detox)
- Student: attendance view → marks download → pay fee
- Faculty: marks entry → student alerts → message send

---

## Deliverables Checklist
- [ ] Expo project setup + TypeScript + ESLint
- [ ] WatermelonDB schema + sync engine
- [ ] Shared API client + authentication
- [ ] Student app (9 screens)
- [ ] Faculty app (9 screens)
- [ ] Push notification setup + deep linking
- [ ] Biometric authentication (Face ID / Fingerprint)
- [ ] Offline-first data sync (SQLite)
- [ ] Dark mode support
- [ ] Internationalization (English + Urdu)
- [ ] EAS build + App Store / Google Play submission
- [ ] Beta testing pipeline
- [ ] Performance monitoring (Sentry integration)
- [ ] Unit tests (Jest)
- [ ] Integration tests (Detox)
- [ ] App Store (iOS) & Google Play (Android) release

---

## Post-Launch Maintenance
- Monitor crash reports (Sentry)
- Track app usage analytics
- Gather user feedback (in-app surveys)
- Monthly updates (bug fixes + features)
- iOS / Android OS compatibility updates
