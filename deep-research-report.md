# 将 RedRunner 改造为可在 iOS/Android 发布的完整方案研究报告

## 执行摘要

本报告基于开源仓库的公开内容、项目代码结构与关键脚本实现（尤其是输入与 UI 管理、地形生成与对象池等模块），并对移动端发布所需的登录、排行榜/成就、广告变现、隐私合规、测试与上架流程进行系统化设计，给出“可落地”的改造路径、文件级改动清单与阶段性工时/人力估算。项目本身采用 MIT 许可证，允许商业化与闭源发布，但需保留版权与许可声明；同时项目使用的美术资源来自带署名要求的 CC BY 4.0 资源包，必须在应用内或上架材料中提供恰当署名。citeturn6view0turn31view0

在技术层面，项目已部分采用 Unity Standard Assets 的 CrossPlatformInput（可在移动端通过虚拟摇杆/按钮映射 “Horizontal”“Jump”等轴/键），但仍存在明显“桌面假设”（例如鼠标光标显示/隐藏逻辑、基于鼠标点击的状态切换等）需要移除或条件编译；此外，核心地形生成逻辑大量 Instantiate/Destroy，移动端容易引发卡顿与 GC 抖动，建议引入地形块/背景块对象池并结合资源压缩与质量分级策略，作为性能改造的优先项。citeturn15view1turn13view1turn16view3

平台集成方面：  
- **Android** 推荐使用最新版 Google Play Games 插件（v11.01 及以上走 Play Games Services v2 路线）完成登录与排行榜/成就；需要特别注意 Google Play Games v1 相关 API 在 2026 年 5 月开始从 SDK 移除的时间风险，因此不建议继续基于旧版插件路线。citeturn42search6turn42search15turn42search7  
- **iOS** 采用 Game Center（GameKit）完成排行榜/成就；若在 iOS 也提供 Google 登录，则会触发 App Store Review Guidelines 对“第三方登录服务”需要提供等效登录选项（实践中通常是 Sign in with Apple）的审核要求，需在产品策略上提前决策并落地。citeturn42search0turn36view1  
- **广告变现** 推荐接入 Google Mobile Ads Unity Plugin v11.0.0（并同步集成 UMP 以满足 GDPR/隐私偏好管理），同时在 iOS 按需接入 AppTrackingTransparency，Android 处理 Advertising ID 与目标 API 要求。citeturn38view0turn33search1turn33search19turn33search2turn33search11

---

## 项目现状与许可合规

### 仓库结构与主要引擎/框架

该仓库是标准 Unity 工程结构，根目录包含 `Assets/`、`Packages/`、`ProjectSettings/` 等目录与 `LICENSE` 文件。citeturn2view0  
工程依赖 Unity 6（Unity Editor 6000.2.6f2）作为推荐版本；Unity 官方发布页亦记录了该版本信息。citeturn43search1turn43search3

从 `Packages/manifest.json` 可见项目使用 Unity 2D/UGUI、2D Tilemap、Post Processing 等官方包（例如 `com.unity.2d.sprite`、`com.unity.2d.tilemap`、`com.unity.ugui`、`com.unity.postprocessing`）。citeturn6view1  
脚本语言为 C#，主要游戏逻辑位于 `Assets/Scripts/RedRunner/` 及其子目录（Characters/UI/TerrainGeneration 等）。citeturn10view0turn12view0turn12view1

### 第三方库与版权边界梳理

项目中明确可识别的第三方来源主要包括：

- **项目主许可证：MIT**。MIT 允许商业使用、修改、分发与再许可（含闭源发布），核心义务是“在软件副本或重要部分保留版权声明与许可声明”。citeturn6view0  
- **美术资源包：CC BY 4.0（署名）**。项目关联的 “Free Platform Game Assets + GUI” 在其发布页标注 Asset license 为 Creative Commons Attribution v4.0 International，意味着发布作品时需提供适当署名。citeturn31view0  
- **SaveGameFree（存档组件）**：在游戏 `GameManager` 中直接引用 `BayatGames.SaveGameFree` 并设置二进制序列化器。citeturn13view0 该组件公开仓库许可证为 MIT。citeturn32view0  
- **FullSerializer**：在 `Assets/SaveGameFree/Plugins/` 出现 `FullSerializer.dll`。citeturn23view0 FullSerializer 上游许可证为 MIT。citeturn32view1  
- **Unity Standard Assets / CrossPlatformInput**：角色脚本 `RedCharacter` 使用 `UnityStandardAssets.CrossPlatformInput`。citeturn15view0turn15view1 但在该仓库内未见清晰的第三方许可证文件（未指定）。这不等于不能用于商业发布，但在做“开源合规归档/第三方声明”时应追溯其原始分发来源与条款，并在发布包中以 Third-Party Notices 方式披露（做法参考 Unity 的“Meeting legal requirements”文档思路）。citeturn30search23turn30search42  

