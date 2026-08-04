plugins {
    id("com.android.application")
    id("org.jetbrains.kotlin.android")
    id("org.jetbrains.kotlin.plugin.compose")
    id("com.google.devtools.ksp")
}

android {
    namespace = "pk.edu.aegiscore.mobile"
    compileSdk = 36

    defaultConfig {
        applicationId = "pk.edu.aegiscore.mobile"
        minSdk = 26
        targetSdk = 36
        versionCode = 1
        versionName = "1.0.0"
        buildConfigField("String", "AEGISCORE_API_BASE_URL", "\"${System.getenv("AEGISCORE_API_BASE_URL") ?: "https://api.aegiscore.local"}\"")
    }

    signingConfigs {
        create("release") {
            storeFile = file(System.getenv("AEGISCORE_KEYSTORE_PATH") ?: "${rootProject.projectDir}/keystore/release.jks")
            storePassword = System.getenv("AEGISCORE_KEYSTORE_PASSWORD") ?: ""
            keyAlias = System.getenv("AEGISCORE_KEY_ALIAS") ?: "aegiscore"
            keyPassword = System.getenv("AEGISCORE_KEY_PASSWORD") ?: ""

            if (storePassword.isBlank()) {
                // Fall back to debug keystore for local builds — CI must provide env vars
                storeFile = file(System.getenv("HOME") + "/.android/debug.keystore")
                storePassword = "android"
                keyAlias = "androiddebugkey"
                keyPassword = "android"
            }
        }
    }

    buildTypes {
        release {
            isMinifyEnabled = true
            isShrinkResources = true
            proguardFiles(getDefaultProguardFile("proguard-android-optimize.txt"), "proguard-rules.pro")
            signingConfig = signingConfigs.getByName("release")
        }
        debug {
            applicationIdSuffix = ".debug"
            versionNameSuffix = "-debug"
        }
    }

    buildFeatures {
        buildConfig = true
        compose = true
    }

    bundle {
        language { enableSplit = false }
        density { enableSplit = true }
    }
}

dependencies {
    implementation("androidx.activity:activity-compose:1.9.3")
    implementation("androidx.biometric:biometric:1.1.0")
    implementation("androidx.compose.material3:material3:1.3.1")
    implementation("androidx.compose.ui:ui:1.7.5")
    implementation("androidx.lifecycle:lifecycle-viewmodel-compose:2.8.7")
    implementation("androidx.room:room-ktx:2.6.1")
    implementation("com.squareup.retrofit2:retrofit:2.11.0")
    implementation("com.squareup.retrofit2:converter-moshi:2.11.0")
    implementation("com.google.firebase:firebase-messaging-ktx:24.1.0")
    ksp("androidx.room:room-compiler:2.6.1")
}
