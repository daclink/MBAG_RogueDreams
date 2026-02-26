using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace WFC
{
    public class MinimapRenderer : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private RawImage minimapImage;
        [SerializeField] private RectTransform minimapContainer;
        
        [Header("Minimap Settings")]
        [SerializeField] private int pixelsPerRoom = 16;
        [SerializeField] private int roomSpacing = 4;
        [SerializeField] private int roomBorderSize = 2;
        [SerializeField] private int paddingRooms = 1;  
        
        [Header("Connection Settings")]
        [SerializeField] private bool showConnections = true;
        [SerializeField] private int connectionWidth = 2;
        [SerializeField] private Color connectionColor = new Color(0.5f, 0.5f, 0.5f, 1f);
        
        [Header("Room Colors")]
        [SerializeField] private Color emptyColor = new Color(0.1f, 0.1f, 0.1f, 1f);
        [SerializeField] private Color normalRoomColor = Color.white;
        [SerializeField] private Color startRoomColor = Color.green;
        [SerializeField] private Color endRoomColor = Color.red;
        [SerializeField] private Color itemRoomColor = Color.yellow;
        [SerializeField] private Color borderColor = Color.black;
        
        private Texture2D minimapTexture;
        private int[,] currentRoomLayout;
        private Vector2Int boundingBoxMin;
        private Vector2Int boundingBoxMax;
        
        private AspectRatioFitter aspectRatioFitter;

        void Awake()
        {
            // Get or add AspectRatioFitter component
            if (minimapImage != null)
            {
                aspectRatioFitter = minimapImage.GetComponent<AspectRatioFitter>();
                if (aspectRatioFitter == null)
                {
                    aspectRatioFitter = minimapImage.gameObject.AddComponent<AspectRatioFitter>();
                }
                
                aspectRatioFitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            }
        }

        /**
         * Driver method that takes in the roomlayour grid array and renders the minimap from it
         */
        public void RenderMinimap(int[,] roomLayout)
        {
            if (roomLayout == null)
            {
                return;
            }

            currentRoomLayout = roomLayout;
            
            int gridWidth = roomLayout.GetLength(0);
            int gridHeight = roomLayout.GetLength(1);
            
            // Calculate bounding box of placed rooms
            CalculateBoundingBox(roomLayout, out boundingBoxMin, out boundingBoxMax);
            
            // Calculate texture size based on bounding box only
            int boundingWidth = boundingBoxMax.x - boundingBoxMin.x + 1;
            int boundingHeight = boundingBoxMax.y - boundingBoxMin.y + 1;
            
            int textureWidth = boundingWidth * pixelsPerRoom;
            int textureHeight = boundingHeight * pixelsPerRoom;
            
            // Create or resize texture
            if (minimapTexture == null || minimapTexture.width != textureWidth || minimapTexture.height != textureHeight)
            {
                minimapTexture = new Texture2D(textureWidth, textureHeight, TextureFormat.RGBA32, false);
                minimapTexture.filterMode = FilterMode.Point; // Crisp pixels
            }
            
            // Clear to empty color
            Color[] pixels = new Color[textureWidth * textureHeight];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = emptyColor;
            }
            
            // Draw connections first (so rooms draw on top)
            if (showConnections)
            {
                DrawConnections(pixels, textureWidth, textureHeight, roomLayout);
            }
            
            // Draw each room (only within bounding box)
            for (int gridX = boundingBoxMin.x; gridX <= boundingBoxMax.x; gridX++)
            {
                for (int gridY = boundingBoxMin.y; gridY <= boundingBoxMax.y; gridY++)
                {
                    int roomValue = roomLayout[gridX, gridY];
                    
                    if (roomValue >= 0) // Room exists
                    {
                        Color roomColor = GetRoomColor(roomValue);
                        DrawRoom(pixels, textureWidth, textureHeight, gridX, gridY, roomColor);
                    }
                }
            }
            
            // Apply pixels to texture
            minimapTexture.SetPixels(pixels);
            minimapTexture.Apply();
            
            // Display on UI
            if (minimapImage != null)
            {
                minimapImage.texture = minimapTexture;

                UpdateAspectRatio();
            }
            
        }
        
        /**
         * Updates the aspect ratio on the aspectRatioFitter Component to match
         */
        private void UpdateAspectRatio()
        {
            if (minimapTexture != null && aspectRatioFitter != null)
            {
                float aspectRatio = (float)minimapTexture.width / minimapTexture.height;
                aspectRatioFitter.aspectRatio = aspectRatio;
            }
        }

        /**
         * Calculates the bounding box that contains all the rooms on the map
         */
        private void CalculateBoundingBox(int[,] roomLayout, out Vector2Int min, out Vector2Int max)
        {
            int gridWidth = roomLayout.GetLength(0);
            int gridHeight = roomLayout.GetLength(1);
            
            int minX = gridWidth;
            int minY = gridHeight;
            int maxX = -1;
            int maxY = -1;
            
            // Find bounds of all rooms
            for (int x = 0; x < gridWidth; x++)
            {
                for (int y = 0; y < gridHeight; y++)
                {
                    if (roomLayout[x, y] >= 0) // Room exists
                    {
                        if (x < minX) minX = x;
                        if (x > maxX) maxX = x;
                        if (y < minY) minY = y;
                        if (y > maxY) maxY = y;
                    }
                }
            }
            
            // Add padding
            minX = Mathf.Max(0, minX - paddingRooms);
            minY = Mathf.Max(0, minY - paddingRooms);
            maxX = Mathf.Min(gridWidth - 1, maxX + paddingRooms);
            maxY = Mathf.Min(gridHeight - 1, maxY + paddingRooms);
            
            min = new Vector2Int(minX, minY);
            max = new Vector2Int(maxX, maxY);
        }

        /**
         * Converts the grid coordinates to texture coordinates
         */
        private Vector2Int GridToTextureCoords(int gridX, int gridY)
        {
            int textureX = (gridX - boundingBoxMin.x) * pixelsPerRoom + (roomSpacing / 2);
            int textureY = (gridY - boundingBoxMin.y) * pixelsPerRoom + (roomSpacing / 2);
            return new Vector2Int(textureX, textureY);
        }

        /**
         * Draws the room connection coridors
         */
        private void DrawConnections(Color[] pixels, int textureWidth, int textureHeight, int[,] roomLayout)
        {
            int gridWidth = roomLayout.GetLength(0);
            int gridHeight = roomLayout.GetLength(1);

            for (int gridX = boundingBoxMin.x; gridX <= boundingBoxMax.x; gridX++)
            {
                for (int gridY = boundingBoxMin.y; gridY <= boundingBoxMax.y; gridY++)
                {
                    // Skip if this cell doesn't have a room
                    if (roomLayout[gridX, gridY] < 0)
                        continue;

                    // Check East connection
                    if (gridX + 1 < gridWidth && roomLayout[gridX + 1, gridY] >= 0)
                    {
                        DrawHorizontalConnection(pixels, textureWidth, textureHeight, gridX, gridY);
                    }

                    // Check North connection
                    if (gridY + 1 < gridHeight && roomLayout[gridX, gridY + 1] >= 0)
                    {
                        DrawVerticalConnection(pixels, textureWidth, textureHeight, gridX, gridY);
                    }
                }
            }
        }

        /**
         * Draws horizontal connections
         */
        private void DrawHorizontalConnection(Color[] pixels, int textureWidth, int textureHeight, int gridX, int gridY)
        {
            // Calculate room size and positions (with bounding box offset)
            int roomSize = pixelsPerRoom - roomSpacing;
            
            int currentRoomX = (gridX - boundingBoxMin.x) * pixelsPerRoom + (roomSpacing / 2);
            int currentRoomY = (gridY - boundingBoxMin.y) * pixelsPerRoom + (roomSpacing / 2);
            int currentRoomCenterY = currentRoomY + roomSize / 2;
            
            // Start from right edge of current room
            int startX = currentRoomX + roomSize;
            
            // End at left edge of next room
            int endX = currentRoomX + roomSize + roomSpacing;
            
            // Draw the connection tube
            for (int x = startX; x < endX; x++)
            {
                for (int offset = -connectionWidth / 2; offset <= connectionWidth / 2; offset++)
                {
                    int y = currentRoomCenterY + offset;
                    
                    if (x >= 0 && x < textureWidth && y >= 0 && y < textureHeight)
                    {
                        int pixelIndex = y * textureWidth + x;
                        pixels[pixelIndex] = connectionColor;
                    }
                }
            }
        }

        /**
         * Draws vertical connections
         */
        private void DrawVerticalConnection(Color[] pixels, int textureWidth, int textureHeight, int gridX, int gridY)
        {
            // Calculate room size and positions (with bounding box offset)
            int roomSize = pixelsPerRoom - roomSpacing;
            
            int currentRoomX = (gridX - boundingBoxMin.x) * pixelsPerRoom + (roomSpacing / 2);
            int currentRoomY = (gridY - boundingBoxMin.y) * pixelsPerRoom + (roomSpacing / 2);
            int currentRoomCenterX = currentRoomX + roomSize / 2;
            
            // Start from top edge of current room
            int startY = currentRoomY + roomSize;
            
            // End at bottom edge of next room
            int endY = currentRoomY + roomSize + roomSpacing;
            
            // Draw the connection tube
            for (int y = startY; y < endY; y++)
            {
                for (int offset = -connectionWidth / 2; offset <= connectionWidth / 2; offset++)
                {
                    int x = currentRoomCenterX + offset;
                    
                    if (x >= 0 && x < textureWidth && y >= 0 && y < textureHeight)
                    {
                        int pixelIndex = y * textureWidth + x;
                        pixels[pixelIndex] = connectionColor;
                    }
                }
            }
        }

        /**
         * Draws an individual room
         */
        private void DrawRoom(Color[] pixels, int textureWidth, int textureHeight, int gridX, int gridY, Color roomColor)
        {
            // Calculate actual room size (reduced by spacing)
            int roomSize = pixelsPerRoom - roomSpacing;
            
            // Calculate room starting position (with bounding box offset)
            int startX = (gridX - boundingBoxMin.x) * pixelsPerRoom + (roomSpacing / 2);
            int startY = (gridY - boundingBoxMin.y) * pixelsPerRoom + (roomSpacing / 2);
            
            for (int x = 0; x < roomSize; x++)
            {
                for (int y = 0; y < roomSize; y++)
                {
                    int pixelX = startX + x;
                    int pixelY = startY + y;
                    
                    // Check bounds
                    if (pixelX >= textureWidth || pixelY >= textureHeight)
                        continue;
                    
                    int pixelIndex = pixelY * textureWidth + pixelX;
                    
                    // Draw border
                    if (x < roomBorderSize || x >= roomSize - roomBorderSize ||
                        y < roomBorderSize || y >= roomSize - roomBorderSize)
                    {
                        pixels[pixelIndex] = borderColor;
                    }
                    else
                    {
                        pixels[pixelIndex] = roomColor;
                    }
                }
            }
        }

        /**
         * retrieves the proper room color for a room
         */
        private Color GetRoomColor(int roomValue)
        {
            switch ((RoomType)roomValue)
            {
                case RoomType.Start:
                    return startRoomColor;
                case RoomType.End:
                    return endRoomColor;
                case RoomType.Item:
                    return itemRoomColor;
                case RoomType.Normal:
                    return normalRoomColor;
                default:
                    return emptyColor;
            }
        }

        /**
         * Clears the minimap to empty pixels....unused but potentially useful when having to generate multiple maps in one run
         */
        public void ClearMinimap()
        {
            if (minimapTexture != null)
            {
                Color[] pixels = new Color[minimapTexture.width * minimapTexture.height];
                for (int i = 0; i < pixels.Length; i++)
                {
                    pixels[i] = emptyColor;
                }
                minimapTexture.SetPixels(pixels);
                minimapTexture.Apply();
            }
        }

        /**
         * Highlights a room to be its necessary color
         */
        public void HighlightRoom(Vector2Int roomPosition, Color highlightColor)
        {
            if (currentRoomLayout == null || minimapTexture == null)
                return;
            
            // Check if room is within bounding box
            if (roomPosition.x < boundingBoxMin.x || roomPosition.x > boundingBoxMax.x ||
                roomPosition.y < boundingBoxMin.y || roomPosition.y > boundingBoxMax.y)
                return;
            
            int textureWidth = minimapTexture.width;
            int textureHeight = minimapTexture.height;
            Color[] pixels = minimapTexture.GetPixels();
            
            // Calculate room size and position with spacing and bounding box offset
            int roomSize = pixelsPerRoom - roomSpacing;
            int startX = (roomPosition.x - boundingBoxMin.x) * pixelsPerRoom + (roomSpacing / 2);
            int startY = (roomPosition.y - boundingBoxMin.y) * pixelsPerRoom + (roomSpacing / 2);
            int highlightBorder = 1;
            
            for (int x = -highlightBorder; x < roomSize + highlightBorder; x++)
            {
                for (int y = -highlightBorder; y < roomSize + highlightBorder; y++)
                {
                    int pixelX = startX + x;
                    int pixelY = startY + y;
                    
                    if (pixelX < 0 || pixelX >= textureWidth || pixelY < 0 || pixelY >= textureHeight)
                        continue;
                    
                    // Only draw on the border
                    if (x == -highlightBorder || x == roomSize ||
                        y == -highlightBorder || y == roomSize)
                    {
                        int pixelIndex = pixelY * textureWidth + pixelX;
                        pixels[pixelIndex] = highlightColor;
                    }
                }
            }
            
            minimapTexture.SetPixels(pixels);
            minimapTexture.Apply();
        }
    }
}