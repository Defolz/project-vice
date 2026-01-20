using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

// MonoBehaviour для визуализации навигационной сетки в Scene View
// ОПТИМИЗИРОВАННАЯ ВЕРСИЯ - рисует только контуры заблокированных областей
public class NavigationGridVisualizer : MonoBehaviour
{
    [Header("Visualization Settings")]
    [Tooltip("Показывать сетку навигации")]
    public bool showGrid = true;
    
    [Tooltip("Показывать только заблокированные ячейки")]
    public bool showOnlyBlocked = true; // По умолчанию true для производительности
    
    [Tooltip("Цвет проходимых ячеек")]
    public Color walkableColor = new Color(0f, 1f, 0f, 0.15f);
    
    [Tooltip("Цвет заблокированных ячеек")]
    public Color blockedColor = new Color(1f, 0f, 0f, 0.5f);
    
    [Tooltip("Показывать препятствия")]
    public bool showObstacles = true;
    
    [Tooltip("Цвет препятствий")]
    public Color obstacleColor = new Color(1f, 0.5f, 0f, 0.7f);
    
    [Header("Performance")]
    [Tooltip("Максимальное расстояние для отрисовки (0 = без ограничений)")]
    public float maxDrawDistance = 150f; // Уменьшено со 200
    
    [Tooltip("Рисовать каждую N-ю ячейку (1 = все, 2 = каждую вторую)")]
    [Range(1, 4)]
    public int drawEveryNthCell = 2; // Пропускаем ячейки для производительности
    
    [Tooltip("Использовать упрощённую визуализацию (быстрее)")]
    public bool simplifiedMode = true;
    
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
        if (!showGrid && !showObstacles) return;
        if (world == null || !world.IsCreated || entityManager == null) return;
        
        Gizmos.matrix = transform.localToWorldMatrix;
        
        if (showObstacles)
            DrawObstacles();
        
