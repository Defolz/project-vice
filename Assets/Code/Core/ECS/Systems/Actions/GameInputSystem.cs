using Unity.Entities;
using UnityEngine.InputSystem;

public partial class GameInputSystem : SystemBase
{
    private bool _wasEscapePressedLastFrame = false;
    private bool _wasSpacePressedLastFrame = false;
    private bool _wasTabPressedLastFrame = false;

    protected override void OnUpdate()
    {
        var entityManager = EntityManager;

        var keyboard = Keyboard.current;

        // Проверяем, нажата ли клавиша в этот кадр
        var isEscapePressed = keyboard?.escapeKey.isPressed ?? false;
        var isSpacePressed = keyboard?.spaceKey.isPressed ?? false;
        var isTabPressed = keyboard?.tabKey.isPressed ?? false;

        // Определяем, было ли нажатие (а не удержание)
        var isPausePressed = isEscapePressed && !_wasEscapePressedLastFrame;
        var isActionPressed = isSpacePressed && !_wasSpacePressedLastFrame;
        var isMenuPressed = isTabPressed && !_wasTabPressedLastFrame;

        // Сохраняем состояния для следующего кадра
        _wasEscapePressedLastFrame = isEscapePressed;
        _wasSpacePressedLastFrame = isSpacePressed;
        _wasTabPressedLastFrame = isTabPressed;

        var input = new GameInputComponent
        {
            IsPausePressed = isPausePressed,
            IsActionPressed = isActionPressed,
            IsMenuPressed = isMenuPressed
        };

        // Используем SystemAPI для безопасного обновления синглтона
        if (SystemAPI.TryGetSingletonEntity<GameInputComponent>(out var inputEntity))
        {
            EntityManager.SetComponentData(inputEntity, input);
        }
    }
}