using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

using BayatGames.SaveGameFree;
using BayatGames.SaveGameFree.Serializers;

using RedRunner.Characters;
using RedRunner.Collectables;
using RedRunner.Gameplay.Player;
using RedRunner.TerrainGeneration;
using RedRunner.UI;
using Core.Save;
using Services.Auth;
using Services.Leaderboard;
using Services.Privacy;
using Services.Ads;

#if USE_GPGS
using GooglePlayGames;
#endif

namespace RedRunner
{
    public enum GameState
    {
        None,
        Home,
        Playing,
        Paused,
        ReviveOffered,
        GameOver
    }

    public sealed class GameManager : MonoBehaviour
    {
        public delegate void AudioEnabledHandler(bool active);

        public delegate void ScoreHandler(float newScore, float highScore, float lastScore);

        public delegate void ResetHandler();

        public static event ResetHandler OnReset;
        public static event ScoreHandler OnScoreChanged;
        public static event AudioEnabledHandler OnAudioEnabled;

        private static GameManager m_Singleton;

        /// <summary>Cross-scene: set true when returning from Result to revive; cleared when Play loads.</summary>
        public static bool WantsReviveNextLoad { get; set; }

        /// <summary>Cross-scene: score at death for revive (restored when Play loads with WantsReviveNextLoad).</summary>
        public static float CheckpointScore { get; set; }

        /// <summary>When true, Play scene will respawn and open InGame instead of showing start screen.</summary>
        public static bool ReviveRestoreRequested { get; set; }

        public static GameManager Singleton
        {
            get
            {
                return m_Singleton;
            }
        }

        [SerializeField]
        private Character m_MainCharacter;
        [SerializeField]
        [TextArea(3, 30)]
        private string m_ShareText;
        [SerializeField]
        private string m_ShareUrl;
        [Tooltip("If true, load Result scene on game over; otherwise show End screen in same scene.")]
        [SerializeField]
        private bool m_UseResultScene = true;
        private float m_StartScoreX = 0f;
        private float m_HighScore = 0f;
        private float m_LastScore = 0f;
        private float m_Score = 0f;

        private bool m_GameStarted = false;
        private bool m_GameRunning = false;
        private bool m_AudioEnabled = true;
        private GameState m_State = GameState.None;

        /// <summary>
        /// This is my developed callbacks compoents, because callbacks are so dangerous to use we need something that automate the sub/unsub to functions
        /// with this in-house developed callbacks feature, we garantee that the callback will be removed when we don't need it.
        /// </summary>
        /// <summary>当前小鱼数量（收集物，原金币改为小鱼主题）</summary>
        public Property<int> m_Fish = new Property<int>(0);


        #region Getters
        public bool gameStarted
        {
            get
            {
                return m_GameStarted;
            }
        }

        public bool gameRunning
        {
            get
            {
                return m_GameRunning;
            }
        }

        public bool audioEnabled
        {
            get
            {
                return m_AudioEnabled;
            }
        }

        public GameState State => m_State;

        public float Score => m_Score;
        public float HighScore => m_HighScore;
        public float LastScore => m_LastScore;
        /// <summary>当前场景的主控角色（用于点屏跳跃等）</summary>
        public Character MainCharacter => m_MainCharacter;
        #endregion

        void Awake()
        {
            if (m_Singleton != null)
            {
                Destroy(gameObject);
                return;
            }
            SaveGame.Serializer = new SaveGameBinarySerializer();
            m_Singleton = this;
            m_Score = 0f;

#if USE_GPGS
            try { PlayGamesPlatform.Activate(); } catch (Exception e) { Debug.LogWarning("PlayGames init: " + e.Message); }
#endif
            try { GoogleAuthManager.Initialize(); } catch (Exception e) { Debug.LogWarning("GoogleAuth init: " + e.Message); }
            if (Application.isMobilePlatform)
            {
                ApplyMobileUiBaseline();
                EnsureMobileTouchInput();
            }
            SaveData save;
            try
            {
                save = SaveManager.Load();
            }
            catch (Exception e)
            {
                Debug.LogWarning("SaveManager.Load: " + e.Message);
                save = new SaveData();
            }
            m_Fish.Value = save.TotalFish;
            m_HighScore = save.BestScore;
            m_LastScore = save.LastRunScore > 0 ? save.LastRunScore : save.BestScore;
            SetAudioEnabled(save.MusicOn);

            if (WantsReviveNextLoad)
            {
                WantsReviveNextLoad = false;
                m_Score = CheckpointScore;
                m_State = GameState.Playing;
                m_GameStarted = true;
                m_GameRunning = true;
                Time.timeScale = 1f;
            }
            else
            {
                m_State = GameState.Home;
            }
        }

