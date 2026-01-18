using Unity.Entities;
using Unity.Burst;
 // Без [BurstCompile]

// Система, удаляющая временные Entity с инструкциями и компоненты
// Запускается после NPCBufferFillSystem
[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateAfter(typeof(NPCBufferFillSystem))]
public partial struct NPCBufferCleanupSystem : ISystem
{
    [BurstCompile] // УБРАНО
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