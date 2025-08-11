using System;
using System.Collections.Generic;
using System.Drawing;
using SharpDX.Direct2D1;
using SharpDX.DXGI;
using SharpDX.Mathematics.Interop;
// DirectWrite functionality is included in the base SharpDX package
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
    /// Graphics system using SharpDX Direct2D for 2D rendering
    /// </summary>
    public class GraphicsSystem : IDisposable
    {
        private RenderTarget renderTarget;
        private SharpDX.Direct2D1.Factory factory;
        private SharpDX.DirectWrite.Factory dwFactory;
        private IntPtr windowHandle;
        
        // Brushes for rendering
        private SolidColorBrush whiteBrush;
        private SolidColorBrush blackBrush;
        private SolidColorBrush grayBrush;
        private SolidColorBrush blueBrush;
        private SolidColorBrush greenBrush;
        private SolidColorBrush redBrush;
        private SolidColorBrush orangeBrush;
        private SolidColorBrush darkGrayBrush;
        
        // Text formats
        private SharpDX.DirectWrite.TextFormat textFormat;
        private SharpDX.DirectWrite.TextFormat smallTextFormat;
        
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
                this.windowHandle = windowHandle;

                LoggingSystem.Instance.Info("Graphics", "Step 9: Initializing graphics system...");
                LoggingSystem.Instance.Info("Graphics", "GraphicsSystem object created successfully");
                LoggingSystem.Instance.Info("Graphics", $"Initializing with dimensions: {width}x{height}, Handle: {windowHandle}");

                // Create Direct2D factory
                factory = new SharpDX.Direct2D1.Factory(SharpDX.Direct2D1.FactoryType.SingleThreaded);

                // Create DirectWrite factory
                dwFactory = new SharpDX.DirectWrite.Factory(SharpDX.DirectWrite.FactoryType.Shared);

                // Create render target
                var hwndRenderTargetProperties = new HwndRenderTargetProperties
                {
                    Hwnd = windowHandle,
                    PixelSize = new SharpDX.Size2(width, height),
                    PresentOptions = PresentOptions.None
                };

                var renderTargetProperties = new RenderTargetProperties
                {
                    PixelFormat = new PixelFormat(Format.B8G8R8A8_UNorm, SharpDX.Direct2D1.AlphaMode.Premultiplied),
                    Type = RenderTargetType.Default,
                    Usage = RenderTargetUsage.None
                };

                try
                {
                    renderTarget = new WindowRenderTarget(factory, renderTargetProperties, hwndRenderTargetProperties);
                    LoggingSystem.Instance.Info("Graphics", "Direct2D hardware rendering initialized successfully");
                }
                catch
                {
                    // Fallback to software rendering if hardware fails
                    renderTargetProperties.Type = RenderTargetType.Software;
                    renderTarget = new WindowRenderTarget(factory, renderTargetProperties, hwndRenderTargetProperties);
                    LoggingSystem.Instance.Info("Graphics", "Direct2D software rendering initialized successfully");
                }

                // Create brushes
                CreateBrushes();

                // Create text formats
                CreateTextFormats();

                IsInitialized = true;
                LoggingSystem.Instance.Info("Graphics", "Graphics system initialized successfully");
                return true;
            }
            catch (Exception ex)
            {
                LoggingSystem.Instance.Error("Graphics", $"Failed to initialize graphics system: {ex.Message}");
                LoggingSystem.Instance.Error("Graphics", $"Stack trace: {ex.StackTrace}");
                return false;
            }
        }

        /// <summary>
        /// Initialize the graphics system with a GameWindow
        /// </summary>
        public bool Initialize(GameWindow gameWindow)
        {
            if (gameWindow == null) return false;
            return Initialize(gameWindow.Width, gameWindow.Height, gameWindow.IsFullscreen, true, gameWindow.Handle);
        }

        private void CreateBrushes()
        {
            try
            {
                whiteBrush = new SolidColorBrush(renderTarget, ToRawColor4(Color.White));
                blackBrush = new SolidColorBrush(renderTarget, ToRawColor4(Color.Black));
                grayBrush = new SolidColorBrush(renderTarget, ToRawColor4(Color.Gray));
                blueBrush = new SolidColorBrush(renderTarget, ToRawColor4(Color.Blue));
                greenBrush = new SolidColorBrush(renderTarget, ToRawColor4(Color.Green));
                redBrush = new SolidColorBrush(renderTarget, ToRawColor4(Color.Red));
                orangeBrush = new SolidColorBrush(renderTarget, ToRawColor4(Color.Orange));
                darkGrayBrush = new SolidColorBrush(renderTarget, ToRawColor4(Color.DarkGray));
                
                LoggingSystem.Instance.Info("Graphics", "Brushes created successfully");
            }
            catch (Exception ex)
            {
                LoggingSystem.Instance.Error("Graphics", $"Brush creation failed: {ex.Message}");
                throw;
            }
        }

        private void CreateTextFormats()
        {
            try
            {
                textFormat = new SharpDX.DirectWrite.TextFormat(dwFactory, "Arial", 12);
                smallTextFormat = new SharpDX.DirectWrite.TextFormat(dwFactory, "Arial", 10);
                LoggingSystem.Instance.Info("Graphics", "Text formats created successfully");
            }
            catch (Exception ex)
            {
                LoggingSystem.Instance.Error("Graphics", $"Text format creation failed: {ex.Message}");
                throw;
            }
        }

        private RawColor4 ToRawColor4(System.Drawing.Color color)
        {
            return new RawColor4(
                color.R / 255.0f, 
                color.G / 255.0f, 
                color.B / 255.0f, 
                color.A / 255.0f);
        }

        private RawRectangleF ToRawRectangleF(System.Drawing.RectangleF rect)
        {
            return new RawRectangleF(rect.Left, rect.Top, rect.Right, rect.Bottom);
        }

        /// <summary>
        /// Begin rendering a scene
        /// </summary>
        public void BeginScene()
        {
            if (!IsInitialized || renderTarget == null) return;

            try
            {
                renderTarget.BeginDraw();
            }
            catch (Exception ex)
            {
                LoggingSystem.Instance.Error("Graphics", $"BeginScene failed: {ex.Message}");
            }
        }

        /// <summary>
        /// End rendering a scene
        /// </summary>
        public void EndScene()
        {
            if (!IsInitialized || renderTarget == null) return;

            try
            {
                renderTarget.EndDraw();
            }
            catch (Exception ex)
            {
                LoggingSystem.Instance.Error("Graphics", $"EndScene failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Clear the screen with a specific color
        /// </summary>
        public void Clear(RawColor4 color)
        {
            if (!IsInitialized || renderTarget == null) return;

            try
            {
                renderTarget.Clear(color);
            }
            catch (Exception ex)
            {
                LoggingSystem.Instance.Error("Graphics", $"Clear failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Clear the screen with default color
        /// </summary>
        public void Clear()
        {
            Clear(new RawColor4(0.1f, 0.1f, 0.1f, 1.0f));
        }

        /// <summary>
        /// Present the rendered frame
        /// </summary>
        public void Present()
        {
            // Direct2D handles presentation automatically in EndDraw
            // This method is kept for compatibility
        }

        /// <summary>
        /// Resize the graphics system
        /// </summary>
        public void ResizeGraphics(int width, int height)
        {
            if (!IsInitialized) return;

            try
            {
                screenWidth = width;
                screenHeight = height;

                // Dispose the old render target
                renderTarget?.Dispose();
                
                // Create new render target with updated size
                var hwndRenderTargetProperties = new HwndRenderTargetProperties
                {
                    Hwnd = windowHandle,
                    PixelSize = new SharpDX.Size2(width, height),
                    PresentOptions = PresentOptions.None
                };

                var renderTargetProperties = new RenderTargetProperties
                {
                    PixelFormat = new PixelFormat(Format.B8G8R8A8_UNorm, SharpDX.Direct2D1.AlphaMode.Premultiplied),
                    Type = RenderTargetType.Default,
                    Usage = RenderTargetUsage.None
                };

                try
                {
                    renderTarget = new WindowRenderTarget(factory, renderTargetProperties, hwndRenderTargetProperties);
                }
                catch
                {
                    // Fallback to software rendering if hardware fails
                    renderTargetProperties.Type = RenderTargetType.Software;
                    renderTarget = new WindowRenderTarget(factory, renderTargetProperties, hwndRenderTargetProperties);
                }
                
                // Recreate brushes for the new render target
                CreateBrushes();
                
                // Recreate text formats for the new render target
                CreateTextFormats();

                Resize?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                LoggingSystem.Instance.Error("Graphics", $"ResizeGraphics failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Toggle fullscreen mode
        /// </summary>
        public void ToggleFullscreen()
        {
            fullscreen = !fullscreen;
            // Note: Fullscreen toggle would need additional implementation
            // For now, just update the flag
        }

        /// <summary>
        /// Draw text at the specified position with the specified color and alignment
        /// </summary>
        public void DrawText(string text, int x, int y, System.Drawing.Color color, TextAlignment alignment = TextAlignment.Left)
        {
            if (!IsInitialized || renderTarget == null || string.IsNullOrEmpty(text)) return;

            try
            {
                // Create a brush for the text color
                using (var textBrush = new SolidColorBrush(renderTarget, ToRawColor4(color)))
                {
                    // For now, just use left alignment to avoid text measurement issues
                    float textX = x;

                    var textRect = new RawRectangleF(textX, y, textX + 1000, y + 1000);
                    renderTarget.DrawText(text, textFormat, textRect, textBrush);
                }
            }
            catch (Exception ex)
            {
                LoggingSystem.Instance.Error("Graphics", $"Error in DrawText: {ex.Message}");
            }
        }

        /// <summary>
        /// Draw a rectangle
        /// </summary>
        public void DrawRectangle(int x, int y, int width, int height, System.Drawing.Color color, float strokeWidth = 1.0f)
        {
            if (!IsInitialized || renderTarget == null) return;

            try
            {
                using (var brush = new SolidColorBrush(renderTarget, ToRawColor4(color)))
                {
                    var rect = new RawRectangleF(x, y, x + width, y + height);
                    renderTarget.DrawRectangle(rect, brush, strokeWidth);
                }
            }
            catch (Exception ex)
            {
                LoggingSystem.Instance.Error("Graphics", $"Error in DrawRectangle: {ex.Message}");
            }
        }

        /// <summary>
        /// Fill a rectangle
        /// </summary>
        public void FillRectangle(int x, int y, int width, int height, System.Drawing.Color color)
        {
            if (!IsInitialized || renderTarget == null) return;

            try
            {
                using (var brush = new SolidColorBrush(renderTarget, ToRawColor4(color)))
                {
                    var rect = new RawRectangleF(x, y, x + width, y + height);
                    renderTarget.FillRectangle(rect, brush);
                }
            }
            catch (Exception ex)
            {
                LoggingSystem.Instance.Error("Graphics", $"Error in FillRectangle: {ex.Message}");
            }
        }

        /// <summary>
        /// Draw a line
        /// </summary>
        public void DrawLine(int x1, int y1, int x2, int y2, System.Drawing.Color color, float strokeWidth = 1.0f)
        {
            if (!IsInitialized || renderTarget == null) return;

            try
            {
                using (var brush = new SolidColorBrush(renderTarget, ToRawColor4(color)))
                {
                    var start = new RawVector2(x1, y1);
                    var end = new RawVector2(x2, y2);
                    renderTarget.DrawLine(start, end, brush, strokeWidth);
                }
            }
            catch (Exception ex)
            {
                LoggingSystem.Instance.Error("Graphics", $"Error in DrawLine: {ex.Message}");
            }
        }

        /// <summary>
        /// Clear the screen with the specified color
        /// </summary>
        public void Clear(System.Drawing.Color color)
        {
            var sharpDxColor = new RawColor4(color.R / 255.0f, color.G / 255.0f, color.B / 255.0f, color.A / 255.0f);
            Clear(sharpDxColor);
        }

        /// <summary>
        /// Clear the screen with a specific color (use this when you want to clear the screen)
        /// </summary>
        public void ClearScreen(RawColor4 color)
        {
            Clear(color);
        }

        /// <summary>
        /// Clear the screen with default color
        /// </summary>
        public void ClearScreen()
        {
            Clear();
        }

        public void Dispose()
        {
            // Dispose brushes
            whiteBrush?.Dispose();
            blackBrush?.Dispose();
            grayBrush?.Dispose();
            blueBrush?.Dispose();
            greenBrush?.Dispose();
            redBrush?.Dispose();
            orangeBrush?.Dispose();
            darkGrayBrush?.Dispose();
            
            // Dispose text formats
            textFormat?.Dispose();
            smallTextFormat?.Dispose();
            
            // Dispose render target and factories
            renderTarget?.Dispose();
            factory?.Dispose();
            dwFactory?.Dispose();
        }
    }
}
