# Meow Runner – Android 打包与安装到手机

## 一、打包并安装到手机的步骤概览

1. **确认 Unity 已安装 Android 模块**（见下方「Unity 版本与依赖」）。
2. **在 Unity 里配置一次**：Player Settings（包名、版本、Target API 35、IL2CPP、ARM64、Keystore）、Build Settings（选 Android 并加入场景）。
3. **打 APK**：File > Build Settings > Build（或 Build And Run），生成 `.apk` 文件。
4. **装到手机**：用数据线连手机，用 `adb install xxx.apk`，或在手机里直接打开 APK 安装。

下面按顺序说明。

---

## 二、第一次打包前在 Unity 里的配置

### 1. 在 Unity Hub 里勾选并安装 Android 模块

你当前是 **Unity 6.3 LTS (6000.3.11f1)**，只装了 Windows，按下面做即可加上 Android：

1. 在 **Unity Hub** 左侧点 **Installs（安装）**。
2. 在列表里找到 **Unity 6.3 LTS (6000.3.11f1)**，右侧点 **Manage（三个点或齿轮）** 的下拉箭头。
3. 选 **Add modules**（或「添加模块」）。
4. 在弹窗里勾选：
   - **Android Build Support**
   - 勾选后下面会展开子项，把 **Android SDK & NDK Tools**、**OpenJDK** 一并勾上（有的版本是合在一起的一个选项，就勾 Android Build Support 即可）。
5. 点 **Install** / **Done**，等下载安装完成。装好后该版本会显示 **Windows** 和 **Android** 支持。

**说明**：若你**本机已有** Android SDK、NDK、JDK，可以只通过 Hub 装 **Android Build Support**（不装 SDK/NDK/JDK 也行），再在 Unity 编辑器里指定本机路径，见下方「使用本机已有的 Android SDK/NDK/JDK」。

### 2. 打开项目并设置 Player Settings

- **Edit > Project Settings > Player**，左侧选 **Android**（小机器人图标）。
- **Other Settings**：
  - **Package Name**：填唯一包名。**本项目推荐**：`com.yourname.meowrunner`（把 `yourname` 换成你的英文名或品牌，全小写；只能含英文、数字、点）。若已有公司/品牌，可用 `com.公司英文名.meowrunner`。
  - **Version**：如 `1.0.0`。
  - **Bundle Version Code**：如 `1`（每次上架或更新要递增）。
  - **Scripting Backend**：选 **IL2CPP**。
  - **Target Architectures**：勾选 **ARM64**（必勾）。
  - **Minimum API Level** / **Target API Level**：在 **Other Settings** 内，有时会收在 **Configuration** 或 **Android Application Configuration** 子分组里；在 Unity 6.3 中名称一般为 **Minimum API Level**（或 **Min API Level**）和 **Target API Level**。请在该页**向下滚动**或展开所有折叠项查找；Minimum 建议选 **24** 或 **26**，Target 选 **35**（Google Play 要求）。
- **Publishing Settings**：
  - 勾选 **Create Android Keystore**，或使用已有 Keystore。
  - 若创建新的：设置 Keystore 密码、Alias、Alias 密码，并**妥善保存**，以后上架/更新都要用同一 Keystore。

**下面逐项说明怎么在界面里设：**

- **怎么打开**：Edit > Project Settings > Player，中间 Platform 点 **Android**（小机器人）。
- **Other Settings** 点开后：
  - **Package Name**：文本框填 `com.yourname.meowrunner`（yourname 换成你的英文/品牌，全小写，只能英文/数字/点）。
  - **Version**：填 `1.0.0`。
  - **Bundle Version Code**：填数字 `1`（以后更新改成 2、3、4…）。
  - **Scripting Backend**：下拉选 **IL2CPP**。
  - **Target Architectures**：只勾 **ARM64**。
  - **Minimum API Level**（若界面写 **Min API Level** 即同一项）：下拉选 **24** 或 **26**。
  - **Target API Level**：下拉选 **35**。若在 Other Settings 里看不到这两项，请展开 **Configuration** / **Android Application Configuration** 或在该页向下滚动查找。
