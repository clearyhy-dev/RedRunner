using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Core.Save;
using Services.Ads;

namespace UI.Shop
{
    /// <summary>
    /// Simple shop: default skin + 2–4 unlockable skins; select unlocked, unlock by fish or ad.
    /// </summary>
    public class UIShopController : MonoBehaviour
    {
        [SerializeField] private Button backButton;
        [SerializeField] private Transform skinContainer;
        [SerializeField] private GameObject skinItemPrefab;

        [Tooltip("Skin IDs. First is default and unlocked by default.")]
        [SerializeField] private string[] skinIds = { "default", "skin2", "skin3", "skin4" };
        [Tooltip("Cost in fish for each skin (index matches skinIds). 0 = default/unlocked, negative = unlock by watching ad.")]
        [SerializeField] private int[] skinCosts = { 0, 100, 200, -1 };

        private SaveData _save;

        void OnEnable()
        {
            AdsManager.ShowBanner();
        }

        void OnDisable()
        {
            AdsManager.HideBanner();
        }

        void Start()
        {
            _save = SaveManager.Load();
            if (backButton != null)
                backButton.onClick.AddListener(() => SceneManager.LoadScene("Play"));

            if (skinContainer != null && skinItemPrefab != null && skinIds != null)
            {
                for (int i = 0; i < skinIds.Length; i++)
                {
                    string id = skinIds[i];
                    int cost = (skinCosts != null && i < skinCosts.Length) ? skinCosts[i] : 0;
                    var item = Instantiate(skinItemPrefab, skinContainer);
                    SetupSkinItem(item, id, cost);
                }
            }
        }

        private void SetupSkinItem(GameObject item, string skinId, int cost)
        {
            var label = item.GetComponentInChildren<Text>();
            if (label != null)
                label.text = skinId;

            bool unlocked = _save.UnlockedSkinIds != null && _save.UnlockedSkinIds.Contains(skinId);
            bool selected = _save.SelectedSkinId == skinId;

            var selectBtn = item.GetComponentInChildren<Button>();
            if (selectBtn != null)
            {
                if (unlocked)
                {
                    var t = selectBtn.GetComponentInChildren<Text>();
                    if (t != null) t.text = selected ? "Selected" : "Select";
                    selectBtn.onClick.AddListener(() => SelectSkin(skinId));
                }
                else
                {
                    var t = selectBtn.GetComponentInChildren<Text>();
                    if (t != null) t.text = cost < 0 ? "Watch Ad" : (cost + " 小鱼");
                    selectBtn.onClick.AddListener(() => TryUnlock(skinId, cost));
                }
            }
        }

        private void SelectSkin(string skinId)
        {
            if (_save.UnlockedSkinIds == null || !_save.UnlockedSkinIds.Contains(skinId))
                return;
            _save.SelectedSkinId = skinId;
            SaveManager.SaveSelectedSkin(skinId);
        }

        private void TryUnlock(string skinId, int cost)
        {
            if (_save.UnlockedSkinIds != null && _save.UnlockedSkinIds.Contains(skinId))
            {
                SelectSkin(skinId);
                return;
            }
            if (cost > 0 && _save.TotalFish >= cost)
            {
                _save.TotalFish -= cost;
                if (_save.UnlockedSkinIds == null)
                    _save.UnlockedSkinIds = new List<string> { "default" };
                _save.UnlockedSkinIds.Add(skinId);
                SaveManager.Save(_save);
                SelectSkin(skinId);
            }
            else if (cost < 0)
            {
                if (!AdsManager.IsRewardedReady)
                {
                    AdsManager.LoadRewarded();
                    return;
                }

                AdsManager.ShowRewarded(
                    onRewardEarned: () =>
                    {
                        if (_save.UnlockedSkinIds == null)
                            _save.UnlockedSkinIds = new List<string> { "default" };
                        _save.UnlockedSkinIds.Add(skinId);
                        SaveManager.SaveUnlockedSkins(_save.UnlockedSkinIds);
                        _save = SaveManager.Load();
                        SelectSkin(skinId);
                    },
                    onClosed: null
                );
            }
        }
    }
}
