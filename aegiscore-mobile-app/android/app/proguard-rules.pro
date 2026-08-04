# AegisCore Android ProGuard Rules
# Production release minification and obfuscation

# Retrofit
-keepattributes Signature
-keepattributes *Annotation*
-keep class pk.edu.aegiscore.mobile.** { *; }
-keepclassmembers class pk.edu.aegiscore.mobile.** { *; }
-dontwarn retrofit2.**
-keep class retrofit2.** { *; }

# Moshi
-keep class com.squareup.moshi.** { *; }
-keepclasseswithmembers class * {
    @com.squareup.moshi.* <methods>;
}

# Room
-keep class * extends androidx.room.RoomDatabase
-keep @androidx.room.Entity class *
-dontwarn androidx.room.paging.**

# Firebase
-keep class com.google.firebase.** { *; }
-dontwarn com.google.firebase.**

# Biometric
-keep class androidx.biometric.** { *; }

# Compose
-dontwarn androidx.compose.**
