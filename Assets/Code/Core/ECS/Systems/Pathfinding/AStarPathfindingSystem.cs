using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;

// Система A* Pathfinding с межчанковой навигацией
// ИСПРАВЛЕНО: Priority Queue, межчанковая навигация, path smoothing, 8-направленная навигация
[BurstCompile]
[UpdateInGroup(typeof(SimulationSystemGroup))]
public partial struct AStarPathfindingSystem : ISystem
{
    private const float PATH_REQUEST_TIMEOUT = 5f;
    private const int MAX_ITERATIONS = 4096; // Увеличено для межчанковых путей
    private const bool USE_8_DIRECTIONS = true; // 8-направленная навигация
    private const bool USE_PATH_SMOOTHING = true; // Path smoothing
    
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
    }
    
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var currentTime = (float)SystemAPI.Time.ElapsedTime;
        var entityManager = state.EntityManager;
        var ecb = new EntityCommandBuffer(Allocator.TempJob);
        
        // Получаем все NavigationGrid
        var gridQuery = SystemAPI.QueryBuilder()
            .WithAll<NavigationGrid, Chunk>()
            .Build();
        
        var grids = gridQuery.ToComponentDataArray<NavigationGrid>(Allocator.TempJob);
        var chunks = gridQuery.ToComponentDataArray<Chunk>(Allocator.TempJob);
        
        // Обрабатываем каждый PathRequest
        foreach (var (pathRequest, entity) in SystemAPI.Query<RefRW<PathRequest>>().WithEntityAccess())
        {
            // Timeout
            if (currentTime - pathRequest.ValueRO.RequestTime > PATH_REQUEST_TIMEOUT)
            {
                pathRequest.ValueRW.Status = PathStatus.Timeout;
                ecb.AddComponent(entity, PathResult.Failed());
                ecb.RemoveComponent<PathRequest>(entity);
                continue;
            }
            
            // Обрабатываем только Pending
            if (pathRequest.ValueRO.Status != PathStatus.Pending)
                continue;
            
            pathRequest.ValueRW.Status = PathStatus.Processing;
            
            var startTime = (float)SystemAPI.Time.ElapsedTime;
            
            // Запускаем A*
            var pathResult = FindPath(
                pathRequest.ValueRO.StartPosition,
                pathRequest.ValueRO.TargetPosition,
                pathRequest.ValueRO.MaxPathLength,
                grids,
                chunks,
                Allocator.TempJob,
                out var waypoints
            );
            
            var calculationTime = (float)SystemAPI.Time.ElapsedTime - startTime;
            
            pathRequest.ValueRW.Status = pathResult;
            
            if (pathResult == PathStatus.Success && waypoints.IsCreated && waypoints.Length > 0)
            {
                ecb.AddComponent(entity, new PathResult(
                    PathStatus.Success,
                    CalculateTotalDistance(waypoints),
                    calculationTime,
                    waypoints.Length
                ));
                
                var waypointBuffer = ecb.AddBuffer<PathWaypoint>(entity);
                for (int i = 0; i < waypoints.Length; i++)
                {
                    waypointBuffer.Add(waypoints[i]);
                }
                
                UnityEngine.Debug.Log($"<color=green>✅ Path FOUND! {waypoints.Length} waypoints, {calculationTime * 1000:F1}ms</color>");
                waypoints.Dispose();
            }
            else
            {
                ecb.AddComponent(entity, PathResult.Failed());
                UnityEngine.Debug.LogWarning($"<color=red>❌ Path FAILED! Status: {pathResult}</color>");
                
                if (waypoints.IsCreated)
                    waypoints.Dispose();
            }
            
            ecb.RemoveComponent<PathRequest>(entity);
        }
        
        ecb.Playback(entityManager);
        ecb.Dispose();
        
        grids.Dispose();
        chunks.Dispose();
    }
    
    // A* с межчанковой навигацией и Priority Queue
    private static PathStatus FindPath(
        float2 start,
        float2 target,
        int maxPathLength,
        NativeArray<NavigationGrid> grids,
        NativeArray<Chunk> chunks,
        Allocator allocator,
        out NativeList<PathWaypoint> waypoints)
    {
        waypoints = new NativeList<PathWaypoint>(allocator);
        
        // Конвертируем в grid координаты (МЕЖЧАНКОВЫЕ!)
        if (!WorldToGrid(start, grids, chunks, out var startChunk, out var startCell))
        {
            UnityEngine.Debug.LogError($"Start position {start} is not in any walkable chunk!");
            return PathStatus.Failed;
        }
        
        if (!WorldToGrid(target, grids, chunks, out var targetChunk, out var targetCell))
        {
            UnityEngine.Debug.LogError($"Target position {target} is not in any walkable chunk!");
            return PathStatus.Failed;
        }
        
        // Если старт == цель
        if (startChunk.Equals(targetChunk) && startCell.Equals(targetCell))
        {
            waypoints.Add(new PathWaypoint(target, 0f));
            return PathStatus.Success;
        }
        
        // A* структуры
        var openSet = new NativeMinHeap<PathNodeWithPriority>(256, allocator); // ИСПРАВЛЕНО: Priority Queue!
        var closedSet = new NativeHashSet<GridCoord>(512, allocator);
        var cameFrom = new NativeHashMap<GridCoord, GridCoord>(512, allocator);
        var gScore = new NativeHashMap<GridCoord, float>(512, allocator);
        
        // Инициализация
        var startCoord = new GridCoord(startChunk, startCell);
        var targetCoord = new GridCoord(targetChunk, targetCell);
        
        var startG = 0f;
        var startH = Heuristic(startChunk, startCell, targetChunk, targetCell);
        var startF = startG + startH;
        
        openSet.Push(new PathNodeWithPriority(startCoord, startF));
        gScore[startCoord] = startG;
        
        int iterations = 0;
        bool pathFound = false;
        GridCoord finalCoord = default;
        
        // Основной цикл A*
        while (openSet.Count > 0 && iterations < MAX_ITERATIONS)
        {
            iterations++;
            
            // ИСПРАВЛЕНО: O(log n) вместо O(n)
            var current = openSet.Pop().Coord;
            
            if (current.Equals(targetCoord))
            {
                pathFound = true;
                finalCoord = current;
                break;
            }
            
            if (closedSet.Contains(current))
                continue;
            
            closedSet.Add(current);
            
            // Получаем соседей (4 или 8 направлений)
            var neighborCount = USE_8_DIRECTIONS ? 8 : 4;
            
            for (int i = 0; i < neighborCount; i++)
            {
                if (!GetNeighbor(current, i, out var neighbor, out var moveCost))
                    continue;
                
                if (closedSet.Contains(neighbor))
                    continue;
                
                // ИСПРАВЛЕНО: Проверяем walkable с учётом чанков
                if (!IsCellWalkable(neighbor.ChunkId, neighbor.CellPos, grids, chunks))
                    continue;
                
                var tentativeG = gScore[current] + moveCost;
                
                if (!gScore.ContainsKey(neighbor) || tentativeG < gScore[neighbor])
                {
                    cameFrom[neighbor] = current;
                    gScore[neighbor] = tentativeG;
                    
                    var h = Heuristic(neighbor.ChunkId, neighbor.CellPos, targetChunk, targetCell);
                    var f = tentativeG + h;
                    
                    openSet.Push(new PathNodeWithPriority(neighbor, f));
                }
            }
        }
        
        // Восстанавливаем путь
        PathStatus result;
        bool shouldReturnEarly = false; // Флаг для раннего выхода из-за maxPathLength
        if (pathFound)
        {
            var path = new NativeList<GridCoord>(allocator);
            var current = finalCoord;
            
            while (!current.Equals(startCoord))
            {
                path.Add(current);
                
                if (!cameFrom.ContainsKey(current))
                    break;
                
                current = cameFrom[current];
                
                if (path.Length > maxPathLength)
                {
                    path.Dispose();
                    // Устанавливаем флаг, чтобы выйти из блока и вернуть Failed
                    shouldReturnEarly = true;
                    break; // Вместо return
                }
            }
            
            if (!shouldReturnEarly)
            {
                path.Add(startCoord);
                
                // ИСПРАВЛЕНО: Path smoothing
                if (USE_PATH_SMOOTHING)
                {
                    SmoothPath(ref path, grids, chunks);
                }
                
                // Конвертируем в world waypoints
                float totalDistance = 0f;
                
                for (int i = path.Length - 1; i >= 0; i--)
                {
                    if (GridToWorld(path[i].ChunkId, path[i].CellPos, chunks, out var worldPos))
                    {
                        waypoints.Add(new PathWaypoint(worldPos, totalDistance));
                        
                        if (i > 0 && GridToWorld(path[i - 1].ChunkId, path[i - 1].CellPos, chunks, out var nextWorldPos))
                        {
                            totalDistance += math.distance(worldPos, nextWorldPos);
                        }
                    }
                }
                
                path.Dispose();
                result = PathStatus.Success;
                
                UnityEngine.Debug.Log($"Path found in {iterations} iterations");
            }
            else
            {
                // Если был ранний выход, path.Dispose() уже выполнен
                result = PathStatus.Failed;
            }
        }
        else
        {
            result = PathStatus.Failed;
            UnityEngine.Debug.LogWarning($"Path search failed after {iterations} iterations");
        }
        
        // Единая точка очистки и возврата
        openSet.Dispose();
        closedSet.Dispose();
        cameFrom.Dispose();
        gScore.Dispose();
        
        return result;
    }
    
    // НОВОЕ: Получить соседа с учётом направления
    private static bool GetNeighbor(GridCoord current, int direction, out GridCoord neighbor, out float moveCost)
    {
        neighbor = default;
        moveCost = 1.0f;
        
        int2 offset;
        bool isDiagonal = false;
        
        switch (direction)
        {
            case 0: offset = new int2(0, 1); break;   // Север
            case 1: offset = new int2(1, 0); break;   // Восток
            case 2: offset = new int2(0, -1); break;  // Юг
            case 3: offset = new int2(-1, 0); break;  // Запад
            case 4: offset = new int2(1, 1); isDiagonal = true; break;   // Северо-Восток
            case 5: offset = new int2(1, -1); isDiagonal = true; break;  // Юго-Восток
            case 6: offset = new int2(-1, -1); isDiagonal = true; break; // Юго-Запад
            case 7: offset = new int2(-1, 1); isDiagonal = true; break;  // Северо-Запад
            default: return false;
        }
        
        moveCost = isDiagonal ? 1.414f : 1.0f;
        
        var newCellPos = current.CellPos + offset;
        var newChunkId = current.ChunkId;
        
        // Обработка перехода через границу чанка
        while (newCellPos.x < 0)
        {
            newChunkId.x--;
            newCellPos.x += ChunkConstants.NAV_GRID_SIZE;
        }
        while (newCellPos.x >= ChunkConstants.NAV_GRID_SIZE)
        {
            newChunkId.x++;
            newCellPos.x -= ChunkConstants.NAV_GRID_SIZE;
        }
        while (newCellPos.y < 0)
        {
            newChunkId.y--;
            newCellPos.y += ChunkConstants.NAV_GRID_SIZE;
        }
        while (newCellPos.y >= ChunkConstants.NAV_GRID_SIZE)
        {
            newChunkId.y++;
            newCellPos.y -= ChunkConstants.NAV_GRID_SIZE;
        }
        
        neighbor = new GridCoord(newChunkId, newCellPos);
        return true;
    }
    
    // НОВОЕ: Path smoothing (удаление коллинеарных точек)
    private static void SmoothPath(ref NativeList<GridCoord> path, NativeArray<NavigationGrid> grids, NativeArray<Chunk> chunks)
    {
        if (path.Length <= 2)
            return;
        
        var smoothed = new NativeList<GridCoord>(path.Length, Allocator.Temp);
        smoothed.Add(path[0]);
        
        int current = 0;
        
        while (current < path.Length - 1)
        {
            int farthest = current + 1;
            
            // Ищем самую дальнюю видимую точку
            for (int i = current + 2; i < path.Length; i++)
            {
                if (HasLineOfSight(path[current], path[i], grids, chunks))
                {
                    farthest = i;
                }
                else
                {
                    break;
                }
            }
            
            if (farthest != path.Length - 1)
                smoothed.Add(path[farthest]);
            
            current = farthest;
        }
        
        smoothed.Add(path[path.Length - 1]);
        
        path.Clear();
        for (int i = 0; i < smoothed.Length; i++)
        {
            path.Add(smoothed[i]);
        }
        
        smoothed.Dispose();
    }
    
    // НОВОЕ: Проверка прямой видимости (для smoothing)
    private static bool HasLineOfSight(GridCoord from, GridCoord to, NativeArray<NavigationGrid> grids, NativeArray<Chunk> chunks)
    {
        // Bresenham line algorithm
        var delta = to.CellPos - from.CellPos;
        var chunkDelta = to.ChunkId - from.ChunkId;
        
        // Если в разных чанках, упрощённая проверка
        if (!chunkDelta.Equals(int2.zero))
            return false;
        
        var steps = math.max(math.abs(delta.x), math.abs(delta.y));
        
        for (int i = 0; i <= steps; i++)
        {
            var t = steps == 0 ? 0f : (float)i / steps;
            var cellPos = new int2(
                (int)math.round(math.lerp(from.CellPos.x, to.CellPos.x, t)),
                (int)math.round(math.lerp(from.CellPos.y, to.CellPos.y, t))
            );
            
            if (!IsCellWalkable(from.ChunkId, cellPos, grids, chunks))
                return false;
        }
        
        return true;
    }
    
    // ИСПРАВЛЕНО: Эвристика с учётом чанков
    private static float Heuristic(int2 chunkA, int2 cellA, int2 chunkB, int2 cellB)
    {
        // Конвертируем в глобальные координаты ячеек
        var globalA = chunkA * ChunkConstants.NAV_GRID_SIZE + cellA;
        var globalB = chunkB * ChunkConstants.NAV_GRID_SIZE + cellB;
        
        // Euclidean distance для диагоналей
        if (USE_8_DIRECTIONS)
        {
            var dx = math.abs(globalA.x - globalB.x);
            var dy = math.abs(globalA.y - globalB.y);
            return math.sqrt(dx * dx + dy * dy);
        }
        
        // Manhattan distance для 4-направленной навигации
        return math.abs(globalA.x - globalB.x) + math.abs(globalA.y - globalB.y);
    }
    
    // ИСПРАВЛЕНО: WorldToGrid с поддержкой любых чанков
    private static bool WorldToGrid(
        float2 worldPos,
        NativeArray<NavigationGrid> grids,
        NativeArray<Chunk> chunks,
        out int2 chunkId,
        out int2 cellPos)
    {
        chunkId = new int2(
            (int)math.floor(worldPos.x / ChunkConstants.CHUNK_SIZE),
            (int)math.floor(worldPos.y / ChunkConstants.CHUNK_SIZE)
        );
        
        var localPos = worldPos - new float2(
            chunkId.x * ChunkConstants.CHUNK_SIZE,
            chunkId.y * ChunkConstants.CHUNK_SIZE
        );
        
        cellPos = new int2(
            (int)math.clamp(localPos.x / ChunkConstants.NAV_CELL_SIZE, 0, ChunkConstants.NAV_GRID_SIZE - 1),
            (int)math.clamp(localPos.y / ChunkConstants.NAV_CELL_SIZE, 0, ChunkConstants.NAV_GRID_SIZE - 1)
        );
        
        // Проверяем, что ячейка walkable
        return IsCellWalkable(chunkId, cellPos, grids, chunks);
    }
    
    // ИСПРАВЛЕНО: GridToWorld с валидацией чанка
    private static bool GridToWorld(int2 chunkId, int2 cellPos, NativeArray<Chunk> chunks, out float2 worldPos)
    {
        // Проверяем существование чанка
        bool chunkExists = false;
        for (int i = 0; i < chunks.Length; i++)
        {
            if (chunks[i].Id.Equals(chunkId))
            {
                chunkExists = true;
                break;
            }
        }
        
        if (!chunkExists)
        {
            worldPos = float2.zero;
            return false;
        }
        
        worldPos = new float2(
            chunkId.x * ChunkConstants.CHUNK_SIZE + cellPos.x * ChunkConstants.NAV_CELL_SIZE + ChunkConstants.NAV_CELL_SIZE * 0.5f,
            chunkId.y * ChunkConstants.CHUNK_SIZE + cellPos.y * ChunkConstants.NAV_CELL_SIZE + ChunkConstants.NAV_CELL_SIZE * 0.5f
        );
        
        return true;
    }
    
    private static bool IsCellWalkable(
        int2 chunkId,
        int2 cellPos,
        NativeArray<NavigationGrid> grids,
        NativeArray<Chunk> chunks)
    {
        if (cellPos.x < 0 || cellPos.x >= ChunkConstants.NAV_GRID_SIZE ||
            cellPos.y < 0 || cellPos.y >= ChunkConstants.NAV_GRID_SIZE)
        {
            return false;
        }
        
        for (int i = 0; i < chunks.Length; i++)
        {
            if (chunks[i].Id.Equals(chunkId) && grids[i].IsValid)
            {
                ref var gridData = ref grids[i].GridBlob.Value;
                return gridData.IsWalkable(cellPos.x, cellPos.y);
            }
        }
        
        return false;
    }
    
    private static float CalculateTotalDistance(NativeList<PathWaypoint> waypoints)
    {
        if (waypoints.Length == 0) return 0f;
        return waypoints[waypoints.Length - 1].Distance;
    }
}

