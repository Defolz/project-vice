using UnityEditor;
using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;

[InitializeOnLoad]
public static class NPCSceneClickHandler
{
    private const float ClickRadius = 0.25f;

    private static Entity selectedNPC = Entity.Null;
    private static NPCPopupData popupData;
    private static float2 popupWorldPos;

    static NPCSceneClickHandler()
    {
        SceneView.duringSceneGui += OnSceneGUI;
    }

    private static void OnSceneGUI(SceneView sceneView)
    {
        Event e = Event.current;
        if (e == null) return;

        if (e.type == EventType.MouseDown && e.button == 0 && !e.alt)
        {
            bool picked = HandleClick(e, sceneView);
            if (!picked)
            {
                selectedNPC = Entity.Null;
            }
        }

        if (e.type == EventType.KeyDown && e.keyCode == KeyCode.Escape)
        {
            selectedNPC = Entity.Null;
            sceneView.Repaint();
        }

        if (selectedNPC != Entity.Null)
        {
            DrawPopup(sceneView);
        }
    }

    private static bool HandleClick(Event e, SceneView sceneView)
    {
        var world = World.DefaultGameObjectInjectionWorld;
        if (world == null || !world.IsCreated)
            return false;

        var entityManager = world.EntityManager;

        Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);

        if (Mathf.Abs(ray.direction.z) < 0.0001f)
            return false;

        float t = -ray.origin.z / ray.direction.z;
        Vector3 hitPoint = ray.origin + ray.direction * t;
        float2 clickPos2D = new float2(hitPoint.x, hitPoint.y);

        var query = entityManager.CreateEntityQuery(
            ComponentType.ReadOnly<NPCId>(),
            ComponentType.ReadOnly<Location>()
        );

        var entities = query.ToEntityArray(Allocator.Temp);
        var locations = query.ToComponentDataArray<Location>(Allocator.Temp);

        Entity closest = Entity.Null;
        float bestDistance = float.MaxValue;

        for (int i = 0; i < entities.Length; i++)
        {
            float dist = math.distance(clickPos2D, locations[i].GlobalPosition2D);
            if (dist <= ClickRadius && dist < bestDistance)
            {
                bestDistance = dist;
                closest = entities[i];
                popupWorldPos = locations[i].GlobalPosition2D;
            }
        }

        if (closest != Entity.Null)
        {
            selectedNPC = closest;
            popupData = NPCPopupData.Read(entityManager, closest);
            e.Use();
        }

        entities.Dispose();
        locations.Dispose();
        query.Dispose();

        return closest != Entity.Null;
    }

    private static void DrawPopup(SceneView sceneView)
    {
        Handles.BeginGUI();

        Vector3 worldPos = new Vector3(popupWorldPos.x, popupWorldPos.y, 0f);
        Vector2 guiPos = HandleUtility.WorldToGUIPoint(worldPos);

        Rect rect = new Rect(guiPos.x + 12, guiPos.y + 12, 220, 140);

        GUI.Box(rect, GUIContent.none);

        GUILayout.BeginArea(rect);
        GUILayout.Label("<b>NPC</b>", EditorStyles.boldLabel);
        GUILayout.Space(4);

        GUILayout.Label($"Name: {popupData.FullName}");
        GUILayout.Label($"ID: {popupData.Id}");
        GUILayout.Label($"Faction: {popupData.Faction}");
        GUILayout.Label($"Alive: {popupData.IsAlive}");
        GUILayout.Label($"Wanted: {popupData.IsWanted}");

        GUILayout.Space(6);
        GUILayout.Label("LMB – select | ESC – close", EditorStyles.miniLabel);
        GUILayout.EndArea();

        Handles.EndGUI();
    }

    private struct NPCPopupData
    {
        public string FullName;
        public int Id;
        public int Faction;
        public bool IsAlive;
        public bool IsWanted;

        public static NPCPopupData Read(EntityManager em, Entity e)
        {
            var name = em.GetComponentData<NameData>(e);
            var id = em.GetComponentData<NPCId>(e);
            var faction = em.GetComponentData<Faction>(e);
            var state = em.GetComponentData<StateFlags>(e);

            return new NPCPopupData
            {
                FullName = $"{name.FirstName} {name.LastName}",
                Id = (int)id.Value,
                Faction = faction.Value,
                IsAlive = state.IsAlive,
                IsWanted = state.IsWanted
            };
        }
    }
}
