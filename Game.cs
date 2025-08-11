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
        
        // Game data for rendering
        private TilesetData currentTileset;
        private TextureData[] currentTextures;
        
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
                
                // Log loaded game information
                loggingSystem?.Info("Game Runtime", $"Game data loaded: {rpgPath}");
                if (currentGameData.General != null)
                {
                    loggingSystem?.Info("Game Runtime", $"Title: {currentGameData.General.GameTitle}");
                    loggingSystem?.Info("Game Runtime", $"Author: {currentGameData.General.Author}");
                    loggingSystem?.Info("Game Runtime", $"Heroes: {currentGameData.Heroes?.Length ?? 0}");
                    loggingSystem?.Info("Game Runtime", $"Maps: {currentGameData.Maps?.Length ?? 0}");
                    loggingSystem?.Info("Game Runtime", $"Items: {currentGameData.Items?.Length ?? 0}");
                }
                
                // Load tileset data for map rendering
                LoadTilesetData();
                
                // Load texture data for sprites
                LoadTextureData();
                
                loggingSystem?.Info("Game Runtime", "Game data loading completed successfully");
            }
            catch (Exception ex)
            {
                loggingSystem?.Error("Game Runtime", $"Failed to load game data: {ex}");
                throw;
            }
        }
        
        private void LoadTilesetData()
        {
            if (currentGameData?.Maps != null && currentGameData.Maps.Length > 0)
            {
                var firstMap = currentGameData.Maps[0];
                if (firstMap.TilesetId >= 0)
                {
                    currentTileset = rpgLoader.LoadTilesetData(firstMap.TilesetId);
                    if (currentTileset != null)
                    {
                        Console.WriteLine($"Loaded tileset {firstMap.TilesetId} with {currentTileset.TileCount} tiles");
                        
                        // Load tileset texture using Direct2D texture manager
                        // TODO: Re-enable when Direct2DTextureManager is working
                        /*
                        if (graphicsSystem?.TextureManager != null && currentTileset.TileGraphics != null)
                        {
                            try
                            {
                                var tilesetBitmap = graphicsSystem.TextureManager.LoadTilesetFromTiles(
                                    $"tileset_{firstMap.TilesetId}",
                                    currentTileset.TileGraphics,
                                    currentTileset.TileSize,
                                    currentTileset.TileCount,
                                    currentTileset.Palette
                                );
                                
                                if (tilesetBitmap != null)
                                {
                                    mapRenderer?.SetTilesetBitmap(tilesetBitmap);
                                    Console.WriteLine($"Tileset texture loaded successfully");
                                }
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"Failed to load tileset texture: {ex.Message}");
                            }
                        }
                        */
                    }
                    else
                    {
                        Console.WriteLine($"Failed to load tileset {firstMap.TilesetId}");
                    }
                }
            }
        }
        
        private void LoadTextureData()
        {
            try
            {
                if (currentGameData?.Textures != null)
                {
                    loggingSystem?.Info("Game Runtime", $"Loaded {currentGameData.Textures.Length} textures");
                    // Store texture data for sprite rendering
                    currentTextures = currentGameData.Textures;
                }
            }
            catch (Exception ex)
            {
                loggingSystem?.Warning("Game Runtime", $"Failed to load texture data: {ex.Message}");
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
                    var mapData = currentGameData.Maps[0];
                    currentMap = new Map();
                    currentMap.Name = mapData.Name;
                    currentMap.Width = mapData.Width;
                    currentMap.Height = mapData.Height;
                    currentMap.Tiles = mapData.Tiles;
                    currentMap.Passability = mapData.Passability;
                    currentMap.NPCs = mapData.NPCs;
                    currentMap.Doors = mapData.Doors;
                    currentMap.TilesetId = mapData.TilesetId;
                    currentMap.BackgroundMusic = mapData.BackgroundMusic;
                    
                    // Convert layer data if available
                    if (mapData.LayerData != null && mapData.LayerData.Length > 0)
                    {
                        currentMap.Layers = mapData.LayerData.Length;
                        currentMap.LayerData = mapData.LayerData;
                    }
                    else
                    {
                        // Fallback to single layer with tiles array
                        currentMap.Layers = 1;
                        currentMap.LayerData = new int[1][,];
                        currentMap.LayerData[0] = new int[mapData.Width, mapData.Height];
                        
                        // Convert 1D tiles array to 2D if needed
                        if (mapData.Tiles != null && mapData.Tiles.Length > 0)
                        {
                            for (int x = 0; x < mapData.Width; x++)
                            {
                                for (int y = 0; y < mapData.Height; y++)
                                {
                                    int index = y * mapData.Width + x;
                                    if (index < mapData.Tiles.Length)
                                    {
                                        currentMap.LayerData[0][x, y] = mapData.Tiles[index];
                                    }
                                }
                            }
                        }
                    }
                    
                    loggingSystem?.Info("Game Runtime", $"Map initialized: {currentMap.Name} ({currentMap.Width}x{currentMap.Height})");
                }
                
                // Set player position (default to center of map)
                if (currentMap != null)
                {
                    player.Position = new Point(currentMap.Width / 2, currentMap.Height / 2);
                }
                else
                {
                    player.Position = new Point(0, 0); // Default starting position
                }
                
                // Initialize map renderer with graphics system components
                if (graphicsSystem != null && currentMap != null)
                {
                    try
                    {
                        // For Direct2D, we don't need Direct3D device parameters
                        mapRenderer = new MapRenderer();
                        mapRenderer.SetMap(currentMap, currentTileset);
                        loggingSystem?.Info("Game Runtime", "MapRenderer initialized successfully");
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
                if (mapRenderer != null && currentMap != null)
                {
                    mapRenderer.Render(graphicsSystem);
                }
                else
                {
                    // Fallback: render a simple grid pattern
                    RenderFallbackMap();
                }
                
                // Render player
                if (player != null)
                {
                    player.Render(graphicsSystem, currentTextures);
                }
                
                // Render NPCs and other entities
                RenderMapEntities();
                
                // Render UI overlay
                RenderGameUI();
                
                // Console.WriteLine("Rendering game world...");
            }
            catch (Exception ex)
            {
                loggingSystem?.Error("Game Runtime", $"Playing render error: {ex}");
            }
        }
        
        private void RenderFallbackMap()
        {
            try
            {
                if (graphicsSystem?.IsInitialized == true && currentMap != null)
                {
                    // Draw a simple grid pattern as fallback
                    int tileSize = 32; // Default tile size
                    Color gridColor = Color.FromArgb(255, 100, 150, 100);
                    
                    // Draw vertical lines
                    for (int x = 0; x <= currentMap.Width; x++)
                    {
                        int screenX = x * tileSize;
                        graphicsSystem.DrawLine(screenX, 0, screenX, currentMap.Height * tileSize, gridColor, 1);
                    }
                    
                    // Draw horizontal lines
                    for (int y = 0; y <= currentMap.Height; y++)
                    {
                        int screenY = y * tileSize;
                        graphicsSystem.DrawLine(0, screenY, currentMap.Width * tileSize, screenY, gridColor, 1);
                    }
                    
                    // Draw map info text
                    string mapInfo = $"Map: {currentMap.Name} ({currentMap.Width}x{currentMap.Height})";
                    graphicsSystem.DrawText(mapInfo, 10, 10, Color.White, TextAlignment.Left);
                }
            }
            catch (Exception ex)
            {
                loggingSystem?.Warning("Game Runtime", $"Fallback map rendering failed: {ex.Message}");
            }
        }
        
        private void RenderGameUI()
        {
            try
            {
                if (graphicsSystem?.IsInitialized == true)
                {
                    // Draw player position info
                    if (player != null)
                    {
                        string posInfo = $"Player: ({player.Position.X}, {player.Position.Y})";
                        graphicsSystem.DrawText(posInfo, 10, 30, Color.White, TextAlignment.Left);
                    }
                    
                    // Draw game controls help
                    string controls = "Arrow Keys: Move | ESC: Menu | F1: Help";
                    graphicsSystem.DrawText(controls, 10, graphicsSystem.ScreenHeight - 30, Color.Gray, TextAlignment.Left);
                }
            }
            catch (Exception ex)
            {
                loggingSystem?.Warning("Game Runtime", $"Game UI rendering failed: {ex.Message}");
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
                if (currentMap?.NPCs != null && graphicsSystem?.IsInitialized == true)
                {
                    // Render NPCs
                    foreach (var npc in currentMap.NPCs)
                    {
                        if (npc.Active)
                        {
                            RenderNPC(graphicsSystem, npc);
                        }
                    }
                }
                
                if (currentMap?.Doors != null && graphicsSystem?.IsInitialized == true)
                {
                    // Render doors
                    foreach (var door in currentMap.Doors)
                    {
                        if (door.Active)
                        {
                            RenderDoor(door);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                loggingSystem?.Warning("Game Runtime", $"Map entities rendering failed: {ex.Message}");
            }
        }
        
        private void RenderNPC(GraphicsSystem graphicsSystem, NPCData npc)
        {
            int tileSize = 32; // Default tile size
            var screenX = npc.X * tileSize;
            var screenY = npc.Y * tileSize;
            
            // Get NPC appearance data
            var npcId = npc.Picture; // Use Picture as ID
            var npcName = $"NPC{npcId}"; // Generate name from picture ID
            var npcType = GetNPCTypeFromMovement(npc.MovementType); // Derive type from movement
            
            // Create a unique visual representation based on NPC data
            var baseColor = GetNPCColor(npcId, npcType);
            var accentColor = GetNPCAccentColor(npcId, npcType);
            
            // Draw NPC body (main rectangle)
            graphicsSystem.FillRectangle(screenX + 4, screenY + 8, 24, 16, baseColor);
            
            // Draw NPC head
            graphicsSystem.FillRectangle(screenX + 8, screenY + 2, 16, 16, accentColor);
            
            // Draw NPC type indicator
            var typeColor = GetNPCTypeColor(npcType);
            graphicsSystem.FillRectangle(screenX + 2, screenY + 2, 4, 4, typeColor);
            
            // Draw NPC details based on type
            switch (npcType.ToLower())
            {
                case "merchant":
                case "shop":
                    // Draw shop symbol (coin)
                    graphicsSystem.FillRectangle(screenX + 26, screenY + 6, 4, 4, Color.Yellow);
                    graphicsSystem.DrawText("$", screenX + 26, screenY + 6, Color.Black);
                    break;
                    
                case "enemy":
                case "monster":
                    // Draw monster symbol (claws)
                    graphicsSystem.FillRectangle(screenX + 26, screenY + 4, 4, 8, Color.Red);
                    break;
                    
                case "quest":
                case "giver":
                    // Draw quest symbol (exclamation mark)
                    graphicsSystem.FillRectangle(screenX + 26, screenY + 6, 4, 4, Color.Orange);
                    graphicsSystem.DrawText("!", screenX + 26, screenY + 6, Color.Black);
                    break;
                    
                case "healer":
                case "doctor":
                    // Draw healer symbol (cross)
                    graphicsSystem.FillRectangle(screenX + 26, screenY + 6, 4, 4, Color.Green);
                    graphicsSystem.DrawText("+", screenX + 26, screenY + 6, Color.White);
                    break;
                    
                default:
                    // Draw generic NPC symbol (dot)
                    graphicsSystem.FillRectangle(screenX + 26, screenY + 6, 4, 4, Color.Gray);
                    break;
            }
            
            // Draw NPC name
            graphicsSystem.DrawText(npcName, screenX, screenY - 15, Color.White, TextAlignment.Center);
            
            // Draw NPC interaction indicator if they have a script
            if (!string.IsNullOrEmpty(npc.Script))
            {
                var dialogueColor = Color.FromArgb(200, 255, 255, 0);
                graphicsSystem.FillRectangle(screenX + 12, screenY + 28, 8, 4, dialogueColor);
            }
        }

        private string GetNPCTypeFromMovement(int movementType)
        {
            switch (movementType)
            {
                case 0: return "stationary";
                case 1: return "random";
                case 2: return "follow";
                case 3: return "patrol";
                case 4: return "stationary";
                default: return "unknown";
            }
        }

        private void RenderNPCs(GraphicsSystem graphicsSystem)
        {
            if (currentMap?.NPCs == null) return;

            foreach (var npc in currentMap.NPCs)
            {
                if (npc != null)
                {
                    RenderNPC(graphicsSystem, npc);
                }
            }
        }

        private Color GetNPCColor(int npcId, string npcType)
        {
            // Generate consistent colors for each NPC type
            switch (npcType?.ToLower())
            {
                case "merchant":
                case "shop":
                    return Color.FromArgb(255, 255, 215, 0); // Gold
                case "enemy":
                case "monster":
                    return Color.FromArgb(255, 139, 0, 0); // Dark red
                case "quest":
                case "giver":
                    return Color.FromArgb(255, 255, 140, 0); // Dark orange
                case "healer":
                case "doctor":
                    return Color.FromArgb(255, 34, 139, 34); // Forest green
                case "guard":
                case "soldier":
                    return Color.FromArgb(255, 105, 105, 105); // Dim gray
                default:
                    // Generate color based on NPC ID for variety
                    var colors = new[]
                    {
                        Color.FromArgb(255, 150, 150, 150), // Light gray
                        Color.FromArgb(255, 180, 150, 120), // Tan
                        Color.FromArgb(255, 120, 180, 150), // Light green
                        Color.FromArgb(255, 150, 120, 180), // Light purple
                        Color.FromArgb(255, 180, 120, 150), // Light pink
                    };
                    return colors[npcId % colors.Length];
            }
        }

        private Color GetNPCAccentColor(int npcId, string npcType)
        {
            var baseColor = GetNPCColor(npcId, npcType);
            return Color.FromArgb(255,
                Math.Min(255, baseColor.R + 30),
                Math.Min(255, baseColor.G + 30),
                Math.Min(255, baseColor.B + 30));
        }

        private Color GetNPCTypeColor(string npcType)
        {
            switch (npcType?.ToLower())
            {
                case "merchant":
                case "shop":
                    return Color.Yellow;
                case "enemy":
                case "monster":
                    return Color.Red;
                case "quest":
                case "giver":
                    return Color.Orange;
                case "healer":
                case "doctor":
                    return Color.Green;
                case "guard":
                case "soldier":
                    return Color.Blue;
                default:
                    return Color.Gray;
            }
        }
        
        private void RenderDoor(DoorData door)
        {
            try
            {
                if (graphicsSystem?.IsInitialized == true)
                {
                    int tileSize = 32; // Default tile size
                    int screenX = door.X * tileSize;
                    int screenY = door.Y * tileSize;
                    
                    // Draw door (placeholder rectangle for now)
                    Color doorColor = Color.FromArgb(255, 139, 69, 19); // Brown for doors
                    graphicsSystem.FillRectangle(screenX, screenY, tileSize, tileSize, doorColor);
                    
                    // Draw door border
                    graphicsSystem.DrawRectangle(screenX, screenY, tileSize, tileSize, Color.White, 2);
                    
                    // Draw door info
                    string doorInfo = $"Door to Map {door.DestinationMap}";
                    graphicsSystem.DrawText(doorInfo, screenX, screenY - 15, Color.White, TextAlignment.Center);
                }
            }
            catch (Exception ex)
            {
                loggingSystem?.Warning("Game Runtime", $"Door rendering failed: {ex.Message}");
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
        public Direction Direction { get; set; }
        public bool IsMoving { get; private set; }
        public Point TargetPosition { get; private set; }
        public float MoveSpeed { get; set; } = 4.0f; // Tiles per second
        
        public void Initialize(HeroData heroData)
        {
            HeroData = heroData;
            Position = Point.Empty;
            Direction = Direction.South;
            IsMoving = false;
            TargetPosition = Position;
        }
        
        public void Update(double deltaTime, InputSystem inputSystem)
        {
            // Handle movement input
            if (!IsMoving)
            {
                Point newPosition = Position;
                bool moved = false;
                
                if (inputSystem?.IsKeyPressed(Keys.Up) == true)
                {
                    newPosition.Y--;
                    Direction = Direction.North;
                    moved = true;
                }
                else if (inputSystem?.IsKeyPressed(Keys.Down) == true)
                {
                    newPosition.Y++;
                    Direction = Direction.South;
                    moved = true;
                }
                else if (inputSystem?.IsKeyPressed(Keys.Left) == true)
                {
                    newPosition.X--;
                    Direction = Direction.West;
                    moved = true;
                }
                else if (inputSystem?.IsKeyPressed(Keys.Right) == true)
                {
                    newPosition.X++;
                    Direction = Direction.East;
                    moved = true;
                }
                
                if (moved)
                {
                    // Check if new position is valid (within map bounds and passable)
                    if (IsValidPosition(newPosition))
                    {
                        TargetPosition = newPosition;
                        IsMoving = true;
                    }
                }
            }
            
            // Update movement animation
            if (IsMoving)
            {
                // Simple movement: instantly move to target
                Position = TargetPosition;
                IsMoving = false;
            }
        }
        
        private bool IsValidPosition(Point position)
        {
            // TODO: Implement proper collision detection with map passability
            // For now, just check map bounds
            return position.X >= 0 && position.Y >= 0;
        }
        
        public void Render(GraphicsSystem graphicsSystem, TextureData[] textures)
        {
            if (HeroData != null)
            {
                // Render character using actual hero data
                RenderHeroSprite(graphicsSystem, textures);
            }
            else
            {
                // Fallback to basic player representation
                RenderBasicPlayer(graphicsSystem);
            }
        }

        private void RenderHeroSprite(GraphicsSystem graphicsSystem, TextureData[] textures)
        {
            var centerX = Position.X - 16; // Center the sprite
            var centerY = Position.Y - 16;
            
            // Get hero appearance data
            var heroId = HeroData.ID;
            var heroName = HeroData.Name ?? "Hero";
            var heroLevel = HeroData.Level;
            
            // Create a unique visual representation based on hero data
            var baseColor = GetHeroColor(heroId);
            var accentColor = GetHeroAccentColor(heroId);
            
            // Draw hero body (main rectangle)
            graphicsSystem.FillRectangle(centerX + 4, centerY + 8, 24, 16, baseColor);
            
            // Draw hero head
            graphicsSystem.FillRectangle(centerX + 8, centerY + 2, 16, 16, accentColor);
            
            // Draw hero details based on direction
            switch (Direction)
            {
                case Direction.North:
                    // Draw eyes looking up
                    graphicsSystem.FillRectangle(centerX + 10, centerY + 4, 3, 3, Color.White);
                    graphicsSystem.FillRectangle(centerX + 19, centerY + 4, 3, 3, Color.White);
                    graphicsSystem.FillRectangle(centerX + 11, centerY + 5, 1, 1, Color.Black);
                    graphicsSystem.FillRectangle(centerX + 20, centerY + 5, 1, 1, Color.Black);
                    break;
                    
                case Direction.South:
                    // Draw eyes looking down
                    graphicsSystem.FillRectangle(centerX + 10, centerY + 11, 3, 3, Color.White);
                    graphicsSystem.FillRectangle(centerX + 19, centerY + 11, 3, 3, Color.White);
                    graphicsSystem.FillRectangle(centerX + 11, centerY + 12, 1, 1, Color.Black);
                    graphicsSystem.FillRectangle(centerX + 20, centerY + 12, 1, 1, Color.Black);
                    break;
                    
                case Direction.East:
                    // Draw eyes looking right
                    graphicsSystem.FillRectangle(centerX + 18, centerY + 6, 3, 3, Color.White);
                    graphicsSystem.FillRectangle(centerX + 18, centerY + 13, 3, 3, Color.White);
                    graphicsSystem.FillRectangle(centerX + 19, centerY + 7, 1, 1, Color.Black);
                    graphicsSystem.FillRectangle(centerX + 19, centerY + 14, 1, 1, Color.Black);
                    break;
                    
                case Direction.West:
                    // Draw eyes looking left
                    graphicsSystem.FillRectangle(centerX + 11, centerY + 6, 3, 3, Color.White);
                    graphicsSystem.FillRectangle(centerX + 11, centerY + 13, 3, 3, Color.White);
                    graphicsSystem.FillRectangle(centerX + 12, centerY + 7, 1, 1, Color.Black);
                    graphicsSystem.FillRectangle(centerX + 12, centerY + 14, 1, 1, Color.Black);
                    break;
            }
            
            // Draw hero stats indicator (using base stats)
            if (HeroData.BaseStats != null)
            {
                var healthPercent = HeroData.BaseStats.HP / 100.0f; // Normalize to 0-1
                var healthBarWidth = 24;
                var healthBarHeight = 3;
                var healthBarX = centerX + 4;
                var healthBarY = centerY + 26;
                
                // Health bar background
                graphicsSystem.FillRectangle(healthBarX, healthBarY, healthBarWidth, healthBarHeight, Color.DarkRed);
                // Health bar fill
                var healthFillWidth = (int)(healthBarWidth * healthPercent);
                if (healthFillWidth > 0)
                {
                    var healthColor = healthPercent > 0.5f ? Color.Green : healthPercent > 0.25f ? Color.Yellow : Color.Red;
                    graphicsSystem.FillRectangle(healthBarX, healthBarY, healthFillWidth, healthBarHeight, healthColor);
                }
            }
            
            // Draw hero name and level
            var infoY = centerY + 32;
            graphicsSystem.DrawText(heroName, centerX, infoY, Color.White);
            graphicsSystem.DrawText($"Lv.{heroLevel}", centerX + 24, infoY, Color.Yellow);
        }

        private void RenderBasicPlayer(GraphicsSystem graphicsSystem)
        {
            var centerX = Position.X - 16;
            var centerY = Position.Y - 16;
            
            // Basic blue player representation
            graphicsSystem.FillRectangle(centerX + 4, centerY + 8, 24, 16, Color.Blue);
            graphicsSystem.FillRectangle(centerX + 8, centerY + 2, 16, 16, Color.LightBlue);
            
            // Simple face
            graphicsSystem.FillRectangle(centerX + 10, centerY + 6, 3, 3, Color.White);
            graphicsSystem.FillRectangle(centerX + 19, centerY + 6, 3, 3, Color.White);
            graphicsSystem.FillRectangle(centerX + 11, centerY + 7, 1, 1, Color.Black);
            graphicsSystem.FillRectangle(centerX + 20, centerY + 7, 1, 1, Color.Black);
            
            // Direction indicator
            DrawDirectionIndicator(graphicsSystem, centerX + 16, centerY + 16, 8);
        }

        private void DrawDirectionIndicator(GraphicsSystem graphicsSystem, int centerX, int centerY, int size)
        {
            try
            {
                Color indicatorColor = Color.White;
                int indicatorSize = size;
                
                switch (Direction)
                {
                    case Direction.North:
                        // Triangle pointing up - use small rectangle for now
                        graphicsSystem.FillRectangle(centerX - indicatorSize/2, centerY - indicatorSize/2, indicatorSize, indicatorSize, indicatorColor);
                        break;
                    case Direction.South:
                        // Triangle pointing down - use small rectangle for now
                        graphicsSystem.FillRectangle(centerX - indicatorSize/2, centerY - indicatorSize/2, indicatorSize, indicatorSize, indicatorColor);
                        break;
                    case Direction.West:
                        // Triangle pointing left - use small rectangle for now
                        graphicsSystem.FillRectangle(centerX - indicatorSize/2, centerY - indicatorSize/2, indicatorSize, indicatorSize, indicatorColor);
                        break;
                    case Direction.East:
                        // Triangle pointing right - use small rectangle for now
                        graphicsSystem.FillRectangle(centerX - indicatorSize/2, centerY - indicatorSize/2, indicatorSize, indicatorSize, indicatorColor);
                        break;
                }
            }
            catch (Exception ex)
            {
                // Silently fail for direction indicator
            }
        }

        private Color GetHeroColor(int heroId)
        {
            // Generate consistent colors for each hero
            var colors = new[]
            {
                Color.FromArgb(255, 100, 150, 200), // Blue
                Color.FromArgb(255, 200, 100, 100), // Red
                Color.FromArgb(255, 100, 200, 100), // Green
                Color.FromArgb(255, 200, 200, 100), // Yellow
                Color.FromArgb(255, 200, 100, 200), // Purple
                Color.FromArgb(255, 100, 200, 200), // Cyan
                Color.FromArgb(255, 200, 150, 100), // Orange
                Color.FromArgb(255, 150, 100, 200)  // Magenta
            };
            
            return colors[heroId % colors.Length];
        }

        private Color GetHeroAccentColor(int heroId)
        {
            var baseColor = GetHeroColor(heroId);
            return Color.FromArgb(255,
                Math.Min(255, baseColor.R + 40),
                Math.Min(255, baseColor.G + 40),
                Math.Min(255, baseColor.B + 40));
        }
    }
}


