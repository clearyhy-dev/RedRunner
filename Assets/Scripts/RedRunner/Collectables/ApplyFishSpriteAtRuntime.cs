using UnityEngine;
using RedRunner.UI;

namespace RedRunner.Collectables
{
    /// <summary>
    /// 运行时从 Resources 加载小鱼图并替换所有金币显示为小鱼（收集物 + HUD 图标）。
    /// 编辑器 Play 时若 Resources 没有，会从 Assets/Art 加载小鱼并应用。
    /// </summary>
    public class ApplyFishSpriteAtRuntime : MonoBehaviour
    {
        [Tooltip("Resources 下的小鱼图名称（不含后缀），如 Fish 或 fish")]
        public string fishSpriteName = "Fish";

        private static bool s_Applied;

        private void Start()
        {
            ApplyNow();
        }

        /// <summary>由 GameManager 或任意处调用，立即加载小鱼图并应用到所有收集物与 HUD。</summary>
        public static void ApplyNow()
        {
            if (s_Applied) return;
            var sprite = LoadFishSpriteStatic();
#if UNITY_EDITOR
            if (sprite == null) sprite = LoadFishFromArtInEditor();
#endif
            if (sprite == null) return;
            ApplySpriteToAllStatic(sprite);
        }

        private static Sprite LoadFishSpriteStatic()
        {
            var s = Resources.Load<Sprite>("Fish");
            if (s != null) return s;
            s = Resources.Load<Sprite>("fish");
            if (s != null) return s;
            return Resources.Load<Sprite>("fish_collectible");
        }

        /// <summary>供 Coin、UICoinImage 等调用，获取小鱼图（Resources 或编辑器下 Art）。</summary>
        public static Sprite GetFishSprite()
        {
            var s = LoadFishSpriteStatic();
#if UNITY_EDITOR
            if (s == null) s = LoadFishFromArtInEditor();
#endif
            return s;
        }

        private Sprite LoadFishSprite()
        {
            var s = Resources.Load<Sprite>(fishSpriteName);
            return s != null ? s : LoadFishSpriteStatic();
        }

#if UNITY_EDITOR
        private static Sprite LoadFishFromArtInEditor()
        {
            var s = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Collectibles/fish_collectible.png");
            if (s != null) return s;
            s = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Characters/fish.png");
            if (s != null) return s;
            return UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Collectibles/fish.png");
        }
#endif

        private void ApplySpriteToAll(Sprite sprite)
        {
            ApplySpriteToAllStatic(sprite);
        }

        private static void ApplySpriteToAllStatic(Sprite sprite)
        {
            int n = 0;
            foreach (var c in Object.FindObjectsByType<Coin>(FindObjectsSortMode.None))
            {
                var sr = c.GetComponentInChildren<SpriteRenderer>(true);
                if (sr != null) { sr.sprite = sprite; n++; }
            }
            foreach (var c in Object.FindObjectsByType<CoinRigidbody2D>(FindObjectsSortMode.None))
            {
                var sr = c.GetComponentInChildren<SpriteRenderer>(true);
                if (sr != null) { sr.sprite = sprite; n++; }
            }
            foreach (var img in Object.FindObjectsByType<UICoinImage>(FindObjectsSortMode.None))
            {
                if (img != null && img.gameObject != null && (img.gameObject.name.Contains("GoogleLogin") || img.gameObject.name.Contains("Share Google Plus") || img.gameObject.name.Contains("Google Plus")))
                    continue;
                img.sprite = sprite;
                n++;
            }
            if (n > 0)
            {
                s_Applied = true;
                Debug.Log("[ApplyFishSpriteAtRuntime] Applied fish sprite to " + n + " object(s).");
            }
        }

    }
}
