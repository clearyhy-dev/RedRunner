using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Core.Save;
using Services.Auth;

namespace RedRunner.UI
{
    public class StartScreen : UIScreen
    {
        [SerializeField]
        protected Button PlayButton = null;
        [SerializeField]
        protected Button HelpButton = null;
        [SerializeField]
        protected Button InfoButton = null;
        [SerializeField]
        protected Button ExitButton = null;

        [Header("Google 登录（主页）")]
        [SerializeField] protected Button GoogleButton = null;
        [SerializeField] protected Text GoogleStatusText = null;
        [Tooltip("可选：按钮上显示的图标。也可通过菜单 Meow Runner > Setup Google Login Button 自动应用。")]
        [SerializeField] protected Sprite GoogleButtonIcon = null;

        [Header("查看分数（主页）")]
        [SerializeField] protected GameObject ScorePanel = null;
        [SerializeField] protected Text BestScoreText = null;
        [SerializeField] protected Text LastScoreText = null;
        [SerializeField] protected Button ShowScoreButton = null;

        private Canvas _mobileHomeCanvas;
        private GameObject _mobileHomeRoot;
        private Button _mobilePlayButton;
        private Text _mobileBestScoreText;
        private Text _mobileLastScoreText;
        private Button _mobileGoogleButton;
        private Text _mobileGoogleStatusText;
        private Button _mobileExitButton;
        private float _lastMobileOverlayActionTime = -10f;

        private static bool ShouldUseAndroidHomeLayout()
        {
            if (Application.isMobilePlatform)
                return true;

#if UNITY_EDITOR
            return true;
#else
            return false;
#endif
        }

        private void OnEnable()
        {
            GoogleLoginManager.Instance.LoginStateChanged += HandleLoginStateChanged;
            StartCoroutine(BindGoogleLoginDelayed());
        }

        private void OnDisable()
        {
            if (GoogleLoginManager.HasInstance)
                GoogleLoginManager.Instance.LoginStateChanged -= HandleLoginStateChanged;

            if (_mobileHomeRoot != null)
                _mobileHomeRoot.SetActive(false);
        }

        private void Start()
        {
            StartCoroutine(BindGoogleLoginDelayed());
            EnsureMobileHomeLayoutIfNeeded();

            if (PlayButton != null)
            {
                PlayButton.SetButtonAction(OnPlayClick);
            }

            if (ExitButton != null)
                ExitButton.SetButtonAction(() => GameManager.Singleton.ExitGame());

            ApplyGoogleButtonStyle();
            RefreshGoogleUi();

            RefreshScoreUi();
            if (ShowScoreButton != null)
                ShowScoreButton.onClick.AddListener(RefreshScoreUi);
            if (BestScoreText != null || LastScoreText != null || ScorePanel != null)
                RefreshScoreUi();
        }

        private void Update()
        {
            RefreshMobileHomeVisibility();
            HandleMobileHomeTouchFallback();
        }

        private void ApplyGoogleButtonStyle()
        {
            if (GoogleButton == null) return;
            Image img = GoogleButton.targetGraphic as Image;
            if (img == null) img = GoogleButton.GetComponent<Image>();
            if (img != null && GoogleButtonIcon != null)
            {
                img.sprite = GoogleButtonIcon;
                img.preserveAspect = true;
                img.color = Color.white;
            }
            var textChild = GoogleButton.GetComponentInChildren<Text>();
            if (textChild != null)
                textChild.text = GoogleLoginManager.Instance.IsLoggedIn() ? "登出" : "Google 登录";
        }

        private void RefreshGoogleUi()
        {
            string statusText = GoogleLoginManager.Instance.IsLoggedIn()
                ? "已登录: " + GoogleLoginManager.Instance.GetUserName()
                : GoogleLoginManager.Instance.GetLastLoginStatus();

            if (GoogleStatusText != null)
                GoogleStatusText.text = statusText;
            if (GoogleButton != null)
            {
                var t = GoogleButton.GetComponentInChildren<Text>();
                if (t != null && GoogleStatusText == null)
                    t.text = GoogleLoginManager.Instance.IsLoggedIn() ? "登出" : "Google 登录";
            }
            if (_mobileGoogleStatusText != null)
                _mobileGoogleStatusText.text = statusText;
            if (_mobileGoogleButton != null)
            {
                var text = _mobileGoogleButton.GetComponentInChildren<Text>();
                if (text != null)
                    text.text = GoogleLoginManager.Instance.IsLoggedIn() ? "Google 登出" : "Google 登录";
            }
        }

