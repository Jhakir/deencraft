using System;
using UnityEngine;

namespace DeenCraft.World
{
    public static class MasjidStructureGenerator
    {
        private const int WallHeight = 7;
        private const int Front = 6;
        private const int Back = 27;
        private const int Left = 4;
        private const int Right = 27;

        public static void Apply(ChunkData chunk, Vector2Int coord, MasjidDistrictLayout layout)
        {
            if (chunk == null) throw new ArgumentNullException(nameof(chunk));
            if (layout == null) throw new ArgumentNullException(nameof(layout));
            if (!layout.IntersectsChunk(coord)) return;
            int baseX = coord.x * GameConstants.ChunkWidth;
            int baseZ = coord.y * GameConstants.ChunkDepth;
            for (int x = 0; x < GameConstants.ChunkWidth; x++)
            for (int z = 0; z < GameConstants.ChunkDepth; z++)
            {
                int wx = baseX + x;
                int wz = baseZ + z;
                if (wx < layout.Bounds.xMin || wx >= layout.Bounds.xMax ||
                    wz < layout.Bounds.zMin || wz >= layout.Bounds.zMax) continue;
                int dx = wx - layout.Origin.x;
                int dz = wz - layout.Origin.y;
                bool path = dx >= 38 && dx <= 41 || dz >= 38 && dz <= 41 ||
                    dz >= 10 && dz <= 12 || dz >= 58 && dz <= 60;
                for (int y = 1; y < GameConstants.ChunkHeight; y++)
                    chunk.SetBlock(x, y, z, y < layout.FloorY ? BlockType.Stone :
                        y == layout.FloorY ? (path ? BlockType.StoneArch : BlockType.Grass) : BlockType.Air);
                bool fountain = dx >= 35 && dx <= 44 && dz >= 35 && dz <= 44;
                if (fountain)
                {
                    bool rim = dx == 35 || dx == 44 || dz == 35 || dz == 44;
                    chunk.SetBlock(x, layout.FloorY, z, rim ? BlockType.StoneArch : BlockType.Water);
                }
                foreach (MasjidComplexLayout complex in layout.Complexes)
                {
                    int lx = wx - complex.Bounds.xMin;
                    int lz = wz - complex.Bounds.zMin;
                    if (lx < 0 || lx >= MasjidComplexLayout.PlotSize ||
                        lz < 0 || lz >= MasjidComplexLayout.PlotSize) continue;
                    BuildColumn(chunk, x, z, lx, lz, complex);
                }
            }
        }

