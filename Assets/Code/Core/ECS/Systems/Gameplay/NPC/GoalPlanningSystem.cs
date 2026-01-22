using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

// Система планирования целей для NPC
// Анализирует контекст (время суток, состояние, фракцию, характеристики) и назначает новые цели
[BurstCompile]
[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateBefore(typeof(GoalExecutionSystem))]
public partial struct GoalPlanningSystem : ISystem
{
    // Шансы появления разных целей (настраиваемые параметры)
    private const float PATROL_CHANCE = 0.3f;
    private const float SOCIALIZE_CHANCE = 0.2f;
    private const float VISIT_LOCATION_CHANCE = 0.15f;
    private const float WORK_HOURS_START = 8f;  // 8:00
    private const float WORK_HOURS_END = 18f;   // 18:00
    private const float SLEEP_HOURS_START = 22f; // 22:00
    private const float SLEEP_HOURS_END = 6f;    // 6:00
    
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<GameTimeComponent>();
    }
    
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var currentTime = (float)SystemAPI.Time.ElapsedTime;
        var gameTime = SystemAPI.GetSingleton<GameTimeComponent>();
        var random = Random.CreateFromIndex((uint)(currentTime * 1000f + 1));
        var hour = gameTime.Hour + gameTime.Minute / 60f;
        var ecb = new EntityCommandBuffer(Allocator.TempJob);
        
        // Планируем цели для NPC
        foreach (var (goal, traits, faction, stateFlags, location, entity) in 
                 SystemAPI.Query<RefRW<CurrentGoal>, RefRO<Traits>, RefRO<Faction>, 
                                RefRW<StateFlags>, RefRO<Location>>()
                 .WithEntityAccess())
        {
            // Пропускаем мертвых/арестованных
            if (stateFlags.ValueRO.IsDead || stateFlags.ValueRO.IsArrested)
                continue;
            
            // Проверяем, должен ли NPC находиться в другом состоянии исходя из времени
            var shouldBeSleeping = hour >= SLEEP_HOURS_START || hour < SLEEP_HOURS_END;
            var shouldBeWorking = hour >= WORK_HOURS_START && hour < WORK_HOURS_END && 
                                  (faction.ValueRO.Type == FactionType.Civilians || faction.ValueRO.Type == FactionType.Police);
            
            // Форсированное переключение: если NPC спит, но уже день, сбрасываем цель И флаги
            if (goal.ValueRO.Type == GoalType.Sleep && !shouldBeSleeping)
            {
                // Сбрасываем флаги сна
                var flags = stateFlags.ValueRO;
                if (flags.IsSleeping || flags.IsBusy)
                {
                    flags.IsSleeping = false;
                    flags.IsBusy = false;
                    ecb.SetComponent(entity, flags);
                }
                
                UnityEngine.Debug.Log($"<color=purple>Entity {entity.Index}: Woke up (time: {hour:F1}h)</color>");
                goal.ValueRW = new CurrentGoal(GoalType.Idle, priority: 0.1f);
                continue;
            }
            
            // Форсированное переключение: если NPC работает, но уже вечер, сбрасываем цель И флаги
            if (goal.ValueRO.Type == GoalType.Work && !shouldBeWorking)
            {
                // Сбрасываем флаг занятости
                var flags = stateFlags.ValueRO;
                if (flags.IsBusy)
                {
                    flags.IsBusy = false;
                    ecb.SetComponent(entity, flags);
                }
                
                UnityEngine.Debug.Log($"<color=blue>Entity {entity.Index}: Finished work (time: {hour:F1}h)</color>");
                goal.ValueRW = new CurrentGoal(GoalType.Idle, priority: 0.1f);
                continue;
            }
            
            // Планируем цели только для NPC в состоянии Idle
            if (goal.ValueRO.Type != GoalType.Idle)
                continue;
            
            // Планируем новую цель на основе контекста
            var newGoal = PlanNextGoal(
                ref random,
                gameTime,
                traits.ValueRO,
                faction.ValueRO,
                location.ValueRO,
                currentTime
            );
            
            // Назначаем новую цель, если она отличается от текущей
            if (newGoal.Type != GoalType.Idle)
            {
                goal.ValueRW = newGoal;
                
                // Логирование без ToString() для Burst compatibility
                var priorityPercent = (int)(newGoal.Priority * 100);
                UnityEngine.Debug.Log($"<color=cyan>Entity {entity.Index}: New goal (type: {(int)newGoal.Type}, priority: {priorityPercent}%)</color>");
            }
        }
        
        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }
    
    // Планирует следующую цель на основе множества факторов
    private static CurrentGoal PlanNextGoal(
        ref Random random,
        GameTimeComponent gameTime,
        Traits traits,
        Faction faction,
        Location location,
        float currentTime)
    {
        var hour = gameTime.Hour + gameTime.Minute / 60f;
        
        // 1. Проверка времени сна (высокий приоритет)
        if (ShouldSleep(hour, traits))
        {
            return new CurrentGoal(
                GoalType.Sleep,
                priority: 0.8f,
                expiryTime: currentTime + 3600f // 1 час игрового времени
            );
        }
        
        // 2. Проверка рабочего времени (средний приоритет)
        if (ShouldWork(hour, faction.Type))
        {
            var workLocation = GetWorkLocation(location, ref random);
            return new CurrentGoal(
                GoalType.Work,
                targetPosition: workLocation,
                priority: 0.6f,
                expiryTime: currentTime + 7200f // 2 часа
            );
        }
        
        // 3. Патрулирование (для полиции и банд)
        if (ShouldPatrol(faction.Type, traits, ref random))
        {
            var patrolTarget = GetPatrolTarget(location, ref random);
            return new CurrentGoal(
                GoalType.PatrolArea,
                targetPosition: patrolTarget,
                priority: 0.5f,
                expiryTime: currentTime + 600f // 10 минут
            );
        }
        
        // 4. Социализация (случайно)
        if (random.NextFloat() < SOCIALIZE_CHANCE)
        {
            var socialSpot = GetSocialLocation(location, ref random);
            return new CurrentGoal(
                GoalType.Socialize,
                targetPosition: socialSpot,
                priority: 0.4f,
                expiryTime: currentTime + 300f // 5 минут
            );
        }
        
        // 5. Посещение локации (случайно)
        if (random.NextFloat() < VISIT_LOCATION_CHANCE)
        {
            var visitTarget = GetRandomLocation(location, ref random);
            return new CurrentGoal(
                GoalType.VisitLocation,
                targetPosition: visitTarget,
                priority: 0.3f,
                expiryTime: currentTime + 900f // 15 минут
            );
        }
        
        // По умолчанию - оставаться в Idle
        return new CurrentGoal(GoalType.Idle, priority: 0.1f);
    }
    
    // Проверяет, должен ли NPC спать
    private static bool ShouldSleep(float hour, Traits traits)
    {
        // Ночные часы (22:00 - 6:00)
        if (hour >= SLEEP_HOURS_START || hour < SLEEP_HOURS_END)
        {
            // Более тревожные NPC спят меньше
            var sleepChance = 0.9f - (traits.Anxiety * 0.3f);
            return true; // В ночное время большинство спит
        }
        
        return false;
    }
    
    // Проверяет, должен ли NPC работать
    private static bool ShouldWork(float hour, FactionType factionType)
    {
        // Рабочие часы (8:00 - 18:00)
        if (hour >= WORK_HOURS_START && hour < WORK_HOURS_END)
        {
            // Гражданские и полиция работают
            if (factionType == FactionType.Civilians || factionType == FactionType.Police)
            {
                return true;
            }
        }
        
        return false;
    }
    
    // Проверяет, должен ли NPC патрулировать
    private static bool ShouldPatrol(FactionType factionType, Traits traits, ref Random random)
    {
        // Полиция патрулирует чаще
        if (factionType == FactionType.Police)
        {
            return random.NextFloat() < 0.5f;
        }
        
        // Банды патрулируют свою территорию (зависит от агрессии)
        if (factionType == FactionType.Families || 
            factionType == FactionType.Colombians)
        {
            return random.NextFloat() < (PATROL_CHANCE + traits.Aggression * 0.2f);
        }
        
        return false;
    }
    
    // Генерирует случайную рабочую позицию рядом с текущей
    private static float3 GetWorkLocation(Location currentLocation, ref Random random)
    {
        var offset = new float2(
            random.NextFloat(-30f, 30f),
            random.NextFloat(-30f, 30f)
        );
        
        var workPos = currentLocation.GlobalPosition2D + offset;
        return new float3(workPos.x, 0f, workPos.y);
    }
    
    // Генерирует позицию для патрулирования
    private static float3 GetPatrolTarget(Location currentLocation, ref Random random)
    {
        var offset = new float2(
            random.NextFloat(-50f, 50f),
            random.NextFloat(-50f, 50f)
        );
        
        var patrolPos = currentLocation.GlobalPosition2D + offset;
        return new float3(patrolPos.x, 0f, patrolPos.y);
    }
    
    // Генерирует социальную локацию
    private static float3 GetSocialLocation(Location currentLocation, ref Random random)
    {
        var offset = new float2(
            random.NextFloat(-20f, 20f),
            random.NextFloat(-20f, 20f)
        );
        
        var socialPos = currentLocation.GlobalPosition2D + offset;
        return new float3(socialPos.x, 0f, socialPos.y);
    }
    
    // Генерирует случайную локацию для посещения
    private static float3 GetRandomLocation(Location currentLocation, ref Random random)
    {
        var offset = new float2(
            random.NextFloat(-80f, 80f),
            random.NextFloat(-80f, 80f)
        );
        
        var randomPos = currentLocation.GlobalPosition2D + offset;
        return new float3(randomPos.x, 0f, randomPos.y);
    }
}
