// Assets/Scripts/World/WorldSaveManager.cs
// Manages the world save/load lifecycle for the active child's session.
// Attach to a persistent GameObject alongside FirebaseAuthManager.
//
// Workflow:
//   NewWorldAsync(name)   → called by UI "New World" button
//   LoadWorldAsync(saveId) → called by UI when child picks a save slot
//   SaveAsync()           → called by auto-save or pause menu "Save"
//   SaveAndQuitAsync()    → called by pause menu "Save & Quit"
using System;
using System.Threading.Tasks;
using UnityEngine;
using DeenCraft;
using DeenCraft.Auth;
using DeenCraft.Auth.Models;
using DeenCraft.Player;

namespace DeenCraft.World
{
    /// <summary>
    /// Coordinates world save/load between <see cref="FirebaseAuthManager"/>,
    /// <see cref="ChunkManager"/>, and <see cref="PlayerController"/>.
    ///
    /// Attach to the same persistent GameObject as <see cref="FirebaseAuthManager"/>.
    /// Wire <see cref="_chunkManager"/>, <see cref="_playerController"/>, and
    /// <see cref="_characterCustomizer"/> in the Inspector.
    /// </summary>
    [RequireComponent(typeof(FirebaseAuthManager))]
    public sealed class WorldSaveManager : MonoBehaviour
    {
        // ── Inspector ────────────────────────────────────────────────────────
        [SerializeField] private ChunkManager        _chunkManager;
        [SerializeField] private PlayerController    _playerController;
        [SerializeField] private CharacterCustomizer _characterCustomizer;

        // ── State ────────────────────────────────────────────────────────────
        /// <summary>The world save currently in use. Null when no world is loaded.</summary>
        public WorldSaveData ActiveSave   { get; private set; }

        /// <summary>True when a world has been loaded/created this session.</summary>
        public bool IsWorldLoaded         { get; private set; }

        // ── Events ───────────────────────────────────────────────────────────
        public event Action<WorldSaveData> OnWorldLoaded;
        public event Action                OnWorldSaved;
        public event Action<string>        OnError;

        // ── Public API ───────────────────────────────────────────────────────

        /// <summary>
        /// Creates a fresh world save for the active child with a random seed,
        /// then applies it to the chunk manager and player.
        /// </summary>
        public async Task NewWorldAsync(string worldName)
        {
            if (!SessionManager.IsChildActive)
            {
                NotifyError("No active child profile — cannot create world.");
                return;
            }
            if (string.IsNullOrWhiteSpace(worldName))
                worldName = "My World";

            int seed = UnityEngine.Random.Range(int.MinValue, int.MaxValue);
            var saveData = new WorldSaveData(SessionManager.ActiveChild.id, seed, worldName);

            try
            {
                await FirebaseAuthManager.Instance.SaveWorldAsync(saveData);
                ActiveSave = saveData;
                ApplyWorldState(saveData);
                IsWorldLoaded = true;
                OnWorldLoaded?.Invoke(saveData);
            }
            catch (Exception ex) { NotifyError($"Failed to create world: {ex.Message}"); }
        }

        /// <summary>
        /// Loads an existing world save by ID and restores player position + appearance.
        /// </summary>
        public async Task LoadWorldAsync(string saveId)
        {
            if (!SessionManager.IsChildActive)
            {
                NotifyError("No active child profile — cannot load world.");
                return;
            }
            try
            {
                var saveData = await FirebaseAuthManager.Instance.LoadWorldSaveAsync(saveId);
                if (saveData == null || !saveData.IsValid())
                {
                    NotifyError($"Save '{saveId}' not found or is invalid.");
                    return;
                }
                ActiveSave = saveData;
                ApplyWorldState(saveData);
                IsWorldLoaded = true;
                OnWorldLoaded?.Invoke(saveData);
            }
            catch (Exception ex) { NotifyError($"Failed to load world: {ex.Message}"); }
        }

        /// <summary>
        /// Captures current world state (player position) and persists via FirebaseAuthManager.
        /// No-ops if no world is loaded.
        /// </summary>
        public async Task SaveAsync()
        {
            if (!IsWorldLoaded || ActiveSave == null) return;

            CaptureCurrentState();

            try
            {
                await FirebaseAuthManager.Instance.SaveWorldAsync(ActiveSave);
                OnWorldSaved?.Invoke();
            }
            catch (Exception ex) { NotifyError($"Save failed: {ex.Message}"); }
        }

        /// <summary>Saves the world then clears session state ready for scene unload.</summary>
        public async Task SaveAndQuitAsync()
        {
            await SaveAsync();
            IsWorldLoaded = false;
            ActiveSave    = null;
        }

        // ── Private helpers ──────────────────────────────────────────────────

        private void ApplyWorldState(WorldSaveData save)
        {
            // Apply seed to chunk generation
            if (_chunkManager != null)
                _chunkManager.SetSeed(save.seed);

            // Restore player position
            if (_playerController != null)
            {
                WorldSaveData.DecodePosition(save.playerPosition,
                    out float px, out float py, out float pz);
                _playerController.transform.position = new Vector3(px, py, pz);
            }

            // Restore character appearance from saved CharacterData
            if (_characterCustomizer != null && SessionManager.IsChildActive)
            {
                var charData   = SessionManager.ActiveChild.character;
                var appearance = CharacterDataBridge.ToAppearance(charData);
                _characterCustomizer.SetAppearance(appearance);
            }
        }

        private void CaptureCurrentState()
        {
            if (ActiveSave == null) return;

            if (_playerController != null)
            {
                var pos = _playerController.transform.position;
                ActiveSave.playerPosition = WorldSaveData.EncodePosition(pos.x, pos.y, pos.z);
            }

            ActiveSave.savedAt = DateTime.UtcNow.ToString("o");
            ActiveSave.dayCount++;
        }

        private void NotifyError(string message)
        {
            Debug.LogError($"[WorldSaveManager] {message}");
            OnError?.Invoke(message);
        }
    }
}