        if (showGrid)
            DrawNavigationGrids();
    }
    
    private void DrawNavigationGrids()
    {
        var cameraPos = GetCameraPosition();
        
        var gridQuery = entityManager.CreateEntityQuery(
            ComponentType.ReadOnly<NavigationGrid>(),
            ComponentType.ReadOnly<Chunk>()
        );
        
        if (gridQuery.CalculateEntityCount() == 0)
        {
            gridQuery.Dispose();
            return;
        }
        
        var grids = gridQuery.ToComponentDataArray<NavigationGrid>(Unity.Collections.Allocator.Temp);
        var chunks = gridQuery.ToComponentDataArray<Chunk>(Unity.Collections.Allocator.Temp);
        
        for (int i = 0; i < grids.Length; i++)
        {
            var grid = grids[i];
            var chunk = chunks[i];
            
            if (!grid.IsValid) continue;
            
            // Distance culling
            if (maxDrawDistance > 0)
            {
                var chunkCenter = new float2(
                    chunk.WorldPosition.x + ChunkConstants.CHUNK_SIZE * 0.5f,
                    chunk.WorldPosition.y + ChunkConstants.CHUNK_SIZE * 0.5f
                );
                var dist = math.distance(cameraPos, chunkCenter);
                if (dist > maxDrawDistance) continue;
            }
            
            if (simplifiedMode)
                DrawGridSimplified(ref grid.GridBlob.Value, chunk.WorldPosition);
            else
                DrawGrid(ref grid.GridBlob.Value, chunk.WorldPosition);
        }
        
        grids.Dispose();
        chunks.Dispose();
        gridQuery.Dispose();
    }
    
    // Упрощённая отрисовка - только границы чанка и заблокированные области
    private void DrawGridSimplified(ref GridData gridData, float2 chunkWorldPos)
    {
        var cellSize = ChunkConstants.NAV_CELL_SIZE;
        var gridSize = gridData.GridSize;
        
        // Рисуем границу чанка
        Gizmos.color = new Color(0.5f, 0.5f, 0.5f, 0.5f);
        var chunkMin = new Vector3(chunkWorldPos.x, chunkWorldPos.y, 0f);
        var chunkMax = new Vector3(
            chunkWorldPos.x + ChunkConstants.CHUNK_SIZE,
            chunkWorldPos.y + ChunkConstants.CHUNK_SIZE,
            0f
        );
        
        // 4 линии границы
        Gizmos.DrawLine(chunkMin, new Vector3(chunkMax.x, chunkMin.y, 0f));
        Gizmos.DrawLine(new Vector3(chunkMax.x, chunkMin.y, 0f), chunkMax);
        Gizmos.DrawLine(chunkMax, new Vector3(chunkMin.x, chunkMax.y, 0f));
        Gizmos.DrawLine(new Vector3(chunkMin.x, chunkMax.y, 0f), chunkMin);
        
        // Рисуем только заблокированные ячейки с пропуском
        Gizmos.color = blockedColor;
        
        for (int y = 0; y < gridSize; y += drawEveryNthCell)
        {
            for (int x = 0; x < gridSize; x += drawEveryNthCell)
            {
                if (!gridData.IsWalkable(x, y))
                {
                    var cellWorldPos = chunkWorldPos + new float2(x * cellSize, y * cellSize);
                    var center = new Vector3(
                        cellWorldPos.x + cellSize * 0.5f,
                        cellWorldPos.y + cellSize * 0.5f,
                        0f
                    );
                    var size = new Vector3(cellSize * drawEveryNthCell, cellSize * drawEveryNthCell, 0.1f);
                    Gizmos.DrawCube(center, size);
                }
            }
        }
    }
    
    // Полная отрисовка - все ячейки
    private void DrawGrid(ref GridData gridData, float2 chunkWorldPos)
    {
        var cellSize = ChunkConstants.NAV_CELL_SIZE;
        var gridSize = gridData.GridSize;
        
        for (int y = 0; y < gridSize; y += drawEveryNthCell)
        {
            for (int x = 0; x < gridSize; x += drawEveryNthCell)
            {
                var isWalkable = gridData.IsWalkable(x, y);
                
                if (showOnlyBlocked && isWalkable) continue;
                
                var cellWorldPos = chunkWorldPos + new float2(x * cellSize, y * cellSize);
                var color = isWalkable ? walkableColor : blockedColor;
                Gizmos.color = color;
                
                var center = new Vector3(
                    cellWorldPos.x + cellSize * 0.5f,
                    cellWorldPos.y + cellSize * 0.5f,
                    0f
                );
                
                var size = new Vector3(cellSize * drawEveryNthCell, cellSize * drawEveryNthCell, 0.1f);
                Gizmos.DrawCube(center, size);
            }
        }
    }
    
    private void DrawObstacles()
    {
        var obstacleQuery = entityManager.CreateEntityQuery(ComponentType.ReadOnly<StaticObstacle>());
        
        if (obstacleQuery.CalculateEntityCount() == 0)
        {
            obstacleQuery.Dispose();
            return;
        }
        
        var obstacles = obstacleQuery.ToComponentDataArray<StaticObstacle>(Unity.Collections.Allocator.Temp);
        var cameraPos = GetCameraPosition();
        
        Gizmos.color = obstacleColor;
        
        for (int i = 0; i < obstacles.Length; i++)
        {
            var obstacle = obstacles[i];
            
            // Distance culling для препятствий тоже
            if (maxDrawDistance > 0)
            {
                var dist = math.distance(cameraPos, obstacle.Position);
                if (dist > maxDrawDistance) continue;
            }
            
            var center = new Vector3(obstacle.Position.x, obstacle.Position.y, 0f);
            
            // Упрощённая отрисовка препятствий - меньше сегментов
            DrawCircle(center, obstacle.Radius, 16); // Было 32, теперь 16
            
#if UNITY_EDITOR
            // Текст только для близких препятствий
            if (maxDrawDistance == 0 || math.distance(cameraPos, obstacle.Position) < maxDrawDistance * 0.5f)
            {
                UnityEditor.Handles.Label(center, obstacle.Type.ToString());
            }
#endif
        }
        
        obstacles.Dispose();
        obstacleQuery.Dispose();
    }
    
    private void DrawCircle(Vector3 center, float radius, int segments)
    {
        var angleStep = 360f / segments;
        var prevPoint = center + new Vector3(radius, 0, 0);
        
        for (int i = 1; i <= segments; i++)
        {
            var angle = angleStep * i * Mathf.Deg2Rad;
            var newPoint = center + new Vector3(
                Mathf.Cos(angle) * radius,
                Mathf.Sin(angle) * radius,
                0f
            );
            
            Gizmos.DrawLine(prevPoint, newPoint);
            prevPoint = newPoint;
        }
    }
    
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
