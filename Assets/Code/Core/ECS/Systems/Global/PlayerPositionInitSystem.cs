using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;

// Система инициализации позиции игрока
// Создаёт PlayerPosition синглтон при запуске
[UpdateInGroup(typeof(InitializationSystemGroup))]
public partial struct PlayerPositionInitSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        // Создаём PlayerPosition синглтон, если не существует
        if (!SystemAPI.HasSingleton<PlayerPosition>())
        {
            var entity = state.EntityManager.CreateEntity();
            state.EntityManager.AddComponentData(entity, new PlayerPosition(0f, 0f));
            state.EntityManager.SetName(entity, "PlayerPositionSingleton");
        }
        
        // Отключаем систему после первого запуска
        state.Enabled = false;
    }
}
