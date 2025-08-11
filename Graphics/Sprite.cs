using System;
using System.Drawing;
using OHRRPGCEDX.Graphics;

namespace OHRRPGCEDX.Graphics
{
    /// <summary>
    /// Sprite rendering system for OHRRPGCE (Direct2D compatible)
    /// </summary>
    public class Sprite : IDisposable
    {
        private Point position;
        private Size size;
        private float scale;
        private Color color;
        private string texturePath;
        private SharpDX.Direct2D1.Bitmap texture;
        private int currentFrame;
        private int totalFrames;
        private float animationSpeed;
        private float animationTimer;

        public Point Position { get => position; set => position = value; }
        public Size Size { get => size; set => size = value; }
        public float Scale { get => scale; set => scale = value; }
        public Color Color { get => color; set => color = value; }
        public string TexturePath { get => texturePath; }
        public SharpDX.Direct2D1.Bitmap Texture { get => texture; }
        public int CurrentFrame { get => currentFrame; set => currentFrame = value; }
        public int TotalFrames { get => totalFrames; set => totalFrames = value; }
        public float AnimationSpeed { get => animationSpeed; set => animationSpeed = value; }

        public Sprite()
        {
            position = new Point(0, 0);
            size = new Size(32, 32);
            scale = 1.0f;
            color = Color.White;
            texturePath = "";
            texture = null;
            currentFrame = 0;
            totalFrames = 1;
            animationSpeed = 1.0f;
            animationTimer = 0.0f;
        }

        public bool LoadTexture(string filePath)
        {
            texturePath = filePath;
            // Actual texture loading will be done by the texture manager
            return true;
        }

        public void SetTexture(SharpDX.Direct2D1.Bitmap bitmap)
        {
            texture = bitmap;
            if (bitmap != null)
            {
                // Convert from SharpDX.Size2F (float) to System.Drawing.Size (int)
                size = new Size((int)bitmap.Size.Width, (int)bitmap.Size.Height);
            }
        }

        public void SetPosition(Point newPosition)
        {
            position = newPosition;
        }

        public void SetSize(Size newSize)
        {
            size = newSize;
        }

        public void SetScale(float newScale)
        {
            scale = newScale;
        }

        public void SetColor(Color newColor)
        {
            color = newColor;
        }

        public void Update(float deltaTime)
        {
            if (totalFrames > 1)
            {
                animationTimer += deltaTime * animationSpeed;
                if (animationTimer >= 1.0f)
                {
                    currentFrame = (currentFrame + 1) % totalFrames;
                    animationTimer = 0.0f;
                }
            }
        }

        public void Render(GraphicsSystem graphicsSystem)
        {
            if (texture != null)
            {
                // Draw actual texture
                var destX = position.X;
                var destY = position.Y;
                var destWidth = (int)(size.Width * scale);
                var destHeight = (int)(size.Height * scale);

                if (totalFrames > 1)
                {
                    // Animated sprite - draw current frame
                    var frameWidth = (int)(size.Width / totalFrames);
                    var sourceX = (int)(currentFrame * frameWidth);
                    graphicsSystem.DrawSpriteRegion(texture, sourceX, 0, frameWidth, size.Height, destX, destY);
                }
                else
                {
                    // Static sprite
                    graphicsSystem.DrawSprite(texture, destX, destY, scale, scale, 1.0f);
                }
            }
            else
            {
                // Fallback to colored rectangle
                var destX = position.X;
                var destY = position.Y;
                var destWidth = (int)(size.Width * scale);
                var destHeight = (int)(size.Height * scale);

                graphicsSystem.FillRectangle(destX, destY, destWidth, destHeight, color);
                graphicsSystem.DrawRectangle(destX, destY, destWidth, destHeight, Color.Black, 1.0f);

                // Draw frame info for debugging
                if (totalFrames > 1)
                {
                    graphicsSystem.DrawText($"Frame: {currentFrame + 1}/{totalFrames}", destX + 2, destY + 2, Color.White);
                }
            }
        }

        public void Dispose()
        {
            // No Direct3D resources to dispose
            texture = null;
        }
    }
}
