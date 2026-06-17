import java.util.Properties
import java.io.FileInputStream
import org.jetbrains.kotlin.gradle.dsl.JvmTarget

plugins {
    id("com.android.application")
    id("kotlin-android")
    // The Flutter Gradle Plugin must be applied after the Android and Kotlin Gradle plugins.
    id("dev.flutter.flutter-gradle-plugin")
}

val keystoreProperties = Properties()
val keystorePropertiesFile = rootProject.file("key.properties")
if (keystorePropertiesFile.exists()) {
    keystoreProperties.load(FileInputStream(keystorePropertiesFile))
}

val localProperties = Properties()
val localPropertiesFile = rootProject.file("local.properties")
if (localPropertiesFile.exists()) {
    localProperties.load(FileInputStream(localPropertiesFile))
}

val googleServicesFile = project.file("google-services.json")
val agconnectServicesFile = project.file("agconnect-services.json")
val enableFcm =
    providers.gradleProperty("ENABLE_FCM")
        .orElse(localProperties.getProperty("ENABLE_FCM") ?: "")
        .orElse(providers.environmentVariable("ENABLE_FCM"))
        .map { value -> value.equals("true", ignoreCase = true) }
        .orElse(googleServicesFile.exists())
        .get()
val enableHuawei =
    providers.gradleProperty("ENABLE_HUAWEI")
        .orElse(localProperties.getProperty("ENABLE_HUAWEI") ?: "")
        .orElse(providers.environmentVariable("ENABLE_HUAWEI"))
        .map { value -> value.equals("true", ignoreCase = true) }
        .orElse(agconnectServicesFile.exists())
        .get()

fun propertyValue(key: String, defaultValue: String = "") =
    providers.gradleProperty(key)
        .orElse(localProperties.getProperty(key) ?: "")
        .orElse(providers.environmentVariable(key))
        .orElse(defaultValue)

fun enabledFlag(key: String, defaultValue: Boolean = false) =
    propertyValue(key, if (defaultValue) "true" else "false")
        .map { value -> value.equals("true", ignoreCase = true) }
        .get()

fun normalizeOppoValue(value: String): String {
    val trimmed = value.trim()
    if (trimmed.isEmpty()) return ""
    return if (trimmed.startsWith("OP-")) trimmed else "OP-$trimmed"
}

val jpushAppKey = providers.gradleProperty("JPUSH_APPKEY")
    .orElse(localProperties.getProperty("JPUSH_APPKEY") ?: "")
    .orElse(providers.environmentVariable("JPUSH_APPKEY"))
    .orElse("")
val jpushChannel = providers.gradleProperty("JPUSH_CHANNEL")
    .orElse(localProperties.getProperty("JPUSH_CHANNEL") ?: "")
    .orElse(providers.environmentVariable("JPUSH_CHANNEL"))
    .orElse("developer-default")

val xiaomiAppId = propertyValue("XIAOMI_APPID").get().trim()
val xiaomiAppKey = propertyValue("XIAOMI_APPKEY").get().trim()
val vivoAppId = propertyValue("VIVO_APPID").get().trim()
val vivoAppKey = propertyValue("VIVO_APPKEY").get().trim()
val oppoAppId = normalizeOppoValue(propertyValue("OPPO_APPID").get())
val oppoAppKey = normalizeOppoValue(propertyValue("OPPO_APPKEY").get())
val oppoAppSecret = normalizeOppoValue(propertyValue("OPPO_APPSECRET").get())
val honorAppId = propertyValue("HONOR_APPID").get().trim()

val enableXiaomi =
    enabledFlag("ENABLE_XIAOMI") ||
        (xiaomiAppId.isNotEmpty() && xiaomiAppKey.isNotEmpty())
val enableVivo =
    enabledFlag("ENABLE_VIVO") ||
        (vivoAppId.isNotEmpty() && vivoAppKey.isNotEmpty())
val enableOppo =
    enabledFlag("ENABLE_OPPO") ||
        (oppoAppId.isNotEmpty() && oppoAppKey.isNotEmpty() && oppoAppSecret.isNotEmpty())
val enableHonor =
    enabledFlag("ENABLE_HONOR") ||
        honorAppId.isNotEmpty()

if (enabledFlag("ENABLE_XIAOMI") && (xiaomiAppId.isEmpty() || xiaomiAppKey.isEmpty())) {
    logger.lifecycle("Xiaomi channel requested but XIAOMI_APPID/XIAOMI_APPKEY is incomplete.")
}

if (enabledFlag("ENABLE_VIVO") && (vivoAppId.isEmpty() || vivoAppKey.isEmpty())) {
    logger.lifecycle("Vivo channel requested but VIVO_APPID/VIVO_APPKEY is incomplete.")
}

if (enabledFlag("ENABLE_OPPO") &&
    (oppoAppId.isEmpty() || oppoAppKey.isEmpty() || oppoAppSecret.isEmpty())
) {
    logger.lifecycle("Oppo channel requested but OPPO_APPID/OPPO_APPKEY/OPPO_APPSECRET is incomplete.")
}

