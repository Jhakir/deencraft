using UnityEngine;

namespace DeenCraft.World
{
    public enum BlockEditResult { Success, Protected, ChunkUnavailable, OutOfBounds }

    public static class MasjidDistrictProtection
    {
        public static bool Contains(MasjidDistrictLayout layout, Vector3Int position)
        {
            if (layout == null) return false;
            foreach (MasjidComplexLayout complex in layout.Complexes)
                if (complex.Bounds.Contains(position)) return true;
            return false;
        }
    }
}
