using Unity.Entities;
using Unity.Collections;

// Буферный элемент для хранения одной связи с другим NPC
public struct RelationshipEntry : IBufferElementData
{
    public uint OtherNPCId;        // ID другого NPC (см. NPCId.Value)
    public float Value;           // Значение отношения (-1.0f вражда, 0 нейтрально, 1.0f любовь/лояльность)

    public RelationshipEntry(uint otherNpcId, float value)
    {
        OtherNPCId = otherNpcId;
        Value = value;
    }
}

// Компонент, содержащий список отношений NPC с другими NPC
public struct Relationships : IComponentData
{
    // Ссылка на DynamicBuffer<Entity>, каждый из которых содержит Buffer<RelationshipEntry>
    // Это позволяет избежать вложенных буферов (Buffer<Buffer<T>>, что недопустимо в ECS)
    // Вместо этого: у NPC есть Relationships с Entity, внутри которого Buffer<RelationshipEntry>.
    // Это "indirection entity" или "data container entity".
    public Entity RelationshipsDataEntity; 

    public Relationships(Entity dataEntity)
    {
        RelationshipsDataEntity = dataEntity;
    }
}

// Пример вспомогательной структуры данных (может быть помещена в отдельный файл или в System)
// Используется для хранения самого буфера RelationshipEntry
public struct NPCRelationshipBufferData : IComponentData
{
    // Этот компонент будет добавлен к специальному Entity, 
    // который содержит сам Buffer<RelationshipEntry>.
    // Он сам по себе не является IBufferElementData.
}

// Пример использования (псевдокод для System):
/*
// Создание:
var relationshipBufferEntity = EntityManager.CreateEntity();
EntityManager.AddComponentData(relationshipBufferEntity, new NPCRelationshipBufferData());
var relationshipBuffer = EntityManager.AddBuffer<RelationshipEntry>(relationshipBufferEntity);
relationshipBuffer.Add(new RelationshipEntry(someOtherNpcId, 0.8f));

var npcEntity = EntityManager.CreateEntity();
// ... добавить другие компоненты ...
EntityManager.AddComponentData(npcEntity, new Relationships(relationshipBufferEntity));

// Чтение:
var relationshipsComp = EntityManager.GetComponentData<Relationships>(npcEntity);
var bufferFromEntity = SystemAPI.GetBufferLookup<RelationshipEntry>();
var currentBuffer = bufferFromEntity[relationshipsComp.RelationshipsDataEntity];

foreach(var entry in currentBuffer)
{
    // Работа с entry.OtherNPCId и entry.Value
    if(entry.OtherNPCId == targetNpcId)
    {
        currentRelationshipValue = entry.Value;
        break;
    }
}
*/