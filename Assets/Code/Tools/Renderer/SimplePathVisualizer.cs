using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using Unity.Mathematics;

public class SimplePathVisualizer : MonoBehaviour
{
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
            Debug.Log("<color=cyan>🔍 SimplePathVisualizer (2D FIXED) initialized</color>");
        }
    }

    void OnDrawGizmos()
    {
        if (!Application.isPlaying || entityManager == default)
            return;

        var currentTime = Time.time;
        bool shouldDebug = currentTime - lastDebugTime > DEBUG_INTERVAL;

        if (shouldDebug)
        {
            lastDebugTime = currentTime;
            Debug.Log("<color=yellow>=== 2D PATH VISUALIZATION (FIXED COORDS) ===</color>");
        }

        var query = entityManager.CreateEntityQuery(
            ComponentType.ReadOnly<PathFollower>(),
            ComponentType.ReadOnly<Location>(),
            ComponentType.ReadOnly<PathWaypoint>()
        );

        using (var entities = query.ToEntityArray(Allocator.Temp))
        {
            foreach (var entity in entities)
            {
                var location = entityManager.GetComponentData<Location>(entity);
                var follower = entityManager.GetComponentData<PathFollower>(entity);
                var waypointBuffer = entityManager.GetBuffer<PathWaypoint>(entity);

                if (waypointBuffer.Length == 0 || follower.CurrentWaypointIndex >= waypointBuffer.Length)
                    continue;

                // === ГЛОБАЛЬНЫЕ 2D КООРДИНАТЫ ===
                var npcGlobalPos = location.GlobalPosition2D;
                var wpGlobalPos = waypointBuffer[follower.CurrentWaypointIndex].Position;

                // === ПРАВИЛЬНАЯ КОНВЕРТАЦИЯ ДЛЯ 2D TOP-DOWN ===
                // X = global_x, Y = global_y, Z = фиксированная глубина для отрисовки
                float drawDepth = -1f; // Немного позади чанков для правильного наложения
                
                var npcPos3D = new Vector3(npcGlobalPos.x, npcGlobalPos.y, drawDepth);
                var wpPos3D = new Vector3(wpGlobalPos.x, wpGlobalPos.y, drawDepth);

                // === ОТЛАДОЧНЫЙ ВЫВОД ===
                if (shouldDebug)
                {
                    Debug.Log($"Entity {entity.Index}:");
                    Debug.Log($"  NPC 2D Pos: ({npcGlobalPos.x:F1}, {npcGlobalPos.y:F1}) -> {npcPos3D}");
                    Debug.Log($"  WP 2D Pos:  ({wpGlobalPos.x:F1}, {wpGlobalPos.y:F1}) -> {wpPos3D}");
                    Debug.Log($"  Distance: {math.distance(npcGlobalPos, wpGlobalPos):F1}");
                }

                // === ВИЗУАЛИЗАЦИЯ ===
                // 1. Точка NPC
                Gizmos.color = Color.blue;
                Gizmos.DrawWireSphere(npcPos3D, 1.5f);
                Gizmos.DrawSphere(npcPos3D, 0.8f);

                // 2. Точка waypoint
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(wpPos3D, 1.5f);
                Gizmos.DrawSphere(wpPos3D, 0.8f);

                // 3. ПРАВИЛЬНАЯ ЛИНИЯ ОТ NPC ДО WAYPOINT
                Gizmos.color = Color.green;
                Gizmos.DrawLine(npcPos3D, wpPos3D);

                // 4. Расстояние (как в вашем скриншоте)
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(npcPos3D, Vector3.Lerp(npcPos3D, wpPos3D, 0.5f));
            }
        }

        if (shouldDebug)
        {
            Debug.Log("<color=yellow>====================================</color>");
        }
    }
}