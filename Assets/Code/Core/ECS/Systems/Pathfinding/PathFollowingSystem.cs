using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

// Система следования NPC по вычисленному пути
// ИСПРАВЛЕНО: улучшенная логика движения, лучшая детекция застревания
[BurstCompile]
[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateAfter(typeof(GoalPathIntegrationSystem))]
public partial struct PathFollowingSystem : ISystem
{
    private const float STUCK_THRESHOLD = 0.05f; // Минимальное движение за секунду (м/с)
    private const float STUCK_TIMEOUT = 2.5f; // Время до признания "застрял"
    
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
    }
    
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var deltaTime = SystemAPI.Time.DeltaTime;
        
        // Обрабатываем все entity с PathFollower
        foreach (var (follower, location, waypointBuffer, entity) in 
                 SystemAPI.Query<RefRW<PathFollower>, RefRW<Location>, DynamicBuffer<PathWaypoint>>()
                 .WithEntityAccess())
        {
            // Пропускаем, если не Following
            if (follower.ValueRO.State != PathFollowerState.Following)
                continue;
            
            // Проверяем наличие waypoints
            if (waypointBuffer.Length == 0)
            {
                follower.ValueRW.State = PathFollowerState.Completed;
                UnityEngine.Debug.LogWarning($"<color=yellow>Entity {entity.Index}: No waypoints, marking as Completed</color>");
                continue;
            }
            
            // Проверяем завершение пути
            if (follower.ValueRO.CurrentWaypointIndex >= waypointBuffer.Length)
            {
                follower.ValueRW.State = PathFollowerState.Completed;
                UnityEngine.Debug.Log($"<color=green>Entity {entity.Index}: Path completed! ({follower.ValueRO.CurrentWaypointIndex}/{waypointBuffer.Length})</color>");
                continue;
            }
            
            // Получаем текущую позицию
            var currentPos = location.ValueRO.GlobalPosition2D;
            
            // Получаем целевой waypoint
            var targetWaypoint = waypointBuffer[follower.ValueRO.CurrentWaypointIndex];
            var targetPos = targetWaypoint.Position;
            
            // Вычисляем направление и расстояние
            var direction = targetPos - currentPos;
            var distance = math.length(direction);
            
            // Достигли waypoint?
            if (distance <= follower.ValueRO.ArrivalThreshold)
            {
                follower.ValueRW.CurrentWaypointIndex++;
                follower.ValueRW.StuckTimer = 0f;
                follower.ValueRW.LastPosition = currentPos;
                
                // Логируем прогресс каждые 5 waypoints
                if (follower.ValueRO.CurrentWaypointIndex % 5 == 0 || 
                    follower.ValueRO.CurrentWaypointIndex >= waypointBuffer.Length - 1)
                {
                    UnityEngine.Debug.Log($"<color=cyan>Entity {entity.Index}: Waypoint {follower.ValueRO.CurrentWaypointIndex}/{waypointBuffer.Length}</color>");
                }
                
                continue;
            }
            
            // Двигаемся к waypoint
            var moveDistance = follower.ValueRO.Speed * deltaTime;
            
            if (moveDistance >= distance)
            {
                // Достигаем waypoint напрямую
                location.ValueRW.UpdatePosition(targetPos);
            }
            else
            {
                // Движемся на фиксированное расстояние
                var normalizedDirection = direction / distance; // Normalize
                var newPos = currentPos + normalizedDirection * moveDistance;
                location.ValueRW.UpdatePosition(newPos);
            }
            
            // ИСПРАВЛЕНО: Детекция застревания
            var actualMovedDistance = math.distance(currentPos, follower.ValueRO.LastPosition);
            var expectedMovement = follower.ValueRO.Speed * deltaTime;
            
            // Если двигаемся слишком медленно
            if (actualMovedDistance < STUCK_THRESHOLD && expectedMovement > STUCK_THRESHOLD)
            {
                follower.ValueRW.StuckTimer += deltaTime;
                
                if (follower.ValueRW.StuckTimer >= STUCK_TIMEOUT)
                {
                    follower.ValueRW.State = PathFollowerState.Stuck;
                    UnityEngine.Debug.LogWarning($"<color=red>Entity {entity.Index}: STUCK! Moved {actualMovedDistance:G}m in {STUCK_TIMEOUT}s</color>");
                }
            }
            else
            {
                // Успешное движение - сбрасываем таймер
                follower.ValueRW.StuckTimer = 0f;
            }
            
            // Обновляем последнюю позицию для следующего кадра
            follower.ValueRW.LastPosition = currentPos;
        }
    }
}
