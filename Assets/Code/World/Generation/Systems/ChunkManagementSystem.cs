using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

// Система управления загрузкой, выгрузкой и жизненным циклом чанков (теперь 2D X-Y)
[UpdateInGroup(typeof(InitializationSystemGroup))] // Или SimulationSystemGroup, зависит от логики
public partial struct ChunkManagementSystem : ISystem
{
    // Центральная точка мира (2D X-Y), вокруг которой загружаются чанки (временно - фиксированная)
    // В реальности это будет позиция игрока или центр активной симуляции.
    private static readonly float2 CENTER_POINT = new float2(0, 0);

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        var entityManager = state.EntityManager;
        // 1. Создаём Prefab для чанков (в реальности это может быть загружено из ScriptableObject или другого источника)
        var chunkPrefab = entityManager.CreateEntity();
        entityManager.AddComponent<Chunk>(chunkPrefab);
        // ... добавить другие компоненты, характерные для содержимого чанка (здания, декорации и т.п.)

        // 2. Создаём Entity для хранения буфера ChunkMapEntry
        var mapDataEntity = entityManager.CreateEntity();
        entityManager.AddComponentData(mapDataEntity, new ChunkMapBufferData());
        // --- ДОБАВЛЕНИЕ БУФЕРА ПРЯМО ТУТ ---
        entityManager.AddBuffer<ChunkMapEntry>(mapDataEntity);
        // -----------------------------------

        // 3. Создаём синглтон ChunkMapSingleton
        var singletonEntity = entityManager.CreateEntity();
        entityManager.AddComponentData(singletonEntity, new ChunkMapSingleton(chunkPrefab, mapDataEntity));

        // Логика инициализации завершена
        // Debug.Log("ChunkManagementSystem initialized."); // Убрали
    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {
        // Освобождение ресурсов, если необходимо
        // Буферы и другие NativeContainers, прикреплённые к Entity, удалятся автоматически при удалении Entity
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

        // 3. Определяем центр и границы зоны загрузки (в 2D X-Y)
        var centerChunkId = WorldToChunkId(CENTER_POINT);
        var minChunkId = centerChunkId - new int2(viewDistance, viewDistance);
        var maxChunkId = centerChunkId + new int2(viewDistance, viewDistance);

        // 4. Создаём список чанков, которые должны быть загружены
        var requiredChunks = new NativeList<int2>(Allocator.Temp);
        for (int x = minChunkId.x; x <= maxChunkId.x; x++)
        {
            for (int y = minChunkId.y; y <= maxChunkId.y; y++)
            {
                var id = new int2(x, y);
                requiredChunks.Add(id);
            }
        }

        // 5. Создаём CommandBuffer для безопасного изменения ECS структуры
        var ecb = new EntityCommandBuffer(Allocator.Temp);

        // 6. Проверяем, какие чанки нужно создать (загрузить)
        foreach (var id in requiredChunks)
        {
            // Проверяем, есть ли уже запись о чанке в буфере
            bool exists = false;
            for (int i = 0; i < chunkMapBuffer.Length; i++)
            {
                if (chunkMapBuffer[i].Id.Equals(id))
                {
                    exists = true;
                    // Проверяем состояние, возможно, нужно обновить (например, из Unloaded в Loaded)
                    if (chunkMapBuffer[i].State == ChunkState.Unloaded)
                    {
                         ecb.SetComponent(chunkMapBuffer[i].Entity, new Chunk(id, ChunkIdToWorldPosition(id), ChunkState.Loaded));
                         chunkMapBuffer[i] = new ChunkMapEntry(id, chunkMapBuffer[i].Entity, ChunkState.Loaded);
                    }
                    break;
                }
            }

            if (!exists)
            {
                // Чанк не существует, создаём его
                var newChunkEntity = ecb.Instantiate(chunkPrefab);
                ecb.SetComponent(newChunkEntity, new Chunk(id, ChunkIdToWorldPosition(id), ChunkState.Loaded));

                // Добавляем запись в буфер (через ECB, если буфер будет обновляться в другой системе)
                // Для простоты в этой системе добавляем сразу в буфер
                chunkMapBuffer.Add(new ChunkMapEntry(id, newChunkEntity, ChunkState.Loaded));
                
                // Debug.Log($"Created and loaded chunk {id}"); // Убрали
            }
        }

        // 7. Проверяем, какие чанки нужно выгрузить (не находятся в requiredChunks)
        // (Это упрощённая логика, в реальности может быть сложнее, учитывая задержки, флаги и т.д.)
        for (int i = chunkMapBuffer.Length - 1; i >= 0; i--)
        {
            var entry = chunkMapBuffer[i];
            bool shouldBeLoaded = false;
            foreach (var req_id in requiredChunks)
            {
                if (entry.Id.Equals(req_id))
                {
                    shouldBeLoaded = true;
                    break;
                }
            }

            if (!shouldBeLoaded && entry.State == ChunkState.Loaded)
            {
                // Помечаем чанк для выгрузки
                // Удаляем чанк Entity (NPC будут удалены в отдельной системе)
                ecb.DestroyEntity(entry.Entity);
                chunkMapBuffer.RemoveAt(i); // Удаляем запись из буфера
                // Debug.Log($"Marked chunk {entry.Id} for unloading (NPC cleanup handled by ChunkNPCCleanupSystem)"); // Убрали
            }
        }

        // 8. Проигрываем команды
        ecb.Playback(entityManager);
        ecb.Dispose();

        // 9. Освобождаем временный список
        requiredChunks.Dispose();
    }

    // Вспомогательная функция: получить ID чанка по 2D мировой позиции (X-Y)
    private static int2 WorldToChunkId(float2 worldPos2D)
    {
        var x = (int)math.floor(worldPos2D.x / ChunkConstants.CHUNK_SIZE);
        var y = (int)math.floor(worldPos2D.y / ChunkConstants.CHUNK_SIZE);
        return new int2(x, y);
    }

    // Вспомогательная функция: получить 2D мировую позицию угла чанка по ID (X-Y)
    private static float2 ChunkIdToWorldPosition(int2 chunkId)
    {
        return new float2(chunkId.x * ChunkConstants.CHUNK_SIZE, chunkId.y * ChunkConstants.CHUNK_SIZE);
    }
}