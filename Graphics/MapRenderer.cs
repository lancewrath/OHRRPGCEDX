using System;
using System.Collections.Generic;
using System.Drawing;
using OHRRPGCEDX.GameData;

namespace OHRRPGCEDX.Graphics
{
    /// <summary>
    /// Map rendering system for OHRRPGCE
    /// </summary>
    public class MapRenderer : IDisposable
    {
        private Map currentMap;
        private TilesetData currentTileset;
        private int tileSize = 32;
        private SharpDX.Direct2D1.Bitmap tilesetBitmap;

        public MapRenderer()
        {
            currentMap = null;
            currentTileset = null;
            tilesetBitmap = null;
        }

        public void SetMap(Map map, TilesetData tileset)
        {
            currentMap = map;
            currentTileset = tileset;
            tilesetBitmap = null; // Will be loaded when needed
        }

        public void SetTilesetBitmap(SharpDX.Direct2D1.Bitmap bitmap)
        {
            tilesetBitmap = bitmap;
        }

        public void Render(GraphicsSystem graphicsSystem)
        {
            if (currentMap == null)
            {
                Console.WriteLine("MapRenderer: No current map, using fallback");
                RenderFallbackMap(graphicsSystem);
                return;
            }

            Console.WriteLine($"MapRenderer: Rendering map {currentMap.Name} ({currentMap.Width}x{currentMap.Height})");
            Console.WriteLine($"MapRenderer: Tileset available: {currentTileset != null}, TileCount: {currentTileset?.TileCount ?? 0}");
            
            RenderMapLayers(graphicsSystem);
            RenderMapInfo(graphicsSystem);
        }

        private void RenderMapLayers(GraphicsSystem graphicsSystem)
        {
            if (currentMap.LayerData == null || currentMap.LayerData.Length == 0)
            {
                // Fallback to 1D tile array if no layer data
                RenderMapLayer(graphicsSystem, 0);
                return;
            }

            for (int layer = 0; layer < currentMap.LayerData.Length; layer++)
            {
                RenderMapLayer(graphicsSystem, layer);
            }
        }

        private void RenderMapLayer(GraphicsSystem graphicsSystem, int layer)
        {
            if (currentMap.LayerData != null && layer < currentMap.LayerData.Length)
            {
                var layerData = currentMap.LayerData[layer];
                Console.WriteLine($"MapRenderer: Rendering layer {layer} with {currentMap.Width}x{currentMap.Height} tiles");
                for (int y = 0; y < currentMap.Height; y++)
                {
                    for (int x = 0; x < currentMap.Width; x++)
                    {
                        var tileId = layerData[y, x];
                        RenderTile(graphicsSystem, x, y, tileId, layer);
                    }
                }
            }
            else if (currentMap.Tiles != null)
            {
                // Fallback to 1D tile array
                Console.WriteLine($"MapRenderer: Using fallback 1D tiles array, length: {currentMap.Tiles.Length}");
                for (int y = 0; y < currentMap.Height; y++)
                {
                    for (int x = 0; x < currentMap.Width; x++)
                    {
                        var tileIndex = y * currentMap.Width + x;
                        if (tileIndex < currentMap.Tiles.Length)
                        {
                            var tileId = currentMap.Tiles[tileIndex];
                            RenderTile(graphicsSystem, x, y, tileId, layer);
                        }
                    }
                }
            }
            else
            {
                Console.WriteLine("MapRenderer: No tile data available for layer rendering");
            }
        }

