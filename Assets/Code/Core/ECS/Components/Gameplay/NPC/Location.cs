using Unity.Entities;
using Unity.Mathematics;

public struct Location : IComponentData
{
    public int2 ChunkId; // ID чанка, в котором находится NPC

    // Позиция внутри чанка теперь в 3D (X, Y, Z), где Y — высота
    public float3 PositionInChunk; // Позиция внутри чанка (локальная, 3D X-Y-Z)

    // Вспомогательное свойство для получения глобальной 3D позиции
    public float3 GlobalPosition3D => new float3(
        ChunkId.x * ChunkConstants.CHUNK_SIZE + PositionInChunk.x,
        PositionInChunk.y, // Высота
        ChunkId.y * ChunkConstants.CHUNK_SIZE + PositionInChunk.z
    );

    // Совместимость с 2D: проецируем Z в Y
    public float2 GlobalPosition2D => new float2(GlobalPosition3D.x, GlobalPosition3D.z);

    public Location(int2 chunkId, float3 localPosition)
    {
        ChunkId = chunkId;
        PositionInChunk = localPosition;
        
        // Валидация: проверяем, что позиция внутри чанка в допустимых пределах
#if ENABLE_UNITY_COLLECTIONS_CHECKS
        if (localPosition.x < 0 || localPosition.x >= ChunkConstants.CHUNK_SIZE ||
            localPosition.z < 0 || localPosition.z >= ChunkConstants.CHUNK_SIZE)
        {
            UnityEngine.Debug.LogWarning($"Local position {localPosition} is outside chunk bounds [0, {ChunkConstants.CHUNK_SIZE}]. Position will be clamped.");
            PositionInChunk = new float3(
                Unity.Mathematics.math.clamp(localPosition.x, 0, ChunkConstants.CHUNK_SIZE - 0.01f),
                localPosition.y,
                Unity.Mathematics.math.clamp(localPosition.z, 0, ChunkConstants.CHUNK_SIZE - 0.01f)
            );
        }
#endif
    }

    // Совместимость: конструктор из float2 (Z = 0)
    public Location(int2 chunkId, float2 localPosition) : this(chunkId, new float3(localPosition.x, 0, localPosition.y)) {}

    /// <summary>
    /// Создает Location из 2D глобальной позиции (X-Y), автоматически вычисляя ChunkId и PositionInChunk.
    /// </summary>
    public static Location FromGlobal(float2 globalPos2D)
    {
        var chunkX = (int)math.floor(globalPos2D.x / ChunkConstants.CHUNK_SIZE);
        var chunkY = (int)math.floor(globalPos2D.y / ChunkConstants.CHUNK_SIZE);

        var localX = globalPos2D.x - (chunkX * ChunkConstants.CHUNK_SIZE);
        var localZ = globalPos2D.y - (chunkY * ChunkConstants.CHUNK_SIZE);

        return new Location(new int2(chunkX, chunkY), new float3(localX, 0, localZ));
    }

    /// <summary>
    /// Создает Location из 3D глобальной позиции (X-Y-Z), автоматически вычисляя ChunkId и PositionInChunk.
    /// </summary>
    public static Location FromGlobal(float3 globalPos3D)
    {
        var chunkX = (int)math.floor(globalPos3D.x / ChunkConstants.CHUNK_SIZE);
        var chunkY = (int)math.floor(globalPos3D.z / ChunkConstants.CHUNK_SIZE); // Z → Y в чанке

        var localX = globalPos3D.x - (chunkX * ChunkConstants.CHUNK_SIZE);
        var localY = globalPos3D.y; // Высота
        var localZ = globalPos3D.z - (chunkY * ChunkConstants.CHUNK_SIZE);

        return new Location(new int2(chunkX, chunkY), new float3(localX, localY, localZ));
    }

    // Метод для обновления позиции и, при необходимости, ChunkId
    public void UpdatePosition(float2 newGlobalPos2D)
    {
        this = FromGlobal(newGlobalPos2D);
    }

    // Метод для обновления позиции в 3D
    public void UpdatePosition(float3 newGlobalPos3D)
    {
        this = FromGlobal(newGlobalPos3D);
    }

    public override string ToString()
    {
        return $"Location(Chunk:{ChunkId}, Local:{PositionInChunk})";
    }
}