using System.Collections;
using System.Collections.Generic;
using DeenCraft.World;
using UnityEngine;

namespace DeenCraft.Villager
{
    public sealed class MasjidDistrictSpawner : MonoBehaviour
    {
        private const float RefreshSeconds = 0.25f;
        private const int ComplexCount = 4;
        private const float PerformanceSampleSeconds = 60f;
        private const float MinimumMaterialLight = 0.18f;
        [SerializeField] private Transform _playerTransform;
        [SerializeField] private ChunkManager _chunkManager;
        private readonly GameObject[] _complexes = new GameObject[ComplexCount];
        private readonly TextMesh[][] _labels = new TextMesh[ComplexCount][];
        private readonly List<Material> _materials = new List<Material>();
        private readonly Dictionary<Vector2Int, ChunkData>[] _supports =
            new Dictionary<Vector2Int, ChunkData>[ComplexCount];
        private Material _wood;
        private Material _cream;
        private Material _green;
        private Material _blue;
        private Material _skin;
        private int _revision = -1;
        private float _sampleSeconds;
        private int _sampleFrames;
        private bool _performanceReported;

        private void Start()
        {
            if (_playerTransform == null || _chunkManager == null)
            {
                Debug.LogError("[MasjidDistrictSpawner] Player and chunk manager must be assigned.", this);
                enabled = false;
                return;
            }
            CreatePool();
            _chunkManager.ChunkUnloading += Unloading;
            _chunkManager.WorldReset += Deactivate;
            StartCoroutine(PopulationLoop());
        }

        private Material Colour(Color color)
        {
            Shader shader = Shader.Find("Standard");
            if (shader == null) throw new System.InvalidOperationException("Masjid content needs the Standard shader.");
            var material = new Material(shader) { color = color };
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", color * MinimumMaterialLight);
            _materials.Add(material);
            return material;
        }

        private void CreatePool()
        {
            _wood = Colour(new Color(0.35f, 0.22f, 0.12f));
            _cream = Colour(new Color(0.94f, 0.9f, 0.78f));
            _green = Colour(new Color(0.15f, 0.43f, 0.32f));
            _blue = Colour(new Color(0.2f, 0.43f, 0.65f));
            _skin = Colour(new Color(0.63f, 0.41f, 0.26f));
            for (int i = 0; i < ComplexCount; i++)
            {
                Transform root = Group(((MasjidStyle)i) + " masjid content", transform, Vector3.zero);
                root.gameObject.SetActive(false);
                _complexes[i] = root.gameObject;
                _supports[i] = new Dictionary<Vector2Int, ChunkData>();
                BuildFurniture(root, (MasjidStyle)i);
                BuildTeacher(root, (MasjidStyle)i);
                Board(root, "Architecture board", new Vector3(8f, 0f, 9f), (MasjidStyle)i, MasjidTopic.Architecture);
                Board(root, "Community board", new Vector3(24f, 0f, 9f), (MasjidStyle)i, MasjidTopic.Community);
                _labels[i] = root.GetComponentsInChildren<TextMesh>(true);
            }
        }

        private void BuildFurniture(Transform root, MasjidStyle style)
        {
            Transform rugs = Group("Prayer rugs", root, Vector3.zero);
            for (int row = 0; row < 3; row++)
            for (int col = 0; col < 5; col++)
            {
                Vector3 p = new Vector3(6.7f + col * 2.1f, 0.02f, 18f + row * 2.5f);
                Box("Rug", rugs, p, new Vector3(1.5f, 0.025f, 2f), style == MasjidStyle.Turkish ? _blue : _green, false);
                Box("Rug border", rugs, p + new Vector3(0f, 0.02f, 0.7f), new Vector3(1.15f, 0.02f, 0.12f), _cream, false);
            }
            Box("Bookshelf", root, new Vector3(26.4f, 1f, 23f), new Vector3(0.5f, 2f, 3f), _wood);
            for (int i = 0; i < 7; i++)
                Box("Book", root, new Vector3(26.08f, 1.3f, 21.8f + i * 0.4f),
                    new Vector3(0.18f, 0.65f, 0.25f), i % 2 == 0 ? _blue : _green, false);
            Box("Learning table", root, new Vector3(23.5f, 0.65f, 23f), new Vector3(1.6f, 0.12f, 1.6f), _wood);
            Box("Table base", root, new Vector3(23.5f, 0.3f, 23f), new Vector3(0.3f, 0.6f, 0.3f), _wood);
            for (int side = -1; side <= 1; side += 2)
                Box("Learning seat", root, new Vector3(23.5f + side * 1.25f, 0.25f, 23f),
                    new Vector3(0.55f, 0.5f, 0.7f), _wood);
            Box("Entrance shelves", root, new Vector3(22f, 0.45f, 7.3f), new Vector3(2f, 0.9f, 0.4f), _wood);
            Transform washing = Group("Washing area", root, new Vector3(28.5f, 0f, 19f));
            for (int i = 0; i < 3; i++)
            {
                Box("Basin", washing, new Vector3(0f, 0.5f, i * 1.7f), new Vector3(1.2f, 1f, 1.2f), _cream);
                Box("Decorative water", washing, new Vector3(0f, 1.01f, i * 1.7f), new Vector3(0.8f, 0.03f, 0.8f), _blue, false);
                Box("Tap", washing, new Vector3(0.4f, 1.2f, i * 1.7f), new Vector3(0.3f, 0.2f, 0.12f), _wood, false);
            }
            LightAt(root, new Vector3(12f, 5.3f, 21f));
            LightAt(root, new Vector3(23f, 4.8f, 21f));
        }

