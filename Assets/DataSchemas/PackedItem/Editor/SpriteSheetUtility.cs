using UnityEngine;
using Tables;

namespace DataSchemas.PackedItem.Editor
{
    /// <summary>
    /// Slices a Texture2D into Sprites by frame size. Frames are left-to-right, top-to-bottom.
    /// Capped at 16 frames (icon + 15 animation).
    /// </summary>
    public static class SpriteSheetUtility
    {
        public const int DefaultFrameWidth = 64;
        public const int DefaultFrameHeight = 64;

        /// <summary>
        /// Slices the texture into 64×64 frames. Texture dimensions must be divisible by frame size.
        /// Texture must be readable (Enable Read/Write in import settings).
        /// Limited to MaxFramesPerItem (16) — frame 0 = icon, frames 1–15 = animation.
        /// </summary>
        public static Sprite[] SliceTexture(Texture2D texture, int frameWidth = DefaultFrameWidth, int frameHeight = DefaultFrameHeight)
        {
            if (texture == null) return null;
            if (frameWidth <= 0 || frameHeight <= 0)
                return null;
            if (texture.width % frameWidth != 0 || texture.height % frameHeight != 0)
                return null;

            if (!texture.isReadable)
            {
                Debug.LogWarning($"Texture '{texture.name}' is not readable. Enable Read/Write in import settings.");
                return null;
            }

            int cols = texture.width / frameWidth;
            int rows = texture.height / frameHeight;
            int totalFrames = cols * rows;
            int frameCount = Mathf.Min(totalFrames, SpriteTableSerialization.MaxFramesPerItem);
            if (totalFrames > frameCount)
                Debug.LogWarning($"Texture '{texture.name}' has {totalFrames} frames; using first {frameCount} (icon + 15 animation).");

            Sprite[] sprites = new Sprite[frameCount];
            Vector2 pivot = new Vector2(0.5f, 0.5f);

            int idx = 0;
            for (int row = 0; row < rows && idx < frameCount; row++)
            {
                for (int col = 0; col < cols && idx < frameCount; col++)
                {
                    Rect rect = new Rect(col * frameWidth, texture.height - (row + 1) * frameHeight, frameWidth, frameHeight);
                    sprites[idx++] = Sprite.Create(texture, rect, pivot);
                }
            }
            return sprites;
        }
    }
}
