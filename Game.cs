using System;
using System.Windows.Forms;
using System.Drawing;
using SharpDX;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using OHRRPGCEDX.Graphics;
using OHRRPGCEDX.GameData;
using OHRRPGCEDX.Input;
using OHRRPGCEDX.Audio;
using OHRRPGCEDX.Scripting;
using OHRRPGCEDX.UI;
using OHRRPGCEDX.Utils;
using OHRRPGCEDX.Configuration;
using OHRRPGCEDX.Session;

namespace OHRRPGCEDX.Game
{
    /// <summary>
    /// Main entry point for the OHRRPGCE Game Runtime
    /// This is the equivalent of game.bas in the original engine
    /// </summary>
    public class GameRuntime : Form
    {
        private GraphicsSystem graphicsSystem;
        private ShaderSystem shaderSystem;
        private TextureManager textureManager;
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
                
                graphicsSystem = new GraphicsSystem();
                graphicsSystem.Initialize(this.Width, this.Height, false, true, this.Handle);
                
                            // Initialize shader and texture systems
            shaderSystem = new ShaderSystem(graphicsSystem.GetDevice());
            
            textureManager = new TextureManager(graphicsSystem.GetDevice());
                
                inputSystem = new InputSystem();
                inputSystem.Initialize();
                
                audioSystem = new AudioSystem();
                audioSystem.Initialize();
                
                scriptEngine = new ScriptEngine();
                scriptEngine.Initialize();
                
                menuSystem = new MenuSystem();
                
                rpgLoader = new RPGFileLoader();
                battleSystem = new BattleSystem();
                saveLoadSystem = new SaveLoadSystem();
                
                // Initialize MapRenderer after graphics system is ready
                // mapRenderer will be initialized when we have a map to render
                
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
            
            // Set up game timer
            gameTimer = new Timer();
            gameTimer.Interval = FRAME_TIME_MS;
            gameTimer.Tick += OnGameTimerTick;
        }
        
        private void OnFormLoad(object sender, EventArgs e)
        {
            try
            {
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
            if (graphicsSystem != null && isRunning)
            {
                RenderFrame();
            }
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
            if (isRunning)
            {
                Update();
                this.Invalidate(); // Trigger repaint
            }
        }
        
        private void Update()
        {
            try
            {
                DateTime currentTime = DateTime.Now;
                double deltaTime = (currentTime - lastFrameTime).TotalSeconds;
                lastFrameTime = currentTime;
                
                // Update input system
                inputSystem?.Update();
                
                // Update current game state
                UpdateCurrentState(deltaTime);
                
                // Update audio system
                // audioSystem?.Update(); // Commented out until AudioSystem.Update is implemented
                
                // Update script engine
                // scriptEngine?.Update(); // Commented out until ScriptEngine.Update is implemented
            }
            catch (Exception ex)
            {
                loggingSystem?.Error("Game Runtime", $"Update error: {ex}");
            }
        }
        
        private void RenderFrame()
        {
            try
            {
                graphicsSystem?.BeginScene();
                
                // Render current game state
                RenderCurrentState();
                
                // Render UI overlays
                if (currentState != GameState.Loading)
                {
                    // menuSystem?.Render(); // Commented out until MenuSystem.Render is implemented
                }
                
                graphicsSystem?.EndScene();
                graphicsSystem?.Present();
            }
            catch (Exception ex)
            {
                loggingSystem?.Error("Game Runtime", $"Render error: {ex}");
            }
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
            switch (currentState)
            {
                case GameState.Loading:
                    RenderLoading();
                    break;
                case GameState.MainMenu:
                    RenderMainMenu();
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
            if (currentState == GameState.Loading)
            {
                ShowMainMenu();
            }
        }
        
        private void RenderLoading()
        {
            // TODO: Render loading screen
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
            // TODO: Render main menu
        }
        
        private void StartNewGame()
        {
            try
            {
                // Load the RPG file
                if (string.IsNullOrEmpty(currentRPGPath))
                {
                    // Show file dialog to select RPG file
                    using (OpenFileDialog openDialog = new OpenFileDialog())
                    {
                        openDialog.Filter = "OHRRPGCE Game Files (*.rpg)|*.rpg|All Files (*.*)|*.*";
                        openDialog.Title = "Select Game File";
                        
                        if (openDialog.ShowDialog() == DialogResult.OK)
                        {
                            currentRPGPath = openDialog.FileName;
                        }
                        else
                        {
                            return; // User cancelled
                        }
                    }
                }
                
                // Load the game data
                LoadGameData(currentRPGPath);
                
                // Initialize player and starting map
                InitializeNewGame();
                
                // Start playing
                currentState = GameState.Playing;
                loggingSystem?.Info("Game Runtime", "New game started");
            }
            catch (Exception ex)
            {
                loggingSystem?.Error("Game Runtime", $"Failed to start new game: {ex}");
                MessageBox.Show($"Failed to start game: {ex.Message}", "Game Error", 
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
                player.Position = new Vector2(0, 0); // Default starting position
                
                // Initialize map renderer with graphics system components
                if (graphicsSystem != null && currentMap != null && shaderSystem != null && textureManager != null)
                {
                    try
                    {
                        mapRenderer = new MapRenderer(
                            graphicsSystem.GetDevice(), 
                            graphicsSystem.GetDeviceContext(), 
                            shaderSystem, 
                            textureManager
                        );
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
                // Render map
                // mapRenderer?.Render(); // Commented out until MapRenderer.Render is implemented
                
                // Render player
                player?.Render(graphicsSystem);
                
                // Render NPCs and other entities
                RenderMapEntities();
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
            // TODO: Render NPCs, events, and other map entities
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
            // Render pause menu overlay
        }
        
        private void UpdateBattle(double deltaTime)
        {
            // battleSystem?.Update(deltaTime); // Commented out until BattleSystem.Update is implemented
        }
        
        private void RenderBattle()
        {
            // battleSystem?.Render(graphicsSystem); // Commented out until BattleSystem.Render is implemented
        }
        
        private void UpdateMenu(double deltaTime)
        {
            // Handle menu input
        }
        
        private void RenderMenu()
        {
            // Render current menu
        }
        
        private void UpdateDialog(double deltaTime)
        {
            // Handle dialog input
        }
        
        private void RenderDialog()
        {
            // Render dialog box
        }
        
        private void UpdateGameOver(double deltaTime)
        {
            // Handle game over input
        }
        
        private void RenderGameOver()
        {
            // Render game over screen
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
    }
    
    /// <summary>
    /// Enumeration of game states
    /// </summary>
    public enum GameState
    {
        Loading,
        MainMenu,
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
        public Vector2 Position { get; set; }
        public HeroData HeroData { get; private set; }
        
        public void Initialize(HeroData heroData)
        {
            HeroData = heroData;
            Position = Vector2.Zero;
        }
        
        public void Update(double deltaTime, InputSystem inputSystem)
        {
            // TODO: Implement player movement and input handling
        }
        
        public void Render(GraphicsSystem graphicsSystem)
        {
            // TODO: Implement player rendering
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
