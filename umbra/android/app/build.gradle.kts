plugins {
    id("com.android.application")
    id("org.jetbrains.kotlin.android")
}

android {
    namespace = "com.umbra.client"
    compileSdk = 34

    defaultConfig {
        applicationId = "com.umbra.client"
        minSdk = 24
        targetSdk = 34
        versionCode = 1
        versionName = (project.findProperty("versionName") as String?) ?: "1.0.0"
    }

    buildTypes {
        release {
            isMinifyEnabled = false
            // Ayri keystore gerektirmemesi icin debug imzasiyla imzala.
            signingConfig = signingConfigs.getByName("debug")
        }
    }

    compileOptions {
        sourceCompatibility = JavaVersion.VERSION_17
        targetCompatibility = JavaVersion.VERSION_17
    }

    kotlinOptions {
        jvmTarget = "17"
    }
}

dependencies {
    implementation("androidx.core:core-ktx:1.13.1")
    implementation("androidx.appcompat:appcompat:1.7.0")
}
