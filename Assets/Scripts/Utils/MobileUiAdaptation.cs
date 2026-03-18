using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 手机 UI 适配统一参数。
/// 负责统一参考分辨率、CanvasScaler 推荐配置，以及正交相机在手机纵横比下的视野计算。
/// </summary>
public static class MobileUiAdaptation
{
    public const int ReferenceWidth = 1080;
    public const int ReferenceHeight = 1920;
    public const float ReferencePixelsPerUnit = 100f;

    public static Vector2 ReferenceResolution => new Vector2(ReferenceWidth, ReferenceHeight);

    /// <summary>
    /// 竖屏优先按宽适配，横屏优先按高适配。
    /// 这样在手机上更不容易出现按钮过大或被挤出屏幕的问题。
    /// </summary>
    public static float GetRecommendedMatchWidthOrHeight(int screenWidth, int screenHeight)
    {
        if (screenWidth <= 0 || screenHeight <= 0)
            return 0.5f;

        return screenHeight >= screenWidth ? 0f : 1f;
    }

    public static void ConfigureCanvasScaler(CanvasScaler scaler)
    {
        if (scaler == null)
            return;

        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = ReferenceResolution;
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = GetRecommendedMatchWidthOrHeight(Screen.width, Screen.height);
        scaler.referencePixelsPerUnit = ReferencePixelsPerUnit;
    }

    /// <summary>
    /// 保证手机上至少能看到设计时的参考宽度。
    /// 当屏幕更“窄高”时，自动拉大 orthographicSize，避免左右被裁切。
    /// </summary>
    public static float GetOrthographicSizeForPhone(float designOrthoSize, int screenWidth, int screenHeight)
    {
        if (designOrthoSize <= 0f || screenWidth <= 0 || screenHeight <= 0)
            return designOrthoSize;

        float referenceAspect = (float)ReferenceWidth / ReferenceHeight;
        float currentAspect = (float)screenWidth / screenHeight;
        if (currentAspect <= 0f)
            return designOrthoSize;

        float widthSafeOrtho = designOrthoSize * (referenceAspect / currentAspect);
        return Mathf.Max(designOrthoSize, widthSafeOrtho);
    }
}
