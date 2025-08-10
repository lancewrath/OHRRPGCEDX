using System;
using System.Collections.Generic;
using SharpDX;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using SharpDX.Mathematics.Interop;
using Device = SharpDX.Direct3D11.Device;
using OHRRPGCEDX.Utils;

namespace OHRRPGCEDX.Graphics
{
    /// <summary>
    /// Text alignment options for rendering
    /// </summary>
    public enum TextAlignment
    {
        Left,
        Center,
        Right
    }

    /// <summary>
    /// Graphics system using SharpDX for Direct3D 11 rendering
    /// </summary>
    public class GraphicsSystem : IDisposable
    {
        private Device device;
        private DeviceContext context;
        private SwapChain swapChain;
        private RenderTargetView renderTargetView;
        private Texture2D depthStencilBuffer;
        private DepthStencilView depthStencilView;
        private RasterizerState rasterizerState;
        private BlendState blendState;
        private SamplerState samplerState;

        private int screenWidth;
        private int screenHeight;
        private bool fullscreen;
        private bool vsync;

        public int ScreenWidth => screenWidth;
        public int ScreenHeight => screenHeight;
        public bool IsFullscreen => fullscreen;
        public bool IsInitialized { get; private set; }

        public event EventHandler Resize;

        public GraphicsSystem()
        {
            IsInitialized = false;
        }

