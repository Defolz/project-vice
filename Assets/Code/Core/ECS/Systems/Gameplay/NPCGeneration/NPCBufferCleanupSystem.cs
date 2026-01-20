using Unity.Entities;

// Система очистки: удаляет временные Entity с инструкциями после заполнения буферов
// Запускается после NPCBufferFillSystem
[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateAfter(typeof(NPCBufferFillSystem))]
public partial struct NPCBufferCleanupSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        var entityManager = state.EntityManager;

        var query = SystemAPI.QueryBuilder()
            .WithAll<NPCBufferFillInstruction>()
            .Build();

        if (query.IsEmpty) return;

        var instructions = query.ToComponentDataArray<NPCBufferFillInstruction>(Unity.Collections.Allocator.Temp);
        var entities = query.ToEntityArray(Unity.Collections.Allocator.Temp);

        for (int i = 0; i < entities.Length; i++)
        {
            var entity = entities[i];
            var instruction = instructions[i];

            // Удаляем временные Entity с инструкциями
            if (entityManager.Exists(instruction.ScheduleInstructionsBufferEntity))
                entityManager.DestroyEntity(instruction.ScheduleInstructionsBufferEntity);
            
            if (entityManager.Exists(instruction.RelationshipsInstructionsBufferEntity))
                entityManager.DestroyEntity(instruction.RelationshipsInstructionsBufferEntity);
            
            // Удаляем компонент инструкций
            entityManager.RemoveComponent<NPCBufferFillInstruction>(entity);
        }

        instructions.Dispose();
        entities.Dispose();
    }
}
