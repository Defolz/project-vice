using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

/// <summary>
/// Система генерации зданий в чанках
/// Создает здания при загрузке чанка на основе ChunkGenerationConfig
/// Интегрируется с навигационной системой, помечая здания как препятствия
/// </summary>
[BurstCompile]
[UpdateInGroup(typeof(ChunkManagementGroup))]
[UpdateAfter(typeof(ChunkManagementSystem))]
public partial struct BuildingGenerationSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        // Требуем наличие конфига для генерации
        state.RequireForUpdate<ChunkGenerationSettings>();
    }
    
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var entityManager = state.EntityManager;
        
        // Проверяем, есть ли конфиг
        if (!SystemAPI.HasSingleton<ChunkGenerationSettings>())
            return;
        
        var config = SystemAPI.GetSingleton<ChunkGenerationSettings>();
        
        // Используем текущее время для seed'а random
        var currentTime = (float)SystemAPI.Time.ElapsedTime;
        
        var ecb = new EntityCommandBuffer(Allocator.TempJob);
        
        // Находим все новые чанки (Loaded, но без зданий)
        // Используем EntityCommandBuffer для отсроченного создания Entity
        foreach (var (chunk, entity) in SystemAPI.Query<RefRO<Chunk>>()
                 .WithNone<BuildingGenerated>()
                 .WithEntityAccess())
        {
            // Пропускаем неактивные чанки
            if (chunk.ValueRO.State != ChunkState.Loaded)
                continue;
            
            // Генерируем уникальный seed на основе координат чанка
            var seed = (uint)(chunk.ValueRO.Id.x * 73856093 ^ chunk.ValueRO.Id.y * 19349663);
            var random = Random.CreateFromIndex(seed);
            
            // Определяем количество зданий
            var buildingCount = random.NextInt(
                config.MinBuildingsPerChunk, 
                config.MaxBuildingsPerChunk + 1
            );
            
            // Получаем нормализованные веса
            config.GetNormalizedWeights(
                out float residentialWeight,
                out float commercialWeight,
                out float industrialWeight,
                out float publicWeight,
                out float specialWeight
            );
            
            // Храним занятые позиции для проверки коллизий
            var occupiedPositions = new NativeList<BuildingBounds>(buildingCount, Allocator.Temp);
            
            // Генерируем здания
            for (int i = 0; i < buildingCount; i++)
            {
                // Определяем тип здания на основе весов
                var buildingType = SelectBuildingType(
                    ref random, 
                    residentialWeight, 
                    commercialWeight, 
                    industrialWeight, 
                    publicWeight, 
                    specialWeight
                );
                
                // Генерируем размеры
                var width = random.NextFloat(config.MinBuildingSize, config.MaxBuildingSize);
                var length = random.NextFloat(config.MinBuildingSize, config.MaxBuildingSize);
                var height = random.NextFloat(config.MinBuildingHeight, config.MaxBuildingHeight);
                var size = new float2(width, length);
                
                // Пытаемся найти свободную позицию
                float2 position;
                bool foundValidPosition = false;
                int attempts = 0;
                const int MAX_ATTEMPTS = 20;
                
                do
                {
                    position = GenerateRandomPosition(
                        ref random, 
                        chunk.ValueRO.WorldPosition, 
                        size, 
                        config.EdgeMargin
                    );
                    
                    foundValidPosition = IsPositionValid(
                        position, 
                        size, 
                        occupiedPositions, 
                        config.MinBuildingSpacing
                    );
                    
                    attempts++;
                } while (!foundValidPosition && attempts < MAX_ATTEMPTS);
                
                // Если не нашли позицию - пропускаем это здание
                if (!foundValidPosition)
                    continue;
                
                // Сохраняем занятую область
                occupiedPositions.Add(new BuildingBounds(position, size));
                
                // Создаем Entity для здания
                var buildingEntity = ecb.CreateEntity();
                ecb.AddComponent(buildingEntity, new Building(
                    buildingType,
                    position,
                    size,
                    height,
                    chunk.ValueRO.Id,
                    config.MaxOccupancy
                ));
                
                // Добавляем здание как статическое препятствие для навигации
                // Используем радиус = половина диагонали здания
                var radius = math.length(size) * 0.5f;
                ecb.AddComponent(buildingEntity, new StaticObstacle(
                    position,
                    radius,
                    ObstacleType.Building
                ));
            }
            
            occupiedPositions.Dispose();
            
            // Помечаем чанк как обработанный
            ecb.AddComponent(entity, new BuildingGenerated());
            
            UnityEngine.Debug.Log($"<color=green>Generated {buildingCount} buildings in chunk {chunk.ValueRO.Id}</color>");
        }
        
        ecb.Playback(entityManager);
        ecb.Dispose();
    }
    
    /// <summary>
    /// Выбирает тип здания на основе весов
    /// </summary>
    private static BuildingType SelectBuildingType(
        ref Random random,
        float residentialWeight,
        float commercialWeight,
        float industrialWeight,
        float publicWeight,
        float specialWeight)
    {
        var roll = random.NextFloat();
        
        if (roll < residentialWeight)
            return BuildingType.Residential;
        
        roll -= residentialWeight;
        if (roll < commercialWeight)
            return BuildingType.Commercial;
        
        roll -= commercialWeight;
        if (roll < industrialWeight)
            return BuildingType.Industrial;
        
        roll -= industrialWeight;
        if (roll < publicWeight)
            return BuildingType.Public;
        
        return BuildingType.Special;
    }
    
    /// <summary>
    /// Генерирует случайную позицию внутри чанка с учетом размера здания и отступов
    /// </summary>
    private static float2 GenerateRandomPosition(
        ref Random random,
        float2 chunkWorldPos,
        float2 buildingSize,
        float edgeMargin)
    {
        var halfSize = buildingSize * 0.5f;
        var minPos = chunkWorldPos + new float2(edgeMargin + halfSize.x, edgeMargin + halfSize.y);
        var maxPos = chunkWorldPos + new float2(
            ChunkConstants.CHUNK_SIZE - edgeMargin - halfSize.x,
            ChunkConstants.CHUNK_SIZE - edgeMargin - halfSize.y
        );
        
        return new float2(
            random.NextFloat(minPos.x, maxPos.x),
            random.NextFloat(minPos.y, maxPos.y)
        );
    }
    
    /// <summary>
    /// Проверяет, не пересекается ли новая позиция с существующими зданиями
    /// </summary>
    private static bool IsPositionValid(
        float2 position,
        float2 size,
        NativeList<BuildingBounds> occupiedPositions,
        float minSpacing)
    {
        var halfSize = size * 0.5f;
        var newMin = position - halfSize - new float2(minSpacing, minSpacing);
        var newMax = position + halfSize + new float2(minSpacing, minSpacing);
        
        for (int i = 0; i < occupiedPositions.Length; i++)
        {
            var occupied = occupiedPositions[i];
            var occupiedHalfSize = occupied.Size * 0.5f;
            var occupiedMin = occupied.Position - occupiedHalfSize;
            var occupiedMax = occupied.Position + occupiedHalfSize;
            
            // AABB collision check
            bool overlaps = 
                newMin.x < occupiedMax.x && newMax.x > occupiedMin.x &&
                newMin.y < occupiedMax.y && newMax.y > occupiedMin.y;
            
            if (overlaps)
                return false;
        }
        
        return true;
    }
    
    /// <summary>
    /// Вспомогательная структура для хранения границ зданий
    /// </summary>
    private struct BuildingBounds
    {
        public float2 Position;
        public float2 Size;
        
        public BuildingBounds(float2 position, float2 size)
        {
            Position = position;
            Size = size;
        }
    }
}

/// <summary>
/// Tag компонент, помечающий чанки, в которых уже сгенерированы здания
/// </summary>
public struct BuildingGenerated : IComponentData { }
