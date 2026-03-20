#if UNITY_EDITOR
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using RedRunner.UI;

/// <summary>
/// 一键把 Google 登录图标挂到 Start Screen 的 Google 按钮上，并设置样式与当前 UI 一致。
/// 菜单：Meow Runner > Tools > 登录与服务 > Setup Google Login Button (Apply Icon & Style)
/// </summary>
public static class MeowRunnerGoogleLoginSetup
{
    private const string IconFileName = "GoogleLoginButtonIcon";
    private const string IconSearchFilter = "GoogleLoginButtonIcon t:Texture2D";

    [MenuItem("Meow Runner/Tools/登录与服务/Setup Google Login Button (Apply Icon & Style)")]
    public static void SetupGoogleLoginButton()
    {
        if (!EditorSceneManager.GetActiveScene().name.Equals("Play"))
        {
            if (EditorUtility.DisplayDialog("打开 Play 场景", "需要先打开 Play 场景才能设置 Google 按钮。是否现在打开？", "打开", "取消"))
            {
                string playPath = "Assets/Scenes/Play.unity";
                if (File.Exists(playPath))
                    EditorSceneManager.OpenScene(playPath);
                else
                {
                    Debug.LogWarning("未找到 Play 场景：" + playPath);
                    return;
                }
            }
            else
                return;
        }

        StartScreen startScreen = Object.FindFirstObjectByType<StartScreen>();
        if (startScreen == null)
        {
            Debug.LogWarning("当前场景中未找到 StartScreen。请确认 Play 场景的 Canvas 下有 Start Screen。");
            return;
        }

        SerializedObject so = new SerializedObject(startScreen);
        SerializedProperty googleButtonProp = so.FindProperty("GoogleButton");
        Button googleButton = googleButtonProp?.objectReferenceValue as Button;
        if (googleButton == null)
        {
            var btn = startScreen.transform.Find("GoogleLoginButton");
            if (btn != null) googleButton = btn.GetComponent<Button>();
            if (googleButton == null)
            {
                Debug.LogWarning("StartScreen 的 Google Button 未绑定，且场景中未找到名为 GoogleLoginButton 的按钮。请在 Inspector 中把 Google 登录按钮拖到 Start Screen 的 Google Button 上。");
                Selection.activeGameObject = startScreen.gameObject;
                return;
            }
            googleButtonProp.objectReferenceValue = googleButton;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        Sprite sprite = FindGoogleIconSprite();
        if (sprite == null)
        {
            Debug.LogWarning("未找到图标 " + IconFileName + ".png。请将 GoogleLoginButtonIcon.png 放到 Assets/Art 或 Assets/Resources 下，或从项目根复制到 Assets 后重试。");
        }
        else
        {
            Image img = googleButton.targetGraphic as Image;
            if (img == null) img = googleButton.GetComponent<Image>();
            if (img != null)
            {
                img.sprite = sprite;
                img.preserveAspect = true;
                img.color = Color.white;
                img.raycastTarget = true;
                if (img.rectTransform != null)
                {
                    img.rectTransform.sizeDelta = new Vector2(80, 80);
                }
                Debug.Log("已把 Google 登录图标应用到按钮，并设置 Preserve Aspect、80x80 大小。");
            }

            SerializedProperty iconProp = so.FindProperty("GoogleButtonIcon");
            if (iconProp != null)
            {
                iconProp.objectReferenceValue = sprite;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Selection.activeGameObject = googleButton != null ? googleButton.gameObject : startScreen.gameObject;
    }

    private static Sprite FindGoogleIconSprite()
    {
        TryImportIconFromProjectRoot();

        string[] guids = AssetDatabase.FindAssets(IconSearchFilter);
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (tex != null)
            {
                EnsureSpriteImport(path);
                return AssetDatabase.LoadAssetAtPath<Sprite>(path);
            }
        }
        guids = AssetDatabase.FindAssets(IconFileName);
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Sprite s = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (s != null) return s;
            Texture2D t = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (t != null)
            {
                EnsureSpriteImport(path);
                return AssetDatabase.LoadAssetAtPath<Sprite>(path);
            }
        }
        return null;
    }

    private static void EnsureSpriteImport(string path)
    {
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer != null && importer.textureType != TextureImporterType.Sprite)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.SaveAndReimport();
        }
    }

    private static void TryImportIconFromProjectRoot()
    {
        string dataPath = Application.dataPath;
        string projectRoot = Path.GetDirectoryName(dataPath);
        string[] candidates = new[]
        {
            Path.Combine(projectRoot, "GoogleLoginButtonIcon.png"),
            Path.Combine(projectRoot, "assets", "GoogleLoginButtonIcon.png"),
            Path.Combine(dataPath, "Art", "GoogleLoginButtonIcon.png"),
            Path.Combine(dataPath, "Resources", "GoogleLoginButtonIcon.png"),
            Path.Combine(dataPath, "GoogleLoginButtonIcon.png")
        };
        string artDir = Path.Combine(dataPath, "Art");
        foreach (string full in candidates)
        {
            if (!File.Exists(full)) continue;
            string dest = Path.Combine(artDir, "GoogleLoginButtonIcon.png");
            if (!full.Replace("\\", "/").StartsWith(dataPath.Replace("\\", "/")))
            {
                if (!Directory.Exists(artDir)) Directory.CreateDirectory(artDir);
                try
                {
                    File.Copy(full, dest, true);
                    AssetDatabase.Refresh();
                }
                catch (System.Exception e) { Debug.LogWarning("Copy icon: " + e.Message); }
            }
            break;
        }
    }
}
#endif
