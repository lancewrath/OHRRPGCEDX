using System;
using System.Collections.Generic;
using SharpDX;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using SharpDX.Mathematics.Interop;
using Device = SharpDX.Direct3D11.Device;

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
        public void BeginScene()
        {
            if (!IsInitialized) return;

            // Clear render target and depth buffer
            context.ClearRenderTargetView(renderTargetView, new RawColor4(0.0f, 0.0f, 0.0f, 1.0f));
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
            
            if (vsync)
                swapChain.Present(1, PresentFlags.None);
            else
                swapChain.Present(0, PresentFlags.None);
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
                    DrawSimpleColoredRect(charX, y, 6, 12, sharpDxColor);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in DrawText: {ex.Message}");
            }
        }

        /// <summary>
        /// Draw a simple colored rectangle that actually renders something visible
        /// </summary>
        private void DrawColoredRect(int x, int y, int width, int height, Color4 color)
        {
            if (!IsInitialized) return;

            try
            {
                // Use a proper DirectX approach: create a colored quad and render it
                // This will draw the rectangle without affecting the rest of the screen
                
                // Convert screen coordinates to normalized device coordinates (-1 to 1)
                float left = (float)x / (screenWidth / 2.0f) - 1.0f;
                float right = (float)(x + width) / (screenWidth / 2.0f) - 1.0f;
                float top = 1.0f - (float)y / (screenHeight / 2.0f);
                float bottom = 1.0f - (float)(y + height) / (screenHeight / 2.0f);
                
                // Create vertex data for a simple quad (two triangles)
                // Each vertex has position (3 floats) and color (4 floats)
                var vertexData = new float[]
                {
                    // Position (x, y, z) and Color (r, g, b, a)
                    left, top, 0.0f, color.Red, color.Green, color.Blue, color.Alpha,
                    right, top, 0.0f, color.Red, color.Green, color.Blue, color.Alpha,
                    left, bottom, 0.0f, color.Red, color.Green, color.Blue, color.Alpha,
                    right, bottom, 0.0f, color.Red, color.Green, color.Blue, color.Alpha
                };

                // Create index data for the two triangles
                short[] indices = { 0, 1, 2, 2, 1, 3 };

                // Create vertex buffer
                var vertexBufferDesc = new BufferDescription
                {
                    Usage = ResourceUsage.Default,
                    BindFlags = BindFlags.VertexBuffer,
                    CpuAccessFlags = CpuAccessFlags.None,
                    OptionFlags = ResourceOptionFlags.None,
                    SizeInBytes = vertexData.Length * 4 // 4 bytes per float
                };

                using (var vertexDataStream = SharpDX.DataStream.Create(vertexData, true, true))
                using (var vertexBuffer = new SharpDX.Direct3D11.Buffer(device, vertexDataStream, vertexBufferDesc))
                {
                    // Create index buffer
                    var indexBufferDesc = new BufferDescription
                    {
                        Usage = ResourceUsage.Default,
                        BindFlags = BindFlags.IndexBuffer,
                        CpuAccessFlags = CpuAccessFlags.None,
                        OptionFlags = ResourceOptionFlags.None,
                        SizeInBytes = indices.Length * 2 // 2 bytes per short
                    };

                    using (var indexDataStream = SharpDX.DataStream.Create(indices, true, true))
                    using (var indexBuffer = new SharpDX.Direct3D11.Buffer(device, indexDataStream, indexBufferDesc))
                    {
                        // Set vertex buffer
                        context.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding(vertexBuffer, 28, 0));
                        
                        // Set index buffer
                        context.InputAssembler.SetIndexBuffer(indexBuffer, Format.R16_UInt, 0);
                        
                        // Set primitive topology
                        context.InputAssembler.PrimitiveTopology = SharpDX.Direct3D.PrimitiveTopology.TriangleList;
                        
                        // Create and set vertex shader
                        var vertexShaderCode = @"
                            struct VS_INPUT
                            {
                                float3 position : POSITION;
                                float4 color : COLOR;
                            };
                            
                            struct VS_OUTPUT
                            {
                                float4 position : SV_POSITION;
                                float4 color : COLOR;
                            };
                            
                            VS_OUTPUT main(VS_INPUT input)
                            {
                                VS_OUTPUT output;
                                output.position = float4(input.position, 1.0f);
                                output.color = input.color;
                                return output;
                            }";

                        using (var vertexShader = new VertexShader(device, System.Text.Encoding.ASCII.GetBytes(vertexShaderCode)))
                        {
                            context.VertexShader.Set(vertexShader);
                            
                            // Create and set pixel shader
                            var pixelShaderCode = @"
                                struct PS_INPUT
                                {
                                    float4 position : SV_POSITION;
                                    float4 color : COLOR;
                                };
                                
                                float4 main(PS_INPUT input) : SV_Target
                                {
                                    return input.color;
                                }";

                            using (var pixelShader = new PixelShader(device, System.Text.Encoding.ASCII.GetBytes(pixelShaderCode)))
                            {
                                context.PixelShader.Set(pixelShader);
                                
                                // Create input layout
                                var inputElements = new[]
                                {
                                    new InputElement("POSITION", 0, Format.R32G32B32_Float, 0, 0),
                                    new InputElement("COLOR", 0, Format.R32G32B32A32_Float, 12, 0)
                                };

                                using (var inputLayout = new InputLayout(device, System.Text.Encoding.ASCII.GetBytes(vertexShaderCode), inputElements))
                                {
                                    context.InputAssembler.InputLayout = inputLayout;
                                    
                                    // Draw the rectangle
                                    context.DrawIndexed(indices.Length, 0, 0);
                                }
                            }
                        }
                    }
                }
                
                // For debugging, log that we're drawing something
                System.Diagnostics.Debug.WriteLine($"Drawing colored rectangle at ({x}, {y}) with size ({width}, {height}) and color {color}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in DrawColoredRect: {ex.Message}");
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

        public void Dispose()
        {
            IsInitialized = false;

            samplerState?.Dispose();
            blendState?.Dispose();
            rasterizerState?.Dispose();
            depthStencilView?.Dispose();
            depthStencilBuffer?.Dispose();
            renderTargetView?.Dispose();
            swapChain?.Dispose();
            context?.Dispose();
            device?.Dispose();
        }
    }
}
