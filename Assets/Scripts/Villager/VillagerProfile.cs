// Assets/Scripts/Villager/VillagerProfile.cs
// Pure C# — no UnityEngine dependency.
// Names come from a hardcoded whitelist (no user input) — safe from injection.
using DeenCraft.Player;

namespace DeenCraft.Villager
{
    /// <summary>
    /// Data container for a villager's identity and trade list.
    /// Created at spawn time via <see cref="CreateDefault"/>.
    /// </summary>
    public sealed class VillagerProfile
    {
        // ── Arabic names whitelist ────────────────────────────────────────────
        // Hardcoded; never populated from user input.
        private static readonly string[] s_maleNames =
        {
            "Omar", "Yusuf", "Ibrahim", "Hassan", "Khalid",
            "Tariq", "Bilal", "Kareem", "Zayd", "Salim",
            "Nabil", "Rashid",
        };

        private static readonly string[] s_femaleNames =
        {
            "Fatimah", "Maryam", "Aisha", "Khadijah", "Zaynab",
            "Hana", "Layla", "Nour", "Rania", "Sana",
            "Amira", "Safiya",
        };

        // ── Properties ────────────────────────────────────────────────────────
        public string       Name   { get; }
        public TradeOffer[] Trades { get; }

        // ── Construction ──────────────────────────────────────────────────────
        public VillagerProfile(string name, TradeOffer[] trades)
        {
            Name   = name   ?? string.Empty;
            Trades = trades ?? System.Array.Empty<TradeOffer>();
        }

        // ── Factories ─────────────────────────────────────────────────────────

        /// <summary>Returns a random Arabic name from the hardcoded whitelist.</summary>
        public static string GetRandomName(System.Random rng = null)
        {
            rng = rng ?? new System.Random();
            // Pick from combined pool
            int total = s_maleNames.Length + s_femaleNames.Length;
            int idx   = rng.Next(total);
            return idx < s_maleNames.Length
                ? s_maleNames[idx]
                : s_femaleNames[idx - s_maleNames.Length];
        }

        /// <summary>
        /// Creates a villager with a random name and a default trade set.
        /// Default trades use Diamond and Gold so children learn their value.
        /// </summary>
        public static VillagerProfile CreateDefault(System.Random rng = null)
        {
            rng = rng ?? new System.Random();
            var trades = new TradeOffer[]
            {
                new TradeOffer(ItemId.Diamond, 1, ItemId.WoodPickaxe,  2),
                new TradeOffer(ItemId.Diamond, 2, ItemId.StonePickaxe, 1),
                new TradeOffer(ItemId.Gold,    3, ItemId.WoodAxe,      1),
                new TradeOffer(ItemId.Wheat,   5, ItemId.Bread,        2),
                new TradeOffer(ItemId.Wool,    3, ItemId.String,       2),
            };
            return new VillagerProfile(GetRandomName(rng), trades);
        }
    }
}