        private static void LightAt(Transform root, Vector3 position)
        {
            var light = Group("Masjid lantern", root, position).gameObject.AddComponent<Light>();
            light.type = LightType.Point;
            light.range = 17f;
            light.intensity = 2.5f;
            light.color = new Color(1f, 0.9f, 0.74f);
            light.shadows = LightShadows.None;
            light.renderMode = LightRenderMode.ForcePixel;
        }

        private void BuildTeacher(Transform root, MasjidStyle style)
        {
            Transform teacher = Group("Teacher", root, new Vector3(21f, 0f, 13f));
            string name = VillagerProfile.GetRandomName(new System.Random(100 + (int)style));
            var explorer = teacher.gameObject.AddComponent<MasjidExplorer>();
            explorer.Configure(style, MasjidTopic.Welcome, "Welcome from " + name);
            var hitbox = teacher.gameObject.AddComponent<BoxCollider>();
            hitbox.center = Vector3.up;
            hitbox.size = new Vector3(0.7f, 2f, 0.7f);
            Box("Robe", teacher, new Vector3(0f, 0.8f, 0f), new Vector3(0.65f, 1.2f, 0.5f), _cream, false);
            Box("Head", teacher, new Vector3(0f, 1.65f, 0f), Vector3.one * 0.55f, _skin, false);
            Box("Kufi", teacher, new Vector3(0f, 1.98f, 0f), new Vector3(0.6f, 0.15f, 0.6f), _green, false);
            Box("Arms", teacher, new Vector3(0f, 1.05f, 0.3f), new Vector3(0.8f, 0.2f, 0.2f), _skin, false);
            Label(teacher, name + "\n[E] Welcome", new Vector3(0f, 2.6f, 0f), 0.07f);
            Label(root, MasjidContentCatalog.StyleName(style), new Vector3(15.5f, 5.6f, 5.8f), 0.09f);
        }

        private void Board(Transform root, string name, Vector3 position, MasjidStyle style, MasjidTopic topic)
        {
            Transform board = Group(name, root, position);
            Box("Board panel", board, new Vector3(0f, 1.3f, 0f), new Vector3(2.4f, 1.6f, 0.2f), _wood);
            Box("Board leg", board, new Vector3(0f, 0.5f, 0f), new Vector3(0.2f, 1f, 0.2f), _wood);
            var explorer = board.gameObject.AddComponent<MasjidExplorer>();
            explorer.Configure(style, topic, topic == MasjidTopic.Architecture ?
                MasjidContentCatalog.StyleName(style) : "Learning and community");
            Label(board, (topic == MasjidTopic.Architecture ? "Architecture" : "Community") +
                "\n[E] Read", new Vector3(0f, 2.5f, 0f), 0.065f);
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
            if (!collision)
            {
                var collider = go.GetComponent<Collider>();
                collider.enabled = false;
                if (Application.isPlaying) Destroy(collider);
                else DestroyImmediate(collider);
            }
        }

        private static void Label(Transform parent, string value, Vector3 position, float scale)
        {
            var label = Group("Exploration label", parent, position).gameObject.AddComponent<TextMesh>();
            label.text = value;
            label.anchor = TextAnchor.MiddleCenter;
            label.alignment = TextAlignment.Center;
            label.fontSize = 32;
            label.color = Color.white;
            label.transform.localScale = Vector3.one * scale;
        }

