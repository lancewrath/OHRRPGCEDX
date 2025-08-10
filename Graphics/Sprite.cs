using System;
using System.Collections.Generic;
using SharpDX;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using SharpDX.Mathematics.Interop;

namespace OHRRPGCEDX.Graphics
{
    public class Sprite : IDisposable
    {
        private SharpDX.Direct3D11.Device device;
        private DeviceContext context;
        private ShaderSystem shaderSystem;
        private TextureManager textureManager;
        private SharpDX.Direct3D11.Buffer vertexBuffer;
        private SharpDX.Direct3D11.Buffer indexBuffer;
        private bool isDisposed = false;

        // Sprite properties
        private Vector2 position;
        private Vector2 size;
        private float rotation;
        private Vector2 scale;
        private Vector4 color;
        private string texturePath;
        private ShaderResourceView texture;

        // Animation properties
        private int currentFrame;
        private int totalFrames;
        private float frameTime;
        private float animationSpeed;
        private bool isAnimating;

        public Sprite(SharpDX.Direct3D11.Device device, DeviceContext context, ShaderSystem shaderSystem, TextureManager textureManager)
        {
            this.device = device;
            this.context = context;
            this.shaderSystem = shaderSystem;
            this.textureManager = textureManager;
            
            // Initialize default values
            position = Vector2.Zero;
            size = new Vector2(32, 32); // Default sprite size
            rotation = 0.0f;
            scale = Vector2.One;
            color = Vector4.One;
            currentFrame = 0;
            totalFrames = 1;
            frameTime = 0.0f;
            animationSpeed = 1.0f;
            isAnimating = false;
        }

        public bool LoadTexture(string filePath)
        {
            try
            {
                texturePath = filePath;
                texture = textureManager.LoadTexture(filePath);
                return texture != null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to load texture for sprite: {ex.Message}");
                return false;
            }
        }

        public void SetPosition(Vector2 newPosition)
        {
            position = newPosition;
        }

        public void SetSize(Vector2 newSize)
        {
            size = newSize;
        }

        public void SetRotation(float newRotation)
        {
            rotation = newRotation;
        }

        public void SetScale(Vector2 newScale)
        {
            scale = newScale;
        }

        public void SetColor(Vector4 newColor)
        {
            color = newColor;
        }

        public void SetAnimation(int frames, float speed)
        {
            totalFrames = Math.Max(1, frames);
            animationSpeed = speed;
            isAnimating = speed > 0;
            currentFrame = 0;
            frameTime = 0.0f;
        }

        public void Update(float deltaTime)
        {
            if (!isAnimating) return;

            frameTime += deltaTime * animationSpeed;
            if (frameTime >= 1.0f)
            {
                frameTime -= 1.0f;
                currentFrame = (currentFrame + 1) % totalFrames;
            }
        }

        public void Render(Matrix worldViewProjection)
        {
            if (isDisposed || texture == null)
                return;

            try
            {
                // Set shaders and input layout
                context.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding(vertexBuffer, Utilities.SizeOf<VertexPositionColor>(), 0));
                context.InputAssembler.SetIndexBuffer(indexBuffer, Format.R32_UInt, 0);
                context.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList;

                context.VertexShader.Set(shaderSystem.GetSpriteVertexShader());
                context.PixelShader.Set(shaderSystem.GetSpritePixelShader());
                context.InputAssembler.InputLayout = shaderSystem.GetSpriteInputLayout();

                // Set texture
                context.PixelShader.SetShaderResource(0, texture);

                // TODO: Set constant buffer with worldViewProjection matrix and sprite transform
                // For now, we'll just render without transformations
                
                // Draw the sprite
                context.DrawIndexed(6, 0, 0); // 6 indices for a quad (2 triangles)
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error rendering sprite: {ex.Message}");
            }
        }

        public void Dispose()
        {
            if (!isDisposed)
            {
                vertexBuffer?.Dispose();
                indexBuffer?.Dispose();
                isDisposed = true;
            }
        }

        public bool IsDisposed => isDisposed;

        // Getters for sprite properties
        public Vector2 Position => position;
        public Vector2 Size => size;
        public float Rotation => rotation;
        public Vector2 Scale => scale;
        public Vector4 Color => color;
        public bool IsAnimating => isAnimating;
        public int CurrentFrame => currentFrame;
        public int TotalFrames => totalFrames;
    }
}