        private void RenderTile(GraphicsSystem graphicsSystem, int x, int y, int tileId, int layer)
        {
            var screenX = x * tileSize;
            var screenY = y * tileSize;

            if (currentTileset != null && tileId >= 0 && tileId < currentTileset.TileCount)
            {
                // Render actual tile using tileset data
                if (x == 0 && y == 0) // Only log for first few tiles to avoid spam
                {
                    Console.WriteLine($"MapRenderer: Rendering tile {tileId} at ({x},{y}) using tileset data");
                }
                RenderTileFromTileset(graphicsSystem, screenX, screenY, tileId, layer);
            }
            else
            {
                // Fallback to colored rectangle
                if (x == 0 && y == 0) // Only log for first few tiles to avoid spam
                {
                    Console.WriteLine($"MapRenderer: Rendering tile {tileId} at ({x},{y}) using fallback color");
                }
                var color = GetTileColor(tileId, layer);
                graphicsSystem.FillRectangle(screenX, screenY, tileSize, tileSize, color);
                
                // Draw tile ID for debugging
                graphicsSystem.DrawText(tileId.ToString(), screenX + 2, screenY + 2, Color.White);
            }
        }

        private void RenderTileFromTileset(GraphicsSystem graphicsSystem, int screenX, int screenY, int tileId, int layer)
        {
            if (currentTileset?.TileGraphics == null || tileId >= currentTileset.TileGraphics.Length)
                return;

            var tileData = currentTileset.TileGraphics[tileId];
            if (tileData == null || tileData.Length == 0)
                return;

            // Create a visual representation of the tile based on its graphics data
            RenderTileGraphics(graphicsSystem, screenX, screenY, tileData, layer);
        }

        private void RenderTileGraphics(GraphicsSystem graphicsSystem, int screenX, int screenY, byte[] tileData, int layer)
        {
            // Calculate tile dimensions (assuming square tiles)
            var tileWidth = currentTileset.TileSize;
            var tileHeight = currentTileset.TileSize;
            
            // Create a pattern based on the tile data
            if (tileData.Length > 0)
            {
                // Use the first few bytes of tile data to create a unique visual pattern
                var patternType = tileData[0] % 8; // 8 different pattern types
                var baseColor = GetTileBaseColor(tileData, layer);
                
                switch (patternType)
                {
                    case 0: // Solid color
                        graphicsSystem.FillRectangle(screenX, screenY, tileWidth, tileHeight, baseColor);
                        break;
                        
                    case 1: // Checkerboard pattern
                        RenderCheckerboardTile(graphicsSystem, screenX, screenY, tileWidth, tileHeight, baseColor);
                        break;
                        
                    case 2: // Stripes pattern
                        RenderStripedTile(graphicsSystem, screenX, screenY, tileWidth, tileHeight, baseColor);
                        break;
                        
                    case 3: // Dots pattern
                        RenderDottedTile(graphicsSystem, screenX, screenY, tileWidth, tileHeight, baseColor);
                        break;
                        
                    case 4: // Gradient pattern
                        RenderGradientTile(graphicsSystem, screenX, screenY, tileWidth, tileHeight, baseColor);
                        break;
                        
                    case 5: // Border pattern
                        RenderBorderedTile(graphicsSystem, screenX, screenY, tileWidth, tileHeight, baseColor);
                        break;
                        
                    case 6: // Cross pattern
                        RenderCrossTile(graphicsSystem, screenX, screenY, tileWidth, tileHeight, baseColor);
                        break;
                        
                    case 7: // Diagonal pattern
                        RenderDiagonalTile(graphicsSystem, screenX, screenY, tileWidth, tileHeight, baseColor);
                        break;
                }
                
                // Add a subtle border to distinguish tiles
                graphicsSystem.DrawRectangle(screenX, screenY, tileWidth, tileHeight, Color.FromArgb(50, 255, 255, 255), 0.5f);
            }
            else
            {
                // Empty tile - just draw a subtle outline
                graphicsSystem.DrawRectangle(screenX, screenY, tileWidth, tileHeight, Color.FromArgb(30, 128, 128, 128), 0.5f);
            }
        }

        private Color GetTileBaseColor(byte[] tileData, int layer)
        {
            if (tileData.Length == 0) return Color.Black;
            
            // Use tile data to generate a consistent color for each tile
            var r = (byte)((tileData[0] * 7 + tileData.Length * 3) % 256);
            var g = (byte)((tileData[0] * 11 + tileData.Length * 5) % 256);
            var b = (byte)((tileData[0] * 13 + tileData.Length * 7) % 256);
            
            // Adjust brightness based on layer
            var brightness = 0.5f + (layer * 0.2f);
            r = (byte)(r * brightness);
            g = (byte)(g * brightness);
            b = (byte)(b * brightness);
            
            return Color.FromArgb(255, r, g, b);
        }

