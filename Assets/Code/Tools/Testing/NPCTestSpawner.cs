using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;
using UnityEngine;

// ВНИМАНИЕ: Это временный скрипт для тестирования компонентов Core NPC и Chunks.
// Не предназначен для использования в продакшен-коде.
public class NPCTestSpawner : MonoBehaviour
{
    [SerializeField]
    private int testSeed = 12345; // Сид для генерации тестового NPC

    private World testWorld; // Наш изолированный World
    private EntityManager entityManager;

    void Start()
    {
        // Создаем изолированный World для теста
        testWorld = new World("TestWorld");
        entityManager = testWorld.EntityManager;

        SpawnAndLogTestNPC();
    }

    void OnDestroy()
    {
        // Обязательно удаляем тестовый World при уничтожении объекта
        // Проверяем, что World существует и неDisposed
        if (testWorld != null && testWorld.IsCreated)
        {
            testWorld.Dispose();
        }
        // Если testWorld == null или !testWorld.IsCreated, Dispose() не вызывается
    }

    private void SpawnAndLogTestNPC()
    {
        Debug.Log("--- Создание тестового NPC ---");

        // 1. Создаем Entity для NPC
        Entity npcEntity = entityManager.CreateEntity();

        // 2. Создаем Entity для буфера расписания
        Entity scheduleBufferEntity = entityManager.CreateEntity();
        entityManager.AddComponentData(scheduleBufferEntity, new NPCRelationshipBufferData()); // Компонент-заглушка для сущности с буфером
        var scheduleBuffer = entityManager.AddBuffer<TimeSlot>(scheduleBufferEntity);
        // Заполняем буфер тестовыми данными
        scheduleBuffer.Add(new TimeSlot(9, 11, 1)); // 9-11: Работа
        scheduleBuffer.Add(new TimeSlot(12, 13, 2)); // 12-13: Обед
        scheduleBuffer.Add(new TimeSlot(18, 22, 3)); // 18-22: Дома

        // 3. Создаем Entity для буфера отношений
        Entity relationshipBufferEntity = entityManager.CreateEntity();
        entityManager.AddComponentData(relationshipBufferEntity, new NPCRelationshipBufferData()); // Компонент-заглушка
        var relationshipBuffer = entityManager.AddBuffer<RelationshipEntry>(relationshipBufferEntity);
        // Заполняем буфер тестовыми данными
        relationshipBuffer.Add(new RelationshipEntry(67890U, 0.9f)); // Дружба с NPC ID 67890 (uint)
        relationshipBuffer.Add(new RelationshipEntry(11111U, -0.7f)); // Вражда с NPC ID 11111 (uint)


        // 3. Добавляем все компоненты Core NPC Template
        // ИСПОЛЬЗУЕМ ОБНОВЛЁННЫЙ Location с X-Y
        var globalPos = new float2(10.5f, -5.2f); // 2D позиция
        var location = Location.FromGlobal(globalPos);

        // ИСПРАВЛЕНО: передаём uint в NPCId.Generate
        entityManager.AddComponentData(npcEntity, NPCId.Generate((uint)testSeed)); // Приводим int seed к uint
        
        // ИСПРАВЛЕНО: передаём FixedString32Bytes в NameData
        entityManager.AddComponentData(npcEntity, new NameData(
            new FixedString128Bytes("Джон"), 
            new FixedString128Bytes("Смит"), 
            new FixedString128Bytes("Жирный Джон")
        ));
        
        entityManager.AddComponentData(npcEntity, location); // <-- Используем обновлённый Location
        entityManager.AddComponentData(npcEntity, new Faction(Faction.Families.Value)); // Присоединяем к Families
        entityManager.AddComponentData(npcEntity, new Schedule(scheduleBufferEntity)); // Передаем Entity с буфером
        // ИСПРАВЛЕНО: используем float3 для CurrentGoal
        entityManager.AddComponentData(npcEntity, new CurrentGoal(GoalType.Work, targetPosition: new float3(15, 0, 0), priority: 0.7f)); // Используем float3
        entityManager.AddComponentData(npcEntity, new StateFlags(alive: true, injured: false, wanted: true));
        entityManager.AddComponentData(npcEntity, new Traits(aggression: 0.6f, loyalty: 0.8f, intelligence: 0.5f));
        entityManager.AddComponentData(npcEntity, new Relationships(relationshipBufferEntity)); // Передаем Entity с буфером


        Debug.Log($"Создан NPC Entity: {npcEntity.Index}");

        // 4. Читаем и логируем данные
        var id = entityManager.GetComponentData<NPCId>(npcEntity);
        var name = entityManager.GetComponentData<NameData>(npcEntity);
        var loc = entityManager.GetComponentData<Location>(npcEntity); // <-- Читаем обновлённый Location
        var faction = entityManager.GetComponentData<Faction>(npcEntity);
        var scheduleComp = entityManager.GetComponentData<Schedule>(npcEntity);
        var goal = entityManager.GetComponentData<CurrentGoal>(npcEntity);
        var states = entityManager.GetComponentData<StateFlags>(npcEntity);
        var traits = entityManager.GetComponentData<Traits>(npcEntity);
        var relationshipsComp = entityManager.GetComponentData<Relationships>(npcEntity);

        Debug.Log($"ID: {id}");
        // ИСПРАВЛЕНО: выводим поля NameData, а не сам объект
        Debug.Log($"Имя: {name.FirstName} '{name.Nickname}' {name.LastName}");
        Debug.Log($"Позиция (Chunk/Local): {loc}"); // <-- Выводит ChunkId и LocalPos
        Debug.Log($"Позиция (Global 2D): {loc.GlobalPosition2D}"); // <-- Выводит вычисленную GlobalPos 2D
        Debug.Log($"Позиция (Global 3D): {loc.GlobalPosition3D}"); // <-- Выводит 3D (X, Y, 0)
        Debug.Log($"Фракция: {faction}");
        Debug.Log($"Цель: {goal}");
        Debug.Log($"Состояния: {states}");
        Debug.Log($"Черты: {traits}");

        // Чтение буфера расписания
        var scheduleBufferReader = entityManager.GetBuffer<TimeSlot>(scheduleComp.TimeSlotsBufferEntity);
        Debug.Log($"Расписание:");
        foreach (var slot in scheduleBufferReader)
        {
            Debug.Log($"  - {slot}");
        }

        // Чтение буфера отношений
        var relationshipBufferReader = entityManager.GetBuffer<RelationshipEntry>(relationshipsComp.RelationshipsDataEntity);
        Debug.Log($"Отношения:");
        foreach (var rel in relationshipBufferReader)
        {
            Debug.Log($"  - NPC {rel.OtherNPCId}: {rel.Value:F2}"); // OtherNPCId теперь uint
        }


        Debug.Log("--- Тест завершен ---");
    }
}