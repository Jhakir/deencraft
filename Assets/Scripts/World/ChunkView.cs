using UnityEngine;

namespace DeenCraft.World
{
    /// <summary>
    /// Lightweight MonoBehaviour that owns the MeshFilter + MeshRenderer for one chunk.
    /// Pulled from the ChunkManager pool; never instantiated or destroyed at runtime.
    /// </summary>
    [RequireComponent(typeof(MeshFilter))]
    [RequireComponent(typeof(MeshRenderer))]
    public sealed class ChunkView : MonoBehaviour
    {
        private MeshFilter _meshFilter;

        private void Awake()
        {
            _meshFilter = GetComponent<MeshFilter>();
        }

        public void ApplyMesh(MeshData data)
        {
            if (_meshFilter == null)
                _meshFilter = GetComponent<MeshFilter>();

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
            mesh.RecalculateNormals();
            _meshFilter.sharedMesh = mesh;
        }

        public void Clear()
        {
            if (_meshFilter != null && _meshFilter.sharedMesh != null)
                _meshFilter.sharedMesh.Clear();
            gameObject.SetActive(false);
        }
    }
}
