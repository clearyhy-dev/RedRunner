#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using RedRunner.Collectables;
using RedRunner.Characters;
using RedRunner.Gameplay.Player;

/// <summary>
/// 扫描 Assets/Art/Collectibles/ 下的图片，按文件名放入对应位置：
/// - 含 fish/小鱼 → 收集物与 HUD 小鱼图标
/// - run + 数字(1,2,3,4) → 猫咪跑步序列
/// - jump + 数字 → 猫咪跳跃
/// - hurt + 数字 → 猫咪受伤/死亡
/// 菜单：Meow Runner > Apply Art from Collectibles Folder (Open Play First).
/// </summary>
public static class ApplyCollectiblesArt
{
    private const string CollectiblesPath = "Assets/Art/Collectibles";
    private const string CharactersPath = "Assets/Art/Characters";
    private const string PlayScenePath = "Assets/Scenes/Play.unity";

    public static void Apply()
    {
        var collectiblesFiles = ListArtFiles(CollectiblesPath);
        var charactersFiles = ListArtFiles(CharactersPath);

        Sprite fishSprite = null;
        var runList = new List<(int index, Sprite sprite)>();
        var jumpList = new List<(int index, Sprite sprite)>();
        var hurtList = new List<(int index, Sprite sprite)>();

        foreach (var (name, path) in collectiblesFiles)
        {
            string lower = name.ToLowerInvariant();
            if (lower.Contains("fish") || lower.Contains("小鱼"))
            {
                var s = LoadSprite(path);
                if (s != null) fishSprite = s;
                continue;
            }
            if (MatchNumbered(lower, "run", out int rn)) { var s = LoadSprite(path); if (s != null) AddSorted(runList, rn, s); continue; }
            if (MatchNumbered(lower, "jump", out int jn)) { var s = LoadSprite(path); if (s != null) AddSorted(jumpList, jn, s); continue; }
            if (MatchNumbered(lower, "hurt", out int hn)) { var s = LoadSprite(path); if (s != null) AddSorted(hurtList, hn, s); }
        }

        foreach (var (name, path) in charactersFiles)
        {
            string lower = name.ToLowerInvariant();
            if (lower.Contains("fish") || lower.Contains("小鱼")) { var s = LoadSprite(path); if (s != null) fishSprite = s; continue; }
            if (MatchNumbered(lower, "run", out int rn)) { var s = LoadSprite(path); if (s != null) AddSorted(runList, rn, s); continue; }
            if (MatchNumbered(lower, "jump", out int jn)) { var s = LoadSprite(path); if (s != null) AddSorted(jumpList, jn, s); continue; }
            if (MatchNumbered(lower, "hurt", out int hn)) { var s = LoadSprite(path); if (s != null) AddSorted(hurtList, hn, s); }
        }

        var runSprites = ToOrderedSpriteList(runList);
        var jumpSprites = ToOrderedSpriteList(jumpList);
        var hurtSprites = ToOrderedSpriteList(hurtList);

        EnsurePlaySceneOpen();

        int fishCount = 0;

        if (fishSprite != null)
        {
            foreach (var c in Object.FindObjectsByType<Coin>(FindObjectsSortMode.None))
            {
                var sr = c.GetComponentInChildren<SpriteRenderer>(true);
                if (sr != null) { sr.sprite = fishSprite; fishCount++; EditorUtility.SetDirty(c); }
            }
            foreach (var c in Object.FindObjectsByType<CoinRigidbody2D>(FindObjectsSortMode.None))
            {
                var sr = c.GetComponentInChildren<SpriteRenderer>(true);
                if (sr != null) { sr.sprite = fishSprite; fishCount++; EditorUtility.SetDirty(c); }
            }
            foreach (var img in Object.FindObjectsByType<RedRunner.UI.UICoinImage>(FindObjectsSortMode.None))
            {
                img.sprite = fishSprite;
                fishCount++;
                EditorUtility.SetDirty(img);
            }
        }

        int catCount = 0;
        var redChar = Object.FindFirstObjectByType<RedCharacter>();
        if (redChar != null && (runSprites.Count > 0 || jumpSprites.Count > 0 || hurtSprites.Count > 0))
        {
            SpriteRenderer bodySr = redChar.GetComponent<SpriteRenderer>();
            if (bodySr == null)
            {
                foreach (var sr in redChar.GetComponentsInChildren<SpriteRenderer>(true))
                {
                    bodySr = sr;
                    break;
                }
            }
            if (bodySr != null)
            {
                var switcher = bodySr.GetComponent<CatSpriteSwitcher>();
                if (switcher == null)
                    switcher = bodySr.gameObject.AddComponent<CatSpriteSwitcher>();
                var so = new SerializedObject(switcher);
                var anim = redChar.GetComponent<Animator>();
                if (anim != null)
                    so.FindProperty("m_Animator").objectReferenceValue = anim;
                so.FindProperty("RunSprites").arraySize = runSprites.Count;
                for (int i = 0; i < runSprites.Count; i++)
                    so.FindProperty("RunSprites").GetArrayElementAtIndex(i).objectReferenceValue = runSprites[i];
                so.FindProperty("JumpSprites").arraySize = jumpSprites.Count;
                for (int i = 0; i < jumpSprites.Count; i++)
                    so.FindProperty("JumpSprites").GetArrayElementAtIndex(i).objectReferenceValue = jumpSprites[i];
                so.FindProperty("HurtSprites").arraySize = hurtSprites.Count;
                for (int i = 0; i < hurtSprites.Count; i++)
                    so.FindProperty("HurtSprites").GetArrayElementAtIndex(i).objectReferenceValue = hurtSprites[i];
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(switcher);
                catCount = runSprites.Count + jumpSprites.Count + hurtSprites.Count;
            }
        }

        EditorSceneManager.SaveOpenScenes();
        AssetDatabase.SaveAssets();
        Debug.Log(string.Format("Meow Runner Art: fish assigned to {0} object(s). Cat run={1} jump={2} hurt={3} sprites.", fishCount, runSprites.Count, jumpSprites.Count, hurtSprites.Count));
    }

