using Unity.Entities;
using Unity.Mathematics;

public struct NPCId : IComponentData
{
    public uint Value;
    public uint GenerationSeed; // Изменено на uint для согласованности с Value
    
    public static NPCId Generate(uint seed)
    {
        var random = new Random(seed ^ 123456789U);
        return new NPCId
        {
            Value = random.NextUInt(),
            GenerationSeed = seed // Теперь оба поля uint
        };
    }
    
    public override string ToString()
    {
        return $"NPC_{Value:X8}"; // Форматирование uint
    }
}