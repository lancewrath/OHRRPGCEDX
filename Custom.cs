using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using OHRRPGCEDX.Graphics;
using OHRRPGCEDX.UI;
using OHRRPGCEDX.Utils;
using OHRRPGCEDX.Input;
using System.IO;

namespace OHRRPGCEDX
{
    public class Custom : Form
    {
        private MenuSystem menuSystem;
        private GraphicsSystem graphicsSystem;
        private InputSystem inputSystem;
        private LoggingSystem loggingSystem;
        private FileBrowser fileBrowser;
        private FileBrowserRenderer fileBrowserRenderer;
        
        // Version information (similar to old engine)
        private const string SHORT_VERSION = "OHRRPGCE Custom Editor";
        private const string VERSION_CODENAME = "WIP";
        private const string VERSION_DATE = "2024";
        private const string VERSION_REVISION = "1";
        private const string GFX_BACKEND = "SharpDX";
        private const string MUSIC_BACKEND = "XAudio";
        
        // Main editor menu options (matching old engine's main_editor_menu)
        private List<string> startupMenuOptions = new List<string>
        {
            "CREATE NEW GAME",
            "LOAD EXISTING GAME", 
            "EXIT PROGRAM"
        };
        
        private List<string> mainMenuOptions = new List<string>
        {
            "Edit Graphics",
            "Edit Maps",
            "Edit Heroes", 
            "Edit Enemies",
            "Edit Attacks",
            "Edit Battle Formations",
            "Edit Items",
            "Edit Shops",
            "Edit Text Boxes",
            "Edit Tag Names",
            "Edit Menus",
            "Edit Slice Collections",
            "Edit Vehicles",
            "Import Music",
            "Import Sound Effects", 
            "Edit Global Text Strings",
            "Edit General Game Settings",
            "Script Management",
            "Distribute Game",
            "Test Game",
            "Quit or Save"
        };
        
        private int selectedMenuIndex = 0;
        private int selectedStartupMenuIndex = 1; // Default to "LOAD EXISTING GAME" (index 1)
        private bool isRunning = true;
        private bool showHelpText = false;
        private bool showingStartupMenu = true; // Start with startup menu
        private bool showingFileBrowser = false; // Show file browser for loading games

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
                // Initialize logging system first
                loggingSystem = LoggingSystem.Instance;
                loggingSystem.Initialize("Custom.log");
                loggingSystem.Info("Custom", "Starting system initialization...");
                
                // Ensure the form handle is created before initializing graphics
                if (!this.IsHandleCreated)
                {
                    loggingSystem.Info("Custom", "Creating form handle...");
                    this.CreateHandle();
                    loggingSystem.Info("Custom", $"Form handle created: {this.Handle}");
                }
                
                // Initialize graphics system
                loggingSystem.Info("Custom", "Creating graphics system...");
                graphicsSystem = new GraphicsSystem();
                loggingSystem.Info("Custom", "Graphics system created, initializing...");
                
