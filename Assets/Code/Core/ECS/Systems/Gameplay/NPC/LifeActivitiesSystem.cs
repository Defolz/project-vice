using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

// Система управления основными жизненными активностями NPC
// Обрабатывает работу, сон и прием пищи
[BurstCompile]
[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateAfter(typeof(GoalExecutionSystem))]
public partial struct LifeActivitiesSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
    }
    
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var currentTime = (float)SystemAPI.Time.ElapsedTime;
        var ecb = new EntityCommandBuffer(Allocator.TempJob);
        
        // === РАБОТА ===
        HandleWorkActivity(ref state, ref ecb, currentTime);
        
        // === СОН ===
        HandleSleepActivity(ref state, ref ecb, currentTime);
        
        // === ПРИЕМ ПИЩИ ===
        HandleEatActivity(ref state, ref ecb, currentTime);
        
        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }
    
    // Обрабатывает состояние Work
    private void HandleWorkActivity(
        ref SystemState state,
        ref EntityCommandBuffer ecb,
        float currentTime)
    {
        foreach (var (goal, pathFollower, stateFlags, entity) in 
                 SystemAPI.Query<RefRW<CurrentGoal>, RefRO<PathFollower>, RefRW<StateFlags>>()
                 .WithEntityAccess())
        {
            if (goal.ValueRO.Type != GoalType.Work)
                continue;
            
            // Если NPC дошел до работы
            if (pathFollower.ValueRO.State == PathFollowerState.Completed)
            {
                // Отмечаем как занятого работой
                var flags = stateFlags.ValueRO;
                if (!flags.IsBusy)
                {
                    flags.IsBusy = true;
                    ecb.SetComponent(entity, flags);
                    
                    UnityEngine.Debug.Log($"<color=blue>Entity {entity.Index}: Started working</color>");
                }
            }
            
            // Если рабочее время закончилось
            if (stateFlags.ValueRO.IsBusy && goal.ValueRO.IsExpired(currentTime))
            {
                var flags = stateFlags.ValueRO;
                flags.IsBusy = false;
                ecb.SetComponent(entity, flags);
                
                UnityEngine.Debug.Log($"<color=blue>Entity {entity.Index}: Finished working</color>");
            }
        }
    }
    
    // Обрабатывает состояние Sleep
    private void HandleSleepActivity(
        ref SystemState state,
        ref EntityCommandBuffer ecb,
        float currentTime)
    {
        foreach (var (goal, stateFlags, entity) in 
                 SystemAPI.Query<RefRO<CurrentGoal>, RefRW<StateFlags>>()
                 .WithEntityAccess())
        {
            if (goal.ValueRO.Type != GoalType.Sleep)
                continue;
            
            var flags = stateFlags.ValueRO;
            
            // Начинаем спать
            if (!flags.IsSleeping)
            {
                flags.IsSleeping = true;
                flags.IsBusy = true;
                ecb.SetComponent(entity, flags);
                
                UnityEngine.Debug.Log($"<color=purple>Entity {entity.Index}: Went to sleep</color>");
            }
            
            // Просыпаемся
            if (flags.IsSleeping && goal.ValueRO.IsExpired(currentTime))
            {
                flags.IsSleeping = false;
                flags.IsBusy = false;
                ecb.SetComponent(entity, flags);
                
                UnityEngine.Debug.Log($"<color=purple>Entity {entity.Index}: Woke up</color>");
            }
        }
    }
    
    // Обрабатывает состояние Eat
    private void HandleEatActivity(
        ref SystemState state,
        ref EntityCommandBuffer ecb,
        float currentTime)
    {
        const float EAT_DURATION = 600f; // 10 минут
        
        foreach (var (goal, pathFollower, stateFlags, entity) in 
                 SystemAPI.Query<RefRW<CurrentGoal>, RefRO<PathFollower>, RefRW<StateFlags>>()
                 .WithEntityAccess())
        {
            if (goal.ValueRO.Type != GoalType.Eat)
                continue;
            
            // Если NPC дошел до места приема пищи
            if (pathFollower.ValueRO.State == PathFollowerState.Completed)
            {
                var flags = stateFlags.ValueRO;
                if (!flags.IsBusy)
                {
                    flags.IsBusy = true;
                    ecb.SetComponent(entity, flags);
                    
                    // Устанавливаем время окончания приема пищи
                    goal.ValueRW = new CurrentGoal(
                        GoalType.Eat,
                        targetPosition: goal.ValueRO.TargetPosition,
                        priority: goal.ValueRO.Priority,
                        expiryTime: currentTime + EAT_DURATION
                    );
                    
                    UnityEngine.Debug.Log($"<color=orange>Entity {entity.Index}: Started eating</color>");
                }
            }
            
            // Закончил есть
            if (stateFlags.ValueRO.IsBusy && goal.ValueRO.IsExpired(currentTime))
            {
                var flags = stateFlags.ValueRO;
                flags.IsBusy = false;
                ecb.SetComponent(entity, flags);
                
                UnityEngine.Debug.Log($"<color=orange>Entity {entity.Index}: Finished eating</color>");
            }
        }
    }
}
