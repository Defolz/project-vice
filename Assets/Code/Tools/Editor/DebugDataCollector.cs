using Unity.Entities;
using Unity.Collections;
using Unity.Mathematics;
using UnityEditor;

/// <summary>
/// Собирает данные из ECS World для отладки
/// </summary>
public class DebugDataCollector
{
    public bool AutoRefresh = true;
    public float RefreshInterval = 0.5f;
    
    public OverviewData OverviewData { get; private set; }
    public ChunkData ChunkData { get; private set; }
    public NPCData NPCData { get; private set; }
    
    private double lastRefreshTime;
    
    public DebugDataCollector()
    {
        OverviewData = new OverviewData();
        ChunkData = new ChunkData();
        NPCData = new NPCData();
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
    }
    
    private void CollectOverview(EntityManager em)
    {
        OverviewData.TotalEntities = em.CreateEntityQuery(ComponentType.ReadOnly<Entity>()).CalculateEntityCount();
        OverviewData.TotalChunks = em.CreateEntityQuery(ComponentType.ReadOnly<Chunk>()).CalculateEntityCount();
        OverviewData.TotalNPCs = em.CreateEntityQuery(ComponentType.ReadOnly<NPCId>()).CalculateEntityCount();
        OverviewData.FPS = 1.0f / UnityEngine.Time.deltaTime;
        
        var gameTimeQuery = em.CreateEntityQuery(ComponentType.ReadOnly<GameTimeComponent>());
        if (gameTimeQuery.CalculateEntityCount() > 0)
        {
            var gameTime = gameTimeQuery.GetSingleton<GameTimeComponent>();
            OverviewData.GameDay = gameTime.DayCount;
            OverviewData.GameHour = (int)(gameTime.TimeOfDay / 3600f);
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
            
            ChunkData.Chunks.Add(new ChunkInfo
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
            NPCData.NPCs.Add(new NPCInfo
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
}
