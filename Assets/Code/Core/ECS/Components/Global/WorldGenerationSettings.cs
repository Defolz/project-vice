using Unity.Entities;

/// <summary>
/// ECS компонент для хранения настроек генерации мира
/// Singleton компонент, существует в одном экземпляре
/// </summary>
public struct WorldGenerationSettings : IComponentData
{
    public int ViewDistanceInChunks;
    public int InitialChunksToLoad;
    
    public bool AutoUnloadDistantChunks;
    public int UnloadDistanceInChunks;
    
    public int MaxChunksToLoadPerFrame;
    public int MaxChunksToUnloadPerFrame;
    
    public bool ShowDebugLogs;
    public bool ShowPerformanceWarnings;
    
    /// <summary>
    /// Получить приблизительное количество чанков в области видимости
    /// </summary>
    public int GetApproximateLoadedChunksCount()
    {
        int sideLength = ViewDistanceInChunks * 2 + 1;
        return sideLength * sideLength;
    }
}
