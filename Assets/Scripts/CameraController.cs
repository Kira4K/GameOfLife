using Unity.Entities;
using UnityEngine;

namespace GameOfLife
{
    [RequireComponent(typeof(Camera))]
    public class CameraController : MonoBehaviour
    {
        private Camera _camera;
        private EntityQuery entityQuery;

        private void Awake()
        {
            _camera = GetComponent<Camera>();
            entityQuery = World.DefaultGameObjectInjectionWorld.EntityManager.CreateEntityQuery(ComponentType.ReadOnly<SpawnCellsConfig>());
        }

        public void UpdatePosition()
        {
            if (entityQuery.TryGetSingleton<SpawnCellsConfig>(out var config))
            {
                transform.position = new Vector3((config.Width - 1) / 2f, 2f, (config.Height - 1) / 2f);
                _camera.orthographicSize = config.Height / 2f;
            }
        }
    }
}