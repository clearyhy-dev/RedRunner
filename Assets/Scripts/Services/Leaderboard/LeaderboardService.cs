using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using Services.Auth;
using Services.Config;

#if USE_GPGS
using GooglePlayGames;
#endif

namespace Services.Leaderboard
{
    /// <summary>
    /// 线上排名（Google Play Games 排行榜）：按场景/模式分别排名。
    /// 在 GetLeaderboardIdForScene 中填入 Play Console 创建的真实排行榜 ID（形如 CgkI...）；定义 USE_GPGS 后使用真实 API。
    /// </summary>
    public static class LeaderboardService
    {
        /// <summary>当前场景名对应的排行榜 ID（在 Google Play Console 创建排行榜后获得，形如 CgkI...）。</summary>
        private static string GetLeaderboardIdForScene(string sceneName)
        {
            if (string.IsNullOrEmpty(sceneName)) sceneName = "Play";
            return AndroidPlatformServicesConfig.GetLeaderboardIdForScene(sceneName);
        }

        /// <summary>获取当前场景的排行榜 ID。</summary>
        public static string GetCurrentLeaderboardId()
        {
            string sceneName = SceneManager.GetActiveScene().name;
            return GetLeaderboardIdForScene(sceneName);
        }

        /// <summary>
        /// 提交分数到当前场景的线上排行榜。需已登录 Google；未登录时仅本地存档，不提交。
        /// 正式版：调用 PlayGamesPlatform.Instance.ReportScore(score, leaderboardId, callback)。
        /// </summary>
        public static void SubmitScore(long score, Action<bool> onComplete = null)
        {
            if (!GoogleAuthManager.IsLoggedIn)
            {
                onComplete?.Invoke(false);
                return;
            }
            string leaderboardId = GetCurrentLeaderboardId();
            if (string.IsNullOrWhiteSpace(leaderboardId))
            {
                Debug.LogWarning("[Leaderboard] AndroidPlatformServicesConfig.json 中未填写当前场景的排行榜 ID。");
                onComplete?.Invoke(false);
                return;
            }
#if USE_GPGS
            PlayGamesPlatform.Instance.ReportScore(score, leaderboardId, (bool success) => onComplete?.Invoke(success));
#else
            Debug.Log("[Leaderboard] 提交分数（桩）: " + score + " -> " + leaderboardId);
            onComplete?.Invoke(false);
#endif
        }

        /// <summary>
        /// 提交分数到指定场景的排行榜（用于多场景各自排名）。
        /// </summary>
        public static void SubmitScoreForScene(long score, string sceneName, Action<bool> onComplete = null)
        {
            if (!GoogleAuthManager.IsLoggedIn) { onComplete?.Invoke(false); return; }
            string leaderboardId = GetLeaderboardIdForScene(sceneName);
            if (string.IsNullOrWhiteSpace(leaderboardId)) { onComplete?.Invoke(false); return; }
#if USE_GPGS
            PlayGamesPlatform.Instance.ReportScore(score, leaderboardId, (bool success) => onComplete?.Invoke(success));
#else
            Debug.Log("[Leaderboard] 提交分数（桩）: " + score + " scene=" + sceneName);
            onComplete?.Invoke(false);
#endif
        }

        /// <summary>
        /// 显示当前场景的排行榜 UI（Play Games 内置界面）。需已登录。
        /// 正式版：PlayGamesPlatform.Instance.ShowLeaderboardUI(leaderboardId);
        /// </summary>
        public static void ShowLeaderboardUI(Action onClosed = null)
        {
            if (!GoogleAuthManager.IsLoggedIn)
            {
                Debug.Log("[Leaderboard] 请先登录 Google 再查看排名。");
                onClosed?.Invoke();
                return;
            }
            string leaderboardId = GetCurrentLeaderboardId();
            if (string.IsNullOrWhiteSpace(leaderboardId))
            {
                Debug.LogWarning("[Leaderboard] AndroidPlatformServicesConfig.json 中未填写当前场景的排行榜 ID。");
                onClosed?.Invoke();
                return;
            }
#if USE_GPGS
            PlayGamesPlatform.Instance.ShowLeaderboardUI(leaderboardId, (status) => onClosed?.Invoke());
#else
            Debug.Log("[Leaderboard] 显示排行榜（桩）: " + leaderboardId);
            onClosed?.Invoke();
#endif
        }

        /// <summary>显示指定场景的排行榜 UI（用于「按场景看谁排名最高」）。</summary>
        public static void ShowLeaderboardUIForScene(string sceneName, Action onClosed = null)
        {
            if (!GoogleAuthManager.IsLoggedIn) { onClosed?.Invoke(); return; }
            string leaderboardId = GetLeaderboardIdForScene(sceneName);
            if (string.IsNullOrWhiteSpace(leaderboardId)) { onClosed?.Invoke(); return; }
#if USE_GPGS
            PlayGamesPlatform.Instance.ShowLeaderboardUI(leaderboardId, (status) => onClosed?.Invoke());
#else
            Debug.Log("[Leaderboard] 显示排行榜（桩）: " + sceneName + " -> " + leaderboardId);
            onClosed?.Invoke();
#endif
        }
    }
}
