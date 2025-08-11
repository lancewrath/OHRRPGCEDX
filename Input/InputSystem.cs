using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using SharpDX.DirectInput;
using System.Windows.Forms; // Add this back for Windows Forms Keys support
using System.Linq; // Added for .ToList()

namespace OHRRPGCEDX.Input
{
    /// <summary>
    /// Input system for handling keyboard, mouse, and gamepad input
    /// </summary>
    public class InputSystem : IDisposable
    {
        private DirectInput directInput;
        private Keyboard keyboard;
        private Mouse mouse;
        private List<Joystick> gamepads;
        
        private KeyboardState currentKeyboardState;
        private KeyboardState previousKeyboardState;
        private MouseState currentMouseState;
        private MouseState previousMouseState;
        private List<JoystickState> currentGamepadStates;
        private List<JoystickState> previousGamepadStates;
        
        private bool isInitialized;
        private Dictionary<Key, bool> keyBindings;
        private Dictionary<string, int> actionBindings;

        // Key repeat system fields
        private Dictionary<Key, DateTime> keyPressStartTimes;
        private Dictionary<Key, DateTime> keyLastRepeatTimes;
        private int initialRepeatDelayMs = 150;  // 150ms initial delay (much more responsive)
        private int repeatIntervalMs = 50;       // 50ms between repeats (much faster)

        // Key repeat configuration properties
        public int InitialRepeatDelayMs
        {
            get { return initialRepeatDelayMs; }
            set { initialRepeatDelayMs = Math.Max(100, value); } // Minimum 100ms
        }

        public int RepeatIntervalMs
        {
            get { return repeatIntervalMs; }
            set { repeatIntervalMs = Math.Max(20, value); } // Minimum 20ms
        }

        public InputSystem()
        {
            gamepads = new List<Joystick>();
            currentGamepadStates = new List<JoystickState>();
            previousGamepadStates = new List<JoystickState>();
            keyBindings = new Dictionary<Key, bool>();
            actionBindings = new Dictionary<string, int>();
            
            // Initialize key repeat tracking
            keyPressStartTimes = new Dictionary<Key, DateTime>();
            keyLastRepeatTimes = new Dictionary<Key, DateTime>();
        }

