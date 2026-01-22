using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;
using UnityEngine;

// ВНИМАНИЕ: Это временный скрипт для тестирования компонентов Core NPC и Chunks.
// Не предназначен для использования в продакшен-коде.
public class NPCTestSpawner : MonoBehaviour
{
    [SerializeField]
    private int testSeed = 12345;

    private World testWorld;
    private EntityManager entityManager;

    void Start()
    {
        testWorld = new World("TestWorld");
        entityManager = testWorld.EntityManager;

        SpawnAndLogTestNPC();
    }

    void OnDestroy()
    {
        if (testWorld != null && testWorld.IsCreated)
        {
            testWorld.Dispose();
        }
    }

    private void SpawnAndLogTestNPC()
    {
        Debug.Log("--- Создание тестового NPC ---");

        Entity npcEntity = entityManager.CreateEntity();

        Entity scheduleBufferEntity = entityManager.CreateEntity();
        entityManager.AddComponentData(scheduleBufferEntity, new NPCRelationshipBufferData());
        var scheduleBuffer = entityManager.AddBuffer<TimeSlot>(scheduleBufferEntity);
        scheduleBuffer.Add(new TimeSlot(9, 11, 1));
        scheduleBuffer.Add(new TimeSlot(12, 13, 2));
        scheduleBuffer.Add(new TimeSlot(18, 22, 3));

        Entity relationshipBufferEntity = entityManager.CreateEntity();
        entityManager.AddComponentData(relationshipBufferEntity, new NPCRelationshipBufferData());
        var relationshipBuffer = entityManager.AddBuffer<RelationshipEntry>(relationshipBufferEntity);
        relationshipBuffer.Add(new RelationshipEntry(67890U, 0.9f));
        relationshipBuffer.Add(new RelationshipEntry(11111U, -0.7f));

        var globalPos = new float2(10.5f, -5.2f);
        var location = Location.FromGlobal(globalPos);

        entityManager.AddComponentData(npcEntity, NPCId.Generate((uint)testSeed));
        
        entityManager.AddComponentData(npcEntity, new NameData(
            new FixedString128Bytes("Джон"), 
            new FixedString128Bytes("Смит"), 
            new FixedString128Bytes("Жирный Джон")
        ));
        
        entityManager.AddComponentData(npcEntity, location);
        entityManager.AddComponentData(npcEntity, new Faction(Faction.Families.Value));
        entityManager.AddComponentData(npcEntity, new Schedule(scheduleBufferEntity));
        entityManager.AddComponentData(npcEntity, new CurrentGoal(GoalType.Work, targetPosition: new float3(15, 0, 0), priority: 0.7f));
        entityManager.AddComponentData(npcEntity, new StateFlags(alive: true, injured: false, wanted: true));
        entityManager.AddComponentData(npcEntity, new Traits(aggression: 0.6f, loyalty: 0.8f, intelligence: 0.5f));
        entityManager.AddComponentData(npcEntity, new Relationships(relationshipBufferEntity));

        Debug.Log($"Создан NPC Entity: {npcEntity.Index}");

        var id = entityManager.GetComponentData<NPCId>(npcEntity);
        var name = entityManager.GetComponentData<NameData>(npcEntity);
        var loc = entityManager.GetComponentData<Location>(npcEntity);
        var faction = entityManager.GetComponentData<Faction>(npcEntity);
        var scheduleComp = entityManager.GetComponentData<Schedule>(npcEntity);
        var goal = entityManager.GetComponentData<CurrentGoal>(npcEntity);
        var states = entityManager.GetComponentData<StateFlags>(npcEntity);
        var traits = entityManager.GetComponentData<Traits>(npcEntity);
        var relationshipsComp = entityManager.GetComponentData<Relationships>(npcEntity);

        Debug.Log($"ID: {id}");
        Debug.Log($"Имя: {name.FirstName} '{name.Nickname}' {name.LastName}");
        Debug.Log($"Позиция (Chunk/Local): {loc}");
        Debug.Log($"Позиция (Global 2D): {loc.GlobalPosition2D}");
        Debug.Log($"Позиция (Global 3D): {loc.GlobalPosition3D}");
        Debug.Log($"Фракция: {faction}");
        Debug.Log($"Цель: {goal}");
        Debug.Log($"Состояния: {states}");
        Debug.Log($"Черты: {traits}");

        var scheduleBufferReader = entityManager.GetBuffer<TimeSlot>(scheduleComp.TimeSlotsBufferEntity);
        Debug.Log($"Расписание:");
        foreach (var slot in scheduleBufferReader)
        {
            Debug.Log($"  - {slot}");
        }

        var relationshipBufferReader = entityManager.GetBuffer<RelationshipEntry>(relationshipsComp.RelationshipsDataEntity);
        Debug.Log($"Отношения:");
        foreach (var rel in relationshipBufferReader)
        {
            Debug.Log($"  - NPC {rel.OtherNPCId}: {rel.Value:F2}");
        }

        Debug.Log("--- Тест завершен ---");
    }
}
