using Unity.Entities;
using Unity.Mathematics;

public struct Location : IComponentData
{
    public float3 Value; // Глобальная позиция NPC в 3D-мире

    public Location(float3 position)
    {
        Value = position;
    }
    
    // Позволяет легко получить позицию
    public float3 GetPosition() => Value;

    // Позволяет легко установить новую позицию
    public void SetPosition(float3 newPosition)
    {
        Value = newPosition;
    }
    
    public override string ToString()
    {
        return $"Location({Value.x:F2}, {Value.y:F2}, {Value.z:F2})";
    }
}