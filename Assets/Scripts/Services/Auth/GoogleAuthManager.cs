using System;

namespace Services.Auth
{
    /// <summary>
    /// 兼容层：保留原有静态调用方式，底层统一转给 GoogleLoginManager 单例。
    /// 这样可以不破坏旧业务逻辑，同时让后续排行榜继续复用这套接口。
    /// </summary>
    public static class GoogleAuthManager
    {
        public static bool IsLoggedIn => GoogleLoginManager.Instance.IsLoggedIn();
        public static string UserId => GoogleLoginManager.Instance.GetUserId();
        public static string DisplayName => GoogleLoginManager.Instance.GetUserName();

        public static void Initialize()
        {
            GoogleLoginManager.Instance.InitializeManager();
        }

        /// <summary>
        /// 静默登录（启动时恢复上次登录）。
        /// </summary>
        public static void TrySilentSignIn(Action<bool> onComplete = null)
        {
            GoogleLoginManager.Instance.TrySilentLogin(onComplete);
        }

        /// <summary>
        /// 发起登录。
        /// </summary>
        public static void Login(Action<bool> onComplete = null)
        {
            GoogleLoginManager.Instance.Login(onComplete);
        }

        /// <summary>登出。之后分数仅存本地（本设备），再登录会加载该账号的存档。</summary>
        public static void Logout()
        {
            GoogleLoginManager.Instance.Logout();
        }
    }
}
