using System.Collections;
using UnityEngine;
using DeenCraft.World;

namespace DeenCraft.Animals
{
    public sealed class AnimalSpawner : MonoBehaviour
    {
        private const int SpeciesCount = 5;
        private const int AnimalsPerSpecies = 3;
        private const int AttemptsPerAnimal = 8;
        private const float RefreshSeconds = 0.5f;
        private const float DespawnDistance = 70f;
        private const float SpawnMinDistance = 12f;
        private const float SpawnRingSpacing = 8f;
        private const int SpawnRingCount = 4;
        private const float SpawnAngle = 137.5f;
        private const float Clearance = 0.1f;
        private const float FootprintRadius = 0.6f;
        private const float MinimumAnimalSpacing = 3f;

        [SerializeField] private Transform _playerTransform;
        [SerializeField] private ChunkManager _chunkManager;
        [SerializeField] private Material _animalMaterial;

        private readonly AnimalController[] _animals = new AnimalController[SpeciesCount * AnimalsPerSpecies];
        private readonly Mesh[] _meshes = new Mesh[SpeciesCount];
        private int _spawnAttempt;

        private void Start()
        {
            if (_playerTransform == null || _chunkManager == null || _animalMaterial == null)
            {
                Debug.LogError("[AnimalSpawner] Player, chunk manager and animal material must be assigned.", this);
                enabled = false;
                return;
            }
            CreatePool();
            StartCoroutine(PopulationLoop());
        }

        private void CreatePool()
        {
            var properties = new MaterialPropertyBlock();
            properties.SetTexture("_MainTex", Texture2D.whiteTexture);
            for (int species = 0; species < SpeciesCount; species++)
            {
                Mesh mesh = BlockAnimalMesh.Build((AnimalType)species);
                _meshes[species] = mesh;
                for (int count = 0; count < AnimalsPerSpecies; count++)
                {
                    var animal = new GameObject(((AnimalType)species).ToString());
                    animal.SetActive(false);
                    animal.transform.SetParent(transform, false);
                    animal.AddComponent<MeshFilter>().sharedMesh = mesh;
                    var renderer = animal.AddComponent<MeshRenderer>();
                    renderer.sharedMaterial = _animalMaterial;
                    renderer.SetPropertyBlock(properties);
                    var collider = animal.AddComponent<CharacterController>();
                    collider.height = mesh.bounds.size.y;
                    collider.radius = Mathf.Min(mesh.bounds.size.x * 0.5f, collider.height * 0.5f);
                    collider.center = Vector3.up * collider.height * 0.5f;
                    collider.stepOffset = Mathf.Min(0.3f, collider.height * 0.4f);
                    var controller = animal.AddComponent<AnimalController>();
                    controller.Configure((AnimalType)species);
                    _animals[species * AnimalsPerSpecies + count] = controller;
                }
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
            foreach (var animal in _animals)
            {
                if (animal.gameObject.activeSelf)
                {
                    Vector3 delta = animal.transform.position - _playerTransform.position;
                    delta.y = 0f;
                    if (delta.sqrMagnitude <= DespawnDistance * DespawnDistance
                        && animal.transform.position.y >= 0f
                        && _chunkManager.IsChunkReady(ChunkManager.WorldToChunkCoord(
                            Vector3Int.FloorToInt(animal.transform.position))))
                        continue;
                    animal.gameObject.SetActive(false);
                }
                for (int attempt = 0; attempt < AttemptsPerAnimal; attempt++)
                {
                    int sample = _spawnAttempt++;
                    float angle = sample * SpawnAngle * Mathf.Deg2Rad;
                    float distance = SpawnMinDistance + sample % SpawnRingCount * SpawnRingSpacing;
                    Vector3 position = _playerTransform.position
                        + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * distance;
                    if (!TryFindGround(position, out Vector3 grounded)) continue;
                    bool occupied = false;
                    foreach (var other in _animals)
                    {
                        if (!other.gameObject.activeSelf) continue;
                        if ((other.transform.position - grounded).sqrMagnitude
                            < MinimumAnimalSpacing * MinimumAnimalSpacing)
                        {
                            occupied = true;
                            break;
                        }
                    }
                    if (occupied) continue;
                    animal.transform.position = grounded;
                    animal.gameObject.SetActive(true);
                    break;
                }
            }
        }

        private bool TryFindGround(Vector3 position, out Vector3 grounded)
        {
            grounded = position;
            if (!_chunkManager.TryGetSurfaceHeight(position.x, position.z, out float height))
                return false;
            int x = Mathf.FloorToInt(position.x);
            int z = Mathf.FloorToInt(position.z);
            var ground = _chunkManager.GetBlock(x, Mathf.RoundToInt(height) - 1, z);
            if (ground != BlockType.Grass && ground != BlockType.Dirt && ground != BlockType.Sand
                && ground != BlockType.Snow && ground != BlockType.Moss && ground != BlockType.Ice)
                return false;
            for (int dx = -1; dx <= 1; dx += 2)
            for (int dz = -1; dz <= 1; dz += 2)
            {
                if (!_chunkManager.TryGetSurfaceHeight(position.x + dx * FootprintRadius,
                    position.z + dz * FootprintRadius, out float cornerHeight)
                    || Mathf.Abs(cornerHeight - height) > Clearance)
                    return false;
            }
            grounded.y = height + Clearance;
            return true;
        }

        private void OnDestroy()
        {
            foreach (var mesh in _meshes)
                if (mesh != null) Destroy(mesh);
        }
    }
}