        private void OnGoogleClick()
        {
            try
            {
                GoogleLoginManager.Instance.InitializeManager();
                if (GoogleLoginManager.Instance.IsLoggedIn())
                {
                    GoogleLoginManager.Instance.Logout();
                    if (GameManager.Singleton != null) GameManager.Singleton.ReloadSaveIntoMemory();
                    RefreshGoogleUi();
                    RefreshScoreUi();
                }
                else
                {
                    GoogleLoginManager.Instance.Login(success =>
                    {
                        try
                        {
                            if (GameManager.Singleton != null) GameManager.Singleton.ReloadSaveIntoMemory();
                            RefreshGoogleUi();
                            RefreshScoreUi();
                            if (success)
                            {
                                if (GoogleStatusText != null)
                                    GoogleStatusText.text = "已登录: " + GoogleLoginManager.Instance.GetUserName();
                                if (_mobileGoogleStatusText != null)
                                    _mobileGoogleStatusText.text = "已登录: " + GoogleLoginManager.Instance.GetUserName();
                            }
                            else if (_mobileGoogleStatusText != null)
                            {
                                _mobileGoogleStatusText.text = GoogleLoginManager.Instance.GetLastLoginStatus();
                            }
                        }
                        catch (System.Exception e) { UnityEngine.Debug.LogWarning("Google login refresh: " + e.Message); }
                    });
                }
            }
            catch (System.Exception e) { UnityEngine.Debug.LogWarning("OnGoogleClick: " + e.Message); }
        }

        private void RefreshScoreUi()
        {
            var save = SaveManager.Load();
            int best = Mathf.FloorToInt(save.BestScore);
            int last = Mathf.FloorToInt(save.LastRunScore);
            if (BestScoreText != null) { BestScoreText.text = "最高分: " + best; BestScoreText.gameObject.SetActive(true); }
            if (LastScoreText != null) { LastScoreText.text = "上次: " + last; LastScoreText.gameObject.SetActive(true); }
            if (ScorePanel != null) ScorePanel.SetActive(true);
            if (_mobileBestScoreText != null) _mobileBestScoreText.text = "最高分: " + best;
            if (_mobileLastScoreText != null) _mobileLastScoreText.text = "上次成绩: " + last;
        }

        private void HandleLoginStateChanged(bool _)
        {
            RefreshGoogleUi();
            RefreshScoreUi();
        }

        public override void UpdateScreenStatus(bool open)
        {
            base.UpdateScreenStatus(open);
            if (open)
            {
                EnsureMobileHomeLayoutIfNeeded();
                RefreshMobileHomeVisibility();
                StartCoroutine(BindGoogleLoginDelayed());
                if (GoogleButton != null)
                    GoogleButton.interactable = true;
                RefreshGoogleUi();
                RefreshScoreUi();
            }
            else
                RefreshMobileHomeVisibility();
        }

        /// <summary>延迟一帧绑定，避免被场景里其他 Start 覆盖 Inspector 上绑的按钮的 OnClick。</summary>
        private IEnumerator BindGoogleLoginDelayed()
        {
            yield return null;
            if (GoogleButton == null) TryFindGoogleButton();
            BindGoogleLoginToButton(GoogleButton);
            ApplyGoogleButtonStyle();
            RefreshGoogleUi();
        }

        /// <summary>
        /// 把 Google 登录绑到指定按钮：优先用 Inspector 里拖的 Google Button（如 Share Google Plus Button），清掉场景里旧的 OnClick 后只做登录/登出。
        /// </summary>
        private void BindAllGoogleButtonsToInAppLogin()
        {
            if (GoogleButton == null) TryFindGoogleButton();
            BindGoogleLoginToButton(GoogleButton);
        }

        /// <summary>对单个按钮做“仅登录/登出”绑定：清空 onClick，只加 OnGoogleClick，并保证可点、可射线检测。</summary>
        private void BindGoogleLoginToButton(Button btn)
        {
            if (btn == null) return;
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(OnGoogleClick);
            btn.interactable = true;
            var graphic = btn.targetGraphic ?? btn.GetComponent<Graphic>();
            if (graphic != null) graphic.raycastTarget = true;
        }

        /// <summary>未在 Inspector 绑定 Google Button 时，按名称在场景里找 “Share Google Plus” 等作为备用。</summary>
        private void TryFindGoogleButton()
        {
            var allButtons = UnityEngine.Object.FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var b in allButtons)
            {
                if (b == null || b.gameObject == null) continue;
                string name = b.gameObject.name ?? "";
                if (name.Contains("Share Google Plus") || name.Contains("Google Plus") || name.Contains("G+")
                    || name.Contains("GoogleLogin") || name.Contains("Google 登录"))
                    { GoogleButton = b; return; }
            }
            foreach (var b in allButtons)
            {
                if (b != null && b.gameObject != null && (b.gameObject.name ?? "").Contains("Google"))
                    { GoogleButton = b; return; }
            }
        }

