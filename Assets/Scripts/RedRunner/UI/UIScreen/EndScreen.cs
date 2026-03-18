using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Services.Ads;
using Core.Save;
using Services.Leaderboard;

namespace RedRunner.UI
{
    public class EndScreen : UIScreen
    {
        [SerializeField]
        protected Button ResetButton = null;
        [SerializeField]
        protected Button ReviveButton = null;
        [SerializeField]
        protected Button HomeButton = null;
        /// <summary>红色按钮：点击后显示本局分数与最高分。若未绑定分数文本则无效果。</summary>
        [SerializeField]
        protected Button ShowScoreButton = null;
        [SerializeField]
        protected Button ExitButton = null;

        [Header("分数显示（红色按钮点击后显示）")]
        [SerializeField]
        protected GameObject ScorePanel = null;
        [SerializeField]
        protected Text BestScoreText = null;
        [SerializeField]
        protected Text LastScoreText = null;
        [Header("线上排名（可选）")]
        [SerializeField] protected Button LeaderboardButton = null;

        private void Start()
        {
            if (ResetButton != null)
            {
                ResetButton.SetButtonAction(() =>
                {
                    void Restart()
                    {
                        GameManager.Singleton.RestartGame();
                        var ingameScreen = UIManager.Singleton.GetUIScreen(UIScreenInfo.IN_GAME_SCREEN);
                        UIManager.Singleton.OpenScreen(ingameScreen);
                    }

                    if (AdsManager.ShouldShowInterstitial())
                        AdsManager.ShowInterstitial(Restart);
                    else
                        Restart();
                });
            }
            if (ReviveButton != null)
            {
                ReviveButton.SetButtonAction(OnReviveClicked);
                bool canRevive = SaveManager.Load().IsMember || AdsManager.IsRewardedReady;
                ReviveButton.interactable = canRevive;
            }
            if (HomeButton != null)
                HomeButton.SetButtonAction(() =>
                {
                    if (AdsManager.ShouldShowInterstitial())
                        AdsManager.ShowInterstitial(() => GameManager.Singleton.ReturnHome());
                    else
                        GameManager.Singleton.ReturnHome();
                });
            // 红色按钮：显示最高分/本局分数（与 ExitButton 二选一绑定，优先 ShowScoreButton）
            var scoreBtn = ShowScoreButton != null ? ShowScoreButton : ExitButton;
            if (scoreBtn != null)
                scoreBtn.SetButtonAction(OnShowScoreClicked);
            if (LeaderboardButton != null)
                LeaderboardButton.onClick.AddListener(OnLeaderboardClick);
        }

        private void OnLeaderboardClick()
        {
            LeaderboardService.ShowLeaderboardUI(null);
        }

        private void OnShowScoreClicked()
        {
            var save = SaveManager.Load();
            if (BestScoreText != null)
            {
                BestScoreText.text = "最高分: " + Mathf.FloorToInt(save.BestScore);
                BestScoreText.gameObject.SetActive(true);
            }
            if (LastScoreText != null)
            {
                LastScoreText.text = "本局: " + Mathf.FloorToInt(save.LastRunScore);
                LastScoreText.gameObject.SetActive(true);
            }
            if (ScorePanel != null)
                ScorePanel.SetActive(true);
        }

        private void OnReviveClicked()
        {
            var save = SaveManager.Load();
            if (save.IsMember)
            {
                GameManager.Singleton.RevivePlayer();
                var ingameScreen = UIManager.Singleton.GetUIScreen(UIScreenInfo.IN_GAME_SCREEN);
                UIManager.Singleton.OpenScreen(ingameScreen);
                return;
            }
            if (!AdsManager.IsRewardedReady) return;
            AdsManager.ShowRewarded(
                onRewardEarned: () =>
                {
                    GameManager.Singleton.RevivePlayer();
                    var ingameScreen = UIManager.Singleton.GetUIScreen(UIScreenInfo.IN_GAME_SCREEN);
                    UIManager.Singleton.OpenScreen(ingameScreen);
                },
                onClosed: null
            );
        }

        public override void UpdateScreenStatus(bool open)
        {
            base.UpdateScreenStatus(open);
            if (open)
            {
                AdsManager.ShowBanner();
                var save = SaveManager.Load();
                if (ReviveButton != null)
                    ReviveButton.interactable = save.IsMember || AdsManager.IsRewardedReady;
                int best = Mathf.FloorToInt(save.BestScore);
                int last = Mathf.FloorToInt(save.LastRunScore);
                if (BestScoreText != null) { BestScoreText.text = "最高分: " + best; BestScoreText.gameObject.SetActive(true); }
                if (LastScoreText != null) { LastScoreText.text = "本局: " + last; LastScoreText.gameObject.SetActive(true); }
                if (ScorePanel != null) ScorePanel.SetActive(true);
                if (BestScoreText == null && LastScoreText == null)
                {
                    var fallback = GetComponentInChildren<Text>(true);
                    if (fallback != null) { fallback.text = "本局: " + last + "  最高分: " + best; fallback.gameObject.SetActive(true); }
                }
            }
            else
            {
                AdsManager.HideBanner();
            }
        }
    }
}