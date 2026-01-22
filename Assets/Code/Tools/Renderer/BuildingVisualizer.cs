using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

/// <summary>
/// Визуализатор зданий в Scene View через Gizmos
/// Отрисовывает здания в виде цветных прямоугольников с информацией
/// </summary>
public class BuildingVisualizer : MonoBehaviour
{
    [Header("Visualization Settings")]
    [Tooltip("Показывать здания")]
    public bool showBuildings = true;
    
    [Tooltip("Показывать информацию о зданиях (тип, размер)")]
    public bool showBuildingInfo = true;
    
    [Tooltip("Показывать точки входов в здания")]
    public bool showEntrances = true;
    
    [Tooltip("Показывать информацию о заполненности")]
    public bool showOccupancy = true;
    
    [Header("Colors")]
    [Tooltip("Цвет жилых зданий (Residential)")]
    public Color residentialColor = new Color(0.2f, 0.6f, 1f, 0.6f);
    
    [Tooltip("Цвет коммерческих зданий (Commercial)")]
    public Color commercialColor = new Color(1f, 0.8f, 0.2f, 0.6f);
    
    [Tooltip("Цвет промышленных зданий (Industrial)")]
    public Color industrialColor = new Color(0.6f, 0.4f, 0.2f, 0.6f);
    
    [Tooltip("Цвет общественных зданий (Public)")]
    public Color publicColor = new Color(0.2f, 1f, 0.4f, 0.6f);
    
    [Tooltip("Цвет специальных зданий (Special)")]
    public Color specialColor = new Color(1f, 0.2f, 0.4f, 0.7f);
    
    [Tooltip("Цвет входов")]
    public Color entranceColor = new Color(1f, 1f, 0f, 0.9f);
    
    [Header("Performance")]
    [Tooltip("Максимальное расстояние для отрисовки (0 = без ограничений)")]
    public float maxDrawDistance = 200f;
    
    [Tooltip("Рисовать упрощенно (без высоты)")]
    public bool simplifiedMode = false;
    
    [Tooltip("Показывать только здания с посетителями")]
    public bool showOnlyOccupied = false;
    
    private World world;
    private EntityManager entityManager;
    
    void Start()
    {
        world = World.DefaultGameObjectInjectionWorld;
        if (world != null)
        {
            entityManager = world.EntityManager;
        }
    }
    
    void OnDrawGizmos()
    {
        if (!showBuildings) return;
        if (world == null || !world.IsCreated || entityManager == null) return;
        
        Gizmos.matrix = transform.localToWorldMatrix;
        
        DrawBuildings();
    }
    
    private void DrawBuildings()
    {
        var cameraPos = GetCameraPosition();
        
        var buildingQuery = entityManager.CreateEntityQuery(ComponentType.ReadOnly<Building>());
        
        if (buildingQuery.CalculateEntityCount() == 0)
        {
            buildingQuery.Dispose();
            return;
        }
        
        var buildings = buildingQuery.ToComponentDataArray<Building>(Unity.Collections.Allocator.Temp);
        
        for (int i = 0; i < buildings.Length; i++)
        {
            var building = buildings[i];
            
            // Distance culling
            if (maxDrawDistance > 0)
            {
                var dist = math.distance(cameraPos, building.Position);
                if (dist > maxDrawDistance)
                    continue;
            }
            
            // Filter по заполненности
            if (showOnlyOccupied && building.CurrentOccupancy == 0)
                continue;
            
            // Выбираем цвет по типу
            var color = GetBuildingColor(building.Type);
            
            // Затемняем цвет если здание недоступно
            if (!building.IsAccessible)
                color = new Color(color.r * 0.5f, color.g * 0.5f, color.b * 0.5f, color.a);
            
            Gizmos.color = color;
            
            if (simplifiedMode)
            {
                DrawBuildingSimplified(building);
            }
            else
            {
                DrawBuildingDetailed(building);
            }
            
            // Рисуем вход
            if (showEntrances)
            {
                DrawEntrance(building);
            }
            
            // Отображаем информацию
#if UNITY_EDITOR
            if (showBuildingInfo || showOccupancy)
            {
                DrawBuildingLabel(building, cameraPos);
            }
#endif
        }
        
        buildings.Dispose();
        buildingQuery.Dispose();
    }
    
    private void DrawBuildingSimplified(Building building)
    {
        var center = new Vector3(building.Position.x, building.Position.y, 0f);
        var size = new Vector3(building.Size.x, building.Size.y, 0.5f);
        
        Gizmos.DrawCube(center, size);
        
        // Контур
        Gizmos.color = new Color(Gizmos.color.r, Gizmos.color.g, Gizmos.color.b, 1f);
        Gizmos.DrawWireCube(center, size);
    }
    
