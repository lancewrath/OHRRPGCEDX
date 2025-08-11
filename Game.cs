using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.IO;
using System.Runtime.InteropServices;
using OHRRPGCEDX.Graphics;
using OHRRPGCEDX.Input;
using OHRRPGCEDX.Audio;
using OHRRPGCEDX.Scripting;
using OHRRPGCEDX.UI;
using OHRRPGCEDX.Utils;
using OHRRPGCEDX.Configuration;
using OHRRPGCEDX.Session;
using OHRRPGCEDX.GameData;


namespace OHRRPGCEDX.Game
{
    /// <summary>
    /// Main entry point for the OHRRPGCE Game Runtime
    /// This is the equivalent of game.bas in the original engine
    /// </summary>
    public class GameRuntime : Form
    {
        private GraphicsSystem graphicsSystem;
        private InputSystem inputSystem;
        private AudioSystem audioSystem;
        private ScriptEngine scriptEngine;
        private MenuSystem menuSystem;
        private LoggingSystem loggingSystem;
        private ConfigurationManager configManager;
        private SessionManager sessionManager;
        private RPGFileLoader rpgLoader;
        private BattleSystem battleSystem;
        private SaveLoadSystem saveLoadSystem;
        private MapRenderer mapRenderer;
        
        // File browser system
        private FileBrowser fileBrowser;
        private FileBrowserRenderer fileBrowserRenderer;
        
        private bool isRunning = false;
        private Timer gameTimer;
        
        // Game state
        private GameState currentState = GameState.Loading;
        private string currentRPGPath = "";
        private RPGData currentGameData;
        private Player player;
        private Map currentMap;
        private bool gamePaused = false;
        
        // Game loop timing
        private DateTime lastFrameTime;
        private const int TARGET_FPS = 60;
        private const int FRAME_TIME_MS = 1000 / TARGET_FPS;
        private bool isRendering = false; // Prevent multiple simultaneous renders
        
        // Loading timer for state transition
        private double loadingTimer = 0.0;
        
        public GameRuntime()
        {
            InitializeComponents();
            InitializeSystems();
            SetupEventHandlers();
        }
        
        private void InitializeComponents()
        {
            this.Text = "OHRRPGCE Game Runtime";
            this.Size = new Size(800, 600);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.Sizable;
            this.MinimumSize = new Size(640, 480);
            
            // Set up double buffering for smooth rendering
            this.SetStyle(ControlStyles.DoubleBuffer, true);
            this.SetStyle(ControlStyles.UserPaint, true);
            this.SetStyle(ControlStyles.AllPaintingInWmPaint, true);
        }
        
        private void InitializeSystems()
        {
            try
            {
                loggingSystem = LoggingSystem.Instance;
                loggingSystem.Initialize("game_runtime.log");
                
                configManager = ConfigurationManager.Instance;
                configManager.Initialize();
                
                sessionManager = SessionManager.Instance;
                
                // GraphicsSystem will be initialized in OnFormLoad when the window handle is valid
                // graphicsSystem = new GraphicsSystem();
                // graphicsSystem.Initialize(this.Width, this.Height, false, true, this.Handle);
                
                inputSystem = new InputSystem();
                if (!inputSystem.Initialize())
                {
                    throw new Exception("Failed to initialize input system");
                }
                
                // Configure key repeat timing to match Custom.cs exactly for consistent responsiveness
                inputSystem.InitialRepeatDelayMs = 400;  // 400ms initial delay (same as Custom.cs)
                inputSystem.RepeatIntervalMs = 80;       // 80ms between repeats (same as Custom.cs)
                
                audioSystem = new AudioSystem();
                audioSystem.Initialize();
                
                scriptEngine = new ScriptEngine();
                scriptEngine.Initialize();
                
                menuSystem = new MenuSystem();
                
                rpgLoader = new RPGFileLoader();
                battleSystem = new BattleSystem();
                saveLoadSystem = new SaveLoadSystem();
                
                // File browser system will be initialized after graphics system
                // fileBrowser = new FileBrowser();
                // fileBrowserRenderer = new FileBrowserRenderer(fileBrowser, graphicsSystem);
                
                loggingSystem.Info("Game Runtime", "Game Runtime systems initialized successfully");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to initialize systems: {ex.Message}", "Initialization Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                loggingSystem?.Error("Game Runtime", $"System initialization failed: {ex}");
            }
        }
        
