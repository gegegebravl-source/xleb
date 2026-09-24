using CHARK.GameManagement.Systems;
using UABPetelnia.GGJ2025.Runtime.Actors;
using UABPetelnia.GGJ2025.Runtime.Settings;
using UnityEngine;

namespace UABPetelnia.GGJ2025.Runtime.Systems.Products
{
    /// <summary>
    /// Shelves are intentionally empty by default. The player or a delivery-box interaction fills
    /// them manually when the back-room stock arrives.
    /// </summary>
    internal interface IProductSystem : ISystem
    {
        /// <summary>
        /// <c>true</c> when at least one shelf slot exists in the loaded scene.
        /// </summary>
        public bool IsShelfAvailable { get; }

        public void AddShelfPoint(ProductShelfPointActor shelfPoint);

        public void RemoveShelfPoint(ProductShelfPointActor shelfPoint);

        public ProductActor SpawnProduct(ItemData item, ProductShelfPointActor shelfPoint);

        /// <summary>
        /// Создать товар вне полки: он ждёт в руках игрока и на полку попадёт только когда
        /// игрок его отпустит рядом со свободным слотом.
        /// </summary>
        public ProductActor SpawnLooseProduct(ItemData item, Vector3 position);

        /// <summary>
        /// Высота товара на полке: размер из ассета с общим множителем и границами.
        /// </summary>
        public float ResolveDisplayHeight(float displayHeight);

        /// <summary>Сколько единиц товара сейчас физически стоит на полках.</summary>
        public int CountOnShelf(ItemData item);

        /// <summary>
        /// Ближайший свободный слот в радиусе от точки: товар «примагничивается» к полке,
        /// если игрок отпустил его рядом. Предпочтение — слот по направлению взгляда.
        /// </summary>
        public bool TryFindFreeShelfPoint(Vector3 position, Vector3 lookDirection, float radius, out ProductShelfPointActor shelfPoint);

        /// <summary>
        /// Fill a specific empty shelf from the back-room stock. This is the manual delivery-to-shelf
        /// flow: the box arrives, the player gets stock in, then this method applies the goods.
        /// </summary>
        public bool TryManualRestockShelf(ProductShelfPointActor shelfPoint);

        /// <summary>
        /// <c>true</c> when a unit of <paramref name="item"/> is physically out on a shelf, and
        /// therefore something the player can pick up and hand over.
        /// </summary>
        public bool IsOnShelf(ItemData item);

        /// <summary>
        /// Зарегистрировать товар в сцене. Товар может быть и заспавнен системой, и расставлен
        /// вручную в редакторе — в обоих случаях он должен считаться доступным для продажи.
        /// </summary>
        public void Register(ProductActor product);

        /// <summary>Убрать товар из учёта (объект выключен или уничтожен).</summary>
        public void Unregister(ProductActor product);

        /// <summary>
        /// The product left the shop. The shelf stays empty until someone manually refills it.
        /// </summary>
        public void Consume(ProductActor product);
    }
}
