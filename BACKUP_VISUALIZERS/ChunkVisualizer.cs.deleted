using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

// MonoBehaviour для визуализации чанков в сцене (редакторе Unity) - теперь 2D X-Y
public class ChunkVisualizer : MonoBehaviour
{
    [Tooltip("Цвет границы загруженного чанка")]
    public Color loadedChunkColor = Color.green;
    [Tooltip("Цвет границы выгруженного чанка")]
    public Color unloadedChunkColor = Color.red;
    [Tooltip("Цвет границы чанка в процессе генерации")]
    public Color generatingChunkColor = Color.yellow;
    [Tooltip("Цвет центральной точки чанка")]
    public Color centerPointColor = Color.blue;
    [Tooltip("Толщина линии границы")] // ВНИМАНИЕ: Gizmos.DrawLine не учитывает толщину линии, установленную в Inspector
    public float lineThickness = 1.0f; // Это значение не влияет на Gizmos.DrawLine
    [Tooltip("Размер центральной точки")]
    public float pointSize = 0.5f;

    private World world;
    private EntityManager entityManager;

    void Start()
    {
        world = World.DefaultGameObjectInjectionWorld;
        if (world != null)
        {
            entityManager = world.EntityManager;
        }
        else
        {
            Debug.LogError("ChunkVisualizer: Default World not found! Make sure ECS systems are running.");
        }
    }

    void OnDrawGizmos()
    {
        if (world == null || !world.IsCreated || entityManager == null) return;

        var chunkMapSingletonQuery = entityManager.CreateEntityQuery(typeof(ChunkMapSingleton));
        if (chunkMapSingletonQuery.CalculateEntityCount() == 0) 
        {
            chunkMapSingletonQuery.Dispose();
            return; 
        }

        var singletonEntity = chunkMapSingletonQuery.GetSingletonEntity();
        if (!entityManager.HasComponent<ChunkMapSingleton>(singletonEntity)) 
        {
            chunkMapSingletonQuery.Dispose();
            return;
        }

        var singleton = entityManager.GetComponentData<ChunkMapSingleton>(singletonEntity);

        var mapDataEntity = singleton.ChunkMapDataEntity;
        if (!entityManager.HasComponent<ChunkMapBufferData>(mapDataEntity) ||
            !entityManager.HasBuffer<ChunkMapEntry>(mapDataEntity)) 
        {
            chunkMapSingletonQuery.Dispose();
            return;
        }

        var chunkMapBuffer = entityManager.GetBuffer<ChunkMapEntry>(mapDataEntity);

        // Применяем трансформ объекта
        Gizmos.matrix = transform.localToWorldMatrix; 

        // Рисуем каждый чанк из буфера
        foreach (var entry in chunkMapBuffer)
        {
            var chunkId = entry.Id;
            // ВСЕГДА вычисляем worldPos из chunkId (теперь X-Y)
            var worldPos = new float2(chunkId.x * ChunkConstants.CHUNK_SIZE, chunkId.y * ChunkConstants.CHUNK_SIZE);
            
            // Определяем 4 угла чанка в 3D пространстве, но используем X-Y как плоскость, Z = 0
            // Для визуализации в Unity Scene View, даже в 2D, мы всё равно используем Vector3
            // где Z = 0 создаёт плоскость X-Y
            var cornerBL = new Vector3(worldPos.x, worldPos.y, 0); // Bottom Left  (X, Y, 0)
            var cornerBR = new Vector3(worldPos.x + ChunkConstants.CHUNK_SIZE, worldPos.y, 0); // Bottom Right (X+size, Y, 0)
            var cornerTR = new Vector3(worldPos.x + ChunkConstants.CHUNK_SIZE, worldPos.y + ChunkConstants.CHUNK_SIZE, 0); // Top Right (X+size, Y+size, 0)
            var cornerTL = new Vector3(worldPos.x, worldPos.y + ChunkConstants.CHUNK_SIZE, 0); // Top Left (X, Y+size, 0)
            
            // Выбираем цвет в зависимости от состояния
            Color gizmoColor;
            switch (entry.State)
            {
                case ChunkState.Loaded:
                    gizmoColor = loadedChunkColor;
                    break;
                case ChunkState.Unloaded:
                    gizmoColor = unloadedChunkColor;
                    break;
                case ChunkState.Generating:
                    gizmoColor = generatingChunkColor;
                    break;
                default:
                    gizmoColor = unloadedChunkColor; // Цвет по умолчанию для других состояний
                    break;
            }

            // Рисуем границы чанка как 4 линии в X-Y плоскости (Z=0)
            Gizmos.color = gizmoColor;
            Gizmos.DrawLine(cornerBL, cornerBR); // Нижняя сторона
            Gizmos.DrawLine(cornerBR, cornerTR); // Правая сторона
            Gizmos.DrawLine(cornerTR, cornerTL); // Верхняя сторона
            Gizmos.DrawLine(cornerTL, cornerBL); // Левая сторона

            // Рисуем центральную точку (тоже в X-Y плоскости, Z=0)
            var centerWorldPos = new Vector3(worldPos.x + ChunkConstants.CHUNK_SIZE / 2, worldPos.y + ChunkConstants.CHUNK_SIZE / 2, 0);
            Gizmos.color = centerPointColor;
            Gizmos.DrawSphere(centerWorldPos, pointSize);
        }

        chunkMapSingletonQuery.Dispose(); // Всегда освобождаем EntityQuery
    }
}