        /// <summary>
        /// Initialize the input system
        /// </summary>
        public bool Initialize()
        {
            try
            {
                directInput = new DirectInput();
                
                // Initialize keyboard
                keyboard = new Keyboard(directInput);
                keyboard.SetCooperativeLevel(IntPtr.Zero, CooperativeLevel.Background | CooperativeLevel.NonExclusive);
                keyboard.Acquire();
                
                // Initialize mouse
                mouse = new Mouse(directInput);
                mouse.SetCooperativeLevel(IntPtr.Zero, CooperativeLevel.Background | CooperativeLevel.NonExclusive);
                mouse.Acquire();
                
                // Initialize gamepads
                InitializeGamepads();
                
                // Set up default key bindings
                SetupDefaultKeyBindings();
                
                isInitialized = true;
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to initialize input system: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Initialize available gamepads
        /// </summary>
        private void InitializeGamepads()
        {
            try
            {
                // TODO: Fix DeviceClass enum value - need to determine correct SharpDX.DirectInput enum
                // For now, skip gamepad initialization to avoid compilation errors
                /*
                var gamepadGuids = directInput.GetDevices(DeviceClass.GameController, DeviceEnumerationFlags.AllDevices);
                
                foreach (var deviceInstance in gamepadGuids)
                {
                    try
                    {
                        var gamepad = new Joystick(directInput, deviceInstance.InstanceGuid);
                        gamepad.SetCooperativeLevel(IntPtr.Zero, CooperativeLevel.Background | CooperativeLevel.NonExclusive);
                        gamepad.Acquire();
                        
                        gamepads.Add(gamepad);
                        currentGamepadStates.Add(new JoystickState());
                        previousGamepadStates.Add(new JoystickState());
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Failed to initialize gamepad {deviceInstance.InstanceName}: {ex.Message}");
                    }
                }
                */
                
                Console.WriteLine("Gamepad initialization temporarily disabled - need to fix DeviceClass enum values");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to enumerate gamepads: {ex.Message}");
            }
        }

        /// <summary>
        /// Set up default key bindings
        /// </summary>
        private void SetupDefaultKeyBindings()
        {
            // Movement keys
            keyBindings[Key.W] = false;
            keyBindings[Key.A] = false;
            keyBindings[Key.S] = false;
            keyBindings[Key.D] = false;
            keyBindings[Key.Up] = false;
            keyBindings[Key.Down] = false;
            keyBindings[Key.Left] = false;
            keyBindings[Key.Right] = false;
            
            // Action keys
            keyBindings[Key.Return] = false;
            keyBindings[Key.Space] = false;
            keyBindings[Key.Escape] = false;
            keyBindings[Key.Tab] = false;
            
            // Number keys for menu selection
            for (int i = 0; i < 10; i++)
            {
                keyBindings[Key.D0 + i] = false;
            }
            
            // Action bindings
            actionBindings["MoveUp"] = 0;
            actionBindings["MoveDown"] = 1;
            actionBindings["MoveLeft"] = 2;
            actionBindings["MoveRight"] = 3;
            actionBindings["Confirm"] = 4;
            actionBindings["Cancel"] = 5;
            actionBindings["Menu"] = 6;
        }

        /// <summary>
        /// Update input states
        /// </summary>
        public void Update()
        {
            if (!isInitialized) return;

            try
            {
                // Update keyboard
                previousKeyboardState = currentKeyboardState;
                currentKeyboardState = keyboard.GetCurrentState();
                
                // Update mouse
                previousMouseState = currentMouseState;
                currentMouseState = mouse.GetCurrentState();
                
                // Update gamepads
                for (int i = 0; i < gamepads.Count; i++)
                {
                    previousGamepadStates[i] = currentGamepadStates[i];
                    currentGamepadStates[i] = gamepads[i].GetCurrentState();
                }
                
                // Update key bindings
                UpdateKeyBindings();
                
                // Update key repeat timing
                UpdateKeyRepeatTiming();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to update input system: {ex.Message}");
            }
        }

        /// <summary>
        /// Update key repeat timing for held keys
        /// </summary>
        private void UpdateKeyRepeatTiming()
        {
            var now = DateTime.Now;
            
            // Check all keys that are currently pressed
            foreach (var key in keyBindings.Keys.ToList())
            {
                bool isCurrentlyPressed = IsKeyPressed(key);
                
                if (isCurrentlyPressed)
                {
                    // Key is pressed - track timing
                    if (!keyPressStartTimes.ContainsKey(key))
                    {
                        // First time this key was pressed
                        keyPressStartTimes[key] = now;
                        keyLastRepeatTimes[key] = now;
                    }
                }
                else
                {
                    // Key is not pressed - remove timing data
                    keyPressStartTimes.Remove(key);
                    keyLastRepeatTimes.Remove(key);
                }
            }
        }

        /// <summary>
        /// Update key binding states
        /// </summary>
        private void UpdateKeyBindings()
        {
            foreach (var key in keyBindings.Keys)
            {
                keyBindings[key] = IsKeyPressed(key);
            }
        }

        /// <summary>
        /// Convert System.Windows.Forms.Keys to SharpDX.DirectInput.Key
        /// </summary>
        private Key ConvertKeys(Keys windowsKey)
        {
            // Map common Windows Forms keys to SharpDX DirectInput keys
            switch (windowsKey)
            {
                case Keys.W: return Key.W;
                case Keys.A: return Key.A;
                case Keys.S: return Key.S;
                case Keys.D: return Key.D;
                case Keys.Up: return Key.Up;
                case Keys.Down: return Key.Down;
                case Keys.Left: return Key.Left;
                case Keys.Right: return Key.Right;
                case Keys.Enter: return Key.Return;
                case Keys.Space: return Key.Space;
                case Keys.Escape: return Key.Escape;
                case Keys.Tab: return Key.Tab;
                case Keys.L: return Key.L;
                case Keys.D0: return Key.D0;
                case Keys.D1: return Key.D1;
                case Keys.D2: return Key.D2;
                case Keys.D3: return Key.D3;
                case Keys.D4: return Key.D4;
                case Keys.D5: return Key.D5;
                case Keys.D6: return Key.D6;
                case Keys.D7: return Key.D7;
                case Keys.D8: return Key.D8;
                case Keys.D9: return Key.D9;
                default: return Key.Unknown;
            }
        }

        /// <summary>
        /// Check if a key is currently pressed (Windows Forms Keys version)
        /// </summary>
        public bool IsKeyPressed(Keys key)
        {
            return IsKeyPressed(ConvertKeys(key));
        }

        /// <summary>
        /// Check if a key was just pressed (Windows Forms Keys version)
        /// </summary>
        public bool IsKeyJustPressed(Keys key)
        {
            return IsKeyJustPressed(ConvertKeys(key));
        }

        /// <summary>
        /// Check if a key was just released (Windows Forms Keys version)
        /// </summary>
        public bool IsKeyJustReleased(Keys key)
        {
            return IsKeyJustReleased(ConvertKeys(key));
        }

        /// <summary>
        /// Check if a key should repeat (for menu navigation)
        /// </summary>
        public bool ShouldKeyRepeat(Key key)
        {
            if (!isInitialized || !keyPressStartTimes.ContainsKey(key)) return false;
            
            var now = DateTime.Now;
            var pressStartTime = keyPressStartTimes[key];
            var lastRepeatTime = keyLastRepeatTimes[key];
            
            // Check if we've passed the initial delay
            if ((now - pressStartTime).TotalMilliseconds < initialRepeatDelayMs)
                return false;
            
            // Check if it's time for the next repeat
            if ((now - lastRepeatTime).TotalMilliseconds < repeatIntervalMs)
                return false;
            
            // Update the last repeat time
            keyLastRepeatTimes[key] = now;
            return true;
        }

        /// <summary>
        /// Check if a key should repeat (Windows Forms Keys version)
        /// </summary>
        public bool ShouldKeyRepeat(Keys key)
        {
            return ShouldKeyRepeat(ConvertKeys(key));
        }

        /// <summary>
        /// Reset key repeat timing for a specific key
        /// Useful when switching between different input contexts (e.g., different menus)
        /// </summary>
        public void ResetKeyRepeat(Key key)
        {
            keyPressStartTimes.Remove(key);
            keyLastRepeatTimes.Remove(key);
        }

        /// <summary>
        /// Reset key repeat timing for a Windows Forms key
        /// </summary>
        public void ResetKeyRepeat(Keys key)
        {
            ResetKeyRepeat(ConvertKeys(key));
        }

        /// <summary>
        /// Reset all key repeat timing
        /// Useful when switching between major game states
        /// </summary>
        public void ResetAllKeyRepeat()
        {
            keyPressStartTimes.Clear();
            keyLastRepeatTimes.Clear();
        }

        /// <summary>
        /// Check if a key is currently pressed
        /// </summary>
        public bool IsKeyPressed(Key key)
        {
            if (!isInitialized) return false;
            
            try
            {
                return currentKeyboardState.IsPressed(key);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Check if a key was just pressed this frame
        /// </summary>
        public bool IsKeyJustPressed(Key key)
        {
            if (!isInitialized) return false;
            
            try
            {
                return currentKeyboardState.IsPressed(key) && !previousKeyboardState.IsPressed(key);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Check if a key was just released this frame
        /// </summary>
        public bool IsKeyJustReleased(Key key)
        {
            if (!isInitialized) return false;
            
            try
            {
                return !currentKeyboardState.IsPressed(key) && previousKeyboardState.IsPressed(key);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Check if an action is currently active
        /// </summary>
        public bool IsActionActive(string actionName)
        {
            if (!actionBindings.ContainsKey(actionName)) return false;
            
            switch (actionName)
            {
                case "MoveUp":
                    return IsKeyPressed(Key.W) || IsKeyPressed(Key.Up);
                case "MoveDown":
                    return IsKeyPressed(Key.S) || IsKeyPressed(Key.Down);
                case "MoveLeft":
                    return IsKeyPressed(Key.A) || IsKeyPressed(Key.Left);
                case "MoveRight":
                    return IsKeyPressed(Key.D) || IsKeyPressed(Key.Right);
                case "Confirm":
                    return IsKeyPressed(Key.Return) || IsKeyPressed(Key.Space);
                case "Cancel":
                    return IsKeyPressed(Key.Escape);
                case "Menu":
                    return IsKeyPressed(Key.Tab);
                default:
                    return false;
            }
        }

        /// <summary>
        /// Check if an action was just activated this frame
        /// </summary>
        public bool IsActionJustActivated(string actionName)
        {
            if (!actionBindings.ContainsKey(actionName)) return false;
            
            switch (actionName)
            {
                case "MoveUp":
                    return IsKeyJustPressed(Key.W) || IsKeyJustPressed(Key.Up);
                case "MoveDown":
                    return IsKeyJustPressed(Key.S) || IsKeyJustPressed(Key.Down);
                case "MoveLeft":
                    return IsKeyJustPressed(Key.A) || IsKeyJustPressed(Key.Left);
                case "MoveRight":
                    return IsKeyJustPressed(Key.D) || IsKeyJustPressed(Key.Right);
                case "Confirm":
                    return IsKeyJustPressed(Key.Return) || IsKeyJustPressed(Key.Space);
                case "Cancel":
                    return IsKeyJustPressed(Key.Escape);
                case "Menu":
                    return IsKeyJustPressed(Key.Tab);
                default:
                    return false;
            }
        }

        /// <summary>
        /// Get mouse position
        /// </summary>
        public System.Drawing.Point GetMousePosition()
        {
            if (!isInitialized) return System.Drawing.Point.Empty;
            
            try
            {
                return new System.Drawing.Point(currentMouseState.X, currentMouseState.Y);
            }
            catch
            {
                return System.Drawing.Point.Empty;
            }
        }

        /// <summary>
        /// Get mouse delta movement
        /// </summary>
        public System.Drawing.Point GetMouseDelta()
        {
            if (!isInitialized) return System.Drawing.Point.Empty;
            
            try
            {
                return new System.Drawing.Point(currentMouseState.X - previousMouseState.X, 
                                              currentMouseState.Y - previousMouseState.Y);
            }
            catch
            {
                return System.Drawing.Point.Empty;
            }
        }

        /// <summary>
        /// Check if mouse button is pressed
        /// </summary>
        public bool IsMouseButtonPressed(int button)
        {
            if (!isInitialized) return false;
            
            try
            {
                switch (button)
                {
                    case 0: return currentMouseState.Buttons[0];
                    case 1: return currentMouseState.Buttons[1];
                    case 2: return currentMouseState.Buttons[2];
                    default: return false;
                }
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Check if mouse button was just pressed
        /// </summary>
        public bool IsMouseButtonJustPressed(int button)
        {
            if (!isInitialized) return false;
            
            try
            {
                bool current = false, previous = false;
                
                switch (button)
                {
                    case 0:
                        current = currentMouseState.Buttons[0];
                        previous = previousMouseState.Buttons[0];
                        break;
                    case 1:
                        current = currentMouseState.Buttons[1];
                        previous = previousMouseState.Buttons[1];
                        break;
                    case 2:
                        current = currentMouseState.Buttons[2];
                        previous = previousMouseState.Buttons[2];
                        break;
                }
                
                return current && !previous;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Get gamepad count
        /// </summary>
        public int GamepadCount => gamepads.Count;

        /// <summary>
        /// Check if gamepad button is pressed
        /// </summary>
        public bool IsGamepadButtonPressed(int gamepadIndex, int button)
        {
            if (!isInitialized || gamepadIndex < 0 || gamepadIndex >= gamepads.Count) return false;
            
            try
            {
                return currentGamepadStates[gamepadIndex].Buttons[button];
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Get gamepad axis value
        /// </summary>
        public int GetGamepadAxis(int gamepadIndex, int axis)
        {
            if (!isInitialized || gamepadIndex < 0 || gamepadIndex >= gamepads.Count) return 0;
            
            try
            {
                switch (axis)
                {
                    case 0: return currentGamepadStates[gamepadIndex].X;
                    case 1: return currentGamepadStates[gamepadIndex].Y;
                    case 2: return currentGamepadStates[gamepadIndex].Z;
                    case 3: return currentGamepadStates[gamepadIndex].RotationX;
                    case 4: return currentGamepadStates[gamepadIndex].RotationY;
                    case 5: return currentGamepadStates[gamepadIndex].RotationZ;
                    default: return 0;
                }
            }
            catch
            {
                return 0;
            }
        }

        /// <summary>
        /// Check if input system is initialized
        /// </summary>
        public bool IsInitialized => isInitialized;

        /// <summary>
        /// Dispose resources
        /// </summary>
        public void Dispose()
        {
            if (keyboard != null)
            {
                keyboard.Unacquire();
                keyboard.Dispose();
                keyboard = null;
            }
            
            if (mouse != null)
            {
                mouse.Unacquire();
                mouse.Dispose();
                mouse = null;
            }
            
            foreach (var gamepad in gamepads)
            {
                try
                {
                    gamepad.Unacquire();
                    gamepad.Dispose();
                }
                catch { }
            }
            gamepads.Clear();
            currentGamepadStates.Clear();
            previousGamepadStates.Clear();
            
            if (directInput != null)
            {
                directInput.Dispose();
                directInput = null;
            }
            
            isInitialized = false;
        }
    }
}
