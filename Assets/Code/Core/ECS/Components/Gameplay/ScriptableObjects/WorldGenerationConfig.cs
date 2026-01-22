using UnityEngine;

/// <summary>
/// ScriptableObject для настройки генерации мира (чанков)
/// Управляет тем, сколько чанков загружается вокруг игрока
/// </summary>
[CreateAssetMenu(fileName = "WorldGenerationConfig", menuName = "Project Vice/Configs/World Generation Config")]
public class WorldGenerationConfig : ScriptableObject
{
    [Header("Chunk Loading Settings")]
    [Tooltip("Расстояние видимости в чанках (радиус вокруг игрока)")]
    [Range(1, 20)]
    public int ViewDistanceInChunks = 5;
    
    [Tooltip("Сколько чанков загружать при старте игры (до появления игрока)")]
    [Range(0, 50)]
    public int InitialChunksToLoad = 10;
    
    [Header("Chunk Management")]
    [Tooltip("Автоматически выгружать чанки за пределами видимости")]
    public bool AutoUnloadDistantChunks = true;
    
    [Tooltip("Расстояние в чанках, после которого чанк выгружается (должно быть > ViewDistance)")]
    [Range(1, 30)]
    public int UnloadDistanceInChunks = 7;
    
    [Header("Performance")]
    [Tooltip("Максимальное количество чанков, которые могут быть загружены одновременно за один кадр")]
    [Range(1, 20)]
    public int MaxChunksToLoadPerFrame = 4;
    
    [Tooltip("Максимальное количество чанков, которые могут быть выгружены одновременно за один кадр")]
    [Range(1, 20)]
    public int MaxChunksToUnloadPerFrame = 2;
    
    [Header("Debug")]
    [Tooltip("Показывать логи о загрузке/выгрузке чанков")]
    public bool ShowDebugLogs = true;
    
    [Tooltip("Показывать предупреждения если чанки не успевают загружаться")]
    public bool ShowPerformanceWarnings = true;
    
    /// <summary>
    /// Получить общее количество чанков в области видимости (приблизительно)
    /// </summary>
    public int GetApproximateLoadedChunksCount()
    {
        // Площадь квадрата: (2 * radius + 1)^2
        int sideLength = ViewDistanceInChunks * 2 + 1;
        return sideLength * sideLength;
    }
    
    /// <summary>
    /// Валидация настроек
    /// </summary>
    private void OnValidate()
    {
        // Расстояние выгрузки должно быть больше расстояния видимости
        if (UnloadDistanceInChunks <= ViewDistanceInChunks)
        {
            UnloadDistanceInChunks = ViewDistanceInChunks + 2;
            Debug.LogWarning($"WorldGenerationConfig: UnloadDistance adjusted to {UnloadDistanceInChunks} " +
                           $"(must be > ViewDistance {ViewDistanceInChunks})");
        }
        
        // Начальное количество чанков не должно превышать максимально возможное
        int maxPossible = GetApproximateLoadedChunksCount();
        if (InitialChunksToLoad > maxPossible)
        {
            Debug.LogWarning($"WorldGenerationConfig: InitialChunksToLoad ({InitialChunksToLoad}) " +
                           $"is larger than max possible ({maxPossible}). This is OK but unusual.");
        }
        
        // Проверка производительности
        if (ViewDistanceInChunks > 10)
        {
            Debug.LogWarning($"WorldGenerationConfig: ViewDistance {ViewDistanceInChunks} is quite large. " +
                           $"This will load ~{GetApproximateLoadedChunksCount()} chunks and may impact performance.");
        }
    }
    
    /// <summary>
    /// Получить рекомендованные настройки для разных уровней производительности
    /// </summary>
    public void ApplyPerformancePreset(PerformancePreset preset)
    {
        switch (preset)
        {
            case PerformancePreset.Low:
                ViewDistanceInChunks = 3;
                InitialChunksToLoad = 5;
                MaxChunksToLoadPerFrame = 2;
                MaxChunksToUnloadPerFrame = 1;
                UnloadDistanceInChunks = 5;
                break;
                
            case PerformancePreset.Medium:
                ViewDistanceInChunks = 5;
                InitialChunksToLoad = 10;
                MaxChunksToLoadPerFrame = 4;
                MaxChunksToUnloadPerFrame = 2;
                UnloadDistanceInChunks = 7;
                break;
                
            case PerformancePreset.High:
                ViewDistanceInChunks = 7;
                InitialChunksToLoad = 15;
                MaxChunksToLoadPerFrame = 6;
                MaxChunksToUnloadPerFrame = 3;
                UnloadDistanceInChunks = 10;
                break;
                
            case PerformancePreset.Ultra:
                ViewDistanceInChunks = 10;
                InitialChunksToLoad = 25;
                MaxChunksToLoadPerFrame = 8;
                MaxChunksToUnloadPerFrame = 4;
                UnloadDistanceInChunks = 15;
                break;
        }
        
        Debug.Log($"Applied {preset} performance preset to WorldGenerationConfig");
    }
}

/// <summary>
/// Пресеты производительности для быстрой настройки
/// </summary>
public enum PerformancePreset
{
    Low,    // Для слабых систем - 3 чанка радиус (~49 чанков)
    Medium, // Стандарт - 5 чанков радиус (~121 чанк)
    High,   // Для мощных систем - 7 чанков радиус (~225 чанков)
    Ultra   // Максимум - 10 чанков радиус (~441 чанк)
}