**合规结论（围绕“是否允许闭源发布、需保留声明”）：**  
- 代码层面：MIT 允许闭源商业发布，但必须保留 MIT 版权与许可文本（建议在应用内“关于/开源许可”页与随包文档同时提供）。citeturn6view0  
- 美术资源：CC BY 4.0 要求署名（建议：应用内“致谢/资源来源”+ 商店描述页“Credits”+ 官网隐私页或支持页长期可访问）。citeturn31view0  
- 第三方库：SaveGameFree 与 FullSerializer 为 MIT，按 MIT 方式保留声明即可；Standard Assets/CrossPlatformInput 在本仓库中未明确条款，需单独核验并在第三方声明中标为“来源与条款未指定（需追溯）”。citeturn23view0turn32view0turn32view1  

---

## 技术改造方案

### 改造目标与分层策略

移动端改造建议以“**输入与 UI 适配优先，其次性能与资源，再做横竖屏与设备分级**”为主线。原因是：当前项目虽然已经引入 CrossPlatformInput，但 UI 管理仍存在鼠标/光标逻辑，且地形生成存在显著的 Instantiate/Destroy 热点；这两类问题会直接导致移动端体验不可用或卡顿。citeturn13view1turn15view1turn16view3

适配策略建议按三层落地：

- **表现层（UI/分辨率/安全区）**：先保证各种屏幕比例下 UI 不被裁切、可点击、可读。Unity 的 `CanvasScaler`（Scale With Screen Size、Match Width Or Height）可作为主方案。citeturn1search2  
- **交互层（触控替代键鼠）**：以“虚拟摇杆/按钮（CrossPlatformInput 或新 Input System）+ 手势”混合实现，做到单手可玩。citeturn15view1turn26view0  
- **性能层（帧率/内存/电池）**：优先解决生成销毁与资源体量；移动端对频繁 GC 与过大纹理非常敏感。地形块对象池应列为 P0。citeturn16view3turn17view0  

### 屏幕适配要点

- **分辨率与纵横比**：建议以“参考分辨率 + 锚点布局 + 动态安全区”组合实现，而不是写死像素。`CanvasScaler` 的 `matchWidthOrHeight` 可按“横屏偏高度、竖屏偏宽度”动态切换。citeturn1search2  
- **安全区（刘海/挖孔/圆角）**：移动端必须应用 `Screen.safeArea` 对顶部 HUD、底部 Banner、侧边按钮区做裁切与内边距处理。citeturn1search3  
- **UI 缩放**：建议统一使用 UGUI 的 anchor + LayoutGroup（或自研适配脚本）确保按钮区在小屏也能保持最小触控尺寸（后文 UI/UX 给出推荐值）。citeturn1search2turn43search0  

### 触控输入替代键盘/鼠标

现状：  
- `RedCharacter` 的移动与跳跃等主要输入使用 `CrossPlatformInputManager.GetAxis("Horizontal")`、`CrossPlatformInputManager.GetButtonDown("Jump")`，说明项目已具备“移动端虚拟控制器映射”基础。citeturn15view1turn29view0  
- 但仍有 `Input.GetButtonDown("Roll")` 混用（非 CrossPlatformInput），以及 `UIManager` 中大量鼠标/光标逻辑（`Input.GetMouseButtonDown/Up`、Cursor 显示隐藏），这些在移动端应移除或做平台分支。citeturn15view1turn13view1  

落地建议：  
- **短平快方案（改动小）**：继续使用 CrossPlatformInput 的 Mobile Control Rig（虚拟摇杆/按钮），把 Roll/Guard/Fire 等统一迁移到 CrossPlatformInput 的 VirtualButton，关闭 UIManager 的光标逻辑。citeturn26view0turn13view1  
- **中期方案（更现代、可维护）**：迁移到 Unity 新 Input System 并以 Action Map 适配触控/手柄；但这是结构性改造，不建议与首发上架强绑定（可作为 v1.1 迭代）。未指定。  

### 性能优化重点

**P0：地形生成与销毁热点**  
`TerrainGenerator` 在生成与移除时大量使用 `Instantiate` 与 `Destroy`（例如 `CreateBlock`、`RemoveBlock`），并按固定周期（每 5 秒）清理远处对象，这在移动端容易出现卡顿与峰值 GC。建议将 `Block` 与 `BackgroundBlock` 纳入对象池（可复用现有思路，但目前 `ObjectPool` 仅覆盖 Collectable/Coins）。citeturn16view3turn17view0  

