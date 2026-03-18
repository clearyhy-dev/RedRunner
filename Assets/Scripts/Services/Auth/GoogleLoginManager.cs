using System;
using Services.Config;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using RedRunner;
using RedRunner.UI;

#if USE_GPGS
using GooglePlayGames;
using GooglePlayGames.BasicApi;
#endif

namespace Services.Auth
{
    /// <summary>
    /// Google 登录单例管理器。
    /// 负责初始化 GPGS、静默登录、手动登录、登出，以及提供统一查询接口。
    /// </summary>
    public sealed class GoogleLoginManager : MonoBehaviour
    {
        private const string ManagerObjectName = "[GoogleLoginManager]";
        private const string PrefUserId = "meow_google_user_id";
        private const string PrefDisplayName = "meow_google_display_name";

        private static GoogleLoginManager s_Instance;

        private bool _initialized;
        private bool _loginInProgress;
        private bool _isLoggedIn;
        private string _cachedUserId = string.Empty;
        private string _cachedUserName = string.Empty;
        private string _lastLoginStatus = "未开始登录";
        private float _loginStartedAt = -1f;
        private Canvas _overlayCanvas;
        private GameObject _overlayRoot;
        private Button _overlayButton;
        private Text _overlayStatusText;
        private float _lastOverlayActionTime = -10f;

        public static GoogleLoginManager Instance
        {
            get
            {
                if (s_Instance == null)
                    CreateSingleton();
                return s_Instance;
            }
        }

        public static bool HasInstance => s_Instance != null;

        /// <summary>登录状态变化时回调，方便后续扩展排行榜或云存档。</summary>
        public event Action<bool> LoginStateChanged;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void AutoCreateOnBoot()
        {
            CreateSingleton();
        }

        private static void CreateSingleton()
        {
            if (s_Instance != null)
                return;

            var existing = FindAnyObjectByType<GoogleLoginManager>();
            if (existing != null)
            {
                s_Instance = existing;
                s_Instance.InitializeManager();
                return;
            }

            var go = new GameObject(ManagerObjectName);
            s_Instance = go.AddComponent<GoogleLoginManager>();
            DontDestroyOnLoad(go);
            s_Instance.InitializeManager();
        }

