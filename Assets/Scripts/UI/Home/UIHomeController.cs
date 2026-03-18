using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Services.Auth;

namespace UI.Home
{
    /// <summary>
    /// Home scene: Play, Shop, Settings, Privacy Policy, Google 登录.
    /// </summary>
    public class UIHomeController : MonoBehaviour
    {
        [SerializeField] private Button playButton;
        [SerializeField] private Button shopButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button privacyButton;
        [Header("Google 登录（可选）")]
        [SerializeField] private Button googleButton;
        [SerializeField] private Text googleStatusText;

        [Tooltip("URL opened when Privacy Policy is clicked.")]
        [SerializeField] private string privacyPolicyUrl = "https://example.com/privacy";

        void Start()
        {
            if (playButton != null)
                playButton.onClick.AddListener(() => SceneManager.LoadScene("Play"));
            if (shopButton != null)
                shopButton.onClick.AddListener(() => SceneManager.LoadScene("Shop"));
            if (settingsButton != null)
                settingsButton.onClick.AddListener(() => SceneManager.LoadScene("Settings"));
            if (privacyButton != null)
                privacyButton.onClick.AddListener(OpenPrivacyPolicy);
            GoogleLoginManager.Instance.InitializeManager();
            RefreshGoogleUi();
            if (googleButton != null)
            {
                googleButton.onClick.RemoveAllListeners();
                googleButton.onClick.AddListener(OnGoogleButtonClick);
            }
        }

        private void RefreshGoogleUi()
        {
            if (googleStatusText != null)
                googleStatusText.text = GoogleLoginManager.Instance.IsLoggedIn() ? "已登录: " + GoogleLoginManager.Instance.GetUserName() : "Google 登录";
            if (googleButton != null && googleButton.GetComponentInChildren<Text>() != null && googleStatusText == null)
                googleButton.GetComponentInChildren<Text>().text = GoogleLoginManager.Instance.IsLoggedIn() ? "登出" : "Google 登录";
        }

        private void OnGoogleButtonClick()
        {
            if (GoogleLoginManager.Instance.IsLoggedIn())
            {
                GoogleLoginManager.Instance.Logout();
                RefreshGoogleUi();
            }
            else
            {
                GoogleLoginManager.Instance.Login(success =>
                {
                    RefreshGoogleUi();
                });
            }
        }

        private void OpenPrivacyPolicy()
        {
            if (!string.IsNullOrEmpty(privacyPolicyUrl))
                Application.OpenURL(privacyPolicyUrl);
        }
    }
}
