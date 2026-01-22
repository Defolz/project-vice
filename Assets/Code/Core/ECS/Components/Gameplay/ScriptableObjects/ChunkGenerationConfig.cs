using UnityEngine;

/// <summary>
/// ScriptableObject для настройки генерации зданий и объектов в чанках
/// </summary>
[CreateAssetMenu(fileName = "ChunkGenerationConfig", menuName = "Project Vice/Configs/Chunk Generation Config")]
public class ChunkGenerationConfig : ScriptableObject
{
    [Header("Building Generation Settings")]
    [Tooltip("Минимальное количество зданий в чанке")]
    [Range(0, 20)]
    public int MinBuildingsPerChunk = 2;
    
    [Tooltip("Максимальное количество зданий в чанке")]
    [Range(0, 50)]
    public int MaxBuildingsPerChunk = 8;
    
    [Header("Building Size Settings")]
    [Tooltip("Минимальный размер здания (ширина/длина в метрах)")]
    [Range(5f, 30f)]
    public float MinBuildingSize = 8f;
    
    [Tooltip("Максимальный размер здания (ширина/длина в метрах)")]
    [Range(10f, 50f)]
    public float MaxBuildingSize = 25f;
    
    [Tooltip("Минимальная высота здания")]
    [Range(3f, 10f)]
    public float MinBuildingHeight = 4f;
    
    [Tooltip("Максимальная высота здания")]
    [Range(5f, 100f)]
    public float MaxBuildingHeight = 30f;
    
    [Header("Building Type Weights")]
    [Tooltip("Вероятность генерации жилых зданий (Residential)")]
    [Range(0f, 1f)]
    public float ResidentialWeight = 0.4f;
    
    [Tooltip("Вероятность генерации коммерческих зданий (Commercial - магазины, рестораны)")]
    [Range(0f, 1f)]
    public float CommercialWeight = 0.3f;
    
    [Tooltip("Вероятность генерации промышленных зданий (Industrial - склады, производство)")]
    [Range(0f, 1f)]
    public float IndustrialWeight = 0.15f;
    
    [Tooltip("Вероятность генерации общественных зданий (Public - полиция, больницы)")]
    [Range(0f, 1f)]
    public float PublicWeight = 0.1f;
    
    [Tooltip("Вероятность генерации специальных зданий (Special - уникальные локации)")]
    [Range(0f, 1f)]
    public float SpecialWeight = 0.05f;
    
    [Header("Spacing Settings")]
    [Tooltip("Минимальное расстояние между зданиями в метрах")]
    [Range(2f, 20f)]
    public float MinBuildingSpacing = 5f;
    
    [Tooltip("Минимальное расстояние от границы чанка")]
    [Range(0f, 20f)]
    public float EdgeMargin = 5f;
    
    [Header("Activity Settings")]
    [Tooltip("Максимальное количество NPC, которые могут одновременно находиться в здании")]
    [Range(1, 50)]
    public int MaxOccupancy = 10;
    
    /// <summary>
    /// Получить нормализованные веса типов зданий (сумма = 1.0)
    /// </summary>
    public void GetNormalizedWeights(out float residential, out float commercial, 
                                      out float industrial, out float publicW, out float special)
    {
        float sum = ResidentialWeight + CommercialWeight + IndustrialWeight + PublicWeight + SpecialWeight;
        
        if (sum <= 0f)
        {
            // Fallback на равномерное распределение
            residential = commercial = industrial = publicW = special = 0.2f;
            return;
        }
        
        residential = ResidentialWeight / sum;
        commercial = CommercialWeight / sum;
        industrial = IndustrialWeight / sum;
        publicW = PublicWeight / sum;
        special = SpecialWeight / sum;
    }
    
    /// <summary>
    /// Валидирует настройки конфига
    /// </summary>
    private void OnValidate()
    {
        // Убеждаемся что минимум не больше максимума
        if (MinBuildingsPerChunk > MaxBuildingsPerChunk)
            MinBuildingsPerChunk = MaxBuildingsPerChunk;
        
        if (MinBuildingSize > MaxBuildingSize)
            MinBuildingSize = MaxBuildingSize;
        
        if (MinBuildingHeight > MaxBuildingHeight)
            MinBuildingHeight = MaxBuildingHeight;
        
        // Проверка что есть хотя бы один ненулевой вес
        float totalWeight = ResidentialWeight + CommercialWeight + IndustrialWeight + 
                           PublicWeight + SpecialWeight;
        
        if (totalWeight <= 0f)
        {
            Debug.LogWarning("ChunkGenerationConfig: All building type weights are zero! " +
                           "Setting default weights.");
            ResidentialWeight = 0.4f;
            CommercialWeight = 0.3f;
            IndustrialWeight = 0.15f;
            PublicWeight = 0.1f;
            SpecialWeight = 0.05f;
        }
    }
}
