using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Drawing;

namespace OHRRPGCEDX.UI
{
    public enum BrowseEntryKind
    {
        Drive = 0,           // Windows only
        ParentDir = 1,       // Parent directory
        SubDir = 2,          // Subdirectory
        Selectable = 3,      // Selectable file
        Root = 4,            // Root of current drive
        Special = 5,         // Not used
        Unselectable = 6     // Disabled
    }

    public class BrowseMenuEntry
    {
        public BrowseEntryKind Kind { get; set; }
        public string Filename { get; set; }         // Actual filename
        public string Caption { get; set; }          // How the entry is shown
        public string About { get; set; }            // Description to show at bottom when selected
        public string FullPath { get; set; }         // Full path for the entry
    }

    public class FileBrowser
    {
        private List<BrowseMenuEntry> entries;
        private int selectedIndex;
        private string currentDirectory;
        private string selectedFile;
        private bool showHidden;
        private string fileMask;
        private BrowseFileType fileType;

        public enum BrowseFileType
        {
            Any,
            RPG,
            Music,
            Sfx,
            Image,
            Tilemap,
            Scripts,
            Reload
        }

        public FileBrowser()
        {
            entries = new List<BrowseMenuEntry>();
            selectedIndex = 0;
            currentDirectory = Environment.CurrentDirectory;
            showHidden = false;
            fileType = BrowseFileType.Any;
        }

        public void Initialize(BrowseFileType type, string defaultPath = "", string mask = "")
        {
            fileType = type;
            fileMask = mask;
            
            if (!string.IsNullOrEmpty(defaultPath))
            {
                if (Directory.Exists(defaultPath))
                {
                    currentDirectory = defaultPath;
                }
                else if (File.Exists(defaultPath))
                {
                    currentDirectory = Path.GetDirectoryName(defaultPath);
                    selectedFile = Path.GetFileName(defaultPath);
                }
            }

            BuildListing();
        }

        public void BuildListing()
        {
            var startTime = DateTime.Now;
            entries.Clear();
            selectedIndex = 0;

            // Add drives (Windows only)
            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            {
                foreach (DriveInfo drive in DriveInfo.GetDrives())
                {
                    if (drive.IsReady)
                    {
                        entries.Add(new BrowseMenuEntry
                        {
                            Kind = BrowseEntryKind.Drive,
                            Filename = drive.Name,
                            Caption = $"{drive.Name} {drive.VolumeLabel}",
                            FullPath = drive.Name
                        });
                    }
                }
            }

            // Add current drive root
            if (!string.IsNullOrEmpty(currentDirectory))
            {
                string driveRoot = Path.GetPathRoot(currentDirectory);
                if (!string.IsNullOrEmpty(driveRoot))
                {
                    entries.Add(new BrowseMenuEntry
                    {
                        Kind = BrowseEntryKind.Root,
                        Filename = driveRoot,
                        Caption = driveRoot,
                        FullPath = driveRoot
                    });
                }

                // Add parent directories
                string[] pathParts = currentDirectory.Split(Path.DirectorySeparatorChar);
                string currentPath = "";
                for (int i = 0; i < pathParts.Length - 1; i++)
                {
                    if (!string.IsNullOrEmpty(pathParts[i]))
                    {
                        currentPath += pathParts[i] + Path.DirectorySeparatorChar;
                        entries.Add(new BrowseMenuEntry
                        {
                            Kind = BrowseEntryKind.ParentDir,
                            Filename = pathParts[i],
                            Caption = pathParts[i] + Path.DirectorySeparatorChar,
                            FullPath = currentPath
                        });
                    }
                }

                // Add subdirectories
                try
                {
                    var dirStartTime = DateTime.Now;
                    var directories = Directory.GetDirectories(currentDirectory)
                        .Where(d => !showHidden || !IsHidden(d))
                        .OrderBy(d => Path.GetFileName(d));

                    foreach (string dir in directories)
                    {
                        string dirName = Path.GetFileName(dir);
                        entries.Add(new BrowseMenuEntry
                        {
                            Kind = BrowseEntryKind.SubDir,
                            Filename = dirName,
                            Caption = dirName + Path.DirectorySeparatorChar,
                            FullPath = dir
                        });
                    }
                    var dirTime = DateTime.Now - dirStartTime;
                    if (dirTime.TotalMilliseconds > 100) // Log if directory scanning takes more than 100ms
                    {
                        Console.WriteLine($"Directory scanning took {dirTime.TotalMilliseconds}ms for {currentDirectory}");
                    }

                    // Add files based on file type
                    var fileStartTime = DateTime.Now;
                    var files = GetFilesByType(currentDirectory);
                    foreach (string file in files)
                    {
                        string fileName = Path.GetFileName(file);
                        entries.Add(new BrowseMenuEntry
                        {
                            Kind = BrowseEntryKind.Selectable,
                            Filename = fileName,
                            Caption = fileName,
                            FullPath = file
                        });
                    }
                    var fileTime = DateTime.Now - fileStartTime;
                    if (fileTime.TotalMilliseconds > 100) // Log if file scanning takes more than 100ms
                    {
                        Console.WriteLine($"File scanning took {fileTime.TotalMilliseconds}ms for {currentDirectory}");
                    }
                }
                catch (Exception ex)
                {
                    // Handle access denied or other errors
                    Console.WriteLine($"Error accessing directory {currentDirectory}: {ex.Message}");
                }
            }

            // Set initial selection
            if (!string.IsNullOrEmpty(selectedFile))
            {
                for (int i = 0; i < entries.Count; i++)
                {
                    if (entries[i].Filename == selectedFile)
                    {
                        selectedIndex = i;
                        break;
                    }
                }
            }

            var totalTime = DateTime.Now - startTime;
            if (totalTime.TotalMilliseconds > 50) // Log if total build takes more than 50ms
            {
                Console.WriteLine($"BuildListing took {totalTime.TotalMilliseconds}ms for {currentDirectory}, found {entries.Count} entries");
            }
        }

