using UnityEngine;
using Unity.Entities;
using Unity.Collections;
using Unity.Mathematics;
using System.Collections.Generic;

[ExecuteAlways]
public class ImprovedChunkVisualizer : MonoBehaviour
{
    [Header("Visualization Settings")]
    public bool ShowChunkBorders = true;
    public bool ShowChunkLabels = true;
    public bool ShowNPCPositions = true;
    public bool ShowNPCCount = true;
    
    [Header("Display Filters")]
    public bool ShowOnlyLoadedChunks = false;
    public int MaxChunksToVisualize = 50;
    
    [Header("Colors")]
    public Color LoadedChunkColor = new Color(0.2f, 0.8f, 0.2f, 0.3f);
    public Color GeneratingChunkColor = new Color(0.8f, 0.8f, 0.2f, 0.3f);
    public Color UnloadedChunkColor = new Color(0.5f, 0.5f, 0.5f, 0.2f);
    public Color DirtyChunkColor = new Color(0.8f, 0.2f, 0.2f, 0.3f);
    public Color UnloadingChunkColor = new Color(0.8f, 0.2f, 0.8f, 0.3f);
    public Color NPCPointColor = new Color(0f, 0.5f, 1f, 0.8f);
    
    [Header("Sizes")]
    public float BorderThickness = 2f;
    public float NPCPointSize = 0.3f;
    public int LabelFontSize = 12;
    
    [Header("Performance")]
    [Range(0.1f, 5f)]
    public float UpdateInterval = 0.5f;
    
    private float lastUpdateTime;
    private List<CachedChunkData> cachedChunks = new List<CachedChunkData>();
    private List<CachedNPCData> cachedNPCs = new List<CachedNPCData>();
    
    private GUIStyle labelStyle;
    private bool stylesInitialized = false;
    
    private void InitializeStyles()
    {
        if (stylesInitialized) return;
        
        labelStyle = new GUIStyle();
        labelStyle.normal.textColor = Color.white;
        labelStyle.fontSize = LabelFontSize;
        labelStyle.fontStyle = FontStyle.Bold;
        labelStyle.alignment = TextAnchor.MiddleCenter;
        
        stylesInitialized = true;
    }
    
    private void Update()
    {
        if (Time.time - lastUpdateTime >= UpdateInterval)
        {
            UpdateCache();
            lastUpdateTime = Time.time;
        }
    }
    
    private void UpdateCache()
    {
        var world = World.DefaultGameObjectInjectionWorld;
        if (world == null || !world.IsCreated) return;
        
        var em = world.EntityManager;
        
        // Cache chunks
        cachedChunks.Clear();
        var chunkQuery = em.CreateEntityQuery(ComponentType.ReadOnly<Chunk>());
        var chunkEntities = chunkQuery.ToEntityArray(Allocator.Temp);
        var chunks = chunkQuery.ToComponentDataArray<Chunk>(Allocator.Temp);
        
        int count = 0;
        for (int i = 0; i < chunks.Length && count < MaxChunksToVisualize; i++)
        {
            if (ShowOnlyLoadedChunks && chunks[i].State != ChunkState.Loaded)
                continue;
            
            cachedChunks.Add(new CachedChunkData
            {
                Id = chunks[i].Id,
                WorldPosition = chunks[i].WorldPosition,
                State = chunks[i].State,
                Entity = chunkEntities[i]
            });
            count++;
        }
        
        chunkEntities.Dispose();
        chunks.Dispose();
        chunkQuery.Dispose();
        
        // Cache NPCs
        if (ShowNPCPositions)
        {
            cachedNPCs.Clear();
            var npcQuery = em.CreateEntityQuery(
                ComponentType.ReadOnly<NPCId>(),
                ComponentType.ReadOnly<Location>()
            );
            
            var npcEntities = npcQuery.ToEntityArray(Allocator.Temp);
            var locations = npcQuery.ToComponentDataArray<Location>(Allocator.Temp);
            
            for (int i = 0; i < npcEntities.Length; i++)
            {
                cachedNPCs.Add(new CachedNPCData
                {
                    Position = locations[i].GlobalPosition2D,
                    ChunkId = locations[i].ChunkId,
                    Entity = npcEntities[i]
                });
            }
            
            npcEntities.Dispose();
            locations.Dispose();
            npcQuery.Dispose();
        }
    }
    
    private void OnDrawGizmos()
    {
        if (!Application.isPlaying && cachedChunks.Count == 0)
            UpdateCache();
        
        DrawChunks();
        DrawNPCs();
    }
    
