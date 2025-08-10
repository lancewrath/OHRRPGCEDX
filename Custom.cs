using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using OHRRPGCEDX.Graphics;
using OHRRPGCEDX.UI;
using OHRRPGCEDX.Utils;
using OHRRPGCEDX.Input;

namespace OHRRPGCEDX
{
    public class Custom : Form
    {
        private MenuSystem menuSystem;
        private GraphicsSystem graphicsSystem;
        private InputSystem inputSystem;
        private LoggingSystem loggingSystem;
        
        // Version information (similar to old engine)
        private const string SHORT_VERSION = "OHRRPGCE Custom Editor";
        private const string VERSION_CODENAME = "WIP";
        private const string VERSION_DATE = "2024";
        private const string VERSION_REVISION = "1";
        private const string GFX_BACKEND = "sdl2";
        private const string MUSIC_BACKEND = "sdl2";
        
        // Menu options
        private List<string> mainMenuOptions = new List<string>
        {
            "CREATE NEW GAME",
            "OPEN EXISTING GAME",
            "IMPORT GAME",
            "EXPORT GAME",
            "GAME SETTINGS",
            "EDITOR SETTINGS",
            "ABOUT",
            "EXIT"
        };
        
        private int selectedMenuIndex = 0;
        private bool isRunning = true;

        public Custom()
        {
            InitializeComponents();
            InitializeSystems();
            
            // Set up the form loaded event to start the main menu
            this.Load += (sender, e) => StartMainMenu();
            
            // Show the form
            this.Show();
        }

        private void InitializeComponents()
        {
            this.Text = "OHRRPGCE Custom Editor";
            this.Size = new Size(800, 600);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            
            // Handle form closing
            this.FormClosing += (sender, e) => 
            {
                isRunning = false;
                CleanupSystems();
            };
        }