                if (!graphicsSystem.Initialize(800, 600, false, true, this.Handle))
                {
                    loggingSystem.Error("Custom", "Failed to initialize graphics system");
                    MessageBox.Show("Failed to initialize graphics system", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                
                loggingSystem.Info("Custom", "Graphics system initialized successfully");

                // Initialize input system
                inputSystem = new InputSystem();
                if (!inputSystem.Initialize())
                {
                    loggingSystem.Error("Custom", "Failed to initialize input system");
                    MessageBox.Show("Failed to initialize input system", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                // Configure key repeat timing for menu navigation
                // Adjust these values to change how responsive the menu feels
                inputSystem.InitialRepeatDelayMs = 400;  // 400ms initial delay (slightly faster than default)
                inputSystem.RepeatIntervalMs = 80;       // 80ms between repeats (slightly faster than default)

                // Initialize menu system
                menuSystem = new MenuSystem();
                menuSystem.Initialize(graphicsSystem);

                // Set up main menu items
                SetupMainMenu();

                // Initialize file browser system
                fileBrowser = new FileBrowser();
                fileBrowserRenderer = new FileBrowserRenderer(fileBrowser, graphicsSystem);

                loggingSystem?.Info("Custom", "All systems initialized successfully");
            }
            catch (Exception ex)
            {
                loggingSystem?.Error("Custom", $"Error initializing systems: {ex.Message}");
                MessageBox.Show($"Failed to initialize systems: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Set up the main editor menu with all options
        /// </summary>
        private void SetupMainMenu()
        {
            // Set up menu options to match original engine appearance
            var menuOptions = new UI.MenuOptions
            {
                edged = false,           // No border around menu (like original engine)
                centered = false,        // Left-aligned (not centered)
                show_numbers = false,    // No item numbers
                max_width = 0            // Auto-width
            };
            menuSystem.SetOptions(menuOptions);
            
            // Add menu items
            foreach (var option in mainMenuOptions)
            {
                menuSystem.AddItem(option);
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
                    // Update input system first
                    if (inputSystem != null)
                        inputSystem.Update();
                    
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
            if (graphicsSystem == null || !graphicsSystem.IsInitialized) 
            {
                loggingSystem?.Warning("Custom", "Graphics system not available for rendering");
                return;
            }

            try
            {
                loggingSystem?.Debug("Custom", "Starting to render main menu...");
                
                // Clear the screen
                graphicsSystem.BeginScene();
                graphicsSystem.Clear(new SharpDX.Mathematics.Interop.RawColor4(0.1f, 0.1f, 0.1f, 1.0f));

                if (showingStartupMenu)
                {
                    RenderStartupMenu();
                }
                else if (showingFileBrowser)
                {
                    RenderFileBrowser();
                }
                else
                {
                    RenderEditorMenu();
                }

                graphicsSystem.EndScene();
                graphicsSystem.Present();
                
                loggingSystem?.Debug("Custom", "Main menu rendering completed successfully");
            }
            catch (Exception ex)
            {
                loggingSystem?.Error("Custom", $"Error rendering main menu: {ex.Message}");
                loggingSystem?.Error("Custom", $"Stack trace: {ex.StackTrace}");
            }
        }

        private void RenderStartupMenu()
        {
            // Draw title "O.H.R.RPG.C.E" at the top-left (matching original engine positioning)
            string title = "O.H.R.RPG.C.E";
            graphicsSystem.DrawText(title, 4, 4, System.Drawing.Color.DarkBlue, Graphics.TextAlignment.Left);
            
            // Draw menu options starting below title (matching original engine layout)
            for (int i = 0; i < startupMenuOptions.Count; i++)
            {
                var option = startupMenuOptions[i];
                var color = (i == selectedStartupMenuIndex) ? System.Drawing.Color.Yellow : System.Drawing.Color.LightGray;
                var yPos = 40 + (i * 20); // Compact spacing like original engine
                
                graphicsSystem.DrawText(option, 4, yPos, color, Graphics.TextAlignment.Left);
            }
            
            // Draw footer text at bottom (matching original engine footer positioning)
            string versionInfo = "OHRRPGCE kaleidophone+1 20250810 sdl2/sd12";
            string helpText = "Press F1 for help on any menu!";
            
            graphicsSystem.DrawText(versionInfo, 4, graphicsSystem.ScreenHeight - 40, System.Drawing.Color.LightGray, Graphics.TextAlignment.Left);
            graphicsSystem.DrawText(helpText, 4, graphicsSystem.ScreenHeight - 20, System.Drawing.Color.LightGray, Graphics.TextAlignment.Left);
        }

        private void RenderEditorMenu()
        {
            // Draw title
            string title = $"{SHORT_VERSION} v{VERSION_REVISION} ({VERSION_CODENAME})";
            string subtitle = $"Built {VERSION_DATE} - {GFX_BACKEND} graphics, {MUSIC_BACKEND} music";
            
            // Draw title text at top-left (matching original engine positioning)
            graphicsSystem.DrawText(title, 4, 4, System.Drawing.Color.White, Graphics.TextAlignment.Left);
            graphicsSystem.DrawText(subtitle, 4, 24, System.Drawing.Color.Gray, Graphics.TextAlignment.Left);

            // Render the menu using MenuSystem below the title (matching original engine)
            if (menuSystem != null)
            {
                // Set menu options to match original engine behavior
                var menuOptions = new MenuOptions
                {
                    edged = false,           // No border around menu
                    centered = false,        // Left-aligned (not centered)
                    show_numbers = false,    // No item numbers
                    max_width = 0            // Auto-width
                };
                menuSystem.SetOptions(menuOptions);
                
                // Position menu below title text (around y=50) like original engine
                // The MenuSystem will handle the actual rendering at the correct position
                menuSystem.Render(graphicsSystem);
            }

            // Draw help text at bottom (matching original engine footer positioning)
            if (showHelpText)
            {
                string helpText = "Use arrow keys to navigate, Enter to select, F1 for help";
                graphicsSystem.DrawText(helpText, 4, graphicsSystem.ScreenHeight - 20, System.Drawing.Color.Gray, Graphics.TextAlignment.Left);
            }
        }

        private void RenderFileBrowser()
        {
            if (fileBrowserRenderer != null)
            {
                fileBrowserRenderer.Render();
            }
        }

        private void ProcessInput()
        {
            if (inputSystem == null) return;

            try
            {
                if (showingStartupMenu)
                {
                    ProcessStartupMenuInput();
                }
                else if (showingFileBrowser)
                {
                    ProcessFileBrowserInput();
                }
                else
                {
                    ProcessEditorMenuInput();
                }
            }
            catch (Exception ex)
            {
                loggingSystem?.Error("Custom", $"Error processing input: {ex.Message}");
            }
        }

        private void ProcessStartupMenuInput()
        {
            if (inputSystem.IsKeyJustPressed(Keys.Up) || inputSystem.ShouldKeyRepeat(Keys.Up))
            {
                selectedStartupMenuIndex = Math.Max(0, selectedStartupMenuIndex - 1);
            }
            else if (inputSystem.IsKeyJustPressed(Keys.Down) || inputSystem.ShouldKeyRepeat(Keys.Down))
            {
                selectedStartupMenuIndex = Math.Min(startupMenuOptions.Count - 1, selectedStartupMenuIndex + 1);
            }
            else if (inputSystem.IsKeyPressed(Keys.Enter))
            {
                ExecuteStartupMenuSelection();
            }
            else if (inputSystem.IsKeyPressed(Keys.Escape))
            {
                // Exit the application
                isRunning = false;
            }
            else if (inputSystem.IsKeyPressed(Keys.F1))
            {
                // Toggle help text
                showHelpText = !showHelpText;
            }
        }

        private void ProcessEditorMenuInput()
        {
            if (menuSystem == null) return;

            // Handle input for menu navigation
            if (inputSystem.IsKeyJustPressed(Keys.Up) || inputSystem.ShouldKeyRepeat(Keys.Up))
            {
                menuSystem.MoveUp();
            }
            else if (inputSystem.IsKeyJustPressed(Keys.Down) || inputSystem.ShouldKeyRepeat(Keys.Down))
            {
                menuSystem.MoveDown();
            }
            else if (inputSystem.IsKeyJustPressed(Keys.Left) || inputSystem.ShouldKeyRepeat(Keys.Left))
            {
                menuSystem.MoveLeft();
            }
            else if (inputSystem.IsKeyJustPressed(Keys.Right) || inputSystem.ShouldKeyRepeat(Keys.Right))
            {
                menuSystem.MoveRight();
            }
            else if (inputSystem.IsKeyPressed(Keys.Enter))
            {
                // Execute the selected menu item
                ExecuteMenuSelection();
            }
            else if (inputSystem.IsKeyPressed(Keys.Escape))
            {
                // Go back to startup menu
                showingStartupMenu = true;
                // Reset key repeat timing when switching menus
                inputSystem.ResetAllKeyRepeat();
                loggingSystem?.Info("Custom", "Starting file browser for loading game");
            }
            else if (inputSystem.IsKeyPressed(Keys.F1))
            {
                // Toggle help text
                showHelpText = !showHelpText;
            }

            // Update the selected menu index to match MenuSystem
            selectedMenuIndex = menuSystem.GetSelectedIndex();
        }

        private void ProcessFileBrowserInput()
        {
            if (fileBrowser == null) return;

            // Handle input for file browser navigation
            if (inputSystem.IsKeyJustPressed(Keys.Up) || inputSystem.ShouldKeyRepeat(Keys.Up))
            {
                fileBrowser.MoveUp();
            }
            else if (inputSystem.IsKeyJustPressed(Keys.Down) || inputSystem.ShouldKeyRepeat(Keys.Down))
            {
                fileBrowser.MoveDown();
            }
            else if (inputSystem.IsKeyPressed(Keys.Enter))
            {
                // Navigate to selected entry or select file
                if (fileBrowser.NavigateToSelected())
                {
                    // File was selected, load it and go to editor
                    string selectedPath = fileBrowser.GetSelectedPath();
                    if (!string.IsNullOrEmpty(selectedPath))
                    {
                        loggingSystem?.Info("Custom", $"Selected RPG file: {selectedPath}");
                        // TODO: Load the RPG file here
                        MessageBox.Show($"Selected RPG file: {selectedPath}\n\nFile loading functionality will be implemented next.", 
                            "File Selected", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        
                        // Go to editor menu
                        showingFileBrowser = false;
                        showingStartupMenu = false;
                        inputSystem.ResetAllKeyRepeat();
                    }
                }
            }
            else if (inputSystem.IsKeyPressed(Keys.Escape))
            {
                // Go back to startup menu
                showingFileBrowser = false;
                showingStartupMenu = true;
                inputSystem.ResetAllKeyRepeat();
                loggingSystem?.Info("Custom", "Returning to startup menu from file browser");
            }
            else if (inputSystem.IsKeyPressed(Keys.Back))
            {
                // Go up a directory
                fileBrowser.GoUpDirectory();
            }
            else if (inputSystem.IsKeyPressed(Keys.F5))
            {
                // Refresh file listing
                fileBrowser.Refresh();
            }
            else if (inputSystem.IsKeyPressed(Keys.F1))
            {
                // Toggle help text
                showHelpText = !showHelpText;
            }
        }

        private void ExecuteStartupMenuSelection()
        {
            try
            {
                string selectedOption = startupMenuOptions[selectedStartupMenuIndex];
                loggingSystem?.Info("Custom", $"Selected startup menu option: {selectedOption} (index: {selectedStartupMenuIndex})");

                switch (selectedStartupMenuIndex)
                {
                    case 0: // CREATE NEW GAME
                        // For now, just switch to editor menu
                        // In a real implementation, this would create a new game file
                        showingStartupMenu = false;
                        // Reset key repeat timing when switching menus
                        inputSystem.ResetAllKeyRepeat();
                        loggingSystem?.Info("Custom", "Switching to editor menu for new game");
                        break;
                    case 1: // LOAD EXISTING GAME
                        // Start file browser for loading RPG files
                        showingStartupMenu = false;
                        showingFileBrowser = true;
                        
                        // Initialize file browser to the bin/Debug/net48 directory where the test RPG file is located
                        string defaultPath = Path.Combine(Application.StartupPath, "bin", "Debug", "net48");
                        if (!Directory.Exists(defaultPath))
                        {
                            // Fallback to current directory if the expected path doesn't exist
                            defaultPath = Environment.CurrentDirectory;
                        }
                        fileBrowser.Initialize(FileBrowser.BrowseFileType.RPG, defaultPath);
                        
                        // Reset key repeat timing when switching menus
                        inputSystem.ResetAllKeyRepeat();
                        loggingSystem?.Info("Custom", "Starting file browser for loading existing game");
                        break;
                    case 2: // EXIT PROGRAM
                        isRunning = false;
                        loggingSystem?.Info("Custom", "Exiting application");
                        break;
                    default:
                        loggingSystem?.Warning("Custom", $"Unknown startup menu option: {selectedOption}");
                        break;
                }
            }
            catch (Exception ex)
            {
                loggingSystem?.Error("Custom", $"Error executing startup menu selection: {ex.Message}");
            }
        }

        private void ExecuteMenuSelection()
        {
            if (menuSystem == null) return;

            try
            {
                var selectedItem = menuSystem.GetSelectedItem();
                if (selectedItem == null) return;

                int menuIndex = menuSystem.GetSelectedIndex();
                string selectedOption = selectedItem.text;

                loggingSystem?.Info("Custom", $"Selected menu option: {selectedOption} (index: {menuIndex})");

                // Execute the selected menu option
                switch (menuIndex)
                {
                    case 0: EditGraphics(); break;
                    case 1: EditMaps(); break;
                    case 2: EditHeroes(); break;
                    case 3: EditEnemies(); break;
                    case 4: EditAttacks(); break;
                    case 5: EditBattleFormations(); break;
                    case 6: EditItems(); break;
                    case 7: EditShops(); break;
                    case 8: EditTextBoxes(); break;
                    case 9: EditTagNames(); break;
                    case 10: EditMenus(); break;
                    case 11: EditSliceCollections(); break;
                    case 12: EditVehicles(); break;
                    case 13: ImportMusic(); break;
                    case 14: ImportSoundEffects(); break;
                    case 15: EditGlobalTextStrings(); break;
                    case 16: EditGeneralGameSettings(); break;
                    case 17: ScriptManagement(); break;
                    case 18: DistributeGame(); break;
                    case 19: TestGame(); break;
                    case 20: PromptForSaveAndQuit(); break;
                    default:
                        loggingSystem?.Warning("Custom", $"Unknown menu option: {selectedOption}");
                        break;
                }
            }
            catch (Exception ex)
            {
                loggingSystem?.Error("Custom", $"Error executing menu selection: {ex.Message}");
            }
        }

        // Menu action methods - these will be implemented as we port each editor
        private void EditGraphics()
        {
            loggingSystem?.Info("Custom", "Edit Graphics selected");
            MessageBox.Show("Edit Graphics functionality not yet implemented", "Not Implemented", 
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void EditMaps()
        {
            loggingSystem?.Info("Custom", "Edit Maps selected");
            MessageBox.Show("Edit Maps functionality not yet implemented", "Not Implemented", 
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void EditHeroes()
        {
            loggingSystem?.Info("Custom", "Edit Heroes selected");
            MessageBox.Show("Edit Heroes functionality not yet implemented", "Not Implemented", 
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void EditEnemies()
        {
            loggingSystem?.Info("Custom", "Edit Enemies selected");
            MessageBox.Show("Edit Enemies functionality not yet implemented", "Not Implemented", 
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void EditAttacks()
        {
            loggingSystem?.Info("Custom", "Edit Attacks selected");
            MessageBox.Show("Edit Attacks functionality not yet implemented", "Not Implemented", 
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void EditBattleFormations()
        {
            loggingSystem?.Info("Custom", "Edit Battle Formations selected");
            MessageBox.Show("Edit Battle Formations functionality not yet implemented", "Not Implemented", 
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void EditItems()
        {
            loggingSystem?.Info("Custom", "Edit Items selected");
            MessageBox.Show("Edit Items functionality not yet implemented", "Not Implemented", 
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void EditShops()
        {
            loggingSystem?.Info("Custom", "Edit Shops selected");
            MessageBox.Show("Edit Shops functionality not yet implemented", "Not Implemented", 
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void EditTextBoxes()
        {
            loggingSystem?.Info("Custom", "Edit Text Boxes selected");
            MessageBox.Show("Edit Text Boxes functionality not yet implemented", "Not Implemented", 
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void EditTagNames()
        {
            loggingSystem?.Info("Custom", "Edit Tag Names selected");
            MessageBox.Show("Edit Tag Names functionality not yet implemented", "Not Implemented", 
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void EditMenus()
        {
            loggingSystem?.Info("Custom", "Edit Menus selected");
            MessageBox.Show("Edit Menus functionality not yet implemented", "Not Implemented", 
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void EditSliceCollections()
        {
            loggingSystem?.Info("Custom", "Edit Slice Collections selected");
            MessageBox.Show("Edit Slice Collections functionality not yet implemented", "Not Implemented", 
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void EditVehicles()
        {
            loggingSystem?.Info("Custom", "Edit Vehicles selected");
            MessageBox.Show("Edit Vehicles functionality not yet implemented", "Not Implemented", 
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void ImportMusic()
        {
            loggingSystem?.Info("Custom", "Import Music selected");
            MessageBox.Show("Import Music functionality not yet implemented", "Not Implemented", 
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void ImportSoundEffects()
        {
            loggingSystem?.Info("Custom", "Import Sound Effects selected");
            MessageBox.Show("Import Sound Effects functionality not yet implemented", "Not Implemented", 
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void EditGlobalTextStrings()
        {
            loggingSystem?.Info("Custom", "Edit Global Text Strings selected");
            MessageBox.Show("Edit Global Text Strings functionality not yet implemented", "Not Implemented", 
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void EditGeneralGameSettings()
        {
            loggingSystem?.Info("Custom", "Edit General Game Settings selected");
            MessageBox.Show("Edit General Game Settings functionality not yet implemented", "Not Implemented", 
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void ScriptManagement()
        {
            loggingSystem?.Info("Custom", "Script Management selected");
            MessageBox.Show("Script Management functionality not yet implemented", "Not Implemented", 
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void DistributeGame()
        {
            loggingSystem?.Info("Custom", "Distribute Game selected");
            MessageBox.Show("Distribute Game functionality not yet implemented", "Not Implemented", 
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void TestGame()
        {
            loggingSystem?.Info("Custom", "Test Game selected");
            MessageBox.Show("Test Game functionality not yet implemented", "Not Implemented", 
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void PromptForSaveAndQuit()
        {
            loggingSystem?.Info("Custom", "Quit or Save selected");
            
            string[] quitOptions = {
                "Continue editing",
                "Save changes and continue editing", 
                "Save changes and quit",
                "Discard changes and quit"
            };
            
            DialogResult result = MessageBox.Show(
                "Do you want to save your changes before quitting?",
                "Save and Quit",
                MessageBoxButtons.YesNoCancel,
                MessageBoxIcon.Question);
                
            if (result == DialogResult.Yes)
            {
                // TODO: Implement save functionality
                loggingSystem?.Info("Custom", "Saving changes before quit");
                MessageBox.Show("Save functionality not yet implemented", "Not Implemented", 
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            
            isRunning = false;
            this.Close();
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
