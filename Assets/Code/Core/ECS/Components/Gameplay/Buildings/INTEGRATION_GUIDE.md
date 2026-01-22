# Интеграция Building System с NPC AI

## Пример использования в GoalPlanningSystem

Этот документ показывает, как интегрировать систему зданий с планировщиком целей NPC.

---

## 🔧 Изменения в GoalPlanningSystem

### 1. Добавить EntityQuery для зданий

```csharp
[BurstCompile]
[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateBefore(typeof(GoalExecutionSystem))]
public partial struct GoalPlanningSystem : ISystem
{
    private EntityQuery buildingQuery;
    
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<GameTimeComponent>();
        
        // Создаем query для зданий
        buildingQuery = state.GetEntityQuery(ComponentType.ReadOnly<Building>());
    }
    
    // ... OnUpdate
}
```

### 2. Улучшить планирование цели Sleep

**Было:**
```csharp
if (ShouldSleep(hour, traits))
{
    return new CurrentGoal(
        GoalType.Sleep,
        priority: 0.8f,
        expiryTime: currentTime + 3600f
    );
}
```

**Стало:**
```csharp
if (ShouldSleep(hour, traits))
{
    // Ищем ближайшее жилое здание
    if (BuildingQuery.TryFindNearestBuilding(
        in buildingQuery,
        location.GlobalPosition2D,
        BuildingType.Residential,
        GoalType.Sleep,
        out var building,
        out var buildingEntity))
    {
        var entrance = building.GetEntrancePosition();
        
        return new CurrentGoal(
            GoalType.Sleep,
            targetPosition: new float3(entrance.x, 0, entrance.y),
            targetEntity: buildingEntity,
            priority: 0.8f,
            expiryTime: currentTime + 3600f
        );
    }
    
    // Fallback: случайная позиция если здание не найдено
    var sleepPos = location.GlobalPosition2D + random.NextFloat2(-20f, 20f);
    return new CurrentGoal(
        GoalType.Sleep,
        targetPosition: new float3(sleepPos.x, 0, sleepPos.y),
        priority: 0.8f,
        expiryTime: currentTime + 3600f
    );
}
```

### 3. Улучшить планирование Work

**Стало:**
```csharp
if (ShouldWork(hour, faction.Type))
{
    // Ищем рабочее место для фракции
    if (BuildingQuery.TryFindWorkplace(
        in buildingQuery,
        location.GlobalPosition2D,
        faction.Type,
        out var workplace,
        out var workplaceEntity))
    {
        var entrance = workplace.GetEntrancePosition();
        
        return new CurrentGoal(
            GoalType.Work,
            targetPosition: new float3(entrance.x, 0, entrance.y),
            targetEntity: workplaceEntity,
            priority: 0.6f,
            expiryTime: currentTime + 7200f
        );
    }
    
    // Fallback: генерируем случайную рабочую позицию
    var workPos = location.GlobalPosition2D + random.NextFloat2(-30f, 30f);
    return new CurrentGoal(
        GoalType.Work,
        targetPosition: new float3(workPos.x, 0, workPos.y),
        priority: 0.6f,
        expiryTime: currentTime + 7200f
    );
}
```

### 4. Добавить цель Eat с поиском ресторанов

```csharp
// В методе PlanNextGoal, после проверки Work
if (ShouldEat(hour))
{
    // Ищем ближайший ресторан/магазин
    if (BuildingQuery.TryFindNearestBuilding(
        in buildingQuery,
        location.GlobalPosition2D,
        BuildingType.Commercial,
        GoalType.Eat,
        out var restaurant,
        out var restaurantEntity))
    {
        var entrance = restaurant.GetEntrancePosition();
        
        return new CurrentGoal(
            GoalType.Eat,
            targetPosition: new float3(entrance.x, 0, entrance.y),
            targetEntity: restaurantEntity,
            priority: 0.5f,
            expiryTime: currentTime + 600f // 10 минут
        );
    }
}

// Вспомогательный метод
private static bool ShouldEat(float hour)
{
    // Обед: 12:00 - 14:00
    return hour >= 12f && hour < 14f;
}
```