if (enabledFlag("ENABLE_HONOR") && honorAppId.isEmpty()) {
    logger.lifecycle("Honor channel requested but HONOR_APPID is incomplete.")
}

val androidApplicationId = "com.luckyfish.lumalis"

android {
    namespace = androidApplicationId
    compileSdk = flutter.compileSdkVersion
    ndkVersion = flutter.ndkVersion

    compileOptions {
        sourceCompatibility = JavaVersion.VERSION_17
        targetCompatibility = JavaVersion.VERSION_17
        isCoreLibraryDesugaringEnabled = true
    }
    signingConfigs {
        create("release") {
            keyAlias = keystoreProperties.getProperty("keyAlias")
            keyPassword = keystoreProperties.getProperty("keyPassword")
            storeFile = keystoreProperties.getProperty("storeFile")?.let { rootProject.file(it) }
            storePassword = keystoreProperties.getProperty("storePassword")
        }
    }

    defaultConfig {
        // TODO: Specify your own unique Application ID (https://developer.android.com/studio/build/application-id.html).
        applicationId = androidApplicationId
        // You can update the following values to match your application needs.
        // For more information, see: https://flutter.dev/to/review-gradle-config.
        minSdk = flutter.minSdkVersion
        targetSdk = flutter.targetSdkVersion
        versionCode = flutter.versionCode
        versionName = flutter.versionName
        manifestPlaceholders["JPUSH_PKGNAME"] = androidApplicationId
        manifestPlaceholders["JPUSH_APPKEY"] = jpushAppKey.get()
        manifestPlaceholders["JPUSH_CHANNEL"] = jpushChannel.get()
        manifestPlaceholders["FCM_NOTIFICATION_ICON"] = "@drawable/ic_stat_marketours_notification"
        manifestPlaceholders["XIAOMI_APPID"] = xiaomiAppId
        manifestPlaceholders["XIAOMI_APPKEY"] = xiaomiAppKey
        manifestPlaceholders["VIVO_APPID"] = vivoAppId
        manifestPlaceholders["VIVO_APPKEY"] = vivoAppKey
        manifestPlaceholders["OPPO_APPID"] = oppoAppId
        manifestPlaceholders["OPPO_APPKEY"] = oppoAppKey
        manifestPlaceholders["OPPO_APPSECRET"] = oppoAppSecret
        manifestPlaceholders["HONOR_APPID"] = honorAppId
    }

    buildTypes {
        getByName("release") {
            // TODO: Add your own signing config for the release build.
            // Signing with the debug keys for now, so `flutter run --release` works.
            signingConfig = signingConfigs.getByName("release")
        }
    }
}

if (!enableFcm) {
    logger.lifecycle(
        "FCM channel is disabled for ${project.path}. Add android/app/google-services.json or set ENABLE_FCM=true to enable it.",
    )
}

if (!enableHuawei) {
    logger.lifecycle(
        "Huawei channel is disabled for ${project.path}. Add android/app/agconnect-services.json or set ENABLE_HUAWEI=true to enable it.",
    )
}

if (!enableXiaomi) {
    logger.lifecycle(
        "Xiaomi channel is disabled for ${project.path}. Set XIAOMI_APPID/XIAOMI_APPKEY or ENABLE_XIAOMI=true to enable it.",
    )
}

if (!enableVivo) {
    logger.lifecycle(
        "Vivo channel is disabled for ${project.path}. Set VIVO_APPID/VIVO_APPKEY or ENABLE_VIVO=true to enable it.",
    )
}

if (!enableOppo) {
    logger.lifecycle(
        "Oppo channel is disabled for ${project.path}. Set OPPO_APPID/OPPO_APPKEY/OPPO_APPSECRET or ENABLE_OPPO=true to enable it.",
    )
}

if (!enableHonor) {
    logger.lifecycle(
        "Honor channel is disabled for ${project.path}. Set HONOR_APPID or ENABLE_HONOR=true to enable it.",
    )
}

kotlin {
    compilerOptions {
        jvmTarget.set(JvmTarget.JVM_17)
    }
}

flutter {
    source = "../.."
}

if (enableFcm) {
    apply(plugin = "com.google.gms.google-services")
}

if (enableHuawei) {
    apply(plugin = "com.huawei.agconnect")
}

dependencies {
    coreLibraryDesugaring("com.android.tools:desugar_jdk_libs:2.1.4")
    if (enableFcm) {
        implementation("cn.jiguang.sdk.plugin:fcm:4.8.6")
        implementation("com.google.firebase:firebase-messaging:24.1.2")
    }
    if (enableHuawei) {
        implementation("cn.jiguang.sdk.plugin:huawei:6.0.1")
    }
    if (enableXiaomi) {
        implementation("cn.jiguang.sdk.plugin:xiaomi:6.0.1")
    }
    if (enableVivo) {
        implementation("cn.jiguang.sdk.plugin:vivo:6.0.1")
    }
    if (enableOppo) {
        implementation("cn.jiguang.sdk.plugin:oppo:6.0.1")
    }
    if (enableHonor) {
        implementation("cn.jiguang.sdk.plugin:honor:6.0.1")
    }
}
