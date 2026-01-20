using Unity.Entities;
using Unity.Mathematics;
using System.Collections.Generic;

/// <summary>
/// Структуры данных для отладки
/// </summary>

public class ViceOverviewData
{
    public int TotalEntities;
    public int TotalChunks;
    public int TotalNPCs;
    public float FPS;
    public int GameDay;
    public int GameHour;
    public int GameMinute;
}

public class ViceChunkData
{
    public List<ViceChunkInfo> Chunks = new List<ViceChunkInfo>();
    public string SearchFilter = "";
}

public class ViceNPCData
{
    public List<ViceNPCInfo> NPCs = new List<ViceNPCInfo>();
    public string SearchFilter = "";
    public bool ShowOnlyAlive = false;
    public int FactionFilter = -1;
}

public struct ViceChunkInfo
{
    public Entity Entity;
    public int2 Id;
    public float2 Position;
    public ChunkState State;
    public int NPCCount;
}

public struct ViceNPCInfo
{
    public Entity Entity;
    public uint Id;
    public string Name;
    public float2 Position;
    public int2 ChunkId;
    public int Faction;
    public bool IsAlive;
    public bool IsWanted;
    public bool IsInjured;
}

public class ViceNavigationData
{
    public List<ViceNavigationChunkInfo> Chunks = new List<ViceNavigationChunkInfo>();
    public int TotalWalkableCells;
    public int TotalBlockedCells;
    public int TotalObstacles;
    public float TotalMemoryKB;
}

public struct ViceNavigationChunkInfo
{
    public int2 ChunkId;
    public int WalkableCells;
    public int BlockedCells;
    public int ObstacleCount;
    public float WalkablePercentage;
}
