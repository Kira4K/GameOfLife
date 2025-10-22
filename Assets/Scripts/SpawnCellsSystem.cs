using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;

namespace GameOfLife
{
    [UpdateBefore(typeof(DrawCellsSystem))]
    public partial struct SpawnCellsSystem : ISystem
    {
        private EntityQuery _cellsQuery;

        [BurstCompile]
        void ISystem.OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<SpawnCellsConfig>();
            _cellsQuery = new EntityQueryBuilder(Allocator.Temp).WithAll<Cell>().Build(ref state);
        }

        [BurstCompile]
        void ISystem.OnUpdate(ref SystemState state)
        {
            state.Enabled = false;

            SpawnCellsConfig spawnCellsConfig = SystemAPI.GetSingleton<SpawnCellsConfig>();

            int width = spawnCellsConfig.Width;
            int height = spawnCellsConfig.Height;
            int amount = width * height;

            state.EntityManager.DestroyEntity(_cellsQuery);

            NativeArray<Entity> spawnedEntities = new(amount, Allocator.Temp);
            state.EntityManager.Instantiate(spawnCellsConfig.CellPrefabEntity, spawnedEntities);

            for (int z = 0; z < height; z++)
            {
                for (int x = 0; x < width; x++)
                {
                    Entity spawnedEntity = spawnedEntities[z * width + x];
                    state.EntityManager.SetComponentData(spawnedEntity, LocalTransform.FromPosition(x, 0f, z));

                    var neighbours = state.EntityManager.AddBuffer<NeighbourCell>(spawnedEntity);

                    Add(-1, -1);
                    Add(-1, +0);
                    Add(-1, +1);
                    Add(+0, -1);
                    Add(+0, +1);
                    Add(+1, -1);
                    Add(+1, +0);
                    Add(+1, +1);

                    void Add(int xDelta, int zDelta)
                    {
                        int xNeighbour = (x + xDelta + width) % width;
                        int zNeighbour = (z + zDelta + height) % height;
                        int index = zNeighbour * width + xNeighbour;
                        neighbours.Add(new NeighbourCell() { Entity = spawnedEntities[index] });
                    }
                }
            }
        }
    }
}