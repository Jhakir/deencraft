using System;
using DeenCraft.World;

namespace DeenCraft.Villager
{
    public enum MasjidTopic { Architecture, Community, Welcome }

    public static class MasjidContentCatalog
    {
        public static string StyleName(MasjidStyle style)
        {
            switch (style)
            {
                case MasjidStyle.Saudi: return "Saudi-inspired courtyard masjid";
                case MasjidStyle.Iraqi: return "Iraqi-inspired brick masjid";
                case MasjidStyle.Pakistani: return "Pakistani-inspired domed masjid";
                case MasjidStyle.Turkish: return "Turkish-inspired domed masjid";
                default: throw new ArgumentOutOfRangeException(nameof(style));
            }
        }

        public static string Body(MasjidStyle style, MasjidTopic topic)
        {
            string introduction = "Welcome! Everyone is invited to explore respectfully. " +
                "A masjid is a place for Muslim prayer, learning, and community.\n\n";
            if (topic == MasjidTopic.Welcome)
                return introduction + "I am a teacher here. Look around the prayer hall, courtyard, and learning room. " +
                    "The information boards introduce this building and our shared spaces.\n\n" +
                    "This is an exploration visit. Playable lectures, classes, and guided Salah are not available yet.";
            if (topic == MasjidTopic.Community)
                return introduction + "In the prayer hall, the rugs form rows. The mihrab is a niche that marks " +
                    "the prayer direction. Here all four buildings use the same fictional world direction; " +
                    "this is not a real-world qibla calculator.\n\n" +
                    "The learning room has books and places to sit together. Masjids can host teaching and " +
                    "community gatherings. These spaces vary between communities.\n\n" +
                    "Visitors can speak quietly, keep the space clean, and ask their host about local customs. " +
                    "Entrance shelves and the washing area are decorative in this game.\n\n" +
                    "There are no points, prizes, or penalties for worship.";
            if (topic != MasjidTopic.Architecture) throw new ArgumentOutOfRangeException(nameof(topic));
            string architecture;
            switch (style)
            {
                case MasjidStyle.Saudi:
                    architecture = "This original design takes selected inspiration from Najdi architecture in " +
                        "central Saudi Arabia: earth-toned walls, geometric details, and a shaded courtyard. " +
                        "At-Turaif in ad-Dir'iyah is a heritage reference for Najdi building traditions.";
                    break;
                case MasjidStyle.Iraqi:
                    architecture = "This original design celebrates brickwork, repeated arches, and patterned walls. " +
                        "Brick construction is part of Iraq's rich architectural history. Samarra is one heritage " +
                        "reference for Abbasid mosque planning and distinctive minarets. This building is not a copy of Samarra.";
                    break;
                case MasjidStyle.Pakistani:
                    architecture = "This original design takes selected inspiration from Mughal mosque architecture " +
                        "in Pakistan, with a red-and-pale palette, a framed gateway, and grouped domes. " +
                        "Lahore's Badshahi Mosque is one architectural reference, not a building reproduced here.";
                    break;
                case MasjidStyle.Turkish:
                    architecture = "This original design takes selected inspiration from Ottoman mosque complexes " +
                        "in Turkey: a large central dome, lower domes, slender minarets, and cool-colored accents. " +
                        "Istanbul's historic mosque skyline is one heritage reference.";
                    break;
                default: throw new ArgumentOutOfRangeException(nameof(style));
            }
            return architecture + "\n\nEvery country has many architectural traditions. These are selected " +
                "inspirations, not a single style representing everyone.\n\nWalk through the hall and learning " +
                "room, compare the roof shapes, and visit the other three masjids around the garden.";
        }
    }
}
