using Unity.Entities;
using UnityEngine;

namespace GameOfLife
{
    public class SpawnCellsConfigAuthoring : MonoBehaviour
    {
        [SerializeField] private int width = 50;
        [SerializeField] private int height = 50;
        [SerializeField] private GameObject cellPrefab;

        private class Baker : Baker<SpawnCellsConfigAuthoring>
        {
            public override void Bake(SpawnCellsConfigAuthoring authoring)
            {
                Entity entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity, new SpawnCellsConfig()
                {
                    Width = authoring.width,
                    Height = authoring.height,
                    CellPrefabEntity = GetEntity(authoring.cellPrefab, TransformUsageFlags.Dynamic),
                });
            }
        }
    }

    public struct SpawnCellsConfig : IComponentData
    {
        public int Width;
        public int Height;
        public Entity CellPrefabEntity;
    }
}