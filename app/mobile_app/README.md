## 部署

1. Windows (msix):

   ```bash
   dart run msix:create --store
   ```

2. Android (apk):
   ```bash
   flutter build apk --obfuscate --split-debug-info=xx --target-platform android-arm64 --split-per-abi
   ```

可以加入 `--dart-define=UPDATE_CHANNEL=appstore` 来表明使用应用商店版本

### Android 极光推送配置

系统通知已接入 Android + JPush。要让真机收到远程推送，还需要补齐以下本地配置：

1. 在极光控制台创建 Android 应用，包名填写 `com.luckyfish.lumalis`
2. 推荐在本地私有文件 `app/mobile_app/android/local.properties` 中提供：

   ```bash
   JPUSH_APPKEY=你的极光 AppKey
   JPUSH_CHANNEL=developer-default
   ```

   如果要启用厂商通道，也可以继续在同一个文件里补：

   ```bash
   # FCM
   ENABLE_FCM=true

   # Xiaomi
   ENABLE_XIAOMI=true
   XIAOMI_APPID=你的小米 AppID
   XIAOMI_APPKEY=你的小米 AppKey

   # Vivo
   ENABLE_VIVO=true
   VIVO_APPID=你的 Vivo AppID
   VIVO_APPKEY=你的 Vivo AppKey

   # Oppo / OnePlus / realme
   ENABLE_OPPO=true
   OPPO_APPID=你的 Oppo AppID
   OPPO_APPKEY=你的 Oppo AppKey
   OPPO_APPSECRET=你的 Oppo AppSecret

   # Huawei
   ENABLE_HUAWEI=true

   # Honor
   ENABLE_HONOR=true
   HONOR_APPID=你的 Honor AppID
   ```

   `local.properties` 已被 Git 忽略，不会提交到仓库。

   也支持放在 `gradle.properties` 或环境变量中：

   ```bash
   JPUSH_APPKEY=你的极光 AppKey
   JPUSH_CHANNEL=developer-default
   ```

3. 如果启用 FCM，还需要把 Firebase 控制台下载的
   `google-services.json`
   放到：

   `app/mobile_app/android/app/google-services.json`

4. 如果启用 Huawei，还需要把华为后台下载的
   `agconnect-services.json`
   放到：

   `app/mobile_app/android/app/agconnect-services.json`

5. 重新执行：

   ```bash
   flutter pub get
   flutter analyze
   flutter run
   ```

说明：
- 如果没有配置 `JPUSH_APPKEY`，应用仍可正常启动，只是会自动跳过推送初始化。
- 如果没有配置对应厂商的 AppId / AppKey，相关厂商通道会自动保持关闭，不影响其他通道。
- `OPPO_APPID`、`OPPO_APPKEY`、`OPPO_APPSECRET` 支持直接写后台原值；构建脚本会自动补 `OP-` 前缀。
- 登录后会自动注册极光 `registrationId`；登出时会自动向后端清空该 token。
- 华为 / 荣耀 / 小米 / vivo / oppo 通道都通过极光 Android 插件自动接入，无需手动添加原生 receiver/service。

#### 厂商通道备注

- Huawei：需要 `agconnect-services.json`，并且调试包/正式包签名证书指纹要和华为开发者后台配置一致。
- Honor：需要在荣耀开发者后台配置应用签名 SHA256，单有 `HONOR_APPID` 还不够。
- Xiaomi：建议在小米开放平台中确认应用包名仍然是 `com.luckyfish.lumalis`。
- Vivo：推送测试通常还需要在 vivo 开发平台完成应用审核或测试设备绑定。
- Oppo：支持范围通常覆盖 OPPO / OnePlus / realme；平台参数经常要求使用正式应用包名。

3. Android (aab):
   ```bash
   flutter build appbundle --obfuscate --split-debug-info=xx --target-platform android-arm64
   ```

4. Web (wasm):

   ```bash
   flutter build web--wasm
   ```

5. macOS

   ```bash
   flutter build macos --release
   ```

6. iOS (ipa):
   ```bash
   flutter build ipa
   ```

7. Linux
   ```bash
   flutter build linux --release
   ```
