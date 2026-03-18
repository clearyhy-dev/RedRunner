using UnityEngine;

namespace RedRunner.UI
{
    /// <summary>
    /// 通用安全区适配组件。
    /// 将 RectTransform 的锚点限制到 Screen.safeArea 内，避免刘海、挖孔和手势区域遮挡。
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public sealed class SafeAreaFitter : MonoBehaviour
    {
        [SerializeField] private bool applyLeft = true;
        [SerializeField] private bool applyRight = true;
        [SerializeField] private bool applyTop = true;
        [SerializeField] private bool applyBottom = true;
        [SerializeField] private bool onlyOnMobile = true;

        private RectTransform _rectTransform;
        private Rect _lastSafeArea;
        private Vector2 _lastAnchorMin;
        private Vector2 _lastAnchorMax;
        private int _lastScreenWidth;
        private int _lastScreenHeight;

        private void Awake()
        {
            CacheRectTransform();
            ApplyIfNeeded(force: true);
        }

        private void OnEnable()
        {
            CacheRectTransform();
            ApplyIfNeeded(force: true);
        }

        private void Update()
        {
            ApplyIfNeeded(force: false);
        }

        public void ApplyNow()
        {
            CacheRectTransform();
            ApplyIfNeeded(force: true);
        }

        private void CacheRectTransform()
        {
            if (_rectTransform == null)
                _rectTransform = GetComponent<RectTransform>();
        }

        private void ApplyIfNeeded(bool force)
        {
            if (_rectTransform == null)
                return;

            if (onlyOnMobile && !Application.isMobilePlatform)
                return;

            Rect safeArea = Screen.safeArea;
            if (!force &&
                safeArea == _lastSafeArea &&
                Screen.width == _lastScreenWidth &&
                Screen.height == _lastScreenHeight)
            {
                return;
            }

            _lastSafeArea = safeArea;
            _lastScreenWidth = Screen.width;
            _lastScreenHeight = Screen.height;

            Vector2 anchorMin = _rectTransform.anchorMin;
            Vector2 anchorMax = _rectTransform.anchorMax;

            float normalizedMinX = safeArea.xMin / Mathf.Max(1f, Screen.width);
            float normalizedMaxX = safeArea.xMax / Mathf.Max(1f, Screen.width);
            float normalizedMinY = safeArea.yMin / Mathf.Max(1f, Screen.height);
            float normalizedMaxY = safeArea.yMax / Mathf.Max(1f, Screen.height);

            if (applyLeft)
                anchorMin.x = normalizedMinX;
            if (applyRight)
                anchorMax.x = normalizedMaxX;
            if (applyBottom)
                anchorMin.y = normalizedMinY;
            if (applyTop)
                anchorMax.y = normalizedMaxY;

            _rectTransform.anchorMin = anchorMin;
            _rectTransform.anchorMax = anchorMax;
            _lastAnchorMin = anchorMin;
            _lastAnchorMax = anchorMax;
        }
    }
}
