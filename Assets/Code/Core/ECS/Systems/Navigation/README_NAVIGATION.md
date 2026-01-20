# Navigation Grid System

Система навигационной сетки для PROJECT-VICE, реализованная на Unity DOTS/ECS.

## 📋 Обзор

Навигационная сетка разделяет каждый чанк мира (100x100м) на сетку 64x64 ячеек (~1.56м на ячейку). Каждая ячейка помечена как `walkable` (проходимая) или `blocked` (заблокированная) на основе статических препятствий.

## 🗂️ Структура

### Компоненты (`Assets/Code/Core/ECS/Components/Navigation/`)

#### `NavigationGrid`
- **Тип**: `IComponentData`
- **Назначение**: Хранит BlobAssetReference к GridData для каждого чанка
- **Важно**: Автоматически dispose при выгрузке чанка через `NavigationGridCleanupSystem`

#### `GridData`
- **Тип**: `BlobAsset`
- **Содержимое**: 
  - `Cells: BlobArray<byte>` - 64x64 ячеек (0=walkable, 1=blocked)
  - `ChunkId: int2` - ID чанка
  - `GridSize: int` - Размер сетки (64)
- **Память**: ~4KB на чанк

#### `StaticObstacle`
- **Тип**: `IComponentData`
- **Свойства**:
  - `Position: float2` - Глобальная позиция
  - `Radius: float` - Радиус препятствия
  - `Type: ObstacleType` - Тип (Building, Tree, Rock, Water, Custom)

#### `NavigationDebugData`
- **Тип**: `IComponentData`
- **Назначение**: Статистика для debug окна
- **Свойства**: WalkableCells, BlockedCells, ObstacleCount

### Системы (`Assets/Code/Core/ECS/Systems/Navigation/`)

#### `NavigationGridBuildSystem`
- **UpdateInGroup**: `ChunkManagementGroup`
- **Триггер**: Создание нового чанка (без NavigationGrid)
- **Процесс**:
  1. Находит новые загруженные чанки
  2. Собирает релевантные StaticObstacle (в радиусе чанка + margin)
  3. Создаёт BlobAsset с сеткой
  4. Растеризует препятствия в сетку
  5. Добавляет NavigationGrid и NavigationDebugData компоненты

#### `NavigationGridUpdateSystem`
- **UpdateInGroup**: `SimulationSystemGroup`
- **Триггер**: Изменения в StaticObstacle (упрощённо - каждый фрейм)
- **Процесс**:
  1. Пересоздаёт BlobAsset для всех чанков
  2. Dispose старых BlobAsset
  3. Обновляет NavigationDebugData
- **TODO**: Оптимизировать - отслеживать только изменённые чанки

#### `NavigationGridCleanupSystem`
- **UpdateInGroup**: `ChunkManagementGroup`
- **Назначение**: Освобождает BlobAsset при выгрузке чанков
- **Критично**: Предотвращает memory leaks

#### `TestObstacleGeneratorSystem`
- **UpdateInGroup**: `InitializationSystemGroup`
- **Назначение**: Генерирует тестовые препятствия при старте
- **Настройки**:
  - `OBSTACLES_PER_CHUNK = 5`
  - `MIN_RADIUS = 2f`
  - `MAX_RADIUS = 8f`
- **ВАЖНО**: Отключить в production!

### Визуализация

#### `NavigationGridVisualizer` (MonoBehaviour)
- **Путь**: `Assets/Code/Tools/Renderer/NavigationGridVisualizer.cs`
- **Использование**: Добавить на GameObject в сцене
- **Настройки**:
  - `showGrid` - показывать сетку
  - `showOnlyBlocked` - показывать только заблокированные ячейки
  - `walkableColor` - цвет проходимых ячеек (зелёный, прозрачный)
  - `blockedColor` - цвет заблокированных ячеек (красный, полупрозрачный)
  - `showObstacles` - показывать препятствия
  - `maxDrawDistance` - distance culling для оптимизации

#### `ViceDebugWindow` → вкладка "Navigation"
- **Меню**: `VICE > Debug Window`
- **Отображает**:
  - Общую статистику (total walkable/blocked cells, obstacles, memory)
  - Per-chunk breakdown с процентом walkable
  - Цветовая индикация: зелёный (>80%), жёлтый (50-80%), оранжевый (20-50%), красный (<20%)

## 🚀 Использование

### Базовый Setup

