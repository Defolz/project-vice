using Unity.Entities;
using Unity.Mathematics;

public struct NPCId : IComponentData
{
    public int Value;
    public int GenerationSeed; // Для воспроизводимости процедурной генерации
    
    public static NPCId Generate(int seed)
    {
        var random = new Random((uint)(seed ^ 123456789));
        return new NPCId
        {
            Value = random.NextInt(),
            GenerationSeed = seed
        };
    }
    
    public override string ToString()
    {
        return $"NPC_{Value:X8}";
    }
}