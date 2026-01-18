using Unity.Entities;
using Unity.Burst; // Без [BurstCompile]

// Система, заполняющая буферы на основе инструкций из NPCBufferFillInstruction
// Читает NPCBufferEntities для доступа к целевым Entity буферов
// Не удаляет временные Entity или компоненты
[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateAfter(typeof(NPCBufferCreationSystem))] // Теперь это не обязательно, но можно оставить для ясности
public partial struct NPCBufferFillSystem : ISystem
{
    [BurstCompile] // УБРАНО
    public void OnUpdate(ref SystemState state)
    {
        // var ecb = ... // Не нужен ECB для удаления в этой системе

        foreach (var (fillInstruction, bufferEntities, entity) in SystemAPI.Query<RefRO<NPCBufferFillInstruction>, RefRO<NPCBufferEntities>>().WithEntityAccess())
        {
            var instruction = fillInstruction.ValueRO;
            var buffers = bufferEntities.ValueRO;

            // Получаем буферы с инструкциями
            var scheduleInstructionBuffer = state.EntityManager.GetBuffer<TimeSlot>(instruction.ScheduleInstructionsBufferEntity);
            var relationshipsInstructionBuffer = state.EntityManager.GetBuffer<RelationshipEntry>(instruction.RelationshipsInstructionsBufferEntity);

            // Получаем буферы, которые нужно заполнить
            var scheduleBuffer = state.EntityManager.GetBuffer<TimeSlot>(buffers.ScheduleBufferEntity);
            var relationshipsBuffer = state.EntityManager.GetBuffer<RelationshipEntry>(buffers.RelationshipsBufferEntity);

            // Заполняем целевые буферы из буферов инструкций
            foreach (var slot in scheduleInstructionBuffer)
            {
                scheduleBuffer.Add(slot);
            }

            foreach (var entry in relationshipsInstructionBuffer)
            {
                relationshipsBuffer.Add(entry);
            }

            // НЕ удаляем временные Entity или компоненты здесь
            // ecb.DestroyEntity(instruction.ScheduleInstructionsBufferEntity);
            // ecb.DestroyEntity(instruction.RelationshipsInstructionsBufferEntity);
            // ecb.RemoveComponent<NPCBufferFillInstruction>(entity);
        }
    }
}