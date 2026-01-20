using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

// Система обновления навигационной сетки при изменении препятствий
// Пересоздаёт BlobAsset для чанков, затронутых изменениями
// ВРЕМЕННО ОТКЛЮЧЕНА - вызывает memory leaks при частом обновлении
[UpdateInGroup(typeof(SimulationSystemGroup))]
[DisableAutoCreation]
public partial struct NavigationGridUpdateSystem : ISystem
{
    private EntityQuery obstacleQuery;
    private NativeHashMap<int2, bool> dirtyChunks;
    
    public void OnCreate(ref SystemState state)
    {
        obstacleQuery = new EntityQueryBuilder(Allocator.Temp)
            .WithAll<StaticObstacle>()
            .Build(ref state);
        dirtyChunks = new NativeHashMap<int2, bool>(16, Allocator.Persistent);
        
        // Не требуем обязательного наличия препятствий
        state.RequireForUpdate<NavigationGrid>();
    }
    
    public void OnDestroy(ref SystemState state)
    {
        if (dirtyChunks.IsCreated)
            dirtyChunks.Dispose();
    }
    
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var entityManager = state.EntityManager;
        
        // Проверяем, были ли изменения в StaticObstacle
        // Для простоты пересоздаём сетку для всех чанков при любом изменении
        // TODO: Оптимизировать - отслеживать конкретные изменения
        
        var currentObstacleCount = obstacleQuery.CalculateEntityCount();
        
        // Если препятствий нет, ничего не делаем
        if (currentObstacleCount == 0) return;
        
        // Получаем все чанки с NavigationGrid
        var gridQuery = SystemAPI.QueryBuilder()
            .WithAll<NavigationGrid, Chunk>()
            .Build();
        
        var chunks = gridQuery.ToComponentDataArray<Chunk>(Allocator.Temp);
        var grids = gridQuery.ToComponentDataArray<NavigationGrid>(Allocator.Temp);
        var gridEntities = gridQuery.ToEntityArray(Allocator.Temp);
        
        var allObstacles = obstacleQuery.ToComponentDataArray<StaticObstacle>(Allocator.Temp);
        
        var ecb = new EntityCommandBuffer(Allocator.Temp);
        
        // Обновляем каждый чанк
        for (int i = 0; i < chunks.Length; i++)
        {
            var chunk = chunks[i];
            var grid = grids[i];
            var gridEntity = gridEntities[i];
            
            // Фильтруем препятствия для этого чанка
            var relevantObstacles = GetObstaclesForChunk(chunk.Id, allObstacles, Allocator.Temp);
            
            // Dispose старого BlobAsset
            if (grid.IsValid)
            {
                grid.GridBlob.Dispose();
            }
            
            // Создаём новый BlobAsset
            var newGridBlob = CreateNavigationGrid(chunk.Id, relevantObstacles.AsArray());
            
            // Обновляем компонент
            ecb.SetComponent(gridEntity, new NavigationGrid
            {
                GridBlob = newGridBlob,
                ChunkId = chunk.Id
            });
            
            // Обновляем debug данные
            ref var gridData = ref newGridBlob.Value;
            var debugData = NavigationDebugData.FromGridData(ref gridData, relevantObstacles.Length);
            ecb.SetComponent(gridEntity, debugData);
            
            relevantObstacles.Dispose();
        }
        
        ecb.Playback(entityManager);
        ecb.Dispose();
        
        chunks.Dispose();
        grids.Dispose();
        gridEntities.Dispose();
        allObstacles.Dispose();
    }
    
    // Создать BlobAsset с навигационной сеткой
    private static BlobAssetReference<GridData> CreateNavigationGrid(
        int2 chunkId, 
        NativeArray<StaticObstacle> obstacles)
    {
        var builder = new BlobBuilder(Allocator.Temp);
        ref var gridData = ref builder.ConstructRoot<GridData>();
        
        gridData.ChunkId = chunkId;
        gridData.GridSize = ChunkConstants.NAV_GRID_SIZE;
        
        var cells = builder.Allocate(ref gridData.Cells, 
            ChunkConstants.NAV_GRID_SIZE * ChunkConstants.NAV_GRID_SIZE);
        
        RasterizeObstacles(chunkId, obstacles, cells);
        
        var result = builder.CreateBlobAssetReference<GridData>(Allocator.Persistent);
        builder.Dispose();
        
        return result;
    }
    
    // Растеризация препятствий
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
        
        for (int y = 0; y < gridSize; y++)
        {
            for (int x = 0; x < gridSize; x++)
            {
                var cellWorldPos = chunkWorldPos + new float2(
                    x * cellSize + cellSize * 0.5f,
                    y * cellSize + cellSize * 0.5f
                );
                
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
    
    // Получить препятствия для чанка
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
        
        var margin = ChunkConstants.MAX_OBSTACLE_RADIUS;
        
        for (int i = 0; i < allObstacles.Length; i++)
        {
            var obstacle = allObstacles[i];
            
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
