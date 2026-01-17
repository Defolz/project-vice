using Unity.Entities;
using Unity.Mathematics;

public enum GoalType : sbyte
{
    None = 0,
    Idle,
    MoveToLocation,
    PatrolArea,
    FollowTarget,
    AttackTarget,
    DefendLocation,
    Work,
    Sleep,
    Eat,
    Socialize,
    Flee,
    Investigate,
    Retaliate, // Ответная реакция на события (например, нападение на семью)
    VisitLocation,
    EscortTarget
}

// Компонент, хранящий текущую цель NPC и связанные с ней параметры
public struct CurrentGoal : IComponentData
{
    public GoalType Type;              // Тип цели
    public Entity TargetEntity;        // Целевой Entity (например, враг, сопровождаемый, место назначения)
    public float3 TargetPosition;      // Целевая позиция (если не связано с Entity)
    public float ExpiryTime;           // Время, когда цель станет недействительной (в секундах от начала игры)
    public float Priority;             // Приоритет цели (0.0f - низкий, 1.0f - высокий), для планировщика целей

    public CurrentGoal(GoalType type, Entity targetEntity = default, float3 targetPosition = default, 
                       float expiryTime = -1f, float priority = 0.5f)
    {
        Type = type;
        TargetEntity = targetEntity;
        TargetPosition = targetPosition;
        ExpiryTime = expiryTime;
        Priority = math.clamp(priority, 0.0f, 1.0f);
    }

    // Проверяет, истекло ли время действия цели
    public bool IsExpired(float currentTime)
    {
        return ExpiryTime > 0 && currentTime > ExpiryTime;
    }

    // Проверяет, действительна ли цель (тип не None и не истекло время)
    public bool IsActive(float currentTime)
    {
        return Type != GoalType.None && !IsExpired(currentTime);
    }

    public override string ToString()
    {
        var targetInfo = TargetEntity != Entity.Null ? $"TargetEnt:{TargetEntity.Index}" : 
                         (TargetPosition.Equals(float3.zero) ? "NoPos" : $"Pos:{TargetPosition}");
        
        return $"Goal({Type}, {targetInfo}, Prio:{Priority:F2}, Exp:{ExpiryTime:F1}s)";
    }
}