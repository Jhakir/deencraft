// Assets/Editor/SceneAssembler.cs
// Phase 9 — assembles the playable gameplay scene programmatically.
//
// Unity scenes are YAML and must not be hand-edited, so all scene wiring for
// Deencraft is done here. Running this is idempotent: it can be re-run safely
// after any script change and will rebuild the HUD from scratch.
//
//   Menu:        Deencraft > Assemble Gameplay Scene
//   Batch mode:  -executeMethod DeenCraft.Editor.SceneAssembler.AssembleFromCommandLine
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using DeenCraft.Player;
using DeenCraft.UI;
using DeenCraft.World;
using DeenCraft.Animals;
using DeenCraft.Villager;

namespace DeenCraft.Editor
{
    public static class SceneAssembler
    {
        private const string ScenePath = "Assets/Scenes/Main.slunity.unity";
        private const string HudRootName = "HUD Canvas";

        // Layout constants — no magic numbers in the body below.
        private const int   HeartCount     = 10;
        private const float HeartSize      = 26f;
        private const float HeartSpacing   = 4f;
        private const float HotbarSlotSize = 56f;
        private const float HotbarPadding  = 4f;
        private const float CrosshairSize  = 18f;
        private const float HungerBarWidth = 300f;
        private const float HungerBarHeight = 16f;

        // Shaders referenced only at runtime (primitives, UI) are invisible to the
        // build's dependency scan and get stripped. They must be force-included.
        private static readonly string[] RequiredShaders =
        {
            "Standard",                 // GameObject.CreatePrimitive default material
            "DeenCraft/VertexColor",    // chunk meshes
            "UI/Default",               // runtime-built HUD
            "Sprites/Default",
        };

        [MenuItem("Deencraft/Assemble Gameplay Scene")]
        public static void Assemble()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            GameObject player = GameObject.Find("PlayerController");
            if (player == null)
            {
                Debug.LogError("[SceneAssembler] No 'PlayerController' GameObject in the scene. Aborting.");
                return;
            }

            Camera mainCamera = ResolveMainCamera();
            ConfigurePlayer(player, mainCamera);
            GameObject hud = BuildHud(player);
            EnsureEventSystem();
            EnsureAlwaysIncludedShaders();
            ConfigureClouds(player.transform);
            ConfigureAnimals(player.transform);
            ConfigureVillages(player.transform);
            ConfigureMasjids(player.transform);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();

            Debug.Log($"[SceneAssembler] Scene assembled. Player components wired, HUD '{hud.name}' rebuilt.");
        }

        public static void AssembleFromCommandLine()
        {
            Assemble();
            EditorApplication.Exit(0);
        }