**P1：资源体积与加载**  
- 纹理压缩：Android 优先 ASTC（中高端）或 ETC2（兼容）；iOS 优先 ASTC。未指定。  
- Sprite Atlas：将 UI、角色、地形切成 Atlas 以减少 DrawCall（与手机 GPU 友好）。未指定。  
- Post Processing：项目包含 `com.unity.postprocessing`，移动端建议默认关闭或按机型分级开启（低端关闭 bloom/ao 等）。citeturn6view1  

**P2：帧率与电池**  
- 目标帧率策略：默认 60 FPS；低端机或发热时降到 30 FPS（可做动态策略）。未指定。  
- 前后台切换：移动端需在 `OnApplicationPause`/`OnApplicationFocus` 做暂停与恢复，避免后台仍运行导致耗电与异常。当前 `GameManager` 已有 `StopGame/ResumeGame` 与 `Time.timeScale` 控制，但未覆盖系统生命周期钩子，需补齐。citeturn13view0  

### 横竖屏支持与设备分级

- **横竖屏**：建议优先确定“首发只支持横屏/竖屏之一”，否则 UI 与关卡镜头都要做两套适配（成本显著上升）。未指定。  
- **设备分级**：建议至少分为 Low/Mid/High 三档：  
  - Low：关掉后处理、降低粒子、降低纹理分辨率；  
  - Mid：开启部分效果；  
  - High：开启全部效果。  
  具体阈值需基于性能测试数据决定。未指定。  

### 技术改造修改点与文件清单（含示例）

下表为“必须改/高收益改/建议改”的合并清单（路径以仓库实际目录为准）。citeturn10view0turn12view1turn41view0  

| 改造维度 | 现状证据（代码/结构） | 需修改/新增文件（示例路径） | 改造要点 | 验收与风险 |
|---|---|---|---|---|
| 触控输入统一 | 角色移动/跳跃已用 CrossPlatformInput，但 Roll 仍用 Input；UIManager 依赖鼠标/光标 | **改** `Assets/Scripts/RedRunner/Characters/RedCharacter.cs`；**改** `Assets/Scripts/RedRunner/UIManager.cs`；**配** `Assets/Standard Assets/CrossPlatformInput/Prefabs/*`；**改** `ProjectSettings/InputManager.asset`（未指定是否存在自定义轴） | 统一 Roll/Guard/Fire 到 CrossPlatformInput；移除/条件编译 Cursor 与鼠标点击逻辑；Android 返回键映射为暂停 | 触控误触与按键冲突；需做输入回归（含虚拟按钮不遮挡 HUD）citeturn15view1turn13view1turn26view0 |
| UI 缩放与适配 | 目前未见 safe area 处理；按桌面光标交互设计 | **新增** `Assets/Scripts/RedRunner/UI/SafeAreaFitter.cs`；**改** 场景 `Assets/Scenes/Play.unity`、`Assets/Scenes/Creation.unity` 中 Canvas 与锚点 | 使用 CanvasScaler + anchor；按 `Screen.safeArea` 动态加边距；横竖屏策略明确化 | 不同刘海机型遮挡；需真机覆盖 iPhone 刘海/安卓挖孔citeturn1search2turn1search3turn41view0 |
| 地形生成性能 | TerrainGenerator 频繁 Instantiate/Destroy；5 秒周期 Remove | **改** `Assets/Scripts/RedRunner/TerrainGeneration/TerrainGenerator.cs`；**新增** `Assets/Scripts/RedRunner/ObjectPool/BlockPool.cs`（示例）或扩展现有 `ObjectPool.cs` | 为 Block/BackgroundBlock 建立池；移除 Destroy 改为 SetActive(false)+回收；避免每次生成 new Dictionary key 大量分配 | 不当复用导致“残留状态”；需每类 Block 实现 Reset/OnReuse | citeturn16view3turn17view0 |
| 存档与数据 | 使用 SaveGameFree 存 coin/highScore/lastScore；退出时保存 | **改** `Assets/Scripts/RedRunner/GameManager.cs`；（可选）新增云存档接口 | 移动端更常见被系统杀死；在 `OnApplicationPause(true)` 保存关键数据；为云端接口预留（未指定） | 数据丢失投诉风险；需反复后台/杀进程测试citeturn13view0turn32view0 |
| 分享/外链 | 仍包含 GooglePlus 分享 URL（已过时服务），用 OpenURL | **改** `Assets/Scripts/RedRunner/GameManager.cs`；（可选）新增 Native Share 插件封装 | 移除失效分享入口；改用系统分享面板或仅保留通用链接分享（未指定） | 审核与用户体验；外链可触发商店规则敏感项（需评估）citeturn13view0 |

