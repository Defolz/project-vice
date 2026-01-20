using Unity.Entities;
using Unity.Mathematics;

// Компонент позиции игрока/камеры для управления загрузкой чанков
public struct PlayerPosition : IComponentData
{
    public float2 Value;
    
    public PlayerPosition(float2 position)
    {
        Value = position;
    }
    
    public PlayerPosition(float x, float y)
    {
        Value = new float2(x, y);
    }
}
