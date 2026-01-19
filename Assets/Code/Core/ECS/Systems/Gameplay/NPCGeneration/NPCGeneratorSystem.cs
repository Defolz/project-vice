using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Random = Unity.Mathematics.Random;

// Система, определяющая, где и сколько NPC нужно создать
// Создаёт только Entity NPC с NPCSpawnData
// Использует настройки из NPCGenerationSettings синглтона
[UpdateInGroup(typeof(InitializationSystemGroup))]
public partial struct NPCGeneratorSystem : ISystem
{

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var entityManager = state.EntityManager;
        // Используем динамический сид на основе времени (в миллисекундах) + 1 для избежания нулевого сида
        var seed = (uint)(SystemAPI.Time.ElapsedTime * 1000.0) + 1;
        var random = new Random(seed);

        // 0. Получаем настройки генерации
        if (!SystemAPI.TryGetSingleton<NPCGenerationSettings>(out var settings))
        {
            // Если настройки не найдены, используем значения по умолчанию
            settings = NPCGenerationSettings.Default;
        }

        // 1. Получаем синглтон ChunkMap с проверкой наличия
        if (!SystemAPI.TryGetSingleton<ChunkMapSingleton>(out var chunkMapSingleton))
        {
            // ChunkMapSingleton еще не создан, пропускаем этот кадр
            return;
        }

        // Проверяем, что ChunkMapDataEntity валиден
        if (!entityManager.Exists(chunkMapSingleton.ChunkMapDataEntity) || 
            !entityManager.HasBuffer<ChunkMapEntry>(chunkMapSingleton.ChunkMapDataEntity))
        {
            // Entity или буфер не существует, пропускаем
            return;
        }

        var chunkMapBuffer = entityManager.GetBuffer<ChunkMapEntry>(chunkMapSingleton.ChunkMapDataEntity);

        // 2. Определяем, какие чанки загружены
        var loadedChunks = new NativeList<int2>(Allocator.Temp);
        foreach (var entry in chunkMapBuffer)
        {
            if (entry.State == ChunkState.Loaded)
            {
                loadedChunks.Add(entry.Id);
            }
        }

        // 3. Подсчитываем текущее количество NPC в каждом загруженном чанке
        var currentNPCCounts = new NativeParallelHashMap<int2, int>(loadedChunks.Length, Allocator.Temp);
        
        // Инициализируем счетчики
        foreach (var chunkId in loadedChunks)
        {
            currentNPCCounts[chunkId] = 0;
        }

        // Подсчитываем существующих NPC (только те, которые уже полностью созданы)
        // Используем SystemAPI.Query для получения всех NPC с Location, но без NPCSpawnData
        foreach (var (location, entity) in SystemAPI.Query<RefRO<Location>>().WithEntityAccess())
        {
            // Пропускаем NPC, которые еще в процессе спавна (имеют NPCSpawnData)
            if (!SystemAPI.HasComponent<NPCSpawnData>(entity))
            {
                if (currentNPCCounts.ContainsKey(location.ValueRO.ChunkId))
                {
                    currentNPCCounts[location.ValueRO.ChunkId]++;
                }
            }
        }

        // 4. Для каждого загруженного чанка решаем, сколько NPC в нём создать (только недостающих)
        var spawnRequests = new NativeList<NPCSpawnData>(Allocator.Temp);
        foreach (var chunkId in loadedChunks)
        {
            // Подсчитываем, сколько NPC уже есть в чанке
            var currentCount = currentNPCCounts.TryGetValue(chunkId, out var count) ? count : 0;
            
            // Определяем сколько NPC должно быть в чанке (используем настройки)
            var desiredCount = (int)math.round(settings.AverageNPCPerChunk);
            desiredCount = math.min(desiredCount, settings.MaxNPCPerChunk);
            
            // Создаем только недостающих NPC
            var numNPCsToCreate = math.max(0, desiredCount - currentCount);
            
            for (int i = 0; i < numNPCsToCreate; i++)
            {
                // 5. Генерируем данные для одного NPC (без Entity буферов)
                var npcData = GenerateNPCData(ref random, chunkId, entityManager, settings);
                spawnRequests.Add(npcData);
            }
        }

        // 6. Отправляем запросы на спавн (например, через EntityCommandBuffer)
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
        currentNPCCounts.Dispose();
        spawnRequests.Dispose();
    }

    // Вспомогательная функция для генерации данных NPC
    // Возвращает NPCSpawnData с null Entity для буферов
    private NPCSpawnData GenerateNPCData(ref Random random, int2 chunkId, EntityManager entityManager, NPCGenerationSettings settings)
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

        // Генерация Faction (используем веса из настроек)
        var totalWeight = settings.FamiliesWeight + settings.PoliceWeight + settings.CiviliansWeight;
        var randFrac = random.NextFloat();
        Faction faction;
        if (randFrac < (settings.FamiliesWeight / totalWeight))
            faction = new Faction(FactionType.Families);
        else if (randFrac < ((settings.FamiliesWeight + settings.PoliceWeight) / totalWeight))
            faction = new Faction(FactionType.Police);
        else
            faction = new Faction(FactionType.Civilians);

        // Генерация Goal (упрощённо)
        // Требуется float3 для targetPosition
        var goal = new CurrentGoal(GoalType.Idle, targetPosition: new float3(location.GlobalPosition2D.x, location.GlobalPosition2D.y, 0), priority: 0.5f);

        // Генерация States (начальное состояние)
        var states = new StateFlags(alive: true, injured: false, wanted: false);

        // Генерация Traits (используем диапазоны из настроек)
        var traits = new Traits(
            aggression: random.NextFloat() * (settings.MaxAggression - settings.MinAggression) + settings.MinAggression,
            loyalty: random.NextFloat(),
            intelligence: random.NextFloat()
        );

        // ВОЗВРАЩАЕМ NPCSpawnData с null Entity для буферов
        // Их создаст NPCBufferCreationSystem
        return new NPCSpawnData(id, name, location, faction, Entity.Null, goal, states, traits, Entity.Null);
    }
}