        private void InitializeSystems()
        {
            try
            {
                loggingSystem = LoggingSystem.Instance;
                loggingSystem.Initialize();
                
                loggingSystem.Info("Custom", "Starting system initialization...");
                
                // Ensure the form handle is created before initializing graphics
                if (!this.IsHandleCreated)
                {
                    loggingSystem.Info("Custom", "Creating form handle...");
                    this.CreateHandle();
                    loggingSystem.Info("Custom", $"Form handle created: {this.Handle}");
                }
                else
                {
                    loggingSystem.Info("Custom", $"Form handle already exists: {this.Handle}");
                }
                
                // Initialize graphics system directly on this form
                loggingSystem.Info("Custom", "Initializing graphics system...");
                graphicsSystem = new GraphicsSystem();
                if (!graphicsSystem.Initialize(800, 600, false, true, this.Handle))
                {
                    throw new Exception("Failed to initialize graphics system");
                }
                loggingSystem.Info("Custom", "Graphics system initialized successfully");
                
                inputSystem = new InputSystem();
                inputSystem.Initialize();
                loggingSystem.Info("Custom", "Input system initialized successfully");
                
                menuSystem = new MenuSystem();
                menuSystem.Initialize(graphicsSystem);
                loggingSystem.Info("Custom", "Menu system initialized successfully");
                
                loggingSystem.Info("Custom", "Custom Editor initialized successfully");
            }
            catch (Exception ex)
            {
                loggingSystem?.Error("Custom", $"Failed to initialize systems: {ex.Message}", null, ex);
                MessageBox.Show($"Failed to initialize systems: {ex.Message}", "Initialization Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                throw; // Re-throw to prevent the application from continuing with failed systems
            }
        }

        private Timer gameTimer;

        private void StartMainMenu()
        {
            // Set up a timer for the game loop instead of a separate thread
            gameTimer = new Timer();
            gameTimer.Interval = 16; // ~60 FPS
            gameTimer.Tick += (sender, e) =>
            {
                if (isRunning && !this.IsDisposed)
                {
                    RenderMainMenu();
                    ProcessInput();
                }
                else
                {
                    gameTimer.Stop();
                }
            };
            gameTimer.Start();
        }

        private void RenderMainMenu()
        {
            if (graphicsSystem == null) return;
            
            // Clear with a dark blue background instead of black to make it visible
            graphicsSystem.Clear(Color.DarkBlue);
            
            // Draw title
            string title = "OHRRPGCE CUSTOM EDITOR";
            graphicsSystem.DrawText(title, 400, 100, Color.White, TextAlignment.Center);
            
            // Draw version info at top
            string versionInfo = $"{VERSION_CODENAME} {VERSION_DATE}.{VERSION_REVISION}";
            graphicsSystem.DrawText(versionInfo, 400, 130, Color.Gray, TextAlignment.Center);
            
            // Draw backend info
            string backendInfo = $"In use: {GFX_BACKEND}/{MUSIC_BACKEND}";
            graphicsSystem.DrawText(backendInfo, 400, 150, Color.Gray, TextAlignment.Center);
            
            // Draw menu options
            int startY = 250;
            for (int i = 0; i < mainMenuOptions.Count; i++)
            {
                Color textColor = (i == selectedMenuIndex) ? Color.Yellow : Color.White;
                graphicsSystem.DrawText(mainMenuOptions[i], 400, startY + (i * 40), textColor, TextAlignment.Center);
            }
            
            // Draw version at bottom
            string bottomVersion = $"{SHORT_VERSION} {GFX_BACKEND}/{MUSIC_BACKEND}";
            graphicsSystem.DrawText(bottomVersion, 400, 500, Color.Gray, TextAlignment.Center);
            
            graphicsSystem.Present();
        }

        private void ProcessInput()
        {
            if (inputSystem == null) return;
            
            inputSystem.Update();
            
            // Handle keyboard input
            if (inputSystem.IsKeyPressed(Keys.Up))
            {
                selectedMenuIndex = (selectedMenuIndex - 1 + mainMenuOptions.Count) % mainMenuOptions.Count;
            }
            else if (inputSystem.IsKeyPressed(Keys.Down))
            {
                selectedMenuIndex = (selectedMenuIndex + 1) % mainMenuOptions.Count;
            }
            else if (inputSystem.IsKeyPressed(Keys.Enter))
            {
                ExecuteMenuSelection();
            }
            else if (inputSystem.IsKeyPressed(Keys.Escape))
            {
                if (selectedMenuIndex == mainMenuOptions.Count - 1) // EXIT option
                {
                    isRunning = false;
                    this.Close();
                }
            }
        }

        private void ExecuteMenuSelection()
        {
            switch (selectedMenuIndex)
            {
                case 0: // CREATE NEW GAME
                    CreateNewGame();
                    break;
                case 1: // OPEN EXISTING GAME
                    OpenExistingGame();
                    break;
                case 2: // IMPORT GAME
                    ImportGame();
                    break;
                case 3: // EXPORT GAME
                    ExportGame();
                    break;
                case 4: // GAME SETTINGS
                    GameSettings();
                    break;
                case 5: // EDITOR SETTINGS
                    EditorSettings();
                    break;
                case 6: // ABOUT
                    ShowAbout();
                    break;
                case 7: // EXIT
                    isRunning = false;
                    this.Close();
                    break;
            }
        }

        private void CreateNewGame()
        {
            loggingSystem?.Info("Custom", "Create New Game selected");
            // TODO: Implement new game creation
            MessageBox.Show("Create New Game functionality not yet implemented", "Not Implemented", 
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void OpenExistingGame()
        {
            loggingSystem?.Info("Custom", "Open Existing Game selected");
            // TODO: Implement game opening
            MessageBox.Show("Open Existing Game functionality not yet implemented", "Not Implemented", 
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void ImportGame()
        {
            loggingSystem?.Info("Custom", "Import Game selected");
            // TODO: Implement game import
            MessageBox.Show("Import Game functionality not yet implemented", "Not Implemented", 
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void ExportGame()
        {
            loggingSystem?.Info("Custom", "Export Game selected");
            // TODO: Implement game export
            MessageBox.Show("Export Game functionality not yet implemented", "Not Implemented", 
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void GameSettings()
        {
            loggingSystem?.Info("Custom", "Game Settings selected");
            // TODO: Implement game settings
            MessageBox.Show("Game Settings functionality not yet implemented", "Not Implemented", 
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void EditorSettings()
        {
            loggingSystem?.Info("Custom", "Editor Settings selected");
            // TODO: Implement editor settings
            MessageBox.Show("Editor Settings functionality not yet implemented", "Not Implemented", 
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void ShowAbout()
        {
            loggingSystem?.Info("Custom", "About selected");
            string aboutText = $"OHRRPGCE Custom Editor\n" +
                             $"Version: {VERSION_CODENAME} {VERSION_DATE}.{VERSION_REVISION}\n" +
                             $"Graphics Backend: {GFX_BACKEND}\n" +
                             $"Music Backend: {MUSIC_BACKEND}\n\n" +
                             $"This is a C# port of the OHRRPGCE engine.\n" +
                             $"Original engine written in FreeBASIC.\n\n" +
                             $"Port Status: Work in Progress";
            
            MessageBox.Show(aboutText, "About OHRRPGCE Custom Editor", 
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void CleanupSystems()
        {
            try
            {
                loggingSystem?.Info("Custom", "Shutting down Custom Editor");
                
                inputSystem?.Dispose();
                graphicsSystem?.Dispose();
                loggingSystem?.Shutdown();
                
                loggingSystem?.Info("Custom", "Custom Editor shutdown complete");
            }
            catch (Exception ex)
            {
                // Log error but don't throw during shutdown
                Console.WriteLine($"Error during shutdown: {ex.Message}");
            }
        }

        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Custom());
        }
    }
}
