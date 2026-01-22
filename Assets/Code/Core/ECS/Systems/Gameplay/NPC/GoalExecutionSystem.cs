using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

// Главная система выполнения целей NPC
// Анализирует CurrentGoal и делегирует выполнение соответствующим поведенческим системам
// Обновляет приоритеты, проверяет истечение целей и управляет переходами между целями
[BurstCompile]
[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateBefore(typeof(GoalPathIntegrationSystem))]
public partial struct GoalExecutionSystem : ISystem
{
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
        var ecb = new EntityCommandBuffer(Allocator.TempJob);
        
        // Проверяем все активные цели NPC
        foreach (var (goal, stateFlags, entity) in 
                 SystemAPI.Query<RefRW<CurrentGoal>, RefRO<StateFlags>>()
                 .WithEntityAccess())
        {
            // Пропускаем мертвых/арестованных NPC
            if (stateFlags.ValueRO.IsDead || stateFlags.ValueRO.IsArrested)
            {
                if (goal.ValueRO.Type != GoalType.None)
                {
                    goal.ValueRW = new CurrentGoal(GoalType.None);
                }
                continue;
            }
            
            // Проверяем истечение цели
            if (goal.ValueRO.IsExpired(currentTime))
            {
                UnityEngine.Debug.Log($"<color=yellow>Entity {entity.Index}: Goal (type: {(int)goal.ValueRO.Type}) expired</color>");
                goal.ValueRW = new CurrentGoal(GoalType.None);
                
                // Очистка связанных компонентов
                CleanupGoalComponents(ref ecb, entity, goal.ValueRO.Type);
                continue;
            }
            
            // Если цель None - назначаем Idle
            if (goal.ValueRO.Type == GoalType.None)
            {
                AssignIdleGoal(ref goal.ValueRW, ref ecb, entity, gameTime);
            }
        }
        
        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }
    
    // Назначает цель Idle для NPC
    private static void AssignIdleGoal(
        ref CurrentGoal goal, 
        ref EntityCommandBuffer ecb, 
        Entity entity,
        GameTimeComponent gameTime)
    {
        goal = new CurrentGoal(
            GoalType.Idle,
            priority: 0.1f,
            expiryTime: -1f // Idle не истекает
        );
    }
    
    // Очищает компоненты, связанные с завершенной целью
    private static void CleanupGoalComponents(
        ref EntityCommandBuffer ecb,
        Entity entity,
        GoalType completedGoalType)
    {
        // Очистка pathfinding компонентов для целей движения
        if (completedGoalType == GoalType.MoveToLocation || 
            completedGoalType == GoalType.PatrolArea ||
            completedGoalType == GoalType.VisitLocation)
        {
            ecb.RemoveComponent<PathRequest>(entity);
            ecb.RemoveComponent<PathResult>(entity);
            ecb.RemoveComponent<PathFollower>(entity);
        }
    }
}
