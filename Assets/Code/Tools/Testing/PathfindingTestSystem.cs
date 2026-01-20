using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

// Тестовая система для демонстрации pathfinding
// Создаёт тестовых NPC с целями движения
// ВАЖНО: Отключить в production!
[UpdateInGroup(typeof(InitializationSystemGroup))]
public partial struct PathfindingTestSystem : ISystem
{
    private bool initialized;
    
    public void OnCreate(ref SystemState state)
    {
        initialized = false;
    }
    
    public void OnUpdate(ref SystemState state)
    {
        if (initialized)
        {
            state.Enabled = false;
            return;
        }
        
        initialized = true;
        
        var entityManager = state.EntityManager;
        var random = new Unity.Mathematics.Random((uint)System.DateTime.Now.Ticks);
        
        // Создаём несколько тестовых NPC с pathfinding
        const int TEST_NPC_COUNT = 5;
        
        for (int i = 0; i < TEST_NPC_COUNT; i++)
        {
            // Случайная стартовая позиция
            var startPos = new float2(
                random.NextFloat(10f, 90f),
                random.NextFloat(10f, 90f)
            );
            
            // Случайная целевая позиция (в другом углу чанка)
            var targetPos = new float2(
                random.NextFloat(110f, 190f),
                random.NextFloat(110f, 190f)
            );
            
            // Создаём test NPC entity
            var testEntity = entityManager.CreateEntity();
            
            // Добавляем базовые компоненты
            entityManager.AddComponentData(testEntity, Location.FromGlobal(startPos));
            
            entityManager.AddComponentData(testEntity, new NPCId
            {
                Value = (uint)(1000 + i)
            });
            
            entityManager.AddComponentData(testEntity, new NameData
            {
                FirstName = new FixedString32Bytes($"PathTest{i}")
            });
            
            // Добавляем цель движения
            entityManager.AddComponentData(testEntity, new CurrentGoal(
                GoalType.MoveToLocation,
                targetPosition: new float3(targetPos.x, 0, targetPos.y),
                priority: 0.8f
            ));
            
            UnityEngine.Debug.Log($"Created test NPC #{i}: {startPos} -> {targetPos}");
        }
        
        UnityEngine.Debug.Log($"<color=cyan>Pathfinding Test System initialized with {TEST_NPC_COUNT} test NPCs</color>");
    }
}
