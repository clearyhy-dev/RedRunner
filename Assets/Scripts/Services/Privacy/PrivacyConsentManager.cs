using System;
using Services.Config;
using UnityEngine;

#if USE_ADMOB
using System.Collections.Generic;
using GoogleMobileAds.Ump.Api;
#endif

namespace Services.Privacy
{
    /// <summary>
    /// Wraps UMP consent flow for production ads and falls back to a local dev toggle otherwise.
    /// </summary>
    public static class PrivacyConsentManager
    {
        private const string KeyConsentGiven = "meow_privacy_consent_given";

        private static bool _initialized;
        private static bool _initializing;
        private static Action _pendingCallbacks;

        public static bool IsInitialized => _initialized;

        public static bool HasConsent
        {
            get
            {
#if USE_ADMOB
                return _initialized && ConsentInformation.CanRequestAds();
#else
                return _initialized && (PlayerPrefs.GetInt(KeyConsentGiven, 0) != 0);
#endif
            }
        }

        public static bool CanRequestAds => HasConsent;

        public static bool RequiresPrivacyOptions
        {
            get
            {
#if USE_ADMOB
                return ConsentInformation.PrivacyOptionsRequirementStatus == PrivacyOptionsRequirementStatus.Required;
#else
                return false;
#endif
            }
        }

        public static void Initialize(Action onComplete = null)
        {
            if (_initialized)
            {
                onComplete?.Invoke();
                return;
            }

            _pendingCallbacks += onComplete;
            if (_initializing)
                return;

            _initializing = true;

#if USE_ADMOB
            var request = BuildRequestParameters();
            ConsentInformation.Update(request, consentError =>
            {
                if (consentError != null)
                    Debug.LogWarning("[Privacy] Consent update failed: " + consentError.Message);

                ConsentForm.LoadAndShowConsentFormIfRequired(formError =>
                {
                    if (formError != null)
                        Debug.LogWarning("[Privacy] Consent form failed: " + formError.Message);

                    CompleteInitialization();
                });
            });
#else
            if (PlayerPrefs.GetInt(KeyConsentGiven, -1) == -1)
                SetConsent(true);
            CompleteInitialization();
#endif
        }

        public static void SetConsent(bool given)
        {
            PlayerPrefs.SetInt(KeyConsentGiven, given ? 1 : 0);
            PlayerPrefs.Save();
        }

        public static void ShowPrivacyOptionsForm(Action onComplete = null)
        {
#if USE_ADMOB
            if (!RequiresPrivacyOptions)
            {
                onComplete?.Invoke();
                return;
            }

            ConsentForm.ShowPrivacyOptionsForm(showError =>
            {
                if (showError != null)
                    Debug.LogWarning("[Privacy] Privacy options form failed: " + showError.Message);
                onComplete?.Invoke();
            });
#else
            onComplete?.Invoke();
#endif
        }

#if USE_ADMOB
        private static ConsentRequestParameters BuildRequestParameters()
        {
            var request = new ConsentRequestParameters();
            var config = AndroidPlatformServicesConfig.Current;
            if (config?.consent == null)
                return request;

            if (!config.consent.enableDebugGeographyEea &&
                (config.consent.debugTestDeviceIds == null || config.consent.debugTestDeviceIds.Length == 0))
            {
                return request;
            }

            var debugSettings = new ConsentDebugSettings();
            if (config.consent.enableDebugGeographyEea)
                debugSettings.DebugGeography = DebugGeography.EEA;

            if (config.consent.debugTestDeviceIds != null && config.consent.debugTestDeviceIds.Length > 0)
                debugSettings.TestDeviceHashedIds = new List<string>(config.consent.debugTestDeviceIds);

            request.ConsentDebugSettings = debugSettings;
            return request;
        }
#endif

        private static void CompleteInitialization()
        {
            _initialized = true;
            _initializing = false;
            var callbacks = _pendingCallbacks;
            _pendingCallbacks = null;
            callbacks?.Invoke();
        }
    }
}
