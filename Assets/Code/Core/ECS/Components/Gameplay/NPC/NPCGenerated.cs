using Unity.Entities;

// Компонент-маркер, указывающий что NPC для этого чанка уже сгенерированы
// Предотвращает повторную генерацию NPC каждый фрейм
public struct NPCGenerated : IComponentData
{
    public int GeneratedCount; // Сколько NPC было сгенерировано
    
    public NPCGenerated(int count)
    {
        GeneratedCount = count;
    }
}