        private static void ConfigureClouds(Transform player)
        {
            GameObject clouds = GameObject.Find("Cloud Layer");
            if (clouds == null) clouds = new GameObject("Cloud Layer");
            var layer = Require<CloudLayer>(clouds);
            SetPrivateField(layer, "_playerTransform", player);
            SetPrivateField(layer, "_cloudMaterial",
                AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Materials/WorldMaterial.mat"));
        }

        private static void ConfigureAnimals(Transform player)
        {
            GameObject animals = GameObject.Find("Animal Population");
            if (animals == null) animals = new GameObject("Animal Population");
            var spawner = Require<AnimalSpawner>(animals);
            SetPrivateField(spawner, "_playerTransform", player);
            SetPrivateField(spawner, "_chunkManager", Object.FindObjectOfType<ChunkManager>());
            SetPrivateField(spawner, "_animalMaterial",
                AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Materials/WorldMaterial.mat"));
        }

        private static void ConfigureVillages(Transform player)
        {
            GameObject villages = GameObject.Find("Village Population");
            if (villages == null) villages = new GameObject("Village Population");
            var spawner = Require<VillageSpawner>(villages);
            SetPrivateField(spawner, "_playerTransform", player);
            SetPrivateField(spawner, "_chunkManager", Object.FindObjectOfType<ChunkManager>());
        }

        private static void ConfigureMasjids(Transform player)
        {
            GameObject district = GameObject.Find("Masjid District Population");
            if (district == null) district = new GameObject("Masjid District Population");
            var population = Require<MasjidDistrictSpawner>(district);
            SetPrivateField(population, "_playerTransform", player);
            SetPrivateField(population, "_chunkManager", Object.FindObjectOfType<ChunkManager>());
            GameObject approach = GameObject.Find("Masjid District Approach");
            if (approach == null) approach = new GameObject("Masjid District Approach");
            var path = Require<MasjidApproachPath>(approach);
            SetPrivateField(path, "_chunkManager", Object.FindObjectOfType<ChunkManager>());
        }

        // ── Graphics settings ────────────────────────────────────────────────

        /// <summary>
        /// Guarantees every shader in <see cref="RequiredShaders"/> survives build
        /// stripping. Without this, Shader.Find returns null in WebGL and any
        /// runtime-created material renders magenta or throws.
        /// </summary>
        [MenuItem("Deencraft/Ensure Always Included Shaders")]
        public static void EnsureAlwaysIncludedShaders()
        {
            var graphicsSettings = AssetDatabase.LoadAllAssetsAtPath(
                "ProjectSettings/GraphicsSettings.asset")[0];
            var serialized = new SerializedObject(graphicsSettings);
            SerializedProperty list = serialized.FindProperty("m_AlwaysIncludedShaders");

            foreach (string shaderName in RequiredShaders)
            {
                Shader shader = Shader.Find(shaderName);
                if (shader == null)
                {
                    Debug.LogWarning($"[SceneAssembler] Shader '{shaderName}' not found; skipping.");
                    continue;
                }

                if (ContainsShader(list, shader))
                    continue;

                int index = list.arraySize;
                list.InsertArrayElementAtIndex(index);
                list.GetArrayElementAtIndex(index).objectReferenceValue = shader;
                Debug.Log($"[SceneAssembler] Added '{shaderName}' to Always Included Shaders.");
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.SaveAssets();
        }

        private static bool ContainsShader(SerializedProperty list, Shader shader)
        {
            for (int i = 0; i < list.arraySize; i++)
            {
                if (list.GetArrayElementAtIndex(i).objectReferenceValue == shader)
                    return true;
            }
            return false;
        }

        // ── Player ───────────────────────────────────────────────────────────

        /// <summary>
        /// Attaches every gameplay component the player needs and points the
        /// existing scene camera at the player, so PlayerController does not
        /// spawn a second MainCamera-tagged camera at runtime.
        /// </summary>
        private static void ConfigurePlayer(GameObject player, Camera mainCamera)
        {
            var controller = Require<PlayerController>(player);
            Require<InventoryHolder>(player);
            Require<VitalitySystem>(player);
            Require<HotbarController>(player);
            Require<CharacterCustomizer>(player);
            Require<PlayerAnimator>(player);
            var interactor = Require<BlockInteractor>(player);

            if (mainCamera != null)
            {
                SetPrivateField(controller, "_cameraTarget", mainCamera.transform);
                SetPrivateField(interactor, "_playerCamera", mainCamera);

                // Detach the camera so PlayerController can position it in LateUpdate.
                mainCamera.transform.SetParent(null);
                mainCamera.farClipPlane = 500f;
                mainCamera.nearClipPlane = 0.05f;
            }
        }

        private static Camera ResolveMainCamera()
        {
            Camera cam = Camera.main;
            if (cam == null) cam = Object.FindObjectOfType<Camera>();
            if (cam == null)
            {
                var go = new GameObject("Main Camera");
                cam = go.AddComponent<Camera>();
                go.AddComponent<AudioListener>();
                go.tag = "MainCamera";
            }
            return cam;
        }

        // ── HUD ──────────────────────────────────────────────────────────────

        private static GameObject BuildHud(GameObject player)
        {
            // Always rebuild from scratch so re-running never duplicates elements.
            GameObject existing = GameObject.Find(HudRootName);
            if (existing != null) Object.DestroyImmediate(existing);

            var canvasGo = new GameObject(HudRootName,
                typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));

            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight  = 0.5f;

            Sprite box   = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            Sprite knob  = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
            // Unity 2022 renamed the built-in font; "Arial.ttf" now throws.
            Font   font  = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font == null)
                throw new System.InvalidOperationException("The gameplay HUD requires the built-in LegacyRuntime font.");

            BuildCrosshair(canvasGo.transform, box);
            Image[] hearts = BuildHearts(canvasGo.transform, knob);
            Image hungerFill = BuildHungerBar(canvasGo.transform, box);
            HotbarSlotUI[] slots = BuildHotbar(canvasGo.transform, box, font, out RectTransform selector);
            Text timeLabel = BuildTimeLabel(canvasGo.transform, font);
            var tradePanel = canvasGo.AddComponent<VillagerTradePanel>();
            SetPrivateField(tradePanel, "_font", font);
            var masjidPanel = canvasGo.AddComponent<MasjidInformationPanel>();
            SetPrivateField(masjidPanel, "_font", font);

            // Wire the HudController to everything it drives.
            var hudController = canvasGo.AddComponent<HudController>();
            SetPrivateField(hudController, "_vitalitySystem",  player.GetComponent<VitalitySystem>());
            SetPrivateField(hudController, "_inventoryHolder", player.GetComponent<InventoryHolder>());
            SetPrivateArray(hudController, "_heartIcons",   hearts);
            SetPrivateField(hudController, "_heartFull",    knob);
            SetPrivateField(hudController, "_heartEmpty",   box);
            SetPrivateField(hudController, "_hungerFill",   hungerFill);
            SetPrivateArray(hudController, "_hotbarSlots",  slots);
            SetPrivateField(hudController, "_hotbarSelector", selector);
            SetPrivateField(hudController, "_timeLabel",    timeLabel);

            return canvasGo;
        }

        private static void BuildCrosshair(Transform parent, Sprite sprite)
        {
            var go = NewUIElement("Crosshair", parent, sprite, new Color(1f, 1f, 1f, 0.75f));
            var rt = go.GetComponent<RectTransform>();
            Anchor(rt, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(CrosshairSize, CrosshairSize));
        }

        private static Image[] BuildHearts(Transform parent, Sprite sprite)
        {
            var row = new GameObject("Hearts", typeof(RectTransform)).GetComponent<RectTransform>();
            row.SetParent(parent, false);
            Anchor(row, new Vector2(0f, 0f), new Vector2(40f, 150f),
                   new Vector2(HeartCount * (HeartSize + HeartSpacing), HeartSize));

            var hearts = new Image[HeartCount];
            for (int i = 0; i < HeartCount; i++)
            {
                var go = NewUIElement($"Heart{i}", row, sprite, new Color(0.85f, 0.15f, 0.2f, 1f));
                var rt = go.GetComponent<RectTransform>();
                Anchor(rt, new Vector2(0f, 0.5f),
                       new Vector2(i * (HeartSize + HeartSpacing) + HeartSize * 0.5f, 0f),
                       new Vector2(HeartSize, HeartSize));
                hearts[i] = go.GetComponent<Image>();
            }
            return hearts;
        }

        private static Image BuildHungerBar(Transform parent, Sprite sprite)
        {
            var back = NewUIElement("HungerBackground", parent, sprite, new Color(0f, 0f, 0f, 0.45f));
            var backRt = back.GetComponent<RectTransform>();
            Anchor(backRt, new Vector2(0f, 0f), new Vector2(40f + HungerBarWidth * 0.5f, 120f),
                   new Vector2(HungerBarWidth, HungerBarHeight));

            var fill = NewUIElement("HungerFill", back.transform, sprite, new Color(0.85f, 0.6f, 0.2f, 1f));
            var fillRt = fill.GetComponent<RectTransform>();
            Stretch(fillRt);

            var fillImage = fill.GetComponent<Image>();
            fillImage.type        = Image.Type.Filled;
            fillImage.fillMethod  = Image.FillMethod.Horizontal;
            fillImage.fillOrigin  = (int)Image.OriginHorizontal.Left;
            fillImage.fillAmount  = 1f;
            return fillImage;
        }

        private static HotbarSlotUI[] BuildHotbar(
            Transform parent, Sprite sprite, Font font, out RectTransform selector)
        {
            int slotCount = DeenCraft.GameConstants.HotbarSlots;
            float width   = slotCount * (HotbarSlotSize + HotbarPadding);

            var bar = new GameObject("Hotbar", typeof(RectTransform)).GetComponent<RectTransform>();
            bar.SetParent(parent, false);
            Anchor(bar, new Vector2(0.5f, 0f), new Vector2(0f, 50f),
                   new Vector2(width, HotbarSlotSize));

            var slots = new HotbarSlotUI[slotCount];
            for (int i = 0; i < slotCount; i++)
            {
                float x = (i + 0.5f) * (HotbarSlotSize + HotbarPadding) - width * 0.5f;

                var slotGo = NewUIElement($"Slot{i}", bar, sprite, new Color(0f, 0f, 0f, 0.5f));
                var slotRt = slotGo.GetComponent<RectTransform>();
                Anchor(slotRt, new Vector2(0.5f, 0.5f), new Vector2(x, 0f),
                       new Vector2(HotbarSlotSize, HotbarSlotSize));

                // Item swatch — tinted by HotbarSlotUI to the block's world colour.
                var iconGo = NewUIElement("Icon", slotGo.transform, sprite, Color.white);
                var iconRt = iconGo.GetComponent<RectTransform>();
                Anchor(iconRt, new Vector2(0.5f, 0.5f), Vector2.zero,
                       new Vector2(HotbarSlotSize - 14f, HotbarSlotSize - 14f));

                var countGo = new GameObject("Count", typeof(RectTransform), typeof(Text));
                countGo.transform.SetParent(slotGo.transform, false);
                var countRt = countGo.GetComponent<RectTransform>();
                Anchor(countRt, new Vector2(1f, 0f), new Vector2(-14f, 10f), new Vector2(40f, 20f));
                var countText = countGo.GetComponent<Text>();
                countText.font      = font;
                countText.fontSize  = 16;
                countText.alignment = TextAnchor.LowerRight;
                countText.color     = Color.white;
                countText.raycastTarget = false;

                var slotUi = slotGo.AddComponent<HotbarSlotUI>();
                SetPrivateField(slotUi, "_icon",       iconGo.GetComponent<Image>());
                SetPrivateField(slotUi, "_countLabel", countText);
                slots[i] = slotUi;
            }

            // Selection highlight — HudController slides this along the bar.
            var selGo = NewUIElement("Selector", bar, sprite, new Color(1f, 1f, 1f, 0.35f));
            selector = selGo.GetComponent<RectTransform>();
            selector.anchorMin = new Vector2(0f, 0.5f);
            selector.anchorMax = new Vector2(0f, 0.5f);
            selector.pivot     = new Vector2(0f, 0.5f);
            selector.sizeDelta = new Vector2(HotbarSlotSize + HotbarPadding, HotbarSlotSize + HotbarPadding);
            selector.anchoredPosition = Vector2.zero;

            return slots;
        }

        private static Text BuildTimeLabel(Transform parent, Font font)
        {
            var go = new GameObject("TimeLabel", typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            Anchor(rt, new Vector2(1f, 1f), new Vector2(-120f, -40f), new Vector2(200f, 40f));

            var text = go.GetComponent<Text>();
            text.font      = font;
            text.fontSize  = 22;
            text.alignment = TextAnchor.UpperRight;
            text.color     = Color.white;
            text.raycastTarget = false;
            return text;
        }

        private static void EnsureEventSystem()
        {
            if (Object.FindObjectOfType<EventSystem>() != null) return;
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        }

        // ── Helpers ──────────────────────────────────────────────────────────

        private static T Require<T>(GameObject go) where T : Component
        {
            T component = go.GetComponent<T>();
            return component != null ? component : go.AddComponent<T>();
        }

        private static GameObject NewUIElement(string name, Transform parent, Sprite sprite, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var image = go.GetComponent<Image>();
            image.sprite        = sprite;
            image.color         = color;
            image.raycastTarget = false;
            return go;
        }

        private static void Anchor(RectTransform rt, Vector2 anchor, Vector2 position, Vector2 size)
        {
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot     = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = position;
            rt.sizeDelta = size;
        }

        private static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        /// <summary>
        /// Assigns a [SerializeField] private field. SerializedObject is the only
        /// supported way to do this from an editor script.
        /// </summary>
        private static void SetPrivateField(Object target, string fieldName, Object value)
        {
            var so   = new SerializedObject(target);
            var prop = so.FindProperty(fieldName);
            if (prop == null)
            {
                Debug.LogWarning($"[SceneAssembler] Field '{fieldName}' not found on {target.GetType().Name}.");
                return;
            }
            prop.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetPrivateArray(Object target, string fieldName, Object[] values)
        {
            var so   = new SerializedObject(target);
            var prop = so.FindProperty(fieldName);
            if (prop == null)
            {
                Debug.LogWarning($"[SceneAssembler] Array '{fieldName}' not found on {target.GetType().Name}.");
                return;
            }
            prop.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++)
                prop.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
#endif
