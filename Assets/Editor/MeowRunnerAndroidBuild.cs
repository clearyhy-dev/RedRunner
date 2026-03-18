#if UNITY_EDITOR
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.Build.Reporting;

/// <summary>
/// 打包安卓并可选安装到手机。菜单：Meow Runner > Build Android (APK) / Build Android & Install。
/// </summary>
public static class MeowRunnerAndroidBuild
{
    private const string BuildFolderName = "Builds";
    private const string PlayScenePath = "Assets/Scenes/Play.unity";

    [MenuItem("Meow Runner/Build Android (APK)", false, 100)]
    public static void BuildAndroidApk()
    {
        BuildAndroid(installToDevice: false);
    }

    [MenuItem("Meow Runner/Build Android & Install", false, 101)]
    public static void BuildAndroidAndInstall()
    {
        BuildAndroid(installToDevice: true);
    }

    [MenuItem("Meow Runner/Open Build Settings", false, 110)]
    public static void OpenBuildSettings()
    {
        EditorApplication.ExecuteMenuItem("File/Build Settings");
    }

    /// <summary>与 Google Play 上的项目名一致，打出来的应用在手机/商店显示为 "Hungry Kitty: Fish Run"。</summary>
    [MenuItem("Meow Runner/设置 Google Play 应用名 (Hungry Kitty: Fish Run)", false, 120)]
    public static void SetGooglePlayProductName()
    {
        const string name = "Hungry Kitty: Fish Run";
        PlayerSettings.productName = name;
        Debug.Log("已设置 Product Name 为: " + name + "。可在 Edit > Project Settings > Player 中查看或修改。");
    }

    private static void BuildAndroid(bool installToDevice)
    {
        if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
        {
            if (!EditorUtility.DisplayDialog("切换平台", "当前不是 Android 平台，需要先切换到 Android 再打包。是否现在切换？（会花一些时间）", "切换并继续", "取消"))
                return;
            EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);
        }

        EnsurePlaySceneInBuild();

        string productName = Application.productName;
        if (string.IsNullOrEmpty(productName)) productName = "Hungry Kitty Fish Run";
        productName = SanitizeFileName(productName);
        string dir = Path.Combine(Path.GetDirectoryName(Application.dataPath), BuildFolderName);
        if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
        string fileName = productName + "-" + PlayerSettings.bundleVersion + ".apk";
        string path = Path.Combine(dir, fileName);

        var options = BuildOptions.None;
        var report = BuildPipeline.BuildPlayer(GetScenePaths(), path, BuildTarget.Android, options);

        if (report.summary.result != BuildResult.Succeeded)
        {
            EditorUtility.DisplayDialog("打包失败", "请查看 Console 中的错误信息。\n" + report.summary.result, "确定");
            return;
        }

        Debug.Log("Android 包已生成: " + path);
        if (installToDevice)
            InstallApk(path);
        else
            EditorUtility.RevealInFinder(path);
    }

    private static void EnsurePlaySceneInBuild()
    {
        string dataDir = Path.GetDirectoryName(Application.dataPath);
        string playFull = Path.Combine(dataDir, PlayScenePath.Replace("/", Path.DirectorySeparatorChar.ToString()));
        if (!File.Exists(playFull))
            return;
        var scenes = new System.Collections.Generic.List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        bool hasPlay = false;
        foreach (var s in scenes)
            if (s.path != null && s.path.EndsWith("Play.unity", System.StringComparison.OrdinalIgnoreCase)) { hasPlay = true; break; }
        if (!hasPlay)
        {
            scenes.Add(new EditorBuildSettingsScene(PlayScenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
            Debug.Log("已将 Play 场景加入 Build Settings。");
        }
    }

    private static string[] GetScenePaths()
    {
        var list = new System.Collections.Generic.List<string>();
        foreach (var s in EditorBuildSettings.scenes)
            if (s.enabled && !string.IsNullOrEmpty(s.path))
                list.Add(s.path);
        string dataDir = Path.GetDirectoryName(Application.dataPath);
        if (list.Count == 0 && File.Exists(Path.Combine(dataDir, PlayScenePath.Replace("/", Path.DirectorySeparatorChar.ToString()))))
            list.Add(PlayScenePath);
        return list.ToArray();
    }

    private static void InstallApk(string apkPath)
    {
        string adb = "adb";
        string args = "install -r \"" + apkPath + "\"";
        var startInfo = new System.Diagnostics.ProcessStartInfo
        {
            FileName = adb,
            Arguments = args,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };
        try
        {
            using (var p = System.Diagnostics.Process.Start(startInfo))
            {
                string stdout = p.StandardOutput.ReadToEnd();
                string stderr = p.StandardError.ReadToEnd();
                p.WaitForExit(60000);
                if (p.ExitCode == 0)
                {
                    Debug.Log("已安装到手机: " + apkPath);
                    EditorUtility.DisplayDialog("安装完成", "APK 已安装到已连接的设备。", "确定");
                }
                else
                {
                    Debug.LogError("adb install 失败: " + stderr + "\n" + stdout);
                    EditorUtility.DisplayDialog("安装失败", "请确认：\n1. 手机已用 USB 连接并开启 USB 调试\n2. 已安装 Android SDK platform-tools（含 adb）\n3. 控制台中有 adb 报错详情", "确定");
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError("执行 adb 失败: " + e.Message);
            EditorUtility.DisplayDialog("安装失败", "未找到 adb 或执行失败。请安装 Android SDK 并将 platform-tools 加入系统 PATH。\n" + e.Message, "确定");
        }
    }

    /// <summary>Windows 文件名不允许包含 : * ? 等字符，打包前统一替换，避免生成异常路径。</summary>
    private static string SanitizeFileName(string value)
    {
        if (string.IsNullOrEmpty(value))
            return "Meow Runner";

        foreach (char c in Path.GetInvalidFileNameChars())
            value = value.Replace(c, '_');

        return value.Trim();
    }
}
#endif
