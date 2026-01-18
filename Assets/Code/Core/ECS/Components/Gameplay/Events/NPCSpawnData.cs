using Unity.Entities;
using Unity.Mathematics;

// Временная структура данных для передачи информации о создании NPC
// Добавим поля для готовых Entity буферов
public struct NPCSpawnData : IComponentData
{
    public NPCId Id;
    public NameData Name;
    public Location Location;
    public Faction Faction;
    public Entity ScheduleBufferEntity; // Должен быть создан до добавления компонентов NPC
    public CurrentGoal Goal;
    public StateFlags States;
    public Traits Traits;
    public Entity RelationshipsBufferEntity; // Должен быть создан до добавления компонентов NPC

    // Конструктор для удобства
    public NPCSpawnData(NPCId id, NameData name, Location location, Faction faction, 
                       Entity scheduleEntity, CurrentGoal goal, StateFlags states, Traits traits, Entity relationshipsEntity)
    {
        Id = id;
        Name = name;
        Location = location;
        Faction = faction;
        ScheduleBufferEntity = scheduleEntity;
        Goal = goal;
        States = states;
        Traits = traits;
        RelationshipsBufferEntity = relationshipsEntity;
    }
}