        private List<string> GetFilesByType(string directory)
        {
            var files = new List<string>();
            
            try
            {
                switch (fileType)
                {
                    case BrowseFileType.RPG:
                        files.AddRange(Directory.GetFiles(directory, "*.rpg"));
                        break;
                    case BrowseFileType.Music:
                        files.AddRange(Directory.GetFiles(directory, "*.mid"));
                        files.AddRange(Directory.GetFiles(directory, "*.xm"));
                        files.AddRange(Directory.GetFiles(directory, "*.it"));
                        files.AddRange(Directory.GetFiles(directory, "*.mod"));
                        files.AddRange(Directory.GetFiles(directory, "*.s3m"));
                        files.AddRange(Directory.GetFiles(directory, "*.ogg"));
                        files.AddRange(Directory.GetFiles(directory, "*.mp3"));
                        files.AddRange(Directory.GetFiles(directory, "*.wav"));
                        break;
                    case BrowseFileType.Sfx:
                        files.AddRange(Directory.GetFiles(directory, "*.wav"));
                        files.AddRange(Directory.GetFiles(directory, "*.ogg"));
                        files.AddRange(Directory.GetFiles(directory, "*.mp3"));
                        break;
                    case BrowseFileType.Image:
                        files.AddRange(Directory.GetFiles(directory, "*.bmp"));
                        files.AddRange(Directory.GetFiles(directory, "*.png"));
                        files.AddRange(Directory.GetFiles(directory, "*.jpg"));
                        files.AddRange(Directory.GetFiles(directory, "*.jpeg"));
                        break;
                    case BrowseFileType.Any:
                    default:
                        if (!string.IsNullOrEmpty(fileMask))
                        {
                            files.AddRange(Directory.GetFiles(directory, fileMask));
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting files from {directory}: {ex.Message}");
            }

            return files.OrderBy(f => Path.GetFileName(f)).ToList();
        }

        private bool IsHidden(string path)
        {
            try
            {
                FileAttributes attributes = File.GetAttributes(path);
                return (attributes & FileAttributes.Hidden) == FileAttributes.Hidden;
            }
            catch
            {
                return false;
            }
        }

        public void MoveUp()
        {
            if (selectedIndex > 0)
                selectedIndex--;
        }

        public void MoveDown()
        {
            if (selectedIndex < entries.Count - 1)
                selectedIndex++;
        }

        public string GetSelectedPath()
        {
            if (selectedIndex >= 0 && selectedIndex < entries.Count)
            {
                var entry = entries[selectedIndex];
                switch (entry.Kind)
                {
                    case BrowseEntryKind.Drive:
                        return entry.FullPath;
                    case BrowseEntryKind.ParentDir:
                        return entry.FullPath;
                    case BrowseEntryKind.SubDir:
                        return entry.FullPath;
                    case BrowseEntryKind.Selectable:
                        return entry.FullPath;
                    case BrowseEntryKind.Root:
                        return entry.FullPath;
                    default:
                        return "";
                }
            }
            return "";
        }

        public BrowseMenuEntry GetSelectedEntry()
        {
            if (selectedIndex >= 0 && selectedIndex < entries.Count)
                return entries[selectedIndex];
            return null;
        }

        public bool NavigateToSelected()
        {
            var entry = GetSelectedEntry();
            if (entry == null) return false;

            switch (entry.Kind)
            {
                case BrowseEntryKind.Drive:
                    currentDirectory = entry.FullPath;
                    BuildListing();
                    return false; // Don't exit browser
                    
                case BrowseEntryKind.ParentDir:
                    currentDirectory = entry.FullPath;
                    BuildListing();
                    return false; // Don't exit browser
                    
                case BrowseEntryKind.SubDir:
                    currentDirectory = entry.FullPath;
                    BuildListing();
                    return false; // Don't exit browser
                    
                case BrowseEntryKind.Selectable:
                    selectedFile = entry.Filename;
                    return true; // Exit browser with selected file
                    
                case BrowseEntryKind.Root:
                    currentDirectory = entry.FullPath;
                    BuildListing();
                    return false; // Don't exit browser
                    
                default:
                    return false;
            }
        }

        public void GoUpDirectory()
        {
            if (!string.IsNullOrEmpty(currentDirectory))
            {
                string parent = Directory.GetParent(currentDirectory)?.FullName;
                if (!string.IsNullOrEmpty(parent))
                {
                    currentDirectory = parent;
                    BuildListing();
                }
            }
        }

        public void Refresh()
        {
            BuildListing();
        }

        public List<BrowseMenuEntry> GetEntries()
        {
            return entries;
        }

        public int GetSelectedIndex()
        {
            return selectedIndex;
        }

        public string GetCurrentDirectory()
        {
            return currentDirectory;
        }

        public string GetSelectedFile()
        {
            return selectedFile;
        }
    }
}