        void UpdateDeathEvent(bool isDead)
        {
            if (isDead)
            {
                StartCoroutine(DeathCrt());
            }
            else
            {
                StopCoroutine("DeathCrt");
            }
        }

        IEnumerator DeathCrt()
        {
            PlayerDied();
            yield return new WaitForSecondsRealtime(1.5f);
            m_State = GameState.GameOver;
            PersistScoresToSaveManager();
            LeaderboardService.SubmitScore((long)Mathf.Floor(m_HighScore));
            EndGame();
            // 优先在 Play 场景内显示结束界面，避免切到空的 Result/Home 导致蓝屏
            var ui = UIManager.Singleton;
            if (ui != null)
            {
                var endScreen = ui.GetUIScreen(UIScreenInfo.END_SCREEN);
                if (endScreen != null)
                {
                    ui.OpenScreen(endScreen);
                    yield break;
                }
            }
            Time.timeScale = 1f;
            if (SceneManager.sceneCountInBuildSettings > 0)
                SceneManager.LoadScene(0);
            else
                SceneManager.LoadScene("Play");
        }

        private static int GetBuildIndexBySceneName(string name)
        {
            int n = SceneManager.sceneCountInBuildSettings;
            for (int i = 0; i < n; i++)
            {
                string path = UnityEngine.SceneManagement.SceneUtility.GetScenePathByBuildIndex(i);
                if (path != null && path.IndexOf(name, StringComparison.OrdinalIgnoreCase) >= 0)
                    return i;
            }
            return -1;
        }

        /// <summary>
        /// 统一应用手机端 UI 基线。
        /// 这里只处理参考分辨率和全屏 Screen 根节点，不再让各处脚本各自兜底。
        /// </summary>
        private static void ApplyMobileUiBaseline()
        {
            ApplyMobileCanvasScaling();
            NormalizeMobileScreensToReferenceFrame();
        }

        /// <summary>手机端统一 CanvasScaler 参数，保证 UI 缩放口径一致。</summary>
        private static void ApplyMobileCanvasScaling()
        {
            Canvas[] canvases = UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (Canvas c in canvases)
            {
                if (c.renderMode != RenderMode.ScreenSpaceOverlay && c.renderMode != RenderMode.ScreenSpaceCamera)
                    continue;

                CanvasScaler scaler = c.GetComponent<CanvasScaler>();
                if (scaler == null)
                    scaler = c.gameObject.AddComponent<CanvasScaler>();

                MobileUiAdaptation.ConfigureCanvasScaler(scaler);
            }
        }

        /// <summary>
        /// 让每个 UIScreen 根节点回到统一的手机参考画布框。
        /// 不直接拉满父级，避免把原有 Animator 位移动画和布局参考系破坏掉。
        /// </summary>
        private static void NormalizeMobileScreensToReferenceFrame()
        {
            UIScreen[] screens = UnityEngine.Object.FindObjectsByType<UIScreen>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (UIScreen screen in screens)
            {
                RectTransform rectTransform = screen.GetComponent<RectTransform>();
                if (rectTransform == null)
                    continue;

                rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
                rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                rectTransform.pivot = new Vector2(0.5f, 0.5f);
                rectTransform.sizeDelta = MobileUiAdaptation.ReferenceResolution;
                rectTransform.anchoredPosition = Vector2.zero;
                rectTransform.localScale = Vector3.one;
            }
        }

