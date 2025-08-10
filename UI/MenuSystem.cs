using System;
using System.Collections.Generic;
using System.Linq;
using OHRRPGCEDX.Graphics;

namespace OHRRPGCEDX.UI
{
    /// <summary>
    /// Menu state for tracking current selection and navigation
    /// </summary>
    public class MenuState
    {
        public int pt = 0;                    // Current selection point
        public int last = 0;                  // Last valid menu item index
        public int size = 20;                 // Menu display size
        public bool autosize = false;         // Auto-size menu to fit screen
        public bool autosize_ignore_pixels = false; // Ignore pixel count for autosize
        public bool need_update = false;      // Menu needs redraw

        public MenuState()
        {
            pt = 0;
            last = 0;
            size = 20;
            autosize = false;
            autosize_ignore_pixels = false;
            need_update = false;
        }
    }

    /// <summary>
    /// Menu options for display customization
    /// </summary>
    public class MenuOptions
    {
        public bool edged = false;            // Draw edges around menu
        public bool centered = false;         // Center menu on screen
        public bool show_numbers = false;     // Show item numbers
        public int max_width = 0;             // Maximum menu width (0 = auto)

        public MenuOptions()
        {
            edged = false;
            centered = false;
            show_numbers = false;
            max_width = 0;
        }
    }

    /// <summary>
    /// Menu item with text and optional data
    /// </summary>
    public class MenuItem
    {
        public string text;                   // Display text
        public object data;                   // Associated data
        public bool enabled = true;           // Item is selectable
        public bool visible = true;           // Item is visible
        public int color = 0;                 // Text color (0 = default)

        public MenuItem(string text, object data = null)
        {
            this.text = text;
            this.data = data;
            this.enabled = true;
            this.visible = true;
            this.color = 0;
        }
    }

    /// <summary>
    /// Menu system for handling user interface menus
    /// </summary>
    public class MenuSystem
    {
        private List<MenuItem> items;
        private MenuState state;
        private MenuOptions options;
        private int cursor_x, cursor_y;
        private int menu_width, menu_height;

        public MenuSystem()
        {
            items = new List<MenuItem>();
            state = new MenuState();
            options = new MenuOptions();
        }

        /// <summary>
        /// Initialize the menu system with a graphics system
        /// </summary>
        public void Initialize(GraphicsSystem graphicsSystem)
        {
            // Store reference to graphics system for rendering
            // For now, we'll just mark that initialization is complete
            // TODO: Set up graphics system integration
        }

        /// <summary>
        /// Add a menu item
        /// </summary>
        public void AddItem(string text, object data = null)
        {
            items.Add(new MenuItem(text, data));
            state.last = items.Count - 1;
        }

        /// <summary>
        /// Add multiple menu items
        /// </summary>
        public void AddItems(params string[] texts)
        {
            foreach (var text in texts)
            {
                AddItem(text);
            }
        }

        /// <summary>
        /// Clear all menu items
        /// </summary>
        public void Clear()
        {
            items.Clear();
            state.pt = 0;
            state.last = 0;
        }

        /// <summary>
        /// Get the currently selected item
        /// </summary>
        public MenuItem GetSelectedItem()
        {
            if (state.pt >= 0 && state.pt < items.Count)
                return items[state.pt];
            return null;
        }

        /// <summary>
        /// Get the currently selected item index
        /// </summary>
        public int GetSelectedIndex()
        {
            return state.pt;
        }

        /// <summary>
        /// Set the selected item by index
        /// </summary>
        public void SetSelection(int index)
        {
            if (index >= 0 && index < items.Count)
                state.pt = index;
        }

        /// <summary>
        /// Move selection up
        /// </summary>
        public void MoveUp()
        {
            if (state.pt > 0)
                state.pt--;
            else if (state.last > 0)
                state.pt = state.last;
        }

        /// <summary>
        /// Move selection down
        /// </summary>
        public void MoveDown()
        {
            if (state.pt < state.last)
                state.pt++;
            else
                state.pt = 0;
        }

        /// <summary>
        /// Move selection left (for grid menus)
        /// </summary>
        public void MoveLeft()
        {
            // For now, just move up - can be overridden for grid layouts
            MoveUp();
        }