        private IEnumerator PopulationLoop()
        {
            var delay = new WaitForSeconds(RefreshSeconds);
            while (true)
            {
                RefreshPopulation();
                yield return delay;
            }
        }

        private void RefreshPopulation()
        {
            if (_revision != _chunkManager.WorldRevision)
            {
                Deactivate();
                _revision = _chunkManager.WorldRevision;
                _sampleSeconds = 0f;
                _sampleFrames = 0;
                _performanceReported = false;
            }
            MasjidDistrictLayout district = _chunkManager.DistrictLayout;
            if (district == null) { Deactivate(); return; }
            for (int i = 0; i < ComplexCount; i++)
            {
                MasjidComplexLayout complex = district.Complexes[i];
                Vector2Int min = ChunkManager.WorldToChunkCoord(complex.Bounds.min);
                Vector2Int max = ChunkManager.WorldToChunkCoord(complex.Bounds.max - Vector3Int.one);
                bool ready = true;
                bool replaced = false;
                for (int x = min.x; x <= max.x; x++)
                for (int z = min.y; z <= max.y; z++)
                {
                    var coord = new Vector2Int(x, z);
                    if (!_chunkManager.IsChunkReady(coord)) ready = false;
                    if (_supports[i].TryGetValue(coord, out ChunkData previous) &&
                        !ReferenceEquals(previous, _chunkManager.GetChunkData(coord))) replaced = true;
                }
                if (!ready || replaced)
                {
                    _complexes[i].SetActive(false);
                    _supports[i].Clear();
                    if (!ready) continue;
                }
                if (_complexes[i].activeSelf) continue;
                for (int x = min.x; x <= max.x; x++)
                for (int z = min.y; z <= max.y; z++)
                {
                    var coord = new Vector2Int(x, z);
                    _supports[i][coord] = _chunkManager.GetChunkData(coord);
                }
                _complexes[i].transform.position = new Vector3(complex.Bounds.xMin, complex.FloorY + 1f, complex.Bounds.zMin);
                _complexes[i].SetActive(true);
            }
        }

        private void Unloading(Vector2Int coord)
        {
            for (int i = 0; i < ComplexCount; i++)
                if (_supports[i].ContainsKey(coord))
                {
                    _complexes[i].SetActive(false);
                    _supports[i].Clear();
                }
        }

        private void Deactivate()
        {
            for (int i = 0; i < ComplexCount; i++)
            {
                if (_complexes[i] != null) _complexes[i].SetActive(false);
                _supports[i]?.Clear();
            }
        }

        private void LateUpdate()
        {
            ObservePerformance();
            Camera camera = Camera.main;
            if (camera == null) return;
            for (int i = 0; i < ComplexCount; i++)
            {
                if (_complexes[i] == null || !_complexes[i].activeSelf) continue;
                foreach (TextMesh label in _labels[i])
                    label.transform.rotation = camera.transform.rotation;
            }
        }

        private void ObservePerformance()
        {
            if (_performanceReported || _chunkManager == null || _playerTransform == null) return;
            MasjidDistrictLayout district = _chunkManager.DistrictLayout;
            if (district == null || !district.Bounds.Contains(Vector3Int.FloorToInt(_playerTransform.position)))
            {
                _sampleSeconds = 0f;
                _sampleFrames = 0;
                return;
            }
            _sampleSeconds += Time.unscaledDeltaTime;
            _sampleFrames++;
            if (_sampleSeconds < PerformanceSampleSeconds) return;
            _performanceReported = true;
            Debug.Log($"[MasjidDistrict] Performance: {_sampleFrames / _sampleSeconds:F1} FPS over " +
                $"{_sampleSeconds:F1}s; {Screen.width}x{Screen.height}; " +
                $"{SystemInfo.deviceModel}; {SystemInfo.graphicsDeviceName}; {Application.platform}.", this);
        }

        private void OnDestroy()
        {
            if (_chunkManager != null)
            {
                _chunkManager.ChunkUnloading -= Unloading;
                _chunkManager.WorldReset -= Deactivate;
            }
            foreach (Material material in _materials)
            {
                if (Application.isPlaying) Destroy(material);
                else DestroyImmediate(material);
            }
        }
    }
}
