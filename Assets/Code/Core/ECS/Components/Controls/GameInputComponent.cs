using Unity.Entities;

public struct GameInputComponent : IComponentData
{
    public bool IsPausePressed;
    public bool WasPausePressedLastFrame; // Debounce для pause
    public bool IsActionPressed;
    public bool IsMenuPressed;
}