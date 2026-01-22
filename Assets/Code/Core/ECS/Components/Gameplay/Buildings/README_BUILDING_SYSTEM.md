# Building System - Документация

## 📋 Обзор

Система генерации и управления зданиями в PROJECT-VICE. Здания служат целевыми точками для NPC, интегрируются с системой навигации и AI планировщиком целей.

---

## 🏗️ Архитектура

### Компоненты

#### `Building` (IComponentData)
Основной компонент здания с полной информацией:

```csharp
public struct Building : IComponentData
{
    public BuildingType Type;           // Тип здания
    public float2 Position;             // Мировая позиция центра (2D X-Y)
    public float2 Size;                 // Размер (ширина x длина)
    public float Height;                // Высота в метрах
    public int2 ChunkId;                // ID чанка
    public int CurrentOccupancy;        // Текущее количество NPC внутри
    public int MaxOccupancy;            // Максимальная вместимость
    public bool IsAccessible;           // Доступно ли здание
}
```

**Методы:**
- `CanAcceptVisitor` - может ли здание принять посетителя
- `GetEntrancePosition()` - получить позицию входа
- `ContainsPoint(float2)` - проверка точки внутри здания
- `GetDistanceToPoint(float2)` - расстояние до здания

#### `BuildingType` (enum)
Типы зданий для различных активностей NPC:

| Тип | Описание | Поддерживаемые цели | Фракции |
|-----|----------|---------------------|---------|
| **Residential** | Жилые здания | Sleep, Socialize | Все |
| **Commercial** | Магазины, рестораны | Eat, Socialize, VisitLocation | Все |
| **Industrial** | Склады, фабрики | Work | Civilians |
| **Public** | Полиция, больницы | Work, VisitLocation | Police, Civilians |
| **Special** | Банки, казино, базы | PatrolArea, VisitLocation | Банды |

#### `ChunkGenerationConfig` (ScriptableObject)
Конфигурация генерации зданий:

```csharp
[CreateAssetMenu(fileName = "ChunkGenerationConfig", 
                 menuName = "Project Vice/Configs/Chunk Generation Config")]
public class ChunkGenerationConfig : ScriptableObject
{
    // Building Generation
    [Range(0, 20)] public int MinBuildingsPerChunk = 2;
    [Range(0, 50)] public int MaxBuildingsPerChunk = 8;
    
    // Size Settings
    [Range(5f, 30f)] public float MinBuildingSize = 8f;
    [Range(10f, 50f)] public float MaxBuildingSize = 25f;
    [Range(3f, 10f)] public float MinBuildingHeight = 4f;
    [Range(5f, 100f)] public float MaxBuildingHeight = 30f;
    
    // Type Weights (сумма должна быть > 0)
    [Range(0f, 1f)] public float ResidentialWeight = 0.4f;
    [Range(0f, 1f)] public float CommercialWeight = 0.3f;
    [Range(0f, 1f)] public float IndustrialWeight = 0.15f;
    [Range(0f, 1f)] public float PublicWeight = 0.1f;
    [Range(0f, 1f)] public float SpecialWeight = 0.05f;
    
    // Spacing
    [Range(2f, 20f)] public float MinBuildingSpacing = 5f;
    [Range(0f, 20f)] public float EdgeMargin = 5f;
    
    // Activity
    [Range(1, 50)] public int MaxOccupancy = 10;
}
```

---

## 🎯 Системы

### BuildingGenerationSystem

**UpdateInGroup:** `ChunkManagementGroup`  
**UpdateAfter:** `ChunkManagementSystem`

**Функция:** Генерирует здания при загрузке чанков

**Процесс:**
1. Находит новые загруженные чанки (без `BuildingGenerated` tag)
2. Генерирует уникальный seed на основе координат чанка
3. Определяет количество зданий (MinBuildings - MaxBuildings)
4. Для каждого здания:
   - Выбирает тип на основе весов
   - Генерирует размеры (ширина, длина, высота)
   - Ищет свободную позицию (до 20 попыток)
   - Проверяет коллизии с другими зданиями
   - Создает Entity с компонентами `Building` и `StaticObstacle`
5. Помечает чанк тегом `BuildingGenerated`

**Интеграция с навигацией:**
- Каждое здание автоматически добавляется как `StaticObstacle`
- Радиус препятствия = половина диагонали здания
- `NavigationGridBuildSystem` учитывает здания при построении сетки

---

