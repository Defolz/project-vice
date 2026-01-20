using Unity.Entities;
using Unity.Mathematics;
using System.Collections.Generic;

/// <summary>
/// Структуры данных для отладки
/// </summary>

public class OverviewData
{
    public int TotalEntities;
    public int TotalChunks;
    public int TotalNPCs;
    public float FPS;
    public int GameDay;
    public int GameHour;
}

public class ChunkData
{
    public List<ChunkInfo> Chunks = new List<ChunkInfo>();
    public string SearchFilter = "";
}

public class NPCData
{
    public List<NPCInfo> NPCs = new List<NPCInfo>();
    public string SearchFilter = "";
    public bool ShowOnlyAlive = false;
    public int FactionFilter = -1;
}

public struct ChunkInfo
{
    public Entity Entity;
    public int2 Id;
    public float2 Position;
    public ChunkState State;
    public int NPCCount;
}

public struct NPCInfo
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
