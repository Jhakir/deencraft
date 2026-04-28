using System.Collections.Generic;
using UnityEngine;
using DeenCraft;

namespace DeenCraft.World
{
    /// <summary>
    /// Produces a greedy mesh from a ChunkData.
    /// Combines adjacent same-type faces into minimal quads, dramatically reducing
    /// vertex/triangle count versus naive face-per-voxel meshing.
    /// </summary>
    public static class ChunkMesher
    {
        private const int TotalBlockTypes = 20; // must match BlockType enum count

        // neighbors: [0]=+X, [1]=-X, [2]=+Z, [3]=-Z
        public static MeshData BuildMesh(ChunkData chunk, ChunkData[] neighbors)
        {
            var verts = new List<Vector3>();
            var tris  = new List<int>();
            var uvs   = new List<Vector2>();

            int[] dims = { GameConstants.ChunkWidth, GameConstants.ChunkHeight, GameConstants.ChunkDepth };

            for (int axis = 0; axis < 3; axis++)
            {
                int uAxis = (axis + 1) % 3;
                int vAxis = (axis + 2) % 3;

                var pos  = new int[3];
                var mask = new int[dims[uAxis] * dims[vAxis]];

                for (int backFace = 0; backFace <= 1; backFace++)
                {
                    bool isBack = (backFace == 1);

                    for (pos[axis] = -1; pos[axis] < dims[axis];)
                    {
                        pos[axis]++;

                        // Build mask for this slice
                        int n = 0;
                        for (pos[vAxis] = 0; pos[vAxis] < dims[vAxis]; pos[vAxis]++)
                        for (pos[uAxis] = 0; pos[uAxis] < dims[uAxis]; pos[uAxis]++, n++)
                        {
                            int aId = GetBlockId(chunk, neighbors, pos[0], pos[1], pos[2]);
                            int bId = GetBlockId(chunk, neighbors,
                                pos[0] + (axis == 0 ? 1 : 0),
                                pos[1] + (axis == 1 ? 1 : 0),
                                pos[2] + (axis == 2 ? 1 : 0));

                            bool aSolid = aId != (int)BlockType.Air;
                            bool bSolid = bId != (int)BlockType.Air;

                            if (!isBack)
                                mask[n] = (aSolid && !bSolid) ? aId : 0;
                            else
                                mask[n] = (!aSolid && bSolid) ? -bId : 0;
                        }

                        // Greedy-merge mask into quads
                        for (int j = 0; j < dims[vAxis]; j++)
                        {
                            for (int i = 0; i < dims[uAxis];)
                            {
                                int maskVal = mask[i + dims[uAxis] * j];
                                if (maskVal == 0) { i++; continue; }

                                // Width: how far we can extend along u
                                int w = 1;
                                while (i + w < dims[uAxis] &&
                                       mask[(i + w) + dims[uAxis] * j] == maskVal)
                                    w++;

                                // Height: how far we can extend along v
                                int h = 1;
                                bool done = false;
                                while (!done && j + h < dims[vAxis])
                                {
                                    for (int k = 0; k < w; k++)
                                        if (mask[(i + k) + dims[uAxis] * (j + h)] != maskVal)
                                        { done = true; break; }
                                    if (!done) h++;
                                }

                                // Build quad corners
                                var origin = new int[3];
                                origin[axis]  = pos[axis];
                                origin[uAxis] = i;
                                origin[vAxis] = j;

                                var du = new int[3];
                                var dv = new int[3];
                                du[uAxis] = w;
                                dv[vAxis] = h;

                                // Offset top-face origin along axis for positive direction
                                int axisOffset = isBack ? 0 : 1;

                                var v0 = new Vector3(origin[0] + (axis == 0 ? axisOffset : 0),
                                                     origin[1] + (axis == 1 ? axisOffset : 0),
                                                     origin[2] + (axis == 2 ? axisOffset : 0));
                                var v1 = v0 + new Vector3(du[0], du[1], du[2]);
                                var v2 = v0 + new Vector3(du[0] + dv[0], du[1] + dv[1], du[2] + dv[2]);
                                var v3 = v0 + new Vector3(dv[0], dv[1], dv[2]);

                                AddQuad(verts, tris, uvs, v0, v1, v2, v3,
                                    System.Math.Abs(maskVal), isBack);

                                // Zero out used cells
                                for (int l = 0; l < h; l++)
                                for (int k = 0; k < w; k++)
                                    mask[(i + k) + dims[uAxis] * (j + l)] = 0;

                                i += w;
                            }
                        }
                    }
                }
            }

            return new MeshData(verts.ToArray(), tris.ToArray(), uvs.ToArray());
        }

        private static void AddQuad(List<Vector3> verts, List<int> tris, List<Vector2> uvs,
            Vector3 v0, Vector3 v1, Vector3 v2, Vector3 v3,
            int blockId, bool flip)
        {
            int start = verts.Count;
            verts.Add(v0); verts.Add(v1); verts.Add(v2); verts.Add(v3);

            float uvY    = (float)blockId / TotalBlockTypes;
            float uvStep = 1f / TotalBlockTypes;
            uvs.Add(new Vector2(0f, uvY));
            uvs.Add(new Vector2(1f, uvY));
            uvs.Add(new Vector2(1f, uvY + uvStep));
            uvs.Add(new Vector2(0f, uvY + uvStep));

            if (!flip)
            {
                tris.Add(start);     tris.Add(start + 2); tris.Add(start + 1);
                tris.Add(start);     tris.Add(start + 3); tris.Add(start + 2);
            }
            else
            {
                tris.Add(start);     tris.Add(start + 1); tris.Add(start + 2);
                tris.Add(start);     tris.Add(start + 2); tris.Add(start + 3);
            }
        }

        private static int GetBlockId(ChunkData chunk, ChunkData[] neighbors,
            int x, int y, int z)
        {
            int w = GameConstants.ChunkWidth;
            int h = GameConstants.ChunkHeight;
            int d = GameConstants.ChunkDepth;

            if (chunk.IsInBounds(x, y, z))
                return (int)chunk.GetBlock(x, y, z);

            if (y < 0 || y >= h) return (int)BlockType.Air;
            if (neighbors == null) return (int)BlockType.Air;

            if (x >= w && neighbors.Length > 0 && neighbors[0] != null)
                return (int)neighbors[0].GetBlock(x - w, y, z);
            if (x < 0  && neighbors.Length > 1 && neighbors[1] != null)
                return (int)neighbors[1].GetBlock(x + w, y, z);
            if (z >= d && neighbors.Length > 2 && neighbors[2] != null)
                return (int)neighbors[2].GetBlock(x, y, z - d);
            if (z < 0  && neighbors.Length > 3 && neighbors[3] != null)
                return (int)neighbors[3].GetBlock(x, y, z + d);

            return (int)BlockType.Air;
        }
    }
}
