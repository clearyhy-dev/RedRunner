#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using RedRunner.Gameplay.Player;
using RedRunner.Collectables;

/// <summary>
/// 只保留 Play、Creation 场景并写入 Build Settings。
/// 菜单：Meow Runner > Setup Scenes for Build
/// </summary>
public static class MeowRunnerSceneSetup
{
    private const string ScenesPath = "Assets/Scenes";

    [MenuItem("Meow Runner/Setup Scenes for Build")]
    public static void SetupScenes()
    {
        if (!Directory.Exists(ScenesPath))
        {
            Directory.CreateDirectory(ScenesPath);
            AssetDatabase.Refresh();
        }

        var allScenes = new List<EditorBuildSettingsScene>();
        string[] buildOrder = { "Play", "Creation" };
        foreach (string name in buildOrder)
        {
            string path = Path.Combine(ScenesPath, name + ".unity").Replace("\\", "/");
            if (File.Exists(path))
                allScenes.Add(new EditorBuildSettingsScene(path, true));
            else
                Debug.LogWarning("场景不存在，已跳过: " + path);
        }

        EditorBuildSettings.scenes = allScenes.ToArray();
        AssetDatabase.SaveAssets();
        Debug.Log("Meow Runner: Build 已更新，仅包含 Play、Creation。");
    }

    [MenuItem("Meow Runner/Set Player Product Name to Meow Runner")]
    public static void SetProductName()
    {
        PlayerSettings.productName = "Meow Runner";
        Debug.Log("Product Name set to 'Meow Runner'. App name on device will update after next build.");
    }

    private const string PlayScenePath = "Assets/Scenes/Play.unity";

    [MenuItem("Meow Runner/Setup Play Scene for Mobile (Auto-Run + Tap Jump + Screen Adapter)")]
    public static void SetupPlayForMobile()
    {
        OpenPlayAndAddMobileSetup(addRunJump: true, addScreenAdapter: true);
    }

    private static void OpenPlayAndAddMobileSetup(bool addRunJump, bool addScreenAdapter)
    {
        if (!File.Exists(PlayScenePath))
        {
            Debug.LogWarning("Play scene not found at " + PlayScenePath);
            return;
        }
        var scene = EditorSceneManager.OpenScene(PlayScenePath);
        bool changed = false;

        if (addRunJump)
        {
            var existingInput = Object.FindFirstObjectByType<MobileRunJumpInput>();
            if (existingInput == null)
            {
                var gm = Object.FindFirstObjectByType<RedRunner.GameManager>();
                if (gm != null)
                {
                    gm.gameObject.AddComponent<MobileRunJumpInput>();
                    changed = true;
                    Debug.Log("Added MobileRunJumpInput to GameManager. On phone: cat auto-runs, tap to jump.");
                }
                else
                {
                    var go = new GameObject("MobileRunJumpInput");
                    go.AddComponent<MobileRunJumpInput>();
                    changed = true;
                }
            }
        }

        if (addScreenAdapter)
        {
            var cam = Object.FindFirstObjectByType<Camera>();
            if (cam != null && cam.orthographic)
            {
                var existingAdapter = cam.GetComponent<MobileScreenAdapter>();
                if (existingAdapter == null)
                {
                    cam.gameObject.AddComponent<MobileScreenAdapter>();
                    changed = true;
                    Debug.Log("Added MobileScreenAdapter to Camera. Screen will scale for different phone sizes.");
                }
            }
        }

        var gmForFish = Object.FindFirstObjectByType<RedRunner.GameManager>();
        if (gmForFish != null && gmForFish.GetComponent<ApplyFishSpriteAtRuntime>() == null)
        {
            gmForFish.gameObject.AddComponent<ApplyFishSpriteAtRuntime>();
            changed = true;
            Debug.Log("Added ApplyFishSpriteAtRuntime. Put Fish.png in Assets/Resources for runtime fish sprite.");
        }

        if (changed)
            EditorSceneManager.SaveScene(scene);
    }

    [MenuItem("Meow Runner/小鱼显示一键设置 (Copy to Resources + Apply in Play)")]
    public static void OneClickFishSetup()
    {
        string resourcesDir = Path.Combine(Application.dataPath, "Resources").Replace("\\", "/");
        if (!Directory.Exists(resourcesDir))
        {
            Directory.CreateDirectory(resourcesDir);
            AssetDatabase.Refresh();
        }
        string destFull = Path.Combine(Application.dataPath, "Resources", "Fish.png").Replace("\\", "/");
        string[] sources = {
            Path.Combine(Application.dataPath, "Art/Collectibles/fish_collectible.png").Replace("\\", "/"),
            Path.Combine(Application.dataPath, "Art/Characters/fish.png").Replace("\\", "/"),
            Path.Combine(Application.dataPath, "Art/Collectibles/fish.png").Replace("\\", "/"),
        };
        bool copied = false;
        foreach (var src in sources)
        {
            if (File.Exists(src))
            {
                File.Copy(src, destFull, true);
                copied = true;
                break;
            }
        }
        AssetDatabase.Refresh();
        string resourceAssetPath = "Assets/Resources/Fish.png";
        if (copied && File.Exists(Path.Combine(Application.dataPath, "Resources", "Fish.png")))
        {
            var importer = AssetImporter.GetAtPath(resourceAssetPath) as UnityEditor.TextureImporter;
            if (importer != null && importer.textureType != UnityEditor.TextureImporterType.Sprite)
            {
                importer.textureType = UnityEditor.TextureImporterType.Sprite;
                importer.SaveAndReimport();
            }
        }
        if (!copied)
        {
            Debug.LogWarning("Meow Runner: No fish image in Art/Collectibles or Art/Characters. Add fish.png or fish_collectible.png first.");
            return;
        }
        if (!File.Exists(PlayScenePath))
        {
            Debug.Log("Meow Runner: Fish copied to Resources. Open Play scene and run this menu again to apply in scene.");
            return;
        }
        EditorSceneManager.OpenScene(PlayScenePath);
        ApplyCollectiblesArt.Apply();
        Debug.Log("Meow Runner: 小鱼已复制到 Resources 并应用到 Play 场景。请保存场景后重新运行或打包。");
    }
}
#endif