## 🔌 Интеграция с NPC AI

### Extension методы для BuildingType

```csharp
// Проверка совместимости здания с целью
buildingType.SupportsGoal(GoalType.Work);

// Проверка совместимости здания с фракцией для работы
buildingType.CanWorkIn(FactionType.Police);
```

### Примеры использования в GoalPlanningSystem

```csharp
// 1. Найти ближайшее жилое здание для сна
var nearestResidential = FindNearestBuilding(
    npcPosition, 
    BuildingType.Residential, 
    GoalType.Sleep
);

if (nearestResidential.CanAcceptVisitor)
{
    var entrance = nearestResidential.GetEntrancePosition();
    goal = new CurrentGoal(
        GoalType.Sleep,
        targetPosition: new float3(entrance.x, 0, entrance.y),
        priority: 0.8f
    );
}

// 2. Найти коммерческое здание для обеда
var nearestRestaurant = FindNearestBuilding(
    npcPosition, 
    BuildingType.Commercial, 
    GoalType.Eat
);

// 3. Найти рабочее место для фракции
var workplace = FindWorkplace(npcPosition, faction);
if (workplace.Type.CanWorkIn(faction.Type))
{
    // Назначить цель Work
}
```

### Query зданий

```csharp
// Все здания в чанке
foreach (var building in SystemAPI.Query<RefRO<Building>>())
{
    if (building.ValueRO.ChunkId.Equals(targetChunkId))
    {
        // Обработка
    }
}

// Доступные здания определенного типа
foreach (var building in SystemAPI.Query<RefRW<Building>>())
{
    if (building.ValueRO.Type == BuildingType.Commercial && 
        building.ValueRO.CanAcceptVisitor)
    {
        // Можно отправить NPC
        building.ValueRW.CurrentOccupancy++;
    }
}
```

---

## 🎨 Визуализация

### BuildingVisualizer (MonoBehaviour)

**Путь:** `Assets/Code/Tools/Renderer/BuildingVisualizer.cs`

**Использование:**
1. Добавить компонент на GameObject в сцене
2. Настроить параметры в Inspector
3. Включить Gizmos в Scene View

**Настройки:**

| Параметр | Описание | Значение по умолчанию |
|----------|----------|-----------------------|
| `showBuildings` | Показывать здания | true |
| `showBuildingInfo` | Информация (тип, размер) | true |
| `showEntrances` | Показывать входы (желтые сферы) | true |
| `showOccupancy` | Заполненность (👥 X/Y) | true |
| `maxDrawDistance` | Дистанс каллинг | 200м |
| `simplifiedMode` | Упрощенная отрисовка (2D) | false |
| `showOnlyOccupied` | Только занятые здания | false |

**Цвета по типам:**
- 🟦 **Residential** - Синий (0.2, 0.6, 1.0)
- 🟨 **Commercial** - Желтый (1.0, 0.8, 0.2)
- 🟫 **Industrial** - Коричневый (0.6, 0.4, 0.2)
- 🟢 **Public** - Зеленый (0.2, 1.0, 0.4)
- 🟥 **Special** - Красный (1.0, 0.2, 0.4)

**Визуальные элементы:**
- Контур здания (прозрачный куб)
- Вертикальные линии (высота)
- Вход (желтая сфера)
- Стрелка от центра к входу
- Label с информацией

---

## 🚀 Setup Guide

### 1. Создание конфига

```
Project Window → Right Click
→ Create → Project Vice → Configs → Chunk Generation Config
```

**Рекомендуемые настройки для начала:**
```
MinBuildingsPerChunk: 3
MaxBuildingsPerChunk: 7
MinBuildingSize: 10m
MaxBuildingSize: 20m
MinBuildingHeight: 5m
MaxBuildingHeight: 15m
MinBuildingSpacing: 6m
EdgeMargin: 8m
MaxOccupancy: 8
```

### 2. Добавление в сцену

1. Создать GameObject: `ChunkGenerationSettings`
2. Добавить компонент: `ChunkGenerationConfigAuthoring`
3. Назначить созданный конфиг в поле `Config`

### 3. Визуализация

1. Создать GameObject: `BuildingVisualizer`
2. Добавить компонент: `BuildingVisualizer`
3. Настроить параметры отображения
4. Включить Gizmos в Scene View

---

## 📊 Performance

### Генерация

**Complexity:** O(N * M)
- N = количество зданий (2-8 на чанк)
- M = попытки размещения (до 20)

