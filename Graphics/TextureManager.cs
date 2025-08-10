using System;
using System.Collections.Generic;
using System.IO;
using SharpDX;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using SharpDX.Mathematics.Interop;

namespace OHRRPGCEDX.Graphics
{
    /// <summary>
    /// Texture manager for loading and managing DirectX textures
    /// This provides texture functionality needed for sprites and tiles
    /// </summary>
    public class TextureManager : IDisposable
    {
        private readonly Dictionary<string, ShaderResourceView> loadedTextures;
        private readonly SharpDX.Direct3D11.Device device;
        private bool isDisposed;

        public TextureManager(SharpDX.Direct3D11.Device device)
        {
            this.device = device ?? throw new ArgumentNullException(nameof(device));
            this.loadedTextures = new Dictionary<string, ShaderResourceView>();
        }

        public ShaderResourceView LoadTexture(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
                throw new ArgumentException("File path cannot be null or empty.", nameof(filePath));

            if (loadedTextures.ContainsKey(filePath))
                return loadedTextures[filePath];

            try
            {
                // For now, create a default texture since we can't load from file without WIC
                // In a real implementation, you'd want to implement a custom image loader
                var texture = CreateDefaultTexture();
                loadedTextures[filePath] = texture;
                return texture;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to load texture from {filePath}: {ex.Message}");
                var fallbackTexture = CreateDefaultTexture();
                loadedTextures[filePath] = fallbackTexture;
                return fallbackTexture;
            }
        }

        public ShaderResourceView LoadTextureFromMemory(byte[] data, int width, int height)
        {
            if (data == null || data.Length == 0)
                throw new ArgumentException("Data cannot be null or empty.", nameof(data));

            if (width <= 0 || height <= 0)
                throw new ArgumentException("Width and height must be positive.", nameof(width), nameof(height));

            try
            {
                var texture2D = new Texture2D(device, new Texture2DDescription
                {
                    Width = width,
                    Height = height,
                    MipLevels = 1,
                    ArraySize = 1,
                    Format = Format.R8G8B8A8_UNorm,
                    Usage = ResourceUsage.Default,
                    BindFlags = BindFlags.ShaderResource,
                    CpuAccessFlags = CpuAccessFlags.None,
                    OptionFlags = ResourceOptionFlags.None,
                    SampleDescription = new SampleDescription(1, 0)
                });

                var dataBox = device.ImmediateContext.MapSubresource(texture2D, 0, MapMode.WriteDiscard, MapFlags.None);
                
                // Copy the data to the texture
                // Note: This is a simplified implementation - you might need to handle different pixel formats
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        var pixelIndex = (y * width + x) * 4;
                        if (pixelIndex + 3 < data.Length)
                        {
                            var destIndex = dataBox.DataPointer + (y * dataBox.RowPitch) + (x * 4);
                            System.Runtime.InteropServices.Marshal.WriteByte(destIndex, data[pixelIndex]);
                            System.Runtime.InteropServices.Marshal.WriteByte(destIndex + 1, data[pixelIndex + 1]);
                            System.Runtime.InteropServices.Marshal.WriteByte(destIndex + 2, data[pixelIndex + 2]);
                            System.Runtime.InteropServices.Marshal.WriteByte(destIndex + 3, data[pixelIndex + 3]);
                        }
                    }
                }

                device.ImmediateContext.UnmapSubresource(texture2D, 0);

                var shaderResourceView = new ShaderResourceView(device, texture2D);
                texture2D.Dispose();

                return shaderResourceView;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to create texture from memory: {ex.Message}");
                return CreateDefaultTexture();
            }
        }

        private ShaderResourceView CreateDefaultTexture()
        {
            // Create a simple checkerboard pattern texture
            var width = 64;
            var height = 64;
            var data = new byte[width * height * 4];

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    var pixelIndex = (y * width + x) * 4;
                    var isChecker = ((x / 8) + (y / 8)) % 2 == 0;
                    
                    if (isChecker)
                    {
                        data[pixelIndex] = 255;     // R
                        data[pixelIndex + 1] = 255; // G
                        data[pixelIndex + 2] = 255; // B
                        data[pixelIndex + 3] = 255; // A
                    }
                    else
                    {
                        data[pixelIndex] = 128;     // R
                        data[pixelIndex + 1] = 128; // G
                        data[pixelIndex + 2] = 128; // B
                        data[pixelIndex + 3] = 255; // A
                    }
                }
            }

            return LoadTextureFromMemory(data, width, height);
        }

        public ShaderResourceView CreateColorTexture(byte r, byte g, byte b, byte a = 255)
        {
            var data = new byte[] { r, g, b, a };
            return LoadTextureFromMemory(data, 1, 1);
        }

        public ShaderResourceView GetTexture(string filePath)
        {
            return loadedTextures.ContainsKey(filePath) ? loadedTextures[filePath] : null;
        }

        public bool IsTextureLoaded(string filePath)
        {
            return loadedTextures.ContainsKey(filePath);
        }

        public void UnloadTexture(string filePath)
        {
            if (loadedTextures.ContainsKey(filePath))
            {
                loadedTextures[filePath].Dispose();
                loadedTextures.Remove(filePath);
            }
        }

        public void UnloadAllTextures()
        {
            foreach (var texture in loadedTextures.Values)
            {
                texture.Dispose();
            }
            loadedTextures.Clear();
        }

        public int LoadedTextureCount => loadedTextures.Count;

        public long GetEstimatedMemoryUsage()
        {
            // Rough estimate: assume each texture uses about 4 bytes per pixel
            long totalBytes = 0;
            foreach (var texture in loadedTextures.Values)
            {
                // This is a simplified estimate - in practice you'd want to track actual texture dimensions
                totalBytes += 1024 * 1024; // Assume 1MB per texture
            }
            return totalBytes;
        }

        public void Dispose()
        {
            if (!isDisposed)
            {
                UnloadAllTextures();
                isDisposed = true;
            }
        }

        public bool IsDisposed => isDisposed;
    }
}
