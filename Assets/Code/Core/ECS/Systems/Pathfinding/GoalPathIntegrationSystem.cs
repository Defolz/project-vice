using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

// Система интеграции pathfinding с AI Goals
// ИСПРАВЛЕНО: правильное управление компонентами, улучшенное логирование
[BurstCompile]
[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateAfter(typeof(AStarPathfindingSystem))]
public partial struct GoalPathIntegrationSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
    }
    
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var currentTime = (float)SystemAPI.Time.ElapsedTime;
        var ecb = new EntityCommandBuffer(Allocator.TempJob);
        
        // 1. Создаём PathRequest для NPC с целью движения
        foreach (var (goal, location, entity) in 
                 SystemAPI.Query<RefRO<CurrentGoal>, RefRO<Location>>()
                 .WithNone<PathRequest, PathResult>()
                 .WithEntityAccess())
        {
            if (goal.ValueRO.Type != GoalType.MoveToLocation && 
                goal.ValueRO.Type != GoalType.VisitLocation)
                continue;
            
            if (!goal.ValueRO.IsActive(currentTime))
                continue;
            
            var startPos = location.ValueRO.GlobalPosition2D;
            var targetPos = new float2(goal.ValueRO.TargetPosition.x, goal.ValueRO.TargetPosition.z);
            
            ecb.AddComponent(entity, new PathRequest(startPos, targetPos, currentTime));
            
            UnityEngine.Debug.Log($"<color=cyan>🎯 PathRequest created for Entity {entity.Index}: {startPos} -> {targetPos}</color>");
        }
        
        // 2. Обрабатываем успешные PathResult
        foreach (var (pathResult, entity) in 
                 SystemAPI.Query<RefRO<PathResult>>()
                 .WithNone<PathFollower>()
                 .WithEntityAccess())
        {
            if (pathResult.ValueRO.IsValid)
            {
                // ИСПРАВЛЕНО: Устанавливаем состояние Following
                var pathFollower = new PathFollower(
                    speed: 3.5f,
                    arrivalThreshold: 1.5f
                );
                pathFollower.State = PathFollowerState.Following;
                ecb.AddComponent(entity, pathFollower);
                
                UnityEngine.Debug.Log($"<color=green>✅ PathFollower added to Entity {entity.Index}! " +
                    $"Waypoints: {pathResult.ValueRO.WaypointCount}, Distance: {pathResult.ValueRO.TotalDistance:G}m</color>");
            }
            else
            {
                // Путь не найден
                UnityEngine.Debug.LogWarning($"<color=red>❌ Path FAILED for Entity {entity.Index}</color>");
                
                ecb.RemoveComponent<PathResult>(entity);
                
                // Сбрасываем цель в Idle
                var goal = SystemAPI.GetComponent<CurrentGoal>(entity);
                goal.Type = GoalType.Idle;
                ecb.SetComponent(entity, goal);
            }
        }
        
        // 3. ИСПРАВЛЕНО: Правильно очищаем завершённые пути
        foreach (var (follower, goal, entity) in 
                 SystemAPI.Query<RefRO<PathFollower>, RefRW<CurrentGoal>>()
                 .WithEntityAccess())
        {
            if (follower.ValueRO.State == PathFollowerState.Completed)
            {
                // Цель достигнута
                goal.ValueRW.Type = GoalType.Idle;
                
                // ИСПРАВЛЕНО: Удаляем ВСЕ компоненты pathfinding
                ecb.RemoveComponent<PathFollower>(entity);
                ecb.RemoveComponent<PathResult>(entity);
                
                var buffer = ecb.SetBuffer<PathWaypoint>(entity);
                buffer.Clear();
                
                UnityEngine.Debug.Log($"<color=green>🎉 Entity {entity.Index} reached destination!</color>");
            }
            else if (follower.ValueRO.State == PathFollowerState.Stuck)
            {
                // Застрял - перерасчитываем путь
                UnityEngine.Debug.LogWarning($"<color=yellow>⚠️ Entity {entity.Index} is STUCK! Recalculating path...</color>");
                
                ecb.RemoveComponent<PathFollower>(entity);
                ecb.RemoveComponent<PathResult>(entity);
                
                var buffer = ecb.SetBuffer<PathWaypoint>(entity);
                buffer.Clear();
                
                // Запрашиваем новый путь
                var location = SystemAPI.GetComponent<Location>(entity);
                var startPos = location.GlobalPosition2D;
                var targetPos = new float2(goal.ValueRO.TargetPosition.x, goal.ValueRO.TargetPosition.z);
                
                ecb.AddComponent(entity, new PathRequest(startPos, targetPos, currentTime));
            }
        }
        
        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }
}