**Оптимизации:**
- ✅ Burst compilation
- ✅ Детерминированная генерация (seed по координатам чанка)
- ✅ EntityCommandBuffer для batch создания
- ✅ NativeList для временного хранения
- ✅ Ранний выход при неудачном размещении

**Типичное время:**
- 1 чанк: < 0.1ms
- 25 чанков: < 2ms

### Визуализация

**Оптимизации:**
- ✅ Distance culling (maxDrawDistance)
- ✅ Simplified mode для дальних зданий
- ✅ Filter по заполненности
- ✅ Label только для близких зданий (60% от maxDistance)

---

## 🔮 Примеры интеграции

### Система поиска ближайшего здания

```csharp
private Building FindNearestBuilding(
    float2 position, 
    BuildingType type, 
    GoalType goal)
{
    Building nearest = default;
    float minDist = float.MaxValue;
    
    foreach (var building in SystemAPI.Query<RefRO<Building>>())
    {
        if (!building.ValueRO.Type.SupportsGoal(goal))
            continue;
        
        if (!building.ValueRO.CanAcceptVisitor)
            continue;
        
        var dist = building.ValueRO.GetDistanceToPoint(position);
        if (dist < minDist)
        {
            minDist = dist;
            nearest = building.ValueRO;
        }
    }
    
    return nearest;
}
```

### Вход и выход из здания

```csharp
// При входе
if (building.CanAcceptVisitor)
{
    building.CurrentOccupancy++;
    stateFlags.IsBusy = true;
}

// При выходе
building.CurrentOccupancy = math.max(0, building.CurrentOccupancy - 1);
stateFlags.IsBusy = false;
```

### Динамическое открытие/закрытие

```csharp
// Закрыть здание ночью
if (gameTime.Hour >= 22 || gameTime.Hour < 6)
{
    if (building.Type == BuildingType.Commercial)
        building.IsAccessible = false;
}
else
{
    building.IsAccessible = true;
}
```

---

## 🐛 Known Issues

1. **Здания могут генерироваться за границами NavigationGrid**
   - **Fix:** NavigationGridBuildSystem учитывает margin вокруг чанка
   - **Status:** ✅ Resolved

2. **Вход здания может быть заблокирован препятствием**
   - **Workaround:** Увеличить MinBuildingSpacing
   - **Fix:** Проверять доступность пути к входу
   - **Status:** ⏳ Planned

3. **NPC не выходят из здания при истечении цели**
   - **Fix:** Требуется система выхода в LifeActivitiesSystem
   - **Status:** ⏳ In Progress

---

## 🔧 Troubleshooting

### Здания не генерируются

**Проверьте:**
1. ChunkGenerationConfigAuthoring добавлен в сцену
2. Config назначен в Inspector
3. ChunkGenerationSettings существует как Singleton
4. BuildingGenerationSystem запущена

**Debug:**
```csharp
// В BuildingGenerationSystem
Debug.Log($"Generated {buildingCount} buildings in chunk {chunkId}");
```

### Здания не видны

**Проверьте:**
1. BuildingVisualizer добавлен на GameObject
2. showBuildings = true
3. Gizmos включены в Scene View
4. Камера в пределах maxDrawDistance

### Коллизии зданий

**Проверьте:**
1. MinBuildingSpacing >= 5м
2. EdgeMargin >= 5м
3. MaxBuildingSize не слишком большой для чанка

---

## 📝 TODO / Roadmap

### Phase 1 (Current) ✅
- ✅ Базовая генерация зданий
- ✅ Интеграция с навигацией (StaticObstacle)
- ✅ Типы зданий и extension методы
- ✅ Визуализация через Gizmos

### Phase 2 (In Progress)
- ⏳ Система входа/выхода из зданий
- ⏳ Динамическое открытие/закрытие
- ⏳ Интеграция с GoalPlanningSystem
- ⏳ Building query helpers

### Phase 3 (Planned)
- 🔜 Внутренняя структура зданий (этажи, комнаты)
- 🔜 Специальные здания с уникальными свойствами
- 🔜 Владение зданиями (фракции)
- 🔜 События в зданиях (ограбления, рейды)
- 🔜 Procedural mesh generation для зданий

---

## 👥 Авторы

- Building System v1.0
- PROJECT-VICE Team
- 2025

## 📄 License

Следует основной лицензии проекта PROJECT-VICE.
