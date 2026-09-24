using UnityEngine;

namespace UABPetelnia.GGJ2025.Runtime.Components.Utilities
{
    /// <summary>
    /// Проигрывает спрайт-лист: кадры лежат в текстуре сеткой (столбцы × ряды),
    /// а компонент сдвигает <c>_BaseMap_ST</c> и показывает по одному кадру за раз.
    /// </summary>
    /// <remarks>
    /// Отдельные текстуры под каждый кадр не нужны: анимация живёт в одном материале, поэтому
    /// десяток кадров не превращается в десяток ассетов. Материал должен быть с отсечением
    /// прозрачных пикселей, иначе кадры будут видны прямоугольниками.
    /// </remarks>
    [DisallowMultipleComponent]
    internal sealed class SpriteSheetAnimator : MonoBehaviour
    {
        private static readonly int BaseMapStId = Shader.PropertyToID("_BaseMap_ST");

        [Header("Target")]
        [SerializeField]
        private Renderer targetRenderer;

        [Tooltip("Текстура-лист. Задаётся аниматором: товар на полке может подменить текстуру своей картинкой.")]
        [SerializeField]
        private Texture2D sheet;

        [SerializeField]
        private string texturePropertyId = "_BaseMap";

        [Header("Sheet")]
        [Tooltip("Сколько кадров в одном ряду текстуры (столбцов).")]
        [Min(1)]
        [SerializeField]
        private int frameCount = 4;

        [Tooltip("Сколько рядов кадров в текстуре. Кадры идут сверху вниз.")]
        [Min(1)]
        [SerializeField]
        private int rows = 1;

        [Min(0.05f)]
        [SerializeField]
        private float framesPerSecond = 6f;

        [SerializeField]
        private bool playOnStart = true;

        private MaterialPropertyBlock block;
        private int frameIndex;
        private int lastAppliedFrame = -1;
        private float nextFrameTimeSeconds;

        public int FrameCount => frameCount;

        /// <summary>
        /// Настроить проигрывание из редакторных инструментов.
        /// </summary>
        public void Initialize(
            Renderer renderer,
            Texture2D sheetTexture,
            int frames,
            float framesPerSecondValue,
            int rowsCount = 1
        )
        {
            targetRenderer = renderer;
            sheet = sheetTexture;
            frameCount = Mathf.Max(1, frames);
            framesPerSecond = Mathf.Max(0.05f, framesPerSecondValue);
            rows = Mathf.Max(1, rowsCount);
        }

        private void Awake()
        {
            block = new MaterialPropertyBlock();

            if (targetRenderer == false)
            {
                targetRenderer = GetComponentInChildren<Renderer>(true);
            }
        }

        private void OnEnable()
        {
            lastAppliedFrame = -1;
            Apply(0);
        }

        private void Update()
        {
            if (playOnStart == false || frameCount <= 1 || targetRenderer == false)
            {
                return;
            }

            if (Time.time < nextFrameTimeSeconds)
            {
                return;
            }

            frameIndex = (frameIndex + 1) % Mathf.Max(1, frameCount * rows);
            nextFrameTimeSeconds = Time.time + (1f / framesPerSecond);

            Apply(frameIndex);
        }

        private void Apply(int index)
        {
            if (targetRenderer == false || frameCount <= 1 && rows <= 1 || index == lastAppliedFrame)
            {
                return;
            }

            // Блок свойств может быть ещё не создан, если OnEnable обогнал Awake
            // (например, при включении объекта без перезагрузки домена).
            block ??= new MaterialPropertyBlock();

            lastAppliedFrame = index;

            // Кадры идут сеткой: сначала строка слева направо, потом следующая.
            var total = Mathf.Max(1, frameCount * rows);
            var clamped = ((index % total) + total) % total;
            var column = clamped % frameCount;
            var row = clamped / frameCount;

            // V-координата текстуры растёт снизу вверх, поэтому верхний ряд — последний по V.
            var scale = new Vector4(
                1f / frameCount,
                1f / rows,
                column / (float)frameCount,
                1f - (row + 1f) / rows
            );

            targetRenderer.GetPropertyBlock(block);

            if (sheet)
            {
                block.SetTexture(texturePropertyId, sheet);
            }

            block.SetVector(BaseMapStId, scale);
            targetRenderer.SetPropertyBlock(block);
        }
    }
}