    private void DrawChunks()
    {
        foreach (var chunk in cachedChunks)
        {
            Color chunkColor = GetChunkColor(chunk.State);
            
            if (ShowChunkBorders)
            {
                DrawChunkBorder(chunk, chunkColor);
            }
            
            if (ShowChunkLabels)
            {
                DrawChunkLabel(chunk);
            }
        }
    }
    
    private void DrawChunkBorder(CachedChunkData chunk, Color color)
    {
        Gizmos.color = color;
        
        float2 pos = chunk.WorldPosition;
        float size = ChunkConstants.CHUNK_SIZE;
        
        // Draw rectangle using lines
        Vector3 bottomLeft = new Vector3(pos.x, pos.y, 0);
        Vector3 bottomRight = new Vector3(pos.x + size, pos.y, 0);
        Vector3 topRight = new Vector3(pos.x + size, pos.y + size, 0);
        Vector3 topLeft = new Vector3(pos.x, pos.y + size, 0);
        
        Gizmos.DrawLine(bottomLeft, bottomRight);
        Gizmos.DrawLine(bottomRight, topRight);
        Gizmos.DrawLine(topRight, topLeft);
        Gizmos.DrawLine(topLeft, bottomLeft);
        
        // Fill with semi-transparent quad
        Gizmos.color = new Color(color.r, color.g, color.b, color.a * 0.2f);
        DrawQuad(bottomLeft, bottomRight, topRight, topLeft);
    }
    
    private void DrawQuad(Vector3 p1, Vector3 p2, Vector3 p3, Vector3 p4)
    {
        // Unity Gizmos doesn't have a direct quad draw, so we draw two triangles
        Gizmos.DrawLine(p1, p2);
        Gizmos.DrawLine(p2, p3);
        Gizmos.DrawLine(p3, p4);
        Gizmos.DrawLine(p4, p1);
        Gizmos.DrawLine(p1, p3);
    }
    
    private void DrawChunkLabel(CachedChunkData chunk)
    {
        float2 pos = chunk.WorldPosition;
        float size = ChunkConstants.CHUNK_SIZE;
        Vector3 center = new Vector3(pos.x + size * 0.5f, pos.y + size * 0.5f, -0.1f);
        
        string label = $"[{chunk.Id.x},{chunk.Id.y}]";
        if (ShowNPCCount)
        {
            int npcCount = CountNPCsInChunk(chunk.Id);
            label += $"\nNPCs: {npcCount}";
        }
        
#if UNITY_EDITOR
        UnityEditor.Handles.Label(center, label, GetLabelStyle(chunk.State));
#endif
    }
    
    private void DrawNPCs()
    {
        if (!ShowNPCPositions) return;
        
        Gizmos.color = NPCPointColor;
        
        foreach (var npc in cachedNPCs)
        {
            Vector3 pos = new Vector3(npc.Position.x, npc.Position.y, -0.2f);
            Gizmos.DrawSphere(pos, NPCPointSize);
        }
    }
    
    private int CountNPCsInChunk(int2 chunkId)
    {
        int count = 0;
        foreach (var npc in cachedNPCs)
        {
            if (npc.ChunkId.Equals(chunkId))
                count++;
        }
        return count;
    }
    
    private Color GetChunkColor(ChunkState state)
    {
        switch (state)
        {
            case ChunkState.Loaded: return LoadedChunkColor;
            case ChunkState.Generating: return GeneratingChunkColor;
            case ChunkState.Unloaded: return UnloadedChunkColor;
            case ChunkState.Dirty: return DirtyChunkColor;
            case ChunkState.Unloading: return UnloadingChunkColor;
            default: return Color.white;
        }
    }
    
    private GUIStyle GetLabelStyle(ChunkState state)
    {
        InitializeStyles();
        
        var style = new GUIStyle(labelStyle);
        
        switch (state)
        {
            case ChunkState.Loaded:
                style.normal.textColor = Color.green;
                break;
            case ChunkState.Generating:
                style.normal.textColor = Color.yellow;
                break;
            case ChunkState.Unloaded:
                style.normal.textColor = Color.gray;
                break;
            case ChunkState.Dirty:
                style.normal.textColor = Color.red;
                break;
            case ChunkState.Unloading:
                style.normal.textColor = Color.magenta;
                break;
        }
        
        return style;
    }
    
    private struct CachedChunkData
    {
        public int2 Id;
        public float2 WorldPosition;
        public ChunkState State;
        public Entity Entity;
    }
    
    private struct CachedNPCData
    {
        public float2 Position;
        public int2 ChunkId;
        public Entity Entity;
    }
}
