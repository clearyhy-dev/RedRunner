using UnityEngine;
using RedRunner;
using Core.Save;

namespace Core.Managers
{
    /// <summary>
    /// Facade for current run score, best score, and fish (collectibles). In Play scene delegates to GameManager; persists via SaveManager.
    /// Can be used from UI or other systems without referencing GameManager directly.
    /// </summary>
    public class ScoreManager : MonoBehaviour
    {
        private static ScoreManager _instance;

        public static ScoreManager Instance
        {
            get
            {
                if (_instance == null)
                    _instance = FindFirstObjectByType<ScoreManager>();
                return _instance;
            }
        }

        void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
        }

        void OnDestroy()
        {
            if (_instance == this)
                _instance = null;
        }

        public float CurrentScore => GameManager.Singleton != null ? GameManager.Singleton.Score : 0f;

        public float BestScore
        {
            get
            {
                if (GameManager.Singleton != null)
                    return GameManager.Singleton.HighScore;
                return SaveManager.Load().BestScore;
            }
        }

        public int Fish
        {
            get
            {
                if (GameManager.Singleton != null)
                    return GameManager.Singleton.m_Fish.Value;
                return SaveManager.Load().TotalFish;
            }
        }

        public float LastScore => GameManager.Singleton != null ? GameManager.Singleton.LastScore : 0f;
    }
}