    private static List<(string, string)> ListArtFiles(string folder)
    {
        var list = new List<(string, string)>();
        if (!Directory.Exists(folder)) return list;
        foreach (var ext in new[] { "*.png", "*.jpg", "*.jpeg" })
        {
            foreach (var f in Directory.GetFiles(folder, ext, SearchOption.TopDirectoryOnly))
            {
                var name = Path.GetFileName(f);
                if (string.IsNullOrEmpty(name) || name.EndsWith(".meta")) continue;
                list.Add((name, folder + "/" + name));
            }
        }
        return list;
    }

    private static bool MatchNumbered(string lowerName, string tag, out int number)
    {
        number = 0;
        if (!lowerName.Contains(tag)) return false;
        var m = Regex.Match(lowerName, @"(\d+)");
        if (m.Success) number = int.Parse(m.Groups[1].Value);
        else number = 1;
        return true;
    }

    private static void AddSorted(List<(int index, Sprite sprite)> list, int index, Sprite s)
    {
        list.Add((index, s));
    }

    private static List<Sprite> ToOrderedSpriteList(List<(int index, Sprite sprite)> list)
    {
        return list.OrderBy(x => x.index).Select(x => x.sprite).ToList();
    }

    private static Sprite LoadSprite(string path)
    {
        path = path.Replace("\\", "/");
        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (sprite != null) return sprite;
        var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        if (tex != null)
        {
            var all = AssetDatabase.LoadAllAssetsAtPath(path);
            foreach (var a in all)
                if (a is Sprite sp) return sp;
            return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
        }
        return null;
    }

    private static void EnsurePlaySceneOpen()
    {
        var active = EditorSceneManager.GetActiveScene().path.Replace("\\", "/");
        if (!active.EndsWith("Play.unity") && File.Exists(PlayScenePath))
            EditorSceneManager.OpenScene(PlayScenePath);
    }
}
#endif
