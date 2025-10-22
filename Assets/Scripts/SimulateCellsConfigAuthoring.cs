using Unity.Entities;
using UnityEngine;

namespace GameOfLife
{
    public class SimulateCellsConfigAuthoring : MonoBehaviour
    {
        [SerializeField] private bool isEnabled;
        [Range(0f, 1f)]
        [SerializeField] private float tickDuration;

        private class Baker : Baker<SimulateCellsConfigAuthoring>
        {
            public override void Bake(SimulateCellsConfigAuthoring authoring)
            {
                Entity entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity, new SimulateCellsConfig()
                {
                    IsEnabled = authoring.isEnabled,
                    TickDuration = authoring.tickDuration,
                });
            }
        }
    }

    public struct SimulateCellsConfig : IComponentData
    {
        public bool IsEnabled;
        public float TickDuration;
        public float ElapsedTime;
    }
}