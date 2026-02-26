using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Tilemaps;


namespace WFC
{
    public static class DungeonSetup
    {
        public class SetupResult
        {
            public Grid grid;
            public Tilemap baseTilemap;
            public Canvas canvas;
            public RectTransform minimapPanel;
            public RawImage minimapImage;
            public GameObject minimapRendererObject;
        }

        /// <summary>
        /// Main public method to setup complete dungeon hierarchy
        /// Call this from your DungeonGeneration script
        /// </summary>
        public static SetupResult SetupDungeonHierarchy(Transform parentTransform = null)
        {
            SetupResult result = new SetupResult();

            // Setup Grid and Tilemap
            SetupGridAndTilemap(parentTransform, out result.grid, out result.baseTilemap);

            // Setup Canvas and Minimap UI
            SetupMinimapUI(out result.canvas, out result.minimapPanel, out result.minimapImage);

            // Create MinimapRenderer GameObject
            result.minimapRendererObject = CreateMinimapRenderer();

            Debug.Log("✓ Dungeon hierarchy setup complete");
            return result;
        }

        /// <summary>
        /// Creates Grid and Tilemap hierarchy under parent
        /// </summary>
        private static void SetupGridAndTilemap(Transform parent, out Grid grid, out Tilemap tilemap)
        {
            // Create Grid GameObject
            GameObject gridObj = new GameObject("MapGrid");
            if (parent != null)
            {
                gridObj.transform.SetParent(parent);
            }

            grid = gridObj.AddComponent<Grid>();
            grid.cellSize = new Vector3(1, 1, 0);
            grid.cellLayout = GridLayout.CellLayout.Rectangle;

            // Create Tilemap as child of Grid
            GameObject tilemapObj = new GameObject("BaseLayerTilemap");
            tilemapObj.transform.SetParent(gridObj.transform);

            tilemap = tilemapObj.AddComponent<Tilemap>();
            TilemapRenderer tilemapRenderer = tilemapObj.AddComponent<TilemapRenderer>();

            // Configure renderer
            tilemapRenderer.sortingOrder = 0;
            tilemapRenderer.sortingLayerName = "Default";

            Debug.Log("✓ Grid and Tilemap created");
        }

        /// <summary>
        /// Creates or finds Canvas and sets up Minimap UI hierarchy
        /// </summary>
        private static void SetupMinimapUI(out Canvas canvas, out RectTransform minimapPanel, out RawImage minimapImage)
        {
            // Find or create Canvas
            canvas = GameObject.FindObjectOfType<Canvas>();
            if (canvas == null)
            {
                canvas = CreateCanvas();
            }

            // Create Minimap Panel
            GameObject panelObj = new GameObject("MinimapPanel");
            panelObj.transform.SetParent(canvas.transform, false);

            minimapPanel = panelObj.AddComponent<RectTransform>();
            Image panelBackground = panelObj.AddComponent<Image>();

            // Configure panel - Top-right corner
            minimapPanel.anchorMin = new Vector2(1, 1);
            minimapPanel.anchorMax = new Vector2(1, 1);
            minimapPanel.pivot = new Vector2(1, 1);
            minimapPanel.anchoredPosition = new Vector2(-10, -10);
            minimapPanel.sizeDelta = new Vector2(220, 220);

            // Semi-transparent background
            panelBackground.color = new Color(0, 0, 0, 0f);

            // Create Minimap Image as child of Panel
            GameObject imageObj = new GameObject("MinimapImage");
            imageObj.transform.SetParent(panelObj.transform, false);

            RectTransform imageRect = imageObj.AddComponent<RectTransform>();
            minimapImage = imageObj.AddComponent<RawImage>();

            // Configure image - Centered in panel
            imageRect.anchorMin = new Vector2(0.5f, 0.5f);
            imageRect.anchorMax = new Vector2(0.5f, 0.5f);
            imageRect.pivot = new Vector2(0.5f, 0.5f);
            imageRect.anchoredPosition = Vector2.zero;
            imageRect.sizeDelta = new Vector2(200, 200);

            // Add AspectRatioFitter for proper scaling
            AspectRatioFitter aspectFitter = imageObj.AddComponent<AspectRatioFitter>();
            aspectFitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            aspectFitter.aspectRatio = 1f;

            Debug.Log("✓ Minimap UI created");
        }

        /// <summary>
        /// Creates a Canvas with proper configuration
        /// </summary>
        private static Canvas CreateCanvas()
        {
            GameObject canvasObj = new GameObject("Canvas");
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            // Add CanvasScaler for responsive UI
            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            // Add GraphicRaycaster for UI interaction
            canvasObj.AddComponent<GraphicRaycaster>();

            // Create EventSystem if needed
            if (GameObject.FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                CreateEventSystem();
            }

            Debug.Log("✓ Canvas created");
            return canvas;
        }

        /// <summary>
        /// Creates EventSystem for UI interaction
        /// </summary>
        private static void CreateEventSystem()
        {
            GameObject eventSystemObj = new GameObject("EventSystem");
            eventSystemObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
            eventSystemObj.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();

            Debug.Log("✓ EventSystem created");
        }

        /// <summary>
        /// Creates a GameObject with MinimapRenderer component
        /// </summary>
        private static GameObject CreateMinimapRenderer()
        {
            GameObject rendererObj = new GameObject("MinimapRenderer");
            rendererObj.AddComponent<MinimapRenderer>();

            Debug.Log("✓ MinimapRenderer GameObject created");
            return rendererObj;
        }

        /// <summary>
        /// Optional: Cleans up existing hierarchy if re-running setup
        /// </summary>
        public static void CleanupExistingHierarchy(Transform parent)
        {
            // Find and destroy existing Grid
            Transform existingGrid = parent.Find("MapGrid");
            if (existingGrid != null)
            {
                GameObject.DestroyImmediate(existingGrid.gameObject);
                Debug.Log("✓ Removed existing MapGrid");
            }

            // Find and destroy existing Minimap UI
            Canvas canvas = GameObject.FindObjectOfType<Canvas>();
            if (canvas != null)
            {
                Transform existingPanel = canvas.transform.Find("MinimapPanel");
                if (existingPanel != null)
                {
                    GameObject.DestroyImmediate(existingPanel.gameObject);
                    Debug.Log("✓ Removed existing MinimapPanel");
                }
            }

            // Find and destroy existing MinimapRenderer
            MinimapRenderer existingRenderer = GameObject.FindObjectOfType<MinimapRenderer>();
            if (existingRenderer != null)
            {
                GameObject.DestroyImmediate(existingRenderer.gameObject);
                Debug.Log("✓ Removed existing MinimapRenderer");
            }
        }
    }
}