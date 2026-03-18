using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 运行时兜底：若场景中 Canvas 的 Canvas Scaler 未配置为自适应，则在 Awake 时设置为 Scale With Screen Size，解决 APK 在手机上页面过大的问题。
/// 挂到场景中任意常驻物体（如 GameManager）上即可；不挂则依赖编辑器菜单「Meow Runner > Android 适配 > 配置当前场景 Canvas 自适应」预先配置。
/// </summary>
public class CanvasScalerFallback : MonoBehaviour
{
    [Tooltip("仅移动端生效")]
    public bool onlyOnMobile = true;

    private int _lastScreenWidth;
    private int _lastScreenHeight;

    private void Awake()
    {
        ApplyIfNeeded(force: true);
    }

    private void Update()
    {
        ApplyIfNeeded(force: false);
    }

    private void ApplyIfNeeded(bool force)
    {
        if (onlyOnMobile && !Application.isMobilePlatform)
            return;

        if (!force && Screen.width == _lastScreenWidth && Screen.height == _lastScreenHeight)
            return;

        _lastScreenWidth = Screen.width;
        _lastScreenHeight = Screen.height;

        Canvas[] canvases = UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (Canvas c in canvases)
        {
            if (c.renderMode != RenderMode.ScreenSpaceOverlay && c.renderMode != RenderMode.ScreenSpaceCamera)
                continue;

            CanvasScaler scaler = c.GetComponent<CanvasScaler>();
            if (scaler == null)
                scaler = c.gameObject.AddComponent<CanvasScaler>();

            MobileUiAdaptation.ConfigureCanvasScaler(scaler);
        }
    }
}
