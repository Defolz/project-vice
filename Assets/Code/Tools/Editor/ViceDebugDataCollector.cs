using Unity.Entities;
using Unity.Collections;
using Unity.Mathematics;
using UnityEditor;

/// <summary>
/// Собирает данные из ECS World для отладки
/// </summary>
public class ViceDebugDataCollector
{
    public bool AutoRefresh = true;
    public float RefreshInterval = 0.5f;
    
    public ViceOverviewData OverviewData { get; private set; }
    public ViceChunkData ChunkData { get; private set; }
    public ViceNPCData NPCData { get; private set; }
    public ViceNavigationData NavigationData { get; private set; }
    
    private double lastRefreshTime;
    
    public ViceDebugDataCollector()
    {
        OverviewData = new ViceOverviewData();
        ChunkData = new ViceChunkData();
        NPCData = new ViceNPCData();
        NavigationData = new ViceNavigationData();
    }
    
    public bool ShouldRefresh()
    {
        return AutoRefresh && EditorApplication.timeSinceStartup - lastRefreshTime > RefreshInterval;
    }
    
    public void Refresh()
    {
        var world = World.DefaultGameObjectInjectionWorld;
        if (world == null || !world.IsCreated) return;
        
        var em = world.EntityManager;
        lastRefreshTime = EditorApplication.timeSinceStartup;
        
        CollectOverview(em);
        CollectChunks(em);
        CollectNPCs(em);
        CollectNavigation(em);
    }
    
    private void CollectOverview(EntityManager em)
    {
        // Count all entities using UniversalQuery
        OverviewData.TotalEntities = em.UniversalQuery.CalculateEntityCount();
        
        var chunkQuery = em.CreateEntityQuery(ComponentType.ReadOnly<Chunk>());
        OverviewData.TotalChunks = chunkQuery.CalculateEntityCount();
        chunkQuery.Dispose();
        
        var npcQuery = em.CreateEntityQuery(ComponentType.ReadOnly<NPCId>());
        OverviewData.TotalNPCs = npcQuery.CalculateEntityCount();
        npcQuery.Dispose();
        
        OverviewData.FPS = 1.0f / UnityEngine.Time.deltaTime;
        
        var gameTimeQuery = em.CreateEntityQuery(ComponentType.ReadOnly<GameTimeComponent>());
        if (gameTimeQuery.CalculateEntityCount() > 0)
        {
            var gameTime = gameTimeQuery.GetSingleton<GameTimeComponent>();
            OverviewData.GameDay = gameTime.Day;
            OverviewData.GameHour = gameTime.Hour;
            OverviewData.GameMinute = gameTime.Minute;
        }
        gameTimeQuery.Dispose();
    }
    
    private void CollectChunks(EntityManager em)
    {
        ChunkData.Chunks.Clear();
        
        var query = em.CreateEntityQuery(ComponentType.ReadOnly<Chunk>());
        var entities = query.ToEntityArray(Allocator.Temp);
        var chunks = query.ToComponentDataArray<Chunk>(Allocator.Temp);
        
        for (int i = 0; i < chunks.Length; i++)
        {
            var npcCount = CountNPCsInChunk(em, chunks[i].Id);
            
            ChunkData.Chunks.Add(new ViceChunkInfo
            {
                Entity = entities[i],
                Id = chunks[i].Id,
                Position = chunks[i].WorldPosition,
                State = chunks[i].State,
                NPCCount = npcCount
            });
        }
        
        entities.Dispose();
        chunks.Dispose();
        query.Dispose();
    }
    
    private void CollectNPCs(EntityManager em)
    {
        NPCData.NPCs.Clear();
        
        var query = em.CreateEntityQuery(
            ComponentType.ReadOnly<NPCId>(),
            ComponentType.ReadOnly<Location>(),
            ComponentType.ReadOnly<NameData>(),
            ComponentType.ReadOnly<Faction>(),
            ComponentType.ReadOnly<StateFlags>()
        );
        
        var entities = query.ToEntityArray(Allocator.Temp);
        var ids = query.ToComponentDataArray<NPCId>(Allocator.Temp);
        var locations = query.ToComponentDataArray<Location>(Allocator.Temp);
        var names = query.ToComponentDataArray<NameData>(Allocator.Temp);
        var factions = query.ToComponentDataArray<Faction>(Allocator.Temp);
        var states = query.ToComponentDataArray<StateFlags>(Allocator.Temp);
        
        for (int i = 0; i < entities.Length; i++)
        {
            NPCData.NPCs.Add(new ViceNPCInfo
            {
                Entity = entities[i],
                Id = ids[i].Value,
                Name = $"{names[i].FirstName} {names[i].LastName}",
                Position = locations[i].GlobalPosition2D,
                ChunkId = locations[i].ChunkId,
                Faction = factions[i].Value,
                IsAlive = states[i].IsAlive,
                IsWanted = states[i].IsWanted,
                IsInjured = states[i].IsInjured
            });
        }
        
        entities.Dispose();
        ids.Dispose();
        locations.Dispose();
        names.Dispose();
        factions.Dispose();
        states.Dispose();
        query.Dispose();
    }
    
    private int CountNPCsInChunk(EntityManager em, int2 chunkId)
    {
        var query = em.CreateEntityQuery(ComponentType.ReadOnly<Location>());
        var locations = query.ToComponentDataArray<Location>(Allocator.Temp);
        
        int count = 0;
        for (int i = 0; i < locations.Length; i++)
        {
            if (locations[i].ChunkId.Equals(chunkId))
                count++;
        }
        
        locations.Dispose();
        query.Dispose();
        return count;
    }
    
    private void CollectNavigation(EntityManager em)
    {
        NavigationData.Chunks.Clear();
        NavigationData.TotalWalkableCells = 0;
        NavigationData.TotalBlockedCells = 0;
        NavigationData.TotalObstacles = 0;
        NavigationData.TotalMemoryKB = 0;
        
        // Collect navigation grid data
        var gridQuery = em.CreateEntityQuery(ComponentType.ReadOnly<NavigationDebugData>());
        var debugData = gridQuery.ToComponentDataArray<NavigationDebugData>(Allocator.Temp);
        
        for (int i = 0; i < debugData.Length; i++)
        {
            var data = debugData[i];
            
            NavigationData.Chunks.Add(new ViceNavigationChunkInfo
            {
                ChunkId = data.ChunkId,
                WalkableCells = data.WalkableCells,
                BlockedCells = data.BlockedCells,
                ObstacleCount = data.ObstacleCount,
                WalkablePercentage = data.WalkablePercentage
            });
            
            NavigationData.TotalWalkableCells += data.WalkableCells;
            NavigationData.TotalBlockedCells += data.BlockedCells;
            NavigationData.TotalObstacles += data.ObstacleCount;
        }
        
        debugData.Dispose();
        gridQuery.Dispose();
        
        // Calculate memory usage (approximate)
        // Each chunk: 64*64 bytes = 4096 bytes = 4KB
        NavigationData.TotalMemoryKB = NavigationData.Chunks.Count * 4f;
    }
}
