using Unity.Entities;
using Unity.Collections;

// Диагностическая система для отладки Pathfinding
// Показывает состояние всех компонентов каждые 2 секунды
[UpdateInGroup(typeof(SimulationSystemGroup))]
public partial struct PathfindingDiagnosticSystem : ISystem
{
    private float lastDiagnosticTime;
    private const float DIAGNOSTIC_INTERVAL = 2f; // Каждые 2 секунды
    
    public void OnCreate(ref SystemState state)
    {
        lastDiagnosticTime = 0f;
    }
    
    public void OnUpdate(ref SystemState state)
    {
        var currentTime = (float)SystemAPI.Time.ElapsedTime;
        
        if (currentTime - lastDiagnosticTime < DIAGNOSTIC_INTERVAL)
            return;
            
        lastDiagnosticTime = currentTime;
        
        // Подсчитываем entity с разными компонентами
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
        
        UnityEngine.Debug.Log($"<color=cyan>📊 PATHFINDING DIAGNOSTIC (Time: {currentTime:F1}s)</color>\n" +
            $"  🎯 Entities with CurrentGoal: {goalCount}\n" +
            $"  📝 Entities with PathRequest: {requestCount}\n" +
            $"  ✅ Entities with PathResult: {resultCount}\n" +
            $"  🚶 Entities with PathFollower: {followerCount}\n" +
            $"  📍 Entities with PathWaypoint buffer: {waypointBufferCount}");
        
        // Детальная информация о PathFollowers
        if (followerCount > 0)
        {
            foreach (var (follower, entity) in SystemAPI.Query<RefRO<PathFollower>>().WithEntityAccess())
            {
                var waypointBuffer = state.EntityManager.GetBuffer<PathWaypoint>(entity);
                UnityEngine.Debug.Log($"  🚶 Entity {entity.Index}: State={follower.ValueRO.State}, " +
                    $"Waypoint {follower.ValueRO.CurrentWaypointIndex}/{waypointBuffer.Length}, " +
                    $"Speed={follower.ValueRO.Speed:F1}");
            }
        }
        
        // Проверяем NavigationGrid
        var gridQuery = SystemAPI.QueryBuilder().WithAll<NavigationGrid, Chunk>().Build();
        var gridCount = gridQuery.CalculateEntityCount();
        UnityEngine.Debug.Log($"  🗺️  NavigationGrid chunks: {gridCount}");
        
        if (gridCount == 0)
        {
            UnityEngine.Debug.LogWarning("<color=red>⚠️  NO NavigationGrid found! Pathfinding will FAIL!</color>");
        }
    }
}