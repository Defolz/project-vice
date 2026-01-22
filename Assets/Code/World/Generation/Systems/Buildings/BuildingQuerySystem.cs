using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

/// <summary>
/// Вспомогательные методы для поиска зданий
/// Используется в GoalPlanningSystem и других AI системах
/// </summary>
[BurstCompile]
public static class BuildingQuery
{
    /// <summary>
    /// Найти ближайшее здание определенного типа, поддерживающее указанную цель
    /// </summary>
    public static bool TryFindNearestBuilding(
        in EntityQuery buildingQuery,
        float2 position,
        BuildingType type,
        GoalType goal,
        out Building nearestBuilding,
        out Entity nearestEntity)
    {
        nearestBuilding = default;
        nearestEntity = Entity.Null;
        float minDist = float.MaxValue;
        bool found = false;
        
        var buildings = buildingQuery.ToComponentDataArray<Building>(Allocator.Temp);
        var entities = buildingQuery.ToEntityArray(Allocator.Temp);
        
        for (int i = 0; i < buildings.Length; i++)
        {
            var building = buildings[i];
            
            // Проверки фильтрации
            if (building.Type != type)
                continue;
            
            if (!building.Type.SupportsGoal(goal))
                continue;
            
            if (!building.CanAcceptVisitor)
                continue;
            
            var dist = building.GetDistanceToPoint(position);
            if (dist < minDist)
            {
                minDist = dist;
                nearestBuilding = building;
                nearestEntity = entities[i];
                found = true;
            }
        }
        
        buildings.Dispose();
        entities.Dispose();
        
        return found;
    }
    
    /// <summary>
    /// Найти случайное здание определенного типа в радиусе
    /// </summary>
    public static bool TryFindRandomBuilding(
        in EntityQuery buildingQuery,
        ref Random random,
        float2 position,
        BuildingType type,
        float maxRadius,
        out Building randomBuilding,
        out Entity randomEntity)
    {
        randomBuilding = default;
        randomEntity = Entity.Null;
        
        var buildings = buildingQuery.ToComponentDataArray<Building>(Allocator.Temp);
        var entities = buildingQuery.ToEntityArray(Allocator.Temp);
        
        // Собираем подходящие здания
        var candidates = new NativeList<int>(buildings.Length, Allocator.Temp);
        
        for (int i = 0; i < buildings.Length; i++)
        {
            var building = buildings[i];
            
            if (building.Type != type)
                continue;
            
            if (!building.CanAcceptVisitor)
                continue;
            
            var dist = building.GetDistanceToPoint(position);
            if (dist <= maxRadius)
            {
                candidates.Add(i);
            }
        }
        
        bool found = false;
        
        if (candidates.Length > 0)
        {
            var randomIndex = random.NextInt(0, candidates.Length);
            var buildingIndex = candidates[randomIndex];
            
            randomBuilding = buildings[buildingIndex];
            randomEntity = entities[buildingIndex];
            found = true;
        }
        
        candidates.Dispose();
        buildings.Dispose();
        entities.Dispose();
        
        return found;
    }
    
    /// <summary>
    /// Найти рабочее место для указанной фракции
    /// </summary>
    public static bool TryFindWorkplace(
        in EntityQuery buildingQuery,
        float2 position,
        FactionType faction,
        out Building workplace,
        out Entity workplaceEntity)
    {
        workplace = default;
        workplaceEntity = Entity.Null;
        float minDist = float.MaxValue;
        bool found = false;
        
        var buildings = buildingQuery.ToComponentDataArray<Building>(Allocator.Temp);
        var entities = buildingQuery.ToEntityArray(Allocator.Temp);
        
        for (int i = 0; i < buildings.Length; i++)
        {
            var building = buildings[i];
            
            if (!building.Type.CanWorkIn(faction))
                continue;
            
            if (!building.CanAcceptVisitor)
                continue;
            
            var dist = building.GetDistanceToPoint(position);
            if (dist < minDist)
            {
                minDist = dist;
                workplace = building;
                workplaceEntity = entities[i];
                found = true;
            }
        }
        
        buildings.Dispose();
        entities.Dispose();
        
        return found;
    }
    
    /// <summary>
    /// Получить все здания в чанке
    /// </summary>
    public static NativeArray<Building> GetBuildingsInChunk(
        in EntityQuery buildingQuery,
        int2 chunkId,
        Allocator allocator)
    {
        var allBuildings = buildingQuery.ToComponentDataArray<Building>(Allocator.Temp);
        var result = new NativeList<Building>(allBuildings.Length, Allocator.Temp);
        
        for (int i = 0; i < allBuildings.Length; i++)
        {
            if (allBuildings[i].ChunkId.Equals(chunkId))
            {
                result.Add(allBuildings[i]);
            }
        }
        
        var finalArray = result.ToArray(allocator);
        
        result.Dispose();
        allBuildings.Dispose();
        
        return finalArray;
    }
    
    /// <summary>
    /// Подсчитать количество доступных зданий определенного типа
    /// </summary>
    public static int CountAvailableBuildings(
        in EntityQuery buildingQuery,
        BuildingType type)
    {
        int count = 0;
        var buildings = buildingQuery.ToComponentDataArray<Building>(Allocator.Temp);
        
        for (int i = 0; i < buildings.Length; i++)
        {
            if (buildings[i].Type == type && buildings[i].CanAcceptVisitor)
            {
                count++;
            }
        }
        
        buildings.Dispose();
        return count;
    }
}

/// <summary>
/// Система управления заполненностью зданий
/// Отслеживает вход/выход NPC из зданий
/// </summary>
[BurstCompile]
[UpdateInGroup(typeof(SimulationSystemGroup))]
public partial struct BuildingOccupancySystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<Building>();
    }
    
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var currentTime = (float)SystemAPI.Time.ElapsedTime;
        var ecb = new EntityCommandBuffer(Allocator.TempJob);
        
        // TODO: Интеграция с GoalExecutionSystem
        // Когда NPC достигает здания - увеличиваем Occupancy
        // Когда NPC покидает здание - уменьшаем Occupancy
        
        // Пока что просто валидируем значения
        foreach (var (building, entity) in SystemAPI.Query<RefRW<Building>>()
                 .WithEntityAccess())
        {
            // Убеждаемся что Occupancy не выходит за границы
            if (building.ValueRO.CurrentOccupancy < 0)
            {
                building.ValueRW.CurrentOccupancy = 0;
            }
            
            if (building.ValueRO.CurrentOccupancy > building.ValueRO.MaxOccupancy)
            {
                building.ValueRW.CurrentOccupancy = building.ValueRO.MaxOccupancy;
            }
        }
        
        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }
}
