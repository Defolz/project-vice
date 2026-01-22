using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

#if UNITY_INPUT_SYSTEM || ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Инструмент для назначения путей существующим NPC
/// Работает в Scene view и Game view
/// </summary>
public class NPCPathAssigner : MonoBehaviour
{
    [Header("Selection Settings")]
    [SerializeField] private float selectionRadius = 10f;
    [SerializeField] private float massSelectionRadius = 20f;
    
    [Header("Camera Settings")]
    [SerializeField] private float groundPlaneHeight = 0f;
    
    [Header("Visual Feedback")]
    [SerializeField] private bool showSelectionGizmo = true;
    [SerializeField] private Color selectedColor = new Color(0f, 1f, 0f, 0.8f);
    [SerializeField] private Color massSelectedColor = new Color(1f, 1f, 0f, 0.8f);
    [SerializeField] private Color targetColor = new Color(1f, 0f, 0f, 0.8f);
    
    [Header("Debug")]
    [SerializeField] private bool verboseLogging = false;
    
    private World world;
    private EntityManager entityManager;
    private Entity selectedNPC = Entity.Null;
    private float3 targetPosition;
    private bool hasTarget = false;
    
#if UNITY_INPUT_SYSTEM || ENABLE_INPUT_SYSTEM
    private Mouse mouse;
    private Keyboard keyboard;
#endif
    
    void OnEnable()
    {
#if UNITY_EDITOR
        SceneView.duringSceneGui += OnSceneGUI;
#endif
    }
    
    void OnDisable()
    {
#if UNITY_EDITOR
        SceneView.duringSceneGui -= OnSceneGUI;
#endif
    }
    
    void Start()
    {
        world = World.DefaultGameObjectInjectionWorld;
        if (world != null)
        {
            entityManager = world.EntityManager;
            Debug.Log("<color=cyan>🎯 NPCPathAssigner initialized</color>");
            
            var query = entityManager.CreateEntityQuery(ComponentType.ReadOnly<NPCId>());
            int npcCount = query.CalculateEntityCount();
            Debug.Log($"<color=cyan>📊 Found {npcCount} NPCs in world</color>");
        }
        
#if UNITY_INPUT_SYSTEM || ENABLE_INPUT_SYSTEM
        mouse = Mouse.current;
        keyboard = Keyboard.current;
#endif
    }
    
#if UNITY_EDITOR
    void OnSceneGUI(SceneView sceneView)
    {
        if (world == null || !world.IsCreated)
            return;
        
        Event e = Event.current;
        
        if (e.type == EventType.MouseDown)
        {
            // Для ЛКМ: используем только если нажат Shift (массовый выбор) или Control (выбор без popup)
            // Используем Control вместо Alt, так как Alt может конфликтовать с Unity Editor
            if (e.button == 0 && (e.shift || e.control))
            {
                Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
                if (e.shift)
                    SelectNPCsInRadius(ray);
                else
                    SelectSingleNPC(ray);
                e.Use();
            }
            // Для ПКМ: всегда работает для назначения пути
            else if (e.button == 1)
            {
                Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
                SetDestination(ray);
                e.Use();
            }
        }
        
        if (e.type == EventType.KeyDown && e.keyCode == KeyCode.Escape)
        {
            ClearSelection();
            e.Use();
        }
    }
#endif
    
    void Update()
    {
        if (!Application.isPlaying || world == null || !world.IsCreated)
            return;
        
        HandleGameViewInput();
    }
    
    void HandleGameViewInput()
    {
#if UNITY_INPUT_SYSTEM || ENABLE_INPUT_SYSTEM
        if (mouse != null && keyboard != null)
        {
            if (mouse.leftButton.wasPressedThisFrame)
            {
                Camera cam = Camera.main;
                if (cam != null)
                {
                    Ray ray = cam.ScreenPointToRay(mouse.position.ReadValue());
                    if (keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed)
                        SelectNPCsInRadius(ray);
                    else if (keyboard.leftCtrlKey.isPressed || keyboard.rightCtrlKey.isPressed)
                        SelectSingleNPC(ray);
                }
            }
            
            if (mouse.rightButton.wasPressedThisFrame)
            {
                Camera cam = Camera.main;
                if (cam != null)
                {
                    Ray ray = cam.ScreenPointToRay(mouse.position.ReadValue());
                    SetDestination(ray);
                }
            }
            
            if (keyboard.escapeKey.wasPressedThisFrame)
                ClearSelection();
            return;
        }
#endif
        
        if (Input.GetMouseButtonDown(0))
        {
            Camera cam = Camera.main;
            if (cam != null)
            {
                Ray ray = cam.ScreenPointToRay(Input.mousePosition);
                if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
                    SelectNPCsInRadius(ray);
                else if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
                    SelectSingleNPC(ray);
            }
        }
        
        if (Input.GetMouseButtonDown(1))
        {
            Camera cam = Camera.main;
            if (cam != null)
            {
                Ray ray = cam.ScreenPointToRay(Input.mousePosition);
                SetDestination(ray);
            }
        }
        
        if (Input.GetKeyDown(KeyCode.Escape))
            ClearSelection();
    }
    
