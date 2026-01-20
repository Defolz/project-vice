using Unity.Entities;
using Unity.Mathematics;

// Компонент статического препятствия (круглая область)
public struct StaticObstacle : IComponentData
{
    public float2 Position; // Глобальная позиция в мире (X-Y)
    public float Radius;    // Радиус препятствия в метрах
    public ObstacleType Type;
    
    public StaticObstacle(float2 position, float radius, ObstacleType type = ObstacleType.Building)
    {
        Position = position;
        Radius = radius;
        Type = type;
    }
    
    // Проверить, перекрывается ли препятствие с точкой
    public bool ContainsPoint(float2 point)
    {
        return math.distance(Position, point) < Radius;
    }
    
    // Получить ID чанка, в котором находится препятствие
    public int2 GetChunkId()
    {
        var x = (int)math.floor(Position.x / ChunkConstants.CHUNK_SIZE);
        var y = (int)math.floor(Position.y / ChunkConstants.CHUNK_SIZE);
        return new int2(x, y);
    }
}

// Типы препятствий
public enum ObstacleType : byte
{
    Building = 0,
    Tree = 1,
    Rock = 2,
    Water = 3,
    Custom = 255
}
