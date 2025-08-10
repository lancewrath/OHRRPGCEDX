using System;
using System.Windows.Forms;
using SharpDX;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using SharpDX.Mathematics.Interop;

namespace OHRRPGCEDX.Graphics
{
    /// <summary>
    /// Game window class that handles window management and DirectX integration
    /// This replaces the window.cpp/hpp functionality from the old engine
    /// </summary>
    public class GameWindow : Form, IDisposable
    {
        private GraphicsSystem graphicsSystem;
        private bool isFullscreen = false;
        private FormWindowState previousWindowState;
        private bool isDisposed = false;

        public event EventHandler WindowResized;
        public event EventHandler WindowActivated;
        public event EventHandler WindowDeactivated;

        public GraphicsSystem GraphicsSystem => graphicsSystem;
        public bool IsFullscreen => isFullscreen;
        public bool IsInitialized => graphicsSystem?.IsInitialized ?? false;

        public GameWindow()
        {
            InitializeWindow();
        }

        /// <summary>
        /// Initialize the window properties
        /// </summary>
        private void InitializeWindow()
        {
            // Set window properties
            Text = "OHRRPGCE .NET Port";
            Size = new System.Drawing.Size(800, 600);
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.Sizable;
            MinimumSize = new System.Drawing.Size(320, 240);

            // Set window icon if available
            try
            {
                // TODO: Load window icon from resources
                // Icon = new Icon("icon.ico");
            }
            catch
            {
                // Icon loading failed, continue without it
            }

            // Subscribe to window events
            Resize += OnWindowResize;
            Activated += OnWindowActivated;
            Deactivate += OnWindowDeactivated;
            FormClosing += OnWindowClosing;

            // Enable double buffering to reduce flickering
            SetStyle(ControlStyles.DoubleBuffer, true);
            SetStyle(ControlStyles.AllPaintingInWmPaint, true);
            SetStyle(ControlStyles.UserPaint, true);
        }

        /// <summary>
        /// Initialize the graphics system
        /// </summary>
        public bool InitializeGraphics(int width = 800, int height = 600, bool fullscreen = false)
        {
            try
            {
                // Create graphics system
                graphicsSystem = new GraphicsSystem();
                
                // Initialize with window handle
                if (!graphicsSystem.Initialize(width, height, fullscreen, true))
                {
                    MessageBox.Show("Failed to initialize graphics system", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return false;
                }

                // Set window size
                if (fullscreen)
                {
                    SetFullscreen(true);
                }
                else
                {
                    Size = new System.Drawing.Size(width, height);
                }

                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Graphics initialization failed: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        /// <summary>
        /// Toggle fullscreen mode
        /// </summary>
        public void ToggleFullscreen()
        {
            SetFullscreen(!isFullscreen);
        }

        /// <summary>
        /// Set fullscreen mode
        /// </summary>
        public void SetFullscreen(bool fullscreen)
        {
            if (fullscreen == isFullscreen) return;

            try
            {
                if (fullscreen)
                {
                    // Save current window state
                    previousWindowState = WindowState;
                    
                    // Set fullscreen
                    FormBorderStyle = FormBorderStyle.None;
                    WindowState = FormWindowState.Maximized;
                    TopMost = true;
                    
                    // Update graphics system
                    if (graphicsSystem != null)
                    {
                        graphicsSystem.ToggleFullscreen();
                    }
                }
                else
                {
                    // Restore window state
                    TopMost = false;
                    FormBorderStyle = FormBorderStyle.Sizable;
                    WindowState = previousWindowState;
                    
                    // Update graphics system
                    if (graphicsSystem != null)
                    {
                        graphicsSystem.ToggleFullscreen();
                    }
                }

                isFullscreen = fullscreen;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to change fullscreen mode: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Handle window resize events
        /// </summary>
        private void OnWindowResize(object sender, EventArgs e)
        {
            if (graphicsSystem != null && !isFullscreen)
            {
                graphicsSystem.ResizeGraphics(ClientSize.Width, ClientSize.Height);
            }
            
            WindowResized?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Handle window activation
        /// </summary>
        private void OnWindowActivated(object sender, EventArgs e)
        {
            WindowActivated?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Handle window deactivation
        /// </summary>
        private void OnWindowDeactivated(object sender, EventArgs e)
        {
            WindowDeactivated?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Handle window closing
        /// </summary>
        private void OnWindowClosing(object sender, FormClosingEventArgs e)
        {
            // Clean up resources
            Dispose();
        }

        /// <summary>
        /// Begin rendering a frame
        /// </summary>
        public void BeginRender()
        {
            if (graphicsSystem != null && !IsDisposed)
            {
                graphicsSystem.BeginScene();
            }
        }

        /// <summary>
        /// End rendering a frame
        /// </summary>
        public void EndRender()
        {
            if (graphicsSystem != null && !IsDisposed)
            {
                graphicsSystem.EndScene();
                graphicsSystem.Present();
            }
        }

        /// <summary>
        /// Clear the screen
        /// </summary>
        public void ClearScreen()
        {
            if (graphicsSystem != null && !IsDisposed)
            {
                graphicsSystem.Clear();
            }
        }

        /// <summary>
        /// Dispose of resources
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (!isDisposed)
            {
                if (disposing)
                {
                    graphicsSystem?.Dispose();
                }
                
                isDisposed = true;
                base.Dispose(disposing);
            }
        }

        /// <summary>
        /// Check if the window has been disposed
        /// </summary>
        public new bool IsDisposed => isDisposed;
    }
}
