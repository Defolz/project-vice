using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

// Визуализатор путей для отладки pathfinding
// ИСПРАВЛЕНО: улучшенная визуализация, отладочная информация
public class PathVisualizer : MonoBehaviour
{
    [Header("Visualization Settings")]
    [SerializeField] private bool showPaths = true;
    [SerializeField] private bool showWaypoints = true;
    [SerializeField] private bool showCurrentWaypoint = true;
    [SerializeField] private bool showEntityInfo = true;
    
    [Header("Colors")]
    [SerializeField] private Color pathColor = new Color(0, 1, 0, 0.8f);
    [SerializeField] private Color waypointColor = new Color(1, 1, 0, 0.9f);
    [SerializeField] private Color currentWaypointColor = new Color(1, 0, 0, 1f);
    [SerializeField] private Color startColor = new Color(0, 0, 1, 1f); // Синий
    
    [Header("Sizes")]
    [SerializeField] private float waypointRadius = 0.5f;
    [SerializeField] private float currentWaypointRadius = 0.8f;
    
    [Header("Performance")]
    [SerializeField] private float maxDrawDistance = 200f;
    [SerializeField] private int maxPathsToDraw = 100;
    
    [Header("Debug")]
    [SerializeField] private bool verboseLogging = false;
    
    private World world;
    private EntityManager entityManager;
    private float lastDebugTime = 0f;
    private const float DEBUG_INTERVAL = 2f;
    
    void Start()
    {
        world = World.DefaultGameObjectInjectionWorld;
        if (world != null)
        {
            entityManager = world.EntityManager;
            Debug.Log("<color=cyan>PathVisualizer initialized!</color>");
        }
        else
        {
            Debug.LogError("<color=red>PathVisualizer: World not found!</color>");
        }
    }
    
    void OnDrawGizmos()
    {
        if (!Application.isPlaying || world == null || !world.IsCreated)
            return;
        
        if (!showPaths && !showWaypoints)
            return;
        
        DrawPaths();
    }
    
    void Update()
    {
        if (!Application.isPlaying || world == null || !world.IsCreated)
            return;
        
        if (!showPaths)
            return;
        
        DrawPathsRuntime();
    }
    
