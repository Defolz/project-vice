using Unity.Entities;
using Unity.Mathematics;
using UnityEngine; // Для Debug.Log в этой тестовой системе

// Тестовая система для проверки работы ChunkManagementSystem (теперь 2D)
// Проверяет заполнение ChunkMapBuffer
[UpdateInGroup(typeof(SimulationSystemGroup))] // Перемещено в SimulationGroup
public partial class ChunkTestSystem : SystemBase
{
    protected override void OnUpdate()
    {
        // Получаем синглтон
        var singleton = SystemAPI.GetSingleton<ChunkMapSingleton>();
        var entityManager = EntityManager; // Используем EntityManager из SystemBase

        // Получаем буфер с картой чанков
        var mapDataEntity = singleton.ChunkMapDataEntity;
        var chunkMapBuffer = entityManager.GetBuffer<ChunkMapEntry>(mapDataEntity);

        // Считаем количество загруженных чанков
        int loadedChunkCount = 0;
        foreach (var entry in chunkMapBuffer)
        {
            if (entry.State == ChunkState.Loaded)
            {
                loadedChunkCount++;
            }
        }

        //Debug.Log($"ChunkTestSystem: Found {loadedChunkCount} loaded chunks out of {chunkMapBuffer.Length} total entries in map.");

        // Вывести ВСЕ ID чанков из буфера и их состояния
        //Debug.Log("ChunkMapBuffer contents:");
        foreach (var entry in chunkMapBuffer)
        {
            //Debug.Log($"  ID: ({entry.Id.x}, {entry.Id.y}), State: {entry.State}, Entity: {entry.Entity}");
        }

        // Проверка уникальности ID
        var seenIds = new Unity.Collections.NativeHashSet<int2>(chunkMapBuffer.Length, Unity.Collections.Allocator.Temp);
        bool hasDuplicates = false;
        foreach (var entry in chunkMapBuffer)
        {
            if (seenIds.Contains(entry.Id))
            {
                //Debug.LogError($"  ERROR: Duplicate Chunk ID found in map: ({entry.Id.x}, {entry.Id.y})!");
                hasDuplicates = true;
            }
            seenIds.Add(entry.Id);
        }
        if (!hasDuplicates)
        {
            //Debug.Log("  Chunk IDs in map are unique.");
        }
        seenIds.Dispose(); // Всегда освобождаем NativeContainer

        // Подсчёт Entity с компонентом Chunk
        var allChunkEntities = EntityManager.GetAllEntities(Unity.Collections.Allocator.Temp);
        int totalChunkEntities = 0;
        foreach(var ent in allChunkEntities)
        {
            if (EntityManager.HasComponent<Chunk>(ent))
            {
                totalChunkEntities++;
            }
        }
        //Debug.Log($"Total Chunk Entities in World: {totalChunkEntities}");
        allChunkEntities.Dispose(); // Всегда освобождаем NativeArray

        //Debug.Log("--- Chunk Test Complete ---");
    }
}