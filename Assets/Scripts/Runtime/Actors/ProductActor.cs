using CHARK.GameManagement;
using UABPetelnia.GGJ2025.Runtime.Settings;
using UABPetelnia.GGJ2025.Runtime.Systems.Products;
using UnityEngine;

namespace UABPetelnia.GGJ2025.Runtime.Actors
{
    /// <summary>
    /// A single physical product on a shelf. Grabbing it is handled by the generic grab
    /// interaction, so this actor only knows which <see cref="ItemData"/> it represents and how
    /// it renders.
    /// </summary>
    internal sealed class ProductActor : MonoBehaviour, IProductActor
    {
        [Header("General")]
        [SerializeField]
        private ItemData item;

        [Header("Rendering")]
        [SerializeField]
        private Renderer imageRenderer;

        [SerializeField]
        private string texturePropertyId = "_BaseMap";

        [Header("Drop magnet")]
        [Tooltip("Радиус, в котором отпущенный товар встаёт на свободную полку.")]
        [Min(0.05f)]
        [SerializeField]
        private float magnetRadius = 0.3f;

        [Tooltip("Сколько секунд товар летит в слот.")]
        [Min(0.01f)]
        [SerializeField]
        private float magnetDuration = 0.16f;

        private IProductSystem productSystem;
        private Rigidbody body;
        private Coroutine magnetRoutine;

        /// <summary>Рендер тонкой обводки (дочерний квад у картинки товара).</summary>
        private Renderer outlineRenderer;

        public ItemData Item => item;

        /// <summary>Высота товара в метрах после общих правил размера.</summary>
        public float Height { get; private set; } = 0.25f;

        /// <summary><c>true</c>, пока товар летит в слот после отпускания.</summary>
        public bool IsFlyingToShelf => magnetRoutine != null;

        /// <summary>
        /// Shelf slot this product was spawned on. Used to put a replacement back in place.
        /// </summary>
        public ProductShelfPointActor ShelfPoint { get; private set; }

        private void Awake()
        {
            productSystem = ResolveProductSystem();
            body = GetComponent<Rigidbody>();
        }

        private void OnEnable()
        {
            // Товар сам сообщает о себе системе: так работают и заспавненные товары,
            // и те, что расставлены в сцене вручную (их может быть много на одну полку).
            ResolveProductSystem()?.Register(this);
        }

        private void OnDisable()
        {
            if (magnetRoutine != null)
            {
                StopCoroutine(magnetRoutine);
                magnetRoutine = null;
            }

            productSystem?.Unregister(this);
        }

        private IProductSystem ResolveProductSystem()
        {
            if (productSystem != null)
            {
                return productSystem;
            }

            // Слот полки будит товар раньше, чем сработает Awake, поэтому систему ищем лениво.
            UABPetelnia.GGJ2025.Runtime.Utilities.SystemsUtility.TryGetSystem(out productSystem);

            return productSystem;
        }

        /// <summary>
        /// Создать товар из префаба или принять уже стоящий в сцене.
        /// </summary>
        /// <param name="snapToShelfPoint">
        /// <c>true</c> — выровнять товар по точке полки (для заспавненных),
        /// <c>false</c> — оставить авторскую расстановку из редактора.
        /// </param>
        public void Initialize(ItemData data, ProductShelfPointActor shelfPoint, bool snapToShelfPoint = true)
        {
            item = data;
            ShelfPoint = shelfPoint;

            if (item)
            {
                gameObject.name = $"Actor_Product_{item.Id}";
            }

            if (item == false)
            {
                return;
            }

            if (snapToShelfPoint == false)
            {
                // Товар расставлен в сцене вручную: сохраняем и позицию, и масштаб автора.
                // Высота нужна только для отступа при выкладке на полку.
                Height = Mathf.Max(0.01f, transform.localScale.x);
            }
            else
            {
                // Картинка — метровый квад: масштаб задаёт размер товара на полке. Общие правила
                // размера живут в системе товаров, чтобы мелочь не терялась, а гигант не заслонял полку.
                var system = ResolveProductSystem();
                Height = system != null ? system.ResolveDisplayHeight(item.DisplayHeight) : item.DisplayHeight;
                if (IsFinite(Height) == false || Height <= 0f)
                {
                    Height = 0.25f;
                }

                transform.localScale = Vector3.one * Height;

                if (shelfPoint)
                {
                    // Товар стоит на поверхности полки, а не висит по центру картинки.
                    transform.SetPositionAndRotation(
                        shelfPoint.Position + Vector3.up * (Height * 0.5f),
                        shelfPoint.Rotation
                    );
                }
            }

            if (imageRenderer == false)
            {
                return;
            }

            var block = new MaterialPropertyBlock();
            block.SetTexture(texturePropertyId, item.Image);
            imageRenderer.SetPropertyBlock(block);

            // Обводка строится по силуэту картинки, поэтому ей нужна та же текстура.
            ApplyOutlineTexture();
        }

        /// <summary>Передать текстуру предмета в рендер обводки.</summary>
        private void ApplyOutlineTexture()
        {
            if (outlineRenderer == false && imageRenderer != false)
            {
                var outlineTransform = imageRenderer.transform.Find("Outline");

                if (outlineTransform != false)
                {
                    outlineRenderer = outlineTransform.GetComponent<Renderer>();
                }
            }

            if (outlineRenderer == false || item == false)
            {
                return;
            }

            var outlineBlock = new MaterialPropertyBlock();
            outlineBlock.SetTexture(texturePropertyId, item.Image);
            outlineRenderer.SetPropertyBlock(outlineBlock);
        }

