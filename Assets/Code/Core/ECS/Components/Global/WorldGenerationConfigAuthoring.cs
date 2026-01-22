using Unity.Entities;
using UnityEngine;

/// <summary>
/// MonoBehaviour для авторинга настроек генерации мира
/// Конвертирует ScriptableObject WorldGenerationConfig в ECS компонент WorldGenerationSettings
/// </summary>
public class WorldGenerationConfigAuthoring : MonoBehaviour
{
    [Header("Configuration")]
    [Tooltip("Ссылка на ScriptableObject с настройками генерации мира")]
    public WorldGenerationConfig Config;
    
    [Header("Quick Presets")]
    [Tooltip("Применить пресет производительности при старте (опционально)")]
    public bool ApplyPresetOnStart = false;
    
    [Tooltip("Выберите пресет производительности")]
    public PerformancePreset Preset = PerformancePreset.Medium;
    
    class Baker : Baker<WorldGenerationConfigAuthoring>
    {
        public override void Bake(WorldGenerationConfigAuthoring authoring)
        {
            if (authoring.Config == null)
            {
                Debug.LogError("WorldGenerationConfigAuthoring: Config is null! Please assign a WorldGenerationConfig asset.");
                return;
            }
            
            // Применяем пресет если нужно
            if (authoring.ApplyPresetOnStart)
            {
                authoring.Config.ApplyPerformancePreset(authoring.Preset);
            }
            
            var entity = GetEntity(TransformUsageFlags.None);
            
            // Конвертируем ScriptableObject в ECS компонент
            AddComponent(entity, new WorldGenerationSettings
            {
                ViewDistanceInChunks = authoring.Config.ViewDistanceInChunks,
                InitialChunksToLoad = authoring.Config.InitialChunksToLoad,
                
                AutoUnloadDistantChunks = authoring.Config.AutoUnloadDistantChunks,
                UnloadDistanceInChunks = authoring.Config.UnloadDistanceInChunks,
                
                MaxChunksToLoadPerFrame = authoring.Config.MaxChunksToLoadPerFrame,
                MaxChunksToUnloadPerFrame = authoring.Config.MaxChunksToUnloadPerFrame,
                
                ShowDebugLogs = authoring.Config.ShowDebugLogs,
                ShowPerformanceWarnings = authoring.Config.ShowPerformanceWarnings
            });
            
            if (authoring.Config.ShowDebugLogs)
            {
                Debug.Log($"<color=cyan>WorldGenerationSettings initialized: " +
                         $"ViewDistance={authoring.Config.ViewDistanceInChunks} chunks, " +
                         $"Initial={authoring.Config.InitialChunksToLoad} chunks, " +
                         $"~{authoring.Config.GetApproximateLoadedChunksCount()} total chunks will be loaded</color>");
            }
        }
    }
}
