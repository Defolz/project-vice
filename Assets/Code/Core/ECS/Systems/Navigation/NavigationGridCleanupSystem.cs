using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

// Система очистки NavigationGrid при выгрузке чанков
// Освобождает BlobAsset для предотвращения утечек памяти
[UpdateInGroup(typeof(ChunkManagementGroup))]
public partial struct NavigationGridCleanupSystem : ISystem
{
    private EntityQuery cleanupQuery;
    
    public void OnCreate(ref SystemState state)
    {
        // Query для entities с NavigationGrid но без Chunk (чанк был удалён)
        cleanupQuery = new EntityQueryBuilder(Allocator.Temp)
            .WithAll<NavigationGrid>()
            .WithNone<Chunk>()
            .Build(ref state);
    }
    
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var entityManager = state.EntityManager;
        
        // Проверяем есть ли entities для cleanup
        if (cleanupQuery.IsEmpty) return;
        
        var grids = cleanupQuery.ToComponentDataArray<NavigationGrid>(Allocator.Temp);
        var entities = cleanupQuery.ToEntityArray(Allocator.Temp);
        
        // Освобождаем BlobAssets и удаляем компоненты НАПРЯМУЮ
        for (int i = 0; i < grids.Length; i++)
        {
            var grid = grids[i];
            
            // Dispose BlobAsset
            if (grid.IsValid)
            {
                grid.GridBlob.Dispose();
            }
            
            // Удаляем NavigationGrid компонент
            entityManager.RemoveComponent<NavigationGrid>(entities[i]);
            
            if (entityManager.HasComponent<NavigationDebugData>(entities[i]))
            {
                entityManager.RemoveComponent<NavigationDebugData>(entities[i]);
            }
        }
        
        grids.Dispose();
        entities.Dispose();
    }
}
