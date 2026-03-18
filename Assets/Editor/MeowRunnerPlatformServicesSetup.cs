#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using GooglePlayGames.Editor;
using Services.Config;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>
/// Keeps Android package name, scripting defines, GPGS resources and AdMob manifest
/// in sync with Resources/Configs/AndroidPlatformServicesConfig.json.
/// </summary>
public sealed class MeowRunnerPlatformServicesSetup : IPreprocessBuildWithReport
{
    private const string ConfigPath = "Assets/Resources/Configs/AndroidPlatformServicesConfig.json";
    private const string MainManifestPath = "Assets/Plugins/Android/AndroidManifest.xml";
    private const string UseGpgsDefine = "USE_GPGS";
    private const string UseAdMobDefine = "USE_ADMOB";

    public int callbackOrder => 0;

    [MenuItem("Meow Runner/Platform Services/Apply Android Services Config", false, 140)]
    public static void ApplyAndroidServicesConfigMenu()
    {
        ApplyAll(logToConsole: true);
    }

    [MenuItem("Meow Runner/Platform Services/Open Services Config", false, 141)]
    public static void OpenServicesConfig()
    {
        EnsureConfigFileExists();
        Selection.activeObject = AssetDatabase.LoadAssetAtPath<TextAsset>(ConfigPath);
    }

    public void OnPreprocessBuild(BuildReport report)
    {
        if (report.summary.platform == BuildTarget.Android)
            ApplyAll(logToConsole: false);
    }

    private static void ApplyAll(bool logToConsole)
    {
        EnsureConfigFileExists();
        var config = LoadConfig();
        if (config == null)
        {
            Debug.LogWarning("[PlatformServices] Unable to load AndroidPlatformServicesConfig.json.");
            return;
        }

        ApplyPackageName(config, logToConsole);
        ApplyGooglePlayGames(config, logToConsole);
        ApplyAdMob(config, logToConsole);
        AssetDatabase.Refresh();
    }

    private static AndroidPlatformServicesConfigData LoadConfig()
    {
        if (!File.Exists(ConfigPath))
            return AndroidPlatformServicesConfig.CreateDefault();

        try
        {
            string json = File.ReadAllText(ConfigPath);
            if (string.IsNullOrWhiteSpace(json))
                return AndroidPlatformServicesConfig.CreateDefault();

            return JsonUtility.FromJson<AndroidPlatformServicesConfigData>(json) ?? AndroidPlatformServicesConfig.CreateDefault();
        }
        catch (Exception ex)
        {
            Debug.LogWarning("[PlatformServices] Failed to read config json: " + ex.Message);
            return AndroidPlatformServicesConfig.CreateDefault();
        }
    }