        /// <summary>
        /// Hand the product over. It leaves the shelf and a replacement is spawned in its place so
        /// the shop keeps offering the whole assortment.
        /// </summary>
        public void Consume()
        {
            var system = ResolveProductSystem();
            if (system != null)
            {
                system.Consume(this);
                return;
            }

            // A broken standalone scene must not leave a sold product available for a second sale.
            Destroy(gameObject);
        }

        /// <summary>
        /// Put the product back where it was taken from. Products have no physics, so without this
        /// a released good would stay floating wherever the player let go of it.
        /// </summary>
        public void ReturnToShelf()
        {
            if (ShelfPoint == false)
            {
                return;
            }

            SetKinematic(true);

            transform.SetPositionAndRotation(
                ShelfPoint.Position + Vector3.up * (Height * 0.5f),
                ShelfPoint.Rotation
            );
        }

        /// <summary>
        /// Встать на указанный слот: товар выравнивается по поверхности полки и перестаёт
        /// быть физическим телом.
        /// </summary>
        public void PlaceOn(ProductShelfPointActor shelfPoint)
        {
            var previous = ShelfPoint;

            if (previous != false && previous != shelfPoint && previous.Product == this)
            {
                previous.Product = default;
            }

            ShelfPoint = shelfPoint;

            if (shelfPoint == false)
            {
                // Товар покинул полку (взята в руку или брошена на пол): отцепляем от слота.
                // Иначе спрятанный в инвентаре предмет остаётся ребёнком полки, визуально
                // «живёт» на её месте и попадает в перебор детей при переактивации полки.
                transform.SetParent(null, worldPositionStays: true);

                return;
            }

            shelfPoint.Product = this;

            transform.SetParent(shelfPoint.transform, worldPositionStays: true);
            ReturnToShelf();
        }

        /// <summary>
        /// Отпустить товар из рук: товар встаёт на полку только если его бросили прямо на место —
        /// слот свободен, лежит по направлению взгляда и подходит по размеру. Иначе — падает.
        /// </summary>
        public void Drop(Vector3 lookDirection)
        {
            var system = ResolveProductSystem();

            if (system != null
                && system.TryFindFreeShelfPoint(
                    transform.position,
                    lookDirection,
                    IsFinite(magnetRadius) ? Mathf.Max(0.05f, magnetRadius) : 0.3f,
                    out var shelfPoint)
                && IsGoodPlacement(shelfPoint, lookDirection))
            {
                StartMagnet(shelfPoint);

                return;
            }

            // Свободного места рядом нет: товар падает и остаётся лежать в мире.
            SetKinematic(false);
        }

        /// <summary>
        /// Проверка посадки: слот под прицелом (не сбоку и не за спиной) и товар влезает по размеру.
        /// </summary>
        private bool IsGoodPlacement(ProductShelfPointActor shelfPoint, Vector3 lookDirection)
        {
            if (shelfPoint == false || shelfPoint.IsFree == false || shelfPoint.CanFit(Height) == false)
            {
                return false;
            }

            var toSlot = shelfPoint.Position - transform.position;
            var distance = toSlot.magnitude;

            if (distance > magnetRadius)
            {
                return false;
            }

            // Товар должен лежать именно по направлению взгляда: около 45 градусов и точнее.
            if (lookDirection.sqrMagnitude > 0.001f && distance > 0.001f)
            {
                var alignment = Vector3.Dot(toSlot / distance, lookDirection.normalized);

                if (alignment < 0.72f)
                {
                    return false;
                }
            }

            return true;
        }

        private void StartMagnet(ProductShelfPointActor shelfPoint)
        {
            if (shelfPoint == false)
            {
                SetKinematic(false);
                return;
            }

            if (magnetRoutine != null)
            {
                StopCoroutine(magnetRoutine);
            }

            magnetRoutine = StartCoroutine(MagnetRoutine(shelfPoint));
        }

        private System.Collections.IEnumerator MagnetRoutine(ProductShelfPointActor shelfPoint)
        {
            if (shelfPoint == false)
            {
                SetKinematic(false);
                yield break;
            }

            SetKinematic(true);

            var startPosition = transform.position;
            var startRotation = transform.rotation;
            var targetPosition = shelfPoint.Position + Vector3.up * (Height * 0.5f);
            var targetRotation = shelfPoint.Rotation;
            var elapsed = 0f;

            var duration = IsFinite(magnetDuration) ? Mathf.Max(0.01f, magnetDuration) : 0.16f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;

                var t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
                transform.SetPositionAndRotation(
                    Vector3.Lerp(startPosition, targetPosition, t),
                    Quaternion.Slerp(startRotation, targetRotation, t)
                );

                yield return null;
            }

            magnetRoutine = null;

            if (shelfPoint == false)
            {
                SetKinematic(false);
                yield break;
            }

            PlaceOn(shelfPoint);
        }

        private static bool IsFinite(float value)
        {
            return float.IsNaN(value) == false && float.IsInfinity(value) == false;
        }

        private void SetKinematic(bool isKinematic)
        {
            if (body == false)
            {
                body = GetComponent<Rigidbody>();
            }

            if (body == false)
            {
                return;
            }

            body.isKinematic = isKinematic;

            if (isKinematic)
            {
                body.linearVelocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
            }
        }
    }
}
