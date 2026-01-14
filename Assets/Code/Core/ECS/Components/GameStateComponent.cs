using Unity.Entities;

public enum GameStateType : sbyte
{
    Unknown = -1,
    Initializing = 0,
    MainMenu = 1,
    Running = 2,
    Paused = 3,
    GameOver = 4
}
public struct GameStateComponent : IComponentData
{
    public GameStateType Value;
    public bool IsTimePaused;
}