        private static void BuildColumn(ChunkData chunk, int x, int z, int lx, int lz,
            MasjidComplexLayout complex)
        {
            int floor = complex.FloorY;
            bool building = lx >= Left && lx <= Right && lz >= Front && lz <= Back;
            bool walkway = lx >= 14 && lx <= 16 || lz >= 10 && lz <= 12;
            if (building || walkway)
                chunk.SetBlock(x, floor, z, BlockType.Mosque);
            for (int y = 1; y < MasjidComplexLayout.StructureHeight; y++)
            {
                BlockType block = BlockType.Air;
                if (building)
                {
                    bool perimeter = lx == Left || lx == Right || lz == Front || lz == Back;
                    bool courtyard = lx >= 7 && lx <= 24 && lz >= 8 && lz <= 14;
                    bool partition = lx == 19 && lz >= 17 && lz <= 26;
                    if (y <= WallHeight && (perimeter || partition))
                        block = WallMaterial(complex.Style, lx, y, lz);
                    if (y == WallHeight && !courtyard)
                        block = WallMaterial(complex.Style, lx, y, lz);
                    bool arcadeColumn = (lx == 6 || lx == 25) && (lz == 8 || lz == 14);
                    if (arcadeColumn && y <= 6)
                        block = WallMaterial(complex.Style, lx, y, lz);
                    bool window = (lx == Left || lx == Right) &&
                        (lz == 18 || lz == 19 || lz == 23 || lz == 24);
                    if (window && y >= 3 && y <= 5) block = BlockType.Air;
                    if (partition && lz >= 19 && lz <= 21 && y <= 4) block = BlockType.Air;
                    if (lx >= 10 && lx <= 12 && lz == Back && y <= 4)
                        block = BlockType.Air;
                }
                if (y > WallHeight && DomeAt(complex.Style, lx, y, lz))
                    block = complex.Style == MasjidStyle.Pakistani ? BlockType.Snow :
                        complex.Style == MasjidStyle.Turkish ? BlockType.Stone : BlockType.Dome;
                bool tower = lx >= 4 && lx <= 6 && lz >= 4 && lz <= 6;
                if (complex.Style == MasjidStyle.Turkish || complex.Style == MasjidStyle.Pakistani)
                    tower |= lx >= 25 && lx <= 27 && lz >= 4 && lz <= 6;
                int towerHeight = complex.Style == MasjidStyle.Saudi ? 15 :
                    complex.Style == MasjidStyle.Iraqi ? 18 : 21;
                if (tower && y <= towerHeight)
                    block = y == towerHeight ? BlockType.Dome : BlockType.Minaret;
                if (complex.Style == MasjidStyle.Pakistani && lz == Front &&
                    lx >= 11 && lx <= 19 && y >= 8 && y <= 10)
                    block = y == 10 || lx == 11 || lx == 19 ? BlockType.Snow : BlockType.MudBrick;
                var world = new Vector3Int(complex.Bounds.xMin + lx, floor + y, complex.Bounds.zMin + lz);
                if (complex.EntranceClearance.Contains(world) ||
                    complex.HallClearance.Contains(world) || complex.LearningRoomClearance.Contains(world))
                    block = BlockType.Air;
                foreach (BoundsInt clearance in complex.CirculationClearances)
                    if (clearance.Contains(world)) block = BlockType.Air;
                chunk.SetBlock(x, floor + y, z, block);
            }
            // Back the mihrab recess with a small exterior niche.
            if (lx >= 9 && lx <= 13 && lz == 28)
                for (int y = 1; y <= 5; y++)
                    chunk.SetBlock(x, floor + y, z, WallMaterial(complex.Style, lx, y, lz));
        }

        private static BlockType WallMaterial(MasjidStyle style, int x, int y, int z)
        {
            switch (style)
            {
                case MasjidStyle.Saudi: return y == WallHeight ? BlockType.StoneArch : BlockType.Mosque;
                case MasjidStyle.Iraqi: return (x + z + y) % 5 == 0 ? BlockType.StoneArch : BlockType.MudBrick;
                case MasjidStyle.Pakistani: return y == 1 || y == WallHeight || x % 6 == 0
                    ? BlockType.Snow : BlockType.MudBrick;
                case MasjidStyle.Turkish: return y == 2 ? BlockType.WaterSlide : BlockType.StoneArch;
                default: throw new ArgumentOutOfRangeException(nameof(style));
            }
        }

        private static bool DomeAt(MasjidStyle style, int x, int y, int z)
        {
            switch (style)
            {
                case MasjidStyle.Saudi: return false;
                case MasjidStyle.Iraqi: return Shell(x, y, z, 12, 21, 4);
                case MasjidStyle.Pakistani:
                    return Shell(x, y, z, 10, 21, 4) || Shell(x, y, z, 18, 21, 3) ||
                        Shell(x, y, z, 24, 21, 3);
                case MasjidStyle.Turkish:
                    return Shell(x, y, z, 12, 21, 6) || Shell(x, y, z, 23, 21, 3) ||
                        Shell(x, y, z, 12, 13, 3);
                default: throw new ArgumentOutOfRangeException(nameof(style));
            }
        }

        private static bool Shell(int x, int y, int z, int cx, int cz, int radius)
        {
            int dy = y - WallHeight;
            int distance = (x - cx) * (x - cx) + dy * dy + (z - cz) * (z - cz);
            return dy > 0 && distance <= radius * radius && distance >= (radius - 1) * (radius - 1);
        }
    }
}
