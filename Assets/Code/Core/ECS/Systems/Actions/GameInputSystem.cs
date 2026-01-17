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

        // Проверяем, существует ли синглтон
        var inputQuery = GetEntityQuery(typeof(GameInputComponent));
        if (inputQuery.CalculateEntityCount() > 0)
        {
            var inputEntity = inputQuery.GetSingletonEntity();
            EntityManager.SetComponentData(inputEntity, input);
        }
    }
}