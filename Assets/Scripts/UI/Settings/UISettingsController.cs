using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Core.Save;
using Services.Privacy;

namespace UI.Settings
{
    /// <summary>
    /// Settings scene: music, sfx toggles; privacy & terms links; version text; back to Home.
    /// </summary>
    public class UISettingsController : MonoBehaviour
    {
        [SerializeField] private Toggle musicToggle;
        [SerializeField] private Toggle sfxToggle;
        [SerializeField] private Button privacyButton;
        [SerializeField] private Button termsButton;
        [SerializeField] private Button backButton;
        [SerializeField] private Text versionText;

        [SerializeField] private string privacyPolicyUrl = "https://example.com/privacy";
        [SerializeField] private string termsUrl = "https://example.com/terms";

        void Start()
        {
            var save = SaveManager.Load();
            if (musicToggle != null)
            {
                musicToggle.isOn = save.MusicOn;
                musicToggle.onValueChanged.AddListener(OnMusicChanged);
            }
            if (sfxToggle != null)
            {
                sfxToggle.isOn = save.SfxOn;
                sfxToggle.onValueChanged.AddListener(OnSfxChanged);
            }
            if (privacyButton != null)
                privacyButton.onClick.AddListener(OpenPrivacyOptionsOrPolicy);
            if (termsButton != null)
                termsButton.onClick.AddListener(() => Application.OpenURL(termsUrl));
            if (backButton != null)
                backButton.onClick.AddListener(() => SceneManager.LoadScene("Play"));
            if (versionText != null)
                versionText.text = "v" + Application.version;

            AudioListener.volume = save.MusicOn ? 1f : 0f;
        }

        private void OnMusicChanged(bool value)
        {
            SaveManager.SaveMusicOn(value);
            AudioListener.volume = value ? 1f : 0f;
        }

        private void OnSfxChanged(bool value)
        {
            SaveManager.SaveSfxOn(value);
        }

        private void OpenPrivacyOptionsOrPolicy()
        {
            if (PrivacyConsentManager.RequiresPrivacyOptions)
            {
                PrivacyConsentManager.ShowPrivacyOptionsForm(() =>
                {
                    if (!string.IsNullOrEmpty(privacyPolicyUrl))
                        Application.OpenURL(privacyPolicyUrl);
                });
                return;
            }

            if (!string.IsNullOrEmpty(privacyPolicyUrl))
                Application.OpenURL(privacyPolicyUrl);
        }
    }
}
