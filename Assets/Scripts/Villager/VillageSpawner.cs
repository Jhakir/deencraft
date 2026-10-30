using System.Collections;
using System.Collections.Generic;
using DeenCraft.World;
using UnityEngine;

namespace DeenCraft.Villager
{
    public sealed class VillageSpawner : MonoBehaviour
    {
        private const int PoolSize = 12;
        private const int SearchRadius = 3;
        private const float RefreshSeconds = 0.5f;
        private const float Clearance = 0.02f;
        private const float LabelHeight = 2.4f;
        private const float LabelScale = 0.08f;
        private const float InteriorLightRange = 8f;
        private const float InteriorLightIntensity = 2.5f;

        [SerializeField] private Transform _playerTransform;
        [SerializeField] private ChunkManager _chunkManager;
        private readonly GameObject[] _houses = new GameObject[PoolSize];
        private readonly ChunkData[] _houseData = new ChunkData[PoolSize];
        private readonly Dictionary<Vector2Int, int> _assigned = new Dictionary<Vector2Int, int>();
        private readonly List<Material> _materials = new List<Material>();
        private Material _wood;
        private Material _cream;
        private Material _green;
        private Material _red;
        private Material _skin;
        private Material _dark;
        private Material _gold;

        private void Start()
        {
            if (_playerTransform == null || _chunkManager == null)
            {
                Debug.LogError("[VillageSpawner] Player and chunk manager must be assigned.", this);
                enabled = false;
                return;
            }
            CreatePool();
            StartCoroutine(PopulationLoop());
        }

        private Material Colour(Color colour)
        {
            Shader shader = Shader.Find("Standard");
            if (shader == null) throw new System.InvalidOperationException("Village models require the Standard shader.");
            var material = new Material(shader) { color = colour };
            _materials.Add(material);
            return material;
        }

        private void CreatePool()
        {
            _wood = Colour(new Color(0.45f, 0.26f, 0.12f));
            _cream = Colour(new Color(0.93f, 0.89f, 0.77f));
            _green = Colour(new Color(0.12f, 0.48f, 0.32f));
            _red = Colour(new Color(0.68f, 0.18f, 0.2f));
            _skin = Colour(new Color(0.65f, 0.41f, 0.25f));
            _dark = Colour(new Color(0.12f, 0.1f, 0.09f));
            _gold = Colour(new Color(0.9f, 0.68f, 0.18f));
            for (int i = 0; i < PoolSize; i++)
            {
                var house = new GameObject("Furnished village house");
                house.transform.SetParent(transform, false);
                house.SetActive(false);
                _houses[i] = house;
                BuildFurniture(house.transform);
                BuildVillager(house.transform, i);
            }
        }

