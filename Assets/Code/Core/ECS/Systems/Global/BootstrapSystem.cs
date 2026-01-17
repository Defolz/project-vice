using Unity.Entities;

public partial class BootstrapSystem : SystemBase
{
    protected override void OnStartRunning()
    {
        base.OnStartRunning();

        // Создаём GameTimeComponent
        var gameTimeEntity = EntityManager.CreateEntity();
        EntityManager.AddComponentData(gameTimeEntity, new GameTimeComponent
        {
            TotalSeconds = 0f,
            Day = 1,
            Hour = 0,
            Minute = 0,
            TimeScale = 1f // Установим нормальную скорость
        });
        EntityManager.SetName(gameTimeEntity, "GameTimeSingleton");

        // Создаём GameInputComponent
        var inputEntity = EntityManager.CreateEntity();
        EntityManager.AddComponentData(inputEntity, new GameInputComponent
        {
            IsPausePressed = false,
            IsActionPressed = false,
            IsMenuPressed = false
        });
        EntityManager.SetName(inputEntity, "GameInputSingleton");
        
        // Удалим GameStateComponent из Bootstrap, т.к. он создаётся в GameStateSystem.OnCreate
    }

    protected override void OnUpdate() { }
}