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

        if (SystemAPI.TryGetSingletonRW<GameInputComponent>(out var inputSingleton))
        {
            ref var inputComp = ref inputSingleton.ValueRW;
            
            // Debounce: только при новом нажатии
            if (inputComp.IsPausePressed && !inputComp.WasPausePressedLastFrame)
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
            
            // Обновляем состояние предыдущего фрейма
            inputComp.WasPausePressedLastFrame = inputComp.IsPausePressed;
        }
    }
}