using System;
using System.IO;

namespace OHRRPGCEDX
{
    /// <summary>
    /// Main entry point for the OHRRPGCE .NET port
    /// This replaces the separate custom.bas and game.bas entry points
    /// </summary>
    class Program
    {
        /// <summary>
        /// Main entry point
        /// </summary>
        /// <param name="args">Command line arguments</param>
        static void Main(string[] args)
        {
            try
            {
                Console.WriteLine("OHRRPGCE .NET Port v1.0.0");
                Console.WriteLine("Port from FreeBasic to .NET Framework 4.8");
                Console.WriteLine("Using SharpDX for Direct3D 11 graphics");
                Console.WriteLine();

                // Parse command line arguments
                var options = ParseCommandLine(args);
                
                if (options.ShowHelp)
                {
                    ShowUsage();
                    return;
                }

                if (options.ShowVersion)
                {
                    ShowVersion();
                    return;
                }

                // Determine mode based on arguments
                if (options.GameMode)
                {
                    // Run in Game mode (equivalent to game.bas)
                    Console.WriteLine("Starting in GAME mode...");
                    Game.RunGame(options.RPGPath);
                }
                else
                {
                    // Run in Custom mode (equivalent to custom.bas)
                    Console.WriteLine("Starting in CUSTOM mode...");
                    Custom.StartCustomEngine(options.RPGPath);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Fatal error: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
                Environment.Exit(1);
            }
        }

        /// <summary>
        /// Command line options
        /// </summary>
        private class CommandLineOptions
        {
            public bool ShowHelp { get; set; }
            public bool ShowVersion { get; set; }
            public bool GameMode { get; set; }
            public string RPGPath { get; set; }
            public bool DebugMode { get; set; }
        }

        /// <summary>
        /// Parse command line arguments
        /// </summary>
        private static CommandLineOptions ParseCommandLine(string[] args)
        {
            var options = new CommandLineOptions();

            for (int i = 0; i < args.Length; i++)
            {
                var arg = args[i].ToLower();

                switch (arg)
                {
                    case "-h":
                    case "--help":
                    case "/?":
                        options.ShowHelp = true;
                        break;

                    case "-v":
                    case "--version":
                        options.ShowVersion = true;
                        break;

                    case "-g":
                    case "--game":
                    case "/game":
                        options.GameMode = true;
                        break;

                    case "-c":
                    case "--custom":
                    case "/custom":
                        options.GameMode = false;
                        break;

                    case "-d":
                    case "--debug":
                        options.DebugMode = true;
                        break;

                    default:
                        // If it's not a flag and looks like a file path, it's the RPG file
                        if (!arg.StartsWith("-") && !arg.StartsWith("/") && !arg.StartsWith("--"))
                        {
                            options.RPGPath = args[i];
                        }
                        break;
                }
            }

            // If no RPG path specified, try to find one
            if (string.IsNullOrEmpty(options.RPGPath))
            {
                options.RPGPath = FindDefaultRPGFile();
            }

            return options;
        }

        /// <summary>
        /// Find a default RPG file in the current directory
        /// </summary>
        private static string FindDefaultRPGFile()
        {
            // Look for .rpg files in current directory
            var rpgFiles = Directory.GetFiles(".", "*.rpg");
            if (rpgFiles.Length > 0)
            {
                return rpgFiles[0];
            }

            // Look for RPG directories
            var rpgDirs = Directory.GetDirectories(".", "*rpg*");
            if (rpgDirs.Length > 0)
            {
                return rpgDirs[0];
            }

            return null;
        }

        /// <summary>
        /// Show usage information
        /// </summary>
        private static void ShowUsage()
        {
            Console.WriteLine("OHRRPGCE .NET Port");
            Console.WriteLine("Usage: OHRRPGCEDX.exe [options] [rpg_file]");
            Console.WriteLine();
            Console.WriteLine("Options:");
            Console.WriteLine("  -h, --help     Show this help message");
            Console.WriteLine("  -v, --version  Show version information");
            Console.WriteLine("  -g, --game     Force Game mode (runtime engine)");
            Console.WriteLine("  -c, --custom   Force Custom mode (editor engine)");
            Console.WriteLine("  -d, --debug    Enable debug mode");
            Console.WriteLine();
            Console.WriteLine("Modes:");
            Console.WriteLine("  Custom mode (default): Game editor and launcher");
            Console.WriteLine("  Game mode: Direct game runtime");
            Console.WriteLine();
            Console.WriteLine("Examples:");
            Console.WriteLine("  OHRRPGCEDX.exe                    # Custom mode, auto-find RPG");
            Console.WriteLine("  OHRRPGCEDX.exe game.rpg          # Custom mode with specific RPG");
            Console.WriteLine("  OHRRPGCEDX.exe -g game.rpg       # Game mode with specific RPG");
            Console.WriteLine("  OHRRPGCEDX.exe -c                # Force Custom mode");
        }

        /// <summary>
        /// Show version information
        /// </summary>
        private static void ShowVersion()
        {
            Console.WriteLine("OHRRPGCE .NET Port v1.0.0");
            Console.WriteLine("Port from FreeBasic to .NET Framework 4.8");
            Console.WriteLine("Using SharpDX for Direct3D 11 graphics");
            Console.WriteLine("Built on " + DateTime.Now.ToString("yyyy-MM-dd"));
            Console.WriteLine();
            Console.WriteLine("Original OHRRPGCE Engine:");
            Console.WriteLine("  RPG Version: " + Constants.CURRENT_RPG_VERSION);
            Console.WriteLine("  RGFX Version: " + Constants.CURRENT_RGFX_VERSION);
            Console.WriteLine("  RSAV Version: " + Constants.CURRENT_RSAV_VERSION);
        }
    }
}
