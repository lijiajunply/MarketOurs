buildscript {
    val localProperties = java.util.Properties()
    val localPropertiesFile = file("local.properties")
    if (localPropertiesFile.exists()) {
        localPropertiesFile.inputStream().use { localProperties.load(it) }
    }

    val enableFcmProperty = findProperty("ENABLE_FCM") as String?
    val enableFcmLocal = localProperties.getProperty("ENABLE_FCM")
    val enableFcmEnv = System.getenv("ENABLE_FCM")
    val enableHuaweiProperty = findProperty("ENABLE_HUAWEI") as String?
    val enableHuaweiLocal = localProperties.getProperty("ENABLE_HUAWEI")
    val enableHuaweiEnv = System.getenv("ENABLE_HUAWEI")
    val enableFcm =
        when {
            !enableFcmProperty.isNullOrBlank() -> enableFcmProperty.equals("true", ignoreCase = true)
            !enableFcmLocal.isNullOrBlank() -> enableFcmLocal.equals("true", ignoreCase = true)
            !enableFcmEnv.isNullOrBlank() -> enableFcmEnv.equals("true", ignoreCase = true)
            else -> file("app/google-services.json").exists()
        }
    val enableHuawei =
        when {
            !enableHuaweiProperty.isNullOrBlank() -> enableHuaweiProperty.equals("true", ignoreCase = true)
            !enableHuaweiLocal.isNullOrBlank() -> enableHuaweiLocal.equals("true", ignoreCase = true)
            !enableHuaweiEnv.isNullOrBlank() -> enableHuaweiEnv.equals("true", ignoreCase = true)
            else -> file("app/agconnect-services.json").exists()
        }

    repositories {
        google()
        mavenCentral()
        maven(url = "https://developer.huawei.com/repo/")
    }
    if (enableFcm) {
        dependencies {
            classpath("com.google.gms:google-services:4.4.2")
        }
    }
    if (enableHuawei) {
        dependencies {
            classpath("com.huawei.agconnect:agcp:1.9.1.301")
        }
    }
}

allprojects {
    repositories {
        google()
        mavenCentral()
        maven(url = "https://developer.huawei.com/repo/")
    }
}

val newBuildDir: Directory =
    rootProject.layout.buildDirectory
        .dir("../../build")
        .get()
rootProject.layout.buildDirectory.value(newBuildDir)

subprojects {
    val newSubprojectBuildDir: Directory = newBuildDir.dir(project.name)
    project.layout.buildDirectory.value(newSubprojectBuildDir)
}
subprojects {
    project.evaluationDependsOn(":app")
}

tasks.register<Delete>("clean") {
    delete(rootProject.layout.buildDirectory)
}
