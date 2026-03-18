using UnityEngine;

namespace RedRunner.Gameplay.Player
{
    /// <summary>
    /// 根据状态切换猫咪精灵：run(1-4 循环)、jump、hurt(死亡/受伤)。
    /// 需在 Editor 中或通过 ApplyCollectiblesArt 赋好 Run/Jump/Hurt 数组。
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class CatSpriteSwitcher : MonoBehaviour
    {
        [Header("Cat sprites (assign from Assets/Art/Collectibles)")]
        [Tooltip("Run 1,2,3,4... 按顺序循环")]
        public Sprite[] RunSprites = new Sprite[0];
        [Tooltip("Jump 1,2,3,4... 跳跃时显示")]
        public Sprite[] JumpSprites = new Sprite[0];
        [Tooltip("Hurt 1,2,3,4... 受伤/死亡时显示")]
        public Sprite[] HurtSprites = new Sprite[0];

        [Header("Optional: use Animator on same object to read state")]
        [SerializeField] private Animator m_Animator;

        private SpriteRenderer m_Renderer;
        private float m_RunCycle;
        private const float RunCycleSpeed = 8f;

        private void Awake()
        {
            m_Renderer = GetComponent<SpriteRenderer>();
            if (m_Animator == null)
                m_Animator = GetComponent<Animator>();
        }

        private void LateUpdate()
        {
            if (m_Renderer == null || !m_Renderer.enabled) return;

            bool isDead = false;
            bool isGrounded = true;
            if (m_Animator != null)
            {
                isDead = m_Animator.GetBool("IsDead");
                isGrounded = m_Animator.GetBool("IsGrounded");
            }

            if (isDead && HurtSprites != null && HurtSprites.Length > 0)
            {
                m_Renderer.sprite = HurtSprites[0];
                return;
            }

            if (!isGrounded && JumpSprites != null && JumpSprites.Length > 0)
            {
                m_Renderer.sprite = JumpSprites[0];
                return;
            }

            if (RunSprites != null && RunSprites.Length > 0)
            {
                m_RunCycle += RunCycleSpeed * Time.deltaTime;
                int index = Mathf.FloorToInt(m_RunCycle) % RunSprites.Length;
                m_Renderer.sprite = RunSprites[index];
            }
        }
    }
}
