using System;
using UnityEngine;

/// <summary>
/// Converts between Unity Sprites and RGBA pixel buffers. Works with any dimensions.
/// Caller chooses output size; handles non-readable textures via RenderTexture.
/// </summary>
public static class SpritePixelConversion
{
    /// <summary>
    /// Converts a sprite to RGBA bytes (row-major). Uses RenderTexture to handle non-readable textures.
    /// </summary>
    /// <param name="sprite">Source sprite.</param>
    /// <param name="width">Output width.</param>
    /// <param name="height">Output height.</param>
    /// <returns>RGBA bytes, length = width * height * 4.</returns>
    public static byte[] SpriteToPixels(Sprite sprite, int width, int height)
    {
        if (sprite == null || sprite.texture == null)
            throw new ArgumentException("Sprite and its texture must be non-null.", nameof(sprite));
        if (width <= 0 || height <= 0)
            throw new ArgumentOutOfRangeException("Width and height must be positive.");

        Texture2D tex = sprite.texture;
        Rect rect = sprite.textureRect;
        int x = Mathf.FloorToInt(rect.x);
        int y = Mathf.FloorToInt(rect.y);
        int bytesTotal = width * height * 4;

        if (tex.isReadable)
        {
            Color[] colors = tex.GetPixels(x, y, width, height);
            byte[] copy = new byte[bytesTotal];
            for (int i = 0; i < colors.Length; i++)
            {
                Color c = colors[i];
                int j = i * 4;
                copy[j] = (byte)(Mathf.Clamp01(c.r) * 255f);
                copy[j + 1] = (byte)(Mathf.Clamp01(c.g) * 255f);
                copy[j + 2] = (byte)(Mathf.Clamp01(c.b) * 255f);
                copy[j + 3] = (byte)(Mathf.Clamp01(c.a) * 255f);
            }
            return copy;
        }

        Shader shader = Shader.Find("Sprites/Default") ?? Shader.Find("Unlit/Transparent");
        if (shader == null)
            throw new InvalidOperationException("No suitable shader found for sprite copy. Add Sprites/Default or Unlit/Transparent to the project.");

        RenderTexture rt = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Default);
        RenderTexture prev = RenderTexture.active;
        try
        {
            RenderTexture.active = rt;
            Material mat = new Material(shader);
            mat.mainTexture = tex;
            mat.SetTextureOffset("_MainTex", new Vector2(rect.x / tex.width, rect.y / tex.height));
            mat.SetTextureScale("_MainTex", new Vector2(rect.width / tex.width, rect.height / tex.height));
            Graphics.Blit(tex, rt, mat);
            UnityEngine.Object.Destroy(mat);

            Texture2D readTex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            readTex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            readTex.Apply();
            Color32[] rtColors = readTex.GetPixels32();
            byte[] copy = new byte[bytesTotal];
            for (int i = 0; i < rtColors.Length; i++)
            {
                int j = i * 4;
                copy[j] = rtColors[i].r;
                copy[j + 1] = rtColors[i].g;
                copy[j + 2] = rtColors[i].b;
                copy[j + 3] = rtColors[i].a;
            }
            UnityEngine.Object.Destroy(readTex);
            return copy;
        }
        finally
        {
            RenderTexture.active = prev;
            RenderTexture.ReleaseTemporary(rt);
        }
    }

    /// <summary>
    /// Creates a Sprite from RGBA bytes (row-major).
    /// </summary>
    /// <param name="pixels">RGBA bytes. Must have length = width * height * 4.</param>
    /// <param name="width">Image width.</param>
    /// <param name="height">Image height.</param>
    /// <returns>New Sprite. Caller is responsible for the lifetime of the underlying texture.</returns>
    public static Sprite PixelsToSprite(byte[] pixels, int width, int height)
    {
        int expected = width * height * 4;
        if (pixels == null || pixels.Length != expected)
            throw new ArgumentException($"Pixels must be exactly {expected} bytes (width={width}, height={height}).", nameof(pixels));
        if (width <= 0 || height <= 0)
            throw new ArgumentOutOfRangeException("Width and height must be positive.");

        Color32[] colors = new Color32[width * height];
        for (int i = 0; i < colors.Length; i++)
        {
            int j = i * 4;
            colors[i] = new Color32(pixels[j], pixels[j + 1], pixels[j + 2], pixels[j + 3]);
        }
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.SetPixels32(colors);
        tex.Apply(false, false);
        return Sprite.Create(tex, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f));
    }

    /// <summary>
    /// Converts multiple sprites (frames) to concatenated RGBA bytes. Frame order preserved.
    /// </summary>
    /// <param name="sprites">Source sprites. Null or empty returns empty array.</param>
    /// <param name="frameWidth">Output width per frame.</param>
    /// <param name="frameHeight">Output height per frame.</param>
    /// <returns>RGBA bytes, length = sprites.Length * frameWidth * frameHeight * 4.</returns>
    public static byte[] SpriteToPixels(Sprite[] sprites, int frameWidth, int frameHeight)
    {
        if (sprites == null || sprites.Length == 0)
            return Array.Empty<byte>();
        if (frameWidth <= 0 || frameHeight <= 0)
            throw new ArgumentOutOfRangeException("Frame width and height must be positive.");

        int bytesPerFrame = frameWidth * frameHeight * 4;
        byte[] result = new byte[sprites.Length * bytesPerFrame];
        for (int i = 0; i < sprites.Length; i++)
        {
            byte[] frame = SpriteToPixels(sprites[i], frameWidth, frameHeight);
            Array.Copy(frame, 0, result, i * bytesPerFrame, bytesPerFrame);
        }
        return result;
    }

    /// <summary>
    /// Creates an array of Sprites from concatenated RGBA bytes (one texture per frame).
    /// </summary>
    /// <param name="pixels">RGBA bytes. Must have length = frameCount * frameWidth * frameHeight * 4.</param>
    /// <param name="frameWidth">Width per frame.</param>
    /// <param name="frameHeight">Height per frame.</param>
    /// <param name="frameCount">Number of frames.</param>
    /// <returns>Array of Sprites. Caller is responsible for texture lifetime.</returns>
    public static Sprite[] PixelsToSprites(byte[] pixels, int frameWidth, int frameHeight, int frameCount)
    {
        int bytesPerFrame = frameWidth * frameHeight * 4;
        int expected = frameCount * bytesPerFrame;
        if (pixels == null || pixels.Length != expected)
            throw new ArgumentException($"Pixels must be exactly {expected} bytes ({frameCount} frames of {frameWidth}x{frameHeight}).", nameof(pixels));
        if (frameWidth <= 0 || frameHeight <= 0 || frameCount <= 0)
            throw new ArgumentOutOfRangeException("Frame dimensions and count must be positive.");

        Sprite[] result = new Sprite[frameCount];
        for (int i = 0; i < frameCount; i++)
        {
            byte[] framePixels = new byte[bytesPerFrame];
            Array.Copy(pixels, i * bytesPerFrame, framePixels, 0, bytesPerFrame);
            result[i] = PixelsToSprite(framePixels, frameWidth, frameHeight);
        }
        return result;
    }
}
