using CHARK.GameManagement;
using UABPetelnia.GGJ2025.Runtime.Systems.Products;
using UABPetelnia.GGJ2025.Runtime.Utilities;
using UnityEngine;

namespace UABPetelnia.GGJ2025.Runtime.Actors
{
    /// <summary>
    /// A single slot on a kiosk shelf. Products are spawned here, so placing these markers inside
    /// the kiosk is all that is needed to fill it with sellable goods.
    /// </summary>
    internal sealed class ProductShelfPointActor : MonoBehaviour
    {
        private IProductSystem productSystem;

        public Vector3 Position => transform.position;

        public Quaternion Rotation => transform.rotation;

        /// <summary>
        /// One representative product currently spawned on this shelf point, if any. A scene may
        /// contain several authored products under one marker; when the representative is removed,
        /// the next remaining child becomes the representative instead of leaving the point stuck
        /// occupied or incorrectly free.
        /// </summary>
        private ProductActor product;

        public ProductActor Product
        {
            get
            {
                if (product == false)
                {
                    product = FindChildProduct();
                }

                return product;
            }
            set
            {
                product = value;

                if (product == false)
                {
                    product = FindChildProduct();
                }
            }
        }

        /// <summary><c>true</c>, когда слот свободен и на него можно поставить товар.</summary>
        public bool IsFree => Product == false;

        [Header("Посадка")]
        [Tooltip("Максимальная высота товара, который влезает в этот слот, в метрах.")]
        [Min(0.05f)]
        [SerializeField]
        private float capacity = 0.55f;

        /// <summary>Влезает ли товар такой высоты в этот слот.</summary>
        public bool CanFit(float height)
        {
            return height <= capacity;
        }

        /// <summary>Максимальная высота товара для этого слота.</summary>
        public float Capacity => capacity;


        private ProductActor FindChildProduct()
        {
            var products = GetComponentsInChildren<ProductActor>(true);

            for (var index = 0; index < products.Length; index++)
            {
                if (products[index] != false)
                {
                    return products[index];
                }
            }

            return default;
        }

        private void Awake()
        {
            SystemsUtility.TryGetSystem(out productSystem);
        }

        private void OnEnable()
        {
            // Системы могут подняться позже сцены — тогда ищем их повторно, иначе слот
            // останется невидимым для магнита и покупателей.
            if (productSystem == null)
            {
                SystemsUtility.TryGetSystem(out productSystem);
            }

            // Принимаем ВСЕ товары слота (их может быть несколько — игрок расставляет вручную)
            // и НЕ двигаем их: авторская расстановка важнее авто-выравнивания.
            var bakedProducts = GetComponentsInChildren<ProductActor>(true);

            for (var index = 0; index < bakedProducts.Length; index++)
            {
                var bakedProduct = bakedProducts[index];

                if (bakedProduct == false)
                {
                    continue;
                }

                bakedProduct.Initialize(bakedProduct.Item, this, snapToShelfPoint: false);

                if (Product == false)
                {
                    Product = bakedProduct;
                }
            }

            productSystem?.AddShelfPoint(this);
        }

        private void OnDisable()
        {
            productSystem?.RemoveShelfPoint(this);
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(Position, Vector3.one * 0.15f);
        }
    }
}
