using Unity.Entities;

namespace GameOfLife
{
    [InternalBufferCapacity(8)]
    public struct NeighbourCell : IBufferElementData
    {
        public Entity Entity;
    }
}