using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;

namespace GameOfLife
{
    [UpdateAfter(typeof(SimulateCellsSystem))]
    [RequireMatchingQueriesForUpdate]
    public partial struct DrawCellsSystem : ISystem
    {
        [BurstCompile]
        void ISystem.OnUpdate(ref SystemState state)
        {
            state.Dependency = new DrawCellsJob().ScheduleParallel(state.Dependency);
        }

        [BurstCompile]
        private partial struct DrawCellsJob : IJobEntity
        {
            public void Execute(ref Cell cell, ref URPMaterialPropertyBaseColor colorProperty)
            {
                colorProperty.Value = cell.IsAliveNext ? new float4(0f, 1f, 0f, 1f) : new float4(0f, 0f, 0f, 1f);
                cell.IsAlive = cell.IsAliveNext;
            }
        }
    }
}