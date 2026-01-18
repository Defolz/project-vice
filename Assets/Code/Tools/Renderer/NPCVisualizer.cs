using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using Unity.Collections;
using UnityEditor;

public class NPCVisualizer : MonoBehaviour
{
    [SerializeField] private Color npcColor = Color.blue;
    [SerializeField] private float npcRadius = 0.25f;

    private EntityManager entityManager;
    private World world;

    private void OnEnable()
    {
        world = World.DefaultGameObjectInjectionWorld;
        if (world != null && world.IsCreated)
        {
            entityManager = world.EntityManager;
        }
    }

    private void OnValidate()
    {
        npcRadius = Mathf.Max(0.01f, npcRadius);
    }

    private void OnDrawGizmos()
    {
        if (!enabled || world == null || !world.IsCreated || entityManager == null)
            return;

        var query = entityManager.CreateEntityQuery(
            ComponentType.ReadOnly<Location>(),
            ComponentType.ReadOnly<NPCId>());

        var locations = query.ToComponentDataArray<Location>(Allocator.Temp);
        var entities = query.ToEntityArray(Allocator.Temp);

        Gizmos.color = npcColor;

        for (int i = 0; i < locations.Length; i++)
        {
            float2 pos2D = locations[i].GlobalPosition2D;
            Vector3 pos3D = new Vector3(pos2D.x, pos2D.y, 0f);

            DrawCircleGizmo(pos3D, npcRadius);
            Gizmos.DrawLine(pos3D, pos3D + Vector3.up * npcRadius * 0.5f);

#if UNITY_EDITOR
            // 🔥 КЛИКАБЕЛЬНАЯ ТОЧКА
            if (Handles.Button(
                pos3D,
                Quaternion.identity,
                npcRadius,
                npcRadius,
                Handles.SphereHandleCap))
            {
                NPCInspector.Inspect(entities[i]);
            }
#endif
        }

        locations.Dispose();
        entities.Dispose();
        query.Dispose();
    }

    private void DrawCircleGizmo(Vector3 center, float radius)
    {
        const int segments = 16;
        float step = 2 * Mathf.PI / segments;

        Vector3 last = center + new Vector3(radius, 0, 0);

        for (int i = 1; i <= segments; i++)
        {
            float angle = i * step;
            Vector3 next = center + new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0);
            Gizmos.DrawLine(last, next);
            last = next;
        }
    }
}
