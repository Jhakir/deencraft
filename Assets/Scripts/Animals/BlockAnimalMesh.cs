using System;
using System.Collections.Generic;
using UnityEngine;

namespace DeenCraft.Animals
{
    public static class BlockAnimalMesh
    {
        private static readonly Color32 Cream = new Color32(240, 235, 214, 255);
        private static readonly Color32 Brown = new Color32(122, 76, 43, 255);
        private static readonly Color32 Dark = new Color32(45, 37, 32, 255);
        private static readonly Color32 Orange = new Color32(204, 132, 65, 255);
        private static readonly Color32 Gold = new Color32(240, 180, 45, 255);
        private static readonly int[] Faces =
        {
            0, 1, 2, 3, 4, 7, 6, 5, 0, 4, 5, 1,
            3, 2, 6, 7, 0, 3, 7, 4, 1, 5, 6, 2
        };

        public static Mesh Build(AnimalType species)
        {
            Vector3 body;
            Vector3 head;
            float legHeight;
            Color32 coat;
            switch (species)
            {
                case AnimalType.Sheep:
                    body = new Vector3(0.9f, 0.65f, 1.15f);
                    head = new Vector3(0.45f, 0.45f, 0.5f);
                    legHeight = 0.4f;
                    coat = Cream;
                    break;
                case AnimalType.Cow:
                    body = new Vector3(0.95f, 0.8f, 1.5f);
                    head = new Vector3(0.6f, 0.6f, 0.6f);
                    legHeight = 0.5f;
                    coat = Cream;
                    break;
                case AnimalType.Horse:
                    body = new Vector3(0.85f, 0.8f, 1.6f);
                    head = new Vector3(0.45f, 0.65f, 0.75f);
                    legHeight = 0.8f;
                    coat = Brown;
                    break;
                case AnimalType.Cat:
                    body = new Vector3(0.4f, 0.35f, 0.8f);
                    head = new Vector3(0.4f, 0.35f, 0.4f);
                    legHeight = 0.25f;
                    coat = Orange;
                    break;
                case AnimalType.Chicken:
                    body = new Vector3(0.5f, 0.45f, 0.6f);
                    head = new Vector3(0.3f, 0.35f, 0.3f);
                    legHeight = 0.25f;
                    coat = Cream;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(species), species, "Unknown passive animal.");
            }

            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            var colours = new List<Color32>();
            Vector3 bodyCentre = new Vector3(0f, legHeight + body.y * 0.5f, 0f);
            Vector3 headCentre = new Vector3(0f, legHeight + body.y, body.z * 0.5f);
            AddBox(vertices, triangles, colours, bodyCentre, body, coat);
            AddBox(vertices, triangles, colours, headCentre, head,
                species == AnimalType.Sheep ? Brown : coat);

            int legCount = species == AnimalType.Chicken ? 2 : 4;
            for (int i = 0; i < legCount; i++)
            {
                float x = (i % 2 == 0 ? -1f : 1f) * body.x * 0.3f;
                float z = legCount == 2 ? 0f : (i / 2 == 0 ? -1f : 1f) * body.z * 0.3f;
                AddBox(vertices, triangles, colours, new Vector3(x, legHeight * 0.5f, z),
                    new Vector3(body.x * 0.2f, legHeight, body.x * 0.2f),
                    species == AnimalType.Chicken ? Gold : Dark);
            }
            for (int side = -1; side <= 1; side += 2)
            {
                AddBox(vertices, triangles, colours,
                    headCentre + new Vector3(side * head.x * 0.28f, head.y * 0.15f, head.z * 0.5f),
                    new Vector3(0.08f, 0.08f, 0.04f), Dark);
                if (species != AnimalType.Chicken)
                    AddBox(vertices, triangles, colours,
                        headCentre + new Vector3(side * head.x * 0.35f, head.y * 0.6f, 0f),
                        new Vector3(head.x * 0.2f, head.y * 0.35f, head.z * 0.25f), coat);
            }
            if (species == AnimalType.Chicken)
            {
                AddBox(vertices, triangles, colours, headCentre + Vector3.forward * head.z * 0.6f,
                    new Vector3(0.18f, 0.12f, 0.2f), Gold);
                AddBox(vertices, triangles, colours, headCentre + Vector3.up * head.y * 0.6f,
                    new Vector3(0.12f, 0.15f, 0.22f), new Color32(190, 45, 45, 255));
            }
            else
            {
                AddBox(vertices, triangles, colours,
                    bodyCentre + new Vector3(0f, 0f, -body.z * 0.65f),
                    new Vector3(body.x * 0.15f, body.y * 0.2f, body.z * 0.4f), Dark);
            }
            if (species == AnimalType.Cow)
                for (int side = -1; side <= 1; side += 2)
                    AddBox(vertices, triangles, colours, bodyCentre + Vector3.right * side * body.x * 0.5f,
                        new Vector3(0.04f, body.y * 0.55f, body.z * 0.4f), Dark);
            if (species == AnimalType.Horse)
                AddBox(vertices, triangles, colours, headCentre + Vector3.back * head.z * 0.4f,
                    new Vector3(head.x * 0.4f, head.y * 1.2f, head.z * 0.2f), Dark);

            var mesh = new Mesh
            {
                name = species + " block model",
                vertices = vertices.ToArray(),
                triangles = triangles.ToArray(),
                colors32 = colours.ToArray()
            };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static void AddBox(List<Vector3> vertices, List<int> triangles, List<Color32> colours,
            Vector3 centre, Vector3 size, Color32 colour)
        {
            Vector3 half = size * 0.5f;
            Vector3[] corners =
            {
                new Vector3(-half.x, -half.y, -half.z), new Vector3(half.x, -half.y, -half.z),
                new Vector3(half.x, -half.y, half.z), new Vector3(-half.x, -half.y, half.z),
                new Vector3(-half.x, half.y, -half.z), new Vector3(half.x, half.y, -half.z),
                new Vector3(half.x, half.y, half.z), new Vector3(-half.x, half.y, half.z)
            };
            for (int face = 0; face < Faces.Length; face += 4)
            {
                int start = vertices.Count;
                for (int i = 0; i < 4; i++)
                {
                    vertices.Add(centre + corners[Faces[face + i]]);
                    colours.Add(colour);
                }
                triangles.Add(start); triangles.Add(start + 1); triangles.Add(start + 2);
                triangles.Add(start); triangles.Add(start + 2); triangles.Add(start + 3);
            }
        }
    }
}
