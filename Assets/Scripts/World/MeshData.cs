using UnityEngine;

namespace DeenCraft.World
{
    /// <summary>
    /// Immutable container for chunk mesh geometry produced by ChunkMesher.
    /// </summary>
    public sealed class MeshData
    {
        public Vector3[] Vertices  { get; }
        public int[]     Triangles { get; }
        public Vector2[] UVs       { get; }
        public Color32[] Colors    { get; }

        public MeshData(Vector3[] vertices, int[] triangles, Vector2[] uvs, Color32[] colors = null)
        {
            Vertices  = vertices  ?? new Vector3[0];
            Triangles = triangles ?? new int[0];
            UVs       = uvs       ?? new Vector2[0];
            Colors    = colors    ?? new Color32[0];
        }

        public static readonly MeshData Empty =
            new MeshData(new Vector3[0], new int[0], new Vector2[0], new Color32[0]);
    }
}
