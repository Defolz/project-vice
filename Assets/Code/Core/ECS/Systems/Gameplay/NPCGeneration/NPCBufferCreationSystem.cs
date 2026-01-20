using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Random = Unity.Mathematics.Random;

// Система, создающая Entity для буферов Schedule и Relationships
// и создающая Entity с буферами для инструкций по заполнению
// НЕ использует ECB - создает все напрямую для избежания deferred entities
[UpdateInGroup(typeof(InitializationSystemGroup))]
[UpdateAfter(typeof(NPCGeneratorSystem))]
public partial struct NPCBufferCreationSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        var entityManager = state.EntityManager;
        var seed = (uint)(SystemAPI.Time.ElapsedTime * 1000.0) + 1;
        var random = new Random(seed);

        // Получаем все NPC с NPCSpawnData
        var query = SystemAPI.QueryBuilder()
            .WithAll<NPCSpawnData>()
            .WithNone<NPCBufferEntities>()
            .Build();

        if (query.IsEmpty) return;

        var entities = query.ToEntityArray(Allocator.Temp);

        foreach (var npcEntity in entities)
        {
            var originalData = entityManager.GetComponentData<NPCSpawnData>(npcEntity);

            // 1. Создаём Entity для буфера расписания НАПРЯМУЮ
            var scheduleBufferEntity = entityManager.CreateEntity();
            entityManager.AddComponentData(scheduleBufferEntity, new NPCRelationshipBufferData());
            var scheduleBuffer = entityManager.AddBuffer<TimeSlot>(scheduleBufferEntity);

            // 2. Создаём Entity для буфера отношений НАПРЯМУЮ
            var relationshipsBufferEntity = entityManager.CreateEntity();
            entityManager.AddComponentData(relationshipsBufferEntity, new NPCRelationshipBufferData());
            var relationshipsBuffer = entityManager.AddBuffer<RelationshipEntry>(relationshipsBufferEntity);

            // 3. Создаём Entity для инструкций расписания НАПРЯМУЮ
            var scheduleInstructionEntity = entityManager.CreateEntity();
            entityManager.AddComponentData(scheduleInstructionEntity, new NPCRelationshipBufferData());
            var scheduleInstructionBuffer = entityManager.AddBuffer<TimeSlot>(scheduleInstructionEntity);
            scheduleInstructionBuffer.Add(new TimeSlot(9, 11, 1));
            scheduleInstructionBuffer.Add(new TimeSlot(12, 13, 2));
            scheduleInstructionBuffer.Add(new TimeSlot(18, 22, 3));

            // 4. Создаём Entity для инструкций отношений НАПРЯМУЮ
            var relationshipsInstructionEntity = entityManager.CreateEntity();
            entityManager.AddComponentData(relationshipsInstructionEntity, new NPCRelationshipBufferData());
            var relationshipsInstructionBuffer = entityManager.AddBuffer<RelationshipEntry>(relationshipsInstructionEntity);
            relationshipsInstructionBuffer.Add(new RelationshipEntry(originalData.Id.Value, random.NextFloat() * 2 - 1));
            relationshipsInstructionBuffer.Add(new RelationshipEntry(random.NextUInt(), random.NextFloat() * 2 - 1));

            // 5. Добавляем NPCBufferEntities к NPC
            entityManager.AddComponentData(npcEntity, new NPCBufferEntities(scheduleBufferEntity, relationshipsBufferEntity));

            // 6. Добавляем инструкции
            entityManager.AddComponentData(npcEntity, new NPCBufferFillInstruction
            {
                ScheduleInstructionsBufferEntity = scheduleInstructionEntity,
                RelationshipsInstructionsBufferEntity = relationshipsInstructionEntity
            });
        }

        entities.Dispose();
    }
}
