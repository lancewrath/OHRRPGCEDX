using System;
using System.Collections.Generic;
using System.IO;

namespace OHRRPGCEDX
{
    /// <summary>
    /// Processes command line arguments for the Custom editor
    /// </summary>
    public static class CommandLineProcessor
    {
        /// <summary>
        /// Parses command line arguments and returns options
        /// </summary>
        public static CommandLineOptions ParseArguments(string[] args)
        {
            CommandLineOptions options = new CommandLineOptions();
            
            for (int i = 0; i < args.Length; i++)
            {
                string arg = args[i].ToLower();
                
                switch (arg)
                {
                    case "-h":
                    case "--help":
                    case "/?":
                        options.help_requested = true;
                        break;
                        
                    case "-d":
                    case "--distrib":
                        if (i + 1 < args.Length)
                        {
                            options.auto_distrib = args[++i];
                        }
                        break;
                        
                    case "--nowait":
                        options.option_nowait = true;
                        break;
                        
                    case "--hsflags":
                        if (i + 1 < args.Length)
                        {
                            options.option_hsflags = args[++i];
                        }
                        break;
                        
                    case "--export-translations":
                        if (i + 1 < args.Length)
                        {
                            options.export_translations_to = args[++i];
                        }
                        break;
                        
                    case "--import-scripts":
                        if (i + 1 < args.Length)
                        {
                            options.import_scripts_from = args[++i];
                        }
                        break;
                        
                    default:
                        // Check if it's a file path
                        if (arg.EndsWith(Constants.RPG_EXTENSION))
                        {
                            // This is an RPG file to open
                            if (string.IsNullOrEmpty(options.import_scripts_from))
                            {
                                // If no import scripts specified, treat as RPG file to open
                                options.import_scripts_from = args[i];
                            }
                        }
                        else if (!arg.StartsWith("-"))
                        {
                            // Unknown argument
                            Console.WriteLine($"Warning: Unknown argument: {args[i]}");
                        }
                        break;
                }
            }
            
            return options;
        }

        /// <summary>
        /// Displays help information
        /// </summary>
        public static void ShowHelp()
        {
            Console.WriteLine("OHRRPGCE Custom Editor - C# Port");
            Console.WriteLine("Usage: OHRRPGCEDX [options] [rpgfile]");
            Console.WriteLine();
            Console.WriteLine("Options:");
            Console.WriteLine("  -h, --help                    Show this help message");
            Console.WriteLine("  -d, --distrib <option>        Package distribution automatically");
            Console.WriteLine("  --nowait                      Don't wait when importing scripts");
            Console.WriteLine("  --hsflags <flags>             Extra flags for HamsterSpeak compiler");
            Console.WriteLine("  --export-translations <file>  Export translations to file");
            Console.WriteLine("  --import-scripts <file>       Import scripts from file");
            Console.WriteLine();
            Console.WriteLine("Examples:");
            Console.WriteLine("  OHRRPGCEDX mygame.rpg");
            Console.WriteLine("  OHRRPGCEDX --distrib windows mygame.rpg");
            Console.WriteLine("  OHRRPGCEDX --import-scripts scripts.txt --nowait");
        }

        /// <summary>
        /// Validates command line options
        /// </summary>
        public static bool ValidateOptions(CommandLineOptions options)
        {
            // Check if help was requested
            if (options.help_requested)
            {
                ShowHelp();
                return false;
            }

            // Validate distribution option
            if (!string.IsNullOrEmpty(options.auto_distrib))
            {
                string[] validDistribs = { "windows", "linux", "mac", "android", "web" };
                if (Array.IndexOf(validDistribs, options.auto_distrib.ToLower()) == -1)
                {
                    Console.WriteLine($"Error: Invalid distribution option: {options.auto_distrib}");
                    Console.WriteLine($"Valid options: {string.Join(", ", validDistribs)}");
                    return false;
                }
            }

            // Validate import scripts file
            if (!string.IsNullOrEmpty(options.import_scripts_from))
            {
                if (!File.Exists(options.import_scripts_from))
                {
                    Console.WriteLine($"Error: Import scripts file not found: {options.import_scripts_from}");
                    return false;
                }
            }

            // Validate export translations file
            if (!string.IsNullOrEmpty(options.export_translations_to))
            {
                string dir = Path.GetDirectoryName(options.export_translations_to);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                {
                    try
                    {
                        Directory.CreateDirectory(dir);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error: Cannot create export directory: {ex.Message}");
                        return false;
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// Processes command line options and returns true if should continue
        /// </summary>
        public static bool ProcessOptions(CommandLineOptions options)
        {
            // Handle import scripts
            if (!string.IsNullOrEmpty(options.import_scripts_from))
            {
                Console.WriteLine($"Importing scripts from: {options.import_scripts_from}");
                // TODO: Implement script importing
                if (options.option_nowait)
                {
                    Console.WriteLine("Import completed (--nowait specified)");
                    return false; // Exit after import
                }
                else
                {
                    Console.WriteLine("Press any key to continue...");
                    Console.ReadKey();
                }
            }

            // Handle export translations
            if (!string.IsNullOrEmpty(options.export_translations_to))
            {
                Console.WriteLine($"Exporting translations to: {options.export_translations_to}");
                // TODO: Implement translation export
                return false; // Exit after export
            }

            // Handle auto distribution
            if (!string.IsNullOrEmpty(options.auto_distrib))
            {
                Console.WriteLine($"Auto-distribution enabled for: {options.auto_distrib}");
                // TODO: Implement auto-distribution
            }

            return true; // Continue with normal operation
        }
    }
}
