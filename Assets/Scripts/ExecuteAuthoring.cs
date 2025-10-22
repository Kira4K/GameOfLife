using Unity.Entities;
using UnityEngine;

namespace GameOfLife.Execute
{
    public class ExecuteAuthoring : MonoBehaviour
    {
        [SerializeField] private bool mainThread;
        [SerializeField] private bool singleThreadedJob;
        [SerializeField] private bool parallelJob;

        private class Baker : Baker<ExecuteAuthoring>
        {
            public override void Bake(ExecuteAuthoring authoring)
            {
                Entity entity = GetEntity(TransformUsageFlags.None);
                AddComponent<Execute>(entity);
                if (authoring.mainThread) AddComponent<MainThread>(entity);
                if (authoring.singleThreadedJob) AddComponent<SingleThreadedJob>(entity);
                if (authoring.parallelJob) AddComponent<ParallelJob>(entity);
            }
        }
    }

    public struct Execute : IComponentData { }

    public struct MainThread : IComponentData { }

    public struct SingleThreadedJob : IComponentData { }

    public struct ParallelJob : IComponentData { }
}