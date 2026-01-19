using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

// Система для очистки NPC при выгрузке чанков
// Перемещено в InitializationSystemGroup для согласованности с ChunkManagementSystem
[UpdateInGroup(typeof(InitializationSystemGroup))]
[UpdateAfter(typeof(ChunkManagementSystem))]
public partial struct ChunkNPCCleanupSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var entityManager = state.EntityManager;
        
        // Получаем синглтон для доступа к карте чанков
        var singleton = SystemAPI.GetSingleton<ChunkMapSingleton>();
        var mapDataEntity = singleton.ChunkMapDataEntity;
        var chunkMapBuffer = entityManager.GetBuffer<ChunkMapEntry>(mapDataEntity);

        // Получаем все NPC с компонентом Location
        var npcQuery = SystemAPI.QueryBuilder()
            .WithAll<Location>()
            .Build();

        var npcEntities = npcQuery.ToEntityArray(Allocator.Temp);
        var ecb = new EntityCommandBuffer(Allocator.Temp);

        // Проверяем, есть ли чанки, которые должны быть выгружены
        // Для этого проверим, есть ли NPC, которые находятся в чанках, которые больше не существуют в карте
        
        // Сначала соберем все существующие ChunkId из карты
        var existingChunkIds = new NativeHashSet<int2>(1000, Allocator.Temp);
        foreach (var entry in chunkMapBuffer)
        {
            existingChunkIds.Add(entry.Id);
        }

        // Удаляем NPC, которые находятся в чанках, которых больше нет на карте
        foreach (var npcEntity in npcEntities)
        {
            if (entityManager.HasComponent<Location>(npcEntity))
            {
                var location = entityManager.GetComponentData<Location>(npcEntity);
                
                // Если чанк, в котором находится NPC, больше не существует в карте, удаляем NPC
                if (!existingChunkIds.Contains(location.ChunkId))
                {
                    ecb.DestroyEntity(npcEntity);
                }
            }
        }

        existingChunkIds.Dispose();
        npcEntities.Dispose();
        ecb.Playback(entityManager);
        ecb.Dispose();
    }
}