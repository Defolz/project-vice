using Unity.Entities;
using Unity.Mathematics;

public struct Location : IComponentData
{
    public int2 ChunkId; // ID чанка, в котором находится NPC
    // Позиция внутри чанка теперь в 2D (X, Y)
    public float2 PositionInChunk; // Позиция внутри чанка (локальная, 2D X-Y)

    // Вспомогательное свойство для получения глобальной 2D позиции
    public float2 GlobalPosition2D => new float2(
        ChunkId.x * ChunkConstants.CHUNK_SIZE + PositionInChunk.x,
        ChunkId.y * ChunkConstants.CHUNK_SIZE + PositionInChunk.y
    );
    
    // Если нужно передавать 3D позицию в систему рендера, Z можно использовать для слоя/высоты
    // Но для логики 2D игры Z обычно 0 или фиксирован
    public float3 GlobalPosition3D => new float3(GlobalPosition2D.x, GlobalPosition2D.y, 0); // Z=0 как базовая высота/слой

    public Location(int2 chunkId, float2 localPosition)
    {
        ChunkId = chunkId;
        PositionInChunk = localPosition;
    }
    
    /// <summary>
    /// Создает Location из 2D глобальной позиции (X-Y), автоматически вычисляя ChunkId и PositionInChunk.
    /// </summary>
    public static Location FromGlobal(float2 globalPos2D)
    {
        var chunkX = (int)math.floor(globalPos2D.x / ChunkConstants.CHUNK_SIZE);
        var chunkY = (int)math.floor(globalPos2D.y / ChunkConstants.CHUNK_SIZE);
        
        var localX = globalPos2D.x - (chunkX * ChunkConstants.CHUNK_SIZE);
        var localY = globalPos2D.y - (chunkY * ChunkConstants.CHUNK_SIZE);

        return new Location(new int2(chunkX, chunkY), new float2(localX, localY));
    }
    
    // Метод для обновления позиции и, при необходимости, ChunkId
    public void UpdatePosition(float2 newGlobalPos2D)
    {
        var newLoc = FromGlobal(newGlobalPos2D);
        ChunkId = newLoc.ChunkId;
        PositionInChunk = newLoc.PositionInChunk;
    }
    
    public override string ToString()
    {
        return $"Location(Chunk:{ChunkId}, Local:{PositionInChunk})";
    }
}