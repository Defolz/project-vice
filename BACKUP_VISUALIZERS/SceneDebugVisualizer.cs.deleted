using UnityEngine;
using Unity.Entities;
using Unity.Collections;
using Unity.Mathematics;

/// <summary>
/// Визуализация чанков и NPC в Scene View. Добавьте на GameObject в сцене.
/// </summary>
[ExecuteAlways]
public class SceneDebugVisualizer : MonoBehaviour
{
    [Header("Chunk Visualization")]
    public bool ShowChunks = true;
    public bool ShowChunkLabels = true;
    public int MaxChunksToShow = 50;
    
    [Header("NPC Visualization")]
    public bool ShowNPCs = true;
    public float NPCPointSize = 0.3f;
    
    [Header("Performance")]
    [Range(0.1f, 2f)]
    public float UpdateInterval = 0.5f;
    
    private float lastUpdate;
    private ChunkCache[] cachedChunks;
    private NPCCache[] cachedNPCs;
    
    private void Update()
    {
        if (Time.time - lastUpdate >= UpdateInterval)
        {
            UpdateCache();
            lastUpdate = Time.time;
        }
    }
    
    private void UpdateCache()
    {
        var world = World.DefaultGameObjectInjectionWorld;
        if (world == null || !world.IsCreated) return;
        
        var em = world.EntityManager;
        
        // Cache chunks
        if (ShowChunks)
        {
            var chunkQuery = em.CreateEntityQuery(ComponentType.ReadOnly<Chunk>());
            var chunks = chunkQuery.ToComponentDataArray<Chunk>(Allocator.Temp);
            
            int count = Mathf.Min(chunks.Length, MaxChunksToShow);
            cachedChunks = new ChunkCache[count];
            
            for (int i = 0; i < count; i++)
            {
                cachedChunks[i] = new ChunkCache
                {
                    Id = chunks[i].Id,
                    Position = chunks[i].WorldPosition,
                    State = chunks[i].State
                };
            }
            
            chunks.Dispose();
            chunkQuery.Dispose();
        }
        
        // Cache NPCs
        if (ShowNPCs)
        {
            var npcQuery = em.CreateEntityQuery(
                ComponentType.ReadOnly<NPCId>(),
                ComponentType.ReadOnly<Location>(),
                ComponentType.ReadOnly<StateFlags>()
            );
            
            var locations = npcQuery.ToComponentDataArray<Location>(Allocator.Temp);
            var states = npcQuery.ToComponentDataArray<StateFlags>(Allocator.Temp);
            
            cachedNPCs = new NPCCache[locations.Length];
            
            for (int i = 0; i < locations.Length; i++)
            {
                cachedNPCs[i] = new NPCCache
                {
                    Position = locations[i].GlobalPosition2D,
                    IsAlive = states[i].IsAlive
                };
            }
            
            locations.Dispose();
            states.Dispose();
            npcQuery.Dispose();
        }
    }
    
    private void OnDrawGizmos()
    {
        if (ShowChunks && cachedChunks != null)
        {
            foreach (var chunk in cachedChunks)
            {
                DrawChunk(chunk);
            }
        }
        
        if (ShowNPCs && cachedNPCs != null)
        {
            foreach (var npc in cachedNPCs)
            {
                DrawNPC(npc);
            }
        }
    }
    
    private void DrawChunk(ChunkCache chunk)
    {
        float size = ChunkConstants.CHUNK_SIZE;
        Vector3 center = new Vector3(chunk.Position.x + size * 0.5f, chunk.Position.y + size * 0.5f, 0);
        
        // Draw border
        Gizmos.color = GetStateColor(chunk.State);
        
        Vector3 bl = new Vector3(chunk.Position.x, chunk.Position.y, 0);
        Vector3 br = new Vector3(chunk.Position.x + size, chunk.Position.y, 0);
        Vector3 tr = new Vector3(chunk.Position.x + size, chunk.Position.y + size, 0);
        Vector3 tl = new Vector3(chunk.Position.x, chunk.Position.y + size, 0);
        
        Gizmos.DrawLine(bl, br);
        Gizmos.DrawLine(br, tr);
        Gizmos.DrawLine(tr, tl);
        Gizmos.DrawLine(tl, bl);
        
#if UNITY_EDITOR
        if (ShowChunkLabels)
        {
            var style = new GUIStyle();
            style.normal.textColor = GetStateColor(chunk.State);
            style.fontSize = 10;
            style.fontStyle = FontStyle.Bold;
            UnityEditor.Handles.Label(center, $"[{chunk.Id.x},{chunk.Id.y}]", style);
        }
#endif
    }
    
    private void DrawNPC(NPCCache npc)
    {
        Gizmos.color = npc.IsAlive ? new Color(0f, 0.8f, 1f, 0.9f) : new Color(0.5f, 0.5f, 0.5f, 0.5f);
        Vector3 pos = new Vector3(npc.Position.x, npc.Position.y, -0.1f);
        Gizmos.DrawSphere(pos, NPCPointSize);
    }
    
    private Color GetStateColor(ChunkState state)
    {
        switch (state)
        {
            case ChunkState.Loaded: return Color.green;
            case ChunkState.Generating: return Color.yellow;
            case ChunkState.Dirty: return Color.red;
            case ChunkState.Unloading: return Color.magenta;
            default: return Color.gray;
        }
    }
    
    private struct ChunkCache
    {
        public int2 Id;
        public float2 Position;
        public ChunkState State;
    }
    
    private struct NPCCache
    {
        public float2 Position;
        public bool IsAlive;
    }
}
