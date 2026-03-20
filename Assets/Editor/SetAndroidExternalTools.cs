#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

/// <summary>
/// 把本机已有的 Android SDK/NDK/JDK 路径一次性写入 Unity 的 External Tools。
/// 使用前请把下面三个路径改成你电脑上的实际路径，再在菜单执行 Meow Runner > Android 环境 > Set Android SDK/NDK/JDK From Script。
/// </summary>
public static class SetAndroidExternalTools
{
    // ========== 已按你本机环境变量配置（ANDROID_SDK_ROOT、JAVA_HOME）==========
    private const string SDK_PATH = "D:\\Android\\Sdk";
    private const string NDK_PATH = "D:\\Android\\Sdk\\ndk\\26.1.10909125";
    private const string JDK_PATH = "E:\\java\\jdk-17";

    [MenuItem("Meow Runner/Android 环境/Set Android SDK/NDK/JDK From Script")]
    public static void ApplyPaths()
    {
        int set = 0;

#if UNITY_2023_2_OR_NEWER || UNITY_6000_0_OR_NEWER
        try
        {
            var android = System.Type.GetType("UnityEditor.Android.AndroidExternalToolsSettings, UnityEditor.Android.Extensions");
            if (android != null)
            {
                var sdkProp = android.GetProperty("sdkRootPath");
                var ndkProp = android.GetProperty("ndkRootPath");
                var jdkProp = android.GetProperty("jdkRootPath");
                if (sdkProp != null && !string.IsNullOrEmpty(SDK_PATH)) { sdkProp.SetValue(null, SDK_PATH); set++; }
                if (ndkProp != null && !string.IsNullOrEmpty(NDK_PATH)) { ndkProp.SetValue(null, NDK_PATH); set++; }
                if (jdkProp != null && !string.IsNullOrEmpty(JDK_PATH)) { jdkProp.SetValue(null, JDK_PATH); set++; }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("AndroidExternalToolsSettings failed: " + e.Message);
        }
#endif

        if (set == 0)
        {
            EditorPrefs.SetString("AndroidSdkRoot", SDK_PATH);
            EditorPrefs.SetString("AndroidNdkRoot", NDK_PATH);
            EditorPrefs.SetString("JdkPath", JDK_PATH);
            set = 3;
        }

        Debug.Log("Meow Runner: 已写入 Android 路径（SDK/NDK/JDK）。请到 Edit > Preferences > External Tools 确认。若 Build 仍报错，请在该页手动核对路径。");
    }

    [MenuItem("Meow Runner/Android 环境/Open External Tools (Android 路径页)")]
    public static void OpenExternalTools()
    {
        SettingsService.OpenUserPreferences("Preferences/External Tools");
    }
}
#endif