#### 示例改动（代码片段）

**示例 A：将 Roll 输入从 Input 统一到 CrossPlatformInput**（核心是减少平台分支、让移动端虚拟按钮可控）

```csharp
// 原逻辑（混用 Input.GetButtonDown）
if (Input.GetButtonDown("Roll")) {
    // roll logic...
}

// 建议：统一用 CrossPlatformInput（与 Jump/Guard 一致）
if (CrossPlatformInputManager.GetButtonDown("Roll")) {
    // roll logic...
}
```

该改动与现有输入处理块一致（Move/Jump/Guard/Fire 已走 CrossPlatformInput）。citeturn15view1  

**示例 B：安全区适配脚本（SafeAreaFitter）**（用于顶部 HUD、底部 Banner 容器）

```csharp
using UnityEngine;

[RequireComponent(typeof(RectTransform))]
public class SafeAreaFitter : MonoBehaviour
{
    RectTransform rt;
    Rect lastSafeArea;

    void Awake() {
        rt = GetComponent<RectTransform>();
        Apply();
    }

    void Update() {
        if (Screen.safeArea != lastSafeArea) Apply();
    }

    void Apply() {
        lastSafeArea = Screen.safeArea;
        var sa = lastSafeArea;

        Vector2 anchorMin = sa.position;
        Vector2 anchorMax = sa.position + sa.size;
        anchorMin.x /= Screen.width;
        anchorMin.y /= Screen.height;
        anchorMax.x /= Screen.width;
        anchorMax.y /= Screen.height;

        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
    }
}
```

`Screen.safeArea` 是 Unity 官方提供的安全区矩形。citeturn1search3  

---

## 平台服务与广告集成

### Android 与 iOS 登录、排行榜/成就总体策略

**推荐总体策略（可最大化过审与复用）：**  
- Android：Google Play Games 登录 + Play Games Leaderboards/Achievements（统一由 Google Play Games 插件处理）。citeturn42search1turn42search7  
- iOS：Game Center（GameKit）用于排行榜/成就；登录走 Game Center 本地玩家认证（或补充 Apple 登录用于账号体系）。citeturn42search0  
- 若强需求 iOS 也提供 Google 登录：应同时提供符合 App Store 4.8 Login Services 要求的“等效登录选项”（实践中通常是 Sign in with Apple），否则存在被拒风险。citeturn36view1turn34search0turn34search1  

### 平台集成修改点与文件清单（Android / iOS / 广告）

| 模块 | Android 集成方案 | iOS 集成方案 | 需要新增/修改的工程文件（Unity 侧） | 风险点与注意事项 |
|---|---|---|---|---|
| 登录（Google Sign-In / Play Games） | 采用 Google Play Games 插件 v11.01+（PGS v2），通过 `Authenticate` 获取授权码/令牌（可用于 Firebase/自建后端）citeturn42search6turn42search7turn42search2 | 若提供 Google 登录可用 Google Sign-In Unity 插件；但若其用于“主账号登录”，需同时提供符合 4.8 的等效登录（通常为 Sign in with Apple）citeturn30search8turn36view1turn34search1 | **新增** `Assets/Plugins/GooglePlayGames/*`（导入插件）；**新增** `Assets/Plugins/GoogleSignIn/*`（如需）；**新增** `Assets/Scripts/Platform/Auth/*`（封装）；**改** `PlayerSettings`（包名、签名） | PGS v1 相关 API 2026-05 起从 SDK 移除，务必走 v2 路线，否则未来不可用citeturn42search15turn42search6 |
| 排行榜/成就 | Google Play Games Services（插件支持 achievements/leaderboards 等）citeturn42search1turn42search7 | Game Center：使用 GameKit 的界面控制器展示排行榜/成就citeturn42search0turn42search3 | **新增** `Assets/Scripts/Platform/Social/*`（统一接口 ISocialService）；**改** `GameManager.cs`（上报分数/成就触发点） | 双平台数据不互通（除非自建跨平台榜单）；成就 ID/榜单 ID 需在各平台后台配置一致命名规范（未指定） |
| 广告（AdMob）横幅/插页/激励 | Google Mobile Ads Unity Plugin v11.0.0：内含 GMA Android SDK 25.0.0、UMP Android 4.0.0 等依赖citeturn38view0turn38view1 | 同一插件亦支持 iOS（GMA iOS SDK 13.0.0、UMP iOS 3.1.0）citeturn38view1 | **新增** `Assets/GoogleMobileAds/*`（导入插件）；**新增** `Assets/Scripts/Monetization/AdManager.cs`（频控/兜底）；**改** UI（Banner 容器安全区） | 需实现 UMP 的 GDPR/隐私入口；并在 iOS 处理 ATT（如涉及跟踪/IDFA）citeturn33search1turn33search19turn33search2 |

