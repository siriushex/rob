plugins {
    id("com.android.application")
    kotlin("android")
}

android {
    namespace = "com.example.vpn"
    compileSdk = 33

    defaultConfig {
        applicationId = "com.example.vpn"
        minSdk = 21
        targetSdk = 33
        versionCode = 1
        versionName = "1.0"
    }

    buildFeatures {
        viewBinding = true
    }
}

dependencies {
    implementation("androidx.core:core-ktx:1.9.0")
    implementation("androidx.appcompat:appcompat:1.6.1")
    implementation("com.google.android.material:material:1.9.0")
    implementation("com.github.waterfalltrust:tun2socks:0.4.1")
    implementation("com.github.shadowsocks:core:4.4.0")
}
