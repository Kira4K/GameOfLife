using Unity.Entities;
using UnityEngine;

namespace GameOfLife
{
    public class CellAuthoring : MonoBehaviour
    {
        private class Baker : Baker<CellAuthoring>
        {
            public override void Bake(CellAuthoring authoring)
            {
                Entity entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent<Cell>(entity);
            }
        }
    }

    public struct Cell : IComponentData
    {
        public bool IsAlive;
        public bool IsAliveNext;
    }
}