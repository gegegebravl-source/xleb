---
id: kd_builtin_memory_project_mistake_note
injectMode: full
injectAgents:
- unity
aiEditMode: auto
maintenanceRules: |-
  - Record only verified problems, rework causes, and avoidance steps
  - Prioritize recurring pitfalls, constraints, regression points, and confirmed fixes
  - Keep each entry short and focused on one lesson or constraint
  - Keep the list within 20 items and merge duplicates regularly
  - Remove outdated issues, non-reproducible issues, and unsupported guesses
---

- TMP-тексты на выключенных панелях не проходят Awake: запись outlineWidth/материала падает с NRE внутри TMP (m_canvasRenderer/m_sharedMaterial не инициализированы). Фильтр — `activeInHierarchy` + повторная тема в ShowPanel (см. TryApplyOutline в MainMenuView.cs).
- Бутстрапу нужен только AudioListener, а не камера: созданная «резервная камера» в (0,0,0) с depth 0 перекрывает камеру игрока (depth -1) — игрок видел сцену «из пола». Слушатель пересаживать в Update за Camera.main; корутины при инициализации до активации объекта ненадёжны.
- Missing script на GameObject блокирует SaveAsPrefabAsset («Error while saving Prefab»), при этом RemoveMonoBehavioursWithMissingScript чистит только один GameObject — обходить GetComponentsInChildren по всем детям; ошибка видна только в консоли.
- unity_yaml_read может показывать скаляр там, где в живом редакторе SerializeReference-массив (случай с m_LayerCollisionMatrix): перед «починкой» настроек проверять реальную структуру через SerializedObject в редакторе.
- Калькулятор (Actor_Calculator) обязан оставаться на слое 0: рейкаст захвата бьёт только по слою 6 (Interactables), перевод коллайдера на слой 6 начнёт блокировать захват товаров.
- FMOD .meta плагинов с serializedVersion: 1 вызывают спам варнингов; SaveAndReimport их не обновляет для чужих платформ — обновлять формат meta до v3 (externalObjects/defineConstraints/isExplicitlyReferenced/validateReferences) текстово.
- В проекте нет git: любые удаления контента сцен делать только после явной проверки, предпочтительно выключение вместо удаления.
- Спрайты проекта RGBA с прозрачным фоном, но прозрачные пиксели записаны чёрным: в непрозрачном материале URP/Unlit фон рисуется чёрным квадратом. Лечится `_AlphaClip=1` + `_Cutoff` + keyword `_ALPHATEST_ON` + queue 2450 (инструмент `KioskShelfGridBuilder.EnableAlphaClipOnSpriteMaterials`). Важно: новые материалы товаров создаются через `new Material(shader)` и НЕ наследуют настройки шаблона `Product_Image.mat` — отсечение надо ставить явно (см. `BakeWarmBreadScene.EnsureProductMaterial`).
- Каталог магазина (45 товаров) и число полок в сцене независимы: при 15 `ProductShelfPointActor` в игре видно только 15 картинок. Сетка 15×3 закрывает весь ассортимент — `KioskShelfGridBuilder.BuildFullAssortment` (переиспользует существующие полки, лишние выключает, а не удаляет).
- Анимации предметов — горизонтальные листы кадров внутри одной картинки (пейджер 4, тетрис 4, тамагочи 2–3, у телевизора 8 отдельных картинок-каналов). Проигрываются сдвигом `_BaseMap_ST` (`SpriteSheetAnimator`), но `ProductActor.Initialize` перезаписывает `_BaseMap` товара, поэтому аниматор обязан задавать и текстуру, иначе кадр покажет четверть картинки.
- Runtime-скрипты проекта собираются в сборку UABPetelnia.GGJ2025 (Assets/Scripts/Runtime/UABPetelnia.GGJ2025.asmdef), а не Assembly-CSharp: Type.GetType("...", "Assembly-CSharp") вернёт null — внутренние типы резолвить перебором AppDomain.CurrentDomain.GetAssemblies().
- Квады-«билборды» должны разворачиваться нормалью меша, а не transform.forward: у встроенного Quad видимая сторона — −Z, поэтому Billboard, наводивший forward на камеру, показывал обратную грань и односторонние URP-материалы (_Cull=2) отсекали товары, ПК и декор киоска в игре (в редакторе Billboard не работает — объекты видны). Диагностика: dot(мировая нормаль, направление на камеру) > 0; сверять с уже работающими объектами (у них isFlipDirection = 1).
- Unity YAML пишет кириллицу в .asset как \uXXXX-эскейпы: обычный grep по русскому тексту («Добрый») ничего не находит, хотя строка есть в файле. Искать нужно после декода эскейпов (re.sub(r'\\u([0-9a-f]{4})', …)) либо править текст через SerializedObject/Unity API, а не текстово.
- Второстепенный MonoBehaviour-класс внутри чужого .cs (не первый в файле) при AddComponent из editor-билдера сохраняется в префаб с m_Script: {fileID: 0} — ссылка на компонент после загрузки = null, без ошибок в консоли. Правило: каждый MonoBehaviour — в своём файле (как DeliveryBoxRowView.cs). Диагностика: grep "m_Script: {fileID: 0}" по префабу.
- Атлас TMP-шрифта Font_Tiny5 не содержит кириллицу (только Latin): весь русский текст рендерится фолбэком LiberationSans. TryAddCharacters не работает (sourceFontFile у ассета потерян). Лечение: font.sourceFontFile = TTF + atlasPopulationMode = Dynamic — глифы добираются на лету.
- У товара-пропа (плеер, тамагочи) сущности в сцене нет до старта плей-режима: полки в редакторе пустые, ProductSystem спавнит товары из каталога при старте. Постоянный интерактивный проп — запекать в сцену (инстанс Actor_Product + Initialize(item, shelf, snapToShelfPoint: true) + маркер-компонент).
- Physics.SphereCastNonAlloc в Unity 6 сыплет ассертами «IsNormalized(direction)» на ненормализованном направлении: передавать forward без умножения на дистанцию, длину луча задаёт maxDistance. Старый код с SphereCast это молча прощал.
- Динамическому TMP-атласу нужна Read/Write текстура атласа (m_IsReadable): без неё новые символы («…», редкая кириллица) не добавляются — спам «Unable to add the requested character» и квадраты/фолбэк вместо глифа. Лечится SerializedObject по m_IsReadable на Texture2D атласа.
- Мутатор структуры через `Action<SettingsData>` теряет изменения: лямбда мутирует КОПИЮ структуры, оригинал не меняется; события при этом стреляют и ошибок нет — выглядит как «обработчик вызван, но ничего не сделал». Правило: мутатор значимого типа — `Func<T, T>` (принял — вернул). Симптом этого сеанса: тогглы настроек не меняли SettingsData, а пресеты (через ApplyPreset с возвратом) работали.
- Rigidbody нельзя добавлять после GrabInteractable.Awake того же объекта: порядок Awake не определён, `GetComponentInParent<Rigidbody>` вернёт null и физика объекта не будет управляться интерактором. Решение: `DefaultExecutionOrder(-10)` на компоненте-владельце (см. DeliveryBoxActor: кинематик пока несут, динамик при отпускании).
- Unity 6: `Object.GetInstanceID()` — ошибка компиляции (CS0619), а не предупреждение; использовать `GetEntityId()` (возвращает структуру EntityId — наборы делать `HashSet<EntityId>`, а не `HashSet<int>`).

