using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;
using Services.Auth;

namespace Core.Save
{
    [Serializable]
    public class SaveData
    {
        public float BestScore;
        public float LastRunScore;
        public int TotalFish;
        public string SelectedSkinId;
        public List<string> UnlockedSkinIds;
        public bool MusicOn;
        public bool SfxOn;
        public bool RemoveAds;
        /// <summary>会员：可免广告复活、额外权益等</summary>
        public bool IsMember;

        public SaveData()
        {
            BestScore = 0f;
            LastRunScore = 0f;
            TotalFish = 0;
            SelectedSkinId = "default";
            UnlockedSkinIds = new List<string> { "default" };
            MusicOn = true;
            SfxOn = true;
            RemoveAds = false;
            IsMember = false;
        }
    }

    /// <summary>
    /// Central save/load for Meow Runner. Uses PlayerPrefs.
    /// 登录 Google 时按账号分别存档；未登录时仅使用本地档（本设备当次游玩分数）。
    /// </summary>
    public static class SaveManager
    {
        private const string KeyBestScore = "meow_bestScore";
        private const string KeyLastRunScore = "meow_lastRunScore";
        private const string KeyTotalFish = "meow_totalFish";
        private const string KeySelectedSkinId = "meow_selectedSkinId";
        private const string KeyUnlockedSkinIds = "meow_unlockedSkinIds";
        private const string KeyMusicOn = "meow_musicOn";
        private const string KeySfxOn = "meow_sfxOn";
        private const string KeyRemoveAds = "meow_removeAds";
        private const string KeyIsMember = "meow_isMember";

        /// <summary>当前存档键后缀：未登录为空（本地档），登录后为 _userIdHash（按 Google 账号区分）。</summary>
        private static string GetUserSuffix()
        {
            try
            {
                if (!GoogleAuthManager.IsLoggedIn || string.IsNullOrEmpty(GoogleAuthManager.UserId))
                    return "";
                return "_u" + HashUserId(GoogleAuthManager.UserId);
            }
            catch { return ""; }
        }

        private static string HashUserId(string userId)
        {
            if (string.IsNullOrEmpty(userId)) return "";
            try
            {
                var bytes = Encoding.UTF8.GetBytes(userId);
                using (var sha = SHA256.Create())
                {
                    var hash = sha.ComputeHash(bytes);
                    var sb = new StringBuilder(16);
                    for (int i = 0; i < 8 && i < hash.Length; i++)
                        sb.Append(hash[i].ToString("x2"));
                    return sb.ToString();
                }
            }
            catch { return userId.Length > 16 ? userId.Substring(0, 16) : userId; }
        }

        private static string K(string baseKey) => baseKey + GetUserSuffix();

        public static SaveData Load()
        {
            var data = new SaveData();
            data.BestScore = PlayerPrefs.GetFloat(K(KeyBestScore), 0f);
            data.LastRunScore = PlayerPrefs.GetFloat(K(KeyLastRunScore), 0f);
            data.TotalFish = PlayerPrefs.GetInt(K(KeyTotalFish), 0);
            data.SelectedSkinId = PlayerPrefs.GetString(K(KeySelectedSkinId), "default");
            data.MusicOn = PlayerPrefs.GetInt(K(KeyMusicOn), 1) != 0;
            data.SfxOn = PlayerPrefs.GetInt(K(KeySfxOn), 1) != 0;
            data.RemoveAds = PlayerPrefs.GetInt(K(KeyRemoveAds), 0) != 0;
            data.IsMember = PlayerPrefs.GetInt(K(KeyIsMember), 0) != 0;

            string ids = PlayerPrefs.GetString(K(KeyUnlockedSkinIds), "default");
            data.UnlockedSkinIds = new List<string>(ids.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries));
            if (data.UnlockedSkinIds.Count == 0)
                data.UnlockedSkinIds.Add("default");

            return data;
        }

        public static void Save(SaveData data)
        {
            if (data == null) return;
            PlayerPrefs.SetFloat(K(KeyBestScore), data.BestScore);
            PlayerPrefs.SetFloat(K(KeyLastRunScore), data.LastRunScore);
            PlayerPrefs.SetInt(K(KeyTotalFish), data.TotalFish);
            PlayerPrefs.SetString(K(KeySelectedSkinId), data.SelectedSkinId ?? "default");
            PlayerPrefs.SetString(K(KeyUnlockedSkinIds), data.UnlockedSkinIds != null ? string.Join(",", data.UnlockedSkinIds) : "default");
            PlayerPrefs.SetInt(K(KeyMusicOn), data.MusicOn ? 1 : 0);
            PlayerPrefs.SetInt(K(KeySfxOn), data.SfxOn ? 1 : 0);
            PlayerPrefs.SetInt(K(KeyRemoveAds), data.RemoveAds ? 1 : 0);
            PlayerPrefs.SetInt(K(KeyIsMember), data.IsMember ? 1 : 0);
            PlayerPrefs.Save();
        }

        public static void SaveIsMember(bool value)
        {
            PlayerPrefs.SetInt(K(KeyIsMember), value ? 1 : 0);
            PlayerPrefs.Save();
        }

        public static void SaveBestScore(float value)
        {
            PlayerPrefs.SetFloat(K(KeyBestScore), value);
            PlayerPrefs.Save();
        }

        public static void SaveLastRunScore(float value)
        {
            PlayerPrefs.SetFloat(K(KeyLastRunScore), value);
            PlayerPrefs.Save();
        }

        public static void SaveTotalFish(int value)
        {
            PlayerPrefs.SetInt(K(KeyTotalFish), value);
            PlayerPrefs.Save();
        }

        public static void SaveSelectedSkin(string skinId)
        {
            PlayerPrefs.SetString(K(KeySelectedSkinId), skinId ?? "default");
            PlayerPrefs.Save();
        }

        public static void SaveUnlockedSkins(List<string> ids)
        {
            PlayerPrefs.SetString(K(KeyUnlockedSkinIds), ids != null ? string.Join(",", ids) : "default");
            PlayerPrefs.Save();
        }

        public static void SaveMusicOn(bool value)
        {
            PlayerPrefs.SetInt(K(KeyMusicOn), value ? 1 : 0);
            PlayerPrefs.Save();
        }

        public static void SaveSfxOn(bool value)
        {
            PlayerPrefs.SetInt(K(KeySfxOn), value ? 1 : 0);
            PlayerPrefs.Save();
        }
    }
}