- **Publishing Settings** 点开后：
  - **新建 Keystore**：勾选 Create a new keystore → **Keystore** 点 Browse 选保存位置（如 `D:\Builds\meowrunner.keystore`）→ **Password** 和 **Confirm password** 设同一密码并记住 → **Alias** 填 `meowrunner` → **Alias Password** 设好并记住。keystore 文件和密码务必备份。
  - **已有 Keystore**：勾选 Use custom keystore → Browse 选你的 `.keystore` 或 `.jks` → 填对应 Password、Alias、Alias Password。

### 3. 把场景加入 Build

- **File > Build Settings**。
- **Platform** 选 **Android**，点 **Switch Platform**（首次会等一会）。
- 在 **Scenes In Build** 里把要进的场景勾选并排好顺序。若你当前是「只玩 Play」的流程，至少把 **Play** 场景拖进去并勾选（顺序 0 表示启动时第一个加载）。
- 若已按 Meow Runner 多场景方案做了：可运行菜单 **Meow Runner > Setup Scenes for Build** 自动加入 Boot、Home、Play、Result、Shop、Settings。

### 4. 生成 APK 并（可选）直接装到手机

- **File > Build Settings**：
  - 下方勾选 **Build** 只打包；或 **Build And Run** 在连好手机时打包并自动安装。
- 选一个保存位置（如桌面或项目外的 `Builds` 文件夹），文件名如 `MeowRunner.apk`，点保存。
- 等待构建完成。成功后会在该路径得到 **`.apk`** 文件。

---

## 三、把 APK 安装到手机

### 方式 A：Build And Run（推荐，一条龙）

1. 手机用 **USB 数据线** 连到电脑。
2. 手机里打开 **开发者选项**，开启 **USB 调试**。
3. 电脑上若首次连接，手机会弹出「允许 USB 调试」点允许。
4. 在 Unity 里 **File > Build Settings** 点 **Build And Run**，选保存路径后构建；构建结束会自动安装到手机并打开。

### 方式 B：先打 APK，再手动安装

1. **File > Build Settings > Build**，生成 `MeowRunner.apk`（或你起的名字）。
2. **用数据线安装**：
   - 确保手机已开启 **USB 调试**并连上电脑。
   - 在电脑上打开命令行（PowerShell 或 CMD），执行：
     ```bash
     adb install -r "你的APK完整路径.apk"
     ```
   - 例如：`adb install -r "D:\Builds\MeowRunner.apk"`。`-r` 表示若已安装则覆盖。
3. **不用数据线**：把 `MeowRunner.apk` 复制到手机（微信/QQ/网盘/读卡器均可），在手机里用「文件管理」找到该 APK，点击安装。若提示「禁止安装未知应用」，到系统设置里为该来源（如「文件管理」）允许安装应用。

---

## 四、常见问题