### Google Play Games 与 Game Center 对比与落地建议

Google Play Games 插件为 Unity 提供对 Play Games Services 的接入，覆盖认证、好友、成就、排行榜、云存档等（以官方开发者文档为准）。citeturn42search1turn42search7  
Game Center 侧可通过 GameKit 提供统一 UI（例如 `GKGameCenterViewController` 展示成就/排行榜界面）。citeturn42search0turn42search3  

关键差异与对策（面向“完整发布方案”）：

- **平台范围**：Play Games Services 面向 Android；其官方仓库也明确提示 iOS 支持已废弃/不建议用于新应用。citeturn42search4 因此 iOS 必须走 Game Center 或自建后端榜单。  
- **未来兼容性**：Google Play Games v1 SDK/API 正在退场，2026 年 5 月开始从 SDK 移除；应选择 v2 兼容路线（插件 v11.01+）。citeturn42search15turn42search6  
- **跨平台统一榜单**：两者天然割裂。若产品目标要求“iOS/Android 同榜”，需要后端（见下一章）。未指定。  

### AdMob 集成方案（含隐私合规、广告位与频控）

#### SDK/插件版本与依赖

建议使用 **Google Mobile Ads Unity Plugin v11.0.0**（2026-02-25 发布），其 release notes 指出“Built and tested with”包含：  
- Google Mobile Ads Android SDK 25.0.0  
- Google Mobile Ads iOS SDK 13.0.0  
- UMP Android 4.0.0、UMP iOS 3.1.0  
- External Dependency Manager for Unity 1.2.187 citeturn38view0turn38view1  

#### 关键配置点（权限/清单/初始化）

- Android：需在 AndroidManifest 中添加 AdMob App ID 的 `<meta-data android:name="com.google.android.gms.ads.APPLICATION_ID" ...>`（官方 Quick Start 要求）。citeturn33search12  
- Android：目标 API 要求会影响上架；截至 2025-08-31 起，新应用与更新需 target Android 15（API 35）或更高。citeturn33search11  
- Android：涉及 Advertising ID 使用时，需在 Play Console 做广告 ID 相关声明；Advertising ID 为可重置/可删除的广告标识。citeturn33search0 同时在未声明权限等情况下可能返回全 0 ID（会影响广告与分析），需在工程侧核对清单合并结果。citeturn33search34  
- iOS：若广告/分析涉及“跟踪”，需要通过 AppTrackingTransparency 请求授权并读取授权状态（Apple 官方文档）。citeturn33search2turn33search8  

#### GDPR/CCPA 与 UMP（同意管理）

- UMP 的目标是管理用户隐私选择并提供 consent 信息更新机制；官方文档建议在每次应用启动时请求 consent info 更新，以判断是否需要展示同意弹窗或隐私入口。citeturn33search19  
- Unity（AdMob Unity）侧 GDPR 指引明确其基于 UMP SDK 与 IAB TCF v2 消息流程，并建议与 “Get started” 配套使用。citeturn33search1  

**落地建议（最低可过审基线）：**  
应用启动 → 拉取 UMP consent → 若需要则展示同意弹窗 → 初始化广告 SDK → 展示广告；并在设置页提供“隐私选项入口/重设同意”。citeturn33search19turn33search1  

#### 广告位策略与频率控制（推荐值）

- **横幅 Banner**：放置于主菜单/结算页底部安全区容器；游戏进行中默认不展示（避免遮挡与误触）。安全区处理见 `Screen.safeArea`。citeturn1search3  
- **插页 Interstitial**：建议在“Game Over → 返回主菜单/再来一局”间展示，且设置冷却时间（例如 ≥90 秒）与每局最多 1 次。未指定。  
- **激励视频 Rewarded**：用于“继续一次/复活”“双倍金币/双倍奖励”，必须明确奖励规则、失败兜底与防刷（本地节流 + 服务端校验可选）。未指定。  

---

## 架构与后端方案

### 是否需要后端支持的决策框架

当前项目分数/金币存档为本地保存（coin/highScore/lastScore），不存在网络排行榜或账号体系；因此“只做单机 + 平台内排行榜”并不强制需要自建后端。citeturn13view0  

是否需要后端，建议按目标拆分：

- **不需要后端（最省成本）**：  
  - Android：用 Play Games Leaderboards/Achievements  
  - iOS：用 Game Center  
  - 本地存档继续用 SaveGameFree  
  缺点是 iOS/Android 榜单割裂，且作弊/刷分控制受平台能力限制。citeturn42search1turn42search0turn32view0  

