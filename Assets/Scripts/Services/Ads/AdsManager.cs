using System;
using Services.Config;
using Services.Privacy;

#if USE_ADMOB
using System.Collections.Generic;
using GoogleMobileAds.Api;
#endif

namespace Services.Ads
{
    /// <summary>
    /// Wrapper around Google Mobile Ads. Falls back to no-op behavior when AdMob is disabled.
    /// </summary>
    public static class AdsManager
    {
        private static bool _initialized;
        private static bool _initializing;
        private static bool _rewardedLoading;
        private static bool _interstitialLoading;
        private static bool _bannerVisible;

#if USE_ADMOB
        private static RewardedAd _rewardedAd;
        private static InterstitialAd _interstitialAd;
        private static BannerView _bannerView;
#endif

        public static bool IsRewardedReady
        {
            get
            {
#if USE_ADMOB
                return _rewardedAd != null && _rewardedAd.CanShowAd();
#else
                return _initialized;
#endif
            }
        }

        public static bool IsInterstitialReady
        {
            get
            {
#if USE_ADMOB
                return _interstitialAd != null && _interstitialAd.CanShowAd();
#else
                return _initialized;
#endif
            }
        }

        public static void Initialize()
        {
            if (_initialized || _initializing)
                return;

#if USE_ADMOB
            var config = AndroidPlatformServicesConfig.Current?.adMob;
            if (config == null || !config.enableAdMob)
                return;

            if (!PrivacyConsentManager.CanRequestAds)
                return;

            _initializing = true;
            ApplyRequestConfiguration(config);
            MobileAds.Initialize(_ =>
            {
                _initialized = true;
                _initializing = false;
                LoadRewarded();
                LoadInterstitial();
                if (_bannerVisible)
                    ShowBanner();
            });
#else
            _initialized = true;
#endif
        }

        public static void LoadRewarded()
        {
#if USE_ADMOB
            if (!CanUseAdMob() || _rewardedLoading || IsRewardedReady)
                return;

            _rewardedLoading = true;
            RewardedAd.Load(GetRewardedUnitId(), new AdRequest(), (ad, error) =>
            {
                _rewardedLoading = false;
                if (error != null || ad == null)
                {
                    DebugLog("Rewarded load failed: " + (error != null ? error.GetMessage() : "unknown"));
                    return;
                }

                _rewardedAd?.Destroy();
                _rewardedAd = ad;
            });
#endif
        }

        public static void LoadInterstitial()
        {
#if USE_ADMOB
            if (!CanUseAdMob() || _interstitialLoading || IsInterstitialReady)
                return;

            _interstitialLoading = true;
            InterstitialAd.Load(GetInterstitialUnitId(), new AdRequest(), (ad, error) =>
            {
                _interstitialLoading = false;
                if (error != null || ad == null)
                {
                    DebugLog("Interstitial load failed: " + (error != null ? error.GetMessage() : "unknown"));
                    return;
                }

                _interstitialAd?.Destroy();
                _interstitialAd = ad;
            });
#endif
        }

        public static void ShowRewarded(Action onRewardEarned, Action onClosed = null)
        {
#if USE_ADMOB
            if (!CanUseAdMob())
            {
                onClosed?.Invoke();
                return;
            }

            if (_rewardedAd == null || !_rewardedAd.CanShowAd())
            {
                LoadRewarded();
                onClosed?.Invoke();
                return;
            }

            var ad = _rewardedAd;
            _rewardedAd = null;
            bool closed = false;
            ad.OnAdFullScreenContentClosed += () =>
            {
                if (!closed)
                {
                    closed = true;
                    onClosed?.Invoke();
                }
                ad.Destroy();
                LoadRewarded();
            };
            ad.OnAdFullScreenContentFailed += error =>
            {
                DebugLog("Rewarded show failed: " + error.GetMessage());
                if (!closed)
                {
                    closed = true;
                    onClosed?.Invoke();
                }
                ad.Destroy();
                LoadRewarded();
            };
            ad.Show(_ => onRewardEarned?.Invoke());
#else
            onRewardEarned?.Invoke();
            onClosed?.Invoke();
#endif
        }

