using System;
using System.Collections.Generic;
using UnityEngine;

namespace DeenCraft.World
{
    public enum MasjidStyle { Saudi, Iraqi, Pakistani, Turkish }

    public sealed class MasjidComplexLayout
    {
        public const int PlotSize = 32;
        public const int StructureHeight = 24;
        public MasjidStyle Style { get; }
        public BoundsInt Bounds { get; }
        public int FloorY { get; }
        public BoundsInt EntranceClearance { get; }
        public BoundsInt HallClearance { get; }
        public BoundsInt LearningRoomClearance { get; }
        public IReadOnlyList<BoundsInt> CirculationClearances { get; }

        internal MasjidComplexLayout(MasjidStyle style, Vector2Int origin, int floorY)
        {
            Style = style;
            FloorY = floorY;
            Bounds = new BoundsInt(origin.x, 0, origin.y, PlotSize, floorY + StructureHeight, PlotSize);
            EntranceClearance = At(14, 1, 6, 3, 4, 2);
            HallClearance = At(6, 1, 17, 12, 6, 9);
            LearningRoomClearance = At(21, 1, 18, 5, 4, 7);
            CirculationClearances = Array.AsReadOnly(new[]
            {
                At(14, 1, 7, 3, 4, 19),
                At(14, 1, 15, 12, 4, 2),
                At(0, 1, 10, 15, 4, 3),
                At(16, 1, 10, 16, 4, 3)
            });
        }

        private BoundsInt At(int x, int y, int z, int width, int height, int depth) =>
            new BoundsInt(Bounds.xMin + x, FloorY + y, Bounds.zMin + z, width, height, depth);
    }

    public sealed class MasjidDistrictLayout
    {
        public const int CurrentVersion = 1;
        public const int DistrictSize = 80;
        public Vector2Int Origin { get; }
        public int FloorY { get; }
        public int Version => CurrentVersion;
        public BoundsInt Bounds { get; }
        public IReadOnlyList<MasjidComplexLayout> Complexes { get; }
        public float DistanceFromInitialSpawn { get; }
        public Vector3 ApproachStart { get; }
        public Vector3 ApproachEnd { get; }

        internal MasjidDistrictLayout(Vector2Int origin, int floorY, int seed)
        {
            if (floorY < 1 || floorY + MasjidComplexLayout.StructureHeight > GameConstants.ChunkHeight)
                throw new ArgumentOutOfRangeException(nameof(floorY));
            Origin = origin;
            FloorY = floorY;
            Bounds = new BoundsInt(origin.x, 0, origin.y, DistrictSize,
                floorY + MasjidComplexLayout.StructureHeight, DistrictSize);
            Complexes = Array.AsReadOnly(new[]
            {
                new MasjidComplexLayout(MasjidStyle.Saudi, origin, floorY),
                new MasjidComplexLayout(MasjidStyle.Iraqi, origin + new Vector2Int(48, 0), floorY),
                new MasjidComplexLayout(MasjidStyle.Pakistani, origin + new Vector2Int(0, 48), floorY),
                new MasjidComplexLayout(MasjidStyle.Turkish, origin + new Vector2Int(48, 48), floorY)
            });
            float nearestX = Mathf.Clamp(0, Bounds.xMin, Bounds.xMax);
            float nearestZ = Mathf.Clamp(0, Bounds.zMin, Bounds.zMax);
            DistanceFromInitialSpawn = new Vector2(nearestX, nearestZ).magnitude;
            ApproachStart = new Vector3(0f, WorldGenerator.GetTerrainSurfaceY(0, -4, seed) + 1.03f, -4f);
            ApproachEnd = new Vector3(nearestX, floorY + 1.03f, nearestZ);
        }

        public bool IntersectsChunk(Vector2Int coord)
        {
            int x = coord.x * GameConstants.ChunkWidth;
            int z = coord.y * GameConstants.ChunkDepth;
            return x < Bounds.xMax && x + GameConstants.ChunkWidth > Bounds.xMin &&
                z < Bounds.zMax && z + GameConstants.ChunkDepth > Bounds.zMin;
        }
    }
}
