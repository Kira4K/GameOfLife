using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace GameOfLife
{
    [UpdateAfter(typeof(SimulateCellsSystem))]
    [UpdateBefore(typeof(DrawCellsSystem))]
    public partial struct RandomizeCellsSystem : ISystem
    {
        private Random _random;

        void ISystem.OnCreate(ref SystemState state)
        {
            _random = new Random((uint)System.DateTime.Now.Millisecond);
        }

        [BurstCompile]
        void ISystem.OnUpdate(ref SystemState state)
        {
            state.Enabled = false;

            foreach (var cell in SystemAPI.Query<RefRW<Cell>>())
            {
                cell.ValueRW.IsAliveNext = _random.NextBool();
            }
        }
    }
}