using Unity.Entities;
using Unity.Mathematics;

public struct NPCId : IComponentData
{
    public uint Value; // Изменено на uint
    public int GenerationSeed; // Для воспроизводимости процедурной генерации
    
    // Изменён метод Generate, чтобы принимать uint
    public static NPCId Generate(uint seed) // Принимает uint
    {
        var random = new Random(seed ^ 123456789U); // Используем uint для Random, ^ с uint
        return new NPCId
        {
            Value = random.NextUInt(), // Генерируем uint ID
            GenerationSeed = (int)seed // Сохраняем seed как int, если нужно
        };
    }
    
    public override string ToString()
    {
        return $"NPC_{Value:X8}"; // Форматирование uint
    }
}