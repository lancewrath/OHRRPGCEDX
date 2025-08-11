using System;
using System.Collections.Generic;
using System.Drawing;
using OHRRPGCEDX.Graphics;

namespace OHRRPGCEDX.UI
{
    public class FileBrowserRenderer
    {
        private FileBrowser fileBrowser;
        private GraphicsSystem graphicsSystem;
        private int screenWidth;
        private int screenHeight;
        private int menuStartY = 50;
        private int menuItemHeight = 20;
        private int maxVisibleItems = 20;

        public FileBrowserRenderer(FileBrowser browser, GraphicsSystem graphics)
        {
            fileBrowser = browser;
            graphicsSystem = graphics;
            screenWidth = graphics.ScreenWidth;
            screenHeight = graphics.ScreenHeight;
        }

        public void Render()
        {
            if (fileBrowser == null || graphicsSystem == null) return;

            try
            {
                // Draw title
                string title = "O.H.R.RPG.C.E";
                graphicsSystem.DrawText(title, 4, 4, Color.DarkBlue, Graphics.TextAlignment.Left);

                // Draw current path (highlighted in blue like original)
                string currentPath = fileBrowser.GetCurrentDirectory();
                if (!string.IsNullOrEmpty(currentPath))
                {
                    // Draw blue background for current path
                    int pathWidth = currentPath.Length * 8; // Approximate character width
                    graphicsSystem.FillRectangle(4, 24, pathWidth + 8, 16, Color.FromArgb(0, 0, 128)); // Dark blue background
                    graphicsSystem.DrawText(currentPath, 8, 26, Color.White, Graphics.TextAlignment.Left);
                }

                // Draw drive list (if any)
                var entries = fileBrowser.GetEntries();
                int startIndex = 0;
                int visibleCount = Math.Min(maxVisibleItems, entries.Count);

                // Calculate scroll position if needed
                int selectedIndex = fileBrowser.GetSelectedIndex();
                if (selectedIndex >= maxVisibleItems)
                {
                    startIndex = Math.Max(0, selectedIndex - maxVisibleItems / 2);
                    visibleCount = Math.Min(maxVisibleItems, entries.Count - startIndex);
                }

                // Draw menu items
                for (int i = 0; i < visibleCount; i++)
                {
                    int actualIndex = startIndex + i;
                    if (actualIndex >= entries.Count) break;

                    var entry = entries[actualIndex];
                    int yPos = menuStartY + (i * menuItemHeight);
                    bool isSelected = (actualIndex == selectedIndex);

                    // Draw selection highlight
                    if (isSelected)
                    {
                        int itemWidth = Math.Min(entry.Caption.Length * 8, screenWidth - 16);
                        Color highlightColor = GetHighlightColor(entry.Kind);
                        graphicsSystem.FillRectangle(4, yPos - 2, itemWidth + 8, menuItemHeight, highlightColor);
                    }

                    // Draw the entry text
                    Color textColor = GetTextColor(entry.Kind, isSelected);
                    string displayText = GetDisplayText(entry);
                    graphicsSystem.DrawText(displayText, 8, yPos, textColor, Graphics.TextAlignment.Left);
                }

                // Draw footer information
                DrawFooter();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error rendering file browser: {ex.Message}");
            }
        }

        private Color GetHighlightColor(BrowseEntryKind kind)
        {
            switch (kind)
            {
                case BrowseEntryKind.Drive:
                    return Color.FromArgb(0, 0, 128); // Blue for drives
                case BrowseEntryKind.ParentDir:
                case BrowseEntryKind.SubDir:
                    return Color.FromArgb(128, 128, 128); // Gray for directories
                case BrowseEntryKind.Selectable:
                    return Color.FromArgb(0, 0, 128); // Blue for files
                case BrowseEntryKind.Root:
                    return Color.FromArgb(128, 128, 128); // Gray for root
                default:
                    return Color.FromArgb(0, 0, 128); // Default blue
            }
        }

        private Color GetTextColor(BrowseEntryKind kind, bool isSelected)
        {
            if (isSelected)
            {
                return Color.White; // White text on highlighted background
            }

            switch (kind)
            {
                case BrowseEntryKind.Drive:
                    return Color.LightGray; // Light gray for drives
                case BrowseEntryKind.ParentDir:
                case BrowseEntryKind.SubDir:
                    return Color.LightGray; // Light gray for directories
                case BrowseEntryKind.Selectable:
                    return Color.LightGray; // Light gray for files
                case BrowseEntryKind.Root:
                    return Color.LightGray; // Light gray for root
                default:
                    return Color.LightGray; // Default light gray
            }
        }

        private string GetDisplayText(BrowseMenuEntry entry)
        {
            switch (entry.Kind)
            {
                case BrowseEntryKind.Drive:
                    return entry.Caption; // Already formatted as "C:\ Volume Label"
                case BrowseEntryKind.ParentDir:
                    return entry.Caption; // Already formatted as "dirname\"
                case BrowseEntryKind.SubDir:
                    return entry.Caption; // Already formatted as "dirname\"
                case BrowseEntryKind.Selectable:
                    return entry.Caption; // Just the filename
                case BrowseEntryKind.Root:
                    return entry.Caption; // Just the drive root
                default:
                    return entry.Caption;
            }
        }

        private void DrawFooter()
        {
            // Draw version info at bottom (like original engine)
            string versionInfo = "OHRRPGCE kaleidophone+1 20250810 Direct2D/sdl2";
            string helpText = "Press F1 for help on any menu!";
            
            graphicsSystem.DrawText(versionInfo, 4, screenHeight - 40, Color.LightGray, Graphics.TextAlignment.Left);
            graphicsSystem.DrawText(helpText, 4, screenHeight - 20, Color.LightGray, Graphics.TextAlignment.Left);

            // Draw selected file info if available
            var selectedEntry = fileBrowser.GetSelectedEntry();
            if (selectedEntry != null && !string.IsNullOrEmpty(selectedEntry.About))
            {
                graphicsSystem.DrawText(selectedEntry.About, 4, screenHeight - 60, Color.LightGray, Graphics.TextAlignment.Left);
            }
        }

        public void SetScreenDimensions(int width, int height)
        {
            screenWidth = width;
            screenHeight = height;
        }
    }
}
