using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Random = Unity.Mathematics.Random;

// Система, определяющая, где и сколько NPC нужно создать
// Создаёт только Entity NPC с NPCSpawnData
[UpdateInGroup(typeof(InitializationSystemGroup))] // Или SimulationSystemGroup, в зависимости от логики спавна
public partial struct NPCGeneratorSystem : ISystem
{
    // Ссылка на конфиг (для простоты, можно загружать через Resource, Addressables или передавать через Singleton)
    // Пока используем статический класс или заглушку
    private static readonly float NPC_DENSITY_PER_CHUNK = 2.0f; // Используем значение из Config
    private static readonly int MAX_NPC_PER_CHUNK = 5; // Используем значение из Config

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var entityManager = state.EntityManager;
        var random = new Random(12345); // Используем фиксированный сид для детерминизма или передаём сид извне

        // 1. Получаем синглтон ChunkMap (или другую структуру, отслеживающую загруженные чанки)
        // Пока используем существующий singleton из ChunkManagementSystem
        // !! ВАЖНО !!: Эта система должна запускаться *после* ChunkManagementSystem, чтобы данные были готовы
        // Также нужно убедиться, что Buffer<ChunkMapEntry> содержит актуальные данные
        var chunkMapSingleton = SystemAPI.GetSingleton<ChunkMapSingleton>();
        var chunkMapBuffer = entityManager.GetBuffer<ChunkMapEntry>(chunkMapSingleton.ChunkMapDataEntity);

        // 2. Определяем, какие чанки загружены и подходят для спавна
        var loadedChunks = new NativeList<int2>(Allocator.Temp);
        foreach (var entry in chunkMapBuffer)
        {
            if (entry.State == ChunkState.Loaded)
            {
                loadedChunks.Add(entry.Id);
            }
        }

        // 3. Для каждого загруженного чанка решаем, сколько NPC в нём создать
        var spawnRequests = new NativeList<NPCSpawnData>(Allocator.Temp);
        foreach (var chunkId in loadedChunks)
        {
            // Простая логика: создать N NPC в чанке, где N зависит от плотности
            var numNPCsInChunk = (int)math.round(random.NextFloat() * NPC_DENSITY_PER_CHUNK);
            numNPCsInChunk = math.min(numNPCsInChunk, MAX_NPC_PER_CHUNK); // Ограничение

            for (int i = 0; i < numNPCsInChunk; i++)
            {
                // 4. Генерируем данные для одного NPC (без Entity буферов)
                var npcData = GenerateNPCData(ref random, chunkId, entityManager);
                spawnRequests.Add(npcData);
            }
        }

        // 5. Отправляем запросы на спавн (например, через EntityCommandBuffer)
        var ecb = new EntityCommandBuffer(Allocator.Temp);
        foreach (var spawnData in spawnRequests)
        {
            var npcEntity = ecb.CreateEntity();
            ecb.AddComponent(npcEntity, spawnData); // Добавляем весь набор данных как IComponentData
            // Компоненты будут установлены в NPCBufferCreationSystem или NPCSpawnerSystem
        }
        ecb.Playback(entityManager);
        ecb.Dispose();

        loadedChunks.Dispose();
        spawnRequests.Dispose();
    }

    // Вспомогательная функция для генерации данных NPC
    // Возвращает NPCSpawnData с null Entity для буферов
    private NPCSpawnData GenerateNPCData(ref Random random, int2 chunkId, EntityManager entityManager)
    {
        // Генерация ID - используем uint напрямую
        var id = NPCId.Generate(random.NextUInt()); // Прямое использование uint из random

        // Генерация имени (упрощённо) - теперь используем FixedString128Bytes и FixedString.Format
        // ИСПРАВЛЕНО: используем FixedString.Format для формирования имени
        var firstName = FixedString.Format("John_{0}", id.Value); // Возвращает FixedString128Bytes
        var lastName = FixedString.Format("Smith_{0}", id.Value % 1000U); // % на uint требует uint литерал
        var nickname = FixedString.Format("NPC_{0}", id.Value % 100U); // %

        // ИСПРАВЛЕНО: передаём FixedString128Bytes напрямую (они совпадают с типами в NameData)
        var name = new NameData(firstName, lastName, nickname);

        // Генерация Location (случайная позиция внутри чанка)
        var localPos = new float2(
            random.NextFloat() * ChunkConstants.CHUNK_SIZE,
            random.NextFloat() * ChunkConstants.CHUNK_SIZE
        );
        var location = new Location(chunkId, localPos);

        // Генерация Faction (веса из Config)
        var totalWeight = 0.3f + 0.2f + 0.5f; // Families + Police + Civilians
        var randFrac = random.NextFloat();
        Faction faction;
        if (randFrac < (0.3f / totalWeight))
            faction = new Faction(Faction.Families.Value);
        else if (randFrac < ((0.3f + 0.2f) / totalWeight))
            faction = new Faction(Faction.Police.Value);
        else
            faction = new Faction(Faction.Civilians.Value);

        // Генерация Goal (упрощённо)
        // Требуется float3 для targetPosition
        var goal = new CurrentGoal(GoalType.Idle, targetPosition: new float3(location.GlobalPosition2D.x, location.GlobalPosition2D.y, 0), priority: 0.5f);

        // Генерация States (начальное состояние)
        var states = new StateFlags(alive: true, injured: false, wanted: false);

        // Генерация Traits (в пределах диапазонов Config)
        var traits = new Traits(
            aggression: random.NextFloat() * (0.9f - 0.1f) + 0.1f, // Используем Min/Max из Config
            loyalty: random.NextFloat(),
            intelligence: random.NextFloat()
        );

        // ВОЗВРАЩАЕМ NPCSpawnData с null Entity для буферов
        // Их создаст NPCBufferCreationSystem
        return new NPCSpawnData(id, name, location, faction, Entity.Null, goal, states, traits, Entity.Null);
    }
}