1. Навигационные сетки создаются **автоматически** при загрузке чанков
2. Для тестирования добавьте препятствия:
   ```csharp
   // В TestObstacleGeneratorSystem уже реализовано
   // Или вручную:
   var entity = entityManager.CreateEntity();
   entityManager.AddComponent(entity, new StaticObstacle(
       new float2(50, 50), // позиция
       5f,                 // радиус
       ObstacleType.Building
   ));
   ```

### Проверка проходимости ячейки

```csharp
// Получить NavigationGrid для чанка
var gridQuery = entityManager.CreateEntityQuery(
    ComponentType.ReadOnly<NavigationGrid>(),
    ComponentType.ReadOnly<Chunk>()
);

// Для конкретного чанка
ref var gridData = ref navigationGrid.GridBlob.Value;

// Проверить ячейку (x, y в диапазоне 0-63)
bool isWalkable = gridData.IsWalkable(x, y);
```

### Получить ячейку по мировой позиции

```csharp
// Конвертировать мировую позицию в координаты ячейки
var chunkId = new int2(
    (int)math.floor(worldPos.x / ChunkConstants.CHUNK_SIZE),
    (int)math.floor(worldPos.y / ChunkConstants.CHUNK_SIZE)
);

var localPos = worldPos - new float2(
    chunkId.x * ChunkConstants.CHUNK_SIZE,
    chunkId.y * ChunkConstants.CHUNK_SIZE
);

var cellX = (int)(localPos.x / ChunkConstants.NAV_CELL_SIZE);
var cellY = (int)(localPos.y / ChunkConstants.NAV_CELL_SIZE);
```

## ⚙️ Константы (ChunkConstants.cs)

```csharp
CHUNK_SIZE = 100.0f;        // Размер чанка в метрах
NAV_GRID_SIZE = 64;         // 64x64 ячеек на чанк
NAV_CELL_SIZE = 1.5625f;    // ~1.56м размер ячейки
```

## 📊 Performance

### Память
- **Per chunk**: ~4KB (64*64 bytes)
- **100 чанков**: ~400KB
- **1000 чанков**: ~4MB

### CPU
- **Build**: O(obstacles * cells) = O(N * 4096) per chunk
  - 5 препятствий, 1 чанк: ~20,480 distance checks
  - Burst-compiled, хорошо оптимизировано
- **Update**: Пересоздаёт все сетки при изменении препятствий
  - **TODO**: Оптимизировать - отслеживать dirty chunks

### Оптимизация
- ✅ Burst compilation
- ✅ BlobAsset для минимального overhead
- ✅ Distance culling в визуализации
- ✅ Margin для граничных препятствий (избегаем дубликатов)
- ⚠️ UpdateSystem пересоздаёт все сетки (нужна оптимизация)

## 🔮 Roadmap (не в этом PR)

1. **Pathfinding система (A* / Jump Point Search)**
   - Использование NavigationGrid для поиска пути
   - Async Jobs для больших путей
   - Path caching

2. **Dynamic obstacles**
   - NPC как динамические препятствия
   - Временные блокировки (автомобили, события)
   - Incremental grid updates

3. **Multi-level navigation**
   - Vertical движение (лестницы, лифты)
   - Portals между уровнями

4. **Navigation mesh (опционально)**
   - Альтернатива grid для больших открытых пространств
   - Hybrid approach

## 🐛 Known Issues

1. **NavigationGridUpdateSystem** пересоздаёт сетки каждый фрейм если есть препятствия
   - **Workaround**: Временно отключить систему после тестирования
   - **Fix**: Отслеживать dirty chunks через ChangeFilter

2. **TestObstacleGeneratorSystem** создаёт препятствия только при запуске
   - **Workaround**: Перезапустить проект для регенерации
   - **Fix**: Добавить кнопку в DebugWindow для ручной генерации

## 📝 Testing

1. **Запустить проект** → чанки загрузятся автоматически
2. **Открыть VICE > Debug Window** → вкладка "Navigation"
3. **Проверить статистику**:
   - Total obstacles должно быть ~25 (5 на чанк * 5 загруженных чанков)
   - Average walkable должно быть 70-90%
4. **Scene View** → включить Gizmos → видеть сетку и препятствия
5. **Настроить NavigationGridVisualizer** на любом GameObject

## 👥 Авторы

- Navigation Grid System v1.0
- PROJECT-VICE Team
- 2025

## 📄 License

Следует основной лицензии проекта PROJECT-VICE.
