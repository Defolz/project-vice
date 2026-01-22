using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

// Система поведения NPC в состоянии Idle
// Обрабатывает простые действия: стоять на месте, небольшие случайные движения
[BurstCompile]
[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateAfter(typeof(GoalExecutionSystem))]
public partial struct IdleBehaviorSystem : ISystem
{
    private const float IDLE_MOVE_CHANCE = 0.05f; // 5% шанс двинуться
    private const float MAX_IDLE_MOVE_DISTANCE = 5f;
    private const float IDLE_MOVE_DURATION = 3f; // 3 секунды
    
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
    }
    
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var currentTime = (float)SystemAPI.Time.ElapsedTime;
        var random = Random.CreateFromIndex((uint)(currentTime * 1000f + 1));
        
        foreach (var (goal, location, stateFlags, entity) in 
                 SystemAPI.Query<RefRW<CurrentGoal>, RefRO<Location>, RefRO<StateFlags>>()
                 .WithEntityAccess())
        {
            // Обрабатываем только NPC в состоянии Idle
            if (goal.ValueRO.Type != GoalType.Idle)
                continue;
            
            // Пропускаем занятых NPC
            if (stateFlags.ValueRO.IsBusy || stateFlags.ValueRO.IsSleeping)
                continue;
            
            // С небольшим шансом создаем мини-цель движения
            if (random.NextFloat() < IDLE_MOVE_CHANCE)
            {
                var currentPos = location.ValueRO.GlobalPosition2D;
                var offset = new float2(
                    random.NextFloat(-MAX_IDLE_MOVE_DISTANCE, MAX_IDLE_MOVE_DISTANCE),
                    random.NextFloat(-MAX_IDLE_MOVE_DISTANCE, MAX_IDLE_MOVE_DISTANCE)
                );
                
                var targetPos = currentPos + offset;
                
                // Создаем временную цель движения
                goal.ValueRW = new CurrentGoal(
                    GoalType.MoveToLocation,
                    targetPosition: new float3(targetPos.x, 0f, targetPos.y),
                    priority: 0.2f,
                    expiryTime: currentTime + IDLE_MOVE_DURATION
                );
                
                UnityEngine.Debug.Log($"<color=grey>Entity {entity.Index}: Idle movement to nearby position</color>");
            }
        }
    }
}
