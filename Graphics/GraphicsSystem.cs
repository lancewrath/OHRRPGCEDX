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
