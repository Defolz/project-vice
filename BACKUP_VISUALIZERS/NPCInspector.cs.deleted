using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public class NPCInspector : MonoBehaviour
{
    private static EntityManager EntityManager;

    private void OnEnable()
    {
        var world = World.DefaultGameObjectInjectionWorld;
        if (world != null && world.IsCreated)
        {
            EntityManager = world.EntityManager;
        }
    }

    public static void Inspect(Entity npcEntity)
    {
        if (EntityManager == null || !EntityManager.Exists(npcEntity))
        {
            Debug.LogWarning("NPCInspector: Invalid entity");
            return;
        }

        if (!EntityManager.HasComponent<NPCId>(npcEntity))
        {
            Debug.LogWarning("Entity is not NPC");
            return;
        }

        var npcId = EntityManager.GetComponentData<NPCId>(npcEntity);
        var location = EntityManager.GetComponentData<Location>(npcEntity);
        var name = EntityManager.GetComponentData<NameData>(npcEntity);
        var faction = EntityManager.GetComponentData<Faction>(npcEntity);
        var traits = EntityManager.GetComponentData<Traits>(npcEntity);
        var state = EntityManager.GetComponentData<StateFlags>(npcEntity);

        Debug.Log($@"
=== NPC INSPECTION ===
ID: {npcId.Value} (Seed: {npcId.GenerationSeed})
Name: {name.FirstName} {name.LastName} ({name.Nickname})
Position: {location.GlobalPosition2D} | Chunk {location.ChunkId}
Faction: {faction.Value}
Traits:
  Aggression: {traits.Aggression:F2}
  Loyalty: {traits.Loyalty:F2}
  Intelligence: {traits.Intelligence:F2}
State:
  Alive: {state.IsAlive}
  Injured: {state.IsInjured}
  Wanted: {state.IsWanted}
Entity: {npcEntity}
====================
");
    }
}
