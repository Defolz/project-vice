using Unity.Entities;
using Unity.Mathematics;

// Компонент для следования по пути
public struct PathFollower : IComponentData
{
    public int CurrentWaypointIndex; // Индекс текущего waypoint
    public float ArrivalThreshold;   // Порог прибытия к waypoint (метры)
    public float Speed;              // Скорость движения (м/с)
    public PathFollowerState State;  // Текущее состояние
    public float StuckTimer;         // Таймер для определения застревания
    public float2 LastPosition;      // Последняя позиция (для детекта застревания)
    
    public PathFollower(float speed, float arrivalThreshold = 0.5f)
    {
        CurrentWaypointIndex = 0;
        ArrivalThreshold = arrivalThreshold;
        Speed = speed;
        State = PathFollowerState.Idle;
        StuckTimer = 0f;
        LastPosition = float2.zero;
    }
    
    // Проверить, достиг ли entity конца пути
    public bool IsPathComplete(int waypointCount)
    {
        return CurrentWaypointIndex >= waypointCount;
    }
    
    // Получить прогресс (0-1)
    public float GetProgress(int waypointCount)
    {
        if (waypointCount == 0) return 0f;
        return math.clamp((float)CurrentWaypointIndex / waypointCount, 0f, 1f);
    }
}

public enum PathFollowerState : byte
{
    Idle = 0,           // Нет активного пути
    Following = 1,      // Следует по пути
    Paused = 2,         // Приостановлено
    Stuck = 3,          // Застрял
    Completed = 4       // Путь завершён
}
