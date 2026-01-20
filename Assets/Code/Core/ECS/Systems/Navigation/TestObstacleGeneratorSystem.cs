using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

// Система генерации тестовых препятствий для проверки навигационной сетки
// ВНИМАНИЕ: Только для тестирования! Отключить в продакшене
[UpdateInGroup(typeof(InitializationSystemGroup))]
public partial struct TestObstacleGeneratorSystem : ISystem
{
    private bool initialized;
    private Random random;
    
    // Настройки генерации
    private const int OBSTACLES_PER_CHUNK = 8;   // Увеличено с 5
    private const float MIN_RADIUS = 3f;         // Увеличено с 2f
    private const float MAX_RADIUS = 12f;        // Увеличено с 8f
    private const float EDGE_MARGIN = 5f;        // Отступ от краёв чанка
    
    public void OnCreate(ref SystemState state)
    {
        initialized = false;
        random = new Random(12345); // Фиксированный seed для воспроизводимости
    }
    
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        // Генерируем препятствия только один раз при запуске
        if (initialized) return;
        
        var entityManager = state.EntityManager;
        
        // Проверяем наличие ChunkMapSingleton
        if (!SystemAPI.TryGetSingleton<ChunkMapSingleton>(out var chunkMapSingleton))
            return;
        
        if (!entityManager.Exists(chunkMapSingleton.ChunkMapDataEntity))
            return;
        
        var chunkMapBuffer = entityManager.GetBuffer<ChunkMapEntry>(chunkMapSingleton.ChunkMapDataEntity);
        
        // Собираем все загруженные чанки
        var loadedChunks = new NativeList<int2>(Allocator.Temp);
        foreach (var entry in chunkMapBuffer)
        {
            if (entry.State == ChunkState.Loaded)
            {
                loadedChunks.Add(entry.Id);
            }
        }
        
        // Если нет загруженных чанков, ждём
        if (loadedChunks.Length == 0)
        {
            loadedChunks.Dispose();
            return;
        }
        
        // Генерируем препятствия
        var ecb = new EntityCommandBuffer(Allocator.Temp);
        var obstacleCount = 0;
        
        foreach (var chunkId in loadedChunks)
        {
            GenerateObstaclesForChunk(chunkId, ref ecb, ref random);
            obstacleCount += OBSTACLES_PER_CHUNK;
        }
        
        ecb.Playback(entityManager);
        ecb.Dispose();
        
        loadedChunks.Dispose();
        
        initialized = true;
        
        UnityEngine.Debug.Log($"[TestObstacleGenerator] Generated {obstacleCount} test obstacles");
    }
    
    private static void GenerateObstaclesForChunk(int2 chunkId, ref EntityCommandBuffer ecb, ref Random random)
    {
        var chunkWorldPos = new float2(
            chunkId.x * ChunkConstants.CHUNK_SIZE,
            chunkId.y * ChunkConstants.CHUNK_SIZE
        );
        
        for (int i = 0; i < OBSTACLES_PER_CHUNK; i++)
        {
            // Случайная позиция внутри чанка с отступом от краёв
            var localPos = new float2(
                random.NextFloat(EDGE_MARGIN, ChunkConstants.CHUNK_SIZE - EDGE_MARGIN),
                random.NextFloat(EDGE_MARGIN, ChunkConstants.CHUNK_SIZE - EDGE_MARGIN)
            );
            var worldPos = chunkWorldPos + localPos;
            
            // Случайный радиус
            var radius = random.NextFloat(MIN_RADIUS, MAX_RADIUS);
            
            // Случайный тип
            var type = (ObstacleType)(random.NextInt(0, 4));
            
            // Создаём entity с препятствием
            var obstacleEntity = ecb.CreateEntity();
            ecb.AddComponent(obstacleEntity, new StaticObstacle(worldPos, radius, type));
        }
    }
}
