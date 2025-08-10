using System;
using System.Windows.Forms;
using System.Threading;
using OHRRPGCEDX.Graphics;

namespace OHRRPGCEDX
{
    /// <summary>
    /// Main game loop system for OHRRPGCEDX
    /// This provides the core game loop functionality
    /// </summary>
    public class GameLoop : IDisposable
    {
        private GameWindow gameWindow;
        private bool isRunning = false;
        private bool isPaused = false;
        private Thread gameThread;
        private readonly object lockObject = new object();
        
        // Timing
        private DateTime lastFrameTime;
        private double targetFrameRate = 60.0;
        private double frameTime = 1.0 / 60.0;
        
        // Events
        public event EventHandler GameInitialized;
        public event EventHandler GameStarted;
        public event EventHandler GamePaused;
        public event EventHandler GameResumed;
        public event EventHandler GameStopped;
        public event EventHandler<FrameEventArgs> FrameUpdate;
        public event EventHandler<FrameEventArgs> FrameRender;

        public bool IsRunning => isRunning;
        public bool IsPaused => isPaused;
        public double FrameRate => 1.0 / frameTime;
        public double TargetFrameRate => targetFrameRate;

        public GameLoop()
        {
            lastFrameTime = DateTime.Now;
        }

        /// <summary>
        /// Initialize the game loop with a window
        /// </summary>
        public bool Initialize(GameWindow window)
        {
            try
            {
                gameWindow = window;
                
                // Subscribe to window events
                gameWindow.WindowActivated += OnWindowActivated;
                gameWindow.WindowDeactivated += OnWindowDeactivated;
                gameWindow.WindowResized += OnWindowResized;
                
                // Initialize graphics if not already done
                if (!gameWindow.IsInitialized)
                {
                    if (!gameWindow.InitializeGraphics(800, 600, false))
                    {
                        return false;
                    }
                }

                GameInitialized?.Invoke(this, EventArgs.Empty);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to initialize game loop: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Start the game loop
        /// </summary>
        public void Start()
        {
            lock (lockObject)
            {
                if (isRunning) return;
                
                isRunning = true;
                isPaused = false;
                
                // Start game thread
                gameThread = new Thread(RunGameLoop);
                gameThread.IsBackground = true;
                gameThread.Start();
                
                GameStarted?.Invoke(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// Stop the game loop
        /// </summary>
        public void Stop()
        {
            lock (lockObject)
            {
                if (!isRunning) return;
                
                isRunning = false;
                
                // Wait for game thread to finish
                if (gameThread != null && gameThread.IsAlive)
                {
                    gameThread.Join(1000); // Wait up to 1 second
                }
                
                GameStopped?.Invoke(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// Pause the game loop
        /// </summary>
        public void Pause()
        {
            lock (lockObject)
            {
                if (!isRunning || isPaused) return;
                
                isPaused = true;
                GamePaused?.Invoke(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// Resume the game loop
        /// </summary>
        public void Resume()
        {
            lock (lockObject)
            {
                if (!isRunning || !isPaused) return;
                
                isPaused = false;
                lastFrameTime = DateTime.Now; // Reset timing
                GameResumed?.Invoke(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// Set target frame rate
        /// </summary>
        public void SetTargetFrameRate(double frameRate)
        {
            if (frameRate > 0)
            {
                targetFrameRate = frameRate;
                frameTime = 1.0 / frameRate;
            }
        }

        /// <summary>
        /// Main game loop
        /// </summary>
        private void RunGameLoop()
        {
            lastFrameTime = DateTime.Now;
            
            while (isRunning)
            {
                try
                {
                    if (!isPaused)
                    {
                        // Calculate delta time
                        var currentTime = DateTime.Now;
                        var deltaTime = (currentTime - lastFrameTime).TotalSeconds;
                        lastFrameTime = currentTime;
                        
                        // Cap delta time to prevent spiral of death
                        if (deltaTime > 0.1) deltaTime = 0.1;
                        
                        // Update frame time
                        frameTime = deltaTime;
                        
                        // Invoke frame update event
                        var frameArgs = new FrameEventArgs(deltaTime);
                        FrameUpdate?.Invoke(this, frameArgs);
                        
                        // Render frame
                        if (gameWindow != null && !gameWindow.IsDisposed)
                        {
                            gameWindow.BeginRender();
                            
                            // Invoke frame render event
                            FrameRender?.Invoke(this, frameArgs);
                            
                            gameWindow.EndRender();
                        }
                        
                        // Frame rate limiting
                        var targetFrameTime = 1.0 / targetFrameRate;
                        if (deltaTime < targetFrameTime)
                        {
                            var sleepTime = (int)((targetFrameTime - deltaTime) * 1000);
                            if (sleepTime > 0)
                            {
                                Thread.Sleep(sleepTime);
                            }
                        }
                    }
                    else
                    {
                        // When paused, just sleep a bit
                        Thread.Sleep(16); // ~60 FPS sleep
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error in game loop: {ex.Message}");
                    // Continue running unless it's a fatal error
                }
            }
        }

        /// <summary>
        /// Handle window activation
        /// </summary>
        private void OnWindowActivated(object sender, EventArgs e)
        {
            // Resume if paused
            if (isPaused)
            {
                Resume();
            }
        }

        /// <summary>
        /// Handle window deactivation
        /// </summary>
        private void OnWindowDeactivated(object sender, EventArgs e)
        {
            // Pause when window loses focus
            if (!isPaused)
            {
                Pause();
            }
        }

        /// <summary>
        /// Handle window resize
        /// </summary>
        private void OnWindowResized(object sender, EventArgs e)
        {
            // Handle resize if needed
        }

        /// <summary>
        /// Get the game window
        /// </summary>
        public GameWindow GetGameWindow() => gameWindow;

        /// <summary>
        /// Dispose of resources
        /// </summary>
        public void Dispose()
        {
            Stop();
            
            if (gameWindow != null)
            {
                gameWindow.WindowActivated -= OnWindowActivated;
                gameWindow.WindowDeactivated -= OnWindowDeactivated;
                gameWindow.WindowResized -= OnWindowResized;
                gameWindow = null;
            }
        }
    }

    /// <summary>
    /// Frame event arguments
    /// </summary>
    public class FrameEventArgs : EventArgs
    {
        public double DeltaTime { get; }
        
        public FrameEventArgs(double deltaTime)
        {
            DeltaTime = deltaTime;
        }
    }
}
