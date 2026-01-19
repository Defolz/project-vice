using Unity.Burst;
using Unity.Entities;

// Система, фактически добавляющая окончательные компоненты NPC из NPCSpawnData
// и удаляющая NPCSpawnData и NPCBufferEntities
// Читает NPCBufferEntities для доступа к Entity буферов
// Перемещено в SimulationSystemGroup, запускается после очистки инструкций
[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateAfter(typeof(NPCBufferCleanupSystem))]
public partial struct NPCSpawnerSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var ecb = SystemAPI
            .GetSingletonRW<EndInitializationEntityCommandBufferSystem.Singleton>()
            .ValueRW
            .CreateCommandBuffer(state.WorldUnmanaged);

        foreach (var (spawnDataRO, bufferEntitiesRO, entity)
                in SystemAPI.Query<RefRO<NPCSpawnData>, RefRO<NPCBufferEntities>>()
                            .WithEntityAccess())
        {
            var data = spawnDataRO.ValueRO;
            var buffers = bufferEntitiesRO.ValueRO;

            // ✅ ВСЁ через ECB
            ecb.AddComponent(entity, data.Id);
            ecb.AddComponent(entity, data.Name);
            ecb.AddComponent(entity, data.Location);
            ecb.AddComponent(entity, data.Faction);
            ecb.AddComponent(entity, new Schedule(buffers.ScheduleBufferEntity));
            ecb.AddComponent(entity, data.Goal);
            ecb.AddComponent(entity, data.States);
            ecb.AddComponent(entity, data.Traits);
            ecb.AddComponent(entity, new Relationships(buffers.RelationshipsBufferEntity));

            // cleanup
            ecb.RemoveComponent<NPCSpawnData>(entity);
            ecb.RemoveComponent<NPCBufferEntities>(entity);
        }
    }

}