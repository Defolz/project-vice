using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Random = Unity.Mathematics.Random;

// Система, создающая Entity для буферов Schedule и Relationships
// и создающая Entity с буферами для инструкций по заполнению
// Использует EntityCommandBuffer для всех структурных изменений
// Не обновляет NPCSpawnData, а создаёт NPCBufferEntities
[UpdateInGroup(typeof(InitializationSystemGroup))] // Или SimulationSystemGroup
[UpdateAfter(typeof(NPCGeneratorSystem))]
public partial struct NPCBufferCreationSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var ecbSingleton = SystemAPI.GetSingletonRW<BeginInitializationEntityCommandBufferSystem.Singleton>();
        var ecb = ecbSingleton.ValueRW.CreateCommandBuffer(state.WorldUnmanaged);
        // Используем динамический сид на основе времени (в миллисекундах) + 1 для избежания нулевого сида
        var seed = (uint)(SystemAPI.Time.ElapsedTime * 1000.0) + 1;
        var random = new Random(seed);

        // Итерируемся только по NPCSpawnData для чтения
        foreach (var (spawnDataRO, npcEntity) in SystemAPI.Query<RefRO<NPCSpawnData>>().WithEntityAccess())
        {
            var originalData = spawnDataRO.ValueRO; // Используем RefRO

            // 1. Создаём Entity для буфера расписания
            var scheduleBufferEntity = ecb.CreateEntity();
            ecb.AddComponent(scheduleBufferEntity, new NPCRelationshipBufferData()); // Заглушка
            // ДОБАВЛЯЕМ БУФЕР
            ecb.AddBuffer<TimeSlot>(scheduleBufferEntity); // <-- ЭТОЙ СТРОКИ НЕ ХВАТАЛО

            // 2. Создаём Entity для буфера отношений
            var relationshipsBufferEntity = ecb.CreateEntity();
            ecb.AddComponent(relationshipsBufferEntity, new NPCRelationshipBufferData()); // Заглушка
            // ДОБАВЛЯЕМ БУФЕР
            ecb.AddBuffer<RelationshipEntry>(relationshipsBufferEntity); // <-- ЭТОЙ СТРОКИ НЕ ХВАТАЛО

            // 3. Создаём Entity для *инструкций* по заполнению буфера расписания
            var scheduleInstructionEntity = ecb.CreateEntity();
            ecb.AddComponent(scheduleInstructionEntity, new NPCRelationshipBufferData()); // Заглушка
            var scheduleInstructionBuffer = ecb.AddBuffer<TimeSlot>(scheduleInstructionEntity);
            scheduleInstructionBuffer.Add(new TimeSlot(9, 11, 1)); // 9-11: Работа
            scheduleInstructionBuffer.Add(new TimeSlot(12, 13, 2)); // 12-13: Обед
            scheduleInstructionBuffer.Add(new TimeSlot(18, 22, 3)); // 18-22: Дома

            // 4. Создаём Entity для *инструкций* по заполнению буфера отношений
            var relationshipsInstructionEntity = ecb.CreateEntity();
            ecb.AddComponent(relationshipsInstructionEntity, new NPCRelationshipBufferData()); // Заглушка
            var relationshipsInstructionBuffer = ecb.AddBuffer<RelationshipEntry>(relationshipsInstructionEntity);
            relationshipsInstructionBuffer.Add(new RelationshipEntry(originalData.Id.Value, random.NextFloat() * 2 - 1)); // Используем ID этого NPC
            relationshipsInstructionBuffer.Add(new RelationshipEntry(random.NextUInt(), random.NextFloat() * 2 - 1)); // Используем NextUInt()

            // 5. Создаём компонент NPCBufferEntities и добавляем его к NPC Entity
            var bufferEntities = new NPCBufferEntities(scheduleBufferEntity, relationshipsBufferEntity);
            ecb.AddComponent(npcEntity, bufferEntities); // <-- Добавляем новый компонент

            // 6. Создаём компонент инструкций для следующей системы, передавая Entity с буферами инструкций
            var fillInstruction = new NPCBufferFillInstruction
            {
                ScheduleInstructionsBufferEntity = scheduleInstructionEntity,
                RelationshipsInstructionsBufferEntity = relationshipsInstructionEntity
            };
            ecb.AddComponent(npcEntity, fillInstruction); // <-- Добавляем инструкции
        }
    }
}