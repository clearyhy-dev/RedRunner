#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using RedRunner.Collectables;
using RedRunner.Characters;

/// <summary>
/// 一键把已生成的猫咪图、小鱼图赋给 Play 场景中的角色与收集物。
/// 菜单：Meow Runner > Apply Cat & Fish Sprites (Open Play Scene First).
/// </summary>
public static class ApplyMeowRunnerSprites
{
    private const string FishSpritePath = "Assets/Art/Collectibles/fish_collectible.png";
    private const string CatSpritePath = "Assets/Art/Characters/cat_character.png";
    private const string PlayScenePath = "Assets/Scenes/Play.unity";

    public static void ApplySprites()
    {
        Sprite fishSprite = AssetDatabase.LoadAssetAtPath<Sprite>(FishSpritePath);
        if (fishSprite == null)
        {
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(FishSpritePath);
            if (tex != null)
                fishSprite = AssetDatabase.LoadAllAssetsAtPath(FishSpritePath).Length > 1
                    ? (Sprite)AssetDatabase.LoadAllAssetsAtPath(FishSpritePath)[1]
                    : Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
        }
        if (fishSprite == null)
        {
            Debug.LogError("Meow Runner: Fish sprite not found at " + FishSpritePath + ". Import the image and set Texture Type to Sprite (2D and UI).");
            return;
        }

        Sprite catSprite = AssetDatabase.LoadAssetAtPath<Sprite>(CatSpritePath);
        if (catSprite == null)
        {
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(CatSpritePath);
            if (tex != null)
                catSprite = AssetDatabase.LoadAllAssetsAtPath(CatSpritePath).Length > 1
                    ? (Sprite)AssetDatabase.LoadAllAssetsAtPath(CatSpritePath)[1]
                    : Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
        }
        if (catSprite == null)
        {
            Debug.LogError("Meow Runner: Cat sprite not found at " + CatSpritePath + ". Import the image and set Texture Type to Sprite (2D and UI).");
            return;
        }

        if (!UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene().path.Replace("\\", "/").EndsWith("Play.unity"))
        {
            if (System.IO.File.Exists(PlayScenePath))
            {
                EditorSceneManager.OpenScene(PlayScenePath);
            }
            else
            {
                Debug.LogWarning("Meow Runner: Open the Play scene first, then run this menu again.");
                return;
            }
        }

        int fishCount = 0;
        int catCount = 0;

        var coins = Object.FindObjectsByType<Coin>(FindObjectsSortMode.None);
        foreach (var coin in coins)
        {
            var sr = coin.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                sr.sprite = fishSprite;
                fishCount++;
                EditorUtility.SetDirty(coin);
            }
        }

        var coinRbs = Object.FindObjectsByType<CoinRigidbody2D>(FindObjectsSortMode.None);
        foreach (var c in coinRbs)
        {
            var sr = c.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                sr.sprite = fishSprite;
                fishCount++;
                EditorUtility.SetDirty(c);
            }
        }

        var redChar = Object.FindFirstObjectByType<RedCharacter>();
        if (redChar != null)
        {
            var sr = redChar.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                sr.sprite = catSprite;
                catCount++;
                EditorUtility.SetDirty(redChar);
            }
            else
            {
                foreach (var childSr in redChar.GetComponentsInChildren<SpriteRenderer>(true))
                {
                    childSr.sprite = catSprite;
                    catCount++;
                    EditorUtility.SetDirty(childSr);
                    break;
                }
            }
        }

        var uiCoinImages = Object.FindObjectsByType<RedRunner.UI.UICoinImage>(FindObjectsSortMode.None);
        foreach (var img in uiCoinImages)
        {
            img.sprite = fishSprite;
            fishCount++;
            EditorUtility.SetDirty(img);
        }

        EditorSceneManager.SaveOpenScenes();
        AssetDatabase.SaveAssets();
        Debug.Log(string.Format("Meow Runner: Applied fish sprite to {0} object(s), cat sprite to {1} object(s). Save the scene if needed.", fishCount, catCount));
    }
}
#endif
