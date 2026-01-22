using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

// Система боевого поведения NPC
// Обрабатывает агрессию, атаку, бегство и месть
[BurstCompile]
[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateAfter(typeof(GoalExecutionSystem))]
public partial struct CombatBehaviorSystem : ISystem
{
    private const float ATTACK_RANGE = 2f;
    private const float FLEE_DISTANCE = 50f;
    private const float THREAT_DETECTION_RANGE = 30f;
    
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
        
        // === АТАКА ===
        HandleAttackGoal(ref state, ref ecb, currentTime);
        
        // === БЕГСТВО ===
        HandleFleeGoal(ref state, ref ecb, currentTime, ref random);
        
        // === МЕСТЬ (Retaliate) ===
        // TODO: Требует системы событий для отслеживания атак на союзников
        
        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }
    
    // Обрабатывает цель AttackTarget
    private void HandleAttackGoal(
        ref SystemState state,
        ref EntityCommandBuffer ecb,
        float currentTime)
    {
        foreach (var (goal, location, traits, pathFollower, entity) in 
                 SystemAPI.Query<RefRW<CurrentGoal>, RefRO<Location>, RefRO<Traits>, 
                                RefRO<PathFollower>>()
                 .WithEntityAccess())
        {
            if (goal.ValueRO.Type != GoalType.AttackTarget)
                continue;
            
            // Проверяем, существует ли цель
            if (goal.ValueRO.TargetEntity == Entity.Null || 
                !state.EntityManager.Exists(goal.ValueRO.TargetEntity))
            {
                // Цель исчезла - переходим в Idle
                goal.ValueRW = new CurrentGoal(GoalType.Idle);
                ecb.RemoveComponent<PathFollower>(entity);
                ecb.RemoveComponent<PathResult>(entity);
                
                UnityEngine.Debug.LogWarning($"<color=red>Entity {entity.Index}: Attack target lost</color>");
                continue;
            }
            
            // Получаем позицию цели
            if (!SystemAPI.HasComponent<Location>(goal.ValueRO.TargetEntity))
                continue;
            
            var targetLocation = SystemAPI.GetComponent<Location>(goal.ValueRO.TargetEntity);
            var distance = math.distance(
                location.ValueRO.GlobalPosition2D,
                targetLocation.GlobalPosition2D
            );
            
            // Если в радиусе атаки - атакуем
            if (distance <= ATTACK_RANGE)
            {
                PerformAttack(ref ecb, entity, goal.ValueRO.TargetEntity, traits.ValueRO);
            }
            // Иначе преследуем
            else if (pathFollower.ValueRO.State == PathFollowerState.Completed)
            {
                // Обновляем позицию цели для преследования
                var targetPos = targetLocation.GlobalPosition2D;
                goal.ValueRW = new CurrentGoal(
                    GoalType.AttackTarget,
                    targetEntity: goal.ValueRO.TargetEntity,
                    targetPosition: new float3(targetPos.x, 0f, targetPos.y),
                    priority: goal.ValueRO.Priority,
                    expiryTime: goal.ValueRO.ExpiryTime
                );
                
                // Очищаем путь для пересчета
                ecb.RemoveComponent<PathFollower>(entity);
                ecb.RemoveComponent<PathResult>(entity);
            }
        }
    }
    
    // Обрабатывает цель Flee
    private void HandleFleeGoal(
        ref SystemState state,
        ref EntityCommandBuffer ecb,
        float currentTime,
        ref Random random)
    {
        foreach (var (goal, location, pathFollower, entity) in 
                 SystemAPI.Query<RefRW<CurrentGoal>, RefRO<Location>, RefRO<PathFollower>>()
                 .WithEntityAccess())
        {
            if (goal.ValueRO.Type != GoalType.Flee)
                continue;
            
            // Если достигли безопасного места - переходим в Idle
            if (pathFollower.ValueRO.State == PathFollowerState.Completed)
            {
                goal.ValueRW = new CurrentGoal(GoalType.Idle);
                ecb.RemoveComponent<PathFollower>(entity);
                ecb.RemoveComponent<PathResult>(entity);
                
                UnityEngine.Debug.Log($"<color=green>Entity {entity.Index}: Reached safe location</color>");
            }
            
            // Если цель не установлена - генерируем точку бегства
            if (goal.ValueRO.TargetPosition.Equals(float3.zero))
            {
                var fleePosition = GenerateFleePosition(
                    location.ValueRO.GlobalPosition2D,
                    ref random
                );
                
                goal.ValueRW = new CurrentGoal(
                    GoalType.Flee,
                    targetPosition: new float3(fleePosition.x, 0f, fleePosition.y),
                    priority: 0.9f, // Высокий приоритет
                    expiryTime: currentTime + 60f // 1 минута на побег
                );
            }
        }
    }
    
    // Выполняет атаку (заглушка, требует системы здоровья)
    private static void PerformAttack(
        ref EntityCommandBuffer ecb,
        Entity attacker,
        Entity target,
        Traits attackerTraits)
    {
        // TODO: Реализовать систему здоровья и урона
        // Пока просто логируем
        var damageAmount = (int)(attackerTraits.Aggression * 10f);
        UnityEngine.Debug.Log($"<color=red>Entity {attacker.Index} attacks Entity {target.Index} (Damage: {damageAmount})</color>");
        
        // Пример: можно добавить компонент повреждения
        // ecb.AddComponent(target, new DamageEvent { Amount = damageAmount, Source = attacker });
    }
    
    // Генерирует позицию для бегства (противоположная от угрозы)
    private static float2 GenerateFleePosition(float2 currentPosition, ref Random random)
    {
        var angle = random.NextFloat(0f, math.PI * 2f);
        var offset = new float2(
            math.cos(angle) * FLEE_DISTANCE,
            math.sin(angle) * FLEE_DISTANCE
        );
        
        return currentPosition + offset;
    }
}
