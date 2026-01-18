using Unity.Entities;

// Компонент для передачи Entity, содержащих буферы с инструкциями для заполнения
public struct NPCBufferFillInstruction : IComponentData
{
    public Entity ScheduleInstructionsBufferEntity;
    public Entity RelationshipsInstructionsBufferEntity;

    // Конструктор для удобства
    public NPCBufferFillInstruction(Entity scheduleInstrEntity, Entity relationshipsInstrEntity)
    {
        ScheduleInstructionsBufferEntity = scheduleInstrEntity;
        RelationshipsInstructionsBufferEntity = relationshipsInstrEntity;
    }
}