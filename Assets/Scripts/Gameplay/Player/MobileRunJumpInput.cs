using UnityEngine;
using UnityEngine.EventSystems;
using UnityStandardAssets.CrossPlatformInput;
using RedRunner.Characters;
using RedRunner.UI;

namespace RedRunner.Gameplay.Player
{
    /// <summary>
    /// 手机端：统一处理首页开始、游戏内点击跳跃，以及右半屏按住加速。
    /// 编辑器下仍保留原有鼠标模拟逻辑，方便本地快速回归。
    /// 挂到 Play 场景里任意常驻物体上（如 GameManager）。
    /// </summary>
    public class MobileRunJumpInput : MonoBehaviour
    {
        [Tooltip("为 true 时在编辑器里也生效（鼠标点击/按住测试）")]
        public bool enableInEditor = true;

        [Tooltip("最短按住时间（秒）对应最小跳高比例")]
        [Range(0.02f, 0.2f)]
        public float minHoldTime = 0.08f;
        [Tooltip("达到最大跳高的按住时间（秒）")]
        [Range(0.2f, 1f)]
        public float maxHoldTime = 0.5f;

        [Tooltip("手机端按住屏幕右侧为加速跑；非 UI 点击会触发开始游戏或跳跃")]
        public bool enableSprintOnRightSide = true;

        /// <summary>编辑器下用鼠标模拟加速跑时为 true（右键或点击右半屏），不经过 CrossPlatformInputManager 避免 StandaloneInput 抛错。</summary>
        public static bool EditorSprintHeld { get; private set; }

        private float m_TouchDownTime = -1f;
        private int m_SprintTouchId = -1;
        private bool m_EditorSprintHeld = false;
        private bool m_TouchBeganOnRightSide = false;
        private const float SprintHoldThreshold = 0.18f;
        private bool m_MobileSprintHeld = false;

        private bool ShouldApply()
        {
            return Application.isMobilePlatform || (enableInEditor && Application.isEditor);
        }

        private void Update()
        {
            if (!ShouldApply()) return;
            var gm = RedRunner.GameManager.Singleton;
            if (gm != null && (!gm.gameStarted || !gm.gameRunning))
            {
                if (Application.isMobilePlatform)
                    HandleHomeTapStart(gm);
                return;
            }

            // 自动向右跑
            CrossPlatformInputManager.SetAxis("Horizontal", 1f);

            if (Application.isMobilePlatform)
            {
                HandleMobileGameplayInput(gm);
                return;
            }

            // 加速跑：按住屏幕右侧
            if (enableSprintOnRightSide)
            {
                float rightHalf = Screen.width * 0.5f;
                for (int i = 0; i < Input.touchCount; i++)
                {
                    var t = Input.GetTouch(i);
                    if (t.phase == TouchPhase.Began && t.position.x >= rightHalf)
                    {
                        m_SprintTouchId = t.fingerId;
                        CrossPlatformInputManager.SetButtonDown("Sprint");
                        break;
                    }
                }
                if (m_SprintTouchId >= 0)
                {
                    bool stillHeld = false;
                    for (int i = 0; i < Input.touchCount; i++)
                    {
                        var t = Input.GetTouch(i);
                        if (t.fingerId == m_SprintTouchId)
                        {
                            if (t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled)
                            {
                                CrossPlatformInputManager.SetButtonUp("Sprint");
                                m_SprintTouchId = -1;
                            }
                            else
                                stillHeld = true;
                            break;
                        }
                    }
                    if (!stillHeld && m_SprintTouchId >= 0)
                    {
                        CrossPlatformInputManager.SetButtonUp("Sprint");
                        m_SprintTouchId = -1;
                    }
                }
                // 编辑器：用静态标志位传递加速跑，不调用 SetButtonDown/Up（StandaloneInput 会抛错）
                if (Application.isEditor && enableInEditor)
                {
                    if (Input.GetMouseButtonDown(1) || (Input.GetMouseButtonDown(0) && Input.mousePosition.x >= rightHalf))
                    {
                        m_EditorSprintHeld = true;
                        EditorSprintHeld = true;
                    }
                    if (m_EditorSprintHeld && (Input.GetMouseButtonUp(1) || Input.GetMouseButtonUp(0)))
                    {
                        m_EditorSprintHeld = false;
                        EditorSprintHeld = false;
                    }
                }
            }

            // 触摸/鼠标：左侧或短按为跳跃；按下记时，抬起时按按住时长决定跳高
            bool touchDown = false;
            bool touchUp = false;
            bool isRightSide = false;
            if (Input.touchCount > 0)
            {
                var t = Input.GetTouch(0);
                if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(t.fingerId))
                    return;
                isRightSide = t.position.x >= Screen.width * 0.5f;
                if (t.phase == TouchPhase.Began) touchDown = true;
                if (t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled) touchUp = true;
            }
            if (!touchDown && Input.GetMouseButtonDown(0))
            {
                if (EventSystem.current == null || !EventSystem.current.IsPointerOverGameObject())
                {
                    touchDown = true;
                    isRightSide = Input.mousePosition.x >= Screen.width * 0.5f;
                }
            }
            if (!touchUp && Input.GetMouseButtonUp(0))
            {
                if (EventSystem.current == null || !EventSystem.current.IsPointerOverGameObject())
                    touchUp = true;
            }

