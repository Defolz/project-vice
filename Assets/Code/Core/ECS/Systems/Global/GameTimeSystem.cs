using Unity.Entities;

[UpdateInGroup(typeof(SimulationSystemGroup))]
public partial struct GameTimeSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        var deltaTime = SystemAPI.Time.DeltaTime;
        if (deltaTime <= 0) return;

        // Проверяем, существует ли GameTimeComponent
        if (SystemAPI.TryGetSingletonRW<GameTimeComponent>(out var gameTimeSingleton))
        {
            var gameTime = gameTimeSingleton.ValueRO;

            if (SystemAPI.TryGetSingleton<GameStateComponent>(out var gameState))
            {
                if (!gameState.IsTimePaused)
                {
                    var scaledDelta = deltaTime * gameTime.TimeScale;
                    var newTotalSeconds = gameTime.TotalSeconds + scaledDelta;

                    // Пересчитываем Day/Hour/Minute
                    var totalMinutes = (int)(newTotalSeconds / 60f);
                    var newMinute = totalMinutes % 60;
                    var totalHours = totalMinutes / 60;
                    var newHour = totalHours % 24;
                    var newDay = totalHours / 24;

                    // Обновляем синглтон
                    var updatedGameTime = new GameTimeComponent
                    {
                        TotalSeconds = newTotalSeconds,
                        Day = newDay,
                        Hour = newHour,
                        Minute = newMinute,
                        TimeScale = gameTime.TimeScale
                    };

                    SystemAPI.SetSingleton(updatedGameTime);
                }
            }
        }
    }
}