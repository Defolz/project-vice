using Unity.Entities;
using Unity.Burst;

// Система очистки: удаляет временные Entity с инструкциями после заполнения буферов
// Запускается после NPCBufferFillSystem
// 
// ПОРЯДОК РАБОТЫ:
// 1. NPCGeneratorSystem (в InitializationSystemGroup) создает NPC с NPCSpawnData
// 2. NPCBufferCreationSystem (в InitializationSystemGroup) создает Entity с буферами и инструкциями
// 3. ECB применяется в конце InitializationSystemGroup
// 4. NPCBufferFillSystem (в SimulationSystemGroup) заполняет буферы из инструкций
// 5. NPCBufferCleanupSystem (эта, в SimulationSystemGroup) удаляет временные инструкции
// 6. NPCSpawnerSystem (в SimulationSystemGroup) добавляет финальные компоненты
[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateAfter(typeof(NPCBufferFillSystem))]
public partial struct NPCBufferCleanupSystem : ISystem
{
    // BURST отключен для дебага - можно включить позже
    public void OnUpdate(ref SystemState state)
    {
        var ecbSingleton = SystemAPI.GetSingletonRW<EndInitializationEntityCommandBufferSystem.Singleton>(); // Или EndSimulationECB
        var ecb = ecbSingleton.ValueRW.CreateCommandBuffer(state.WorldUnmanaged);

        foreach (var (fillInstruction, entity) in SystemAPI.Query<RefRO<NPCBufferFillInstruction>>().WithEntityAccess())
        {
            var instruction = fillInstruction.ValueRO;

            // Удаляем временные Entity с инструкциями и компонент инструкций
            ecb.DestroyEntity(instruction.ScheduleInstructionsBufferEntity);
            ecb.DestroyEntity(instruction.RelationshipsInstructionsBufferEntity);
            ecb.RemoveComponent<NPCBufferFillInstruction>(entity);
        }
    }
}