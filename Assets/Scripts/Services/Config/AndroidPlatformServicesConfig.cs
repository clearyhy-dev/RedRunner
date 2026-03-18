using System;
using UnityEngine;

namespace Services.Config
{
    [Serializable]
    public sealed class AndroidPlatformServicesConfigData
    {
        public string packageName = "com.clearyhy.meowrunner";
        public GooglePlayGamesConfig googlePlayGames = new GooglePlayGamesConfig();
        public LeaderboardConfig leaderboards = new LeaderboardConfig();
        public AdMobConfig adMob = new AdMobConfig();
        public ConsentConfig consent = new ConsentConfig();
    }

    [Serializable]
    public sealed class GooglePlayGamesConfig
    {
        public bool enableGooglePlayGames;
        public string appId = "";
        public string webClientId = "";
        public string resourceXmlPath = "";
    }

    [Serializable]
    public sealed class LeaderboardConfig
    {
        public string playSceneLeaderboardId = "";
        public string creationSceneLeaderboardId = "";
    }

    [Serializable]
    public sealed class AdMobConfig
    {
        public bool enableAdMob = true;
        public bool useTestAds = true;
        public string androidAppId = "ca-app-pub-3940256099942544~3347511713";
        public string bannerAdUnitId = "ca-app-pub-3940256099942544/6300978111";
        public string interstitialAdUnitId = "ca-app-pub-3940256099942544/1033173712";
        public string rewardedAdUnitId = "ca-app-pub-3940256099942544/5224354917";
        public int interstitialFrequency = 3;
        public string[] testDeviceIds = Array.Empty<string>();
    }

    [Serializable]
    public sealed class ConsentConfig
    {
        public bool enableDebugGeographyEea;
        public string[] debugTestDeviceIds = Array.Empty<string>();
    }

    public static class AndroidPlatformServicesConfig
    {
        private const string ResourcePath = "Configs/AndroidPlatformServicesConfig";
        private static AndroidPlatformServicesConfigData s_Cached;

        public static AndroidPlatformServicesConfigData Current => s_Cached ??= LoadInternal();

        public static AndroidPlatformServicesConfigData Reload()
        {
            s_Cached = LoadInternal();
            return s_Cached;
        }

        public static AndroidPlatformServicesConfigData CreateDefault()
        {
            return new AndroidPlatformServicesConfigData();
        }

        public static string CreateDefaultJson()
        {
            return JsonUtility.ToJson(CreateDefault(), true);
        }

        public static string GetLeaderboardIdForScene(string sceneName)
        {
            var config = Current;
            if (config == null || config.leaderboards == null)
                return string.Empty;

            if (string.Equals(sceneName, "Creation", StringComparison.OrdinalIgnoreCase))
            {
                return string.IsNullOrWhiteSpace(config.leaderboards.creationSceneLeaderboardId)
                    ? config.leaderboards.playSceneLeaderboardId ?? string.Empty
                    : config.leaderboards.creationSceneLeaderboardId;
            }

            return config.leaderboards.playSceneLeaderboardId ?? string.Empty;
        }

        private static AndroidPlatformServicesConfigData LoadInternal()
        {
            var asset = Resources.Load<TextAsset>(ResourcePath);
            if (asset == null || string.IsNullOrWhiteSpace(asset.text))
            {
                Debug.LogWarning("[PlatformServices] Missing Resources/Configs/AndroidPlatformServicesConfig.json, using defaults.");
                return CreateDefault();
            }

            try
            {
                var config = JsonUtility.FromJson<AndroidPlatformServicesConfigData>(asset.text);
                return config ?? CreateDefault();
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[PlatformServices] Failed to parse AndroidPlatformServicesConfig.json: " + ex.Message);
                return CreateDefault();
            }
        }
    }
}
