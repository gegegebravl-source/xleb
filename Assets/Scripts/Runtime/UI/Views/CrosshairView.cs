using CHARK.SimpleUI;
using UnityEngine;
using UnityEngine.UI;

namespace UABPetelnia.GGJ2025.Runtime.UI.Views
{
    /// <summary>
    /// Прицел по центру экрана: маленькая точка, которая подрастает и подсвечивается,
    /// когда под ней есть что взять или с кем поговорить.
    /// </summary>
    [DisallowMultipleComponent]
    internal sealed class CrosshairView : View
    {
        #region Serialized

        [Header("Dot")]
        [Tooltip("Картинка точки прицела.")]
        [SerializeField] private Image dot;

        [Header("Idle")]
        [SerializeField] private Color idleColor = new(1f, 1f, 1f, 0.5f);

        [Min(1f)]
        [SerializeField] private float idleSize = 7f;

        [Header("Hover")]
        [SerializeField] private Color hoverColor = new(0.99f, 0.80f, 0.36f, 0.95f);

        [Min(1f)]
        [SerializeField] private float hoverSize = 14f;

        [Min(1f)]
        [Tooltip("Скорость, с которой точка догоняет целевой размер.")]
        [SerializeField] private float damping = 16f;

        #endregion

        #region Cached state

        private RectTransform dotRect;
        private float targetSize;
        private Color targetColor;

        #endregion

        #region Unity lifecycle

        protected override void Awake()
        {
            base.Awake();

            dotRect = dot != null ? dot.rectTransform : null;
            targetSize = idleSize;
            targetColor = idleColor;

            Apply(instant: true);
        }

        private void Update()
        {
            Apply(instant: false);
        }

        #endregion

        #region Public API

        /// <summary>Подсветить прицел: под ним есть интерактивный объект.</summary>
        public void SetHover(bool isHovering)
        {
            targetSize = isHovering ? hoverSize : idleSize;
            targetColor = isHovering ? hoverColor : idleColor;
        }

        #endregion

        #region Rendering

        private void Apply(bool instant)
        {
            if (dot == null)
            {
                return;
            }

            if (instant)
            {
                dot.color = targetColor;

                if (dotRect != null)
                {
                    dotRect.sizeDelta = new Vector2(targetSize, targetSize);
                }

                return;
            }

            var t = 1f - Mathf.Exp(-damping * Time.unscaledDeltaTime);
            dot.color = Color.Lerp(dot.color, targetColor, t);

            if (dotRect != null)
            {
                var size = Mathf.Lerp(dotRect.sizeDelta.x, targetSize, t);
                dotRect.sizeDelta = new Vector2(size, size);
            }
        }

        #endregion
    }
}