### 5. Улучшить VisitLocation со случайным выбором

```csharp
if (random.NextFloat() < VISIT_LOCATION_CHANCE)
{
    // Ищем случайное здание в радиусе 100м
    if (BuildingQuery.TryFindRandomBuilding(
        in buildingQuery,
        ref random,
        location.GlobalPosition2D,
        BuildingType.Commercial,
        maxRadius: 100f,
        out var building,
        out var buildingEntity))
    {
        var entrance = building.GetEntrancePosition();
        
        return new CurrentGoal(
            GoalType.VisitLocation,
            targetPosition: new float3(entrance.x, 0, entrance.y),
            targetEntity: buildingEntity,
            priority: 0.3f,
            expiryTime: currentTime + 900f
        );
    }
}
```

---

## 🔄 Изменения в LifeActivitiesSystem

### Обновление Occupancy при входе/выходе

```csharp
[BurstCompile]
[UpdateInGroup(typeof(SimulationSystemGroup))]
public partial struct LifeActivitiesSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var currentTime = (float)SystemAPI.Time.ElapsedTime;
        var ecb = new EntityCommandBuffer(Allocator.TempJob);
        
        // Обработка Work цели
        foreach (var (goal, stateFlags, location, entity) in 
                 SystemAPI.Query<RefRO<CurrentGoal>, RefRW<StateFlags>, RefRO<Location>>()
                 .WithEntityAccess())
        {
            if (goal.ValueRO.Type != GoalType.Work)
                continue;
            
            // Проверяем, достиг ли NPC рабочего места
            var distToTarget = math.distance(
                location.ValueRO.GlobalPosition2D, 
                new float2(goal.ValueRO.TargetPosition.x, goal.ValueRO.TargetPosition.z)
            );
            
            if (distToTarget < 2f && !stateFlags.ValueRO.IsBusy)
            {
                // NPC прибыл на работу
                stateFlags.ValueRW.IsBusy = true;
                
                // Увеличиваем Occupancy здания
                if (goal.ValueRO.TargetEntity != Entity.Null)
                {
                    if (state.EntityManager.HasComponent<Building>(goal.ValueRO.TargetEntity))
                    {
                        ecb.SetComponent(goal.ValueRO.TargetEntity, new Building
                        {
                            // Копируем все поля и увеличиваем Occupancy
                            CurrentOccupancy = state.EntityManager.GetComponentData<Building>(
                                goal.ValueRO.TargetEntity).CurrentOccupancy + 1
                        });
                    }
                }
                
                UnityEngine.Debug.Log($"<color=blue>Entity {entity.Index}: Started work</color>");
            }
            
            // Проверяем окончание работы
            if (stateFlags.ValueRO.IsBusy && goal.ValueRO.IsExpired(currentTime))
            {
                stateFlags.ValueRW.IsBusy = false;
                
                // Уменьшаем Occupancy здания
                if (goal.ValueRO.TargetEntity != Entity.Null)
                {
                    if (state.EntityManager.HasComponent<Building>(goal.ValueRO.TargetEntity))
                    {
                        var building = state.EntityManager.GetComponentData<Building>(goal.ValueRO.TargetEntity);
                        building.CurrentOccupancy = math.max(0, building.CurrentOccupancy - 1);
                        ecb.SetComponent(goal.ValueRO.TargetEntity, building);
                    }
                }
                
                ecb.SetComponent(entity, new CurrentGoal(GoalType.Idle, priority: 0.1f));
                UnityEngine.Debug.Log($"<color=blue>Entity {entity.Index}: Finished work</color>");
            }
        }
        
        // Аналогично для Sleep, Eat, Socialize...
        
        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }
}
```

---

## 📊 Добавление CurrentGoal.TargetEntity

Чтобы связать цель с конкретным зданием, нужно добавить поле в `CurrentGoal`:

