using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

// Система управления загрузкой, выгрузкой и жизненным циклом чанков (2D X-Y)
[UpdateInGroup(typeof(InitializationSystemGroup))]
public partial struct ChunkManagementSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        var entityManager = state.EntityManager;
        
        // Проверяем, не создан ли уже singleton
        if (SystemAPI.HasSingleton<ChunkMapSingleton>())
            return;
        
        // 1. Создаём Prefab для чанков
        var chunkPrefab = entityManager.CreateEntity();
        entityManager.AddComponent<Chunk>(chunkPrefab);
        entityManager.AddComponent<Prefab>(chunkPrefab);

        // 2. Создаём Entity для хранения буфера ChunkMapEntry
        var mapDataEntity = entityManager.CreateEntity();
        entityManager.AddComponentData(mapDataEntity, new ChunkMapBufferData());
        entityManager.AddBuffer<ChunkMapEntry>(mapDataEntity);

        // 3. Создаём синглтон ChunkMapSingleton
        var singletonEntity = entityManager.CreateEntity();
        entityManager.AddComponentData(singletonEntity, new ChunkMapSingleton(chunkPrefab, mapDataEntity));
    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var entityManager = state.EntityManager;

        // 1. Получаем данные синглтона
        var singleton = SystemAPI.GetSingleton<ChunkMapSingleton>();
        var chunkPrefab = singleton.ChunkPrefab;
        var viewDistance = singleton.ViewDistanceInChunks;
        var mapDataEntity = singleton.ChunkMapDataEntity;

        // 2. Получаем буфер с картой чанков
        var chunkMapBuffer = entityManager.GetBuffer<ChunkMapEntry>(mapDataEntity);

        // 3. Определяем центр и границы зоны загрузки
        var centerPoint = SystemAPI.HasSingleton<PlayerPosition>() 
            ? SystemAPI.GetSingleton<PlayerPosition>().Value 
            : float2.zero;
        
        var centerChunkId = WorldToChunkId(centerPoint);
        var minChunkId = centerChunkId - new int2(viewDistance, viewDistance);
        var maxChunkId = centerChunkId + new int2(viewDistance, viewDistance);

        // 4. Создаём HashSet требуемых чанков
        int estimatedCount = (viewDistance * 2 + 1) * (viewDistance * 2 + 1);
        var requiredChunksSet = new NativeHashSet<int2>(estimatedCount, Allocator.Temp);
        
        for (int x = minChunkId.x; x <= maxChunkId.x; x++)
        {
            for (int y = minChunkId.y; y <= maxChunkId.y; y++)
            {
                requiredChunksSet.Add(new int2(x, y));
            }
        }

        // 5. Создаём HashMap существующих чанков
        var existingChunks = new NativeHashMap<int2, ChunkMapEntry>(chunkMapBuffer.Length, Allocator.Temp);
        for (int i = 0; i < chunkMapBuffer.Length; i++)
        {
            existingChunks.TryAdd(chunkMapBuffer[i].Id, chunkMapBuffer[i]);
        }

        // 6. Список чанков для добавления в буфер ПОСЛЕ Playback
        var chunksToAdd = new NativeList<int2>(Allocator.Temp);

        var ecb = new EntityCommandBuffer(Allocator.Temp);
        try
        {
            // === ЗАГРУЗКА НОВЫХ ЧАНКОВ ===
            foreach (var chunkId in requiredChunksSet)
            {
                if (existingChunks.TryGetValue(chunkId, out var entry))
                {
                    // Чанк существует - проверяем состояние
                    if (entry.State == ChunkState.Unloaded)
                    {
                        ecb.SetComponent(entry.Entity, new Chunk(
                            chunkId, 
                            ChunkIdToWorldPosition(chunkId), 
                            ChunkState.Loaded
                        ));
                        
                        // Обновляем в буфере
                        for (int i = 0; i < chunkMapBuffer.Length; i++)
                        {
                            if (chunkMapBuffer[i].Id.Equals(chunkId))
                            {
                                chunkMapBuffer[i] = new ChunkMapEntry(chunkId, entry.Entity, ChunkState.Loaded);
                                break;
                            }
                        }
                    }
                }
                else
                {
                    // Чанк не существует - создаём новый НАПРЯМУЮ
                    var newChunkEntity = entityManager.Instantiate(chunkPrefab);
                    entityManager.SetComponentData(newChunkEntity, new Chunk(
                        chunkId, 
                        ChunkIdToWorldPosition(chunkId), 
                        ChunkState.Loaded
                    ));
                    
                    // Сразу добавляем в буфер (entity уже реализован)
                    chunkMapBuffer.Add(new ChunkMapEntry(
                        chunkId, 
                        newChunkEntity, 
                        ChunkState.Loaded
                    ));
                }
            }
            
            // === ВЫГРУЗКА НЕНУЖНЫХ ЧАНКОВ ===
            for (int i = chunkMapBuffer.Length - 1; i >= 0; i--)
            {
                var entry = chunkMapBuffer[i];
                
                if (!requiredChunksSet.Contains(entry.Id) && entry.State == ChunkState.Loaded)
                {
                    if (entityManager.Exists(entry.Entity))
                    {
                        ecb.DestroyEntity(entry.Entity);
                        chunkMapBuffer.RemoveAt(i);
                    }
                    else
                    {
                        chunkMapBuffer.RemoveAt(i);
                    }
                }
            }
            
            ecb.Playback(entityManager);
        }
        finally
        {
            ecb.Dispose();
            existingChunks.Dispose();
            requiredChunksSet.Dispose();
            chunksToAdd.Dispose();
        }
    }

    private static int2 WorldToChunkId(float2 worldPos2D)
    {
        var x = (int)math.floor(worldPos2D.x / ChunkConstants.CHUNK_SIZE);
        var y = (int)math.floor(worldPos2D.y / ChunkConstants.CHUNK_SIZE);
        return new int2(x, y);
    }

    private static float2 ChunkIdToWorldPosition(int2 chunkId)
    {
        return new float2(chunkId.x * ChunkConstants.CHUNK_SIZE, chunkId.y * ChunkConstants.CHUNK_SIZE);
    }
}
