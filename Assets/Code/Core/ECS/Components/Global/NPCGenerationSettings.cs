using Unity.Entities;

// Компонент-синглтон для хранения настроек генерации NPC в ECS
// Значения копируются из NPCGenerationConfig ScriptableObject при инициализации
public struct NPCGenerationSettings : IComponentData
{
    // Basic Spawn Settings
    public int MinNPCPerSpawnCycle;
    public int MaxNPCPerSpawnCycle;

    // Density Settings
    public float AverageNPCPerChunk;
    public int MaxNPCPerChunk;

    // Faction Weights (должны в сумме давать 1.0)
    public float FamiliesWeight;
    public float PoliceWeight;
    public float CiviliansWeight;

    // Traits Range
    public float MinAggression;
    public float MaxAggression;
    // TODO: добавить Min/Max для других черт

    // Конструктор для удобного создания из ScriptableObject
    public NPCGenerationSettings(NPCGenerationConfig config)
    {
        MinNPCPerSpawnCycle = config.MinNPCPerSpawnCycle;
        MaxNPCPerSpawnCycle = config.MaxNPCPerSpawnCycle;
        AverageNPCPerChunk = config.AverageNPCPerChunk;
        MaxNPCPerChunk = config.MaxNPCPerChunk;
        FamiliesWeight = config.FamiliesWeight;
        PoliceWeight = config.PoliceWeight;
        CiviliansWeight = config.CiviliansWeight;
        MinAggression = config.MinAggression;
        MaxAggression = config.MaxAggression;
    }

    // Конструктор по умолчанию (для тестов или если config не задан)
    public static NPCGenerationSettings Default => new NPCGenerationSettings
    {
        MinNPCPerSpawnCycle = 5,
        MaxNPCPerSpawnCycle = 10,
        AverageNPCPerChunk = 2.0f,
        MaxNPCPerChunk = 5,
        FamiliesWeight = 0.3f,
        PoliceWeight = 0.2f,
        CiviliansWeight = 0.5f,
        MinAggression = 0.1f,
        MaxAggression = 0.9f
    };
}
