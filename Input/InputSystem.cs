using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using SharpDX.DirectInput;

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
        private Dictionary<Keys, bool> keyBindings;
        private Dictionary<string, int> actionBindings;

        public InputSystem()
        {
            gamepads = new List<Joystick>();
            currentGamepadStates = new List<JoystickState>();
            previousGamepadStates = new List<JoystickState>();
            keyBindings = new Dictionary<Keys, bool>();
            actionBindings = new Dictionary<string, int>();
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
            keyBindings[Keys.W] = false;
            keyBindings[Keys.A] = false;
            keyBindings[Keys.S] = false;
            keyBindings[Keys.D] = false;
            keyBindings[Keys.Up] = false;
            keyBindings[Keys.Down] = false;
            keyBindings[Keys.Left] = false;
            keyBindings[Keys.Right] = false;
            
            // Action keys
            keyBindings[Keys.Enter] = false;
            keyBindings[Keys.Space] = false;
            keyBindings[Keys.Escape] = false;
            keyBindings[Keys.Tab] = false;
            
            // Number keys for menu selection
            for (int i = 0; i < 10; i++)
            {
                keyBindings[Keys.D0 + i] = false;
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
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to update input system: {ex.Message}");
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
        /// Check if a key is currently pressed
        /// </summary>
        public bool IsKeyPressed(Keys key)
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
        public bool IsKeyJustPressed(Keys key)
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
        public bool IsKeyJustReleased(Keys key)
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
                    return IsKeyPressed(Keys.W) || IsKeyPressed(Keys.Up);
                case "MoveDown":
                    return IsKeyPressed(Keys.S) || IsKeyPressed(Keys.Down);
                case "MoveLeft":
                    return IsKeyPressed(Keys.A) || IsKeyPressed(Keys.Left);
                case "MoveRight":
                    return IsKeyPressed(Keys.D) || IsKeyPressed(Keys.Right);
                case "Confirm":
                    return IsKeyPressed(Keys.Enter) || IsKeyPressed(Keys.Space);
                case "Cancel":
                    return IsKeyPressed(Keys.Escape);
                case "Menu":
                    return IsKeyPressed(Keys.Tab);
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
                    return IsKeyJustPressed(Keys.W) || IsKeyJustPressed(Keys.Up);
                case "MoveDown":
                    return IsKeyJustPressed(Keys.S) || IsKeyJustPressed(Keys.Down);
                case "MoveLeft":
                    return IsKeyJustPressed(Keys.A) || IsKeyJustPressed(Keys.Left);
                case "MoveRight":
                    return IsKeyJustPressed(Keys.D) || IsKeyJustPressed(Keys.Right);
                case "Confirm":
                    return IsKeyJustPressed(Keys.Enter) || IsKeyJustPressed(Keys.Space);
                case "Cancel":
                    return IsKeyJustPressed(Keys.Escape);
                case "Menu":
                    return IsKeyJustPressed(Keys.Tab);
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
