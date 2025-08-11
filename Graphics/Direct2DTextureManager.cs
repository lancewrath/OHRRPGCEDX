using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using SharpDX.Direct2D1;
using SharpDX.DXGI;
using SharpDX.Mathematics.Interop;
using SharpDX.WIC;

namespace OHRRPGCEDX.Graphics
{
    /// <summary>
    /// Direct2D texture manager for loading and managing textures
    /// </summary>
    public class Direct2DTextureManager : IDisposable
    {
        private readonly Dictionary<string, SharpDX.Direct2D1.Bitmap> loadedTextures;
        private readonly Dictionary<string, SharpDX.Direct2D1.Bitmap> loadedTilesets;
        private readonly RenderTarget renderTarget;
        private readonly ImagingFactory imagingFactory;
        private bool isDisposed;

        public Direct2DTextureManager(RenderTarget renderTarget)
        {
            this.renderTarget = renderTarget ?? throw new ArgumentNullException(nameof(renderTarget));
            this.loadedTextures = new Dictionary<string, SharpDX.Direct2D1.Bitmap>();
            this.loadedTilesets = new Dictionary<string, SharpDX.Direct2D1.Bitmap>();
            this.imagingFactory = new ImagingFactory();
        }

        /// <summary>
        /// Load a texture from file
        /// </summary>
        public SharpDX.Direct2D1.Bitmap LoadTexture(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
                throw new ArgumentException("File path cannot be null or empty.", nameof(filePath));

            if (loadedTextures.ContainsKey(filePath))
                return loadedTextures[filePath];

            try
            {
                var bitmap = LoadBitmapFromFile(filePath);
                loadedTextures[filePath] = bitmap;
                return bitmap;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to load texture from {filePath}: {ex.Message}");
                var fallbackTexture = CreateFallbackTexture();
                loadedTextures[filePath] = fallbackTexture;
                return fallbackTexture;
            }
        }

        /// <summary>
        /// Load a tileset from raw pixel data
        /// </summary>
        public SharpDX.Direct2D1.Bitmap LoadTileset(string tilesetName, byte[] pixelData, int width, int height, byte[] palette = null)
        {
            if (loadedTilesets.ContainsKey(tilesetName))
                return loadedTilesets[tilesetName];

            try
            {
                var bitmap = CreateBitmapFromPixelData(pixelData, width, height, palette);
                loadedTilesets[tilesetName] = bitmap;
                return bitmap;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to load tileset {tilesetName}: {ex.Message}");
                var fallbackTexture = CreateFallbackTexture();
                loadedTilesets[tilesetName] = fallbackTexture;
                return fallbackTexture;
            }
        }

        /// <summary>
        /// Load a tileset from individual tile graphics
        /// </summary>
        public SharpDX.Direct2D1.Bitmap LoadTilesetFromTiles(string tilesetName, byte[][] tileGraphics, int tileSize, int tileCount, byte[] palette = null)
        {
            if (loadedTilesets.ContainsKey(tilesetName))
                return loadedTilesets[tilesetName];

            try
            {
                // Calculate tileset dimensions (assuming square tiles)
                var tilesPerRow = (int)Math.Ceiling(Math.Sqrt(tileCount));
                var tilesetWidth = tilesPerRow * tileSize;
                var tilesetHeight = tilesPerRow * tileSize;

                // Create combined pixel data
                var combinedPixels = new byte[tilesetWidth * tilesetHeight * 4]; // RGBA

                for (int tileIndex = 0; tileIndex < tileCount; tileIndex++)
                {
                    var tileRow = tileIndex / tilesPerRow;
                    var tileCol = tileIndex % tilesPerRow;
                    var tileX = tileCol * tileSize;
                    var tileY = tileRow * tileSize;

                    if (tileGraphics[tileIndex] != null)
                    {
                        // Copy tile pixels to combined tileset
                        for (int y = 0; y < tileSize; y++)
                        {
                            for (int x = 0; x < tileSize; x++)
                            {
                                var sourceIndex = y * tileSize + x;
                                var destIndex = ((tileY + y) * tilesetWidth + (tileX + x)) * 4;

                                if (sourceIndex < tileGraphics[tileIndex].Length)
                                {
                                    var paletteIndex = tileGraphics[tileIndex][sourceIndex];
                                    var color = GetColorFromPalette(paletteIndex, palette);
                                    
                                    combinedPixels[destIndex] = color.R;     // R
                                    combinedPixels[destIndex + 1] = color.G; // G
                                    combinedPixels[destIndex + 2] = color.B; // B
                                    combinedPixels[destIndex + 3] = color.A; // A
                                }
                            }
                        }
                    }
                }

                var bitmap = CreateBitmapFromPixelData(combinedPixels, tilesetWidth, tilesetHeight);
                loadedTilesets[tilesetName] = bitmap;
                return bitmap;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to load tileset {tilesetName} from tiles: {ex.Message}");
                var fallbackTexture = CreateFallbackTexture();
                loadedTilesets[tilesetName] = fallbackTexture;
                return fallbackTexture;
            }
        }