// НОВОЕ: Координаты в grid с учётом чанка
struct GridCoord : IEquatable<GridCoord>
{
    public int2 ChunkId;
    public int2 CellPos;
    
    public GridCoord(int2 chunkId, int2 cellPos)
    {
        ChunkId = chunkId;
        CellPos = cellPos;
    }
    
    public bool Equals(GridCoord other)
    {
        return ChunkId.Equals(other.ChunkId) && CellPos.Equals(other.CellPos);
    }
    
    public override int GetHashCode()
    {
        return ChunkId.GetHashCode() ^ (CellPos.GetHashCode() << 16);
    }
}

// НОВОЕ: Node для Priority Queue
struct PathNodeWithPriority : IComparable<PathNodeWithPriority>
{
    public GridCoord Coord;
    public float Priority; // F score
    
    public PathNodeWithPriority(GridCoord coord, float priority)
    {
        Coord = coord;
        Priority = priority;
    }
    
    public int CompareTo(PathNodeWithPriority other)
    {
        return Priority.CompareTo(other.Priority);
    }
}

// НОВОЕ: Min Heap для Priority Queue
public struct NativeMinHeap<T> : IDisposable where T : unmanaged, IComparable<T>
{
    private NativeList<T> data;
    
    public int Count => data.IsCreated ? data.Length : 0;
    
