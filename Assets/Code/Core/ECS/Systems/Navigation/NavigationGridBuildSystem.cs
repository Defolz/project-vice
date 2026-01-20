using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;

// Система построения навигационной сетки для чанков
// Создаёт BlobAsset с сеткой при загрузке нового чанка
[UpdateInGroup(typeof(ChunkManagementGroup))]
public partial struct NavigationGridBuildSystem : ISystem
{
    private EntityQuery newChunksQuery;
    
    public void OnCreate(ref SystemState state)
    {
        // Query для чанков без NavigationGrid (новые чанки), исключая префабы
        newChunksQuery = new EntityQueryBuilder(Allocator.Temp)
            .WithAll<Chunk>()
            .WithNone<NavigationGrid, Prefab>()
            .Build(ref state);
        
        state.RequireForUpdate(newChunksQuery);
    }
    
    public void OnDestroy(ref SystemState state)
    {
        // Очищаем все BlobAssets при выходе из Play Mode
        var gridQuery = new EntityQueryBuilder(Allocator.Temp)
            .WithAll<NavigationGrid>()
            .Build(ref state);
        
        if (!gridQuery.IsEmpty)
        {
            var grids = gridQuery.ToComponentDataArray<NavigationGrid>(Allocator.Temp);
            
            for (int i = 0; i < grids.Length; i++)
            {
                if (grids[i].IsValid)
                {
                    grids[i].GridBlob.Dispose();
                }
            }
            
            grids.Dispose();
        }
    }
    
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var entityManager = state.EntityManager;
        
        // Получаем все новые чанки
        var chunks = newChunksQuery.ToComponentDataArray<Chunk>(Allocator.Temp);
        var chunkEntities = newChunksQuery.ToEntityArray(Allocator.Temp);
        
        // Получаем все StaticObstacle в мире
        var obstacleQuery = SystemAPI.QueryBuilder()
            .WithAll<StaticObstacle>()
            .Build();
        var allObstacles = obstacleQuery.ToComponentDataArray<StaticObstacle>(Allocator.Temp);
        
        // Обрабатываем каждый новый чанк
        for (int i = 0; i < chunks.Length; i++)
        {
            var chunk = chunks[i];
            var chunkEntity = chunkEntities[i];
            
            // Проверяем, что чанк загружен
            if (chunk.State != ChunkState.Loaded) continue;
            
            // Фильтруем препятствия, которые влияют на этот чанк
            var relevantObstacles = GetObstaclesForChunk(chunk.Id, allObstacles, Allocator.Temp);
            
            // Создаём BlobAsset с навигационной сеткой
            var gridBlob = CreateNavigationGrid(chunk.Id, relevantObstacles.AsArray());
            
            // Добавляем NavigationGrid компонент НАПРЯМУЮ
            entityManager.AddComponentData(chunkEntity, new NavigationGrid
            {
                GridBlob = gridBlob,
                ChunkId = chunk.Id
            });
            
            // Создаём debug данные
            ref var gridData = ref gridBlob.Value;
            var debugData = NavigationDebugData.FromGridData(ref gridData, relevantObstacles.Length);
            entityManager.AddComponentData(chunkEntity, debugData);
            
            relevantObstacles.Dispose();
        }
        
        chunks.Dispose();
        chunkEntities.Dispose();
        allObstacles.Dispose();
    }
    
    // Создать BlobAsset с навигационной сеткой для чанка
    private static BlobAssetReference<GridData> CreateNavigationGrid(
        int2 chunkId, 
        NativeArray<StaticObstacle> obstacles)
    {
        var builder = new BlobBuilder(Allocator.Temp);
        ref var gridData = ref builder.ConstructRoot<GridData>();
        
        gridData.ChunkId = chunkId;
        gridData.GridSize = ChunkConstants.NAV_GRID_SIZE;
        
        // Allocate cells array
        var cells = builder.Allocate(ref gridData.Cells, 
            ChunkConstants.NAV_GRID_SIZE * ChunkConstants.NAV_GRID_SIZE);
        
        // Растеризация препятствий
        RasterizeObstacles(chunkId, obstacles, cells);
        
        var result = builder.CreateBlobAssetReference<GridData>(Allocator.Persistent);
        builder.Dispose();
        
        return result;
    }
    
    // Растеризация препятствий в сетку
    private static void RasterizeObstacles(
        int2 chunkId, 
        NativeArray<StaticObstacle> obstacles, 
        BlobBuilderArray<byte> cells)
    {
        var gridSize = ChunkConstants.NAV_GRID_SIZE;
        var cellSize = ChunkConstants.NAV_CELL_SIZE;
        var chunkWorldPos = new float2(
            chunkId.x * ChunkConstants.CHUNK_SIZE,
            chunkId.y * ChunkConstants.CHUNK_SIZE
        );
        
        // Проходим по каждой ячейке сетки
        for (int y = 0; y < gridSize; y++)
        {
            for (int x = 0; x < gridSize; x++)
            {
                // Вычисляем мировую позицию центра ячейки
                var cellWorldPos = chunkWorldPos + new float2(
                    x * cellSize + cellSize * 0.5f,
                    y * cellSize + cellSize * 0.5f
                );
                
                // Проверяем, блокируется ли ячейка каким-либо препятствием
                bool blocked = false;
                for (int i = 0; i < obstacles.Length; i++)
                {
                    var obstacle = obstacles[i];
                    var distance = math.distance(cellWorldPos, obstacle.Position);
                    
                    if (distance < obstacle.Radius)
                    {
                        blocked = true;
                        break;
                    }
                }
                
                cells[y * gridSize + x] = blocked ? (byte)1 : (byte)0;
            }
        }
    }
    
    // Получить препятствия, влияющие на чанк (включая соседние)
    private static NativeList<StaticObstacle> GetObstaclesForChunk(
        int2 chunkId, 
        NativeArray<StaticObstacle> allObstacles, 
        Allocator allocator)
    {
        var result = new NativeList<StaticObstacle>(allocator);
        
        var chunkMin = new float2(
            chunkId.x * ChunkConstants.CHUNK_SIZE,
            chunkId.y * ChunkConstants.CHUNK_SIZE
        );
        var chunkMax = chunkMin + new float2(
            ChunkConstants.CHUNK_SIZE,
            ChunkConstants.CHUNK_SIZE
        );
        
        // Margin для учёта препятствий, которые могут влиять на края чанка
        var margin = ChunkConstants.MAX_OBSTACLE_RADIUS;
        
        for (int i = 0; i < allObstacles.Length; i++)
        {
            var obstacle = allObstacles[i];
            
            // Проверяем, может ли препятствие влиять на чанк
            if (obstacle.Position.x + obstacle.Radius + margin >= chunkMin.x &&
                obstacle.Position.x - obstacle.Radius - margin <= chunkMax.x &&
                obstacle.Position.y + obstacle.Radius + margin >= chunkMin.y &&
                obstacle.Position.y - obstacle.Radius - margin <= chunkMax.y)
            {
                result.Add(obstacle);
            }
        }
        
        return result;
    }
}