        private void Awake()
        {
            if (s_Instance != null && s_Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            s_Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeManager();
        }

        private void Update()
        {
            if (_loginInProgress && _loginStartedAt > 0f && Time.unscaledTime - _loginStartedAt > 8f)
            {
                _loginInProgress = false;
                _loginStartedAt = -1f;
                _lastLoginStatus = "登录超时，请重试";
                Debug.LogWarning("[GoogleLogin] 登录流程超时，已自动释放锁。");
            }

            RefreshOverlayVisibility();

            if (_overlayRoot == null || !_overlayRoot.activeInHierarchy || _overlayButton == null || !_overlayButton.gameObject.activeInHierarchy)
                return;

            // 某些机型上 Unity UI 的 onClick 不触发，这里额外做一次手动触摸命中检测。
            if (Input.touchCount > 0)
            {
                Touch touch = Input.GetTouch(0);
                if (touch.phase == TouchPhase.Ended && IsScreenPointInsideOverlayButton(touch.position))
                {
                    Debug.Log("[GoogleLogin] 手动触摸检测命中悬浮登录按钮。");
                    TriggerOverlayAction();
                }
            }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (Input.GetMouseButtonUp(0) && IsScreenPointInsideOverlayButton(Input.mousePosition))
            {
                Debug.Log("[GoogleLogin] 鼠标点击命中悬浮登录按钮。");
                TriggerOverlayAction();
            }
#endif
        }

        /// <summary>
        /// 初始化登录管理器，并尝试读取本地缓存的登录信息。
        /// 这里只做本地状态准备，不会强制发起手动登录。
        /// </summary>
        public void InitializeManager()
        {
            if (_initialized)
                return;

            _cachedUserId = PlayerPrefs.GetString(PrefUserId, string.Empty);
            _cachedUserName = PlayerPrefs.GetString(PrefDisplayName, string.Empty);
            // 本地缓存只用于显示，不代表 GPGS 真实已完成鉴权。
            _isLoggedIn = false;
            _initialized = true;
            EnsureEventSystem();
            EnsureOverlayUi();

#if USE_GPGS
            try
            {
                PlayGamesPlatform.Activate();
                Debug.Log("[GoogleLogin] Play Games Platform 已激活。");
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[GoogleLogin] 激活 Play Games Platform 失败: " + ex.Message);
            }
#endif
        }

        /// <summary>手动点击按钮后调用，发起 Google 登录。</summary>
        public void Login(Action<bool> onComplete = null)
        {
            InitializeManager();

            if (_loginInProgress)
            {
                if (!string.IsNullOrEmpty(_lastLoginStatus) && _lastLoginStatus.StartsWith("正在尝试静默登录", StringComparison.Ordinal))
                {
                    // 静默登录还在进行时，允许手动登录接管，避免首页一直卡在“已有登录流程进行中”。
                    _loginInProgress = false;
                    _loginStartedAt = -1f;
                }
                else
                {
                    _lastLoginStatus = "已有登录流程进行中";
                    Debug.Log("[GoogleLogin] 当前已有登录流程进行中，忽略重复点击。");
                    onComplete?.Invoke(false);
                    return;
                }
            }

            if (!IsLoginAvailable())
            {
                if (string.IsNullOrEmpty(_lastLoginStatus))
                    _lastLoginStatus = "当前登录条件不满足";
                Debug.LogWarning("[GoogleLogin] 当前登录条件不满足，请先检查 GPGS 配置。");
                onComplete?.Invoke(false);
                return;
            }

            if (_isLoggedIn)
            {
                _lastLoginStatus = "已登录";
                Debug.Log("[GoogleLogin] 当前已登录，无需重复登录。");
                onComplete?.Invoke(true);
                return;
            }

#if USE_GPGS
            _loginInProgress = true;
            _loginStartedAt = Time.unscaledTime;
            _lastLoginStatus = "正在拉起 Google 登录...";
            Debug.Log("[GoogleLogin] 开始发起手动 Google 登录...");
            PlayGamesPlatform.Instance.ManuallyAuthenticate(result =>
            {
                _loginInProgress = false;
                _loginStartedAt = -1f;
                bool success = result == SignInStatus.Success;
                HandleLoginResult(success, result.ToString());
                onComplete?.Invoke(success);
            });
#else
            _lastLoginStatus = "当前构建未启用 USE_GPGS";
            Debug.LogWarning("[GoogleLogin] 当前构建未启用 USE_GPGS，无法执行真实 Google 登录。");
            onComplete?.Invoke(false);
#endif
        }

        /// <summary>应用启动后可调用，尝试静默恢复 Google 登录态。</summary>
        public void TrySilentLogin(Action<bool> onComplete = null)
        {
            InitializeManager();

            if (_loginInProgress)
            {
                _lastLoginStatus = "已有登录流程进行中";
                onComplete?.Invoke(false);
                return;
            }

            if (_isLoggedIn)
            {
                onComplete?.Invoke(true);
                return;
            }

            if (!IsLoginAvailable())
            {
                onComplete?.Invoke(false);
                return;
            }

#if USE_GPGS
            _loginInProgress = true;
            _loginStartedAt = Time.unscaledTime;
            _lastLoginStatus = "正在尝试静默登录...";
            Debug.Log("[GoogleLogin] 尝试静默恢复 Google 登录...");
            PlayGamesPlatform.Instance.Authenticate(result =>
            {
                _loginInProgress = false;
                _loginStartedAt = -1f;
                bool success = result == SignInStatus.Success;
                HandleLoginResult(success, result.ToString(), isSilent: true);
                onComplete?.Invoke(success);
            });
#else
            onComplete?.Invoke(false);
#endif
        }

        /// <summary>本地登出，清理缓存的账号信息。</summary>
        public void Logout()
        {
            InitializeManager();
            _loginInProgress = false;
            _loginStartedAt = -1f;
            _isLoggedIn = false;
            _cachedUserId = string.Empty;
            _cachedUserName = string.Empty;
            _lastLoginStatus = "已登出";
            PlayerPrefs.DeleteKey(PrefUserId);
            PlayerPrefs.DeleteKey(PrefDisplayName);
            PlayerPrefs.Save();
            Debug.Log("[GoogleLogin] 已清理本地登录状态。");
            RefreshOverlayUi();
            LoginStateChanged?.Invoke(false);
        }

        /// <summary>供 UI 和业务判断当前是否已登录。</summary>
        public bool IsLoggedIn()
        {
            InitializeManager();
            return _isLoggedIn;
        }

        /// <summary>返回当前已登录用户昵称，未登录时返回空字符串。</summary>
        public string GetUserName()
        {
            InitializeManager();
            return _cachedUserName ?? string.Empty;
        }

        /// <summary>后续排行榜/云存档可复用用户唯一标识。</summary>
        public string GetUserId()
        {
            InitializeManager();
            return _cachedUserId ?? string.Empty;
        }

        public string GetLastLoginStatus()
        {
            InitializeManager();
            return _lastLoginStatus ?? string.Empty;
        }

        private bool IsLoginAvailable()
        {
            var config = AndroidPlatformServicesConfig.Current;
            bool enabledInConfig = config?.googlePlayGames != null && config.googlePlayGames.enableGooglePlayGames;
            if (!enabledInConfig)
            {
                _lastLoginStatus = "配置未启用 Google Play Games";
                Debug.LogWarning("[GoogleLogin] AndroidPlatformServicesConfig.json 中未启用 Google Play Games。");
                return false;
            }

#if USE_GPGS
            if (!GameInfo.ApplicationIdInitialized())
            {
                _lastLoginStatus = "GPGS App ID 未初始化";
                Debug.LogWarning("[GoogleLogin] GPGS App ID 尚未正确写入 GameInfo.cs。");
                return false;
            }

            _lastLoginStatus = "GPGS 可用，等待授权";
            return true;
#else
            _lastLoginStatus = "当前构建未启用 USE_GPGS";
            Debug.LogWarning("[GoogleLogin] 当前构建未启用 USE_GPGS。");
            return false;
#endif
        }

        private void HandleLoginResult(bool success, string rawStatus, bool isSilent = false)
        {
            if (success)
            {
#if USE_GPGS
                _cachedUserId = PlayGamesPlatform.Instance.GetUserId() ?? string.Empty;
                _cachedUserName = PlayGamesPlatform.Instance.GetUserDisplayName() ?? string.Empty;
#endif
                _isLoggedIn = !string.IsNullOrEmpty(_cachedUserId);
                PlayerPrefs.SetString(PrefUserId, _cachedUserId);
                PlayerPrefs.SetString(PrefDisplayName, _cachedUserName);
                PlayerPrefs.Save();
                _lastLoginStatus = "登录成功: " + _cachedUserName;
                Debug.Log("[GoogleLogin] " + (isSilent ? "静默登录成功" : "手动登录成功") + "，用户: " + _cachedUserName);
                RefreshOverlayUi();
                LoginStateChanged?.Invoke(true);
                return;
            }

            _isLoggedIn = false;
            _lastLoginStatus = (isSilent ? "静默登录失败: " : "手动登录失败: ") + rawStatus;
            Debug.LogWarning("[GoogleLogin] " + (isSilent ? "静默登录失败" : "手动登录失败") + "，状态: " + rawStatus);
            RefreshOverlayUi();
            LoginStateChanged?.Invoke(false);
        }

        /// <summary>
        /// 兜底创建 UI 事件系统，避免按钮能显示但完全收不到点击。
        /// </summary>
        private void EnsureEventSystem()
        {
            EventSystem existing = FindFirstObjectByType<EventSystem>(FindObjectsInactive.Include);
            if (existing != null)
                return;

            GameObject eventSystemGo = new GameObject("GoogleLoginEventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            DontDestroyOnLoad(eventSystemGo);
            Debug.Log("[GoogleLogin] 已自动创建 EventSystem，确保悬浮登录按钮可点击。");
        }

        /// <summary>
        /// 手机端全局悬浮登录入口。
        /// 直接创建顶层 Overlay Canvas，避免被场景里的错误布局遮住。
        /// </summary>
        private void EnsureOverlayUi()
        {
            if (!Application.isMobilePlatform)
                return;

            if (_overlayCanvas != null)
            {
                RefreshOverlayUi();
                return;
            }

            GameObject canvasGo = new GameObject("GoogleLoginOverlayCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.transform.SetParent(transform, false);
            _overlayCanvas = canvasGo.GetComponent<Canvas>();
            _overlayCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _overlayCanvas.sortingOrder = 9999;

            CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
            MobileUiAdaptation.ConfigureCanvasScaler(scaler);

            GameObject safeAreaGo = new GameObject("GoogleLoginOverlaySafeArea", typeof(RectTransform), typeof(SafeAreaFitter));
            safeAreaGo.transform.SetParent(canvasGo.transform, false);
            RectTransform safeAreaRect = safeAreaGo.GetComponent<RectTransform>();
            safeAreaRect.anchorMin = Vector2.zero;
            safeAreaRect.anchorMax = Vector2.one;
            safeAreaRect.offsetMin = Vector2.zero;
            safeAreaRect.offsetMax = Vector2.zero;
            safeAreaGo.GetComponent<SafeAreaFitter>().ApplyNow();

            GameObject panelGo = new GameObject("GoogleLoginOverlayPanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            panelGo.transform.SetParent(safeAreaGo.transform, false);
            _overlayRoot = panelGo;
            RectTransform panelRect = panelGo.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0f, 0f);
            panelRect.anchorMax = new Vector2(1f, 0f);
            panelRect.pivot = new Vector2(0.5f, 0f);
            panelRect.offsetMin = new Vector2(24f, 0f);
            panelRect.offsetMax = new Vector2(-24f, 0f);
            panelRect.anchoredPosition = new Vector2(0f, 24f);

            Image panelImage = panelGo.GetComponent<Image>();
            panelImage.color = new Color(0f, 0f, 0f, 0.38f);
            panelImage.raycastTarget = false;

            VerticalLayoutGroup layoutGroup = panelGo.GetComponent<VerticalLayoutGroup>();
            layoutGroup.padding = new RectOffset(16, 16, 16, 16);
            layoutGroup.spacing = 8f;
            layoutGroup.childAlignment = TextAnchor.LowerCenter;
            layoutGroup.childControlWidth = true;
            layoutGroup.childControlHeight = true;
            layoutGroup.childForceExpandWidth = true;
            layoutGroup.childForceExpandHeight = false;

            ContentSizeFitter contentSizeFitter = panelGo.GetComponent<ContentSizeFitter>();
            contentSizeFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            contentSizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            GameObject buttonGo = new GameObject("GoogleLoginOverlayButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(LayoutElement));
            buttonGo.transform.SetParent(panelGo.transform, false);
            RectTransform buttonRect = buttonGo.GetComponent<RectTransform>();
            buttonRect.anchorMin = new Vector2(0f, 0.5f);
            buttonRect.anchorMax = new Vector2(1f, 0.5f);
            buttonRect.pivot = new Vector2(0.5f, 0.5f);
            buttonRect.offsetMin = Vector2.zero;
            buttonRect.offsetMax = Vector2.zero;

            Image buttonImage = buttonGo.GetComponent<Image>();
            buttonImage.color = new Color(0.13f, 0.47f, 0.96f, 1f);
            LayoutElement buttonLayout = buttonGo.GetComponent<LayoutElement>();
            buttonLayout.minHeight = 96f;
            buttonLayout.preferredHeight = 96f;
            _overlayButton = buttonGo.GetComponent<Button>();
            _overlayButton.targetGraphic = buttonImage;
            _overlayButton.onClick.AddListener(OnOverlayButtonClick);

            GameObject buttonTextGo = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            buttonTextGo.transform.SetParent(buttonGo.transform, false);
            RectTransform buttonTextRect = buttonTextGo.GetComponent<RectTransform>();
            buttonTextRect.anchorMin = Vector2.zero;
            buttonTextRect.anchorMax = Vector2.one;
            buttonTextRect.offsetMin = Vector2.zero;
            buttonTextRect.offsetMax = Vector2.zero;
            Text buttonText = buttonTextGo.GetComponent<Text>();
            buttonText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            buttonText.fontSize = 34;
            buttonText.alignment = TextAnchor.MiddleCenter;
            buttonText.color = Color.white;

            GameObject statusTextGo = new GameObject("StatusText", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text), typeof(LayoutElement));
            statusTextGo.transform.SetParent(panelGo.transform, false);
            RectTransform statusRect = statusTextGo.GetComponent<RectTransform>();
            statusRect.anchorMin = new Vector2(0f, 0.5f);
            statusRect.anchorMax = new Vector2(1f, 0.5f);
            statusRect.pivot = new Vector2(0.5f, 0.5f);
            statusRect.offsetMin = Vector2.zero;
            statusRect.offsetMax = Vector2.zero;

            _overlayStatusText = statusTextGo.GetComponent<Text>();
            _overlayStatusText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _overlayStatusText.fontSize = 24;
            _overlayStatusText.alignment = TextAnchor.MiddleCenter;
            _overlayStatusText.color = Color.white;
            LayoutElement statusLayout = statusTextGo.GetComponent<LayoutElement>();
            statusLayout.minHeight = 40f;
            statusLayout.preferredHeight = 44f;

            RefreshOverlayUi();
            RefreshOverlayVisibility();
            Debug.Log("[GoogleLogin] 已创建全局悬浮 Google 登录按钮。");
        }

        private void RefreshOverlayUi()
        {
            if (_overlayButton == null || _overlayStatusText == null)
                return;

            Text buttonText = _overlayButton.GetComponentInChildren<Text>();
            if (buttonText != null)
                buttonText.text = _isLoggedIn ? "Google 登出" : "Google 登录";

            _overlayStatusText.text = _isLoggedIn
                ? "已登录: " + (_cachedUserName ?? string.Empty)
                : (_lastLoginStatus ?? "手机端悬浮登录入口");
        }

        private void RefreshOverlayVisibility()
        {
            if (_overlayRoot == null)
                return;

            bool shouldShow = ShouldShowOverlay();
            if (_overlayRoot.activeSelf != shouldShow)
                _overlayRoot.SetActive(shouldShow);
        }

        private static bool ShouldShowOverlay()
        {
            if (!Application.isMobilePlatform)
                return false;

            StartScreen startScreen = FindFirstObjectByType<StartScreen>(FindObjectsInactive.Include);
            if (startScreen != null && startScreen.IsOpen)
                return false;

            if (GameManager.Singleton == null)
                return true;

            return !GameManager.Singleton.gameStarted || GameManager.Singleton.State == GameState.Home;
        }

        private void OnOverlayButtonClick()
        {
            Debug.Log("[GoogleLogin] 收到悬浮登录按钮点击。");
            TriggerOverlayAction();
        }

        private void TriggerOverlayAction()
        {
            if (Time.unscaledTime - _lastOverlayActionTime < 0.4f)
                return;

            _lastOverlayActionTime = Time.unscaledTime;

            if (IsLoggedIn())
            {
                Logout();
                if (GameManager.Singleton != null)
                    GameManager.Singleton.ReloadSaveIntoMemory();
                return;
            }

            Login(_ =>
            {
                if (GameManager.Singleton != null)
                    GameManager.Singleton.ReloadSaveIntoMemory();
                RefreshOverlayUi();
            });
        }

        private bool IsScreenPointInsideOverlayButton(Vector2 screenPoint)
        {
            if (_overlayButton == null)
                return false;

            RectTransform rectTransform = _overlayButton.GetComponent<RectTransform>();
            if (rectTransform == null)
                return false;

            Camera uiCamera = _overlayCanvas != null && _overlayCanvas.renderMode != RenderMode.ScreenSpaceOverlay
                ? _overlayCanvas.worldCamera
                : null;

            return RectTransformUtility.RectangleContainsScreenPoint(rectTransform, screenPoint, uiCamera);
        }
    }
}
