using UnityEngine;
using UnityEngine.Rendering;

namespace DeenCraft.World
{
    public sealed class CloudLayer : MonoBehaviour
    {
        private const int GridSize = 5;
        private const float Spacing = 80f;
        private const float Altitude = 155f;
        private const float DriftSpeed = 1.5f;
        private const float Extent = GridSize * Spacing;
        private static readonly Vector3 CloudScale = new Vector3(3f, 2f, 2f);

        [SerializeField] private Transform _playerTransform;
        [SerializeField] private Material _cloudMaterial;

        private readonly Transform[] _clouds = new Transform[GridSize * GridSize];
        private Mesh _mesh;
        private float _drift;

        private void Start()
        {
            if (_playerTransform == null || _cloudMaterial == null)
            {
                Debug.LogError("[CloudLayer] A player transform and cloud material must be assigned.", this);
                enabled = false;
                return;
            }

            var data = new ChunkData();
            for (int x = 1; x < 15; x++)
            for (int z = 2; z < 13; z++)
            {
                bool baseLayer = x >= 3 && x <= 12 || z >= 5 && z <= 9;
                if (baseLayer) data.SetBlock(x, 0, z, BlockType.Snow);
                if (x >= 5 && x <= 10 && z >= 4 && z <= 10)
                    data.SetBlock(x, 1, z, BlockType.Snow);
            }
            MeshData geometry = ChunkMesher.BuildMesh(data, null);
            _mesh = new Mesh
            {
                name = "Block cloud",
                vertices = geometry.Vertices,
                triangles = geometry.Triangles,
                colors32 = geometry.Colors
            };
            _mesh.RecalculateNormals();
            _mesh.RecalculateBounds();

            var properties = new MaterialPropertyBlock();
            properties.SetTexture("_MainTex", Texture2D.whiteTexture);
            for (int i = 0; i < _clouds.Length; i++)
            {
                var cloud = new GameObject("Cloud");
                cloud.transform.SetParent(transform, false);
                cloud.transform.localScale = CloudScale;
                cloud.AddComponent<MeshFilter>().sharedMesh = _mesh;
                var renderer = cloud.AddComponent<MeshRenderer>();
                renderer.sharedMaterial = _cloudMaterial;
                renderer.SetPropertyBlock(properties);
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = false;
                _clouds[i] = cloud.transform;
            }
            Advance(0f);
        }

        private void Update() => Advance(Time.deltaTime);

        private void Advance(float deltaTime)
        {
            _drift = Mathf.Repeat(_drift + deltaTime * DriftSpeed, Extent);
            Vector3 player = _playerTransform.position;
            for (int i = 0; i < _clouds.Length; i++)
            {
                float x = i % GridSize * Spacing + _drift;
                float z = i / GridSize * Spacing;
                x = player.x + Mathf.Repeat(x - player.x + Extent * 0.5f, Extent) - Extent * 0.5f;
                z = player.z + Mathf.Repeat(z - player.z + Extent * 0.5f, Extent) - Extent * 0.5f;
                _clouds[i].position = new Vector3(x, Altitude, z);
            }
        }

        private void OnDestroy()
        {
            if (_mesh != null) Destroy(_mesh);
        }
    }
}