- **找不到 adb**：先安装 [Android SDK Platform-Tools](https://developer.android.com/studio/releases/platform-tools)，把安装目录下的 `adb.exe` 所在文件夹加入系统 PATH；或使用 Unity 自带的 JDK/Android 目录下与 adb 同路径的工具。
- **Build 报错缺少 SDK/NDK/JDK**：回到 Unity Hub 为该 Unity 版本 **Add Modules**，勾选 Android Build Support 及 SDK、NDK、OpenJDK 后重新安装。
- **手机提示「应用未安装」或签名冲突**：若之前装过同包名但不同签名的包，先卸载旧版再装；或改用新的 Package Name 再打包。
- **要上架 Google Play**：用 **Build App Bundle (Google Play)** 生成 **.aab**，并在 Play Console 上传该 AAB；同时 Target API 必须为 **35**，见下方「Player Settings」说明。

### Gradle 构建失败（Build failed with an exception）

若 Console 出现 **CommandInvokationFailure: Gradle build failed** 或 **FAILURE: Build failed with an exception**，按下面顺序排查：

1. **看具体错误**：Unity 只显示「Gradle build failed」，真正原因在 Gradle 输出里。  
   - 打开 **Editor 日志**：Windows 下 `%LOCALAPPDATA%\Unity\Editor\Editor.log`（或菜单 Help > Open Editor Log），在文件中搜索 `FAILURE`、`error`、`Error`，最后几处往往就是原因。  
   - 或再次构建，在 Console 里点该错误条，有时会展开更多行。

2. **先试关闭 Minify（推荐）**：Proguard 经常导致 Gradle 报错。  
   - **Edit > Project Settings > Player > Android**，展开 **Publishing Settings**。  
   - 找到 **Minify** / **Release**（或 **Minify > Release**），改为 **None**（不用 Proguard），保存后重新 Build。

3. **检查重复依赖**：若项目里有 **Assets/Plugins/Android** 或 **Assets/GooglePlayPlugins**，看是否有多份同名的 `.aar`（例如不同版本的 support-v4、广告 SDK）。保留一份，删掉重复的，或使用 Play Services Resolver 统一版本。

4. **JDK/SDK 路径**：确认 **Edit > Preferences > External Tools** 里 Android SDK、NDK、JDK 指向有效目录（若用本机 Android Studio，路径多为 `C:\Users\你的用户名\AppData\Local\Android\Sdk` 等）。

5. **重试与清理**：关闭 Unity，删掉项目下的 **Library/Bee** 文件夹（或整个 **Library** 再重新打开项目让 Unity 重建），然后重新打 Android 包。

按上述仍失败时，把 Editor.log 里 Gradle 相关的那几段错误贴出来，便于精确定位。

---

## 五、使用本机已有的 Android SDK / NDK / JDK

若你电脑上已经装过 **Android Studio** 或单独安装的 **SDK / NDK / JDK**，可以让 Unity 直接用这些，不必在 Hub 里再下一份。

### 方法一：在 Unity 编辑器里手动指定（推荐）

1. 用 Unity Hub 打开 **RedRunner** 项目。
2. 菜单 **Edit > Preferences**（Windows）或 **Unity > Settings**（新版本）。
3. 左侧选 **External Tools**。
4. 在 **Android** 区域填写：
   - **Android SDK tools**：本机 SDK 根目录，例如  
     `C:\Users\你的用户名\AppData\Local\Android\Sdk`（Android Studio 默认）  
     或 `D:\Android\sdk`。
   - **Android NDK**：本机 NDK 目录，例如  
     `C:\Users\你的用户名\AppData\Local\Android\Sdk\ndk\25.x.x`  
     或 `D:\Android\ndk\25.2.12418`（需与 Unity 兼容的版本，一般用 SDK 里自带的即可）。
   - **JDK**：本机 JDK 目录，例如  
     `C:\Program Files\Unity\Hub\Editor\6000.3.11f1\Editor\Data\PlaybackEngines\AndroidPlayer\OpenJDK`  
     或你单独安装的 JDK 路径（如 `C:\Program Files\Java\jdk-17`）。
5. 关闭 Preferences，再试一次 **File > Build Settings > Android > Build**。

### 方法二：用项目里的一键配置脚本（可选）

项目里已提供一个 Editor 菜单，可**一次性写入**本机路径，省去在 Preferences 里逐项填写：

1. 打开 **RedRunner** 项目后，在编辑器里打开 **`Assets/Editor/SetAndroidExternalTools.cs`**，把文件顶部的 `SDK_PATH`、`NDK_PATH`、`JDK_PATH` 改成你本机的实际路径并保存。
2. 在 Unity 菜单点 **Meow Runner > Set Android SDK/NDK/JDK From Script**，执行一次。
3. 再打开 **Edit > Preferences > External Tools** 确认 Android 三项是否已填好。也可直接点 **Meow Runner > Open External Tools (Android 路径页)** 打开该页。

**注意**：Unity 不同版本用的 External Tools 的 key 可能略有差异，若脚本执行后 Build 仍报错，请用**方法一**在 Preferences 里再核对、补全路径。

---

## 六、Unity 版本与依赖（上架参考）

- **Unity**: 6000.2.6f2 (Unity 6)
- **Android 模块**: 安装 Android Build Support、SDK、NDK、JDK/OpenJDK（Unity Hub 或安装程序中勾选）

## Player Settings（Edit > Project Settings > Player）

### 通用

- **Company Name**: 你的公司/个人名
- **Product Name**: Meow Runner
- **Version**: 1.0.0
- **Bundle Version Code** (Android): 1

### Android 分页

- **Package Name**: 本项目推荐 `com.yourname.meowrunner`（yourname 换成你的英文/品牌，全小写）
- **Minimum API Level**: 在 Player > Android > **Other Settings** 里（或其中的 **Configuration** / **Android Application Configuration**），选 **24** 或 **26**
- **Target API Level**: 同上位置，选 **35**（Google Play 自 2025-08-31 起要求新应用/更新至少 API 35）
- **Scripting Backend**: **IL2CPP**
- **Target Architectures**: 勾选 **ARM64**

### 签名

- 创建并配置 **Keystore**，正式发布使用固定 Keystore，不要每次更换。

## Build Settings

1. **File > Build Settings**
2. **Platform** 选择 **Android**，点击 **Switch Platform**
3. 场景顺序应为：**Boot**, **Home**, **Play** (Game), **Result**, **Shop**, **Settings**
4. 若尚未包含上述场景：菜单 **Meow Runner > Setup Scenes for Build** 自动创建并加入。**手机打开蓝屏/黑屏**多为 Boot 后无法加载 Home：请先执行此菜单，确保 Boot、Home、Play、Result 等场景都在 Build Settings 里。
5. 首次构建前在 Boot 场景添加启动逻辑：**Meow Runner > Setup Boot Scene (add GameBootstrap)**

## Meow Runner 体验：应用名、手机操作、收集物小鱼

- **应用名显示为「Meow Runner」**：菜单 **Meow Runner > Set Player Product Name to Meow Runner**，保存后重新打 APK。手机安装后桌面/任务栏会显示 Meow Runner。
- **手机操作（自动跑 + 点屏跳跃）**：菜单 **Meow Runner > Setup Play Scene for Mobile (Auto-Run + Tap Jump + Screen Adapter)** 一次性为 Play 场景添加：小猫自动跑、点屏跳跃、**画面大小自适应**（不同分辨率手机视觉一致）。保存 Play 场景后重新打 APK。游戏中小猫会**一直自动向右跑**，**点击屏幕即可跳跃**，无需虚拟按键。
- **收集物显示为小鱼**（二选一）：
  - **方式 A（推荐）**：小鱼图已在 **Assets/Art/Collectibles** 或 **Assets/Art/Characters** 时，执行 **Meow Runner > Copy Fish from Art to Resources (for runtime)**，再执行 **Meow Runner > Setup Play Scene for Mobile**（会挂上运行时替换脚本）。打 APK 后收集物和 HUD 会显示为小鱼。
  - **方式 B**：打开 **Play** 场景，执行 **Meow Runner > Apply Art from Collectibles Folder (Open Play First)**，保存场景后打 APK。若金币仍未变小鱼，检查 Coin 的 SpriteRenderer 是否在子物体上（已支持子物体），或使用方式 A 把小鱼复制到 Resources。

## 场景与入口

- **Boot**: 首个加载场景，挂载 `GameBootstrap`，负责读档、初始化广告/隐私同意后跳转 Home
- **Home**: 挂载 `UIHomeController`，按钮跳转 Play / Shop / Settings，隐私政策链接
- **Play**: 原 Red Runner 玩法场景（原 Play.unity）
- **Result**: 挂载 `UIResultController`，显示得分、激励复活、再来一局、返回首页
- **Shop**: 挂载 `UIShopController`，简单皮肤列表
- **Settings**: 挂载 `UISettingsController`，音乐/音效、隐私与用户协议链接、版本号

## 死亡与复活、会员、广告、Google 登录

- **小猫死亡后蓝屏**：已修复。若 Result 场景未加入 Build 或当前场景没有 END_SCREEN，会自动退回 **Home** 场景，不再卡蓝屏。
- **复活**：死亡后结算/结束界面提供「复活」：
  - **看广告复活**：调用 `AdsManager.ShowRewarded`（当前为桩实现，接入 Google Mobile Ads 后替换）。
  - **会员复活**：若已开通会员（`SaveData.IsMember == true`），可直接复活，无需看广告。
- **会员**：存档字段 `SaveData.IsMember`，可通过后续 IAP 或调试设置。调试时可用 `SaveManager.SaveIsMember(true)` 开通会员。
- **Google 登录**：启动时调用 `GoogleAuthManager.Initialize()`，当前为占位（可调用 `GoogleAuthManager.Login()` 做游客/测试）。正式版需接入 **Google Play Games Services** 或 **Firebase Auth** 后替换 `Services.Auth.GoogleAuthManager` 实现。

## 输出格式

- Google Play 上架使用 **AAB**（Android App Bundle）。Build Settings 中勾选 **Build App Bundle (Google Play)** 或导出时选 AAB。

## 参考

- [Unity Android 构建](https://docs.unity3d.com/Manual/android-BuildSupport.html)
- [Google Play 目标 API 要求](https://developer.android.com/google/play/requirements/target-sdk)
- [AdMob Unity 插件](https://developers.google.com/admob/unity/quick-start)
