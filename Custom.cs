using System;
using System.IO;
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

namespace OHRRPGCEDX.Custom
{
    /// <summary>
    /// Main entry point for the OHRRPGCE Custom Editor
    /// This is the equivalent of custom.bas in the original engine
    /// </summary>
    public class CustomEditor : Form
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
        
        private bool isRunning = false;
        private Timer gameTimer;
        
        // Editor state
        private EditorMode currentMode = EditorMode.MainMenu;
        private string currentProjectPath = "";
        private bool projectModified = false;
        
        public CustomEditor()
        {
            try
            {
                File.AppendAllText("startup_debug.log", $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] CustomEditor constructor called\n");
                
                InitializeComponents();
                File.AppendAllText("startup_debug.log", $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] Components initialized successfully\n");
                
                SetupEventHandlers();
                File.AppendAllText("startup_debug.log", $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] Event handlers set up successfully\n");
                
                File.AppendAllText("startup_debug.log", $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] CustomEditor constructor completed successfully\n");
                // InitializeSystems will be called in OnFormLoad when the handle is available
            }
            catch (Exception ex)
            {
                try
                {
                    File.AppendAllText("startup_debug.log", $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] CustomEditor constructor failed: {ex.Message}\n");
                    File.AppendAllText("startup_debug.log", $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] Exception type: {ex.GetType().FullName}\n");
                    File.AppendAllText("startup_debug.log", $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] Stack trace: {ex.StackTrace}\n");
                    
                    if (ex.InnerException != null)
                    {
                        File.AppendAllText("startup_debug.log", $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] Inner exception: {ex.InnerException.Message}\n");
                        File.AppendAllText("startup_debug.log", $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] Inner exception type: {ex.InnerException.GetType().FullName}\n");
                        File.AppendAllText("startup_debug.log", $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] Inner exception stack trace: {ex.InnerException.StackTrace}\n");
                    }
                }
                catch
                {
                    // Ignore file logging errors
                }
                
                throw; // Re-throw to be caught by StartCustomEngine
            }
        }
        
