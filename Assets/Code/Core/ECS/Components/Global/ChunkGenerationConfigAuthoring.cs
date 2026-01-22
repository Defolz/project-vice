using Unity.Entities;
using UnityEngine;

/// <summary>
/// MonoBehaviour для авторинга настроек генерации зданий в чанках
/// Конвертирует ScriptableObject ChunkGenerationConfig в ECS компонент ChunkGenerationSettings
/// </summary>
public class ChunkGenerationConfigAuthoring : MonoBehaviour
{
    [Header("Configuration")]
    [Tooltip("Ссылка на ScriptableObject с настройками генерации зданий")]
    public ChunkGenerationConfig Config;
    
    class Baker : Baker<ChunkGenerationConfigAuthoring>
    {
        public override void Bake(ChunkGenerationConfigAuthoring authoring)
        {
            if (authoring.Config == null)
            {
                Debug.LogError("ChunkGenerationConfigAuthoring: Config is null! Please assign a ChunkGenerationConfig asset.");
                return;
            }
            
            var entity = GetEntity(TransformUsageFlags.None);
            
            // Конвертируем ScriptableObject в ECS компонент
            AddComponent(entity, new ChunkGenerationSettings
            {
                MinBuildingsPerChunk = authoring.Config.MinBuildingsPerChunk,
                MaxBuildingsPerChunk = authoring.Config.MaxBuildingsPerChunk,
                
                MinBuildingSize = authoring.Config.MinBuildingSize,
                MaxBuildingSize = authoring.Config.MaxBuildingSize,
                
                MinBuildingHeight = authoring.Config.MinBuildingHeight,
                MaxBuildingHeight = authoring.Config.MaxBuildingHeight,
                
                ResidentialWeight = authoring.Config.ResidentialWeight,
                CommercialWeight = authoring.Config.CommercialWeight,
                IndustrialWeight = authoring.Config.IndustrialWeight,
                PublicWeight = authoring.Config.PublicWeight,
                SpecialWeight = authoring.Config.SpecialWeight,
                
                MinBuildingSpacing = authoring.Config.MinBuildingSpacing,
                EdgeMargin = authoring.Config.EdgeMargin,
                
                MaxOccupancy = authoring.Config.MaxOccupancy
            });
        }
    }
}