        /// <summary>
        /// Get a loaded texture
        /// </summary>
        public SharpDX.Direct2D1.Bitmap GetTexture(string filePath)
        {
            return loadedTextures.ContainsKey(filePath) ? loadedTextures[filePath] : null;
        }

        /// <summary>
        /// Get a loaded tileset
        /// </summary>
        public SharpDX.Direct2D1.Bitmap GetTileset(string tilesetName)
        {
            return loadedTilesets.ContainsKey(tilesetName) ? loadedTilesets[tilesetName] : null;
        }

        /// <summary>
        /// Check if a texture is loaded
        /// </summary>
        public bool IsTextureLoaded(string filePath)
        {
            return loadedTextures.ContainsKey(filePath);
        }

        /// <summary>
        /// Check if a tileset is loaded
        /// </summary>
        public bool IsTilesetLoaded(string tilesetName)
        {
            return loadedTilesets.ContainsKey(tilesetName);
        }

        /// <summary>
        /// Unload a texture
        /// </summary>
        public void UnloadTexture(string filePath)
        {
            if (loadedTextures.ContainsKey(filePath))
            {
                loadedTextures[filePath]?.Dispose();
                loadedTextures.Remove(filePath);
            }
        }

        /// <summary>
        /// Unload a tileset
        /// </summary>
        public void UnloadTileset(string tilesetName)
        {
            if (loadedTilesets.ContainsKey(tilesetName))
            {
                loadedTilesets[tilesetName]?.Dispose();
                loadedTilesets.Remove(tilesetName);
            }
        }

        /// <summary>
        /// Unload all textures and tilesets
        /// </summary>
        public void UnloadAll()
        {
            foreach (var texture in loadedTextures.Values)
            {
                texture?.Dispose();
            }
            loadedTextures.Clear();

            foreach (var tileset in loadedTilesets.Values)
            {
                tileset?.Dispose();
            }
            loadedTilesets.Clear();
        }

        /// <summary>
        /// Get the number of loaded textures
        /// </summary>
        public int LoadedTextureCount => loadedTextures.Count;

        /// <summary>
        /// Get the number of loaded tilesets
        /// </summary>
        public int LoadedTilesetCount => loadedTilesets.Count;

        private SharpDX.Direct2D1.Bitmap LoadBitmapFromFile(string filePath)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException($"Texture file not found: {filePath}");

            try
            {
                // For now, create a fallback texture since WIC loading is complex
                // TODO: Implement proper WIC-based texture loading
                Console.WriteLine($"Texture loading from file not yet implemented: {filePath}");
                return CreateFallbackTexture();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to load texture from {filePath}: {ex.Message}");
                return CreateFallbackTexture();
            }
        }

        private SharpDX.Direct2D1.Bitmap CreateBitmapFromPixelData(byte[] pixelData, int width, int height, byte[] palette = null)
        {
            try
            {
                // Create a simple bitmap from pixel data
                var bitmapProperties = new BitmapProperties(
                    new SharpDX.Direct2D1.PixelFormat(SharpDX.DXGI.Format.B8G8R8A8_UNorm, SharpDX.Direct2D1.AlphaMode.Premultiplied),
                    96, 96);

                // For now, create a fallback texture since direct pixel data creation is complex
                // TODO: Implement proper pixel data to bitmap conversion
                Console.WriteLine("Direct pixel data to bitmap conversion not yet implemented");
                return CreateFallbackTexture();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to create bitmap from pixel data: {ex.Message}");
                return CreateFallbackTexture();
            }
        }

        private SharpDX.Direct2D1.Bitmap CreateFallbackTexture()
        {
            // Create a 32x32 checkerboard pattern as fallback
            var width = 32;
            var height = 32;
            var pixelData = new byte[width * height * 4];

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    var index = (y * width + x) * 4;
                    var isEven = ((x / 8) + (y / 8)) % 2 == 0;
                    
                    if (isEven)
                    {
                        pixelData[index] = 255;     // R
                        pixelData[index + 1] = 255; // G
                        pixelData[index + 2] = 255; // B
                        pixelData[index + 3] = 255; // A
                    }
                    else
                    {
                        pixelData[index] = 128;     // R
                        pixelData[index + 1] = 128; // G
                        pixelData[index + 2] = 128; // B
                        pixelData[index + 3] = 255; // A
                    }
                }
            }

            return CreateBitmapFromPixelData(pixelData, width, height);
        }

        private Color GetColorFromPalette(byte paletteIndex, byte[] palette)
        {
            if (palette == null || paletteIndex >= palette.Length / 3)
            {
                // Return a default color if no palette or invalid index
                return Color.FromArgb(255, paletteIndex, paletteIndex, paletteIndex);
            }

            var baseIndex = paletteIndex * 3;
            var r = palette[baseIndex];
            var g = palette[baseIndex + 1];
            var b = palette[baseIndex + 2];

            return Color.FromArgb(255, r, g, b);
        }

        public void Dispose()
        {
            if (!isDisposed)
            {
                UnloadAll();
                imagingFactory?.Dispose();
                isDisposed = true;
            }
        }

        public bool IsDisposed => isDisposed;
    }
}