        private void BuildFurniture(Transform house)
        {
            Box("Lantern", house, new Vector3(0f, 3.55f, 2f),
                new Vector3(0.25f, 0.3f, 0.25f), _gold, false);
            var lantern = Group("House light", house, new Vector3(0f, 3.3f, 2f));
            var light = lantern.gameObject.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(1f, 0.9f, 0.72f);
            light.range = InteriorLightRange;
            light.intensity = InteriorLightIntensity;
            light.shadows = LightShadows.None;
            light.renderMode = LightRenderMode.ForcePixel;
            var bed = Group("Bed", house, new Vector3(-2.5f, 0f, 2.5f));
            Box("Frame", bed, new Vector3(0f, 0.2f, 0f), new Vector3(1.3f, 0.4f, 1.8f), _wood);
            Box("Blanket", bed, new Vector3(0f, 0.5f, -0.15f), new Vector3(1.25f, 0.25f, 1.4f), _green);
            Box("Pillow", bed, new Vector3(0f, 0.6f, 0.65f), new Vector3(1.1f, 0.2f, 0.4f), _cream);

            var table = Group("Table", house, new Vector3(2f, 0f, 1.2f));
            Box("Top", table, new Vector3(0f, 0.9f, 0f), new Vector3(1.2f, 0.15f, 0.8f), _wood);
            Box("Base", table, new Vector3(0f, 0.45f, 0f), new Vector3(0.3f, 0.9f, 0.3f), _wood);
            var chair = Group("Chair", house, new Vector3(2f, 0f, 2.25f));
            Box("Seat", chair, new Vector3(0f, 0.45f, 0f), new Vector3(0.65f, 0.15f, 0.65f), _wood);
            Box("Legs", chair, new Vector3(0f, 0.2f, 0f), new Vector3(0.4f, 0.4f, 0.4f), _wood);
            Box("Back", chair, new Vector3(0f, 0.85f, 0.3f), new Vector3(0.65f, 0.8f, 0.1f), _wood);

            var chest = Group("Chest", house, new Vector3(-2.5f, 0f, 0.9f));
            Box("Storage chest", chest, new Vector3(0f, 0.4f, 0f), new Vector3(1f, 0.8f, 0.7f), _wood);
            Box("Latch", chest, new Vector3(0f, 0.4f, -0.36f), new Vector3(0.15f, 0.2f, 0.05f), _gold, false);
            var shelf = Group("Bookshelf", house, new Vector3(2f, 0f, 3f));
            Box("Cabinet", shelf, new Vector3(0f, 0.9f, 0f), new Vector3(1.4f, 1.8f, 0.45f), _wood);
            for (int row = 0; row < 2; row++)
            for (int book = 0; book < 5; book++)
                Box("Book", shelf, new Vector3(-0.5f + book * 0.25f, 0.55f + row * 0.7f, -0.25f),
                    new Vector3(0.17f, 0.5f, 0.08f), book % 2 == 0 ? _green : _red, false);
            var rug = Group("Rug", house, new Vector3(0f, Clearance, 2f));
            Box("Woven rug", rug, Vector3.zero, new Vector3(1.8f, 0.02f, 1.6f), _red, false);
            Box("Pattern", rug, new Vector3(0f, 0.015f, 0f), new Vector3(1.5f, 0.01f, 1.3f), _cream, false);
        }

        private void BuildVillager(Transform house, int index)
        {
            var root = Group("Villager", house, new Vector3(1f, 0f, -1f));
            var ai = root.gameObject.AddComponent<VillagerAI>();
            ai.SetProfile(VillagerProfile.CreateDefault(new System.Random(index + 42)));
            var collider = root.gameObject.AddComponent<BoxCollider>();
            collider.center = Vector3.up;
            collider.size = new Vector3(0.7f, 2f, 0.65f);
            Box("Robe", root, new Vector3(0f, 0.85f, 0f), new Vector3(0.65f, 1.2f, 0.5f), index % 2 == 0 ? _cream : _green, false);
            Box("Head", root, new Vector3(0f, 1.7f, 0f), new Vector3(0.55f, 0.55f, 0.55f), _skin, false);
            Box("Kufi", root, new Vector3(0f, 2f, 0f), new Vector3(0.6f, 0.15f, 0.6f), _cream, false);
            Box("Arms", root, new Vector3(0f, 1.05f, 0.35f), new Vector3(0.8f, 0.22f, 0.22f), _skin, false);
            for (int side = -1; side <= 1; side += 2)
            {
                Box("Shoe", root, new Vector3(side * 0.18f, 0.12f, 0.08f), new Vector3(0.25f, 0.24f, 0.5f), _dark, false);
                Box("Eye", root, new Vector3(side * 0.14f, 1.75f, 0.28f), new Vector3(0.07f, 0.07f, 0.025f), _dark, false);
            }
            var label = Group("Name", root, Vector3.up * LabelHeight);
            label.localScale = Vector3.one * LabelScale;
            var text = label.gameObject.AddComponent<TextMesh>();
            text.text = ai.Profile.Name + "\n[E] Trade";
            text.anchor = TextAnchor.MiddleCenter;
            text.alignment = TextAlignment.Center;
            text.fontSize = 32;
            text.color = Color.white;
        }

