using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DeenCraft.World
{
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider))]
    public sealed class MasjidApproachPath : MonoBehaviour
    {
        private const float HalfWidth = 1.5f;
        [SerializeField] private ChunkManager _chunkManager;
        private readonly HashSet<Vector2Int> _supports = new HashSet<Vector2Int>();
        private Mesh _mesh;
        private Material _material;
        private TextMesh _label;
        private int _revision = -1;

        public static Mesh BuildMesh(MasjidDistrictLayout district)
        {
            Vector3 direction = district.ApproachEnd - district.ApproachStart;
            Vector3 side = Vector3.Cross(Vector3.up, direction).normalized * HalfWidth;
            var mesh = new Mesh { name = "Masjid district approach" };
            mesh.vertices = new[] { district.ApproachStart + side, district.ApproachStart - side,
                district.ApproachEnd + side, district.ApproachEnd - side };
            mesh.triangles = new[] { 0, 1, 2, 2, 1, 3 };
            mesh.uv = new[] { Vector2.zero, Vector2.right, Vector2.up, Vector2.one };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        public static void ClearTerrain(ChunkData data, Vector2Int coord, MasjidDistrictLayout district)
        {
            Vector3 start = district.ApproachStart;
            Vector3 end = district.ApproachEnd;
            var segment = new Vector2(end.x - start.x, end.z - start.z);
            for (int x = 0; x < GameConstants.ChunkWidth; x++)
            for (int z = 0; z < GameConstants.ChunkDepth; z++)
            {
                var offset = new Vector2(coord.x * GameConstants.ChunkWidth + x + 0.5f - start.x,
                    coord.y * GameConstants.ChunkDepth + z + 0.5f - start.z);
                float t = Vector2.Dot(offset, segment) / segment.sqrMagnitude;
                if (t < 0f || t > 1f || (offset - segment * t).sqrMagnitude > HalfWidth * HalfWidth) continue;
                int clearFrom = Mathf.FloorToInt(Mathf.Lerp(start.y, end.y, t));
                for (int y = clearFrom; y < GameConstants.ChunkHeight; y++)
                    data.SetBlock(x, y, z, BlockType.Air);
            }
        }

        private void Start()
        {
            if (_chunkManager == null)
            {
                Debug.LogError("[MasjidApproachPath] A chunk manager must be assigned.", this);
                enabled = false;
                return;
            }
            Shader shader = Shader.Find("Standard");
            if (shader == null) throw new System.InvalidOperationException("The masjid approach needs the Standard shader.");
            _material = new Material(shader) { color = new Color(0.75f, 0.69f, 0.55f) };
            GetComponent<MeshRenderer>().sharedMaterial = _material;
            GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            var label = new GameObject("Masjid discovery sign");
            label.transform.SetParent(transform, false);
            _label = label.AddComponent<TextMesh>();
            _label.text = "Masjid discovery district\nFollow the stone walkway";
            _label.fontSize = 32;
            _label.anchor = TextAnchor.MiddleCenter;
            label.transform.localScale = Vector3.one * 0.04f;
            _chunkManager.ChunkUnloading += Unloading;
            _chunkManager.WorldReset += Hide;
            StartCoroutine(RefreshLoop());
        }

        private IEnumerator RefreshLoop()
        {
            var delay = new WaitForSeconds(0.25f);
            while (true)
            {
                RefreshPath();
                yield return delay;
            }
        }

        private void RefreshPath()
        {
            MasjidDistrictLayout district = _chunkManager.DistrictLayout;
            if (district == null) { Hide(); return; }
            if (_revision != _chunkManager.WorldRevision)
            {
                Hide();
                if (_mesh != null) Destroy(_mesh);
                _mesh = BuildMesh(district);
                GetComponent<MeshFilter>().sharedMesh = _mesh;
                GetComponent<MeshCollider>().sharedMesh = _mesh;
                _label.transform.position = district.ApproachStart + new Vector3(-4f, 2.5f, -2f);
                _supports.Clear();
                Vector3 side = Vector3.Cross(Vector3.up, district.ApproachEnd - district.ApproachStart).normalized * HalfWidth;
                int steps = Mathf.CeilToInt(Vector3.Distance(district.ApproachStart, district.ApproachEnd));
                for (int i = 0; i <= steps; i++)
                {
                    Vector3 p = Vector3.Lerp(district.ApproachStart, district.ApproachEnd, (float)i / steps);
                    _supports.Add(ChunkManager.WorldToChunkCoord(Vector3Int.FloorToInt(p + side)));
                    _supports.Add(ChunkManager.WorldToChunkCoord(Vector3Int.FloorToInt(p - side)));
                }
                _revision = _chunkManager.WorldRevision;
            }
            foreach (Vector2Int support in _supports)
                if (!_chunkManager.IsChunkReady(support)) { Hide(); return; }
            GetComponent<MeshRenderer>().enabled = true;
            GetComponent<MeshCollider>().enabled = true;
            _label.gameObject.SetActive(true);
        }

        private void Hide()
        {
            GetComponent<MeshRenderer>().enabled = false;
            GetComponent<MeshCollider>().enabled = false;
            if (_label != null) _label.gameObject.SetActive(false);
        }

        private void Unloading(Vector2Int coord)
        {
            if (_supports.Contains(coord)) Hide();
        }

        private void LateUpdate()
        {
            if (_label != null && Camera.main != null) _label.transform.rotation = Camera.main.transform.rotation;
        }

        private void OnDestroy()
        {
            if (_chunkManager != null)
            {
                _chunkManager.ChunkUnloading -= Unloading;
                _chunkManager.WorldReset -= Hide;
            }
            if (_mesh != null) Destroy(_mesh);
            if (_material != null) Destroy(_material);
        }
    }
}
