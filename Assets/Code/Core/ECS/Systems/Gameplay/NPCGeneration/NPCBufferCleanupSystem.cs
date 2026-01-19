using Unity.Entities;
using Unity.Burst;

// Система очистки: удаляет временные Entity с инструкциями после заполнения буферов
// Запускается после NPCBufferFillSystem
// 
// ПОРЯДОК РАБОТЫ:
// 1. NPCGeneratorSystem создает NPC с NPCSpawnData
// 2. NPCBufferCreationSystem создает Entity с буферами и инструкциями
// 3. NPCBufferFillSystem заполняет буферы из инструкций
// 4. NPCBufferCleanupSystem (эта) удаляет временные инструкции
// 5. NPCSpawnerSystem добавляет финальные компоненты
// Перемещено в InitializationSystemGroup для согласованности с NPCBufferFillSystem
[UpdateInGroup(typeof(InitializationSystemGroup))]
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