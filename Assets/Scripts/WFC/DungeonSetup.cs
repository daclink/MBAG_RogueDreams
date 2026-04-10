using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Tilemaps;
using UnityEngine.InputSystem.UI;


namespace WFC
{
    public static class DungeonSetup
    {
        /**
         * All elements created in the dungeon setup to be returned
         */
        public class SetupResult
        {
            public Grid grid;
            public Tilemap baseTilemap;
            public Canvas canvas;
            public RectTransform minimapPanel;
            public RawImage minimapImage;
            public GameObject minimapRendererObject;
        }

        // --------------------  DRIVER METHOD  ----------------------
        /**
         * Sets up the overall hierarchy for dungeon creation
         * Sets up the grid, tilemap, canvas, panel, image, and minimap renderer
         */
        public static SetupResult SetupDungeonHierarchy(Transform parentTransform = null)
        {
            SetupResult result = new SetupResult();

            // Setup Grid and Tilemap
            SetupGridAndTilemap(parentTransform, out result.grid, out result.baseTilemap);

            // Setup Canvas and Minimap UI
            SetupMinimapUI(out result.canvas, out result.minimapPanel, out result.minimapImage);

            // Create MinimapRenderer GameObject
            result.minimapRendererObject = CreateMinimapRenderer();

            return result;
        }

        // --------------------  MINIMAP METHODS  ----------------------
        /**
         * Creates the minimap renderer object
         */
        private static GameObject CreateMinimapRenderer()
        {
            GameObject rendererObj = new GameObject("MinimapRenderer");
            rendererObj.AddComponent<MinimapRenderer>();

            return rendererObj;
        }
        
                /**
         * Creates the entirety of the minimap UI by adding components if necessary and setting values
         */
        private static void SetupMinimapUI(out Canvas canvas, out RectTransform minimapPanel, out RawImage minimapImage)
        {
            // Find or create Canvas
            canvas = Object.FindFirstObjectByType<Canvas>();
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

        }

        /**
         * Creates the canvas object with the proper start values
         */
        private static Canvas CreateCanvas()
        {
            GameObject canvasObj = new GameObject("Canvas");
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            // Add CanvasScaler for responsive UI
            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            // Can adjust the reference res size to the actual screen size of 640 x 360.
            // 1920 x 1080 is smaller and looks better
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            // Add GraphicRaycaster for UI interaction
            canvasObj.AddComponent<GraphicRaycaster>();

            // Create EventSystem if needed
            if (Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                CreateEventSystem();
            }

            return canvas;
        }
        
        /**
        * Creates the event system to allow for UI Interaction
        * Most likely not needed in the future
        */
        private static void CreateEventSystem()
        {
            GameObject eventSystemObj = new GameObject("EventSystem");
            eventSystemObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
            eventSystemObj.AddComponent<InputSystemUIInputModule>();
        }

        // ------------------------ GRID/TILEMAP -------------------------
        /**
         * Creates the grid and tilemap and sets the parent object to put under
         */
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
        }

        // ------------------------  HELPERS  ------------------------------
        /**
         * Cleans up the existing hierarchy if necessary by removing certain components before having them added back in
         * Prevents duplicates
         */
        public static void CleanupExistingHierarchy(Transform parent)
        {
            // Find and destroy existing Grid
            Transform existingGrid = parent.Find("MapGrid");
            if (existingGrid != null)
            {
                GameObject.DestroyImmediate(existingGrid.gameObject);
            }

            // Find and destroy existing Minimap UI
            Canvas canvas = Object.FindFirstObjectByType<Canvas>();
            if (canvas != null)
            {
                Transform existingPanel = canvas.transform.Find("MinimapPanel");
                if (existingPanel != null)
                {
                    GameObject.DestroyImmediate(existingPanel.gameObject);
                }
            }

            // Find and destroy existing MinimapRenderer
            MinimapRenderer existingRenderer = Object.FindFirstObjectByType<MinimapRenderer>();
            if (existingRenderer != null)
            {
                GameObject.DestroyImmediate(existingRenderer.gameObject);
            }
        }
    }
}