        private void RenderCheckerboardTile(GraphicsSystem graphicsSystem, int x, int y, int width, int height, Color baseColor)
        {
            var checkSize = Math.Max(2, width / 8);
            var altColor = Color.FromArgb(255, 
                Math.Max(0, baseColor.R - 40), 
                Math.Max(0, baseColor.G - 40), 
                Math.Max(0, baseColor.B - 40));
            
            for (int checkY = 0; checkY < height; checkY += checkSize)
            {
                for (int checkX = 0; checkX < width; checkX += checkSize)
                {
                    var isEven = ((checkX / checkSize) + (checkY / checkSize)) % 2 == 0;
                    var color = isEven ? baseColor : altColor;
                    graphicsSystem.FillRectangle(x + checkX, y + checkY, 
                        Math.Min(checkSize, width - checkX), 
                        Math.Min(checkSize, height - checkY), color);
                }
            }
        }

        private void RenderStripedTile(GraphicsSystem graphicsSystem, int x, int y, int width, int height, Color baseColor)
        {
            var stripeWidth = Math.Max(1, width / 4);
            var altColor = Color.FromArgb(255, 
                Math.Min(255, baseColor.R + 30), 
                Math.Min(255, baseColor.G + 30), 
                Math.Min(255, baseColor.B + 30));
            
            for (int stripeX = 0; stripeX < width; stripeX += stripeWidth * 2)
            {
                graphicsSystem.FillRectangle(x + stripeX, y, Math.Min(stripeWidth, width - stripeX), height, baseColor);
                if (stripeX + stripeWidth < width)
                {
                    graphicsSystem.FillRectangle(x + stripeX + stripeWidth, y, 
                        Math.Min(stripeWidth, width - stripeX - stripeWidth), height, altColor);
                }
            }
        }

        private void RenderDottedTile(GraphicsSystem graphicsSystem, int x, int y, int width, int height, Color baseColor)
        {
            var dotSize = Math.Max(2, Math.Min(width, height) / 8);
            var altColor = Color.FromArgb(255, 
                Math.Min(255, baseColor.R + 50), 
                Math.Min(255, baseColor.G + 50), 
                Math.Min(255, baseColor.B + 50));
            
            // Fill background
            graphicsSystem.FillRectangle(x, y, width, height, baseColor);
            
            // Add dots
            for (int dotY = dotSize; dotY < height - dotSize; dotY += dotSize * 2)
            {
                for (int dotX = dotSize; dotX < width - dotSize; dotX += dotSize * 2)
                {
                    graphicsSystem.FillRectangle(x + dotX - dotSize/2, y + dotY - dotSize/2, dotSize, dotSize, altColor);
                }
            }
        }

        private void RenderGradientTile(GraphicsSystem graphicsSystem, int x, int y, int width, int height, Color baseColor)
        {
            var steps = Math.Min(width, height);
            for (int i = 0; i < steps; i++)
            {
                var factor = (float)i / steps;
                var color = Color.FromArgb(255,
                    (byte)(baseColor.R * (1 - factor) + 255 * factor),
                    (byte)(baseColor.G * (1 - factor) + 255 * factor),
                    (byte)(baseColor.B * (1 - factor) + 255 * factor));
                
                graphicsSystem.FillRectangle(x + i, y + i, width - i * 2, height - i * 2, color);
            }
        }

