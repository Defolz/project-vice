using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

// Визуализатор с ОГРОМНЫМИ сферами для теста
public class PathVisualizer : MonoBehaviour
{
    [Header("Visualization Settings")]
    [SerializeField] private bool showPaths = true;
    [SerializeField] private bool showWaypoints = true;
    [SerializeField] private bool showCurrentWaypoint = true;
    [SerializeField] private bool showEntityInfo = true;
    [SerializeField] private bool showDebugSpheres = true;
    [SerializeField] private bool showHugeSpheres = true; // НОВОЕ!
    [SerializeField] private bool showFullPath = true; // Показывать весь путь или только оставшуюся часть
    
    [Header("Colors")]
    [SerializeField] private Color pathColor = new Color(0, 1, 0, 0.8f);
    [SerializeField] private Color waypointColor = new Color(1, 1, 0, 0.9f);
    [SerializeField] private Color currentWaypointColor = new Color(1, 0, 0, 1f);
    [SerializeField] private Color startColor = new Color(0, 0, 1, 1f);
    [SerializeField] private Color debugColor = new Color(1, 0, 1, 1f);
    
    [Header("Sizes")]
    [SerializeField] private float waypointRadius = 0.5f;
    [SerializeField] private float currentWaypointRadius = 0.8f;
    [SerializeField] private float hugeSphereRadius = 5f; // НОВОЕ!
    
    [Header("Performance")]
    [SerializeField] private float maxDrawDistance = 500f; // Увеличено!
    [SerializeField] private int maxPathsToDraw = 100;
    
    [Header("Debug")]
    [SerializeField] private bool verboseLogging = true;
    [SerializeField] private bool logWaypointCoordinates = true;
    
    private World world;
    private EntityManager entityManager;
    private float lastDebugTime = 0f;
    private const float DEBUG_INTERVAL = 3f;
    
    // === ФИКСИРОВАННАЯ ГЛУБИНА ДЛЯ 2D ===
    private const float DRAW_DEPTH = -0.5f;

    void Start()
    {
        world = World.DefaultGameObjectInjectionWorld;
        if (world != null)
        {
            entityManager = world.EntityManager;
            Debug.Log("<color=cyan>🎨 PathVisualizer (2D FIXED) initialized!</color>");
        }
        else
        {
            Debug.LogError("<color=red>❌ PathVisualizer: World not found!</color>");
        }
    }
    
    void OnDrawGizmos()
    {
        if (!Application.isPlaying || world == null || !world.IsCreated)
            return;
        
        DrawPaths();
    }
    
    void Update()
    {
        if (!Application.isPlaying || world == null || !world.IsCreated)
            return;
        
        DrawPathsRuntime();
    }
    
    void DrawPaths()
    {
        if (entityManager == null)
            return;
        
        int pathsDrawn = 0;
        
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
            Debug.Log($"<color=cyan>📍 PathVisualizer: Found {entityCount} entities with paths</color>");
        }
        
