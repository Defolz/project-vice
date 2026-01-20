using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;
using UnityEngine;
using System.Collections.Generic;

public class ChunkDebugVisualizer : MonoBehaviour
{
    public bool ShowChunkInfo = true;
    public Color ChunkInfoColor = Color.white;
    public bool ShowMemoryUsage = true;
    public Color MemoryInfoColor = Color.yellow;
    
    [Header("Performance Settings")]
    public int MaxChunksToDisplay = 100;

    public bool ShowFPS = true;
    public Color FPSColor = Color.green;

    // FPS variables
    private float deltaTime = 0.0f;
    private float fps = 0.0f;
    
    private EntityManager entityManager;
    private World world;
    private List<ChunkMapEntry> cachedEntries = new List<ChunkMapEntry>();
    private int lastFrameUpdated = -1;
    private ChunkMemoryUsage cachedMemoryUsage;
    private int cachedNPCCount = 0;

    private void OnEnable()
    {
        world = World.DefaultGameObjectInjectionWorld;
        if (world != null)
        {
            entityManager = world.EntityManager;
        }
    }

    private void OnDisable()
    {
        // Cleanup references when disabled
    }

    private void Update()
    {
        // Update FPS calculation every frame
        deltaTime += (Time.unscaledDeltaTime - deltaTime) * 0.1f;
        fps = 1.0f / deltaTime;

        if (world != World.DefaultGameObjectInjectionWorld)
        {
            world = World.DefaultGameObjectInjectionWorld;
            if (world != null)
            {
                entityManager = world.EntityManager;
            }
        }
        
        // Update cache only once per frame to avoid buffer conflicts
        if (Time.frameCount != lastFrameUpdated)
        {
            UpdateCache();
            lastFrameUpdated = Time.frameCount;
        }
    }

    private void UpdateCache()
    {
        cachedEntries.Clear();
        
        var singletonQuery = entityManager.CreateEntityQuery(ComponentType.ReadOnly<ChunkMapSingleton>());
        if (singletonQuery.CalculateEntityCount() > 0)
        {
            var singletonEntity = singletonQuery.GetSingletonEntity();
            var singleton = entityManager.GetComponentData<ChunkMapSingleton>(singletonEntity);
            var chunkMapDataEntity = singleton.ChunkMapDataEntity;
            
            if (entityManager.HasBuffer<ChunkMapEntry>(chunkMapDataEntity))
            {
                var chunkMapBuffer = entityManager.GetBuffer<ChunkMapEntry>(chunkMapDataEntity);
                
                // Copy entries to safe list to avoid buffer disposal issues
                for (int i = 0; i < chunkMapBuffer.Length; i++)
                {
                    cachedEntries.Add(chunkMapBuffer[i]);
                }
            }
        }
        singletonQuery.Dispose();

        // Cache NPC count separately to avoid conflicts
        var npcQuery = entityManager.CreateEntityQuery(
            ComponentType.ReadOnly<NPCId>(),
            ComponentType.ReadOnly<Location>());
        cachedNPCCount = npcQuery.CalculateEntityCount();
        npcQuery.Dispose();
        
        // Calculate memory usage with cached data
        cachedMemoryUsage = CalculateMemoryUsage(cachedEntries.Count, cachedNPCCount);
    }

    private void OnValidate()
    {
        MaxChunksToDisplay = Mathf.Max(1, MaxChunksToDisplay);
    }