        private void OnPlayClick()
        {
            var uiManager = UIManager.Singleton;
            var inGameScreen = uiManager.UISCREENS.Find(el => el.ScreenInfo == UIScreenInfo.IN_GAME_SCREEN);
            if (inGameScreen != null)
            {
                uiManager.OpenScreen(inGameScreen);
                GameManager.Singleton.StartGame();
            }
        }

        /// <summary>
        /// 手机端首页重排：
        /// 旧场景首页资源在手机上会缩成小红框，这里直接创建一个适合手机点击的首页卡片。
        /// </summary>
        private void EnsureMobileHomeLayoutIfNeeded()
        {
            if (!ShouldUseAndroidHomeLayout())
                return;

            if (_mobileHomeRoot != null)
                return;

            HideLegacyMobileChildren();

            GameObject canvasGo = new GameObject("MobileHomeOverlayCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.transform.SetParent(transform, false);
            _mobileHomeCanvas = canvasGo.GetComponent<Canvas>();
            _mobileHomeCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _mobileHomeCanvas.sortingOrder = 9800;
            MobileUiAdaptation.ConfigureCanvasScaler(canvasGo.GetComponent<CanvasScaler>());

            GameObject safeAreaRoot = new GameObject("MobileHomeSafeArea", typeof(RectTransform), typeof(SafeAreaFitter));
            safeAreaRoot.transform.SetParent(canvasGo.transform, false);
            RectTransform safeAreaRect = safeAreaRoot.GetComponent<RectTransform>();
            safeAreaRect.anchorMin = Vector2.zero;
            safeAreaRect.anchorMax = Vector2.one;
            safeAreaRect.offsetMin = Vector2.zero;
            safeAreaRect.offsetMax = Vector2.zero;

            _mobileHomeRoot = new GameObject("MobileHomePanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            _mobileHomeRoot.transform.SetParent(safeAreaRoot.transform, false);

            RectTransform rootRect = _mobileHomeRoot.GetComponent<RectTransform>();
            rootRect.anchorMin = new Vector2(0.5f, 0f);
            rootRect.anchorMax = new Vector2(0.5f, 0f);
            rootRect.pivot = new Vector2(0.5f, 0f);
            rootRect.sizeDelta = new Vector2(900f, 0f);
            rootRect.anchoredPosition = new Vector2(0f, 220f);

            Image rootImage = _mobileHomeRoot.GetComponent<Image>();
            rootImage.color = new Color(0f, 0f, 0f, 0.42f);
            rootImage.raycastTarget = false;

            VerticalLayoutGroup layout = _mobileHomeRoot.GetComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(28, 28, 28, 28);
            layout.spacing = 18f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            ContentSizeFitter fitter = _mobileHomeRoot.GetComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            Text titleText = CreateLabel("MobileHomeTitle", "Hungry Kitty", 52, FontStyle.Bold, Color.white, 80f);
            titleText.transform.SetParent(_mobileHomeRoot.transform, false);

            _mobileBestScoreText = CreateLabel("MobileBestScore", "最高分: 0", 30, FontStyle.Normal, Color.white, 40f);
            _mobileBestScoreText.transform.SetParent(_mobileHomeRoot.transform, false);

            _mobileLastScoreText = CreateLabel("MobileLastScore", "上次成绩: 0", 30, FontStyle.Normal, Color.white, 40f);
            _mobileLastScoreText.transform.SetParent(_mobileHomeRoot.transform, false);

            _mobilePlayButton = CreateActionButton("MobilePlayButton", "开始游戏", new Color(0.20f, 0.72f, 0.27f, 1f));
            _mobilePlayButton.transform.SetParent(_mobileHomeRoot.transform, false);

            _mobileGoogleStatusText = CreateLabel("MobileGoogleStatus", "点击进行 Google 登录", 26, FontStyle.Normal, Color.white, 40f);
            _mobileGoogleStatusText.transform.SetParent(_mobileHomeRoot.transform, false);

            _mobileGoogleButton = CreateActionButton("MobileGoogleButton", "Google 登录", new Color(0.13f, 0.47f, 0.96f, 1f));
            _mobileGoogleButton.transform.SetParent(_mobileHomeRoot.transform, false);

            if (ExitButton != null)
            {
                _mobileExitButton = CreateActionButton("MobileExitButton", "退出游戏", new Color(0.83f, 0.27f, 0.27f, 1f));
                _mobileExitButton.transform.SetParent(_mobileHomeRoot.transform, false);
            }

            RefreshGoogleUi();
            RefreshScoreUi();
            RefreshMobileHomeVisibility();
        }

        private void HideLegacyMobileChildren()
        {
            for (int i = 0; i < transform.childCount; i++)
            {
                Transform child = transform.GetChild(i);
                if (child == null)
                    continue;

                if (child == _mobileHomeRoot?.transform)
                    continue;

                if (child.name.Contains("MobileHome"))
                    continue;

                child.gameObject.SetActive(false);
            }
        }

        private void RefreshMobileHomeVisibility()
        {
            if (_mobileHomeRoot == null)
                return;

            bool shouldShow = ShouldShowMobileHome();
            if (_mobileHomeRoot.activeSelf != shouldShow)
                _mobileHomeRoot.SetActive(shouldShow);
        }

        private static bool ShouldShowMobileHome()
        {
            if (!ShouldUseAndroidHomeLayout())
                return false;

            if (GameManager.Singleton == null)
                return true;

            return !GameManager.Singleton.gameStarted || GameManager.Singleton.State == GameState.Home;
        }

        private void HandleMobileHomeTouchFallback()
        {
            if (_mobileHomeRoot == null || !_mobileHomeRoot.activeInHierarchy)
                return;

            Vector2? actionPoint = null;
            if (Input.touchCount > 0)
            {
                Touch touch = Input.GetTouch(0);
                if (touch.phase == TouchPhase.Began)
                    actionPoint = touch.position;
            }
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            else if (Input.GetMouseButtonDown(0))
            {
                actionPoint = Input.mousePosition;
            }
#endif
            if (!actionPoint.HasValue)
                return;

            if (Time.unscaledTime - _lastMobileOverlayActionTime < 0.35f)
                return;

            Vector2 point = actionPoint.Value;
            if (IsScreenPointInsideButton(_mobilePlayButton, point))
            {
                _lastMobileOverlayActionTime = Time.unscaledTime;
                OnPlayClick();
                return;
            }
            if (IsScreenPointInsideButton(_mobileGoogleButton, point))
            {
                _lastMobileOverlayActionTime = Time.unscaledTime;
                OnGoogleClick();
                return;
            }
            if (IsScreenPointInsideButton(_mobileExitButton, point))
            {
                _lastMobileOverlayActionTime = Time.unscaledTime;
                GameManager.Singleton.ExitGame();
            }
        }

        private bool IsScreenPointInsideButton(Button button, Vector2 screenPoint)
        {
            if (button == null)
                return false;

            RectTransform rectTransform = button.GetComponent<RectTransform>();
            if (rectTransform == null)
                return false;

            Camera uiCamera = _mobileHomeCanvas != null && _mobileHomeCanvas.renderMode != RenderMode.ScreenSpaceOverlay
                ? _mobileHomeCanvas.worldCamera
                : null;

            return RectTransformUtility.RectangleContainsScreenPoint(rectTransform, screenPoint, uiCamera);
        }

        private static Text CreateLabel(string objectName, string value, int fontSize, FontStyle fontStyle, Color color, float minHeight)
        {
            GameObject go = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text), typeof(LayoutElement));
            Text text = go.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.text = value;
            text.fontSize = fontSize;
            text.fontStyle = fontStyle;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = color;
            text.raycastTarget = false;

            LayoutElement element = go.GetComponent<LayoutElement>();
            element.minHeight = minHeight;
            element.preferredHeight = minHeight;
            return text;
        }

        private static Button CreateActionButton(string objectName, string label, Color backgroundColor)
        {
            GameObject buttonGo = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(LayoutElement));
            Image buttonImage = buttonGo.GetComponent<Image>();
            buttonImage.color = backgroundColor;
            buttonImage.raycastTarget = true;

            LayoutElement buttonLayout = buttonGo.GetComponent<LayoutElement>();
            buttonLayout.minHeight = 110f;
            buttonLayout.preferredHeight = 110f;

            GameObject textGo = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            textGo.transform.SetParent(buttonGo.transform, false);
            RectTransform textRect = textGo.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            Text text = textGo.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.text = label;
            text.fontSize = 38;
            text.fontStyle = FontStyle.Bold;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.raycastTarget = false;

            Button button = buttonGo.GetComponent<Button>();
            button.targetGraphic = buttonImage;
            return button;
        }

    }
}