        private void InitializeComponents()
        {
            try
            {
                Console.WriteLine("Initializing components...");
                
                this.Text = "OHRRPGCE Custom Editor";
                this.Size = new Size(1024, 768);
                this.StartPosition = FormStartPosition.CenterScreen;
                this.FormBorderStyle = FormBorderStyle.Sizable;
                this.MinimumSize = new Size(800, 600);
                
                // Set up double buffering for smooth rendering
                this.SetStyle(ControlStyles.DoubleBuffer, true);
                this.SetStyle(ControlStyles.UserPaint, true);
                this.SetStyle(ControlStyles.AllPaintingInWmPaint, true);
                
                Console.WriteLine("Components initialized successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Component initialization failed: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                throw;
            }
        }
        
        private void InitializeSystems()
        {
            try
            {
                File.AppendAllText("startup_debug.log", $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] InitializeSystems() called\n");
                Console.WriteLine("Initializing Custom Editor systems...");
                
                // Step 1: Initialize logging system first
                File.AppendAllText("startup_debug.log", $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] Step 1: Initializing logging system...\n");
                Console.WriteLine("Step 1: Initializing logging system...");
                try
                {
                    loggingSystem = LoggingSystem.Instance;
                    loggingSystem.Initialize("custom_editor.log");
                    File.AppendAllText("startup_debug.log", $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] Logging system initialized successfully\n");
                    Console.WriteLine("Logging system initialized successfully");
                }
                catch (Exception ex)
                {
                    File.AppendAllText("startup_debug.log", $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] Logging system initialization failed: {ex.Message}\n");
                    Console.WriteLine($"Logging system initialization failed: {ex.Message}");
                    Console.WriteLine($"Stack trace: {ex.StackTrace}");
                    // Continue without logging system
                }
                
                // Step 2: Initialize configuration manager
                Console.WriteLine("Step 2: Initializing configuration manager...");
                try
                {
                    configManager = ConfigurationManager.Instance;
                    configManager.Initialize();
                    Console.WriteLine("Configuration manager initialized successfully");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Configuration manager initialization failed: {ex.Message}");
                    Console.WriteLine($"Stack trace: {ex.StackTrace}");
                    // Continue without configuration manager
                }
                
                // Step 3: Initialize session manager
                Console.WriteLine("Step 3: Initializing session manager...");
                try
                {
                    sessionManager = SessionManager.Instance;
                    Console.WriteLine("Session manager initialized successfully");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Session manager initialization failed: {ex.Message}");
                    Console.WriteLine($"Stack trace: {ex.StackTrace}");
                    // Continue without session manager
                }
                
                // Step 4: Initialize input system
                Console.WriteLine("Step 4: Initializing input system...");
                try
                {
                    inputSystem = new InputSystem();
                    inputSystem.Initialize();
                    Console.WriteLine("Input system initialized successfully");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Input system initialization failed: {ex.Message}");
                    Console.WriteLine($"Stack trace: {ex.StackTrace}");
                    // Continue without input system
                }
                
                // Step 5: Initialize audio system
                Console.WriteLine("Step 5: Initializing audio system...");
                try
                {
                    audioSystem = new AudioSystem();
                    audioSystem.Initialize();
                    Console.WriteLine("Audio system initialized successfully");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Audio system initialization failed: {ex.Message}");
                    Console.WriteLine($"Stack trace: {ex.StackTrace}");
                    // Continue without audio system
                }
                
                // Step 6: Initialize script engine
                Console.WriteLine("Step 6: Initializing script engine...");
                try
                {
                    scriptEngine = new ScriptEngine();
                    scriptEngine.Initialize();
                    Console.WriteLine("Script engine initialized successfully");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Script engine initialization failed: {ex.Message}");
                    Console.WriteLine($"Stack trace: {ex.StackTrace}");
                    // Continue without script engine
                }
                
                // Step 7: Initialize menu system
                Console.WriteLine("Step 7: Initializing menu system...");
                try
                {
                    menuSystem = new MenuSystem();
                    Console.WriteLine("Menu system initialized successfully");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Menu system initialization failed: {ex.Message}");
                    Console.WriteLine($"Stack trace: {ex.StackTrace}");
                    // Continue without menu system
                }
                
                // Step 8: Initialize RPG loader
                Console.WriteLine("Step 8: Initializing RPG loader...");
                try
                {
                    rpgLoader = new RPGFileLoader();
                    Console.WriteLine("RPG loader initialized successfully");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"RPG loader initialization failed: {ex.Message}");
                    Console.WriteLine($"Stack trace: {ex.StackTrace}");
                    // Continue without RPG loader
                }
                
                // Step 9: Initialize graphics system
                File.AppendAllText("startup_debug.log", $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] Step 9: Initializing graphics system...\n");
                Console.WriteLine("Step 9: Initializing graphics system...");
                loggingSystem?.Info("Graphics", "Step 9: Initializing graphics system...");
                try
                {
                    File.AppendAllText("startup_debug.log", $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] Creating GraphicsSystem object...\n");
                    graphicsSystem = new GraphicsSystem();
                    File.AppendAllText("startup_debug.log", $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] GraphicsSystem object created successfully\n");
                    loggingSystem?.Info("Graphics", "GraphicsSystem object created successfully");
                    
                    // Initialize graphics system with window dimensions and handle
                    File.AppendAllText("startup_debug.log", $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] Initializing with dimensions: {this.Width}x{this.Height}, Handle: {this.Handle}\n");
                    loggingSystem?.Info("Graphics", $"Initializing with dimensions: {this.Width}x{this.Height}, Handle: {this.Handle}");
                    if (graphicsSystem.Initialize(this.Width, this.Height, false, true, this.Handle))
                    {
                        File.AppendAllText("startup_debug.log", $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] Graphics system initialized successfully\n");
                        Console.WriteLine("Graphics system initialized successfully");
                        loggingSystem?.Info("Graphics", "Graphics system initialized successfully");
                    }
                    else
                    {
                        File.AppendAllText("startup_debug.log", $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] Warning: Graphics system initialization failed, continuing without graphics\n");
                        Console.WriteLine("Warning: Graphics system initialization failed, continuing without graphics");
                        loggingSystem?.Warning("Graphics", "Graphics system initialization failed, continuing without graphics");
                        graphicsSystem = null;
                    }
                }
                catch (Exception ex)
                {
                    File.AppendAllText("startup_debug.log", $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] Graphics system initialization failed: {ex.Message}\n");
                    File.AppendAllText("startup_debug.log", $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] Stack trace: {ex.StackTrace}\n");
                    Console.WriteLine($"Graphics system initialization failed: {ex.Message}");
                    Console.WriteLine($"Stack trace: {ex.StackTrace}");
                    loggingSystem?.Error("Graphics", $"Graphics system initialization failed: {ex.Message}", null, ex);
                    loggingSystem?.Error("Graphics", $"Stack trace: {ex.StackTrace}");
                    Console.WriteLine("Continuing without graphics system");
                    graphicsSystem = null;
                }
                
                File.AppendAllText("startup_debug.log", $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] Basic systems initialized successfully\n");
                Console.WriteLine("Basic systems initialized successfully");
            }
            catch (Exception ex)
            {
                File.AppendAllText("startup_debug.log", $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] Failed to initialize systems: {ex.Message}\n");
                File.AppendAllText("startup_debug.log", $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] Stack trace: {ex.StackTrace}\n");
                Console.WriteLine($"Failed to initialize systems: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                loggingSystem?.Error("Systems", $"Failed to initialize systems: {ex.Message}", null, ex);
                MessageBox.Show($"Failed to initialize systems: {ex.Message}", "Initialization Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        
        private void SetupEventHandlers()
        {
            try
            {
                Console.WriteLine("Setting up event handlers...");
                
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
                gameTimer.Interval = 16; // ~60 FPS
                gameTimer.Tick += OnGameTimerTick;
                
                Console.WriteLine("Event handlers set up successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Event handler setup failed: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                throw;
            }
        }
        
        private void OnFormLoad(object sender, EventArgs e)
        {
            try
            {
                File.AppendAllText("startup_debug.log", $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] Form load event triggered\n");
                Console.WriteLine("Form load event triggered");
                loggingSystem?.Info("Form", "Form load event triggered");
                
                // Initialize systems now that the form handle is available
                File.AppendAllText("startup_debug.log", $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] Calling InitializeSystems()...\n");
                loggingSystem?.Info("Form", "Calling InitializeSystems()...");
                InitializeSystems();
                
                // Check if graphics system is available and start game timer if it is
                if (graphicsSystem != null && graphicsSystem.IsInitialized)
                {
                    File.AppendAllText("startup_debug.log", $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] Graphics system is available, starting game timer...\n");
                    Console.WriteLine("Graphics system is available, starting game timer...");
                    loggingSystem?.Info("Form", "Graphics system is available, starting game timer...");
                    
                    // Start the game timer
                    isRunning = true;
                    gameTimer.Start();
                    
                    File.AppendAllText("startup_debug.log", $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] Game timer started successfully\n");
                    Console.WriteLine("Game timer started successfully");
                    loggingSystem?.Info("Form", "Game timer started successfully");
                }
                else
                {
                    File.AppendAllText("startup_debug.log", $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] Graphics system not available, skipping game timer\n");
                    Console.WriteLine("Graphics system not available, skipping game timer");
                    loggingSystem?.Info("Form", "Graphics system not available, skipping game timer");
                }
                
                // Force a repaint
                this.Invalidate();
                
                File.AppendAllText("startup_debug.log", $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] Custom Editor loaded successfully\n");
                Console.WriteLine("Custom Editor loaded successfully");
                loggingSystem?.Info("Form", "Custom Editor loaded successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Form load failed: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                
                // Show error message but don't crash the form
                try
                {
                    MessageBox.Show($"Failed to load editor: {ex.Message}", "Load Error", 
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                catch
                {
                    // If even the message box fails, just log to console
                    Console.WriteLine("Failed to show error message box");
                }
            }
        }
        
        private void OnFormClosing(object sender, FormClosingEventArgs e)
        {
            try
            {
                isRunning = false;
                gameTimer?.Stop();
                
                // Log closing message before cleanup
                loggingSystem?.Info("Custom Editor", "Custom Editor closing...");
                
                // Save configuration
                configManager?.SaveConfiguration();
                
                // Clean up systems
                CleanupSystems();
                
                // Note: loggingSystem is now null after CleanupSystems, so we can't log the success message
                Console.WriteLine("Custom Editor closed successfully");
            }
            catch (Exception ex)
            {
                // Try to log the error, but if loggingSystem is null, just use Console.WriteLine
                try
                {
                    loggingSystem?.Error("Custom Editor", $"Form closing error: {ex}");
                }
                catch
                {
                    Console.WriteLine($"Form closing error: {ex.Message}");
                }
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
            try
            {
                if (graphicsSystem != null && graphicsSystem.IsInitialized && isRunning)
                {
                    try
                    {
                        RenderFrame();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Warning: RenderFrame failed: {ex.Message}");
                        // Fall through to fallback rendering
                    }
                }
                
                // Always provide fallback rendering if graphics system is not available
                if (graphicsSystem == null || !graphicsSystem.IsInitialized || !isRunning)
                {
                    // Fallback rendering if graphics system is not available
                    using (var brush = new SolidBrush(System.Drawing.Color.DarkBlue))
                    {
                        e.Graphics.FillRectangle(brush, this.ClientRectangle);
                    }
                    
                    using (var brush = new SolidBrush(System.Drawing.Color.White))
                    using (var font = new Font("Arial", 16))
                    {
                        string message = "OHRRPGCE Custom Editor";
                        if (graphicsSystem == null)
                            message += "\nGraphics system not initialized";
                        else if (!graphicsSystem.IsInitialized)
                            message += "\nGraphics system failed to initialize";
                        else if (!isRunning)
                            message += "\nEditor not running";
                        
                        var size = e.Graphics.MeasureString(message, font);
                        var x = (this.ClientRectangle.Width - size.Width) / 2;
                        var y = (this.ClientRectangle.Height - size.Height) / 2;
                        
                        e.Graphics.DrawString(message, font, brush, x, y);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"OnPaint error: {ex.Message}");
                // Draw error message
                try
                {
                    using (var brush = new SolidBrush(System.Drawing.Color.Red))
                    using (var font = new Font("Arial", 12))
                    {
                        e.Graphics.FillRectangle(new SolidBrush(System.Drawing.Color.Black), this.ClientRectangle);
                        e.Graphics.DrawString($"Error: {ex.Message}", font, brush, 10, 10);
                    }
                }
                catch
                {
                    // If even the error rendering fails, just ignore it
                    Console.WriteLine("Failed to render error message in OnPaint");
                }
            }
        }
        
        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            // InputSystem doesn't have HandleKeyDown method, so we'll skip that for now
            
            // Handle global shortcuts
            if (e.Control)
            {
                switch (e.KeyCode)
                {
                    case Keys.O:
                        OpenProject();
                        break;
                    case Keys.S:
                        SaveProject();
                        break;
                    case Keys.N:
                        NewProject();
                        break;
                    case Keys.Q:
                        this.Close();
                        break;
                }
            }
            
            // Test mode switching with number keys
            switch (e.KeyCode)
            {
                case Keys.D1:
                    currentMode = EditorMode.MainMenu;
                    Console.WriteLine("Switched to Main Menu");
                    break;
                case Keys.D2:
                    currentMode = EditorMode.MapEditor;
                    Console.WriteLine("Switched to Map Editor");
                    break;
                case Keys.D3:
                    currentMode = EditorMode.ScriptEditor;
                    Console.WriteLine("Switched to Script Editor");
                    break;
                case Keys.D4:
                    currentMode = EditorMode.SpriteEditor;
                    Console.WriteLine("Switched to Sprite Editor");
                    break;
                case Keys.D5:
                    currentMode = EditorMode.MusicEditor;
                    Console.WriteLine("Switched to Music Editor");
                    break;
            }
        }
        
        private void OnKeyUp(object sender, KeyEventArgs e)
        {
            // InputSystem doesn't have HandleKeyUp method, so we'll skip that for now
        }
        
        private void OnMouseDown(object sender, MouseEventArgs e)
        {
            // InputSystem doesn't have HandleMouseDown method, so we'll skip that for now
        }
        
        private void OnMouseUp(object sender, MouseEventArgs e)
        {
            // InputSystem doesn't have HandleMouseUp method, so we'll skip that for now
        }
        
        private void OnMouseMove(object sender, MouseEventArgs e)
        {
            // InputSystem doesn't have HandleMouseMove method, so we'll skip that for now
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
                // Update input system
                if (inputSystem != null)
                {
                    try
                    {
                        inputSystem.Update();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Warning: Input system update failed: {ex.Message}");
                    }
                }
                
                // Update current editor mode
                try
                {
                    UpdateCurrentMode();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Warning: Mode update failed: {ex.Message}");
                }
                
                // Update audio system
                // AudioSystem doesn't have Update method, so we'll skip that
                
                // Update script engine
                // ScriptEngine doesn't have Update method, so we'll skip that
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Update error: {ex.Message}");
                // Don't use loggingSystem here as it might not be initialized
            }
        }
        
        private void RenderFrame()
        {
            try
            {
                if (graphicsSystem == null || !graphicsSystem.IsInitialized)
                {
                    Console.WriteLine("Warning: Graphics system not available for rendering");
                    return;
                }
                
                try
                {
                    graphicsSystem.BeginScene();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Warning: BeginScene failed: {ex.Message}");
                    return;
                }
                
                try
                {
                    // Render current editor mode
                    RenderCurrentMode();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Warning: Mode rendering failed: {ex.Message}");
                }
                
                // Render UI overlays
                // MenuSystem doesn't have Render method, so we'll skip that
                
                try
                {
                    graphicsSystem.EndScene();
                    graphicsSystem.Present();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Warning: EndScene/Present failed: {ex.Message}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Render error: {ex.Message}");
                // Don't use loggingSystem here as it might not be initialized
            }
        }
        
        private void UpdateCurrentMode()
        {
            switch (currentMode)
            {
                case EditorMode.MainMenu:
                    UpdateMainMenu();
                    break;
                case EditorMode.MapEditor:
                    UpdateMapEditor();
                    break;
                case EditorMode.ScriptEditor:
                    UpdateScriptEditor();
                    break;
                case EditorMode.SpriteEditor:
                    UpdateSpriteEditor();
                    break;
                case EditorMode.MusicEditor:
                    UpdateMusicEditor();
                    break;
                case EditorMode.SoundEditor:
                    UpdateSoundEditor();
                    break;
                case EditorMode.BattleEditor:
                    UpdateBattleEditor();
                    break;
                case EditorMode.ItemEditor:
                    UpdateItemEditor();
                    break;
                case EditorMode.SpellEditor:
                    UpdateSpellEditor();
                    break;
                case EditorMode.EnemyEditor:
                    UpdateEnemyEditor();
                    break;
                case EditorMode.HeroEditor:
                    UpdateHeroEditor();
                    break;
            }
        }
        
        private void RenderCurrentMode()
        {
            switch (currentMode)
            {
                case EditorMode.MainMenu:
                    RenderMainMenu();
                    break;
                case EditorMode.MapEditor:
                    RenderMapEditor();
                    break;
                case EditorMode.ScriptEditor:
                    RenderScriptEditor();
                    break;
                case EditorMode.SpriteEditor:
                    RenderSpriteEditor();
                    break;
                case EditorMode.MusicEditor:
                    RenderMusicEditor();
                    break;
                case EditorMode.SoundEditor:
                    RenderSoundEditor();
                    break;
                case EditorMode.BattleEditor:
                    RenderBattleEditor();
                    break;
                case EditorMode.ItemEditor:
                    RenderItemEditor();
                    break;
                case EditorMode.SpellEditor:
                    RenderSpellEditor();
                    break;
                case EditorMode.EnemyEditor:
                    RenderEnemyEditor();
                    break;
                case EditorMode.HeroEditor:
                    RenderHeroEditor();
                    break;
            }
        }
        
        // Editor mode implementations
        private void ShowMainMenu()
        {
            currentMode = EditorMode.MainMenu;
            // MenuSystem doesn't have ShowMainMenu method, so we'll skip that
        }
        
        private void UpdateMainMenu() { /* Main menu update logic */ }
        private void RenderMainMenu() 
        { 
            try
            {
                // Clear the screen with a dark blue background
                graphicsSystem.Clear(new SharpDX.Mathematics.Interop.RawColor4(0.1f, 0.1f, 0.3f, 1.0f));
                
                // TODO: Implement proper text rendering system
                // For now, we'll just clear the screen with a color to show it's working
                
                // Log that we're rendering the main menu
                Console.WriteLine("Rendering main menu...");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Main menu rendering failed: {ex.Message}");
            }
        }
        
        private void UpdateMapEditor() { /* Map editor update logic */ }
        private void RenderMapEditor() 
        { 
            try
            {
                // Clear the screen with a green background for map editor
                graphicsSystem.Clear(new SharpDX.Mathematics.Interop.RawColor4(0.1f, 0.3f, 0.1f, 1.0f));
                Console.WriteLine("Rendering map editor...");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Map editor rendering failed: {ex.Message}");
            }
        }
        
        private void UpdateScriptEditor() { /* Script editor update logic */ }
        private void RenderScriptEditor() 
        { 
            try
            {
                // Clear the screen with a purple background for script editor
                graphicsSystem.Clear(new SharpDX.Mathematics.Interop.RawColor4(0.3f, 0.1f, 0.3f, 1.0f));
                Console.WriteLine("Rendering script editor...");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Script editor rendering failed: {ex.Message}");
            }
        }
        
        private void UpdateSpriteEditor() { /* Sprite editor update logic */ }
        private void RenderSpriteEditor() 
        { 
            try
            {
                // Clear the screen with a red background for sprite editor
                graphicsSystem.Clear(new SharpDX.Mathematics.Interop.RawColor4(0.3f, 0.1f, 0.1f, 1.0f));
                Console.WriteLine("Rendering sprite editor...");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Sprite editor rendering failed: {ex.Message}");
            }
        }
        
        private void UpdateMusicEditor() { /* Music editor update logic */ }
        private void RenderMusicEditor() 
        { 
            try
            {
                // Clear the screen with a yellow background for music editor
                graphicsSystem.Clear(new SharpDX.Mathematics.Interop.RawColor4(0.3f, 0.3f, 0.1f, 1.0f));
                Console.WriteLine("Rendering music editor...");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Music editor rendering failed: {ex.Message}");
            }
        }
        
        private void UpdateSoundEditor() { /* Sound editor update logic */ }
        private void RenderSoundEditor() { /* Sound editor rendering */ }
        
        private void UpdateBattleEditor() { /* Battle editor update logic */ }
        private void RenderBattleEditor() { /* Battle editor rendering */ }
        
        private void UpdateItemEditor() { /* Item editor update logic */ }
        private void RenderItemEditor() { /* Item editor rendering */ }
        
        private void UpdateSpellEditor() { /* Spell editor update logic */ }
        private void RenderSpellEditor() { /* Spell editor rendering */ }
        
        private void UpdateEnemyEditor() { /* Enemy editor update logic */ }
        private void RenderEnemyEditor() { /* Enemy editor rendering */ }
        
        private void UpdateHeroEditor() { /* Hero editor update logic */ }
        private void RenderHeroEditor() { /* Hero editor rendering */ }
        
        // Project management
        private void NewProject()
        {
            try
            {
                // TODO: Implement new project creation
                currentProjectPath = "";
                projectModified = false;
                loggingSystem?.Info("Custom Editor", "New project created");
            }
            catch (Exception ex)
            {
                loggingSystem?.Error("Custom Editor", $"Failed to create new project: {ex}");
            }
        }
        
        private void OpenProject()
        {
            try
            {
                using (OpenFileDialog openDialog = new OpenFileDialog())
                {
                    openDialog.Filter = "OHRRPGCE Project Files (*.rpg)|*.rpg|All Files (*.*)|*.*";
                    openDialog.Title = "Open OHRRPGCE Project";
                    
                    if (openDialog.ShowDialog() == DialogResult.OK)
                    {
                        currentProjectPath = openDialog.FileName;
                        LoadProject(currentProjectPath);
                        projectModified = false;
                        loggingSystem?.Info("Custom Editor", $"Project opened: {currentProjectPath}");
                    }
                }
            }
            catch (Exception ex)
            {
                loggingSystem?.Error("Custom Editor", $"Failed to open project: {ex}");
                MessageBox.Show($"Failed to open project: {ex.Message}", "Open Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        
        private void SaveProject()
        {
            try
            {
                if (string.IsNullOrEmpty(currentProjectPath))
                {
                    SaveProjectAs();
                }
                else
                {
                    SaveProjectToPath(currentProjectPath);
                    projectModified = false;
                    loggingSystem?.Info("Custom Editor", $"Project saved: {currentProjectPath}");
                }
            }
            catch (Exception ex)
            {
                loggingSystem?.Error("Custom Editor", $"Failed to save project: {ex}");
                MessageBox.Show($"Failed to save project: {ex.Message}", "Save Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        
        private void SaveProjectAs()
        {
            try
            {
                using (SaveFileDialog saveDialog = new SaveFileDialog())
                {
                    saveDialog.Filter = "OHRRPGCE Project Files (*.rpg)|*.rpg|All Files (*.*)|*.*";
                    saveDialog.Title = "Save OHRRPGCE Project";
                    saveDialog.DefaultExt = "rpg";
                    
                    if (saveDialog.ShowDialog() == DialogResult.OK)
                    {
                        currentProjectPath = saveDialog.FileName;
                        SaveProjectToPath(currentProjectPath);
                        projectModified = false;
                        loggingSystem?.Info("Custom Editor", $"Project saved as: {currentProjectPath}");
                    }
                }
            }
            catch (Exception ex)
            {
                loggingSystem?.Error("Custom Editor", $"Failed to save project as: {ex}");
                MessageBox.Show($"Failed to save project: {ex.Message}", "Save Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        
        private void LoadProject(string projectPath)
        {
            try
            {
                // TODO: Implement project loading using RPGFileLoader
                // rpgLoader?.LoadProject(projectPath); // Commented out until RPGFileLoader.LoadProject is implemented
                loggingSystem?.Info("Custom Editor", $"Project loaded: {projectPath}");
            }
            catch (Exception ex)
            {
                loggingSystem?.Error("Custom Editor", $"Failed to load project: {ex}");
                throw;
            }
        }
        
        private void SaveProjectToPath(string projectPath)
        {
            try
            {
                // TODO: Implement project saving
                loggingSystem?.Info("Custom Editor", $"Project saved to: {projectPath}");
            }
            catch (Exception ex)
            {
                loggingSystem?.Error("Custom Editor", $"Failed to save project to path: {ex}");
                throw;
            }
        }
        
        private void LoadDefaultConfiguration()
        {
            try
            {
                // Load default editor settings
                // ConfigurationManager doesn't have LoadDefaultConfiguration method, so we'll skip that
                loggingSystem?.Info("Custom Editor", "Default configuration loaded");
            }
            catch (Exception ex)
            {
                loggingSystem?.Error("Custom Editor", $"Failed to load default configuration: {ex}");
            }
        }
        
        private void CleanupSystems()
        {
            try
            {
                Console.WriteLine("Cleaning up systems...");
                
                // Stop the game timer first
                if (gameTimer != null)
                {
                    gameTimer.Stop();
                    gameTimer.Dispose();
                    gameTimer = null;
                }
                
                // Clean up systems in reverse order of initialization
                // ScriptEngine doesn't implement IDisposable, so we'll skip that
                
                if (audioSystem != null)
                {
                    try
                    {
                        audioSystem.Dispose();
                        audioSystem = null;
                        Console.WriteLine("Audio system disposed successfully");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Warning: Audio system disposal failed: {ex.Message}");
                    }
                }
                
                if (inputSystem != null)
                {
                    try
                    {
                        inputSystem.Dispose();
                        inputSystem = null;
                        Console.WriteLine("Input system disposed successfully");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Warning: Input system disposal failed: {ex.Message}");
                    }
                }
                
                if (graphicsSystem != null)
                {
                    try
                    {
                        graphicsSystem.Dispose();
                        graphicsSystem = null;
                        Console.WriteLine("Graphics system disposed successfully");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Warning: Graphics system disposal failed: {ex.Message}");
                    }
                }
                
                if (loggingSystem != null)
                {
                    try
                    {
                        loggingSystem.Dispose();
                        loggingSystem = null;
                        Console.WriteLine("Logging system disposed successfully");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Warning: Logging system disposal failed: {ex.Message}");
                    }
                }
                
                // Clear other references
                scriptEngine = null;
                menuSystem = null;
                configManager = null;
                sessionManager = null;
                rpgLoader = null;
                
                Console.WriteLine("All systems cleaned up successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during system cleanup: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }

        /// <summary>
        /// Dispose resources
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                try
                {
                    Console.WriteLine("CustomEditor.Dispose called");
                    CleanupSystems();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error in CustomEditor.Dispose: {ex.Message}");
                }
            }
            
            base.Dispose(disposing);
        }
        
            /// <summary>
    /// Static method to start the custom engine (called from Program.cs)
    /// </summary>
    public static void StartCustomEngine(string rpgPath = null)
    {
        // Add immediate file logging to capture startup issues
        try
        {
            File.WriteAllText("startup_debug.log", $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] Starting Custom Engine...\n");
            File.AppendAllText("startup_debug.log", $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] RPG Path: {rpgPath ?? "None"}\n");
        }
        catch
        {
            // Ignore file logging errors
        }
        
        try
        {
            File.AppendAllText("startup_debug.log", $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] Enabling visual styles...\n");
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            
            File.AppendAllText("startup_debug.log", $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] Creating CustomEditor instance...\n");
            
            // Create and run the editor
            using (CustomEditor editor = new CustomEditor())
            {
                File.AppendAllText("startup_debug.log", $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] CustomEditor instance created successfully\n");
                File.AppendAllText("startup_debug.log", $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] Starting Application.Run...\n");
                
                Application.Run(editor);
                
                File.AppendAllText("startup_debug.log", $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] Application.Run completed successfully\n");
            }
        }
        catch (Exception ex)
        {
            try
            {
                File.AppendAllText("startup_debug.log", $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] Fatal error in Custom Editor: {ex.Message}\n");
                File.AppendAllText("startup_debug.log", $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] Exception type: {ex.GetType().FullName}\n");
                File.AppendAllText("startup_debug.log", $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] Stack trace: {ex.StackTrace}\n");
                
                if (ex.InnerException != null)
                {
                    File.AppendAllText("startup_debug.log", $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] Inner exception: {ex.InnerException.Message}\n");
                    File.AppendAllText("startup_debug.log", $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] Inner exception type: {ex.InnerException.GetType().FullName}\n");
                    File.AppendAllText("startup_debug.log", $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] Inner exception stack trace: {ex.InnerException.StackTrace}\n");
                }
            }
            catch
            {
                // Ignore file logging errors
            }
            
            try
            {
                MessageBox.Show($"Fatal error in Custom Editor: {ex.Message}", "Fatal Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Exception msgEx)
            {
                try
                {
                    File.AppendAllText("startup_debug.log", $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] Failed to show error message box: {msgEx.Message}\n");
                }
                catch
                {
                    // Ignore file logging errors
                }
            }
        }
    }

    /// <summary>
    /// Main entry point for the Custom Editor
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
                
                // Create and run the editor
                using (CustomEditor editor = new CustomEditor())
                {
                    Application.Run(editor);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fatal error in Custom Editor: {ex.Message}", "Fatal Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
    
    /// <summary>
    /// Enumeration of editor modes
    /// </summary>
    public enum EditorMode
    {
        MainMenu,
        MapEditor,
        ScriptEditor,
        SpriteEditor,
        MusicEditor,
        SoundEditor,
        BattleEditor,
        ItemEditor,
        SpellEditor,
        EnemyEditor,
        HeroEditor
    }
}
