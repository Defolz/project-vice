using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

// Тестовая система для демонстрации pathfinding
// ОТКЛЮЧЕНА! Используй NPCPathAssigner для назначения путей
[UpdateInGroup(typeof(InitializationSystemGroup))]
public partial struct PathfindingTestSystem : ISystem
{
    private bool initialized;
    
    public void OnCreate(ref SystemState state)
    {
        initialized = false;
        // ОТКЛЮЧАЕМ СИСТЕМУ СРАЗУ
        state.Enabled = false;
    }
    
    public void OnUpdate(ref SystemState state)
    {
        // Система отключена
        state.Enabled = false;
    }
}