- **需要后端（跨平台统一与更强运营能力）**：  
  - 统一账号（Apple/Google/游客）  
  - 跨平台排行榜、赛季、反作弊  
  - 云存档、多端同步  
  此时需要 token 验证、分数上报 API、安全与风控。未指定。  

### 推荐后端：Firebase vs 自建（对 RedRunner 这类体量）

**Firebase 路线（推荐给小团队快速上架）**  
优势在认证与移动端 SDK 完整：  
- Firebase 支持 Unity 下的 Google Sign-In 认证流程。citeturn30search27  
- Android 游戏还可用 Play Games Services 授权码走 Firebase `PlayGamesAuthProvider`。citeturn42search13  

可选组件（按需）：Auth + Firestore + Cloud Functions（分数写入/校验）+ Remote Config（难度/广告频控开关）。未指定。

**自建后端路线（适合强定制/更高可控）**  
采用自建 OAuth/JWT、排行榜服务、反作弊与审计；但需要额外 DevOps、合规与安全投入。未指定。

### API 设计要点与安全建议（面向排行榜/用户数据/Token 验证）

即使选 Firebase，也建议按“最小权限 + 可审计”设计：

- **认证**：客户端仅拿到短期 token；后端校验 token（Google/Apple/Play Games）并签发自家 session（JWT）。Android Play Games 授权码流程参考官方示例路径。citeturn42search7turn42search13  
- **分数上报**：只接受“不可逆作弊”的最小输入（例如关卡种子、时间、输入摘要），服务端重算或做异常检测；至少要有频率限制与重放保护。未指定。  
- **数据最小化**：仅收集实现功能所需数据；并确保你集成的第三方 SDK 的数据收集也在隐私披露范围内（App Store 明确要求披露第三方伙伴的数据处理）。citeturn33search4turn33search27  

#### 架构图（推荐方案示意）

```mermaid
flowchart LR
  subgraph Client[Unity Client]
    A[Gameplay & UI]
    B[Auth Adapter]
    C[Telemetry/Ads Adapter]
  end

  subgraph Platform[Platform Services]
    P1[Google Play Games (Android)]
    P2[Game Center (iOS)]
  end

  subgraph Backend[Optional Backend]
    F1[Firebase Auth]
    F2[Cloud Functions]
    F3[Firestore Leaderboards]
  end

  subgraph Ads[Ads & Privacy]
    GMA[Google Mobile Ads SDK]
    UMP[UMP Consent]
    ATT[AppTrackingTransparency (iOS)]
  end

  A --> B
  B --> P1
  B --> P2
  B --> F1
  F1 --> F2 --> F3
  C --> UMP --> GMA
  C --> ATT
```

---

## UI 与交互重设计

### 移动端 UI/UX 设计原则（可量化）

- **可点按尺寸**：  
  - iOS 建议最小可点击尺寸约 44×44 points。citeturn1search4  
  - 多平台通用建议触控目标 ≥48×48dp（约 9mm），Material Design 与 Android 无障碍文档均强调该尺度与间距建议。citeturn43search0turn43search4  

- **触控可达性**：核心操作放在拇指可达区域（通常屏幕下半部），避免将“跳跃/攻击/暂停”等高频操作放到顶部角落（对大屏手机不友好）。未指定。  
- **状态清晰**：暂停、恢复、重开、退出必须“可见且可触达”，同时与系统后台切换状态一致（避免回到前台仍在游戏）。citeturn13view0turn13view1  

### 新手引导与暂停/恢复建议

- **新手引导**：第一局叠加半透明引导层（指示：移动/跳跃/翻滚），并在玩家完成一次关键动作后自动淡出。未指定。  
- **暂停/恢复**：`UIManager` 当前基于 `Input.GetButtonDown("Cancel")` 打开暂停，并调用 `GameManager.StopGame/ResumeGame`；移动端建议增加显式暂停按钮（右上角），Android 返回键同样映射为暂停。citeturn13view1turn13view0  

### 不同 DPI 的资源规范

美术资源包本身就提供多尺寸 PNG（例如背景 2048×1536/1920×1080、角色 1x/2x/4x、tile set 多规格），这为多 DPI 适配提供了素材基础。citeturn31view0  
建议在 Unity 内统一以“Reference Resolution + Atlas + 高分屏优先”组织，并通过 Sprite Atlas 生成不同平台压缩格式（Android/iOS 分别选择合适压缩）。未指定。

### 关键界面尺寸与布局示例（表格）

