using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace GameOfLife
{
    [RequireMatchingQueriesForUpdate]
    public partial struct SimulateCellsSystem : ISystem
    {
        private EntityQuery _cellsQuery;
        private ComponentLookup<Cell> _cellLookup;

        [BurstCompile]
        void ISystem.OnCreate(ref SystemState state)
        {
            _cellsQuery = new EntityQueryBuilder(Allocator.Temp).WithAllRW<Cell>().WithAll<NeighbourCell>().Build(ref state);
            _cellLookup = state.GetComponentLookup<Cell>();
        }

        [BurstCompile]
        void ISystem.OnUpdate(ref SystemState state)
        {
            var config = SystemAPI.GetSingletonRW<SimulateCellsConfig>();

            if (!config.ValueRO.IsEnabled)
            {
                return;
            }

            float elapsedTime = config.ValueRO.ElapsedTime + SystemAPI.Time.DeltaTime;
            if (elapsedTime < config.ValueRO.TickDuration)
            {
                config.ValueRW.ElapsedTime = elapsedTime;
                return;
            }

            _cellLookup.Update(ref state);

            state.Dependency = new SimulateCellsJob()
            {
                CellLookup = _cellLookup,
            }.ScheduleParallel(_cellsQuery, state.Dependency);

            config.ValueRW.ElapsedTime = 0f;
        }

        [BurstCompile]
        private partial struct SimulateCellsJob : IJobEntity
        {
            [NativeDisableParallelForRestriction]
            public ComponentLookup<Cell> CellLookup;

            public void Execute(in DynamicBuffer<NeighbourCell> neighbours, Entity entity)
            {
                var cell = CellLookup[entity];

                int aliveNeighbours = 0;
                for (int i = 0, lenght = neighbours.Length; i < lenght; i++)
                {
                    var neighbourCell = CellLookup[neighbours[i].Entity];
                    if (neighbourCell.IsAlive)
                    {
                        aliveNeighbours++;
                    }
                }
                cell.IsAliveNext = cell.IsAlive ? aliveNeighbours is > 1 and < 4 : aliveNeighbours is 3;
                CellLookup[entity] = cell;
            }
        }
    }
}