    private void DrawBuildingDetailed(Building building)
    {
        var halfSize = building.Size * 0.5f;
        var min = building.Position - halfSize;
        var max = building.Position + halfSize;
        
        // Основание здания
        var base0 = new Vector3(min.x, min.y, 0f);
        var base1 = new Vector3(max.x, min.y, 0f);
        var base2 = new Vector3(max.x, max.y, 0f);
        var base3 = new Vector3(min.x, max.y, 0f);
        
        // Верхушка здания
        var top0 = base0 + Vector3.forward * building.Height;
        var top1 = base1 + Vector3.forward * building.Height;
        var top2 = base2 + Vector3.forward * building.Height;
        var top3 = base3 + Vector3.forward * building.Height;
        
        // Рисуем основание (заполненное)
        DrawQuad(base0, base1, base2, base3);
        
        // Рисуем стены (только контуры для производительности)
        Gizmos.color = new Color(Gizmos.color.r, Gizmos.color.g, Gizmos.color.b, 1f);
        
        // Вертикальные линии
        Gizmos.DrawLine(base0, top0);
        Gizmos.DrawLine(base1, top1);
        Gizmos.DrawLine(base2, top2);
        Gizmos.DrawLine(base3, top3);
        
        // Линии крыши
        Gizmos.DrawLine(top0, top1);
        Gizmos.DrawLine(top1, top2);
        Gizmos.DrawLine(top2, top3);
        Gizmos.DrawLine(top3, top0);
    }
    
    private void DrawEntrance(Building building)
    {
        var entrance = building.GetEntrancePosition();
        var entrancePos = new Vector3(entrance.x, entrance.y, 0f);
        
        Gizmos.color = entranceColor;
        Gizmos.DrawSphere(entrancePos, 0.8f);
        
        // Рисуем стрелку от центра к входу
        var center = new Vector3(building.Position.x, building.Position.y, 0f);
        Gizmos.DrawLine(center, entrancePos);
    }
    
    private void DrawQuad(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3)
    {
        // Рисуем два треугольника для создания квада
        Gizmos.DrawLine(p0, p1);
        Gizmos.DrawLine(p1, p2);
        Gizmos.DrawLine(p2, p3);
        Gizmos.DrawLine(p3, p0);
        
        // Заполнение (meshes в Gizmos нет, рисуем через множество линий)
        var steps = 4;
        var alpha = Gizmos.color.a;
        
        for (int i = 0; i <= steps; i++)
        {
            var t = i / (float)steps;
            var start = Vector3.Lerp(p0, p3, t);
            var end = Vector3.Lerp(p1, p2, t);
            
            Gizmos.color = new Color(Gizmos.color.r, Gizmos.color.g, Gizmos.color.b, alpha * 0.3f);
            Gizmos.DrawLine(start, end);
        }
        
        Gizmos.color = new Color(Gizmos.color.r, Gizmos.color.g, Gizmos.color.b, alpha);
    }
    
    private Color GetBuildingColor(BuildingType type)
    {
        return type switch
        {
            BuildingType.Residential => residentialColor,
            BuildingType.Commercial => commercialColor,
            BuildingType.Industrial => industrialColor,
            BuildingType.Public => publicColor,
            BuildingType.Special => specialColor,
            _ => Color.white
        };
    }
    
#if UNITY_EDITOR
    private void DrawBuildingLabel(Building building, float2 cameraPos)
    {
        // Показываем информацию только для близких зданий
        var dist = math.distance(cameraPos, building.Position);
        if (maxDrawDistance > 0 && dist > maxDrawDistance * 0.6f)
            return;
        
        var labelPos = new Vector3(building.Position.x, building.Position.y, building.Height * 0.5f);
        var label = "";
        
        if (showBuildingInfo)
        {
            label += $"{building.Type}\n";
            label += $"{building.Size.x:F1}x{building.Size.y:F1}m\n";
            label += $"H: {building.Height:F1}m";
        }
        
        if (showOccupancy)
        {
            if (showBuildingInfo)
                label += "\n";
            
            var occupancyPercent = building.MaxOccupancy > 0 
                ? (building.CurrentOccupancy * 100f / building.MaxOccupancy) 
                : 0f;
            
            label += $"👥 {building.CurrentOccupancy}/{building.MaxOccupancy} ({occupancyPercent:F0}%)";
            
            if (!building.IsAccessible)
                label += "\n🚫 Closed";
        }
        
        var style = new GUIStyle();
        style.normal.textColor = Color.white;
        style.fontSize = 10;
        style.alignment = TextAnchor.MiddleCenter;
        
        UnityEditor.Handles.Label(labelPos, label, style);
    }
#endif
    
    private float2 GetCameraPosition()
    {
#if UNITY_EDITOR
        var sceneView = UnityEditor.SceneView.lastActiveSceneView;
        if (sceneView != null && sceneView.camera != null)
        {
            var camPos = sceneView.camera.transform.position;
            return new float2(camPos.x, camPos.y);
        }
#endif
        if (Camera.main != null)
        {
            var camPos = Camera.main.transform.position;
            return new float2(camPos.x, camPos.y);
        }
        
        return float2.zero;
    }
}
