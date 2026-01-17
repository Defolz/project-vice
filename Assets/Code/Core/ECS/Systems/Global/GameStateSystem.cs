using Unity.Entities;
using Unity.Burst;

[UpdateInGroup(typeof(SimulationSystemGroup))]
public partial struct GameStateSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        // Создаём GameStateComponent, если не существует
        if (!SystemAPI.HasSingleton<GameStateComponent>())
        {
            var entity = state.EntityManager.CreateEntity();
            state.EntityManager.AddComponentData(entity, new GameStateComponent
            {
                Value = GameStateType.Running,
                IsTimePaused = false
            });
            state.EntityManager.SetName(entity, "GameStateSingleton");
        }
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var gameStateSingleton = SystemAPI.GetSingletonRW<GameStateComponent>();
        ref var gameState = ref gameStateSingleton.ValueRW;

        if (SystemAPI.TryGetSingleton<GameInputComponent>(out var inputComp) && inputComp.IsPausePressed)
        {
            if (gameState.Value == GameStateType.Running)
            {
                gameState.Value = GameStateType.Paused;
                gameState.IsTimePaused = true;
            }
            else if (gameState.Value == GameStateType.Paused)
            {
                gameState.Value = GameStateType.Running;
                gameState.IsTimePaused = false;
            }
        }
    }
}