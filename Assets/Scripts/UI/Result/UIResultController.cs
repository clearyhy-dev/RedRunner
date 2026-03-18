using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Core.Save;
using RedRunner;
using Services.Ads;

namespace UI.Result
{
    /// <summary>
    /// Result scene UI: show score, best score; buttons for revive (rewarded ad), restart, home.
    /// </summary>
    public class UIResultController : MonoBehaviour
    {
        [SerializeField] private Text scoreText;
        [SerializeField] private Text bestScoreText;
        [SerializeField] private Button reviveButton;
        [SerializeField] private Button restartButton;
        [SerializeField] private Button homeButton;

        private float _lastScore;
        private float _bestScore;

        void OnEnable()
        {
            AdsManager.ShowBanner();
            var save = SaveManager.Load();
            if (reviveButton != null)
                reviveButton.interactable = save.IsMember || AdsManager.IsRewardedReady;
        }

        void OnDisable()
        {
            AdsManager.HideBanner();
        }

        void Start()
        {
            var save = SaveManager.Load();
            _lastScore = save.LastRunScore;
            _bestScore = save.BestScore;

            if (scoreText != null)
                scoreText.text = Mathf.FloorToInt(_lastScore).ToString();
            if (bestScoreText != null)
                bestScoreText.text = "Best: " + Mathf.FloorToInt(_bestScore);

            if (reviveButton != null)
            {
                reviveButton.onClick.AddListener(OnReviveClicked);
                bool canRevive = save.IsMember || AdsManager.IsRewardedReady;
                reviveButton.interactable = canRevive;
            }
            if (restartButton != null)
                restartButton.onClick.AddListener(OnRestartClicked);
            if (homeButton != null)
                homeButton.onClick.AddListener(OnHomeClicked);
        }

        private void OnReviveClicked()
        {
            var save = SaveManager.Load();
            if (save.IsMember)
            {
                GameManager.CheckpointScore = _lastScore;
                GameManager.WantsReviveNextLoad = true;
                GameManager.ReviveRestoreRequested = true;
                LoadPlayScene();
                return;
            }
            if (!AdsManager.IsRewardedReady)
                return;
            AdsManager.ShowRewarded(
                onRewardEarned: () =>
                {
                    GameManager.CheckpointScore = _lastScore;
                    GameManager.WantsReviveNextLoad = true;
                    GameManager.ReviveRestoreRequested = true;
                    LoadPlayScene();
                },
                onClosed: null
            );
        }

        private static void LoadPlayScene()
        {
            int i = UnityEngine.SceneManagement.SceneUtility.GetBuildIndexByScenePath("Assets/Scenes/Play.unity");
            if (i >= 0) SceneManager.LoadScene(i);
            else SceneManager.LoadScene("Play");
        }

        private void OnRestartClicked()
        {
            GameManager.WantsReviveNextLoad = false;
            GameManager.ReviveRestoreRequested = false;
            if (AdsManager.ShouldShowInterstitial())
                AdsManager.ShowInterstitial(LoadPlayScene);
            else
                LoadPlayScene();
        }

        private void OnHomeClicked()
        {
            GameManager.WantsReviveNextLoad = false;
            GameManager.ReviveRestoreRequested = false;
            void GoHome()
            {
                if (SceneManager.sceneCountInBuildSettings > 0) SceneManager.LoadScene(0);
                else SceneManager.LoadScene("Play");
            }

            if (AdsManager.ShouldShowInterstitial())
                AdsManager.ShowInterstitial(GoHome);
            else
                GoHome();
        }
    }
}
