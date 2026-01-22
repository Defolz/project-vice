using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

// Система социального поведения NPC
// Управляет взаимодействием между NPC (общение, встречи)
[BurstCompile]
[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateAfter(typeof(GoalExecutionSystem))]
public partial struct SocialBehaviorSystem : ISystem
{
    private const float SOCIAL_INTERACTION_DISTANCE = 3f;
    private const float MIN_SOCIAL_DURATION = 30f; // 30 секунд
    private const float MAX_SOCIAL_DURATION = 180f; // 3 минуты
    
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
    }
    
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var currentTime = (float)SystemAPI.Time.ElapsedTime;
        var random = Random.CreateFromIndex((uint)(currentTime * 1000f + 1));
        var ecb = new EntityCommandBuffer(Allocator.TempJob);
        
        // Обрабатываем NPC с целью Socialize
        foreach (var (goal, location, pathFollower, stateFlags, entity) in 
                 SystemAPI.Query<RefRW<CurrentGoal>, RefRO<Location>, RefRO<PathFollower>, 
                                RefRW<StateFlags>>()
                 .WithEntityAccess())
        {
            if (goal.ValueRO.Type != GoalType.Socialize)
                continue;
            
            // Если NPC достиг места встречи
            if (pathFollower.ValueRO.State == PathFollowerState.Completed)
            {
                // Отмечаем как занятого (в процессе социализации)
                var flags = stateFlags.ValueRO;
                if (!flags.IsBusy)
                {
                    flags.IsBusy = true;
                    ecb.SetComponent(entity, flags);
                    
                    // Устанавливаем время окончания социализации
                    var socialDuration = random.NextFloat(MIN_SOCIAL_DURATION, MAX_SOCIAL_DURATION);
                    goal.ValueRW = new CurrentGoal(
                        GoalType.Socialize,
                        targetPosition: goal.ValueRO.TargetPosition,
                        priority: goal.ValueRO.Priority,
                        expiryTime: currentTime + socialDuration
                    );
                    
                    UnityEngine.Debug.Log($"<color=magenta>Entity {entity.Index}: Started socializing</color>");
                }
            }
        }
        
        // Завершаем социализацию для NPC, чье время истекло
        foreach (var (goal, stateFlags, entity) in 
                 SystemAPI.Query<RefRO<CurrentGoal>, RefRW<StateFlags>>()
                 .WithEntityAccess())
        {
            if (goal.ValueRO.Type != GoalType.Socialize)
                continue;
            
            if (stateFlags.ValueRO.IsBusy && goal.ValueRO.IsExpired(currentTime))
            {
                // Снимаем флаг занятости
                var flags = stateFlags.ValueRO;
                flags.IsBusy = false;
                ecb.SetComponent(entity, flags);
                
                UnityEngine.Debug.Log($"<color=magenta>Entity {entity.Index}: Finished socializing</color>");
            }
        }
        
        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }
    
    // TODO: Добавить поиск ближайших NPC той же фракции для социализации
    // TODO: Добавить систему отношений (улучшение/ухудшение Relationships)
    // TODO: Добавить анимации/визуальные эффекты социализации
}
