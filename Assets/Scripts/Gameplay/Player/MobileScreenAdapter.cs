using UnityEngine;

namespace RedRunner.Gameplay.Player
{
    /// <summary>
    /// 手机端画面自适应：按屏幕高度缩放相机视野，不同分辨率下游戏内容大小一致、不裁切。
    /// 挂到 Play 场景的 Main Camera 或任意常驻物体上（会自动找 Main Camera）。
    /// </summary>
    [DefaultExecutionOrder(-50)]
    public class MobileScreenAdapter : MonoBehaviour
    {
        [Tooltip("设计时相机的 Orthographic Size（不填则用当前 Main Camera 的值）")]
        public float designOrthoSize = -1f;

        [Tooltip("仅在真机生效；取消勾选后可在编辑器 Game 视图预览手机效果")]
        public bool onlyOnMobile = true;

        private Camera m_Camera;
        private float m_AppliedOrtho;
        private float m_DesignOrtho;
        private int m_LastWidth;
        private int m_LastHeight;

        private void Start()
        {
            m_Camera = Camera.main;
            if (m_Camera == null)
                m_Camera = GetComponent<Camera>();
            if (m_Camera == null)
            {
                Debug.LogWarning("MobileScreenAdapter: No camera found.");
                return;
            }
            if (!m_Camera.orthographic)
            {
                Debug.LogWarning("MobileScreenAdapter: Camera is not orthographic.");
                return;
            }

            m_DesignOrtho = designOrthoSize > 0f ? designOrthoSize : m_Camera.orthographicSize;
            m_LastWidth = Screen.width;
            m_LastHeight = Screen.height;
            bool apply = Application.isMobilePlatform || (!onlyOnMobile && Application.isEditor);
            if (apply) ApplyOrthoSize();
        }

        private void Update()
        {
            if (m_Camera == null || !m_Camera.orthographic) return;
            bool apply = Application.isMobilePlatform || (!onlyOnMobile && Application.isEditor);
            if (!apply) return;
            if (Screen.width != m_LastWidth || Screen.height != m_LastHeight)
            {
                m_LastWidth = Screen.width;
                m_LastHeight = Screen.height;
                ApplyOrthoSize();
            }
        }

        private void ApplyOrthoSize()
        {
            if (m_Camera == null || !m_Camera.orthographic) return;
            float design = m_DesignOrtho > 0f ? m_DesignOrtho : m_Camera.orthographicSize;
            m_AppliedOrtho = MobileUiAdaptation.GetOrthographicSizeForPhone(design, Screen.width, Screen.height);
            m_Camera.orthographicSize = m_AppliedOrtho;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (designOrthoSize == 0f)
                designOrthoSize = -1f;
        }
#endif
    }
}