    void DrawPaths()
    {
        if (entityManager == null)
            return;
        
        var cameraPos = Camera.current != null ? Camera.current.transform.position : Camera.main != null ? Camera.main.transform.position : Vector3.zero;
        var cameraPos2D = new float2(cameraPos.x, cameraPos.z);
        
        int pathsDrawn = 0;
        
        // Query для entity с PathFollower
        var query = entityManager.CreateEntityQuery(
            ComponentType.ReadOnly<PathFollower>(),
            ComponentType.ReadOnly<Location>(),
            ComponentType.ReadOnly<PathWaypoint>()
        );
        
        var entityCount = query.CalculateEntityCount();
        
        bool shouldLog = Time.time - lastDebugTime > DEBUG_INTERVAL;
        if (shouldLog)
        {
            lastDebugTime = Time.time;
            Debug.Log($"<color=cyan>📍 PathVisualizer: {entityCount} entities with paths</color>");
        }
        
        if (entityCount == 0)
        {
            if (shouldLog && verboseLogging)
            {
                Debug.LogWarning("<color=yellow>⚠️ No entities with PathFollower + Location + PathWaypoint found!</color>");
            }
            query.Dispose();
            return;
        }
        
        var entities = query.ToEntityArray(Allocator.Temp);
        
        foreach (var entity in entities)
        {
            if (pathsDrawn >= maxPathsToDraw)
                break;
            
            var location = entityManager.GetComponentData<Location>(entity);
            var follower = entityManager.GetComponentData<PathFollower>(entity);
            var waypointBuffer = entityManager.GetBuffer<PathWaypoint>(entity);
            
            // Distance culling
            if (maxDrawDistance > 0)
            {
                var distance = math.distance(cameraPos2D, location.GlobalPosition2D);
                if (distance > maxDrawDistance)
                    continue;
            }
            
            if (waypointBuffer.Length == 0)
            {
                if (shouldLog && verboseLogging)
                    Debug.LogWarning($"Entity {entity.Index}: Empty waypoint buffer");
                continue;
            }
            
            pathsDrawn++;
            
            // ИСПРАВЛЕНО: Рисуем стартовую позицию NPC
            var npcPos = location.GlobalPosition2D;
            var npcPos3D = new Vector3(npcPos.x, 0.5f, npcPos.y); // ВАЖНО: Y=0.5 для видимости
            
            Gizmos.color = startColor;
            Gizmos.DrawWireSphere(npcPos3D, 0.7f);
            Gizmos.DrawSphere(npcPos3D, 0.3f);
            
            // Рисуем линию от NPC к первому waypoint
            if (follower.CurrentWaypointIndex < waypointBuffer.Length)
            {
                var firstWaypoint = waypointBuffer[follower.CurrentWaypointIndex];
                var firstPos3D = new Vector3(firstWaypoint.Position.x, 0.5f, firstWaypoint.Position.y);
                
                Gizmos.color = currentWaypointColor;
                Gizmos.DrawLine(npcPos3D, firstPos3D);
            }
            
            // Рисуем путь
            if (showPaths)
            {
                for (int i = follower.CurrentWaypointIndex; i < waypointBuffer.Length - 1; i++)
                {
                    var waypoint = waypointBuffer[i];
                    var nextWaypoint = waypointBuffer[i + 1];
                    
                    var pos1 = new Vector3(waypoint.Position.x, 0.5f, waypoint.Position.y);
                    var pos2 = new Vector3(nextWaypoint.Position.x, 0.5f, nextWaypoint.Position.y);
                    
                    Gizmos.color = pathColor;
                    Gizmos.DrawLine(pos1, pos2);
                }
            }
            
            // Рисуем waypoints
            if (showWaypoints)
            {
                for (int i = follower.CurrentWaypointIndex; i < waypointBuffer.Length; i++)
                {
                    var waypoint = waypointBuffer[i];
                    var waypointPos3D = new Vector3(waypoint.Position.x, 0.5f, waypoint.Position.y);
                    
                    if (i == follower.CurrentWaypointIndex && showCurrentWaypoint)
                    {
                        // Текущий waypoint
                        Gizmos.color = currentWaypointColor;
                        Gizmos.DrawWireSphere(waypointPos3D, currentWaypointRadius);
                        Gizmos.DrawSphere(waypointPos3D, currentWaypointRadius * 0.6f);
                    }
                    else
                    {
                        // Обычные waypoints
                        Gizmos.color = waypointColor;
                        Gizmos.DrawWireSphere(waypointPos3D, waypointRadius);
                        Gizmos.DrawSphere(waypointPos3D, waypointRadius * 0.4f);
                    }
                }
            }
            
            // Entity info
            if (showEntityInfo && follower.CurrentWaypointIndex < waypointBuffer.Length)
            {
                var infoPos = npcPos3D + Vector3.up * 2f;
                
#if UNITY_EDITOR
                var style = new GUIStyle();
                style.normal.textColor = Color.white;
                style.fontSize = 12;
                
                var info = $"E:{entity.Index}\n{follower.CurrentWaypointIndex}/{waypointBuffer.Length}\n{follower.State}";
                UnityEditor.Handles.Label(infoPos, info, style);
#endif
            }
        }
        
        if (shouldLog)
        {
            Debug.Log($"<color=green>✅ Drew {pathsDrawn} paths in Scene View</color>");
        }
        
        entities.Dispose();
        query.Dispose();
    }
    
    // Рисование в Runtime (Game View)
    void DrawPathsRuntime()
    {
        if (entityManager == null)
            return;
        
        var query = entityManager.CreateEntityQuery(
            ComponentType.ReadOnly<PathFollower>(),
            ComponentType.ReadOnly<Location>(),
            ComponentType.ReadOnly<PathWaypoint>()
        );
        
        var entities = query.ToEntityArray(Allocator.Temp);
        
        foreach (var entity in entities)
        {
            var location = entityManager.GetComponentData<Location>(entity);
            var follower = entityManager.GetComponentData<PathFollower>(entity);
            var waypointBuffer = entityManager.GetBuffer<PathWaypoint>(entity);
            
            if (waypointBuffer.Length == 0)
                continue;
            
            var npcPos = location.GlobalPosition2D;
            var npcPos3D = new Vector3(npcPos.x, 0.5f, npcPos.y);
            
            // Линия от NPC к первому waypoint
            if (follower.CurrentWaypointIndex < waypointBuffer.Length)
            {
                var firstWaypoint = waypointBuffer[follower.CurrentWaypointIndex];
                var firstPos3D = new Vector3(firstWaypoint.Position.x, 0.5f, firstWaypoint.Position.y);
                
                Debug.DrawLine(npcPos3D, firstPos3D, currentWaypointColor);
            }
            
            // Путь
            for (int i = follower.CurrentWaypointIndex; i < waypointBuffer.Length - 1; i++)
            {
                var waypoint = waypointBuffer[i];
                var nextWaypoint = waypointBuffer[i + 1];
                
                var pos1 = new Vector3(waypoint.Position.x, 0.5f, waypoint.Position.y);
                var pos2 = new Vector3(nextWaypoint.Position.x, 0.5f, nextWaypoint.Position.y);
                
                Debug.DrawLine(pos1, pos2, pathColor);
            }
        }
        
        entities.Dispose();
        query.Dispose();
    }
}
