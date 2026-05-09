using UnityEngine;

namespace DeenCraft.World
{
    /// <summary>
    /// Lightweight MonoBehaviour that owns the MeshFilter + MeshRenderer for one chunk.
    /// Pulled from the ChunkManager pool; never instantiated or destroyed at runtime.
    /// </summary>
    [RequireComponent(typeof(MeshFilter))]
    [RequireComponent(typeof(MeshRenderer))]
    [RequireComponent(typeof(MeshCollider))]
    public sealed class ChunkView : MonoBehaviour
    {
        private MeshFilter   _meshFilter;
        private MeshCollider _meshCollider;

        private void Awake()
        {
            _meshFilter   = GetComponent<MeshFilter>();
            _meshCollider = GetComponent<MeshCollider>();
        }

        public void ApplyMesh(MeshData data)
        {
            if (_meshFilter   == null) _meshFilter   = GetComponent<MeshFilter>();
            if (_meshCollider == null) _meshCollider = GetComponent<MeshCollider>();

            var mesh = _meshFilter.sharedMesh;
            if (mesh == null)
            {
                mesh = new Mesh();
                mesh.name = "ChunkMesh";
            }

            mesh.Clear();
            mesh.vertices  = data.Vertices;
            mesh.triangles = data.Triangles;
            mesh.uv        = data.UVs;
            if (data.Colors != null && data.Colors.Length == data.Vertices.Length)
                mesh.colors32 = data.Colors;
            mesh.RecalculateNormals();
            _meshFilter.sharedMesh  = mesh;
            _meshCollider.sharedMesh = mesh; // player can stand on terrain
        }

        public void Clear()
        {
            if (_meshFilter != null && _meshFilter.sharedMesh != null)
                _meshFilter.sharedMesh.Clear();
            gameObject.SetActive(false);
        }
    }
}