下表给出“横屏首发”的参考布局（竖屏可按同原则重排）。所有数值为建议基线，最终以真机可用性与美术风格为准（未指定的以未指定处理）。

| 界面 | 布局建议（横屏） | 关键控件尺寸建议 | 交互说明 |
|---|---|---|---|
| 启动/开始页 | 中央“开始游戏”；右下“设置/隐私”；左下“登录/排行榜”入口 | 主按钮触控目标 ≥48×48dp；iOS 同时满足 44×44pt | 首次进入先处理同意弹窗，再展示开始页citeturn43search0turn1search4turn33search19 |
| 游戏 HUD | 左下虚拟摇杆（或左右分区）；右下跳跃/翻滚；右上暂停 | 虚拟按钮直径建议 ≥64dp；按钮间距 ≥8dp | 高强度操作区尽量在下半屏，避免遮挡角色 |
| 暂停页 | 中央卡片：继续/重开/退出；底部可选“广告去除/隐私” | 每个按钮 ≥48×48dp，卡片内留白 ≥16dp | 暂停时 Time.timeScale=0；回到前台也应保持暂停citeturn13view0turn43search4 |
| 结算/Game Over | 显示本局分数、最高分；“再来一局”；可选插页广告后展示 | 主行动按钮 ≥56dp 高 | 插页广告放在“再来一局”确认后，频控控制（未指定） |
| 设置/隐私 | 音效开关、画质档位、隐私选项入口、署名/开源许可 | 列表项高度 ≥48dp | 必须提供隐私入口与第三方声明页citeturn33search19turn33search4 |

---

## 测试、打包与上架

### 测试计划与设备矩阵

**设备矩阵建议（最小集合）**  
- Android：  
  - 低端（4GB RAM 以下、720p/HD+）、中端（1080p）、高端（2K/高刷）  
  - API Level：覆盖上架要求的 target API（至少 35）及主流版本（未指定具体机型清单）citeturn33search11  
- iOS：  
  - 刘海屏与非刘海屏各至少 1 台；  
  - iOS 主流版本两档（未指定）。  

**性能测试**  
- 使用 Unity Profiler/真机 Profile，重点压测：  
  - 地形生成/回收瞬间帧时间  
  - 长时间运行内存增长（泄漏/缓存）  
  - 后台切换（pause/resume）稳定性  
未指定（需项目内实测数据）。

**自动化测试**  
- 逻辑层：可用 Unity Test Framework 编写分数、存档、广告频控状态机等单元测试。未指定。  
- 构建层：建议 CI 固化 Android AAB 与 iOS Xcode 工程导出步骤（见交付物）。未指定。

### 打包流程与签名要点

**Android**  
- 需满足 Google Play target API 要求（截至 2025-08-31：新应用与更新 target Android 15/API 35 或更高）。citeturn33search11  
- 建议使用 Play App Signing（官方帮助文档给出启用与上传密钥流程）。citeturn34search2turn34search5  
- 若使用 Play Services/登录等，需要注意签名证书 SHA-1 的获取与配置（例如某些服务需要 SHA-1；官方也说明 Play App Signing 下可在 Play Console 查询）。citeturn34search11  

**iOS**  
- Ads/跟踪相关：如需跟踪，必须走 AppTrackingTransparency 授权流程；并在 App Store Connect 做隐私披露。citeturn33search2turn33search4  
- 若提供第三方登录作为主账号入口，需满足 App Review Guidelines 4.8 对“等效登录服务”的要求（通常落地为 Sign in with Apple）。citeturn36view1turn34search1  

### 上架流程与注意事项（Google Play / App Store）

- Google Play：需要填写 Data safety（数据安全表单）披露数据收集/共享/安全实践。citeturn33search3turn33search26  
- App Store：需要在 App Store Connect 提供 App Privacy Details（包含第三方 SDK 的数据收集与用途），并保持准确更新；隐私政策 URL 为必填项。citeturn33search4turn33search10turn33search22  

---

## 资源预估、风险与交付物

### 时间与人力估算（按模块）

以下为“从当前工程到双端可上架”经验估算，按 1 人日=8h；未指定的需求（如横竖屏都支持、跨平台统一榜单、反作弊强要求）会显著拉长周期。

