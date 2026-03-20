using UnityEngine;
using UnityEngine.UI;
using Services.Auth;

namespace UI
{
    /// <summary>
    /// 绑定一个按钮：未登录显示「Google 登录」，登录后显示「登出 (昵称)」。点击执行登录/登出。
    /// 登录后分数按 Google 账号记录；未登录仅记录本机当次游玩分数。
    /// </summary>
    public class UIGoogleAuth : MonoBehaviour
    {
        [SerializeField] private bool hideTemporarilyOnMobile = true;
        [SerializeField] private Button button;
        [SerializeField] private Text statusText;

        private void Start()
        {
            if (hideTemporarilyOnMobile && Application.isMobilePlatform)
            {
                gameObject.SetActive(false);
                return;
            }

            if (button == null) button = GetComponent<Button>();
            if (button != null)
            {
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(OnButtonClick);
            }

            if (statusText != null && !GoogleLoginManager.Instance.IsLoggedIn())
                statusText.text = "Google 登录(暂未启用)";

            Refresh();
        }

        private void OnButtonClick()
        {
            if (GoogleLoginManager.Instance.IsLoggedIn())
            {
                GoogleLoginManager.Instance.Logout();
                Refresh();
            }
            else
            {
                GoogleLoginManager.Instance.Login(success =>
                {
                    Refresh();
                });
            }
        }

        public void Refresh()
        {
            if (statusText != null)
                statusText.text = GoogleLoginManager.Instance.IsLoggedIn() ? "登出 (" + GoogleLoginManager.Instance.GetUserName() + ")" : "Google 登录";
            if (button != null && button.GetComponentInChildren<Text>() != null && statusText == null)
                button.GetComponentInChildren<Text>().text = GoogleLoginManager.Instance.IsLoggedIn() ? "登出 (" + GoogleLoginManager.Instance.GetUserName() + ")" : "Google 登录";
        }
    }
}
