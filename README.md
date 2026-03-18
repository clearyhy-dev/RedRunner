# Meow Runner Android

Meow Runner Android 是一个基于 Red Runner 改造的 Unity 6 安卓跑酷项目，当前版本重点面向 Android 真机体验，已经补齐手机端 UI 适配、触摸输入、Google Play Games 登录，以及一套可复用的 Android 配置和构建工具链。

## 当前特性

- 面向安卓手机的首页、游戏内、暂停和结算界面适配
- Safe Area 适配与 `CanvasScaler` 统一策略
- 真机触摸输入支持：开始游戏、跳跃、右半屏长按加速
- Google Play Games 登录集成与运行时状态反馈
- Android 构建与场景修复编辑器工具
- 猫咪角色、美术资源和收集物替换

## 技术栈

- Unity `6000.2.6f2`
- C#
- Google Play Games Plugin for Unity
- External Dependency Manager for Unity
- Android Gradle / Manifest / Player Settings 配置

## 项目结构说明

- `Assets/Scripts/Services`
  - Google 登录、配置读取和平台服务相关逻辑
- `Assets/Scripts/RedRunner`
  - 核心玩法、角色、UI、场景逻辑
- `Assets/Scripts/Gameplay`
  - 手机输入与移动端玩法适配
- `Assets/Editor`
  - Android 构建、场景修复、登录配置等编辑器工具
- `Assets/Resources/Configs`
  - 安卓平台服务配置文件

## 已完成的安卓改造

- Google 登录单例管理器，支持 `Login()` / `Logout()` / `IsLoggedIn()` / `GetUserName()`
- 首页 Google 登录入口与登录状态文字回显
- 真机点击开始、点击跳跃、长按加速输入链路收敛
- 运行时移动端 UI 基线修正
- Android 平台构建与配置辅助脚本

## 环境要求

- Unity 6：`6000.2.6f2`
- Android Build Support
- 已配置 Android SDK / JDK / NDK
- Google Play Console 中已完成对应应用与 OAuth / Play Games 配置

## 快速开始

1. 使用 Unity `6000.2.6f2` 打开项目。
2. 检查 `ProjectSettings` 中的 Android 包名、签名和构建配置。
3. 检查 `Assets/Resources/Configs/AndroidPlatformServicesConfig.json`。
4. 如需重新应用安卓配置，可使用 `Assets/Editor` 下的工具脚本。
5. 连接安卓真机后执行打包与安装验证。

## 相关文档

- `MEOW_RUNNER_ANDROID_BUILD.md`
- `ANDROID_UI_VERIFICATION.md`
- `deep-research-report.md`

## 说明

本仓库当前已经不只是原始的 Red Runner 示例项目，而是一个面向 Android 发布和真机游玩的定制版本。  
如果你希望继续扩展排行榜、广告、账号体系或商店功能，可以在当前 Google 登录和安卓构建基础上继续迭代。

## License

MIT
