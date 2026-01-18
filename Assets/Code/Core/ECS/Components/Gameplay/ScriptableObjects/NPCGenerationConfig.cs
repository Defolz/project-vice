using UnityEngine;

// ScriptableObject для хранения настроек генерации NPC
[CreateAssetMenu(fileName = "NPCGenerationConfig", menuName = "Project Vice/Configs/NPC Generation Config")]
public class NPCGenerationConfig : ScriptableObject
{
    [Header("Basic Spawn Settings")]
    [Tooltip("Минимальное количество NPC для генерации за цикл")]
    public int MinNPCPerSpawnCycle = 5;
    [Tooltip("Максимальное количество NPC для генерации за цикл")]
    public int MaxNPCPerSpawnCycle = 10;

    [Header("Density Settings")]
    [Tooltip("Среднее количество NPC на один чанк")]
    public float AverageNPCPerChunk = 2.0f;
    [Tooltip("Максимальное количество NPC в одном чанке")]
    public int MaxNPCPerChunk = 5;

    [Header("Faction Weights")]
    [Tooltip("Вероятность появления NPC из Families")]
    [Range(0, 1)]
    public float FamiliesWeight = 0.3f;
    [Tooltip("Вероятность появления NPC из Police")]
    [Range(0, 1)]
    public float PoliceWeight = 0.2f;
    [Tooltip("Вероятность появления NPC из Civilians")]
    [Range(0, 1)]
    public float CiviliansWeight = 0.5f;

    [Header("Traits Range")]
    [Tooltip("Минимальное значение черты агрессии")]
    [Range(0, 1)]
    public float MinAggression = 0.1f;
    [Tooltip("Максимальное значение черты агрессии")]
    [Range(0, 1)]
    public float MaxAggression = 0.9f;
    // Можно добавить Min/Max для других черт: Loyalty, Intelligence и т.д.
}