        if (entityCount == 0)
        {
            if (shouldLog)
            {
                Debug.LogWarning("<color=red>⚠️ No entities to draw!</color>");
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
            
            if (waypointBuffer.Length == 0)
            {
                if (shouldLog && verboseLogging)
                    Debug.LogWarning($"Entity {entity.Index}: Empty waypoint buffer");
                continue;
            }
            
            pathsDrawn++;
            
            var npcPos = location.GlobalPosition2D;
            // === ПРАВИЛЬНАЯ 2D КОНВЕРТАЦИЯ ===
            var npcPos3D = new Vector3(npcPos.x, npcPos.y, DRAW_DEPTH);
            
            if (shouldLog)
            {
                Debug.Log($"<color=lime>Drawing Entity {entity.Index} at ({npcPos.x:G}, {npcPos.y:G}), {waypointBuffer.Length} waypoints</color>");
            }
            
            // === ОГРОМНАЯ СИНЯЯ СФЕРА НА NPC (ТОЧНО БУДЕТ ВИДНО!) ===
            if (showHugeSpheres)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawWireSphere(npcPos3D, hugeSphereRadius);
                Gizmos.DrawSphere(npcPos3D, hugeSphereRadius * 0.5f);
            }
            
            // Обычная синяя сфера
            Gizmos.color = startColor;
            Gizmos.DrawWireSphere(npcPos3D, 1f);
            Gizmos.DrawSphere(npcPos3D, 0.5f);
            
            // === ОГРОМНАЯ КРАСНАЯ СФЕРА НА ПЕРВОМ WAYPOINT ===
            if (follower.CurrentWaypointIndex < waypointBuffer.Length)
            {
                var firstWaypoint = waypointBuffer[follower.CurrentWaypointIndex];
                // === ПРАВИЛЬНАЯ 2D КОНВЕРТАЦИЯ ===
                var firstPos3D = new Vector3(firstWaypoint.Position.x, firstWaypoint.Position.y, DRAW_DEPTH);
                
                if (showHugeSpheres)
                {
                    Gizmos.color = Color.red;
                    Gizmos.DrawWireSphere(firstPos3D, hugeSphereRadius);
                    Gizmos.DrawSphere(firstPos3D, hugeSphereRadius * 0.5f);
                }
                
                // Толстая линия от NPC к waypoint
                Gizmos.color = currentWaypointColor;
                Gizmos.DrawLine(npcPos3D, firstPos3D);
                
                // Дополнительные параллельные линии для толщины
                var offset = Vector3.right * 0.5f;
                Gizmos.DrawLine(npcPos3D + offset, firstPos3D + offset);
                Gizmos.DrawLine(npcPos3D - offset, firstPos3D - offset);
            }
            
            // === ПУТЬ (ЗЕЛЁНЫЕ ЛИНИИ) ===
            if (showPaths)
            {
                // Определяем с какого индекса начинать рисовать путь
                int startIndex = showFullPath ? 0 : follower.CurrentWaypointIndex;
                
                // Рисуем путь от начального waypoint до конца
                for (int i = startIndex; i < waypointBuffer.Length - 1; i++)
                {
                    var waypoint = waypointBuffer[i];
                    var nextWaypoint = waypointBuffer[i + 1];
                    
                    // === ПРАВИЛЬНАЯ 2D КОНВЕРТАЦИЯ ===
                    var pos1 = new Vector3(waypoint.Position.x, waypoint.Position.y, DRAW_DEPTH);
                    var pos2 = new Vector3(nextWaypoint.Position.x, nextWaypoint.Position.y, DRAW_DEPTH);
                    
                    // Используем разные цвета для пройденной и оставшейся части пути
                    if (showFullPath && i < follower.CurrentWaypointIndex)
                    {
                        Gizmos.color = new Color(0f, 0.5f, 0f, 0.5f); // Темно-зеленый для пройденного
                    }
                    else
                    {
                        Gizmos.color = pathColor; // Обычный цвет для оставшегося пути
                    }
                    
                    Gizmos.DrawLine(pos1, pos2);
                    
                    // Толстые линии - используем смещения по обеим осям для лучшей видимости
                    var offsetX = Vector3.right * 0.3f;
                    Gizmos.DrawLine(pos1 + offsetX, pos2 + offsetX);
                    Gizmos.DrawLine(pos1 - offsetX, pos2 - offsetX);
                }
            }
            
            // === WAYPOINTS (ЖЁЛТЫЕ СФЕРЫ) ===
            if (showWaypoints)
            {
                for (int i = follower.CurrentWaypointIndex; i < waypointBuffer.Length; i++)
                {
                    var waypoint = waypointBuffer[i];
                    // === ПРАВИЛЬНАЯ 2D КОНВЕРТАЦИЯ ===
                    var waypointPos3D = new Vector3(waypoint.Position.x, waypoint.Position.y, DRAW_DEPTH);
                    
                    if (i == follower.CurrentWaypointIndex && showCurrentWaypoint)
                    {
                        Gizmos.color = currentWaypointColor;
                        Gizmos.DrawWireSphere(waypointPos3D, currentWaypointRadius * 2);
                        Gizmos.DrawSphere(waypointPos3D, currentWaypointRadius);
                    }
                    else
                    {
                        Gizmos.color = waypointColor;
                        Gizmos.DrawWireSphere(waypointPos3D, waypointRadius * 2);
                        Gizmos.DrawSphere(waypointPos3D, waypointRadius);
                    }
                }
            }
            
            // === ТЕКСТ ===
            if (showEntityInfo)
            {
                var infoPos = npcPos3D + Vector3.up * 3f;
                
#if UNITY_EDITOR
                var style = new GUIStyle();
                style.normal.textColor = Color.white;
                style.fontSize = 14;
                style.fontStyle = FontStyle.Bold;
                
                var info = $"E:{entity.Index}\n" +
                          $"WP: {follower.CurrentWaypointIndex}/{waypointBuffer.Length}\n" +
                          $"{follower.State}\n" +
                          $"Pos: ({npcPos.x:G}, {npcPos.y:G})";
                
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
            // === ПРАВИЛЬНАЯ 2D КОНВЕРТАЦИЯ ===
            var npcPos3D = new Vector3(npcPos.x, npcPos.y, DRAW_DEPTH);
            
            if (follower.CurrentWaypointIndex < waypointBuffer.Length)
            {
                var firstWaypoint = waypointBuffer[follower.CurrentWaypointIndex];
                // === ПРАВИЛЬНАЯ 2D КОНВЕРТАЦИЯ ===
                var firstPos3D = new Vector3(firstWaypoint.Position.x, firstWaypoint.Position.y, DRAW_DEPTH);
                
                Debug.DrawLine(npcPos3D, firstPos3D, currentWaypointColor, 0.1f);
            }
            
            // Определяем с какого индекса начинать рисовать путь
            int startIndex = showFullPath ? 0 : follower.CurrentWaypointIndex;
            
            for (int i = startIndex; i < waypointBuffer.Length - 1; i++)
            {
                var waypoint = waypointBuffer[i];
                var nextWaypoint = waypointBuffer[i + 1];
                
                // === ПРАВИЛЬНАЯ 2D КОНВЕРТАЦИЯ ===
                var pos1 = new Vector3(waypoint.Position.x, waypoint.Position.y, DRAW_DEPTH);
                var pos2 = new Vector3(nextWaypoint.Position.x, nextWaypoint.Position.y, DRAW_DEPTH);
                
                // Используем разные цвета для пройденной и оставшейся части пути
                if (showFullPath && i < follower.CurrentWaypointIndex)
                {
                    Debug.DrawLine(pos1, pos2, new Color(0f, 0.5f, 0f, 0.5f), 0.1f); // Темно-зеленый для пройденного
                }
                else
                {
                    Debug.DrawLine(pos1, pos2, pathColor, 0.1f); // Обычный цвет для оставшегося пути
                }
            }
        }
        
        entities.Dispose();
        query.Dispose();
    }
}