using Unity.Entities;
public struct GameInputComponent : IComponentData
{
    public bool IsPausePressed;
    public bool IsActionPressed;
    public bool IsMenuPressed;
}