        /// <summary>
        /// Move selection right (for grid menus)
        /// </summary>
        public void MoveRight()
        {
            // For now, just move down - can be overridden for grid layouts
            MoveDown();
        }

        /// <summary>
        /// Get menu item by index
        /// </summary>
        public MenuItem GetItem(int index)
        {
            if (index >= 0 && index < items.Count)
                return items[index];
            return null;
        }

        /// <summary>
        /// Get menu item count
        /// </summary>
        public int ItemCount => items.Count;

        /// <summary>
        /// Check if menu has items
        /// </summary>
        public bool HasItems => items.Count > 0;

        /// <summary>
        /// Get menu state for external access
        /// </summary>
        public MenuState GetState() => state;

        /// <summary>
        /// Set menu options
        /// </summary>
        public void SetOptions(MenuOptions newOptions)
        {
            options = newOptions ?? new MenuOptions();
        }

        /// <summary>
        /// Calculate menu dimensions
        /// </summary>
        public void CalculateDimensions()
        {
            if (items.Count == 0)
            {
                menu_width = 0;
                menu_height = 0;
                return;
            }

            // Calculate width based on longest item
            menu_width = items.Max(item => item.text?.Length ?? 0);
            
            // Add padding for numbers, edges, etc.
            if (options.show_numbers)
                menu_width += 4; // "1. "
            if (options.edged)
                menu_width += 2; // "| "

            // Apply max width constraint
            if (options.max_width > 0 && menu_width > options.max_width)
                menu_width = options.max_width;

            // Calculate height
            menu_height = Math.Min(items.Count, state.size);
        }

        /// <summary>
        /// Get the current menu dimensions
        /// </summary>
        public (int width, int height) GetDimensions() => (menu_width, menu_height);

        /// <summary>
        /// Render the menu using the graphics system
        /// </summary>
        public void Render(GraphicsSystem graphicsSystem)
        {
            if (graphicsSystem == null || !graphicsSystem.IsInitialized) return;

            try
            {
                // Calculate menu dimensions if needed
                CalculateDimensions();

                // Get screen dimensions
                int screenWidth = graphicsSystem.ScreenWidth;
                int screenHeight = graphicsSystem.ScreenHeight;

                // Calculate menu position
                int menuX = options.centered ? (screenWidth - menu_width) / 2 : 50;
                int menuY = options.centered ? (screenHeight - menu_height) / 2 : 50;

                // Draw menu background
                if (options.edged)
                {
                    // Draw border around menu
                    var borderColor = new SharpDX.Color4(0.2f, 0.2f, 0.2f, 1.0f);
                    var backgroundColor = new SharpDX.Color4(0.1f, 0.1f, 0.1f, 0.8f);
                    
                    // Background
                    graphicsSystem.DrawText("", menuX - 5, menuY - 5, System.Drawing.Color.FromArgb(0, 0, 0, 0));
                    // This will be replaced with actual rectangle drawing when implemented
                }

                // Draw menu items
                int currentY = menuY;
                for (int i = 0; i < items.Count && i < state.size; i++)
                {
                    if (!items[i].visible) continue;

                    var item = items[i];
                    var textColor = item.enabled ? 
                        (i == state.pt ? System.Drawing.Color.Yellow : System.Drawing.Color.White) :
                        System.Drawing.Color.Gray;

                    // Add selection indicator
                    string displayText = item.text;
                    if (i == state.pt)
                    {
                        displayText = "> " + displayText;
                    }
                    else
                    {
                        displayText = "  " + displayText;
                    }

                    // Add item numbers if requested
                    if (options.show_numbers)
                    {
                        displayText = $"{i + 1:00}. {displayText}";
                    }

                    // Draw the menu item text
                    graphicsSystem.DrawText(displayText, menuX, currentY, textColor);
                    currentY += 20; // Line height
                }

                // Draw cursor if needed
                if (state.pt < items.Count)
                {
                    int cursorY = menuY + (state.pt * 20);
                    graphicsSystem.DrawText(">", menuX - 20, cursorY, System.Drawing.Color.Yellow);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error rendering menu: {ex.Message}");
            }
        }
    }
}
