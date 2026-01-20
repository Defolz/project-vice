using Unity.Entities;
using Unity.Mathematics;

// Компонент, хранящий BlobAssetReference на навигационную сетку чанка
public struct NavigationGrid : IComponentData
{
    public BlobAssetReference<GridData> GridBlob;
    public int2 ChunkId;
    
    // Проверка валидности BlobAsset
    public bool IsValid => GridBlob.IsCreated;
}
