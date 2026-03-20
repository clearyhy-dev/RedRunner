using UnityEngine;
using UnityEngine.EventSystems;
using UnityStandardAssets.CrossPlatformInput;
using RedRunner.Characters;

namespace RedRunner.Gameplay.Player
{
    /// <summary>
    /// 极简手机输入：
    /// 1. 游戏开始后自动向右跑
    /// 2. 点击任意非 UI 区域跳跃
    /// 3. 如果游戏未开始，第一次点击先开始游戏再立即跳跃
    /// 4. 暂时不处理右半屏加速和长按高跳
    /// </summary>
    public class MobileRunJumpInput : MonoBehaviour
    {
        [Tooltip("编辑器中是否允许鼠标左键模拟手机点击")]
        public bool enableInEditor = true;

        // 保留旧接口，避免其他脚本因历史引用而编译失败。
        public static bool EditorSprintHeld => false;

        private bool ShouldApply()
        {
            return Application.isMobilePlatform || (Application.isEditor && enableInEditor);
        }

        private void Update()
        {
            if (!ShouldApply())
                return;

            var gm = RedRunner.GameManager.Singleton;
            if (gm == null)
                return;

            // 游戏开始后保持自动向右跑。
            if (gm.gameStarted && gm.gameRunning)
                CrossPlatformInputManager.SetAxis("Horizontal", 1f);

            if (Application.isMobilePlatform)
            {
                HandleTouch(gm);
                return;
            }

            if (Application.isEditor && enableInEditor)
                HandleMouse(gm);
        }

        private void HandleTouch(RedRunner.GameManager gm)
        {
            for (int i = 0; i < Input.touchCount; i++)
            {
                Touch touch = Input.GetTouch(i);
                if (touch.phase != TouchPhase.Began)
                    continue;

                if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(touch.fingerId))
                    continue;

                if (!gm.gameStarted || !gm.gameRunning)
                {
                    gm.StartGame();
                    Debug.Log("[MobileInput] 触摸开始游戏");
                }

                JumpMainCharacter(gm, "[MobileInput] 触摸跳跃");
                break;
            }
        }

        private void HandleMouse(RedRunner.GameManager gm)
        {
            if (!Input.GetMouseButtonDown(0))
                return;

            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                return;

            if (!gm.gameStarted || !gm.gameRunning)
            {
                gm.StartGame();
                Debug.Log("[MobileInput] 鼠标开始游戏");
            }

            JumpMainCharacter(gm, "[MobileInput] 鼠标跳跃");
        }

        private void JumpMainCharacter(RedRunner.GameManager gm, string logMessage)
        {
            RedCharacter red = null;

            if (gm != null && gm.MainCharacter != null)
                red = gm.MainCharacter as RedCharacter;

            if (red == null)
                red = FindFirstObjectByType<RedCharacter>(FindObjectsInactive.Include);

            if (red != null)
            {
                Debug.Log(logMessage);
                red.Jump();
            }

            CrossPlatformInputManager.SetButtonDown("Jump");
            CrossPlatformInputManager.SetButtonUp("Jump");
        }
    }
}