        private static Transform Group(string name, Transform parent, Vector3 position)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            return go.transform;
        }

        private static void Box(string name, Transform parent, Vector3 position, Vector3 scale,
            Material material, bool collision = true)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            go.transform.localScale = scale;
            go.GetComponent<MeshRenderer>().sharedMaterial = material;
            var collider = go.GetComponent<BoxCollider>();
            if (!collision)
            {
                collider.enabled = false;
                if (Application.isPlaying) Destroy(collider);
                else DestroyImmediate(collider);
            }
        }

        private IEnumerator PopulationLoop()
        {
            var wait = new WaitForSeconds(RefreshSeconds);
            while (true)
            {
                RefreshPopulation();
                yield return wait;
            }
        }

        private void RefreshPopulation()
        {
            Vector2Int centre = ChunkManager.WorldToChunkCoord(Vector3Int.FloorToInt(_playerTransform.position));
            var toRemove = new List<Vector2Int>();
            foreach (var pair in _assigned)
                if (Mathf.Abs(pair.Key.x - centre.x) > SearchRadius || Mathf.Abs(pair.Key.y - centre.y) > SearchRadius
                    || !_chunkManager.IsChunkReady(pair.Key)
                    || !ReferenceEquals(_chunkManager.GetChunkData(pair.Key), _houseData[pair.Value]))
                    toRemove.Add(pair.Key);
            foreach (var coord in toRemove)
            {
                _houses[_assigned[coord]].SetActive(false);
                _houseData[_assigned[coord]] = null;
                _assigned.Remove(coord);
            }
            for (int radius = 0; radius <= SearchRadius; radius++)
            for (int x = -radius; x <= radius; x++)
            for (int z = -radius; z <= radius; z++)
            {
                if (Mathf.Max(Mathf.Abs(x), Mathf.Abs(z)) != radius) continue;
                var coord = centre + new Vector2Int(x, z);
                if (_assigned.ContainsKey(coord) || !_chunkManager.IsChunkReady(coord)) continue;
                var data = _chunkManager.GetChunkData(coord);
                if (data == null || !data.HouseFloorY.HasValue) continue;
                int slot = System.Array.FindIndex(_houses, h => !h.activeSelf);
                if (slot < 0) return;
                float floor = data.HouseFloorY.Value + 1f;
                var house = _houses[slot];
                house.transform.position = new Vector3(coord.x * GameConstants.ChunkWidth + GameConstants.HouseAnchorX
                    + GameConstants.HouseWidth / 2 + 0.5f, floor, coord.y * GameConstants.ChunkDepth + GameConstants.HouseAnchorZ + 0.5f);
                var villager = house.GetComponentInChildren<VillagerAI>(true);
                Vector3 stand = house.transform.position + new Vector3(1f, 0f, -1f);
                int standX = GameConstants.HouseAnchorX + GameConstants.HouseWidth / 2 + 1;
                int standZ = GameConstants.HouseAnchorZ - 1;
                int floorY = data.HouseFloorY.Value;
                if (data.GetBlock(standX, floorY, standZ) == BlockType.Air
                    || data.GetBlock(standX, floorY + 1, standZ) != BlockType.Air
                    || data.GetBlock(standX, floorY + 2, standZ) != BlockType.Air) continue;
                villager.transform.position = stand;
                house.SetActive(true);
                _houseData[slot] = data;
                _assigned.Add(coord, slot);
            }
        }

        private void LateUpdate()
        {
            Camera camera = Camera.main;
            if (camera == null) return;
            foreach (var house in _houses)
            {
                if (house == null || !house.activeSelf) continue;
                var label = house.GetComponentInChildren<TextMesh>();
                label.transform.rotation = camera.transform.rotation;
            }
        }

        private void OnDestroy()
        {
            foreach (var material in _materials)
            {
                if (Application.isPlaying) Destroy(material);
                else DestroyImmediate(material);
            }
        }
    }
}
