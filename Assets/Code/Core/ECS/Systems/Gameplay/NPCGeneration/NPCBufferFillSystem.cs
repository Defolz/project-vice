using Unity.Entities;
using Unity.Burst; // Без [BurstCompile]

// Система, заполняющая буферы на основе инструкций из NPCBufferFillInstruction
// Читает NPCBufferEntities для доступа к целевым Entity буферов
// Не удаляет временные Entity или компоненты
// Перемещено в InitializationSystemGroup для согласованности с NPCBufferCreationSystem
[UpdateInGroup(typeof(InitializationSystemGroup))]
[UpdateAfter(typeof(NPCBufferCreationSystem))]
public partial struct NPCBufferFillSystem : ISystem
{
    private BufferLookup<TimeSlot> _timeSlotLookup;
    private BufferLookup<RelationshipEntry> _relationshipLookup;

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        _timeSlotLookup = state.GetBufferLookup<TimeSlot>(false);
        _relationshipLookup = state.GetBufferLookup<RelationshipEntry>(false);
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        // Обновляем lookup'ы перед использованием
        _timeSlotLookup.Update(ref state);
        _relationshipLookup.Update(ref state);

        foreach (var (fillInstruction, bufferEntities, entity) in SystemAPI.Query<RefRO<NPCBufferFillInstruction>, RefRO<NPCBufferEntities>>().WithEntityAccess())
        {
            var instruction = fillInstruction.ValueRO;
            var buffers = bufferEntities.ValueRO;

            // Получаем буферы с инструкциями через lookup
            var scheduleInstructionBuffer = _timeSlotLookup[instruction.ScheduleInstructionsBufferEntity];
            var relationshipsInstructionBuffer = _relationshipLookup[instruction.RelationshipsInstructionsBufferEntity];

            // Получаем буферы, которые нужно заполнить
            var scheduleBuffer = _timeSlotLookup[buffers.ScheduleBufferEntity];
            var relationshipsBuffer = _relationshipLookup[buffers.RelationshipsBufferEntity];

            // Заполняем целевые буферы из буферов инструкций
            foreach (var slot in scheduleInstructionBuffer)
            {
                scheduleBuffer.Add(slot);
            }

            foreach (var entry in relationshipsInstructionBuffer)
            {
                relationshipsBuffer.Add(entry);
            }
        }
    }
}