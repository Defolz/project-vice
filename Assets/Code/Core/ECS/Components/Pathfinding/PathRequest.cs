using Unity.Entities;
using Unity.Mathematics;

// Компонент-запрос на построение пути
// Добавляется к entity, которому нужен путь
public struct PathRequest : IComponentData
{
    public float2 StartPosition;    // Стартовая позиция (мировая)
    public float2 TargetPosition;   // Целевая позиция (мировая)
    public PathStatus Status;       // Текущий статус запроса
    public float RequestTime;       // Время создания запроса (для timeout)
    public int MaxPathLength;       // Максимальная длина пути (для ограничения)
    
    public PathRequest(float2 start, float2 target, float requestTime, int maxPathLength = 512)
    {
        StartPosition = start;
        TargetPosition = target;
        Status = PathStatus.Pending;
        RequestTime = requestTime;
        MaxPathLength = maxPathLength;
    }
}

// Статус запроса пути
public enum PathStatus : byte
{
    Pending = 0,        // Ожидает обработки
    Processing = 1,     // В процессе вычисления
    Success = 2,        // Путь найден
    Failed = 3,         // Путь не найден
    Timeout = 4         // Превышено время ожидания
}