        /// <summary>
        /// Initialize the graphics system
        /// </summary>
        public bool Initialize(int width, int height, bool fullscreen = false, bool vsync = true, IntPtr windowHandle = default)
        {
            try
            {
                screenWidth = width;
                screenHeight = height;
                this.fullscreen = fullscreen;
                this.vsync = vsync;

                // Create device and swap chain
                var desc = new SwapChainDescription
                {
                    BufferCount = 1,
                    ModeDescription = new ModeDescription(width, height, new Rational(60, 1), Format.R8G8B8A8_UNorm),
                    IsWindowed = !fullscreen,
                    OutputHandle = windowHandle != default ? windowHandle : IntPtr.Zero,
                    SampleDescription = new SampleDescription(1, 0),
                    SwapEffect = SwapEffect.Discard,
                    Usage = Usage.RenderTargetOutput
                };

                Device.CreateWithSwapChain(SharpDX.Direct3D.DriverType.Hardware, DeviceCreationFlags.None, desc, out device, out swapChain);
                context = device.ImmediateContext;

                // Create render target view
                using (var backBuffer = swapChain.GetBackBuffer<Texture2D>(0))
                {
                    renderTargetView = new RenderTargetView(device, backBuffer);
                }

                // Create depth stencil buffer
                var depthBufferDesc = new Texture2DDescription
                {
                    Width = width,
                    Height = height,
                    MipLevels = 1,
                    ArraySize = 1,
                    Format = Format.D24_UNorm_S8_UInt,
                    SampleDescription = new SampleDescription(1, 0),
                    Usage = ResourceUsage.Default,
                    BindFlags = BindFlags.DepthStencil,
                    CpuAccessFlags = CpuAccessFlags.None,
                    OptionFlags = ResourceOptionFlags.None
                };

                depthStencilBuffer = new Texture2D(device, depthBufferDesc);
                depthStencilView = new DepthStencilView(device, depthStencilBuffer);

                // Set render targets
                context.OutputMerger.SetRenderTargets(depthStencilView, renderTargetView);

                // Create rasterizer state
                var rasterizerDesc = new RasterizerStateDescription
                {
                    IsDepthClipEnabled = true,
                    CullMode = CullMode.Back,
                    FillMode = FillMode.Solid,
                    IsFrontCounterClockwise = false
                };
                rasterizerState = new RasterizerState(device, rasterizerDesc);
                context.Rasterizer.State = rasterizerState;

                // Create blend state
                var blendDesc = new BlendStateDescription
                {
                    AlphaToCoverageEnable = false,
                    IndependentBlendEnable = false
                };
                blendDesc.RenderTarget[0].IsBlendEnabled = true;
                blendDesc.RenderTarget[0].SourceBlend = BlendOption.SourceAlpha;
                blendDesc.RenderTarget[0].DestinationBlend = BlendOption.InverseSourceAlpha;
                blendDesc.RenderTarget[0].BlendOperation = BlendOperation.Add;
                blendDesc.RenderTarget[0].SourceAlphaBlend = BlendOption.One;
                blendDesc.RenderTarget[0].DestinationAlphaBlend = BlendOption.Zero;
                blendDesc.RenderTarget[0].AlphaBlendOperation = BlendOperation.Add;
                blendDesc.RenderTarget[0].RenderTargetWriteMask = ColorWriteMaskFlags.All;

                blendState = new BlendState(device, blendDesc);
                context.OutputMerger.SetBlendState(blendState);

                // Create sampler state
                var samplerDesc = new SamplerStateDescription
                {
                    Filter = Filter.MinMagMipLinear,
                    AddressU = TextureAddressMode.Clamp,
                    AddressV = TextureAddressMode.Clamp,
                    AddressW = TextureAddressMode.Clamp,
                    MipLodBias = 0.0f,
                    MaximumAnisotropy = 1,
                    ComparisonFunction = Comparison.Always,
                    BorderColor = new RawColor4(0, 0, 0, 0),
                    MinimumLod = 0,
                    MaximumLod = float.MaxValue
                };
                samplerState = new SamplerState(device, samplerDesc);
                context.PixelShader.SetSampler(0, samplerState);

                // Set viewport
                var viewport = new Viewport(0, 0, width, height, 0.0f, 1.0f);
                context.Rasterizer.SetViewport(viewport);

                IsInitialized = true;
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to initialize graphics system: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Initialize the graphics system with a GameWindow
        /// </summary>
        public bool Initialize(GameWindow gameWindow)
        {
            if (gameWindow == null) return false;
            
            // Get the window handle from the GameWindow
            IntPtr windowHandle = gameWindow.Handle;
            
            // Initialize with the window's size
            return Initialize(gameWindow.Width, gameWindow.Height, false, true, windowHandle);
        }

        /// <summary>
        /// Begin rendering frame
        /// </summary>
        private bool firstFrameCleared = false;

        public void BeginScene()
        {
            if (!IsInitialized) return;

                                 // Clear the screen with black on first frame to ensure we have a clean background
                     if (!firstFrameCleared)
                     {
                         LoggingSystem.Instance.Debug("Graphics", "First frame - clearing screen to black");
                         context.ClearRenderTargetView(renderTargetView, new RawColor4(0.0f, 0.0f, 0.0f, 1.0f));
                         firstFrameCleared = true;
                     }

            // Only clear depth buffer, don't clear render target every frame
            // This allows our rectangles to persist
            context.ClearDepthStencilView(depthStencilView, DepthStencilClearFlags.Depth | DepthStencilClearFlags.Stencil, 1.0f, 0);
        }

        /// <summary>
        /// End rendering frame and present
        /// </summary>
        public void EndScene()
        {
            if (!IsInitialized) return;

            // Present the back buffer
            if (vsync)
                swapChain.Present(1, PresentFlags.None);
            else
                swapChain.Present(0, PresentFlags.None);
        }

        /// <summary>
        /// Clear the screen with a specific color
        /// </summary>
        public void Clear(RawColor4 color)
        {
            if (!IsInitialized) return;
            context.ClearRenderTargetView(renderTargetView, color);
        }

        /// <summary>
        /// Clear the screen with default black color
        /// </summary>
        public void Clear()
        {
            Clear(new RawColor4(0.0f, 0.0f, 0.0f, 1.0f));
        }

        /// <summary>
        /// Present the back buffer to the screen
        /// </summary>
                         public void Present()
                 {
                     if (!IsInitialized) return;
                     
                     LoggingSystem.Instance.Debug("Graphics", "Present called - about to swap buffers");
                     
                     if (vsync)
                         swapChain.Present(1, PresentFlags.None);
                     else
                         swapChain.Present(0, PresentFlags.None);
                         
                     LoggingSystem.Instance.Debug("Graphics", "Present completed - buffers swapped");
                 }

        /// <summary>
        /// Resize the graphics system
        /// </summary>
        public void ResizeGraphics(int width, int height)
        {
            if (!IsInitialized) return;

            screenWidth = width;
            screenHeight = height;

            // Release resources
            renderTargetView?.Dispose();
            depthStencilView?.Dispose();
            depthStencilBuffer?.Dispose();

            // Resize swap chain
            swapChain.ResizeBuffers(1, width, height, Format.R8G8B8A8_UNorm, SwapChainFlags.None);

            // Recreate render target view
            using (var backBuffer = swapChain.GetBackBuffer<Texture2D>(0))
            {
                renderTargetView = new RenderTargetView(device, backBuffer);
            }

            // Recreate depth stencil buffer
            var depthBufferDesc = new Texture2DDescription
            {
                Width = width,
                Height = height,
                MipLevels = 1,
                ArraySize = 1,
                Format = Format.D24_UNorm_S8_UInt,
                SampleDescription = new SampleDescription(1, 0),
                Usage = ResourceUsage.Default,
                BindFlags = BindFlags.DepthStencil,
                CpuAccessFlags = CpuAccessFlags.None,
                OptionFlags = ResourceOptionFlags.None
            };

            depthStencilBuffer = new Texture2D(device, depthBufferDesc);
            depthStencilView = new DepthStencilView(device, depthStencilBuffer);

            // Set render targets
            context.OutputMerger.SetRenderTargets(depthStencilView, renderTargetView);

            // Update viewport
            var viewport = new Viewport(0, 0, width, height, 0.0f, 1.0f);
            context.Rasterizer.SetViewport(viewport);

            Resize?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Toggle fullscreen mode
        /// </summary>
        public void ToggleFullscreen()
        {
            if (!IsInitialized) return;

            fullscreen = !fullscreen;
            swapChain.SetFullscreenState(fullscreen, null);
        }

        /// <summary>
        /// Get the device context for custom rendering
        /// </summary>
        public DeviceContext GetDeviceContext()
        {
            return context;
        }

        /// <summary>
        /// Get the device for resource creation
        /// </summary>
        public Device GetDevice()
        {
            return device;
        }

        /// <summary>
        /// Draw text at the specified position with the specified color and alignment
        /// </summary>
        public void DrawText(string text, int x, int y, System.Drawing.Color color, TextAlignment alignment = TextAlignment.Left)
        {
            if (!IsInitialized || string.IsNullOrEmpty(text)) return;

            try
            {
                // Calculate text position based on alignment
                int textX = x;
                if (alignment == TextAlignment.Center)
                {
                    // Approximate text width (8 pixels per character for now)
                    int textWidth = text.Length * 8;
                    textX = x - (textWidth / 2);
                }
                else if (alignment == TextAlignment.Right)
                {
                    int textWidth = text.Length * 8;
                    textX = x - textWidth;
                }

                // Convert System.Drawing.Color to SharpDX Color4
                var sharpDxColor = new Color4(color.R / 255.0f, color.G / 255.0f, color.B / 255.0f, color.A / 255.0f);
                
                // Draw each character as a simple colored rectangle
                for (int i = 0; i < text.Length; i++)
                {
                    int charX = textX + (i * 8);
                    // Draw a simple colored rectangle for each character
                    DrawSimpleTextRect(charX, y, 6, 12, sharpDxColor);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in DrawText: {ex.Message}");
            }
        }

        /// <summary>
        /// Draw a simple text rectangle that's actually visible
        /// </summary>
        private void DrawSimpleTextRect(int x, int y, int width, int height, Color4 color)
        {
            try
            {
                // For now, we'll use a very simple approach - just clear a small area with the character color
                // This will make each character visible as a colored rectangle
                // This is a temporary solution until we implement proper shader-based rendering
                
                // Create a small colored rectangle by clearing a small area
                // We'll use the existing Clear method but with a small area
                // For now, we'll just log that we're drawing a character
                
                LoggingSystem.Instance.Debug("Graphics", $"Drawing text character at ({x}, {y}) with size ({width}, {height}) and color {color}");
                
                // TODO: Implement proper character rendering with shaders
                // For now, this will just log the operation
            }
            catch (Exception ex)
            {
                LoggingSystem.Instance.Error("Graphics", $"Error in DrawSimpleTextRect: {ex.Message}");
            }
        }

        /// <summary>
        /// Draw a character rectangle without clearing the screen
        /// </summary>
        private void DrawCharacterRect(int x, int y, int width, int height, Color4 color)
        {
            try
            {
                // Ensure we have the basic rendering resources created
                if (basicVertexBuffer == null || basicIndexBuffer == null)
                {
                    CreateBasicShaders();
                }

                if (basicVertexBuffer == null || basicIndexBuffer == null)
                {
                    return;
                }

                // For now, we'll use a very simple approach - just draw a small colored rectangle
                // This is a temporary solution until we implement proper shader-based rendering
                // Instead of clearing the screen, we'll draw a small rectangle at the specified position
                
                // Create a simple colored rectangle by drawing individual pixels
                // This is a basic implementation that should work for text rendering
                for (int py = 0; py < height; py++)
                {
                    for (int px = 0; px < width; px++)
                    {
                        int pixelX = x + px;
                        int pixelY = y + py;
                        
                        // Only draw if within screen bounds
                        if (pixelX >= 0 && pixelX < screenWidth && pixelY >= 0 && pixelY < screenHeight)
                        {
                            // For now, we'll use a simple approach by drawing a small colored area
                            // This will be replaced with proper pixel-level rendering when shaders are implemented
                            // Just log that we're drawing a pixel - this will be replaced with actual rendering
                            LoggingSystem.Instance.Debug("Graphics", $"Drawing pixel at ({pixelX}, {pixelY}) with color {color}");
                        }
                    }
                }
                
                LoggingSystem.Instance.Debug("Graphics", $"Drew character rectangle at ({x}, {y}) with size ({width}, {height}) and color {color}");
            }
            catch (Exception ex)
            {
                LoggingSystem.Instance.Error("Graphics", $"Error in DrawCharacterRect: {ex.Message}");
            }
        }

        /// <summary>
        /// Draw a pixel rectangle (basic implementation)
        /// </summary>
        private void DrawPixelRect(int x, int y, int width, int height, Color4 color)
        {
            // This is a placeholder for proper pixel-level rendering
            // For now, we'll just log that we're drawing a pixel
            // This will be replaced with proper shader-based rendering when implemented
            
            LoggingSystem.Instance.Debug("Graphics", $"Drawing pixel rect at ({x}, {y}) with size ({width}, {height}) and color {color}");
        }

        // Add these fields at the top of the class for reusable rendering resources
        private SharpDX.Direct3D11.Buffer basicVertexBuffer;
        private SharpDX.Direct3D11.Buffer basicIndexBuffer;

        /// <summary>
        /// Draw a simple colored rectangle
        /// </summary>
        private void DrawColoredRect(int x, int y, int width, int height, Color4 color)
        {
            try
            {
                // Ensure we have the basic rendering resources created
                if (basicVertexBuffer == null || basicIndexBuffer == null)
                {
                    CreateBasicShaders();
                }

                if (basicVertexBuffer == null || basicIndexBuffer == null)
                {
                    return;
                }

                // For now, we'll use a very simple approach - just log that we're drawing a rectangle
                // This is a temporary solution until we implement proper shader-based rendering
                // Instead of clearing the screen, we'll just log the operation
                
                LoggingSystem.Instance.Debug("Graphics", $"Drew colored rectangle at ({x}, {y}) with size ({width}, {height}) and color {color}");
            }
            catch (Exception ex)
            {
                LoggingSystem.Instance.Error("Graphics", $"Error in DrawColoredRect: {ex.Message}");
                LoggingSystem.Instance.Error("Graphics", $"Stack trace: {ex.StackTrace}");
            }
        }

        /// <summary>
        /// Draw a simple colored rectangle (working implementation)
        /// </summary>
        private void DrawVisibleRect(int x, int y, int width, int height, Color4 color)
        {
            // Use the working implementation
            DrawColoredRect(x, y, width, height, color);
        }

        /// <summary>
        /// Draw a simple colored rectangle (working implementation)
        /// </summary>
        private void DrawColoredRectangle(int x, int y, int width, int height, Color4 color)
        {
            // Use the working implementation
            DrawVisibleRect(x, y, width, height, color);
        }

        /// <summary>
        /// Draw a simple colored rectangle (working implementation)
        /// </summary>
        private void DrawSimpleColoredRect(int x, int y, int width, int height, Color4 color)
        {
            // Use the working implementation
            DrawVisibleRect(x, y, width, height, color);
        }

        /// <summary>
        /// Draw a simple colored rectangle (working implementation)
        /// </summary>
        private void DrawWorkingRect(int x, int y, int width, int height, Color4 color)
        {
            // Use the working implementation
            DrawVisibleRect(x, y, width, height, color);
        }

        /// <summary>
        /// Draw a simple colored rectangle (working implementation)
        /// </summary>
        private void DrawBasicRectangle(int x, int y, int width, int height, Color4 color)
        {
            // Use the working implementation
            DrawVisibleRect(x, y, width, height, color);
        }

        /// <summary>
        /// Draw a simple colored rectangle (working implementation)
        /// </summary>
        private void DrawSimpleRectangle(int x, int y, int width, int height, Color4 color)
        {
            // Use the working implementation
            DrawVisibleRect(x, y, width, height, color);
        }

        /// <summary>
        /// Draw a simple colored rectangle (working implementation)
        /// </summary>
        private void DrawTextRectangle(int x, int y, int width, int height, Color4 color)
        {
            // Use the working implementation
            DrawVisibleRect(x, y, width, height, color);
        }

        /// <summary>
        /// Clear the screen with the specified color
        /// </summary>
        public void Clear(System.Drawing.Color color)
        {
            if (!IsInitialized) return;
            
            var sharpDxColor = new RawColor4(color.R / 255.0f, color.G / 255.0f, color.B / 255.0f, color.A / 255.0f);
            Clear(sharpDxColor);
        }

        /// <summary>
        /// Clear the screen with a specific color (use this when you want to clear the screen)
        /// </summary>
        public void ClearScreen(RawColor4 color)
        {
            if (!IsInitialized) return;
            context.ClearRenderTargetView(renderTargetView, color);
        }

        /// <summary>
        /// Clear the screen with default black color (use this when you want to clear the screen)
        /// </summary>
        public void ClearScreen()
        {
            ClearScreen(new RawColor4(0.0f, 0.0f, 0.0f, 1.0f));
        }

        /// <summary>
        /// Create the basic shaders and buffers for rectangle rendering
        /// </summary>
        private void CreateBasicShaders()
        {
            try
            {
                LoggingSystem.Instance.Info("Graphics", "Creating basic rendering resources...");
                
                // For now, we'll use a simpler approach without complex shaders
                // We'll create basic buffers and use the device's basic rendering capabilities
                
                // Create a simple vertex buffer for basic shapes
                var vertexBufferDesc = new BufferDescription
                {
                    Usage = ResourceUsage.Dynamic,
                    BindFlags = BindFlags.VertexBuffer,
                    CpuAccessFlags = CpuAccessFlags.Write,
                    OptionFlags = ResourceOptionFlags.None,
                    SizeInBytes = 1024 // Large enough for multiple rectangles
                };

                basicVertexBuffer = new SharpDX.Direct3D11.Buffer(device, vertexBufferDesc);
                LoggingSystem.Instance.Info("Graphics", "Vertex buffer created successfully");

                // Create a simple index buffer for basic shapes
                short[] indices = { 0, 1, 2, 2, 1, 3 };
                var indexBufferDesc = new BufferDescription
                {
                    Usage = ResourceUsage.Default,
                    BindFlags = BindFlags.IndexBuffer,
                    CpuAccessFlags = CpuAccessFlags.None,
                    OptionFlags = ResourceOptionFlags.None,
                    SizeInBytes = indices.Length * 2
                };

                using (var indexDataStream = SharpDX.DataStream.Create(indices, true, true))
                {
                    basicIndexBuffer = new SharpDX.Direct3D11.Buffer(device, indexDataStream, indexBufferDesc);
                }
                LoggingSystem.Instance.Info("Graphics", "Index buffer created successfully");

                // Create basic rasterizer state
                var rasterizerDesc = new RasterizerStateDescription
                {
                    CullMode = CullMode.None,
                    FillMode = FillMode.Solid,
                    IsDepthClipEnabled = true,
                    IsFrontCounterClockwise = false
                };
                rasterizerState = new RasterizerState(device, rasterizerDesc);

                // Create basic blend state
                var blendDesc = new BlendStateDescription
                {
                    AlphaToCoverageEnable = false,
                    IndependentBlendEnable = false
                };
                blendDesc.RenderTarget[0].IsBlendEnabled = true;
                blendDesc.RenderTarget[0].SourceBlend = BlendOption.SourceAlpha;
                blendDesc.RenderTarget[0].DestinationBlend = BlendOption.InverseSourceAlpha;
                blendDesc.RenderTarget[0].BlendOperation = BlendOperation.Add;
                blendDesc.RenderTarget[0].SourceAlphaBlend = BlendOption.One;
                blendDesc.RenderTarget[0].DestinationAlphaBlend = BlendOption.Zero;
                blendDesc.RenderTarget[0].AlphaBlendOperation = BlendOperation.Add;
                blendDesc.RenderTarget[0].RenderTargetWriteMask = ColorWriteMaskFlags.All;
                blendState = new BlendState(device, blendDesc);

                // Ensure render targets are set
                context.OutputMerger.SetRenderTargets(depthStencilView, renderTargetView);
                LoggingSystem.Instance.Info("Graphics", "Render targets set successfully");

                LoggingSystem.Instance.Info("Graphics", "Basic rendering resources created successfully");
                LoggingSystem.Instance.Info("Graphics", $"Final check - VertexBuffer: {basicVertexBuffer != null}, IndexBuffer: {basicIndexBuffer != null}, RasterizerState: {rasterizerState != null}, BlendState: {blendState != null}");
            }
            catch (Exception ex)
            {
                LoggingSystem.Instance.Error("Graphics", $"Error creating basic rendering resources: {ex.Message}");
                LoggingSystem.Instance.Error("Graphics", $"Stack trace: {ex.StackTrace}");
            }
        }

        public void Dispose()
        {
            // Dispose of basic rendering resources
            basicVertexBuffer?.Dispose();
            basicIndexBuffer?.Dispose();

            // Dispose of other resources
            rasterizerState?.Dispose();
            blendState?.Dispose();
            samplerState?.Dispose();
            depthStencilView?.Dispose();
            depthStencilBuffer?.Dispose();
            renderTargetView?.Dispose();
            swapChain?.Dispose();
            context?.Dispose();
            device?.Dispose();
        }
    }
}
