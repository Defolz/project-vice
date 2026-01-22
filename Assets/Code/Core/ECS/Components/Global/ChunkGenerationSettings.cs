using Unity.Entities;

/// <summary>
/// Singleton компонент для хранения настроек генерации зданий
/// Создается из ChunkGenerationConfig через ChunkGenerationConfigAuthoring
/// </summary>
public struct ChunkGenerationSettings : IComponentData
{
    public int MinBuildingsPerChunk;
    public int MaxBuildingsPerChunk;
    
    public float MinBuildingSize;
    public float MaxBuildingSize;
    
    public float MinBuildingHeight;
    public float MaxBuildingHeight;
    
    public float ResidentialWeight;
    public float CommercialWeight;
    public float IndustrialWeight;
    public float PublicWeight;
    public float SpecialWeight;
    
    public float MinBuildingSpacing;
    public float EdgeMargin;
    
    public int MaxOccupancy;
    
    /// <summary>
    /// Получить нормализованные веса типов зданий
    /// </summary>
    public void GetNormalizedWeights(
        out float residential, 
        out float commercial,
        out float industrial, 
        out float publicW, 
        out float special)
    {
        float sum = ResidentialWeight + CommercialWeight + IndustrialWeight + 
                   PublicWeight + SpecialWeight;
        
        if (sum <= 0f)
        {
            residential = commercial = industrial = publicW = special = 0.2f;
            return;
        }
        
        residential = ResidentialWeight / sum;
        commercial = CommercialWeight / sum;
        industrial = IndustrialWeight / sum;
        publicW = PublicWeight / sum;
        special = SpecialWeight / sum;
    }
}
