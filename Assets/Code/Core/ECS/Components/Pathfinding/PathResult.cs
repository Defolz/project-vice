using Unity.Entities;
using Unity.Mathematics;

// Элемент пути - waypoint
public struct PathWaypoint : IBufferElementData
{
    public float2 Position;         // Мировая позиция waypoint
    public float Distance;          // Расстояние от старта до этой точки
    
    public PathWaypoint(float2 position, float distance)
    {
        Position = position;
        Distance = distance;
    }
}

// Компонент результата pathfinding
public struct PathResult : IComponentData
{
    public PathStatus Status;       // Статус пути
    public float TotalDistance;     // Общая длина пути
    public float CalculationTime;   // Время вычисления (в секундах)
    public int WaypointCount;       // Количество waypoints
    public bool IsValid;            // Валиден ли путь
    
    public PathResult(PathStatus status, float totalDistance, float calculationTime, int waypointCount)
    {
        Status = status;
        TotalDistance = totalDistance;
        CalculationTime = calculationTime;
        WaypointCount = waypointCount;
        IsValid = status == PathStatus.Success && waypointCount > 0;
    }
    
    public static PathResult Failed()
    {
        return new PathResult(PathStatus.Failed, 0, 0, 0);
    }
}
