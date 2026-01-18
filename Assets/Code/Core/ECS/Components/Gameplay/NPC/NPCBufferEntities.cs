using Unity.Entities;

// Компонент для хранения Entity буферов, созданных для NPC
public struct NPCBufferEntities : IComponentData
{
    public Entity ScheduleBufferEntity;
    public Entity RelationshipsBufferEntity;

    public NPCBufferEntities(Entity scheduleEntity, Entity relationshipsEntity)
    {
        ScheduleBufferEntity = scheduleEntity;
        RelationshipsBufferEntity = relationshipsEntity;
    }
}