            if (touchDown)
            {
                m_TouchDownTime = Time.time;
                m_TouchBeganOnRightSide = isRightSide;
            }
            if (touchUp && m_TouchDownTime >= 0f)
            {
                float holdDuration = Time.time - m_TouchDownTime;
                m_TouchDownTime = -1f;
                bool treatAsSprintOnly = enableSprintOnRightSide && m_TouchBeganOnRightSide && holdDuration >= SprintHoldThreshold;
                m_TouchBeganOnRightSide = false;
                if (treatAsSprintOnly)
                    return;
                RedCharacter red = null;
                if (gm != null && gm.MainCharacter != null)
                    red = gm.MainCharacter as RedCharacter;
                if (red == null)
                    red = FindFirstObjectByType<RedCharacter>(FindObjectsInactive.Include);

                if (red != null)
                {
                    float baseStrength = red.JumpStrength;
                    float t = Mathf.Clamp01((holdDuration - minHoldTime) / Mathf.Max(0.01f, maxHoldTime - minHoldTime));
                    float strength = Mathf.Lerp(baseStrength * 0.5f, baseStrength * 1.5f, t);
                    red.Jump(strength);
                }
                CrossPlatformInputManager.SetButtonDown("Jump");
                CrossPlatformInputManager.SetButtonUp("Jump");
            }
        }

        private void HandleMobileGameplayInput(RedRunner.GameManager gm)
        {
            bool sprintHeldThisFrame = false;
            float rightHalf = Screen.width * 0.5f;

            for (int i = 0; i < Input.touchCount; i++)
            {
                Touch touch = Input.GetTouch(i);
                if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(touch.fingerId))
                    continue;

                bool rightSide = touch.position.x >= rightHalf;
                if (rightSide && (touch.phase == TouchPhase.Began || touch.phase == TouchPhase.Moved || touch.phase == TouchPhase.Stationary))
                    sprintHeldThisFrame = true;

                if (touch.phase == TouchPhase.Began)
                    JumpMainCharacter(gm, "[MobileInput] 游戏中触摸开始，执行跳跃。");
            }

            if (sprintHeldThisFrame && !m_MobileSprintHeld)
            {
                m_MobileSprintHeld = true;
                CrossPlatformInputManager.SetButtonDown("Sprint");
            }
            else if (!sprintHeldThisFrame && m_MobileSprintHeld)
            {
                m_MobileSprintHeld = false;
                CrossPlatformInputManager.SetButtonUp("Sprint");
            }
        }

        private void HandleHomeTapStart(RedRunner.GameManager gm)
        {
            if (gm == null || Input.touchCount <= 0)
                return;

            for (int i = 0; i < Input.touchCount; i++)
            {
                Touch touch = Input.GetTouch(i);
                if (touch.phase != TouchPhase.Began)
                    continue;

                if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(touch.fingerId))
                    continue;

                if (UIManager.Singleton != null)
                {
                    var inGameScreen = UIManager.Singleton.GetUIScreen(UIScreenInfo.IN_GAME_SCREEN);
                    if (inGameScreen != null)
                        UIManager.Singleton.OpenScreen(inGameScreen);
                }

                gm.StartGame();
                Debug.Log("[MobileInput] 首页触摸已开始游戏。");
                JumpMainCharacter(gm, "[MobileInput] 首页触摸开始游戏后执行首跳。");

                break;
            }
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
