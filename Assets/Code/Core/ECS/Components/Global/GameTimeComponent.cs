using Unity.Entities;
using Unity.Mathematics;

public struct GameTimeComponent : IComponentData
{
    public float TotalSeconds;
    public int Day;
    public int Hour;
    public int Minute;
    public float TimeScale;
}