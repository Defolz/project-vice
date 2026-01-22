using Unity.Entities;
using Unity.Collections;
using UnityEngine;

// Детальная диагностическая система для отладки Pathfinding
// ВАЖНО: Использует только Burst-совместимые форматы!
[UpdateInGroup(typeof(SimulationSystemGroup))]
public partial struct PathfindingDiagnosticSystem : ISystem
{
    private float lastDiagnosticTime;
    private const float DIAGNOSTIC_INTERVAL = 2f;
    
    public void OnCreate(ref SystemState state)
    {
        lastDiagnosticTime = 0f;
        state.Enabled = false;
    }
    
    public void OnUpdate(ref SystemState state)
    {
        state.Enabled = false;
        var currentTime = (float)SystemAPI.Time.ElapsedTime;
        
        if (currentTime - lastDiagnosticTime < DIAGNOSTIC_INTERVAL)
            return;
            
        lastDiagnosticTime = currentTime;
        
        // === БАЗОВАЯ СТАТИСТИКА ===
        var goalsQuery = SystemAPI.QueryBuilder().WithAll<CurrentGoal>().Build();
        var requestsQuery = SystemAPI.QueryBuilder().WithAll<PathRequest>().Build();
        var resultsQuery = SystemAPI.QueryBuilder().WithAll<PathResult>().Build();
        var followersQuery = SystemAPI.QueryBuilder().WithAll<PathFollower>().Build();
        var waypointsQuery = SystemAPI.QueryBuilder().WithAll<PathWaypoint>().Build();
        
        var goalCount = goalsQuery.CalculateEntityCount();
        var requestCount = requestsQuery.CalculateEntityCount();
        var resultCount = resultsQuery.CalculateEntityCount();
        var followerCount = followersQuery.CalculateEntityCount();
        var waypointBufferCount = waypointsQuery.CalculateEntityCount();
        
        Debug.Log($"<color=cyan>=== PATHFINDING DIAGNOSTIC (t={currentTime:G}) ===</color>");
        Debug.Log($"  CurrentGoal: {goalCount}");
        Debug.Log($"  PathRequest: {requestCount}");
        Debug.Log($"  PathResult: {resultCount}");
        Debug.Log($"  PathFollower: {followerCount}");
        Debug.Log($"  PathWaypoint buffers: {waypointBufferCount}");
        
        // === ДЕТАЛЬНАЯ ИНФОРМАЦИЯ О FOLLOWERS ===
        if (followerCount > 0)
        {
            Debug.Log("<color=yellow>--- PathFollowers Details ---</color>");
            
            foreach (var (follower, entity) in SystemAPI.Query<RefRO<PathFollower>>().WithEntityAccess())
            {
                var hasWaypoints = state.EntityManager.HasBuffer<PathWaypoint>(entity);
                var waypointCount = hasWaypoints ? state.EntityManager.GetBuffer<PathWaypoint>(entity).Length : 0;
                
                Debug.Log($"  Entity {entity.Index}: State={follower.ValueRO.State}, " +
                         $"Waypoint {follower.ValueRO.CurrentWaypointIndex}/{waypointCount}, " +
                         $"HasBuffer={hasWaypoints}");
                
                // Показываем первые 3 waypoint
                if (hasWaypoints && waypointCount > 0)
                {
                    var buffer = state.EntityManager.GetBuffer<PathWaypoint>(entity);
                    for (int i = 0; i < Unity.Mathematics.math.min(3, buffer.Length); i++)
                    {
                        var wp = buffer[i];
                        Debug.Log($"    WP[{i}]: pos=({wp.Position.x:G}, {wp.Position.y:G}), dist={wp.Distance:G}");
                    }
                }
            }
        }
        else
        {
            Debug.LogWarning("<color=red>⚠️  NO PathFollowers found!</color>");
        }
        
        // === ДЕТАЛЬНАЯ ИНФОРМАЦИЯ О GOALS ===
        if (goalCount > 0)
        {
            Debug.Log("<color=yellow>--- CurrentGoals Details ---</color>");
            
            var goalEntities = goalsQuery.ToEntityArray(Allocator.Temp);
            
            for (int i = 0; i < Unity.Mathematics.math.min(5, goalEntities.Length); i++)
            {
                var entity = goalEntities[i];
                var goal = state.EntityManager.GetComponentData<CurrentGoal>(entity);
                
                var hasRequest = state.EntityManager.HasComponent<PathRequest>(entity);
                var hasResult = state.EntityManager.HasComponent<PathResult>(entity);
                var hasFollower = state.EntityManager.HasComponent<PathFollower>(entity);
                var hasLocation = state.EntityManager.HasComponent<Location>(entity);
                
                Debug.Log($"  Entity {entity.Index}: Type={goal.Type}, " +
                         $"Req={hasRequest}, Res={hasResult}, Fol={hasFollower}, Loc={hasLocation}");
                
                if (hasLocation)
                {
                    var loc = state.EntityManager.GetComponentData<Location>(entity);
                    Debug.Log($"    Pos: ({loc.GlobalPosition2D.x:G}, {loc.GlobalPosition2D.y:G})");
                }
            }
            
            goalEntities.Dispose();
        }
        
        // === ДЕТАЛЬНАЯ ИНФОРМАЦИЯ О REQUESTS ===
        if (requestCount > 0)
        {
            Debug.Log("<color=yellow>--- PathRequests Details ---</color>");
            
            foreach (var (request, entity) in SystemAPI.Query<RefRO<PathRequest>>().WithEntityAccess())
            {
                var age = currentTime - request.ValueRO.RequestTime;
                Debug.Log($"  Entity {entity.Index}: Status={request.ValueRO.Status}, Age={age:G}s");
                Debug.Log($"    Start: ({request.ValueRO.StartPosition.x:G}, {request.ValueRO.StartPosition.y:G})");
                Debug.Log($"    Target: ({request.ValueRO.TargetPosition.x:G}, {request.ValueRO.TargetPosition.y:G})");
            }
        }
        
        // === ПРОВЕРКА NavigationGrid ===
        var gridQuery = SystemAPI.QueryBuilder().WithAll<NavigationGrid, Chunk>().Build();
        var gridCount = gridQuery.CalculateEntityCount();
        
        Debug.Log($"  NavigationGrid chunks: {gridCount}");
        
        if (gridCount == 0)
        {
            Debug.LogError("<color=red>❌ NO NavigationGrid found! Pathfinding WILL FAIL!</color>");
        }
        
        Debug.Log("<color=cyan>===========================================</color>");
    }
}