        /// <summary>
        /// 真机上若场景未手动挂 MobileRunJumpInput，则自动补上，保证点屏跳跃链路一定存在。
        /// </summary>
        private static void EnsureMobileTouchInput()
        {
            if (UnityEngine.Object.FindFirstObjectByType<MobileRunJumpInput>(FindObjectsInactive.Include) != null)
                return;

            GameObject inputGo = new GameObject("MobileRunJumpInput_Auto");
            inputGo.AddComponent<MobileRunJumpInput>();
            Debug.Log("[MobileInput] 已自动创建 MobileRunJumpInput。");
        }

        private void PersistScoresToSaveManager()
        {
            try
            {
                var save = SaveManager.Load();
                save.BestScore = m_HighScore;
                save.LastRunScore = m_LastScore;
                save.TotalFish = m_Fish.Value;
                save.MusicOn = m_AudioEnabled;
                SaveManager.Save(save);
            }
            catch (Exception e) { Debug.LogWarning("PersistScores: " + e.Message); }
        }

        /// <summary>Called when player dies. Updates last/high score and fires events.</summary>
        public void PlayerDied()
        {
            m_LastScore = m_Score;
            if (m_Score > m_HighScore)
                m_HighScore = m_Score;
            if (OnScoreChanged != null)
                OnScoreChanged(m_Score, m_HighScore, m_LastScore);
        }

        /// <summary>Respawn in place and resume (same scene). Optional invincibility handled by caller.</summary>
        public void RevivePlayer()
        {
            RespawnMainCharacter();
            m_State = GameState.Playing;
            m_GameStarted = true;
            ResumeGame();
        }

        /// <summary>Restart run in this scene: reset and start game.</summary>
        public void RestartGame()
        {
            Reset();
            StartGame();
        }

        /// <summary>从存档重新加载分数到内存（登录/登出后调用，保证显示与当前账号一致）。</summary>
        public void ReloadSaveIntoMemory()
        {
            try
            {
                var save = SaveManager.Load();
                m_HighScore = save.BestScore;
                m_LastScore = save.LastRunScore > 0 ? save.LastRunScore : save.BestScore;
                if (m_Fish != null) m_Fish.Value = save.TotalFish;
            }
            catch (Exception e) { Debug.LogWarning("ReloadSave: " + e.Message); }
        }

        /// <summary>Load Home scene.</summary>
        public void ReturnHome()
        {
            LoadHomeScene();
        }

        private static void LoadHomeScene()
        {
            Time.timeScale = 1f;
            if (SceneManager.sceneCountInBuildSettings > 0)
                SceneManager.LoadScene(0);
            else
                SceneManager.LoadScene("Play");
        }

        public void PauseGame()
        {
            m_State = GameState.Paused;
            StopGame();
        }

        private static bool s_PlaySceneInitialized;

        private void Start()
        {
            if (!s_PlaySceneInitialized)
            {
                s_PlaySceneInitialized = true;
                try
                {
                    PrivacyConsentManager.Initialize(() =>
                    {
                        try { AdsManager.Initialize(); } catch (Exception e) { Debug.LogWarning("Ads: " + e.Message); }
                    });
                }
                catch (Exception e) { Debug.LogWarning("Privacy: " + e.Message); }
                try { GoogleAuthManager.Initialize(); } catch (Exception e) { Debug.LogWarning("GoogleAuth: " + e.Message); }
                try { SaveManager.Load(); } catch (Exception e) { Debug.LogWarning("LoadPlayerData: " + e.Message); }
            }

            if (Application.isMobilePlatform)
                StartCoroutine(ApplyMobileUiBaselineNextFrame());

            if (m_MainCharacter != null)
            {
                m_MainCharacter.IsDead.AddEventAndFire(UpdateDeathEvent, this);
                m_StartScoreX = m_MainCharacter.transform.position.x;
            }
            StartCoroutine(ApplyFishNextFrame());
            Init();
        }

