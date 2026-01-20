using Unity.Burst;
using Unity.Entities;

// Система, фактически добавляющая окончательные компоненты NPC из NPCSpawnData
// и удаляющая NPCSpawnData и NPCBufferEntities
// Читает NPCBufferEntities для доступа к Entity буферов
[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateAfter(typeof(NPCBufferCleanupSystem))]
public partial struct NPCSpawnerSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        var entityManager = state.EntityManager;

        // Получаем все NPC с NPCSpawnData и NPCBufferEntities
        var query = SystemAPI.QueryBuilder()
            .WithAll<NPCSpawnData, NPCBufferEntities>()
            .Build();

        if (query.IsEmpty) return;

        var spawnDataArray = query.ToComponentDataArray<NPCSpawnData>(Unity.Collections.Allocator.Temp);
        var bufferEntitiesArray = query.ToComponentDataArray<NPCBufferEntities>(Unity.Collections.Allocator.Temp);
        var entities = query.ToEntityArray(Unity.Collections.Allocator.Temp);

        for (int i = 0; i < entities.Length; i++)
        {
            var entity = entities[i];
            var data = spawnDataArray[i];
            var buffers = bufferEntitiesArray[i];

            // Добавляем компоненты напрямую
            entityManager.AddComponentData(entity, data.Id);
            entityManager.AddComponentData(entity, data.Name);
            entityManager.AddComponentData(entity, data.Location);
            entityManager.AddComponentData(entity, data.Faction);
            entityManager.AddComponentData(entity, new Schedule(buffers.ScheduleBufferEntity));
            entityManager.AddComponentData(entity, data.Goal);
            entityManager.AddComponentData(entity, data.States);
            entityManager.AddComponentData(entity, data.Traits);
            entityManager.AddComponentData(entity, new Relationships(buffers.RelationshipsBufferEntity));

            // Cleanup
            entityManager.RemoveComponent<NPCSpawnData>(entity);
            entityManager.RemoveComponent<NPCBufferEntities>(entity);
        }

        spawnDataArray.Dispose();
        bufferEntitiesArray.Dispose();
        entities.Dispose();
    }
}
