using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

// Система следования NPC по вычисленному пути
// ИСПРАВЛЕНО: сохранение Y координаты, улучшенная логика движения
[BurstCompile]
[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateAfter(typeof(GoalPathIntegrationSystem))]
public partial struct PathFollowingSystem : ISystem
{
    private const float STUCK_THRESHOLD = 0.05f;
    private const float STUCK_TIMEOUT = 2.5f;
    
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
    }
    
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var deltaTime = SystemAPI.Time.DeltaTime;
        
        foreach (var (follower, location, waypointBuffer, entity) in 
                 SystemAPI.Query<RefRW<PathFollower>, RefRW<Location>, DynamicBuffer<PathWaypoint>>()
                 .WithEntityAccess())
        {
            if (follower.ValueRO.State != PathFollowerState.Following)
                continue;
            
            if (waypointBuffer.Length == 0)
            {
                follower.ValueRW.State = PathFollowerState.Completed;
                UnityEngine.Debug.LogWarning($"<color=yellow>Entity {entity.Index}: No waypoints, marking as Completed</color>");
                continue;
            }
            
            if (follower.ValueRO.CurrentWaypointIndex >= waypointBuffer.Length)
            {
                follower.ValueRW.State = PathFollowerState.Completed;
                UnityEngine.Debug.Log($"<color=green>Entity {entity.Index}: Path completed!</color>");
                continue;
            }
            
            var currentPos3D = location.ValueRO.GlobalPosition3D;
            var currentPos2D = new float2(currentPos3D.x, currentPos3D.z);
            
            var targetWaypoint = waypointBuffer[follower.ValueRO.CurrentWaypointIndex];
            var targetPos2D = targetWaypoint.Position;
            var targetPos3D = new float3(targetPos2D.x, currentPos3D.y, targetPos2D.y);
            
            var direction2D = targetPos2D - currentPos2D;
            var distance = math.length(direction2D);
            
            if (distance <= follower.ValueRO.ArrivalThreshold)
            {
                follower.ValueRW.CurrentWaypointIndex++;
                follower.ValueRW.StuckTimer = 0f;
                follower.ValueRW.LastPosition = currentPos2D;
                
                if (follower.ValueRO.CurrentWaypointIndex % 5 == 0 || 
                    follower.ValueRO.CurrentWaypointIndex >= waypointBuffer.Length - 1)
                {
                    UnityEngine.Debug.Log($"<color=cyan>Entity {entity.Index}: Waypoint {follower.ValueRO.CurrentWaypointIndex}/{waypointBuffer.Length}</color>");
                }
                
                continue;
            }
            
            var moveDistance = follower.ValueRO.Speed * deltaTime;
            
            if (moveDistance >= distance)
            {
                location.ValueRW.UpdatePosition(targetPos3D);
            }
            else
            {
                var direction3D = targetPos3D - currentPos3D;
                var normalizedDirection3D = math.normalize(direction3D);
                var newPos3D = currentPos3D + normalizedDirection3D * moveDistance;
                
                location.ValueRW.UpdatePosition(newPos3D);
            }
            
            var actualMovedDistance = math.distance(currentPos2D, follower.ValueRO.LastPosition);
            var expectedMovement = follower.ValueRO.Speed * deltaTime;
            
            if (actualMovedDistance < STUCK_THRESHOLD && expectedMovement > STUCK_THRESHOLD)
            {
                follower.ValueRW.StuckTimer += deltaTime;
                
                if (follower.ValueRW.StuckTimer >= STUCK_TIMEOUT)
                {
                    follower.ValueRW.State = PathFollowerState.Stuck;
                    
                    // ИСПРАВЛЕНО: Убран формат :F3 (не поддерживается Burst)
                    var movedDistanceMm = (int)(actualMovedDistance * 1000); // миллиметры
                    var timeoutSec = (int)STUCK_TIMEOUT;
                    UnityEngine.Debug.LogWarning($"<color=red>Entity {entity.Index}: STUCK! Moved {movedDistanceMm}mm in {timeoutSec}s</color>");
                }
            }
            else
            {
                follower.ValueRW.StuckTimer = 0f;
            }
            
            follower.ValueRW.LastPosition = currentPos2D;
        }
    }
}