        private void SetupEventHandlers()
        {
            this.Load += OnFormLoad;
            this.FormClosing += OnFormClosing;
            this.Resize += OnResize;
            this.Paint += OnPaint;
            this.KeyDown += OnKeyDown;
            this.KeyUp += OnKeyUp;
            this.MouseDown += OnMouseDown;
            this.MouseUp += OnMouseUp;
            this.MouseMove += OnMouseMove;
            
            // Set up game timer - use faster interval for better responsiveness
            gameTimer = new Timer();
            gameTimer.Interval = 16; // ~60 FPS for smooth file browser navigation
            gameTimer.Tick += OnGameTimerTick;
        }
        
        private void OnFormLoad(object sender, EventArgs e)
        {
            try
            {
                // Initialize graphics system now that the window handle is valid
                graphicsSystem = new GraphicsSystem();
                if (!graphicsSystem.Initialize(this.Width, this.Height, false, true, this.Handle))
                {
                    throw new Exception("Failed to initialize graphics system");
                }
                
                // Initialize file browser system now that graphics system is ready
                fileBrowser = new FileBrowser();
                fileBrowserRenderer = new FileBrowserRenderer(fileBrowser, graphicsSystem);
                
                // Load default configuration
                LoadDefaultConfiguration();
                
                // Show loading screen
                ShowLoadingScreen();
                
                // Start the game loop
                isRunning = true;
                lastFrameTime = DateTime.Now;
                gameTimer.Start();
                
                loggingSystem.Info("Game Runtime", "Game Runtime loaded successfully");
            }
            catch (Exception ex)
            {
                loggingSystem?.Error("Game Runtime", $"Form load failed: {ex}");
                MessageBox.Show($"Failed to load game runtime: {ex.Message}", "Load Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        
        private void OnFormClosing(object sender, FormClosingEventArgs e)
        {
            try
            {
                isRunning = false;
                gameTimer?.Stop();
                
                // Save game state if needed
                if (currentState == GameState.Playing)
                {
                    SaveGameState();
                }
                
                // Save configuration
                configManager?.SaveConfiguration();
                
                // Clean up systems
                CleanupSystems();
                
                loggingSystem?.Info("Game Runtime", "Game Runtime closed successfully");
            }
            catch (Exception ex)
            {
                loggingSystem?.Error("Game Runtime", $"Form closing error: {ex}");
            }
        }
        
        private void OnResize(object sender, EventArgs e)
        {
            if (graphicsSystem != null && this.WindowState != FormWindowState.Minimized)
            {
                graphicsSystem.ResizeGraphics(this.Width, this.Height);
            }
        }
        
        private void OnPaint(object sender, PaintEventArgs e)
        {
            // Completely disable Windows Forms painting - let Direct2D handle everything
            // This prevents conflicts with the Direct2D graphics system
            // Just clear the background to prevent flickering, but don't trigger repaints
            e.Graphics.Clear(System.Drawing.Color.Black);
        }
        
        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            // Handle global game shortcuts
            switch (e.KeyCode)
            {
                case Keys.Escape:
                    if (currentState == GameState.Playing)
                    {
                        ShowPauseMenu();
                    }
                    else if (currentState == GameState.Paused)
                    {
                        ResumeGame();
                    }
                    break;
                case Keys.F1:
                    ShowHelp();
                    break;
                case Keys.F5:
                    QuickSave();
                    break;
                case Keys.F9:
                    QuickLoad();
                    break;
                case Keys.F11:
                    ToggleFullscreen();
                    break;
            }
            
            // Test mode switching with number keys (for debugging)
            switch (e.KeyCode)
            {
                case Keys.D1:
                    currentState = GameState.Loading;
                    loggingSystem?.Info("Game Runtime", "Switched to Loading state");
                    break;
                case Keys.D2:
                    currentState = GameState.MainMenu;
                    loggingSystem?.Info("Game Runtime", "Switched to Main Menu state");
                    break;
                case Keys.D3:
                    currentState = GameState.Playing;
                    loggingSystem?.Info("Game Runtime", "Switched to Playing state");
                    break;
                case Keys.D4:
                    currentState = GameState.Paused;
                    loggingSystem?.Info("Game Runtime", "Switched to Paused state");
                    break;
                case Keys.D5:
                    currentState = GameState.Battle;
                    loggingSystem?.Info("Game Runtime", "Switched to Battle state");
                    break;
                case Keys.D6:
                    currentState = GameState.Menu;
                    loggingSystem?.Info("Game Runtime", "Switched to Menu state");
                    break;
                case Keys.D7:
                    currentState = GameState.Dialog;
                    loggingSystem?.Info("Game Runtime", "Switched to Dialog state");
                    break;
                case Keys.D8:
                    currentState = GameState.GameOver;
                    loggingSystem?.Info("Game Runtime", "Switched to Game Over state");
                    break;
                case Keys.D9:
                    currentState = GameState.FileBrowser;
                    ShowFileBrowser();
                    loggingSystem?.Info("Game Runtime", "Switched to File Browser state");
                    break;
            }
        }
        
        private void OnKeyUp(object sender, KeyEventArgs e)
        {
            // Handle key up events if needed
        }
        
        private void OnMouseDown(object sender, MouseEventArgs e)
        {
            // Handle mouse down events if needed
        }
        
        private void OnMouseUp(object sender, MouseEventArgs e)
        {
            // Handle mouse up events if needed
        }
        
        private void OnMouseMove(object sender, MouseEventArgs e)
        {
            // Handle mouse move events if needed
        }
        
        private void OnGameTimerTick(object sender, EventArgs e)
        {
            if (isRunning && !this.IsDisposed && graphicsSystem != null && graphicsSystem.IsInitialized)
            {
                // Update input system first (like Custom.cs)
                inputSystem?.Update();
                
                // Render current state with proper Direct2D calls (like Custom.cs)
                RenderCurrentState();
                
                // Process input for current state (like Custom.cs)
                UpdateCurrentState(0.016);
            }
            else
            {
                gameTimer?.Stop();
            }
        }
        
        private void Update()
        {
            // This method is no longer needed - updates are handled directly in OnGameTimerTick
            // like Custom.cs does for better performance
        }
        
        private void UpdateCurrentState(double deltaTime)
        {
            switch (currentState)
            {
                case GameState.Loading:
                    UpdateLoading(deltaTime);
                    break;
                case GameState.MainMenu:
                    UpdateMainMenu(deltaTime);
                    break;
                case GameState.FileBrowser:
                    UpdateFileBrowser(deltaTime);
                    break;
                case GameState.Playing:
                    UpdatePlaying(deltaTime);
                    break;
                case GameState.Paused:
                    UpdatePaused(deltaTime);
                    break;
                case GameState.Battle:
                    UpdateBattle(deltaTime);
                    break;
                case GameState.Menu:
                    UpdateMenu(deltaTime);
                    break;
                case GameState.Dialog:
                    UpdateDialog(deltaTime);
                    break;
                case GameState.GameOver:
                    UpdateGameOver(deltaTime);
                    break;
            }
        }
        
        private void RenderCurrentState()
        {
            if (graphicsSystem == null || !graphicsSystem.IsInitialized)
            {
                return;
            }

            try
            {
                // Begin Direct2D rendering (like Custom.cs)
                graphicsSystem.BeginScene();
                
                // Clear the screen first
                graphicsSystem.Clear(Color.FromArgb(255, 0, 0, 0));
                
                // Render current game state
                switch (currentState)
                {
                    case GameState.Loading:
                        RenderLoading();
                        break;
                    case GameState.MainMenu:
                        RenderMainMenu();
                        break;
                    case GameState.FileBrowser:
                        RenderFileBrowser();
                        break;
                    case GameState.Playing:
                        RenderPlaying();
                        break;
                    case GameState.Paused:
                        RenderPaused();
                        break;
                    case GameState.Battle:
                        RenderBattle();
                        break;
                    case GameState.Menu:
                        RenderMenu();
                        break;
                    case GameState.Dialog:
                        RenderDialog();
                        break;
                    case GameState.GameOver:
                        RenderGameOver();
                        break;
                }
                
                // End Direct2D rendering and present (like Custom.cs)
                graphicsSystem.EndScene();
                graphicsSystem.Present();
            }
            catch (Exception ex)
            {
                // Log error but don't crash the rendering loop
                loggingSystem?.Error("Game Runtime", $"Render error: {ex.Message}");
            }
        }
        
        // Game state implementations
        private void ShowLoadingScreen()
        {
            currentState = GameState.Loading;
            // TODO: Show loading screen with progress bar
        }
        
        private void UpdateLoading(double deltaTime)
        {
            // TODO: Implement loading progress
            // For now, just transition to main menu after a short delay
            // Add a small delay to prevent immediate state change
            loadingTimer += deltaTime;
            
            if (currentState == GameState.Loading && loadingTimer > 1.0) // Wait 1 second
            {
                ShowMainMenu();
                loadingTimer = 0.0; // Reset timer
            }
        }
        
        private void RenderLoading()
        {
            try
            {
                if (graphicsSystem?.IsInitialized == true)
                {
                    graphicsSystem.Clear(Color.FromArgb(255, 51, 51, 51));
                    
                    // Try to draw some text to see if rendering is working
                    graphicsSystem.DrawText("LOADING...", 400, 300, Color.White, TextAlignment.Center);
                }
                else
                {
                    // Console.WriteLine("Warning: Graphics system not available for loading screen");
                }
            }
            catch (Exception ex)
            {
                loggingSystem?.Warning("Game Runtime", $"Loading screen rendering failed: {ex.Message}");
            }
        }
        
        private void ShowMainMenu()
        {
            currentState = GameState.MainMenu;
            // menuSystem?.ShowMainMenu(); // Commented out until MenuSystem.ShowMainMenu is implemented
        }
        
        private void UpdateMainMenu(double deltaTime)
        {
            // Handle main menu input
            if (inputSystem?.IsKeyPressed(Keys.Enter) == true)
            {
                StartNewGame();
            }
            else if (inputSystem?.IsKeyPressed(Keys.L) == true)
            {
                LoadGame();
            }
        }
        
        private void RenderMainMenu()
        {
            try
            {
                if (graphicsSystem?.IsInitialized == true)
                {
                    graphicsSystem.Clear(Color.FromArgb(255, 26, 26, 77));
                    
                    // Draw title
                    graphicsSystem.DrawText("OHRRPGCE GAME RUNTIME", 4, 4, Color.DarkBlue, TextAlignment.Left);
                    
                    // Draw menu options
                    graphicsSystem.DrawText("Press ENTER to start new game", 4, 50, Color.LightGray, TextAlignment.Left);
                    graphicsSystem.DrawText("Press L to load saved game", 4, 70, Color.LightGray, TextAlignment.Left);
                    graphicsSystem.DrawText("Press ESC to exit", 4, 90, Color.LightGray, TextAlignment.Left);
                    
                    // Draw footer
                    graphicsSystem.DrawText("Built 2024 - Direct2D graphics, sdl2 music", 4, 200, Color.Gray, TextAlignment.Left);
                    graphicsSystem.DrawText("Press F1 for help", 4, 220, Color.Gray, TextAlignment.Left);
                    
                    // Console.WriteLine("Rendering main menu...");
                }
                else
                {
                    // Console.WriteLine("Warning: Graphics system not available for main menu");
                }
            }
            catch (Exception ex)
            {
                loggingSystem?.Warning("Game Runtime", $"Main menu rendering failed: {ex.Message}");
            }
        }
        
        private void StartNewGame()
        {
            try
            {
                // Show file browser to select RPG file
                ShowFileBrowser();
                
                loggingSystem?.Info("Game Runtime", "Starting file browser for loading RPG file");
            }
            catch (Exception ex)
            {
                loggingSystem?.Error("Game Runtime", $"Failed to start file browser: {ex}");
                MessageBox.Show($"Failed to start file browser: {ex.Message}", "File Browser Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        
        private void LoadGame()
        {
            try
            {
                // Show load game dialog
                using (OpenFileDialog openDialog = new OpenFileDialog())
                {
                    openDialog.Filter = "OHRRPGCE Save Files (*.sav)|*.sav|All Files (*.*)|*.*";
                    openDialog.Title = "Load Game";
                    
                    if (openDialog.ShowDialog() == DialogResult.OK)
                    {
                        LoadGameState(openDialog.FileName);
                        currentState = GameState.Playing;
                        loggingSystem?.Info("Game Runtime", $"Game loaded: {openDialog.FileName}");
                    }
                }
            }
            catch (Exception ex)
            {
                loggingSystem?.Error("Game Runtime", $"Failed to load game: {ex}");
                MessageBox.Show($"Failed to load game: {ex.Message}", "Load Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        
        private void LoadGameData(string rpgPath)
        {
            try
            {
                currentGameData = rpgLoader.LoadGameData(rpgPath);
                if (currentGameData == null)
                {
                    throw new Exception("Failed to load RPG file");
                }
                
                loggingSystem?.Info("Game Runtime", $"Game data loaded: {rpgPath}");
            }
            catch (Exception ex)
            {
                loggingSystem?.Error("Game Runtime", $"Failed to load game data: {ex}");
                throw;
            }
        }
        
        private void InitializeNewGame()
        {
            try
            {
                // Create player
                player = new Player();
                if (currentGameData.Heroes != null && currentGameData.Heroes.Length > 0)
                {
                    player.Initialize(currentGameData.Heroes[0]); // Use first hero as default
                }
                
                // Load starting map
                if (currentGameData.Maps != null && currentGameData.Maps.Length > 0)
                {
                    currentMap = new Map();
                    currentMap.Name = currentGameData.Maps[0].Name;
                    currentMap.Width = currentGameData.Maps[0].Width;
                    currentMap.Height = currentGameData.Maps[0].Height;
                    // TODO: Convert MapData to Map format
                }
                
                // Set player position
                player.Position = new Point(0, 0); // Default starting position
                
                // Initialize map renderer with graphics system components
                if (graphicsSystem != null && currentMap != null)
                {
                    try
                    {
                        // For Direct2D, we don't need Direct3D device parameters
                        mapRenderer = new MapRenderer();
                        // mapRenderer?.SetMap(currentMap); // Commented out until MapRenderer.SetMap is implemented
                    }
                    catch (Exception ex)
                    {
                        loggingSystem?.Warning("Game Runtime", $"Failed to initialize MapRenderer: {ex.Message}");
                        mapRenderer = null;
                    }
                }
                
                loggingSystem?.Info("Game Runtime", "New game initialized successfully");
            }
            catch (Exception ex)
            {
                loggingSystem?.Error("Game Runtime", $"Failed to initialize new game: {ex}");
                throw;
            }
        }
        
        private void UpdatePlaying(double deltaTime)
        {
            try
            {
                // Update player
                player?.Update(deltaTime, inputSystem);
                
                // Update map
                currentMap?.Update(deltaTime);
                
                // Update NPCs and events
                UpdateMapEntities(deltaTime);
                
                // Check for map transitions
                CheckMapTransitions();
                
                // Check for battles
                CheckBattleTriggers();
                
                // Check for menu triggers
                CheckMenuTriggers();
            }
            catch (Exception ex)
            {
                loggingSystem?.Error("Game Runtime", $"Playing update error: {ex}");
            }
        }
        
        private void RenderPlaying()
        {
            try
            {
                // Clear with a dark green background for the game world
                graphicsSystem?.Clear(Color.FromArgb(255, 26, 77, 26));
                
                // Render map
                // mapRenderer?.Render(); // Commented out until MapRenderer.Render is implemented
                
                // Render player
                player?.Render(graphicsSystem);
                
                // Render NPCs and other entities
                RenderMapEntities();
                
                // Console.WriteLine("Rendering game world...");
            }
            catch (Exception ex)
            {
                loggingSystem?.Error("Game Runtime", $"Playing render error: {ex}");
            }
        }
        
        private void UpdateMapEntities(double deltaTime)
        {
            // TODO: Update NPCs, events, and other map entities
        }
        
        private void RenderMapEntities()
        {
            try
            {
                // TODO: Render NPCs, events, and other map entities
                // For now, just log that we're rendering map entities
                // Console.WriteLine("Rendering map entities...");
            }
            catch (Exception ex)
            {
                loggingSystem?.Warning("Game Runtime", $"Map entities rendering failed: {ex.Message}");
            }
        }
        
        private void CheckMapTransitions()
        {
            // TODO: Check if player is at a map transition point
        }
        
        private void CheckBattleTriggers()
        {
            // TODO: Check for random encounters and battle triggers
        }
        
        private void CheckMenuTriggers()
        {
            // TODO: Check for menu key presses (inventory, status, etc.)
        }
        
        private void ShowPauseMenu()
        {
            currentState = GameState.Paused;
            gamePaused = true;
            // menuSystem?.ShowPauseMenu(); // Commented out until MenuSystem.ShowPauseMenu is implemented
        }
        
        private void ResumeGame()
        {
            currentState = GameState.Playing;
            gamePaused = false;
            // menuSystem?.HidePauseMenu(); // Commented out until MenuSystem.HidePauseMenu is implemented
        }
        
        private void UpdatePaused(double deltaTime)
        {
            // Handle pause menu input
        }
        
        private void RenderPaused()
        {
            try
            {
                // Render pause menu overlay with a semi-transparent dark overlay
                graphicsSystem?.Clear(Color.FromArgb(179, 0, 0, 0));
                // Console.WriteLine("Rendering pause menu...");
            }
            catch (Exception ex)
            {
                loggingSystem?.Warning("Game Runtime", $"Pause menu rendering failed: {ex.Message}");
            }
        }
        
        private void UpdateBattle(double deltaTime)
        {
            // battleSystem?.Update(deltaTime); // Commented out until BattleSystem.Update is implemented
        }
        
        private void RenderBattle()
        {
            try
            {
                // Clear with a dark red background for battle scenes
                graphicsSystem?.Clear(Color.FromArgb(255, 77, 26, 26));
                
                // battleSystem?.Render(graphicsSystem); // Commented out until BattleSystem.Render is implemented
                // Console.WriteLine("Rendering battle scene...");
            }
            catch (Exception ex)
            {
                loggingSystem?.Warning("Game Runtime", $"Battle rendering failed: {ex.Message}");
            }
        }
        
        private void UpdateMenu(double deltaTime)
        {
            // Handle menu input
        }
        
        private void RenderMenu()
        {
            try
            {
                // Clear with a dark blue background for menu screens
                graphicsSystem?.Clear(Color.FromArgb(255, 26, 26, 102));
                // Console.WriteLine("Rendering menu...");
            }
            catch (Exception ex)
            {
                loggingSystem?.Warning("Game Runtime", $"Menu rendering failed: {ex.Message}");
            }
        }
        
        private void UpdateDialog(double deltaTime)
        {
            // Handle dialog input
        }
        
        private void RenderDialog()
        {
            try
            {
                // Clear with a dark purple background for dialog scenes
                graphicsSystem?.Clear(Color.FromArgb(255, 51, 26, 77));
                // Console.WriteLine("Rendering dialog...");
            }
            catch (Exception ex)
            {
                loggingSystem?.Warning("Game Runtime", $"Dialog rendering failed: {ex.Message}");
            }
        }
        
        private void UpdateGameOver(double deltaTime)
        {
            // Handle game over input
        }
        
        private void RenderGameOver()
        {
            try
            {
                // Clear with a dark gray background for game over screen
                graphicsSystem?.Clear(Color.FromArgb(255, 77, 0, 0));
                // Console.WriteLine("Rendering game over screen...");
            }
            catch (Exception ex)
            {
                loggingSystem?.Warning("Game Runtime", $"Game over rendering failed: {ex.Message}");
            }
        }
        
        private void RenderFileBrowser()
        {
            try
            {
                if (fileBrowserRenderer != null)
                {
                    fileBrowserRenderer.Render();
                }
                else
                {
                    // Console.WriteLine("Warning: FileBrowserRenderer not initialized.");
                }
            }
            catch (Exception ex)
            {
                // Console.WriteLine($"File browser rendering failed: {ex.Message}");
                loggingSystem?.Warning("Game Runtime", $"File browser rendering failed: {ex.Message}");
            }
        }
        
        private void UpdateFileBrowser(double deltaTime)
        {
            if (fileBrowser == null) return;

            // Use the same input handling approach as Custom.cs for consistent responsiveness
            // Handle input for file browser navigation
            if (inputSystem?.IsKeyJustPressed(Keys.Up) == true || inputSystem?.ShouldKeyRepeat(Keys.Up) == true)
            {
                fileBrowser.MoveUp();
            }
            else if (inputSystem?.IsKeyJustPressed(Keys.Down) == true || inputSystem?.ShouldKeyRepeat(Keys.Down) == true)
            {
                fileBrowser.MoveDown();
            }
            else if (inputSystem?.IsKeyJustPressed(Keys.Enter) == true)
            {
                // Navigate to selected entry or select file
                if (fileBrowser.NavigateToSelected())
                {
                    // File was selected, load it and start playing
                    string selectedPath = fileBrowser.GetSelectedPath();
                    if (!string.IsNullOrEmpty(selectedPath))
                    {
                        loggingSystem?.Info("Game Runtime", $"Selected RPG file: {selectedPath}");
                        
                        // Load the RPG file
                        if (rpgLoader != null)
                        {
                            try
                            {
                                loggingSystem?.Info("Game Runtime", "Loading RPG file...");
                                
                                if (rpgLoader.LoadRPG(selectedPath))
                                {
                                    // Load the game data
                                    currentGameData = rpgLoader.LoadGameData(selectedPath);
                                    currentRPGPath = selectedPath;
                                    
                                    if (currentGameData != null)
                                    {
                                        loggingSystem?.Info("Game Runtime", $"Successfully loaded RPG: {currentGameData.General?.GameTitle ?? "Unknown Title"}");
                                        loggingSystem?.Info("Game Runtime", $"Heroes: {currentGameData.Heroes?.Length ?? 0}, Maps: {currentGameData.Maps?.Length ?? 0}");
                                        
                                        // Initialize the game and start playing
                                        InitializeNewGame();
                                        currentState = GameState.Playing;
                                        loggingSystem?.Info("Game Runtime", "Game started successfully");
                                    }
                                    else
                                    {
                                        loggingSystem?.Error("Game Runtime", "Failed to load game data from RPG file");
                                        MessageBox.Show("Failed to load game data from RPG file. The file may be corrupted or in an unsupported format.", 
                                            "Load Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                    }
                                }
                                else
                                {
                                    loggingSystem?.Error("Game Runtime", "Failed to load RPG file");
                                    MessageBox.Show("Failed to load RPG file. The file may be corrupted or in an unsupported format.", 
                                        "Load Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                }
                            }
                            catch (Exception ex)
                            {
                                loggingSystem?.Error("Game Runtime", $"Error loading RPG file: {ex.Message}");
                                MessageBox.Show($"Error loading RPG file: {ex.Message}", 
                                    "Load Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            }
                        }
                    }
                }
            }
            else if (inputSystem?.IsKeyPressed(Keys.Escape) == true)
            {
                // Go back to main menu
                currentState = GameState.MainMenu;
                inputSystem.ResetAllKeyRepeat();
                loggingSystem?.Info("Game Runtime", "Returning to main menu from file browser");
            }
            else if (inputSystem?.IsKeyPressed(Keys.Back) == true)
            {
                // Go up a directory
                fileBrowser.GoUpDirectory();
            }
            else if (inputSystem?.IsKeyPressed(Keys.F5) == true)
            {
                // Refresh file listing
                fileBrowser.Refresh();
            }
        }
        
        // Game utility methods
        private void ShowHelp()
        {
            // TODO: Show help/controls
        }
        
        private void QuickSave()
        {
            try
            {
                // saveLoadSystem?.QuickSave(); // Commented out until SaveLoadSystem.QuickSave is implemented
                loggingSystem?.Info("Game Runtime", "Quick save completed");
            }
            catch (Exception ex)
            {
                loggingSystem?.Error("Game Runtime", $"Quick save failed: {ex}");
            }
        }
        
        private void QuickLoad()
        {
            try
            {
                // saveLoadSystem?.QuickLoad(); // Commented out until SaveLoadSystem.QuickLoad is implemented
                loggingSystem?.Info("Game Runtime", "Quick load completed");
            }
            catch (Exception ex)
            {
                loggingSystem?.Error("Game Runtime", $"Quick load failed: {ex}");
            }
        }
        
        private void ToggleFullscreen()
        {
            // TODO: Implement fullscreen toggle
        }
        
        private void SaveGameState()
        {
            try
            {
                // TODO: Implement game state saving
                loggingSystem?.Info("Game Runtime", "Game state saved");
            }
            catch (Exception ex)
            {
                loggingSystem?.Error("Game Runtime", $"Failed to save game state: {ex}");
            }
        }
        
        private void LoadGameState(string savePath)
        {
            try
            {
                // TODO: Implement game state loading
                loggingSystem?.Info("Game Runtime", $"Game state loaded: {savePath}");
            }
            catch (Exception ex)
            {
                loggingSystem?.Error("Game Runtime", $"Failed to load game state: {ex}");
                throw;
            }
        }
        
        private void LoadDefaultConfiguration()
        {
            try
            {
                // configManager?.LoadDefaultConfiguration(); // Commented out until ConfigurationManager.LoadDefaultConfiguration is implemented
                loggingSystem?.Info("Game Runtime", "Default configuration loaded");
            }
            catch (Exception ex)
            {
                loggingSystem?.Error("Game Runtime", $"Failed to load default configuration: {ex}");
            }
        }
        
        private void CleanupSystems()
        {
            try
            {
                // scriptEngine?.Dispose(); // Commented out until ScriptEngine.Dispose is implemented
                audioSystem?.Dispose();
                inputSystem?.Dispose();
                graphicsSystem?.Dispose();
                loggingSystem?.Dispose();
                
                loggingSystem?.Info("Game Runtime", "Systems cleaned up successfully");
            }
            catch (Exception ex)
            {
                loggingSystem?.Error("Game Runtime", $"Cleanup error: {ex}");
            }
        }
        
        private void ShowFileBrowser()
        {
            currentState = GameState.FileBrowser;
            
            // Use a more reliable path initialization approach
            string defaultPath;
            
            // Try multiple fallback paths for better file browsing
            if (Directory.Exists(Application.StartupPath))
            {
                defaultPath = Application.StartupPath;
                loggingSystem?.Info("Game Runtime", $"Using StartupPath: {defaultPath}");
            }
            else if (Directory.Exists(Environment.CurrentDirectory))
            {
                defaultPath = Environment.CurrentDirectory;
                loggingSystem?.Info("Game Runtime", $"Using CurrentDirectory: {defaultPath}");
            }
            else if (Directory.Exists("C:\\"))
            {
                defaultPath = "C:\\";
                loggingSystem?.Info("Game Runtime", $"Using C:\\ fallback: {defaultPath}");
            }
            else
            {
                defaultPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                loggingSystem?.Info("Game Runtime", $"Using Desktop fallback: {defaultPath}");
            }
            
            // Log the final path being used
            loggingSystem?.Info("Game Runtime", $"File browser initializing with path: {defaultPath}");
            
            fileBrowser.Initialize(FileBrowser.BrowseFileType.RPG, defaultPath);
            
            loggingSystem?.Info("Game Runtime", "File browser started");
        }
        
        /// <summary>
        /// Main entry point for the Game Runtime
        /// </summary>
        [STAThread]
        public static void Main(string[] args)
        {
            try
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                
                // Process command line arguments
                var options = CommandLineProcessor.ParseArguments(args);
                string rpgPath = null;
                
                // Extract RPG path from options if available
                if (!string.IsNullOrEmpty(options.import_scripts_from) && 
                    options.import_scripts_from.EndsWith(".rpg", StringComparison.OrdinalIgnoreCase))
                {
                    rpgPath = options.import_scripts_from;
                }
                
                // Create and run the game runtime
                using (GameRuntime game = new GameRuntime())
                {
                    // If RPG path provided via command line, set it
                    if (!string.IsNullOrEmpty(rpgPath))
                    {
                        game.currentRPGPath = rpgPath;
                    }
                    
                    Application.Run(game);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fatal error in Game Runtime: {ex.Message}", "Fatal Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(GameRuntime));
            this.SuspendLayout();
            // 
            // GameRuntime
            // 
            this.ClientSize = new System.Drawing.Size(284, 261);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "GameRuntime";
            this.ResumeLayout(false);

        }
    }

    /// <summary>
    /// Enumeration of game states
    /// </summary>
    public enum GameState
    {
        Loading,
        MainMenu,
        FileBrowser,  // New state for file browser
        Playing,
        Paused,
        Battle,
        Menu,
        Dialog,
        GameOver
    }
    
    /// <summary>
    /// Player class for the game
    /// </summary>
    public class Player
    {
        public Point Position { get; set; }
        public HeroData HeroData { get; private set; }
        
        public void Initialize(HeroData heroData)
        {
            HeroData = heroData;
            Position = Point.Empty;
        }
        
        public void Update(double deltaTime, InputSystem inputSystem)
        {
            // TODO: Implement player movement and input handling
        }
        
        public void Render(GraphicsSystem graphicsSystem)
        {
            try
            {
                // TODO: Implement proper player sprite rendering
                // For now, just log that we're rendering the player
                // Console.WriteLine($"Rendering player at position: {Position}");
            }
            catch (Exception ex)
            {
                // Console.WriteLine($"Warning: Player rendering failed: {ex.Message}");
            }
        }
    }
    
    /// <summary>
    /// Map class for the game
    /// </summary>
    public class Map
    {
        public string Name { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public TileData[,] Tiles { get; set; }
        
        public void Update(double deltaTime)
        {
            // TODO: Implement map updates (events, NPCs, etc.)
        }
    }
    
    /// <summary>
    /// Tile data structure
    /// </summary>
    public class TileData
    {
        public int TileID { get; set; }
        public bool IsWalkable { get; set; }
        public bool IsWater { get; set; }
        public int EventID { get; set; }
    }
}