```csharp
public struct CurrentGoal : IComponentData
{
    public GoalType Type;
    public Entity TargetEntity;    // <-- ДОБАВИТЬ ЭТО ПОЛЕ
    public float3 TargetPosition;
    public float ExpiryTime;
    public float Priority;
    
    // Конструктор с TargetEntity
    public CurrentGoal(
        GoalType type, 
        float3 targetPosition = default, 
        Entity targetEntity = default,  // <-- НОВЫЙ ПАРАМЕТР
        float priority = 0.5f, 
        float expiryTime = float.MaxValue)
    {
        Type = type;
        TargetPosition = targetPosition;
        TargetEntity = targetEntity;    // <-- ПРИСВОИТЬ
        Priority = priority;
        ExpiryTime = expiryTime;
    }
    
    // ... остальные методы
}
```

---

## 🎯 Улучшенная логика выбора зданий

### Приоритизация зданий по расстоянию и заполненности

```csharp
private static bool TryFindBestBuilding(
    in EntityQuery buildingQuery,
    float2 position,
    BuildingType type,
    GoalType goal,
    out Building bestBuilding,
    out Entity bestEntity)
{
    bestBuilding = default;
    bestEntity = Entity.Null;
    
    var buildings = buildingQuery.ToComponentDataArray<Building>(Allocator.Temp);
    var entities = buildingQuery.ToEntityArray(Allocator.Temp);
    
    float bestScore = float.MinValue;
    bool found = false;
    
    for (int i = 0; i < buildings.Length; i++)
    {
        var building = buildings[i];
        
        if (building.Type != type)
            continue;
        
        if (!building.Type.SupportsGoal(goal))
            continue;
        
        if (!building.CanAcceptVisitor)
            continue;
        
        var dist = building.GetDistanceToPoint(position);
        var occupancyRatio = building.CurrentOccupancy / (float)building.MaxOccupancy;
        
        // Скоринг: чем ближе и меньше заполнено - тем лучше
        var score = 100f / (dist + 1f) - occupancyRatio * 50f;
        
        if (score > bestScore)
        {
            bestScore = score;
            bestBuilding = building;
            bestEntity = entities[i];
            found = true;
        }
    }
    
    buildings.Dispose();
    entities.Dispose();
    
    return found;
}
```

---

## 🧪 Тестирование

### Проверка интеграции

1. **Создать ChunkGenerationConfig**
   - MinBuildings: 5, MaxBuildings: 10
   - Residential: 50%, Commercial: 30%, Public: 20%

2. **Запустить сцену**
   - Подождать загрузки чанков
   - Убедиться что здания сгенерированы

3. **Включить BuildingVisualizer**
   - showBuildings = true
   - showOccupancy = true

4. **Наблюдать за NPC**
   - Ночью (22:00-6:00): должны идти в Residential здания
   - Днём (8:00-18:00): должны идти в Work здания
   - Обед (12:00-14:00): должны идти в Commercial здания

5. **Проверить Occupancy**
   - Label должен показывать 👥 X/Y
   - Значение должно увеличиваться когда NPC входят
   - Значение должно уменьшаться когда NPC выходят

---

## 📈 Ожидаемые результаты

После интеграции:

✅ NPC выбирают конкретные здания для целей  
✅ Occupancy обновляется при входе/выходе  
✅ Здания не переполняются (CanAcceptVisitor)  
✅ Визуализация показывает активность зданий  
✅ Система масштабируется на сотни NPC  

---

## 🐛 Возможные проблемы

### NPC не идут в здания
- Проверьте что buildingQuery создан в OnCreate
- Убедитесь что здания сгенерированы (BuildingVisualizer)
- Проверьте логи: "Started work", "Finished work"

### Occupancy не обновляется
- Проверьте что goal.TargetEntity != Entity.Null
- Убедитесь что Building компонент существует на Entity
- Проверьте что ecb.Playback() вызывается

### Здания переполняются
- Проверьте логику CanAcceptVisitor в Building
- Убедитесь что BuildingOccupancySystem запущена
- Проверьте что CurrentOccupancy корректно уменьшается

---

**Версия:** 1.0  
**Дата:** 2025  
**Автор:** PROJECT-VICE Team
