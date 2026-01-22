using Unity.Burst;
using Unity.Entities;

/// <summary>
/// Система для синхронизации настроек генерации мира с ChunkMapSingleton
/// Обновляет ViewDistanceInChunks в ChunkMapSingleton на основе WorldGenerationSettings
/// </summary>
[UpdateInGroup(typeof(InitializationSystemGroup))]
[UpdateBefore(typeof(ChunkManagementSystem))]
public partial struct WorldGenerationInitSystem : ISystem
{
    private bool _isInitialized;
    
    public void OnCreate(ref SystemState state)
    {
        _isInitialized = false;
    }
    
    public void OnUpdate(ref SystemState state)
    {
        // Выполняем только один раз при наличии настроек
        if (_isInitialized)
            return;
        
        if (!SystemAPI.HasSingleton<WorldGenerationSettings>())
            return;
        
        if (!SystemAPI.HasSingleton<ChunkMapSingleton>())
            return;
        
        var settings = SystemAPI.GetSingleton<WorldGenerationSettings>();
        var singleton = SystemAPI.GetSingleton<ChunkMapSingleton>();
        
        // Обновляем ViewDistance в singleton
        singleton.ViewDistanceInChunks = settings.ViewDistanceInChunks;
        SystemAPI.SetSingleton(singleton);
        
        if (settings.ShowDebugLogs)
        {
            UnityEngine.Debug.Log($"<color=cyan>WorldGeneration initialized: " +
                                 $"ViewDistance={settings.ViewDistanceInChunks} chunks, " +
                                 $"~{settings.GetApproximateLoadedChunksCount()} chunks will be loaded</color>");
        }
        
        _isInitialized = true;
    }
}