    public NativeMinHeap(int initialCapacity, Allocator allocator)
    {
        data = new NativeList<T>(initialCapacity, allocator);
    }
    
    public void Push(T item)
    {
        data.Add(item);
        HeapifyUp(data.Length - 1);
    }
    
    public T Pop()
    {
        var result = data[0];
        var lastIndex = data.Length - 1;
        data[0] = data[lastIndex];
        data.RemoveAt(lastIndex);
        
        if (data.Length > 0)
            HeapifyDown(0);
        
        return result;
    }
    
    private void HeapifyUp(int index)
    {
        while (index > 0)
        {
            var parentIndex = (index - 1) / 2;
            if (data[index].CompareTo(data[parentIndex]) >= 0)
                break;
            
            var temp = data[index];
            data[index] = data[parentIndex];
            data[parentIndex] = temp;
            
            index = parentIndex;
        }
    }
    
    private void HeapifyDown(int index)
    {
        while (true)
        {
            var smallest = index;
            var leftChild = 2 * index + 1;
            var rightChild = 2 * index + 2;
            
            if (leftChild < data.Length && data[leftChild].CompareTo(data[smallest]) < 0)
                smallest = leftChild;
            
            if (rightChild < data.Length && data[rightChild].CompareTo(data[smallest]) < 0)
                smallest = rightChild;
            
            if (smallest == index)
                break;
            
            var temp = data[index];
            data[index] = data[smallest];
            data[smallest] = temp;
            
            index = smallest;
        }
    }
    
    public void Dispose()
    {
        if (data.IsCreated)
            data.Dispose();
    }
}