    void SelectSingleNPC(Ray ray)
    {
        var query = entityManager.CreateEntityQuery(
            ComponentType.ReadOnly<NPCId>(),
            ComponentType.ReadOnly<Location>()
        );
        
        var entities = query.ToEntityArray(Unity.Collections.Allocator.Temp);
        var locations = query.ToComponentDataArray<Location>(Unity.Collections.Allocator.Temp);
        
        Entity closestNPC = Entity.Null;
        float closestDistance = selectionRadius;
        
        for (int i = 0; i < entities.Length; i++)
        {
            var npcPos = locations[i].GlobalPosition3D;
            var npcPos3D = new Vector3(npcPos.x, npcPos.y, npcPos.z);
            
            var toPoint = npcPos3D - ray.origin;
            var projectedDistance = Vector3.Dot(toPoint, ray.direction);
            
            if (projectedDistance < 0) continue;
            
            var pointOnRay = ray.origin + ray.direction * projectedDistance;
            var distance = Vector3.Distance(pointOnRay, npcPos3D);
            
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestNPC = entities[i];
            }
        }
        
        if (closestNPC != Entity.Null)
        {
            selectedNPC = closestNPC;
            var npcId = entityManager.GetComponentData<NPCId>(closestNPC);
            var location = entityManager.GetComponentData<Location>(closestNPC);
            
            Debug.Log($"<color=green>✅ Selected NPC {npcId.Value} at ({location.GlobalPosition2D.x:F1}, {location.GlobalPosition2D.y:F1})</color>");
        }
        else if (verboseLogging)
        {
            Debug.Log("<color=gray>○ No NPC found</color>");
        }
        
        entities.Dispose();
        locations.Dispose();
    }
    
    void SelectNPCsInRadius(Ray ray)
    {
        Plane plane = new Plane(Vector3.forward, new Vector3(0, 0, groundPlaneHeight));
        
        if (!plane.Raycast(ray, out float distance))
            return;
        
        Vector3 hitPoint = ray.GetPoint(distance);
        var clickPos = new float2(hitPoint.x, hitPoint.y);
        
        var query = entityManager.CreateEntityQuery(
            ComponentType.ReadOnly<NPCId>(),
            ComponentType.ReadOnly<Location>()
        );
        
        var entities = query.ToEntityArray(Unity.Collections.Allocator.Temp);
        var locations = query.ToComponentDataArray<Location>(Unity.Collections.Allocator.Temp);
        
        int selectedCount = 0;
        
        for (int i = 0; i < entities.Length; i++)
        {
            var npcPos = locations[i].GlobalPosition2D;
            var dist = math.distance(clickPos, npcPos);
            
            if (dist <= massSelectionRadius)
            {
                if (!entityManager.HasComponent<SelectedForPathAssignment>(entities[i]))
                {
                    entityManager.AddComponent<SelectedForPathAssignment>(entities[i]);
                    selectedCount++;
                }
            }
        }
        
        Debug.Log($"<color=yellow>📍 Selected {selectedCount} NPCs in {massSelectionRadius}m radius</color>");
        
        entities.Dispose();
        locations.Dispose();
    }
    
    void SetDestination(Ray ray)
    {
        Plane plane = new Plane(Vector3.forward, new Vector3(0, 0, groundPlaneHeight));
        
        if (!plane.Raycast(ray, out float distance))
            return;
        
        Vector3 hitPoint = ray.GetPoint(distance);
        // ВАЖНО: В нашем 2D мире координаты X-Z соответствуют глобальным X-Y
        // Поэтому targetPosition должен быть: X = hitPoint.x, Y = groundPlaneHeight, Z = hitPoint.y
        targetPosition = new float3(hitPoint.x, groundPlaneHeight, hitPoint.y);
        hasTarget = true;
        
        Debug.Log($"<color=yellow>🎯 Target: ({targetPosition.x:F1}, {targetPosition.z:F1}) [3D: {targetPosition}]</color>");
        
        if (selectedNPC != Entity.Null && entityManager.Exists(selectedNPC))
        {
            AssignPathToNPC(selectedNPC, targetPosition);
            selectedNPC = Entity.Null;
        }
        
        var query = entityManager.CreateEntityQuery(
            ComponentType.ReadOnly<SelectedForPathAssignment>(),
            ComponentType.ReadOnly<Location>()
        );
        
        var entities = query.ToEntityArray(Unity.Collections.Allocator.Temp);
        
        foreach (var entity in entities)
        {
            AssignPathToNPC(entity, targetPosition);
            entityManager.RemoveComponent<SelectedForPathAssignment>(entity);
        }
        
        if (entities.Length > 0)
            Debug.Log($"<color=green>✅ Path assigned to {entities.Length} NPC(s)</color>");
        
        entities.Dispose();
    }
    
    void AssignPathToNPC(Entity npcEntity, float3 destination)
    {
        if (!entityManager.Exists(npcEntity))
            return;
        
        var goal = new CurrentGoal(
            GoalType.MoveToLocation,
            targetPosition: destination,
            priority: 0.9f
        );
        
        if (entityManager.HasComponent<CurrentGoal>(npcEntity))
            entityManager.SetComponentData(npcEntity, goal);
        else
            entityManager.AddComponentData(npcEntity, goal);
        
        var npcId = entityManager.GetComponentData<NPCId>(npcEntity);
        Debug.Log($"<color=lime>🚶 NPC {npcId.Value} → ({destination.x:F1}, {destination.z:F1})</color>");
    }
    
    void ClearSelection()
    {
        selectedNPC = Entity.Null;
        hasTarget = false;
        
        var query = entityManager.CreateEntityQuery(ComponentType.ReadOnly<SelectedForPathAssignment>());
        var entities = query.ToEntityArray(Unity.Collections.Allocator.Temp);
        foreach (var entity in entities)
            entityManager.RemoveComponent<SelectedForPathAssignment>(entity);
        entities.Dispose();
        
        Debug.Log("<color=gray>✖ Selection cleared</color>");
    }
}

public struct SelectedForPathAssignment : IComponentData
{
}