using Unity.Entities;
using Unity.Burst;

[UpdateInGroup(typeof(SimulationSystemGroup))]
public partial struct GameStateSystem : ISystem
{
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