        public static void ShowInterstitial(Action onClosed = null)
        {
#if USE_ADMOB
            if (!CanUseAdMob())
            {
                onClosed?.Invoke();
                return;
            }

            if (_interstitialAd == null || !_interstitialAd.CanShowAd())
            {
                LoadInterstitial();
                onClosed?.Invoke();
                return;
            }

            var ad = _interstitialAd;
            _interstitialAd = null;
            bool closed = false;
            ad.OnAdFullScreenContentClosed += () =>
            {
                if (!closed)
                {
                    closed = true;
                    onClosed?.Invoke();
                }
                ad.Destroy();
                LoadInterstitial();
            };
            ad.OnAdFullScreenContentFailed += error =>
            {
                DebugLog("Interstitial show failed: " + error.GetMessage());
                if (!closed)
                {
                    closed = true;
                    onClosed?.Invoke();
                }
                ad.Destroy();
                LoadInterstitial();
            };
            ad.Show();
#else
            onClosed?.Invoke();
#endif
        }

        public static void ShowBanner()
        {
            _bannerVisible = true;
#if USE_ADMOB
            if (!CanUseAdMob())
                return;

            if (_bannerView == null)
            {
                _bannerView = new BannerView(GetBannerUnitId(), AdSize.Banner, AdPosition.Bottom);
                _bannerView.OnBannerAdLoadFailed += error => DebugLog("Banner load failed: " + error.GetMessage());
                _bannerView.OnBannerAdLoaded += () => _bannerView.Show();
            }

            _bannerView.LoadAd(new AdRequest());
#endif
        }

        public static void HideBanner()
        {
            _bannerVisible = false;
#if USE_ADMOB
            _bannerView?.Hide();
#endif
        }

        public static bool ShouldShowInterstitial()
        {
            var config = AndroidPlatformServicesConfig.Current?.adMob;
            int frequency = config != null && config.interstitialFrequency > 0 ? config.interstitialFrequency : 3;
            int count = UnityEngine.PlayerPrefs.GetInt("meow_interstitial_counter", 0) + 1;
            UnityEngine.PlayerPrefs.SetInt("meow_interstitial_counter", count);
            UnityEngine.PlayerPrefs.Save();
            return count % frequency == 0;
        }

#if USE_ADMOB
        private static bool CanUseAdMob()
        {
            return _initialized
                && AndroidPlatformServicesConfig.Current?.adMob != null
                && AndroidPlatformServicesConfig.Current.adMob.enableAdMob
                && PrivacyConsentManager.CanRequestAds;
        }

        private static void ApplyRequestConfiguration(AdMobConfig config)
        {
            var requestConfiguration = new RequestConfiguration();
            if (config.testDeviceIds != null && config.testDeviceIds.Length > 0)
                requestConfiguration.TestDeviceIds = new List<string>(config.testDeviceIds);
            MobileAds.SetRequestConfiguration(requestConfiguration);
        }

        private static string GetBannerUnitId()
        {
            return AndroidPlatformServicesConfig.Current.adMob.useTestAds
                ? "ca-app-pub-3940256099942544/6300978111"
                : AndroidPlatformServicesConfig.Current.adMob.bannerAdUnitId;
        }

        private static string GetInterstitialUnitId()
        {
            return AndroidPlatformServicesConfig.Current.adMob.useTestAds
                ? "ca-app-pub-3940256099942544/1033173712"
                : AndroidPlatformServicesConfig.Current.adMob.interstitialAdUnitId;
        }

        private static string GetRewardedUnitId()
        {
            return AndroidPlatformServicesConfig.Current.adMob.useTestAds
                ? "ca-app-pub-3940256099942544/5224354917"
                : AndroidPlatformServicesConfig.Current.adMob.rewardedAdUnitId;
        }

        private static void DebugLog(string message)
        {
            UnityEngine.Debug.Log("[Ads] " + message);
        }
#endif
    }
}
