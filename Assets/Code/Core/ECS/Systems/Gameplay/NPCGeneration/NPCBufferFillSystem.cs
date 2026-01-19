using Unity.Entities;
using Unity.Burst; // Без [BurstCompile]

// Система, заполняющая буферы на основе инструкций из NPCBufferFillInstruction
// Читает NPCBufferEntities для доступа к целевым Entity буферов
// Не удаляет временные Entity или компоненты
// ВАЖНО: Перемещена в SimulationSystemGroup, чтобы ECB из InitializationSystemGroup успел применить структурные изменения
[UpdateInGroup(typeof(SimulationSystemGroup), OrderFirst = true)]
public partial struct NPCBufferFillSystem : ISystem
{
    private ComponentLookup<NPCBufferFillInstruction> _instructionLookup;
    private ComponentLookup<NPCBufferEntities> _bufferEntitiesLookup;
    private BufferLookup<TimeSlot> _timeSlotLookup;
    private BufferLookup<RelationshipEntry> _relationshipLookup;
    
    public void OnCreate(ref SystemState state)
    {
        _instructionLookup = state.GetComponentLookup<NPCBufferFillInstruction>(true);
        _bufferEntitiesLookup = state.GetComponentLookup<NPCBufferEntities>(true);
        _timeSlotLookup = state.GetBufferLookup<TimeSlot>(false);
        _relationshipLookup = state.GetBufferLookup<RelationshipEntry>(false);
    }
    
    public void OnUpdate(ref SystemState state)
    {
        // Обновляем все lookup'ы
        _instructionLookup.Update(ref state);
        _bufferEntitiesLookup.Update(ref state);
        _timeSlotLookup.Update(ref state);
        _relationshipLookup.Update(ref state);
        
        var entityManager = state.EntityManager;
        
        // Получаем все Entity с нужными компонентами через GetAllEntities
        using var allEntities = entityManager.GetAllEntities(Unity.Collections.Allocator.Temp);
        
        foreach (var entity in allEntities)
        {
            // Проверяем есть ли нужные компоненты
            if (!_instructionLookup.HasComponent(entity) || !_bufferEntitiesLookup.HasComponent(entity))
                continue;
                
            var instruction = _instructionLookup[entity];
            var buffers = _bufferEntitiesLookup[entity];

            // Проверяем что все Entity существуют
            if (!entityManager.Exists(instruction.ScheduleInstructionsBufferEntity) ||
                !entityManager.Exists(instruction.RelationshipsInstructionsBufferEntity) ||
                !entityManager.Exists(buffers.ScheduleBufferEntity) ||
                !entityManager.Exists(buffers.RelationshipsBufferEntity))
            {
                continue;
            }

            // Получаем буферы через Lookup (они обновлены в начале OnUpdate)
            var scheduleInstructionBuffer = _timeSlotLookup[instruction.ScheduleInstructionsBufferEntity];
            var relationshipsInstructionBuffer = _relationshipLookup[instruction.RelationshipsInstructionsBufferEntity];
            var scheduleBuffer = _timeSlotLookup[buffers.ScheduleBufferEntity];
            var relationshipsBuffer = _relationshipLookup[buffers.RelationshipsBufferEntity];

            // Заполняем целевые буферы
            for (int i = 0; i < scheduleInstructionBuffer.Length; i++)
            {
                scheduleBuffer.Add(scheduleInstructionBuffer[i]);
            }

            for (int i = 0; i < relationshipsInstructionBuffer.Length; i++)
            {
                relationshipsBuffer.Add(relationshipsInstructionBuffer[i]);
            }
        }
    }
}