        private void RenderBorderedTile(GraphicsSystem graphicsSystem, int x, int y, int width, int height, Color baseColor)
        {
            var borderWidth = Math.Max(1, Math.Min(width, height) / 8);
            var borderColor = Color.FromArgb(255, 
                Math.Max(0, baseColor.R - 60), 
                Math.Max(0, baseColor.G - 60), 
                Math.Max(0, baseColor.B - 60));
            
            // Fill center
            graphicsSystem.FillRectangle(x + borderWidth, y + borderWidth, 
                width - borderWidth * 2, height - borderWidth * 2, baseColor);
            
            // Draw border
            graphicsSystem.FillRectangle(x, y, width, borderWidth, borderColor); // Top
            graphicsSystem.FillRectangle(x, y + height - borderWidth, width, borderWidth, borderColor); // Bottom
            graphicsSystem.FillRectangle(x, y, borderWidth, height, borderColor); // Left
            graphicsSystem.FillRectangle(x + width - borderWidth, y, borderWidth, height, borderColor); // Right
        }

        private void RenderCrossTile(GraphicsSystem graphicsSystem, int x, int y, int width, int height, Color baseColor)
        {
            var crossWidth = Math.Max(2, Math.Min(width, height) / 6);
            var crossColor = Color.FromArgb(255, 
                Math.Min(255, baseColor.R + 40), 
                Math.Min(255, baseColor.G + 40), 
                Math.Min(255, baseColor.B + 40));
            
            // Fill background
            graphicsSystem.FillRectangle(x, y, width, height, baseColor);
            
            // Draw cross
            var centerX = x + width / 2 - crossWidth / 2;
            var centerY = y + height / 2 - crossWidth / 2;
            
            graphicsSystem.FillRectangle(centerX, y, crossWidth, height, crossColor); // Vertical
            graphicsSystem.FillRectangle(x, centerY, width, crossWidth, crossColor); // Horizontal
        }

        private void RenderDiagonalTile(GraphicsSystem graphicsSystem, int x, int y, int width, int height, Color baseColor)
        {
            var altColor = Color.FromArgb(255, 
                Math.Min(255, baseColor.R + 35), 
                Math.Min(255, baseColor.G + 35), 
                Math.Min(255, baseColor.B + 35));
            
            // Fill background
            graphicsSystem.FillRectangle(x, y, width, height, baseColor);
            
            // Draw diagonal stripes
            var stripeWidth = Math.Max(2, Math.Min(width, height) / 8);
            for (int i = -height; i < width + height; i += stripeWidth * 2)
            {
                var startX = x + i;
                var startY = y;
                var endX = x + i + height;
                var endY = y + height;
                
                if (startX < x + width && endX > x)
                {
                    graphicsSystem.DrawLine(startX, startY, endX, endY, altColor, stripeWidth);
                }
            }
        }

        private Color GetTileColor(int tileId, int layer)
        {
            if (tileId == 0) return Color.Black; // Empty tile
            
            // Different colors for different layers
            switch (layer)
            {
                case 0: return Color.Green;   // Ground layer
                case 1: return Color.Brown;   // Object layer
                case 2: return Color.Blue;    // Overlay layer
                default: return Color.Gray;   // Other layers
            }
        }

        private void RenderMapInfo(GraphicsSystem graphicsSystem)
        {
            var infoY = 10;
            graphicsSystem.DrawText($"Map: {currentMap?.Name ?? "Unknown"}", 10, infoY, Color.White);
            graphicsSystem.DrawText($"Size: {currentMap?.Width ?? 0}x{currentMap?.Height ?? 0}", 10, infoY + 20, Color.White);
            graphicsSystem.DrawText($"Tileset: {currentTileset?.ID ?? -1}", 10, infoY + 40, Color.White);
            graphicsSystem.DrawText($"Layers: {currentMap?.LayerData?.Length ?? 1}", 10, infoY + 60, Color.White);
        }

        private void RenderFallbackMap(GraphicsSystem graphicsSystem)
        {
            // Draw a simple grid pattern
            for (int y = 0; y < 20; y++)
            {
                for (int x = 0; x < 25; x++)
                {
                    var color = ((x + y) % 2 == 0) ? Color.DarkGray : Color.LightGray;
                    graphicsSystem.FillRectangle(x * tileSize, y * tileSize, tileSize, tileSize, color);
                }
            }

            graphicsSystem.DrawText("No map loaded", 10, 10, Color.Red);
        }

        public void Dispose()
        {
            // No Direct3D resources to dispose
        }
    }
}