| 模块 | 主要工作 | 估算工时（人时） | 主要角色 |
|---|---|---:|---|
| 工程基线与构建跑通 | Unity 版本对齐、Android/iOS 出包通路、基础崩溃修复 | 40–80 | Unity 客户端、构建工程师 |
| 输入改造（触控） | CrossPlatformInput 统一、虚拟按钮布局、返回键/暂停一致性 | 40–80 | Unity 客户端 |
| UI 适配（分辨率/安全区） | CanvasScaler、SafeArea、HUD 重排、不同屏幕回归 | 60–120 | Unity 客户端、UI/UX |
| 性能优化（P0 地形池化） | Terrain/Background 对象池、状态重置、长跑稳定性 | 80–160 | Unity 客户端 |
| 广告变现（AdMob+UMP+频控） | 接入插件、广告位、频控、同意弹窗、调试工具 | 60–120 | Unity 客户端 |
| 登录与平台服务 | Android：PGS v2 插件；iOS：Game Center；（如需）Google 登录与 Apple 等效登录 | 80–200 | Unity 客户端、iOS/Android 集成 |
| 合规与上架材料 | 隐私政策、数据披露、开源/署名页、商店素材与文案 | 40–120 | 产品/运营、法务支持、UI |
| 测试与发布 | 设备矩阵回归、性能压测、灰度/内测、修复与复测 | 80–200 | QA、Unity 客户端 |

**预算范围（低/中/高，人民币，粗略）：**  
- 低：10–20 万（单机+平台榜单+基础广告，横竖屏择一，最小合规）  
- 中：20–50 万（含较完整性能优化、埋点、较完善 UI/UX、较严格测试）  
- 高：50 万以上（跨平台统一榜单/云存档/反作弊/长期运营体系 + 更广设备覆盖）  
（说明：为经验估算，未指定团队地区与人力单价，需按实际外包/自研成本校准。）

### 风险与合规清单（含缓解）

**技术风险**  
- 地形生成 Instantiate/Destroy 导致移动端卡顿（已在代码中存在）：缓解=对象池化与状态重置、Profiler 驱动优化。citeturn16view3  
- Google Play Games v1 退场：若误用旧插件/旧 API，2026-05 后将出现不可用风险；缓解=强制使用 v2 路线（插件 v11.01+）并做版本锁定。citeturn42search15turn42search6  
- iOS 审核登录规则：若提供 Google 登录作为主账号入口但无等效登录选项，可能被拒；缓解=提供符合 4.8 的等效登录（通常为 Sign in with Apple）或调整产品策略（例如 Google 仅用于账号绑定）。citeturn36view1turn34search0  

**法律/隐私风险**  
- CC BY 4.0 资源未署名：缓解=应用内“致谢/资源来源”固定入口 + 商店描述 Credits。citeturn31view0  
- 广告与隐私同意缺失：缓解=UMP 集成、启动时 consent 更新、设置页提供隐私入口；iOS 需要时走 ATT。citeturn33search19turn33search1turn33search2  
- 商店披露不完整：Google Play Data safety 与 App Store Privacy 都强调第三方 SDK 数据处理需要披露；缓解=建立第三方 SDK 清单与数据字典，发布前审计。citeturn33search3turn33search4turn33search27  

**第三方 SDK 风险**  
- AdMob/PGS/UMP 版本更新快：缓解=锁定版本（例如 AdMob Unity Plugin v11.0.0）并在每次大版本升级前跑全量回归。citeturn38view1turn33search19  

### 最终交付物清单

交付物建议按“可审计、可复现、可上架”组织：

- **代码与工程**  
  - 完整 Unity 工程（含移动端输入/安全区/UI 适配与性能优化）  
  - 代码变更清单（按模块与提交/版本标签）  
  - 第三方 SDK/插件导入记录与版本锁定说明（PGS/AdMob/UMP 等）citeturn38view0turn42search6  

- **构建与发布**  
  - Android：AAB 构建脚本、签名/Play App Signing 操作说明、target API 配置说明citeturn34search2turn33search11  
  - iOS：Xcode 导出配置、证书/描述文件配置说明、（如需）ATT 配置说明citeturn33search2  
  - CI/CD 配置（未指定：GitHub Actions/Jenkins 等）

- **测试与质量**  
  - 设备矩阵报告（机型/系统/分辨率/结果）  
  - 性能测试报告（关键场景帧时间、内存、温控）  
  - 自动化测试用例与结果（未指定范围）

- **合规与商店材料**  
  - 隐私政策文本与 URL（App Store 必填；同时用于 Google Play）citeturn33search22turn33search10  
  - Google Play Data safety 填报依据与记录citeturn33search3turn33search26  
  - App Store Privacy Details（包含第三方 SDK 数据实践）citeturn33search4turn33search27  
  - 开源许可与署名页（MIT + CC BY 4.0 + 其他第三方 notices）citeturn6view0turn31view0turn30search23  

- **运维与迭代**  
  - 运营手册（广告位策略、频控参数、异常兜底、版本回滚）  
  - 风险与依赖升级策略（PGS/AdMob/UMP 的版本观察与升级窗口）citeturn42search15turn38view1