using Unity.Entities;
using Unity.Mathematics;

// ECS-компонент, прикрепляемый к синглтон Entity для хранения глобальных данных чанков
public struct ChunkMapSingleton : IComponentData
{
    // Entity, внутри которого будет Buffer<ChunkMapEntry> или другой способ хранения.
    // Это позволяет обойти ограничение IComponentData на NativeContainers.
    public Entity ChunkMapDataEntity; 

    // Ссылка на Prefab для создания чанков
    public Entity ChunkPrefab;
    
    // (Опционально) Радиус загрузки
    public int ViewDistanceInChunks;

    public ChunkMapSingleton(Entity chunkPrefab, Entity mapDataEntity, int viewDistance = ChunkConstants.VIEW_DISTANCE_IN_CHUNKS)
    {
        ChunkMapDataEntity = mapDataEntity;
        ChunkPrefab = chunkPrefab;
        ViewDistanceInChunks = viewDistance;
    }
}

// Буферный элемент для хранения записи ID -> Entity
public struct ChunkMapEntry : IBufferElementData
{
    public int2 Id;       // ID чанка
    public Entity Entity; // Соответствующий Entity
    public ChunkState State; // Состояние (для удобства)

    public ChunkMapEntry(int2 id, Entity entity, ChunkState state)
    {
        Id = id;
        Entity = entity;
        State = state;
    }
}

// Компонент для сущности, хранящей сам буфер ChunkMapEntry
public struct ChunkMapBufferData : IComponentData
{
    // Пустой компонент, просто для идентификации сущности, содержащей Buffer<ChunkMapEntry>
}