    private static void EnsureConfigFileExists()
    {
        if (File.Exists(ConfigPath))
            return;

        string directory = Path.GetDirectoryName(ConfigPath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            Directory.CreateDirectory(directory);

        File.WriteAllText(ConfigPath, AndroidPlatformServicesConfig.CreateDefaultJson());
        AssetDatabase.Refresh();
    }

    private static void ApplyPackageName(AndroidPlatformServicesConfigData config, bool logToConsole)
    {
        if (string.IsNullOrWhiteSpace(config.packageName))
            return;

        SetAndroidApplicationIdentifier(config.packageName.Trim());
        if (logToConsole)
            Debug.Log("[PlatformServices] Android package name set to " + config.packageName.Trim());
    }

    private static void ApplyGooglePlayGames(AndroidPlatformServicesConfigData config, bool logToConsole)
    {
        bool enable = config.googlePlayGames != null && config.googlePlayGames.enableGooglePlayGames;
        if (!enable)
        {
            SetDefineEnabled(UseGpgsDefine, false);
            if (logToConsole)
                Debug.Log("[PlatformServices] Google Play Games disabled by config.");
            return;
        }

        string appId = config.googlePlayGames.appId?.Trim() ?? string.Empty;
        string webClientId = config.googlePlayGames.webClientId?.Trim() ?? string.Empty;
        string resourceXmlPath = ResolveConfigPath(config.googlePlayGames.resourceXmlPath);

        bool setupSucceeded = false;
        try
        {
            if (!string.IsNullOrWhiteSpace(resourceXmlPath) && File.Exists(resourceXmlPath))
            {
                string resourceXml = File.ReadAllText(resourceXmlPath);
                setupSucceeded = GPGSAndroidSetupUI.PerformSetup(webClientId, "Assets", "GPGSIds", resourceXml, null);
            }
            else if (!string.IsNullOrWhiteSpace(appId))
            {
                setupSucceeded = GPGSAndroidSetupUI.PerformSetup(webClientId, appId, null);
            }
            else
            {
                Debug.LogWarning("[PlatformServices] Google Play Games is enabled, but appId/resourceXmlPath is empty.");
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning("[PlatformServices] Google Play Games setup failed: " + ex.Message);
        }

        SetDefineEnabled(UseGpgsDefine, setupSucceeded);
        if (logToConsole)
        {
            Debug.Log(setupSucceeded
                ? "[PlatformServices] Google Play Games resources generated and USE_GPGS enabled."
                : "[PlatformServices] Google Play Games setup not completed. USE_GPGS was disabled to avoid broken runtime auth.");
        }
    }

    private static void ApplyAdMob(AndroidPlatformServicesConfigData config, bool logToConsole)
    {
        bool enable = config.adMob != null && config.adMob.enableAdMob;
        SetDefineEnabled(UseAdMobDefine, enable);

        if (!enable)
        {
            if (HasGoogleMobileAdsSdkInstalled())
            {
                // 即使关闭业务广告，只要项目里仍安装了 Google Mobile Ads 包，
                // MobileAdsInitProvider 仍会在应用启动时检查 APPLICATION_ID。
                // 这里保留一个最小 manifest，避免 APK 启动即崩溃。
                SetCustomMainManifestEnabled(true);
                WriteAdMobManifest("ca-app-pub-3940256099942544~3347511713");
                if (logToConsole)
                    Debug.Log("[PlatformServices] AdMob business logic disabled, but minimal manifest kept to satisfy installed Google Mobile Ads SDK.");
                return;
            }

            SetCustomMainManifestEnabled(false);
            if (File.Exists(MainManifestPath))
                File.Delete(MainManifestPath);
            if (File.Exists(MainManifestPath + ".meta"))
                File.Delete(MainManifestPath + ".meta");
            if (logToConsole)
                Debug.Log("[PlatformServices] AdMob disabled by config, custom main manifest removed.");
            return;
        }

        string appId = GetAndroidAppId(config);
        if (string.IsNullOrWhiteSpace(appId))
        {
            Debug.LogWarning("[PlatformServices] AdMob is enabled, but androidAppId is empty.");
            return;
        }

        SetCustomMainManifestEnabled(true);
        WriteAdMobManifest(appId);
        if (logToConsole)
            Debug.Log("[PlatformServices] AdMob manifest updated and USE_ADMOB enabled.");
    }

    private static string GetAndroidAppId(AndroidPlatformServicesConfigData config)
    {
        if (config?.adMob == null)
            return string.Empty;

        return config.adMob.useTestAds
            ? "ca-app-pub-3940256099942544~3347511713"
            : (config.adMob.androidAppId ?? string.Empty).Trim();
    }

    private static string ResolveConfigPath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return string.Empty;

        if (Path.IsPathRooted(path))
            return path;

        string combined = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), path));
        return combined;
    }

    private static bool HasGoogleMobileAdsSdkInstalled()
    {
        if (File.Exists("Assets/GoogleMobileAds/link.xml"))
            return true;

        if (!File.Exists("Packages/manifest.json"))
            return false;

        try
        {
            string manifestText = File.ReadAllText("Packages/manifest.json");
            return manifestText.Contains("com.google.ads.mobile", StringComparison.Ordinal);
        }
        catch
        {
            return false;
        }
    }

    private static void WriteAdMobManifest(string appId)
    {
        string manifestContent =
            "<?xml version=\"1.0\" encoding=\"utf-8\"?>\n" +
            "<manifest xmlns:android=\"http://schemas.android.com/apk/res/android\">\n" +
            "  <uses-permission android:name=\"android.permission.INTERNET\" />\n" +
            "  <application>\n" +
            "    <activity android:name=\"com.unity3d.player.UnityPlayerActivity\"\n" +
            "              android:theme=\"@style/UnityThemeSelector\"\n" +
            "              android:exported=\"true\"\n" +
            "              android:launchMode=\"singleTask\"\n" +
            "              android:configChanges=\"orientation|keyboardHidden|keyboard|screenSize|smallestScreenSize|screenLayout|uiMode|locale|layoutDirection|fontScale|density\"\n" +
            "              android:screenOrientation=\"fullUser\">\n" +
            "      <intent-filter>\n" +
            "        <action android:name=\"android.intent.action.MAIN\" />\n" +
            "        <category android:name=\"android.intent.category.LAUNCHER\" />\n" +
            "      </intent-filter>\n" +
            "      <meta-data android:name=\"unityplayer.UnityActivity\" android:value=\"true\" />\n" +
            "    </activity>\n" +
            "    <meta-data android:name=\"com.google.android.gms.ads.APPLICATION_ID\" android:value=\"" + appId + "\" />\n" +
            "  </application>\n" +
            "</manifest>\n";

        File.WriteAllText(MainManifestPath, manifestContent);
    }

    private static void SetCustomMainManifestEnabled(bool enabled)
    {
        var androidSettingsType = typeof(PlayerSettings).GetNestedType("Android", BindingFlags.Public);
        if (androidSettingsType == null)
            return;

        var property = androidSettingsType.GetProperty("useCustomMainManifest", BindingFlags.Public | BindingFlags.Static);
        if (property == null || !property.CanWrite)
            return;

        property.SetValue(null, enabled, null);
    }

    private static void SetAndroidApplicationIdentifier(string packageName)
    {
#if UNITY_2021_2_OR_NEWER
        PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, packageName);
#else
        PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, packageName);
#endif
    }

    private static void SetDefineEnabled(string define, bool enabled)
    {
        var defines = GetAndroidDefines();
        bool changed = enabled ? defines.Add(define) : defines.Remove(define);
        if (!changed)
            return;

        SetAndroidDefines(defines);
    }

    private static HashSet<string> GetAndroidDefines()
    {
        string raw;
#if UNITY_2021_2_OR_NEWER
        raw = PlayerSettings.GetScriptingDefineSymbols(NamedBuildTarget.Android) ?? string.Empty;
#else
        raw = PlayerSettings.GetScriptingDefineSymbolsForGroup(BuildTargetGroup.Android) ?? string.Empty;
#endif

        return new HashSet<string>(
            raw.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()),
            StringComparer.Ordinal);
    }

    private static void SetAndroidDefines(HashSet<string> defines)
    {
        string joined = string.Join(";", defines.Where(s => !string.IsNullOrWhiteSpace(s)).OrderBy(s => s));
#if UNITY_2021_2_OR_NEWER
        PlayerSettings.SetScriptingDefineSymbols(NamedBuildTarget.Android, joined);
#else
        PlayerSettings.SetScriptingDefineSymbolsForGroup(BuildTargetGroup.Android, joined);
#endif
    }
}
#endif