        private IEnumerator ApplyMobileUiBaselineNextFrame()
        {
            yield return null;
            ApplyMobileUiBaseline();
        }

        private IEnumerator ApplyFishNextFrame()
        {
            yield return null;
            ApplyFishSpriteAtRuntime.ApplyNow();
        }

        public void Init()
        {
            if (ReviveRestoreRequested)
            {
                ReviveRestoreRequested = false;
                RespawnMainCharacter();
                var ingame = UIManager.Singleton.GetUIScreen(UIScreenInfo.IN_GAME_SCREEN);
                if (ingame != null)
                    UIManager.Singleton.OpenScreen(ingame);
                return;
            }
            EndGame();
            UIManager.Singleton.Init();
            StartCoroutine(Load());
        }

        void Update()
        {
            if (m_GameRunning)
            {
                if (m_MainCharacter.transform.position.x > m_StartScoreX && m_MainCharacter.transform.position.x > m_Score)
                {
                    m_Score = m_MainCharacter.transform.position.x;
                    if (OnScoreChanged != null)
                    {
                        OnScoreChanged(m_Score, m_HighScore, m_LastScore);
                    }
                }
            }
        }

        IEnumerator Load()
        {
            var startScreen = UIManager.Singleton.UISCREENS.Find(el => el.ScreenInfo == UIScreenInfo.START_SCREEN);
            yield return new WaitForSecondsRealtime(3f);
            UIManager.Singleton.OpenScreen(startScreen);
        }

        void OnApplicationQuit()
        {
            if (m_Score > m_HighScore)
                m_HighScore = m_Score;
            PersistScoresToSaveManager();
            LeaderboardService.SubmitScore((long)Mathf.Floor(m_HighScore));
        }

        public void ExitGame()
        {
            Application.Quit();
        }

        public void ToggleAudioEnabled()
        {
            SetAudioEnabled(!m_AudioEnabled);
        }

        public void SetAudioEnabled(bool active)
        {
            m_AudioEnabled = active;
            AudioListener.volume = active ? 1f : 0f;
            SaveManager.SaveMusicOn(active);
            if (OnAudioEnabled != null)
                OnAudioEnabled(active);
        }

        public void StartGame()
        {
            m_State = GameState.Playing;
            m_GameStarted = true;
            ResumeGame();
        }

        public void StopGame()
        {
            m_GameRunning = false;
            Time.timeScale = 0f;
        }

        public void ResumeGame()
        {
            m_State = GameState.Playing;
            m_GameRunning = true;
            Time.timeScale = 1f;
        }

        public void EndGame()
        {
            m_State = GameState.GameOver;
            m_GameStarted = false;
            StopGame();
        }

        public void RespawnMainCharacter()
        {
            RespawnCharacter(m_MainCharacter);
        }

        public void RespawnCharacter(Character character)
        {
            if (TerrainGenerator.Singleton == null) return;
            Block block = TerrainGenerator.Singleton.GetCharacterBlock();
            if (block != null && block)
            {
                Vector3 position = block.transform.position;
                position.y += 2.56f;
                position.x += 1.28f;
                character.transform.position = position;
                character.Reset();
            }
        }

        public void Reset()
        {
            m_Score = 0f;
            if (OnReset != null)
            {
                OnReset();
            }
        }

        public void ShareOnTwitter()
        {
            Share("https://twitter.com/intent/tweet?text={0}&url={1}");
        }

        /// <summary>已废弃：仅保留 Google 登录/登出，不再做分享到 G+。</summary>
        public void ShareOnGooglePlus()
        {
            // 空实现，避免场景里旧按钮引用报错；点击已由 StartScreen 绑为应用内登录。
        }

        public void ShareOnFacebook()
        {
            Share("https://www.facebook.com/sharer/sharer.php?u={1}");
        }

        public void Share(string url)
        {
            Application.OpenURL(string.Format(url, m_ShareText, m_ShareUrl));
        }

        [System.Serializable]
        public class LoadEvent : UnityEvent
        {

        }

    }

}