using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using Core.Save;
using Services.Auth;

namespace Core.Bootstrap
{
    /// <summary>
    /// Attach to a GameObject in the Boot scene. Runs once: load save, init Google login, then load Home.
    /// </summary>
    public class GameBootstrap : MonoBehaviour
    {
        [SerializeField] private float minLoadTime = 0.5f;

        private IEnumerator Start()
        {
            float start = Time.realtimeSinceStartup;

            try { GoogleLoginManager.Instance.InitializeManager(); } catch (Exception e) { Debug.LogWarning("GoogleLogin init: " + e.Message); }
            try { GoogleLoginManager.Instance.TrySilentLogin(null); } catch (Exception e) { Debug.LogWarning("GoogleLogin silent sign-in: " + e.Message); }
            try { LoadPlayerData(); } catch (Exception e) { Debug.LogWarning("LoadPlayerData: " + e.Message); }

            float elapsed = Time.realtimeSinceStartup - start;
            if (elapsed < minLoadTime)
                yield return new WaitForSecondsRealtime(minLoadTime - elapsed);

            int homeIndex = UnityEngine.SceneManagement.SceneUtility.GetBuildIndexByScenePath("Assets/Scenes/Home.unity");
            if (homeIndex >= 0)
                SceneManager.LoadScene(homeIndex);
            else
                SceneManager.LoadScene("Home");
        }

        private void LoadPlayerData()
        {
            SaveManager.Load();
        }
    }
}
