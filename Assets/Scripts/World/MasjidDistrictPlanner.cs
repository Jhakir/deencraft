using System;
using UnityEngine;

namespace DeenCraft.World
{
    public static class MasjidDistrictPlanner
    {
        private static readonly Vector2Int[] CandidateOrigins =
        {
            new Vector2Int(32, -40), new Vector2Int(-112, -40),
            new Vector2Int(-40, 32), new Vector2Int(-40, -112)
        };

        public static MasjidDistrictLayout Create(int seed, Func<BoundsInt, bool> conflicts)
        {
            if (conflicts == null) throw new ArgumentNullException(nameof(conflicts));
            foreach (Vector2Int origin in CandidateOrigins)
            {
                var reserved = new BoundsInt(origin.x, 0, origin.y,
                    MasjidDistrictLayout.DistrictSize, GameConstants.ChunkHeight, MasjidDistrictLayout.DistrictSize);
                if (conflicts(reserved)) continue;
                int floor = 1;
                for (int x = reserved.xMin; x < reserved.xMax; x++)
                for (int z = reserved.zMin; z < reserved.zMax; z++)
                    floor = Mathf.Max(floor, WorldGenerator.GetTerrainSurfaceY(x, z, seed));
                if (floor + MasjidComplexLayout.StructureHeight > GameConstants.ChunkHeight) continue;
                return new MasjidDistrictLayout(origin, floor);
            }
            throw new InvalidOperationException("No safe nearby site is available for the masjid district.");
        }
    }
}
