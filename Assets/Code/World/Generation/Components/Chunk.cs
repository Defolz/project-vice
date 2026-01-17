using Unity.Entities;
using Unity.Mathematics;

// ECS-компонент, представляющий уникальный чанк мира (теперь 2D X-Y)
public struct Chunk : IComponentData
{
    public int2 Id; // Уникальный ID чанка (x, y) в сетке 2D X-Y
    // Мировая позиция "угла" чанка (левый нижний) в 2D X-Y
    public float2 WorldPosition;

    public ChunkState State; // Текущее состояние чанка

    public Chunk(int2 id, float2 worldPos, ChunkState state = ChunkState.Unloaded)
    {
        Id = id;
        WorldPosition = worldPos;
        State = state;
    }

    // Получить мировые 2D границы чанка (X-Y)
    public float2 MinBound => WorldPosition;
    public float2 MaxBound => WorldPosition + new float2(ChunkConstants.CHUNK_SIZE, ChunkConstants.CHUNK_SIZE);
    
    // Проверить, находится ли 2D точка (X-Y) внутри чанка
    public bool ContainsPoint(float2 point)
    {
        return point.x >= MinBound.x && point.x < MaxBound.x &&
               point.y >= MinBound.y && point.y < MaxBound.y;
    }
}

// Перечисление состояний чанка
public enum ChunkState : sbyte
{
    Unloaded = 0,  // Не создан/загружен
    Generating,    // Содержимое генерируется
    Loaded,        // Полностью загружен и активен
    Dirty,         // Изменён, требует сохранения или пересчёта
    Unloading      // Выгружается
}