using UABPetelnia.GGJ2025.Runtime.Constants;
using UnityEngine;

namespace UABPetelnia.GGJ2025.Runtime.Settings
{
    [CreateAssetMenu(
        fileName = CreateAssetMenuConstants.BaseFileName + nameof(ItemData),
        menuName = CreateAssetMenuConstants.BaseMenuName + "/Item Data",
        order = CreateAssetMenuConstants.BaseOrder
    )]
    internal sealed class ItemData : ScriptableObject
    {
        [SerializeField]
        private int price;

        [Tooltip("Русское название для панелей покупателя. Пусто — берётся id ассета.")]
        [SerializeField]
        private string displayName;

        [SerializeField]
        private Texture2D image;

        [Tooltip("Высота товара в метрах. По ней масштабируется картинка товара на полке.")]
        [Min(0.02f)]
        [SerializeField]
        private float displayHeight = 0.22f;

        public string Id
        {
            get
            {
                if (image)
                {
                    return image.name;
                }

                return name;
            }
        }

        public int Cents => price;

        /// <summary>Русское отображаемое имя товара (пусто — показываем id).</summary>
        public bool HasDisplayName => !string.IsNullOrWhiteSpace(displayName);

        public string DisplayName => displayName;

        public Texture2D Image => image;

        /// <summary>
        /// Size of the product on the shelf, in meters. Keeps a chewing gum pack small and a
        /// bottle tall instead of drawing everything at one fixed size.
        /// </summary>
        public float DisplayHeight => displayHeight;
    }
}
