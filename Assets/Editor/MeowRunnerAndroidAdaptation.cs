#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using RedRunner.UI;

/// <summary>
/// Android 手机适配辅助工具：
/// 统一 Canvas 自适应、拉伸 UIScreen 根节点、给选中对象添加安全区组件，以及输出检查结果。
/// 菜单：Meow Runner > Tools > Android 适配 > ...
/// </summary>
public static class MeowRunnerAndroidAdaptation
{
    private static bool EnsureNotInPlayMode()
    {
        if (!EditorApplication.isPlaying)
            return true;

        EditorUtility.DisplayDialog("请先退出运行模式", "Android 适配菜单只能在非 Play 模式下执行。请先停止运行，再执行该菜单。", "确定");
        return false;
    }

    [MenuItem("Meow Runner/Tools/Android 适配/配置当前场景 Canvas 自适应", false, 200)]
    public static void ConfigureCanvasScaler()
    {
        if (!EnsureNotInPlayMode())
            return;

        Canvas[] canvases = Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None);
        if (canvases == null || canvases.Length == 0)
        {
            EditorUtility.DisplayDialog("未找到 Canvas", "当前场景中没有 Canvas。请打开包含主 UI 的场景（如 Play）再执行。", "确定");
            return;
        }
        int configured = 0;
        foreach (Canvas c in canvases)
        {
            if (c.renderMode == RenderMode.ScreenSpaceOverlay || c.renderMode == RenderMode.ScreenSpaceCamera)
            {
                CanvasScaler scaler = c.GetComponent<CanvasScaler>();
                if (scaler == null)
                    scaler = c.gameObject.AddComponent<CanvasScaler>();
                MobileUiAdaptation.ConfigureCanvasScaler(scaler);
                configured++;
                EditorUtility.SetDirty(c.gameObject);
            }
        }
        if (configured > 0)
        {
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
            EditorUtility.DisplayDialog(
                "已配置",
                "已为 " + configured + " 个 Canvas 设置手机自适应（Scale With Screen Size，参考 "
                + MobileUiAdaptation.ReferenceWidth + "x" + MobileUiAdaptation.ReferenceHeight + "）。请保存场景。",
                "确定");
        }
        else
            EditorUtility.DisplayDialog("未修改", "未找到可配置的 Screen Space Canvas。", "确定");
    }

    [MenuItem("Meow Runner/Tools/Android 适配/重置当前场景 UIScreen 到手机参考框", false, 201)]
    public static void NormalizeUIScreenRoots()
    {
        if (!EnsureNotInPlayMode())
            return;

        UIScreen[] screens = Object.FindObjectsByType<UIScreen>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        if (screens == null || screens.Length == 0)
        {
            EditorUtility.DisplayDialog("未找到 UIScreen", "当前场景中没有 UIScreen 组件。", "确定");
            return;
        }

        int updated = 0;
        foreach (UIScreen screen in screens)
        {
            RectTransform rectTransform = screen.GetComponent<RectTransform>();
            if (rectTransform == null)
                continue;

            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.sizeDelta = MobileUiAdaptation.ReferenceResolution;
            rectTransform.anchoredPosition = Vector2.zero;
            rectTransform.localScale = Vector3.one;
            updated++;
            EditorUtility.SetDirty(rectTransform);
        }

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
        EditorUtility.DisplayDialog("已处理", "已将 " + updated + " 个 UIScreen 根节点重置到手机参考框。", "确定");
    }

    [MenuItem("Meow Runner/Tools/Android 适配/给选中对象添加 SafeAreaFitter", false, 202)]
    public static void AddSafeAreaFitterToSelection()
    {
        if (!EnsureNotInPlayMode())
            return;

        if (Selection.gameObjects == null || Selection.gameObjects.Length == 0)
        {
            EditorUtility.DisplayDialog("未选择对象", "请先选中一个或多个需要适配安全区的 UI 容器。", "确定");
            return;
        }

        int added = 0;
        foreach (GameObject go in Selection.gameObjects)
        {
            if (go == null || go.GetComponent<RectTransform>() == null)
                continue;

            if (go.GetComponent<SafeAreaFitter>() == null)
            {
                go.AddComponent<SafeAreaFitter>();
                added++;
                EditorUtility.SetDirty(go);
            }
        }

        if (added > 0)
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

        EditorUtility.DisplayDialog("处理完成", "已新增 " + added + " 个 SafeAreaFitter。", "确定");
    }

    [MenuItem("Meow Runner/Tools/Android 适配/检查当前场景手机适配问题", false, 203)]
    public static void CheckSceneAdaptation()
    {
        if (!EnsureNotInPlayMode())
            return;

        Canvas[] canvases = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        UIScreen[] screens = Object.FindObjectsByType<UIScreen>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        int canvasWarnings = 0;
        int screenWarnings = 0;

        foreach (Canvas canvas in canvases)
        {
            if (canvas.renderMode != RenderMode.ScreenSpaceOverlay && canvas.renderMode != RenderMode.ScreenSpaceCamera)
                continue;

            CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
            if (scaler == null || scaler.uiScaleMode != CanvasScaler.ScaleMode.ScaleWithScreenSize)
            {
                canvasWarnings++;
                Debug.LogWarning("[AndroidAdaptation] Canvas 未正确配置 Scale With Screen Size: " + canvas.name, canvas);
            }
        }

        foreach (UIScreen screen in screens)
        {
            RectTransform rectTransform = screen.GetComponent<RectTransform>();
            if (rectTransform == null)
                continue;

            if (rectTransform.anchorMin != new Vector2(0.5f, 0.5f) ||
                rectTransform.anchorMax != new Vector2(0.5f, 0.5f) ||
                rectTransform.sizeDelta != MobileUiAdaptation.ReferenceResolution)
            {
                screenWarnings++;
                Debug.LogWarning("[AndroidAdaptation] UIScreen 根节点未回到手机参考框: " + screen.name, screen);
            }
        }

        EditorUtility.DisplayDialog(
            "检查完成",
            "Canvas 警告: " + canvasWarnings + "\nUIScreen 警告: " + screenWarnings + "\n详细对象请看 Console。",
            "确定");
    }

    [MenuItem("Meow Runner/Tools/Android 适配/设置安卓竖屏（推荐）", false, 204)]
    public static void SetPortrait()
    {
        if (!EnsureNotInPlayMode())
            return;

        if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
        {
            EditorUtility.DisplayDialog("请先切换平台", "当前不是 Android 平台。请先在 Build Settings 中切换到 Android 再执行。", "确定");
            return;
        }

        PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
        PlayerSettings.allowedAutorotateToPortrait = true;
        PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
        PlayerSettings.allowedAutorotateToLandscapeLeft = false;
        PlayerSettings.allowedAutorotateToLandscapeRight = false;
        EditorUtility.DisplayDialog("已设置", "Android 已设为竖屏优先，适合当前手机 UI 基线。", "确定");
    }
}
#endif