    private void OnGUI()
    {

        if (!ShowMemoryUsage && !ShowChunkInfo) return;

        GUIStyle style = new GUIStyle(GUI.skin.label);
        
        float yPos = 10f;
        int displayedChunks = 0;

        if (ShowFPS)
        {
            style.normal.textColor = GetFPSColor(fps);
            GUI.Label(new Rect(10, yPos, 200, 20), 
                $"FPS: {(int)fps}", 
                style);
            yPos += 25;
        }
        
        if (cachedEntries.Count > 0)
        {
            // Display memory usage info
            if (ShowMemoryUsage)
            {
                style.normal.textColor = MemoryInfoColor;
                GUI.Label(new Rect(10, yPos, 400, 20), 
                    $"Total Chunks: {cachedEntries.Count} | Memory Usage: {FormatBytes(cachedMemoryUsage.TotalMemory)}", 
                    style);
                yPos += 25;
                
                GUI.Label(new Rect(10, yPos, 600, 20), 
                    $"NPC Count: {cachedNPCCount} | Avg per Chunk: {cachedMemoryUsage.AvgNPCPerChunk:F1}", 
                    style);
                yPos += 25;
            }

            // Display individual chunk info
            if (ShowChunkInfo)
            {
                for (int i = 0; i < cachedEntries.Count && displayedChunks < MaxChunksToDisplay; i++)
                {
                    var entry = cachedEntries[i];
                    
                    string chunkInfo = $"Chunk[{entry.Id.x},{entry.Id.y}] - State: {entry.State}";
                    
                    chunkInfo += $" | NPCs: ?"; // Placeholder - we can't safely get per-chunk stats without more complex caching
                    
                    style.normal.textColor = GetChunkStateColor(entry.State);
                    GUI.Label(new Rect(10, yPos, 400, 20), chunkInfo, style);
                    yPos += 20;
                    displayedChunks++;
                }
            }
        }
        else
        {
            if (ShowMemoryUsage)
            {
                style.normal.textColor = MemoryInfoColor;
                GUI.Label(new Rect(10, yPos, 400, 20), "No chunks loaded", style);
                yPos += 25;
            }
        }
    }

    private Color GetFPSColor(float currentFPS)
    {
        if (currentFPS > 50) return FPSColor;           // Green for good performance
        if (currentFPS > 30) return Color.yellow;      // Yellow for moderate performance
        return Color.red;                              // Red for poor performance
    }

    private Color GetChunkStateColor(ChunkState state)
    {
        switch (state)
        {
            case ChunkState.Unloaded: return Color.gray;
            case ChunkState.Generating: return Color.yellow;
            case ChunkState.Loaded: return Color.green;
            case ChunkState.Dirty: return Color.red;
            case ChunkState.Unloading: return Color.magenta;
            default: return Color.white;
        }
    }

    private ChunkMemoryUsage CalculateMemoryUsage(int chunkCount, int npcCount)
    {
        ChunkMemoryUsage usage = new ChunkMemoryUsage();
        
        usage.ChunkCount = chunkCount;
        usage.NPCCount = npcCount;
        
        // Estimate basic chunk memory
        usage.TotalMemory = usage.ChunkCount * (sizeof(int) * 2 + sizeof(float) * 2); // Rough estimate for chunk data
        
        if (usage.ChunkCount > 0)
        {
            usage.AvgNPCPerChunk = (float)usage.NPCCount / usage.ChunkCount;
        }
        
        // Add estimated NPC memory based on component sizes
        // This is a rough approximation - actual memory will vary
        const int approximateNPCSize = 
            sizeof(uint) + sizeof(int) + sizeof(uint) + // NPCId
            128 + 128 + 128 + // NameData (FixedString128Bytes x3) 
            sizeof(int) * 2 + sizeof(float) * 2 + // Location
            sizeof(int) + // Faction
            sizeof(byte) * 4 + // StateFlags
            sizeof(float) * 3; // Traits
        
        long npcComponentMemory = usage.NPCCount * approximateNPCSize;
        usage.TotalMemory += npcComponentMemory;
        
        // Add buffer overhead estimation
        long bufferOverhead = usage.NPCCount * (sizeof(int) * 4); // Rough estimate for buffer overhead
        usage.TotalMemory += bufferOverhead;
        
        return usage;
    }

    private string FormatBytes(long bytes)
    {
        string[] suffixes = {"B", "KB", "MB", "GB", "TB"};
        int counter = 0;
        double number = (double)bytes;
        while (Mathf.Abs((float)number) >= 1024f && counter < suffixes.Length - 1)
        {
            counter++;
            number /= 1024f;
        }
        return string.Format("{0:n1}{1}", number, suffixes[counter]);
    }

    
}

public struct ChunkMemoryUsage
{
    public int ChunkCount;
    public int NPCCount;
    public float AvgNPCPerChunk;
    public long TotalMemory;
}

// Add this component to track statistics per chunk
public struct ChunkStatistics : IComponentData
{
    public int NPCCount;
    public int BuildingCount;
    public int ResourceCount;
}