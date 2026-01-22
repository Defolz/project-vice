using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

// Система поведения патрулирования
// Управляет NPC, которые патрулируют территорию (полиция, банды)
[BurstCompile]
[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateAfter(typeof(GoalExecutionSystem))]
public partial struct PatrolBehaviorSystem : ISystem
{
    private const float PATROL_WAYPOINT_DISTANCE = 40f;
    private const int MIN_PATROL_WAYPOINTS = 3;
    private const int MAX_PATROL_WAYPOINTS = 6;
    private const float WAYPOINT_ARRIVAL_THRESHOLD = 2f;
    
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
    }
    
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var currentTime = (float)SystemAPI.Time.ElapsedTime;
        var random = Random.CreateFromIndex((uint)(currentTime * 1000f + 1));
        var ecb = new EntityCommandBuffer(Allocator.TempJob);
        
        foreach (var (goal, location, pathFollower, entity) in 
                 SystemAPI.Query<RefRW<CurrentGoal>, RefRO<Location>, RefRO<PathFollower>>()
                 .WithEntityAccess())
        {
            // Обрабатываем только патрулирование
            if (goal.ValueRO.Type != GoalType.PatrolArea)
                continue;
            
            // Если путь завершен - генерируем следующую точку патруля
            if (pathFollower.ValueRO.State == PathFollowerState.Completed)
            {
                var newPatrolPoint = GeneratePatrolWaypoint(
                    location.ValueRO.GlobalPosition2D,
                    ref random
                );
                
                // Обновляем цель с новой позицией
                goal.ValueRW = new CurrentGoal(
                    GoalType.PatrolArea,
                    targetPosition: new float3(newPatrolPoint.x, 0f, newPatrolPoint.y),
                    priority: goal.ValueRO.Priority,
                    expiryTime: goal.ValueRO.ExpiryTime
                );
                
                // Очищаем старый путь для генерации нового
                ecb.RemoveComponent<PathFollower>(entity);
                ecb.RemoveComponent<PathResult>(entity);
                
                UnityEngine.Debug.Log($"<color=yellow>Entity {entity.Index}: Next patrol waypoint</color>");
            }
        }
        
        // Инициализация патрулирования для новых NPC
        foreach (var (goal, location, entity) in 
                 SystemAPI.Query<RefRW<CurrentGoal>, RefRO<Location>>()
                 .WithNone<PathFollower, PathRequest>()
                 .WithEntityAccess())
        {
            if (goal.ValueRO.Type != GoalType.PatrolArea)
                continue;
            
            // Если targetPosition не установлена - генерируем первую точку
            if (goal.ValueRO.TargetPosition.Equals(float3.zero))
            {
                var firstWaypoint = GeneratePatrolWaypoint(
                    location.ValueRO.GlobalPosition2D,
                    ref random
                );
                
                goal.ValueRW = new CurrentGoal(
                    GoalType.PatrolArea,
                    targetPosition: new float3(firstWaypoint.x, 0f, firstWaypoint.y),
                    priority: goal.ValueRO.Priority,
                    expiryTime: goal.ValueRO.ExpiryTime
                );
            }
        }
        
        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }
    
    // Генерирует следующую точку патрулирования
    private static float2 GeneratePatrolWaypoint(float2 currentPosition, ref Random random)
    {
        var angle = random.NextFloat(0f, math.PI * 2f);
        var distance = random.NextFloat(PATROL_WAYPOINT_DISTANCE * 0.5f, PATROL_WAYPOINT_DISTANCE);
        
        var offset = new float2(
            math.cos(angle) * distance,
            math.sin(angle) * distance
        );
        
        return currentPosition + offset;
    }
}
