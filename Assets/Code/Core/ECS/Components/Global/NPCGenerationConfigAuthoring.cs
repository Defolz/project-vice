using Unity.Entities;
using UnityEngine;

// MonoBehaviour для загрузки NPCGenerationConfig в ECS World
// Добавьте этот компонент на любой GameObject в сцене
public class NPCGenerationConfigAuthoring : MonoBehaviour
{
    [Header("NPC Generation Configuration")]
    [Tooltip("ScriptableObject с настройками генерации NPC. Если не задан, будут использованы значения по умолчанию.")]
    public NPCGenerationConfig Config;
}

// Baker для конвертации в ECS (Entities 1.0+)
class NPCGenerationConfigBaker : Baker<NPCGenerationConfigAuthoring>
{
    public override void Bake(NPCGenerationConfigAuthoring authoring)
    {
        var entity = GetEntity(TransformUsageFlags.None);
        
        // Если конфиг задан, используем его, иначе дефолтные значения
        var settings = authoring.Config != null 
            ? new NPCGenerationSettings(authoring.Config)
            : NPCGenerationSettings.Default;
        
        AddComponent(entity, settings);
    }
}
