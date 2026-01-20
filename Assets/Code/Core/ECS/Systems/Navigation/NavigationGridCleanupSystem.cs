using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

// Система очистки NavigationGrid при выгрузке чанков
// Освобождает BlobAsset для предотвращения утечек памяти
[UpdateInGroup(typeof(ChunkManagementGroup))]
public partial struct NavigationGridCleanupSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var entityManager = state.EntityManager;
        
        // Query для чанков, которые помечены для выгрузки и имеют NavigationGrid
        var unloadingQuery = SystemAPI.QueryBuilder()
            .WithAll<NavigationGrid>()
            .WithNone<Chunk>() // Чанки без Chunk компонента = уже удалены
            .Build();
        
        var grids = unloadingQuery.ToComponentDataArray<NavigationGrid>(Allocator.Temp);
        var gridEntities = unloadingQuery.ToEntityArray(Allocator.Temp);
        
        for (int i = 0; i < grids.Length; i++)
        {
            var grid = grids[i];
            
            // Освобождаем BlobAsset
            if (grid.IsValid)
            {
                grid.Dispose();
            }
        }
        
        grids.Dispose();
        gridEntities.Dispose();
    }
}
