using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Linq;

namespace OHRRPGCEDX.GameData
{
    /// <summary>
    /// Loads and parses RPG files from the OHRRPGCE engine
    /// </summary>
    public class RPGFileLoader
    {
        private string rpgPath;
        private Dictionary<string, byte[]> lumps;
        private bool isLoaded;

        public string RPGPath => rpgPath;
        public bool IsLoaded => isLoaded;
        public int LumpCount => lumps?.Count ?? 0;

        public RPGFileLoader()
        {
            lumps = new Dictionary<string, byte[]>();
            isLoaded = false;
        }

        /// <summary>
        /// Load an RPG file or directory
        /// </summary>
        public bool LoadRPG(string path)
        {
            try
            {
                rpgPath = path;
                lumps.Clear();

                if (Directory.Exists(path))
                {
                    // Load from RPG directory (unlumped)
                    return LoadFromDirectory(path);
                }
                else if (File.Exists(path) && Path.GetExtension(path).ToLower() == ".rpg")
                {
                    // Load from RPG file (lumped)
                    return LoadFromFile(path);
                }
                else
                {
                    Console.WriteLine($"Invalid RPG path: {path}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to load RPG: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Load a project (alias for LoadRPG for consistency with editor)
        /// </summary>
        public bool LoadProject(string projectPath)
        {
            return LoadRPG(projectPath);
        }

        /// <summary>
        /// Load all game data and return a RPGData object
        /// </summary>
        public RPGData LoadGameData(string path)
        {
            if (!LoadRPG(path))
            {
                return null;
            }

            var gameData = new RPGData
            {
                General = LoadGeneralData(),
                Heroes = LoadHeroData(),
                Enemies = LoadEnemyData(),
                Maps = LoadMapData(),
                Items = LoadItemData(),
                Spells = LoadSpellData(),
                Scripts = LoadScriptData(),
                Textures = LoadTextureData(),
                Audio = LoadAudioData()
            };

            return gameData;
        }

        /// <summary>
        /// Load from unlumped RPG directory
        /// </summary>
        private bool LoadFromDirectory(string directoryPath)
        {
            try
            {
                var files = Directory.GetFiles(directoryPath, "*.*", SearchOption.AllDirectories);
                
                foreach (var file in files)
                {
                    var relativePath = GetRelativePath(directoryPath, file);
                    var lumpName = relativePath.Replace(Path.DirectorySeparatorChar, '/');
                    
                    var data = File.ReadAllBytes(file);
                    lumps[lumpName] = data;
                }

                isLoaded = true;
                Console.WriteLine($"Loaded {lumps.Count} lumps from directory: {directoryPath}");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to load from directory: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Load from lumped RPG file
        /// </summary>
        private bool LoadFromFile(string filePath)
        {
            try
            {
                using (var stream = File.OpenRead(filePath))
                using (var reader = new BinaryReader(stream))
                {
                    // Try to detect file format
                    var magic = reader.ReadBytes(4);
                    stream.Position = 0; // Reset position

                    if (Encoding.ASCII.GetString(magic) == "RPG!")
                    {
                        // Modern RPG format
                        return LoadModernRPGFormat(reader);
                    }
                    else
                    {
                        // Old engine lumped format
                        return LoadOldLumpedFormat(reader);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to load from file: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Load modern RPG format with "RPG!" header
        /// </summary>
        private bool LoadModernRPGFormat(BinaryReader reader)
        {
            try
            {
                // Read RPG header
                var magic = reader.ReadBytes(4);
                var version = reader.ReadInt32();
                var lumpCount = reader.ReadInt32();
                var headerSize = reader.ReadInt32();

                Console.WriteLine($"Modern RPG Version: {version}, Lumps: {lumpCount}");

                // Read lump directory
                for (int i = 0; i < lumpCount; i++)
                {
                    var lumpName = ReadFixedString(reader, 32);
                    var lumpOffset = reader.ReadInt32();
                    var lumpSize = reader.ReadInt32();
                    var lumpFlags = reader.ReadInt32();

                    // Store lump info for later loading
                    lumps[lumpName] = new byte[lumpSize];
                }

                // Read lump data
                foreach (var lump in lumps)
                {
                    var lumpName = lump.Key;
                    var lumpData = lump.Value;
                    
                    // Find lump info in directory
                    var stream = reader.BaseStream;
                    stream.Position = headerSize;
                    for (int i = 0; i < lumpCount; i++)
                    {
                        var name = ReadFixedString(reader, 32);
                        var offset = reader.ReadInt32();
                        var size = reader.ReadInt32();
                        var flags = reader.ReadInt32();

                        if (name == lumpName)
                        {
                            // Read lump data
                            var currentPos = stream.Position;
                            stream.Position = offset;
                            var data = reader.ReadBytes(size);
                            lumps[lumpName] = data;
                            stream.Position = currentPos;
                            break;
                        }
                    }
                }

                isLoaded = true;
                Console.WriteLine($"Loaded {lumps.Count} lumps from modern RPG file");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to load modern RPG format: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Load old engine lumped format (like the original OHRRPGCE)
        /// </summary>
        private bool LoadOldLumpedFormat(BinaryReader reader)
        {
            try
            {
                Console.WriteLine("Loading old engine lumped format...");
                
                var stream = reader.BaseStream;
                var lumpCount = 0;

                while (stream.Position < stream.Length)
                {
                    // Read lump name (null-terminated string)
                    var lumpName = "";
                    var byteValue = reader.ReadByte();
                    
                    while (byteValue != 0 && stream.Position < stream.Length)
                    {
                        lumpName += (char)byteValue;
                        byteValue = reader.ReadByte();
                    }

                    if (string.IsNullOrEmpty(lumpName))
                        break; // End of lumps

                    // Read lump size (4 bytes, PDP-endian byte order = 3,4,1,2)
                    var byte1 = reader.ReadByte(); // Byte 1 -> bits 16-23
                    var byte2 = reader.ReadByte(); // Byte 2 -> bits 24-31
                    var byte3 = reader.ReadByte(); // Byte 3 -> bits 0-7
                    var byte4 = reader.ReadByte(); // Byte 4 -> bits 8-15
                    
                    var lumpSize = (byte1 << 16) | (byte2 << 24) | byte3 | (byte4 << 8);

                    if (lumpSize < 0 || lumpSize > stream.Length || stream.Position + lumpSize > stream.Length)
                    {
                        Console.WriteLine($"Invalid lump size: {lumpSize} for lump: {lumpName}");
                        break;
                    }

                    // Read lump data
                    var lumpData = reader.ReadBytes(lumpSize);
                    lumps[lumpName] = lumpData;
                    lumpCount++;

                    Console.WriteLine($"Loaded lump: {lumpName} ({lumpSize} bytes)");
                }

                isLoaded = true;
                Console.WriteLine($"Loaded {lumpCount} lumps from old lumped format");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to load old lumped format: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Read a fixed-length string from binary reader
        /// </summary>
        private string ReadFixedString(BinaryReader reader, int length)
        {
            var bytes = reader.ReadBytes(length);
            var nullIndex = Array.IndexOf(bytes, (byte)0);
            if (nullIndex >= 0)
                length = nullIndex;
            
            return Encoding.ASCII.GetString(bytes, 0, length);
        }

        /// <summary>
        /// Gets the relative path from base path to target path
        /// </summary>
        private string GetRelativePath(string basePath, string targetPath)
        {
            var baseUri = new Uri(basePath);
            var targetUri = new Uri(targetPath);
            var relativeUri = baseUri.MakeRelativeUri(targetUri);
            return Uri.UnescapeDataString(relativeUri.ToString());
        }

        /// <summary>
        /// Get a lump by name
        /// </summary>
        public byte[] GetLump(string lumpName)
        {
            if (!isLoaded || !lumps.ContainsKey(lumpName))
                return null;

            return lumps[lumpName];
        }

        /// <summary>
        /// Get the project name from the RPG file or directory
        /// </summary>
        private string GetProjectName()
        {
            if (string.IsNullOrEmpty(rpgPath))
                return "GAME";
                
            var fileName = Path.GetFileNameWithoutExtension(rpgPath);
            if (string.IsNullOrEmpty(fileName))
                fileName = Path.GetFileName(rpgPath);
                
            // For old engine format, the project name is usually the filename without extension
            // But some games use "GAME" as the default project name
            // Special case: vikings.rpg uses "VIKING" (singular) not "VIKINGS" (plural)
            var projectName = fileName.ToUpper();
            if (projectName == "VIKINGS")
            {
                projectName = "VIKING";
            }
            
            OHRRPGCEDX.Utils.LoggingSystem.Instance.Info("Project Detection", $"GetProjectName: Detected project name: '{projectName}' from path: '{rpgPath}'");
            
            return projectName;
        }

        /// <summary>
        /// Get a lump as text
        /// </summary>
        public string GetLumpAsText(string lumpName)
        {
            var data = GetLump(lumpName);
            if (data == null) return null;

            return Encoding.UTF8.GetString(data);
        }

        /// <summary>
        /// Check if a lump exists
        /// </summary>
        public bool HasLump(string lumpName)
        {
            return isLoaded && lumps.ContainsKey(lumpName);
        }

        /// <summary>
        /// Get all lump names
        /// </summary>
        public IEnumerable<string> GetLumpNames()
        {
            if (!isLoaded) return new string[0];
            return lumps.Keys;
        }

        /// <summary>
        /// Get lump size
        /// </summary>
        public int GetLumpSize(string lumpName)
        {
            if (!isLoaded || !lumps.ContainsKey(lumpName))
                return 0;

            return lumps[lumpName].Length;
        }

        /// <summary>
        /// Load general data (.GEN lump)
        /// </summary>
        public GeneralData LoadGeneralData()
        {
            // Try to find the project name from loaded lumps
            var projectName = GetProjectName();
            
            var data = GetLump("general.reld");
            if (data == null)
            {
                // Try old format with project name
                data = GetLump(projectName + ".GEN");
            }
            if (data == null)
            {
                // Try generic old format
                data = GetLump(".GEN");
            }

            if (data == null) return null;

            try
            {
                var general = new GeneralData();
                
                // Parse RELD format or old binary format
                if (data.Length > 4 && Encoding.ASCII.GetString(data, 0, 4) == "RELD")
                {
                    ParseRelDFormat(data, general);
                }
                else
                {
                    ParseOldBinaryFormat(data, general);
                }

                return general;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to parse general data: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Parse RELD format data
        /// </summary>
        private void ParseRelDFormat(byte[] data, GeneralData general)
        {
            try
            {
                using (var stream = new MemoryStream(data))
                using (var reader = new BinaryReader(stream))
                {
                    // Skip RELD header
                    stream.Position = 4;
                    
                    // Read version
                    var version = reader.ReadInt32();
                    
                    // Read data blocks
                    while (stream.Position < stream.Length)
                    {
                        var blockType = ReadFixedString(reader, 4);
                        var blockSize = reader.ReadInt32();
                        
                        switch (blockType)
                        {
                            case "TITL": // Title
                                general.Title = ReadFixedString(reader, blockSize);
                                break;
                            case "AUTH": // Author
                                general.Author = ReadFixedString(reader, blockSize);
                                break;
                            case "STMP": // Starting Map
                                general.StartingMap = reader.ReadInt32();
                                break;
                            case "STX ": // Starting X
                                general.StartingX = reader.ReadInt32();
                                break;
                            case "STY ": // Starting Y
                                general.StartingY = reader.ReadInt32();
                                break;
                            case "STGL": // Starting Gold
                                general.StartingGold = reader.ReadInt32();
                                break;
                            case "STHR": // Starting Heroes
                                var heroCount = blockSize / 4;
                                general.StartingHeroes = new int[heroCount];
                                for (int i = 0; i < heroCount; i++)
                                {
                                    general.StartingHeroes[i] = reader.ReadInt32();
                                }
                                break;
                            case "STIT": // Starting Items
                                var itemCount = blockSize / 4;
                                general.StartingItems = new int[itemCount];
                                for (int i = 0; i < itemCount; i++)
                                {
                                    general.StartingItems[i] = reader.ReadInt32();
                                }
                                break;
                            default:
                                // Skip unknown blocks
                                stream.Position += blockSize;
                                break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error parsing RELD format: {ex.Message}");
            }
        }

        /// <summary>
        /// Parse old binary format data
        /// </summary>
        private void ParseOldBinaryFormat(byte[] data, GeneralData general)
        {
            try
            {
                using (var stream = new MemoryStream(data))
                using (var reader = new BinaryReader(stream))
                {
                    // Old format has fixed offsets
                    // Based on oldengine/loading.rbas, most values are 16-bit
                    stream.Position = Constants.genTitle;
                    general.Title = ReadFixedString(reader, 32);
                    
                    stream.Position = Constants.genTitleMus;
                    general.TitleMusic = reader.ReadInt16();
                    
                    stream.Position = Constants.genVictMus;
                    general.VictoryMusic = reader.ReadInt16();
                    
                    stream.Position = Constants.genBatMus;
                    general.BattleMusic = reader.ReadInt16();
                    
                    stream.Position = Constants.genMaxHero;
                    general.MaxHero = reader.ReadInt16();
                    
                    stream.Position = Constants.genMaxEnemy;
                    general.MaxEnemy = reader.ReadInt16();
                    
                    stream.Position = Constants.genMaxMap;
                    general.MaxMap = reader.ReadInt16();
                    
                    stream.Position = Constants.genMaxAttack;
                    general.MaxAttack = reader.ReadInt16();
                    
                    stream.Position = Constants.genMaxTile;
                    general.MaxTile = reader.ReadInt16();
                    
                    stream.Position = Constants.genMaxFormation;
                    general.MaxFormation = reader.ReadInt16();
                    
                    stream.Position = Constants.genMaxPal;
                    general.MaxPalette = reader.ReadInt16();
                    
                    stream.Position = Constants.genMaxTextbox;
                    general.MaxTextbox = reader.ReadInt16();
                    
                    stream.Position = Constants.genNumPlotscripts;
                    general.NumPlotScripts = reader.ReadInt16();
                    
                    stream.Position = Constants.genNewGameScript;
                    general.NewGameScript = reader.ReadInt16();
                    
                    Console.WriteLine($"General data: Title='{general.Title}', MaxHero={general.MaxHero}, MaxMap={general.MaxMap}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error parsing old binary format: {ex.Message}");
            }
        }

        /// <summary>
        /// Load hero data
        /// </summary>
        public HeroData[] LoadHeroData()
        {
            // Try to find the project name from loaded lumps
            var projectName = GetProjectName();
            Console.WriteLine($"Loading hero data, project name: {projectName}");
            
            var data = GetLump("heroes.reld");
            if (data == null)
            {
                Console.WriteLine("heroes.reld not found, trying old format...");
                // Try old format with project name
                data = GetLump(projectName + ".HSP");
            }
            if (data == null)
            {
                Console.WriteLine($"{projectName}.HSP not found, trying generic old format...");
                // Try generic old format
                data = GetLump(".DT2");
            }
            if (data == null)
            {
                Console.WriteLine(".DT2 not found, trying DT0...");
                // Try DT0 (hero data in old format)
                data = GetLump(projectName + ".DT0");
            }

            if (data == null)
            {
                Console.WriteLine("No hero data found in any format");
                return null;
            }

            Console.WriteLine($"Found hero data lump, size: {data.Length} bytes");

            try
            {
                var heroes = new List<HeroData>();
                
                if (data.Length > 4 && Encoding.ASCII.GetString(data, 0, 4) == "RELD")
                {
                    Console.WriteLine("Parsing hero data as RELD format");
                    ParseHeroRelDFormat(data, heroes);
                }
                else
                {
                    Console.WriteLine("Parsing hero data as old binary format");
                    ParseHeroOldBinaryFormat(data, heroes);
                }

                Console.WriteLine($"Hero parsing complete, found {heroes.Count} heroes");
                return heroes.ToArray();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to parse hero data: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Parse hero data in RELD format
        /// </summary>
        private void ParseHeroRelDFormat(byte[] data, List<HeroData> heroes)
        {
            using (var stream = new MemoryStream(data))
            using (var reader = new BinaryReader(stream))
            {
                // Skip RELD header
                stream.Position = 4;
                
                // Read version
                var version = reader.ReadInt32();
                
                // Read hero data blocks
                while (stream.Position < stream.Length)
                {
                    var blockType = ReadFixedString(reader, 4);
                    var blockSize = reader.ReadInt32();
                    
                    if (blockType == "HERO")
                    {
                        var hero = new HeroData();
                        
                        // Read hero name
                        var nameLength = reader.ReadInt32();
                        hero.Name = ReadFixedString(reader, nameLength);
                        
                        // Read hero stats
                        hero.Picture = reader.ReadInt32();
                        hero.Palette = reader.ReadInt32();
                        hero.Portrait = reader.ReadInt32();
                        hero.PortraitPalette = reader.ReadInt32();
                        
                        // Read base stats
                        hero.BaseStats = new Stats
                        {
                            HP = reader.ReadInt32(),
                            MP = reader.ReadInt32(),
                            Attack = reader.ReadInt32(),
                            Defense = reader.ReadInt32(),
                            Speed = reader.ReadInt32(),
                            Magic = reader.ReadInt32(),
                            MagicDef = reader.ReadInt32(),
                            Luck = reader.ReadInt32()
                        };
                        
                        // Read level MP data
                        var mpLevels = reader.ReadInt32();
                        hero.LevelMP = new int[mpLevels];
                        for (int i = 0; i < mpLevels; i++)
                        {
                            hero.LevelMP[i] = reader.ReadInt32();
                        }
                        
                        // Read elemental resistances
                        var elementCount = reader.ReadInt32();
                        hero.Elementals = new float[elementCount];
                        for (int i = 0; i < elementCount; i++)
                        {
                            hero.Elementals[i] = reader.ReadSingle();
                        }
                        
                        // Read hand positions
                        var handPosCount = reader.ReadInt32();
                        hero.HandPositions = new XYPair[handPosCount];
                        for (int i = 0; i < handPosCount; i++)
                        {
                            hero.HandPositions[i] = new XYPair(reader.ReadInt32(), reader.ReadInt32());
                        }
                        
                        heroes.Add(hero);
                    }
                    else
                    {
                        // Skip unknown blocks
                        stream.Position += blockSize;
                    }
                }
            }
        }

        /// <summary>
        /// Parse hero data in old binary format
        /// </summary>
        private void ParseHeroOldBinaryFormat(byte[] data, List<HeroData> heroes)
        {
            using (var stream = new MemoryStream(data))
            using (var reader = new BinaryReader(stream))
            {
                // Old format has fixed hero records
                // Based on oldengine/loading.rbas, each hero record is approximately 256 bytes
                var heroSize = 256; // Approximate size per hero
                var heroCount = data.Length / heroSize;
                
                Console.WriteLine($"Parsing {heroCount} heroes from old binary format (data size: {data.Length})");
                
                for (int i = 0; i < heroCount; i++)
                {
                    var hero = new HeroData();
                    
                    // Read hero name (16 bytes, variable length string)
                    hero.Name = ReadFixedString(reader, 16).Trim('\0');
                    
                    // Read sprite and palette (16-bit values)
                    hero.Picture = reader.ReadInt16();
                    hero.Palette = reader.ReadInt16();
                    
                    // Read walk sprite and palette (16-bit values)
                    var walkSprite = reader.ReadInt16();
                    var walkSpritePal = reader.ReadInt16();
                    
                    // Read default level and weapon (16-bit values)
                    var defLevel = reader.ReadInt16();
                    var defWeapon = reader.ReadInt16();
                    
                    // Read base stats (16-bit values)
                    hero.BaseStats = new Stats
                    {
                        HP = reader.ReadInt16(),
                        MP = reader.ReadInt16(),
                        Attack = reader.ReadInt16(),
                        Defense = reader.ReadInt16(),
                        Speed = reader.ReadInt16(),
                        Magic = reader.ReadInt16(),
                        MagicDef = reader.ReadInt16(),
                        Luck = reader.ReadInt16()
                    };
                    
                    // Skip spell lists (4 * 24 * 2 = 192 bytes)
                    stream.Position += 192;
                    
                    // Read portrait and palette (16-bit values)
                    hero.Portrait = reader.ReadInt16();
                    var portraitPal = reader.ReadInt16();
                    
                    // Skip bits and list names (3 * 2 + 4 * 10 = 46 bytes)
                    stream.Position += 46;
                    
                    // Read portrait palette (16-bit value)
                    var portraitPal2 = reader.ReadInt16();
                    
                    // Skip list types (4 * 2 = 8 bytes)
                    stream.Position += 8;
                    
                    // Read tags (16-bit values)
                    var haveTag = reader.ReadInt16();
                    var aliveTag = reader.ReadInt16();
                    var leaderTag = reader.ReadInt16();
                    var activeTag = reader.ReadInt16();
                    var maxNameLen = reader.ReadInt16();
                    
                    // Read hand positions (2 * 2 * 2 = 8 bytes)
                    hero.HandPositions = new XYPair[2];
                    for (int j = 0; j < 2; j++)
                    {
                        hero.HandPositions[j] = new XYPair(reader.ReadInt16(), reader.ReadInt16());
                    }
                    
                    // Read elemental resistances (float values)
                    hero.Elementals = new float[Constants.maxElements - 1];
                    for (int j = 0; j < Constants.maxElements - 1; j++)
                    {
                        hero.Elementals[j] = reader.ReadSingle();
                    }
                    
                    if (!string.IsNullOrEmpty(hero.Name))
                    {
                        Console.WriteLine($"  Hero {i}: {hero.Name}, HP: {hero.BaseStats.HP}, MP: {hero.BaseStats.MP}");
                        heroes.Add(hero);
                    }
                }
                
                Console.WriteLine($"Successfully parsed {heroes.Count} heroes");
            }
        }

        /// <summary>
        /// Load enemy data
        /// </summary>
        public EnemyData[] LoadEnemyData()
        {
            var data = GetLump("enemies.reld");
            if (data == null)
            {
                // Try old format
                data = GetLump(".DT5");
            }

            if (data == null) return null;

            try
            {
                var enemies = new List<EnemyData>();
                
                if (data.Length > 4 && Encoding.ASCII.GetString(data, 0, 4) == "RELD")
                {
                    ParseEnemyRelDFormat(data, enemies);
                }
                else
                {
                    ParseEnemyOldBinaryFormat(data, enemies);
                }

                return enemies.ToArray();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to parse enemy data: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Parse enemy data in RELD format
        /// </summary>
        private void ParseEnemyRelDFormat(byte[] data, List<EnemyData> enemies)
        {
            using (var stream = new MemoryStream(data))
            using (var reader = new BinaryReader(stream))
            {
                // Skip RELD header
                stream.Position = 4;
                
                // Read version
                var version = reader.ReadInt32();
                
                // Read enemy data blocks
                while (stream.Position < stream.Length)
                {
                    var blockType = ReadFixedString(reader, 4);
                    var blockSize = reader.ReadInt32();
                    
                    if (blockType == "ENEM")
                    {
                        var enemy = new EnemyData();
                        
                        // Read enemy name
                        var nameLength = reader.ReadInt32();
                        enemy.Name = ReadFixedString(reader, nameLength);
                        
                        // Read enemy properties
                        enemy.Picture = reader.ReadInt32();
                        enemy.Palette = reader.ReadInt32();
                        enemy.DeathPicture = reader.ReadInt32();
                        enemy.DeathPalette = reader.ReadInt32();
                        
                        // Read enemy stats
                        enemy.BaseStats = new Stats
                        {
                            HP = reader.ReadInt32(),
                            MP = reader.ReadInt32(),
                            Attack = reader.ReadInt32(),
                            Defense = reader.ReadInt32(),
                            Speed = reader.ReadInt32(),
                            Magic = reader.ReadInt32(),
                            MagicDef = reader.ReadInt32(),
                            Luck = reader.ReadInt32()
                        };
                        
                        // Read enemy behavior
                        enemy.Behavior = (EnemyBehavior)reader.ReadInt32();
                        enemy.Aggression = reader.ReadInt32();
                        enemy.Intelligence = reader.ReadInt32();
                        
                        // Read enemy drops
                        enemy.ExpReward = reader.ReadInt32();
                        enemy.GoldReward = reader.ReadInt32();
                        enemy.ItemDrop = reader.ReadInt32();
                        enemy.ItemDropChance = reader.ReadSingle();
                        
                        // Read enemy elemental properties
                        var elementCount = reader.ReadInt32();
                        enemy.Elementals = new float[elementCount];
                        for (int i = 0; i < elementCount; i++)
                        {
                            enemy.Elementals[i] = reader.ReadSingle();
                        }
                        
                        // Read enemy attacks (store as attack IDs)
                        var attackCount = reader.ReadInt32();
                        enemy.Attacks = new int[attackCount];
                        for (int i = 0; i < attackCount; i++)
                        {
                            // Read attack data but store only the ID
                            var attackType = reader.ReadInt32();
                            var power = reader.ReadInt32();
                            var accuracy = reader.ReadInt32();
                            var element = reader.ReadInt32();
                            var effect = reader.ReadInt32();
                            
                            // For now, just store the attack index as ID
                            // In a full implementation, this would create an AttackData object
                            // and store its ID in the Attacks array
                            enemy.Attacks[i] = i; // Temporary: store index as ID
                        }
                        
                        enemies.Add(enemy);
                    }
                    else
                    {
                        // Skip unknown blocks
                        stream.Position += blockSize;
                    }
                }
            }
        }

        /// <summary>
        /// Parse enemy data in old binary format
        /// </summary>
        private void ParseEnemyOldBinaryFormat(byte[] data, List<EnemyData> enemies)
        {
            using (var stream = new MemoryStream(data))
            using (var reader = new BinaryReader(stream))
            {
                // Old format has fixed enemy records
                var enemySize = 160; // Approximate size per enemy
                var enemyCount = data.Length / enemySize;
                
                for (int i = 0; i < enemyCount; i++)
                {
                    var enemy = new EnemyData();
                    
                    // Read enemy name (32 bytes)
                    enemy.Name = ReadFixedString(reader, 32).Trim('\0');
                    
                    // Read enemy properties
                    enemy.Picture = reader.ReadInt32();
                    enemy.Palette = reader.ReadInt32();
                    enemy.DeathPicture = reader.ReadInt32();
                    enemy.DeathPalette = reader.ReadInt32();
                    
                    // Read enemy stats
                    enemy.BaseStats = new Stats
                    {
                        HP = reader.ReadInt32(),
                        MP = reader.ReadInt32(),
                        Attack = reader.ReadInt32(),
                        Defense = reader.ReadInt32(),
                        Speed = reader.ReadInt32(),
                        Magic = reader.ReadInt32(),
                        MagicDef = reader.ReadInt32(),
                        Luck = reader.ReadInt32()
                    };
                    
                    // Read enemy behavior
                    enemy.Behavior = (EnemyBehavior)reader.ReadInt32();
                    enemy.Aggression = reader.ReadInt32();
                    enemy.Intelligence = reader.ReadInt32();
                    
                    // Read enemy drops
                    enemy.ExpReward = reader.ReadInt32();
                    enemy.GoldReward = reader.ReadInt32();
                    enemy.ItemDrop = reader.ReadInt32();
                    enemy.ItemDropChance = reader.ReadSingle();
                    
                    // Initialize elemental properties
                    enemy.Elementals = new float[Constants.maxElements - 1];
                    for (int j = 0; j < Constants.maxElements - 1; j++)
                    {
                        enemy.Elementals[j] = reader.ReadSingle();
                    }
                    
                    // Initialize attacks (default to 4 attacks)
                    enemy.Attacks = new int[4];
                    for (int j = 0; j < 4; j++)
                    {
                        // Read attack data but store only the ID
                        var attackType = reader.ReadInt32();
                        var power = reader.ReadInt32();
                        var accuracy = reader.ReadInt32();
                        var element = reader.ReadInt32();
                        var effect = reader.ReadInt32();
                        
                        // For now, just store the attack index as ID
                        // In a full implementation, this would create an AttackData object
                        // and store its ID in the Attacks array
                        enemy.Attacks[j] = j; // Temporary: store index as ID
                    }
                    
                    if (!string.IsNullOrEmpty(enemy.Name))
                    {
                        enemies.Add(enemy);
                    }
                }
            }
        }

        /// <summary>
        /// Load map data
        /// </summary>
        public MapData[] LoadMapData()
        {
            // Try to find the project name from loaded lumps
            var projectName = GetProjectName();
            
            OHRRPGCEDX.Utils.LoggingSystem.Instance.Info("Map Loading", $"LoadMapData: Looking for map data with project name: {projectName}");
            
            var data = GetLump("maps.reld");
            if (data == null)
            {
                OHRRPGCEDX.Utils.LoggingSystem.Instance.Info("Map Loading", "LoadMapData: maps.reld not found, trying old format");
                // Try old format with project name
                data = GetLump(projectName + ".MAP");
            }
            if (data == null)
            {
                OHRRPGCEDX.Utils.LoggingSystem.Instance.Info("Map Loading", "LoadMapData: Project MAP not found, trying generic old format");
                // Try generic old format
                data = GetLump(".DT6");
            }

            if (data == null)
            {
                OHRRPGCEDX.Utils.LoggingSystem.Instance.Warning("Map Loading", "LoadMapData: No map data found in any format");
                return null;
            }

            OHRRPGCEDX.Utils.LoggingSystem.Instance.Info("Map Loading", $"LoadMapData: Found map data, size: {data.Length} bytes");

            try
            {
                var maps = new List<MapData>();
                
                if (data.Length > 4 && Encoding.ASCII.GetString(data, 0, 4) == "RELD")
                {
                    OHRRPGCEDX.Utils.LoggingSystem.Instance.Info("Map Loading", "LoadMapData: Detected RELD format, parsing...");
                    ParseMapRelDFormat(data, maps);
                }
                else
                {
                    OHRRPGCEDX.Utils.LoggingSystem.Instance.Info("Map Loading", "LoadMapData: Detected old binary format, parsing...");
                    ParseMapOldBinaryFormat(data, maps);
                }

                OHRRPGCEDX.Utils.LoggingSystem.Instance.Info("Map Loading", $"LoadMapData: Successfully parsed {maps.Count} maps");
                return maps.ToArray();
            }
            catch (Exception ex)
            {
                OHRRPGCEDX.Utils.LoggingSystem.Instance.Error("Map Loading", $"LoadMapData: Failed to parse map data: {ex.Message}", null, ex);
                return null;
            }
        }

        /// <summary>
        /// Parse map data in RELD format
        /// </summary>
        private void ParseMapRelDFormat(byte[] data, List<MapData> maps)
        {
            using (var stream = new MemoryStream(data))
            using (var reader = new BinaryReader(stream))
            {
                // Skip RELD header
                stream.Position = 4;
                
                // Read version
                var version = reader.ReadInt32();
                
                // Read map data blocks
                while (stream.Position < stream.Length)
                {
                    var blockType = ReadFixedString(reader, 4);
                    var blockSize = reader.ReadInt32();
                    
                    if (blockType == "MAP ")
                    {
                        var map = new MapData();
                        
                        // Read map properties
                        map.Width = reader.ReadInt32();
                        map.Height = reader.ReadInt32();
                        map.Layers = reader.ReadInt32();
                        map.Background = reader.ReadInt32();
                        map.Music = reader.ReadInt32();
                        
                        // Read tileset ID (new field)
                        map.TilesetId = reader.ReadInt32();
                        
                        // Validate map dimensions
                        ValidateMapDimensions(map);
                        
                        // Read map layers
                        map.LayerData = new int[map.Layers][,];
                        for (int layer = 0; layer < map.Layers; layer++)
                        {
                            map.LayerData[layer] = new int[map.Width, map.Height];
                            for (int x = 0; x < map.Width; x++)
                            {
                                for (int y = 0; y < map.Height; y++)
                                {
                                    map.LayerData[layer][x, y] = reader.ReadInt32();
                                }
                            }
                        }
                        
                        // Read map passability
                        var passability = new bool[map.Width, map.Height];
                        for (int x = 0; x < map.Width; x++)
                        {
                            for (int y = 0; y < map.Height; y++)
                            {
                                passability[x, y] = reader.ReadBoolean();
                            }
                        }
                        // Convert 2D boolean array to 1D int array
                        int[] passabilityArray = new int[passability.GetLength(0) * passability.GetLength(1)];
                        for (int i = 0; i < passability.GetLength(0); i++)
                        {
                            for (int j = 0; j < passability.GetLength(1); j++)
                            {
                                passabilityArray[i * passability.GetLength(1) + j] = passability[i, j] ? 1 : 0;
                            }
                        }
                        map.Passability = passabilityArray;
                        
                        // Read map NPCs
                        var npcCount = reader.ReadInt32();
                        map.NPCs = new NPCData[npcCount];
                        for (int i = 0; i < npcCount; i++)
                        {
                            map.NPCs[i] = new NPCData
                            {
                                X = reader.ReadInt32(),
                                Y = reader.ReadInt32(),
                                Picture = reader.ReadInt32(),
                                Palette = reader.ReadInt32(),
                                MovementType = reader.ReadInt32(),
                                MovementSpeed = reader.ReadInt32(),
                                Script = reader.ReadInt32().ToString(),
                                Active = true
                            };
                        }
                        
                        // Read map events
                        var eventCount = reader.ReadInt32();
                        map.Events = new MapEvent[eventCount];
                        for (int i = 0; i < eventCount; i++)
                        {
                            map.Events[i] = new MapEvent
                            {
                                ID = reader.ReadInt32(),
                                X = reader.ReadInt32(),
                                Y = reader.ReadInt32(),
                                Trigger = (EventTrigger)reader.ReadInt32(),
                                Script = reader.ReadInt32()
                            };
                        }
                        
                        maps.Add(map);
                    }
                    else
                    {
                        // Skip unknown blocks
                        stream.Position += blockSize;
                    }
                }
            }
        }

        /// <summary>
        /// Parse map data in old binary format
        /// </summary>
        private void ParseMapOldBinaryFormat(byte[] data, List<MapData> maps)
        {
            using (var stream = new MemoryStream(data))
            using (var reader = new BinaryReader(stream))
            {
                // Try different header sizes and formats
                var possibleHeaderSizes = new[] { 32, 24, 16, 8 };
                var mapCount = 0;
                var headerSize = 32; // Default
                
                // Determine header size by trying to find valid map data
                foreach (var size in possibleHeaderSizes)
                {
                    if (data.Length >= size && data.Length % size == 0)
                    {
                        var testCount = data.Length / size;
                        if (testCount > 0 && testCount <= 1000) // Sanity check
                        {
                            // Test if this header size produces reasonable dimensions
                            var testStream = new MemoryStream(data);
                            var testReader = new BinaryReader(testStream);
                            
                            bool validFormat = true;
                            for (int i = 0; i < Math.Min(3, testCount); i++) // Test first 3 maps
                            {
                                testStream.Position = i * size;
                                if (size >= 4)
                                {
                                    var width = testReader.ReadInt16();
                                    var height = testReader.ReadInt16();
                                    
                                    if (width < 16 || width > 32768 || height < 10 || height > 32768)
                                    {
                                        validFormat = false;
                                        break;
                                    }
                                }
                                else
                                {
                                    validFormat = false;
                                    break;
                                }
                            }
                            
                            if (validFormat)
                            {
                                headerSize = size;
                                mapCount = testCount;
                                break;
                            }
                        }
                    }
                }
                
                OHRRPGCEDX.Utils.LoggingSystem.Instance.Info("Map Loading", $"ParseMapOldBinaryFormat: Parsing {mapCount} maps from old binary format with header size {headerSize} (data size: {data.Length})");
                
                for (int i = 0; i < mapCount; i++)
                {
                    var map = new MapData();
                    
                    // Reset stream position for this map
                    stream.Position = i * headerSize;
                    
                    // Read map properties based on header size
                    if (headerSize >= 4)
                    {
                        map.Width = reader.ReadInt16();
                        map.Height = reader.ReadInt16();
                    }
                    else
                    {
                        map.Width = 50; // Default
                        map.Height = 50; // Default
                    }
                    
                    if (headerSize >= 6)
                    {
                        map.Layers = reader.ReadInt16();
                    }
                    else
                    {
                        map.Layers = 3; // Default
                    }
                    
                    if (headerSize >= 8)
                    {
                        map.Background = reader.ReadInt16();
                    }
                    else
                    {
                        map.Background = 0; // Default
                    }
                    
                    if (headerSize >= 10)
                    {
                        map.Music = reader.ReadInt16();
                    }
                    else
                    {
                        map.Music = 0; // Default
                    }
                    
                    // Read tileset ID if available
                    if (headerSize >= 12)
                    {
                        try
                        {
                            map.TilesetId = reader.ReadInt16();
                        }
                        catch
                        {
                            // Old format doesn't have tileset ID, use default
                            map.TilesetId = 0;
                        }
                    }
                    else
                    {
                        map.TilesetId = 0; // Default
                    }
                    
                    OHRRPGCEDX.Utils.LoggingSystem.Instance.Info("Map Loading", $"ParseMapOldBinaryFormat: Map {i} header - Width: {map.Width}, Height: {map.Height}, Layers: {map.Layers}, Tileset: {map.TilesetId}");
                    
                    // Validate and fix dimensions if needed
                    ValidateMapDimensions(map);
                    
                    // Initialize layer data
                    map.LayerData = new int[map.Layers][,];
                    for (int layer = 0; layer < map.Layers; layer++)
                    {
                        map.LayerData[layer] = new int[map.Width, map.Height];
                        
                        // Fill with default tiles (grass/floor)
                        for (int x = 0; x < map.Width; x++)
                        {
                            for (int y = 0; y < map.Height; y++)
                            {
                                map.LayerData[layer][x, y] = 1; // Default grass tile
                            }
                        }
                    }
                    
                    // Initialize other arrays
                    map.Tiles = new int[map.Width * map.Height];
                    map.Passability = new int[map.Width * map.Height];
                    map.NPCs = new NPCData[0];
                    map.Doors = new DoorData[0];
                    map.Events = new MapEvent[0];
                    
                    // Fill tiles array with default values
                    for (int j = 0; j < map.Tiles.Length; j++)
                    {
                        map.Tiles[j] = 1; // Default grass tile
                        map.Passability[j] = 1; // Default passable
                    }
                    
                    OHRRPGCEDX.Utils.LoggingSystem.Instance.Info("Map Loading", $"ParseMapOldBinaryFormat: Map {i}: {map.Width}x{map.Height}, {map.Layers} layers, tileset: {map.TilesetId}");
                    
                    // Load the actual tile data from separate lumps
                    LoadMapTileData(map, i);
                    
                    maps.Add(map);
                }
                
                OHRRPGCEDX.Utils.LoggingSystem.Instance.Info("Map Loading", $"ParseMapOldBinaryFormat: Successfully parsed {maps.Count} maps");
            }
        }

        /// <summary>
        /// Validate and fix map dimensions to ensure they are reasonable
        /// </summary>
        private void ValidateMapDimensions(MapData map)
        {
            var originalWidth = map.Width;
            var originalHeight = map.Height;
            
            // Check for invalid dimensions
            if (map.Width <= 0 || map.Width > 32768)
            {
                Console.WriteLine($"Warning: Invalid map width {map.Width}, using default 50");
                map.Width = 50;
            }
            
            if (map.Height <= 0 || map.Height > 32768)
            {
                Console.WriteLine($"Warning: Invalid map height {map.Height}, using default 50");
                map.Height = 50;
            }
            
            // Check for extremely small dimensions
            if (map.Width < 16)
            {
                Console.WriteLine($"Warning: Map width {map.Width} is very small, using minimum 16");
                map.Width = 16;
            }
            
            if (map.Height < 10)
            {
                Console.WriteLine($"Warning: Map height {map.Height} is very small, using minimum 10");
                map.Height = 10;
            }
            
            // If dimensions changed, reinitialize arrays
            if (map.Width != originalWidth || map.Height != originalHeight)
            {
                Console.WriteLine($"Map dimensions changed from {originalWidth}x{originalHeight} to {map.Width}x{map.Height}");
                
                // Reinitialize arrays with new dimensions
                map.Tiles = new int[map.Width * map.Height];
                map.Passability = new int[map.Width * map.Height];
                
                if (map.LayerData != null)
                {
                    map.LayerData = new int[map.Layers][,];
                    for (int layer = 0; layer < map.Layers; layer++)
                    {
                        map.LayerData[layer] = new int[map.Width, map.Height];
                        
                        // Fill with default tiles
                        for (int x = 0; x < map.Width; x++)
                        {
                            for (int y = 0; y < map.Height; y++)
                            {
                                map.LayerData[layer][x, y] = 1; // Default grass tile
                            }
                        }
                    }
                }
                
                // Fill tiles array with default values
                for (int j = 0; j < map.Tiles.Length; j++)
                {
                    map.Tiles[j] = 1; // Default grass tile
                    map.Passability[j] = 1; // Default passable
                }
            }
        }

        /// <summary>
        /// Load tile data for a specific map from old engine lumps
        /// </summary>
        private void LoadMapTileData(MapData map, int mapId)
        {
            var projectName = GetProjectName();
            
            // Use logging system instead of Console.WriteLine
            OHRRPGCEDX.Utils.LoggingSystem.Instance.Info("Map Loading", $"LoadMapTileData: Map {mapId} initial dimensions: {map.Width}x{map.Height}");
            
            // Try to load tile data from .E lump (tiles) - use uppercase to match actual lump names
            var tileLumpName = $"{projectName}.E{mapId:D2}";
            var tileData = GetLump(tileLumpName);
            
            OHRRPGCEDX.Utils.LoggingSystem.Instance.Info("Map Loading", $"LoadMapTileData: Looking for lump '{tileLumpName}', found: {tileData != null}");
            
            if (tileData != null)
            {
                OHRRPGCEDX.Utils.LoggingSystem.Instance.Info("Map Loading", $"Loading tile data for map {mapId} from {tileLumpName}");
                OHRRPGCEDX.Utils.LoggingSystem.Instance.Info("Map Loading", $"LoadMapTileData: Before LoadTilemapFromLump - map dimensions: {map.Width}x{map.Height}");
                LoadTilemapFromLump(map, tileData);
                OHRRPGCEDX.Utils.LoggingSystem.Instance.Info("Map Loading", $"LoadMapTileData: After LoadTilemapFromLump - map dimensions: {map.Width}x{map.Height}");
            }
            else
            {
                OHRRPGCEDX.Utils.LoggingSystem.Instance.Warning("Map Loading", $"No tile data found for map {mapId} in lump {tileLumpName}");
            }
            
            // Try to load zone data from .Z lump (passability)
            var zoneLumpName = $"{projectName}.Z{mapId:D2}";
            var zoneData = GetLump(zoneLumpName);
            
            if (zoneData != null)
            {
                OHRRPGCEDX.Utils.LoggingSystem.Instance.Info("Map Loading", $"Loading zone data for map {mapId} from {zoneLumpName}");
                LoadZoneDataFromLump(map, zoneData);
            }
            
            // Try to load NPC data from .L lump
            var npcLumpName = $"{projectName}.L{mapId:D2}";
            var npcData = GetLump(npcLumpName);
            
            if (npcData != null)
            {
                OHRRPGCEDX.Utils.LoggingSystem.Instance.Info("Map Loading", $"Loading NPC data for map {mapId} from {npcLumpName}");
                LoadNPCLFromLump(map, npcData);
            }
        }
        
        /// <summary>
        /// Load tilemap data from a lump (based on oldengine/loading.rbas LoadTilemap)
        /// </summary>
        private void LoadTilemapFromLump(MapData map, byte[] data)
        {
            try
            {
                Console.WriteLine($"Tile lump data size: {data.Length} bytes");
                var first20Bytes = "";
                for (int i = 0; i < Math.Min(20, data.Length); i++)
                {
                    first20Bytes += $"{data[i]:X2} ";
                }
                Console.WriteLine($"First 20 bytes: {first20Bytes}");
                
                // Try different data formats
                if (TryParseBSAVEFormat(map, data))
                {
                    Console.WriteLine("Successfully parsed BSAVE format");
                    return;
                }
                else if (TryParseRawFormat(map, data))
                {
                    Console.WriteLine("Successfully parsed raw format");
                    return;
                }
                else if (TryParseLegacyFormat(map, data))
                {
                    Console.WriteLine("Successfully parsed legacy format");
                    return;
                }
                else
                {
                    Console.WriteLine("Failed to parse tile data in any known format");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading tilemap: {ex.Message}");
            }
        }

        /// <summary>
        /// Try to parse data as BSAVE format
        /// </summary>
        private bool TryParseBSAVEFormat(MapData map, byte[] data)
        {
            try
            {
                if (data.Length < 11)
                {
                    Console.WriteLine("Data too short for BSAVE format");
                    return false;
                }
                
                // BSAVE header: 7 bytes
                // Byte 0: Magic number (253 = 0xFD)
                // Bytes 1-2: Segment (obsolete, usually 0x9999)
                // Bytes 3-4: Offset (obsolete, usually 0)
                // Bytes 5-6: Length in bytes
                var magicByte = data[0];
                var segment = BitConverter.ToUInt16(data, 1);
                var offset = BitConverter.ToUInt16(data, 3);
                var length = BitConverter.ToUInt16(data, 5);
                
                Console.WriteLine($"BSAVE header: magic=0x{magicByte:X2}, segment=0x{segment:X4}, offset=0x{offset:X4}, length={length}");
                
                // Validate BSAVE header
                if (magicByte != 0xFD)
                {
                    Console.WriteLine($"Invalid BSAVE magic byte: 0x{magicByte:X2}");
                    return false;
                }
                
                using (var stream = new MemoryStream(data))
                using (var reader = new BinaryReader(stream))
                {
                    // Skip BSAVE header (7 bytes) and read width/height
                    // According to oldengine/loading.rbas LoadTilemap function:
                    // Width is at offset 8-9, Height is at offset 10-11
                    stream.Position = 8;
                    var lumpWidth = reader.ReadInt16();
                    var lumpHeight = reader.ReadInt16();
                    
                    Console.WriteLine($"Lump contains {lumpWidth}x{lumpHeight} tiles");
                    
                    // Validate dimensions (same bounds as old engine)
                    if (lumpWidth < 16 || lumpWidth > 32768 || lumpHeight < 10 || lumpHeight > 32768)
                    {
                        Console.WriteLine($"Invalid tilemap dimensions in lump: {lumpWidth}x{lumpHeight}");
                        return false;
                    }
                    
                    // Calculate number of layers (same formula as old engine)
                    // According to oldengine/loading.rbas, the header is 11 bytes total
                    // BSAVE header (7) + width (2) + height (2) = 11 bytes
                    // Tile data starts at offset 11 (after BSAVE header + width + height)
                    var dataSize = data.Length - 11;
                    var layerSize = lumpWidth * lumpHeight;
                    var numLayers = dataSize / layerSize;
                    
                    if (numLayers <= 0)
                    {
                        Console.WriteLine($"Invalid layer count: {numLayers}");
                        return false;
                    }
                    
                    Console.WriteLine($"Loading {numLayers} layers of {lumpWidth}x{lumpHeight} tiles");
                    
                    // Ensure the map has the correct dimensions
                    if (map.Width != lumpWidth || map.Height != lumpHeight)
                    {
                        Console.WriteLine($"Updating map dimensions from {map.Width}x{map.Height} to {lumpWidth}x{lumpHeight}");
                        map.Width = lumpWidth;
                        map.Height = lumpHeight;
                        
                        // Reinitialize arrays with new dimensions
                        map.Tiles = new int[map.Width * map.Height];
                        map.Passability = new int[map.Width * map.Height];
                        map.LayerData = new int[map.Layers][,];
                        for (int layer = 0; layer < map.Layers; layer++)
                        {
                            map.LayerData[layer] = new int[map.Width, map.Height];
                        }
                    }
                    
                    // Read tile data for each layer
                    // Data starts at offset 11 (after BSAVE header + width + height)
                    stream.Position = 11;
                    
                    // Debug: Show first few bytes of tile data
                    var tileDataStart = new byte[Math.Min(32, data.Length - 11)];
                    Array.Copy(data, 11, tileDataStart, 0, tileDataStart.Length);
                    var tileDataHex = string.Join(" ", tileDataStart.Select(b => $"{b:X2}"));
                    Console.WriteLine($"First {tileDataStart.Length} bytes of tile data: {tileDataHex}");
                    
                    for (int layer = 0; layer < Math.Min(numLayers, map.Layers); layer++)
                    {
                        Console.WriteLine($"Loading layer {layer}...");
                        for (int y = 0; y < lumpHeight; y++)
                        {
                            for (int x = 0; x < lumpWidth; x++)
                            {
                                var tileIndex = reader.ReadByte();
                                if (x < map.Width && y < map.Height)
                                {
                                    map.LayerData[layer][x, y] = tileIndex;
                                    
                                    // Debug: Log first few tile values and some random ones
                                    if ((x < 5 && y < 5) || (x == 32 && y == 32) || (x == 63 && y == 63))
                                    {
                                        Console.WriteLine($"  Layer {layer}, Tile at ({x},{y}): {tileIndex}");
                                    }
                                }
                            }
                        }
                    }
                    
                    // Update the main tiles array with the first layer
                    if (map.LayerData.Length > 0)
                    {
                        for (int y = 0; y < map.Height; y++)
                        {
                            for (int x = 0; x < map.Width; x++)
                            {
                                var index = y * map.Width + x;
                                if (index < map.Tiles.Length)
                                {
                                    map.Tiles[index] = map.LayerData[0][x, y];
                                }
                            }
                        }
                    }
                    
                    Console.WriteLine($"Successfully loaded tile data for map {map.Width}x{map.Height}");
                    return true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error parsing BSAVE format: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Try to parse data as raw format (no header, just tile data)
        /// </summary>
        private bool TryParseRawFormat(MapData map, byte[] data)
        {
            try
            {
                // Assume the data is raw tile data with no header
                // Try to determine dimensions based on data size and map properties
                var dataSize = data.Length;
                var expectedTileCount = map.Width * map.Height;
                
                if (dataSize >= expectedTileCount)
                {
                    Console.WriteLine($"Parsing as raw format: {dataSize} bytes for {expectedTileCount} tiles");
                    
                    using (var stream = new MemoryStream(data))
                    using (var reader = new BinaryReader(stream))
                    {
                        // Read tile data directly
                        for (int y = 0; y < map.Height; y++)
                        {
                            for (int x = 0; x < map.Width; x++)
                            {
                                var tileIndex = reader.ReadByte();
                                var index = y * map.Width + x;
                                if (index < map.Tiles.Length)
                                {
                                    map.Tiles[index] = tileIndex;
                                }
                                
                                if (map.LayerData.Length > 0)
                                {
                                    map.LayerData[0][x, y] = tileIndex;
                                }
                            }
                        }
                        
                        Console.WriteLine($"Successfully loaded raw tile data for map {map.Width}x{map.Height}");
                        return true;
                    }
                }
                
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error parsing raw format: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Try to parse data as legacy format (different header structure)
        /// </summary>
        private bool TryParseLegacyFormat(MapData map, byte[] data)
        {
            try
            {
                if (data.Length < 4)
                {
                    return false;
                }
                
                // Try different legacy header formats
                // Format 1: 2-byte width, 2-byte height
                if (data.Length >= 4)
                {
                    var width = BitConverter.ToUInt16(data, 0);
                    var height = BitConverter.ToUInt16(data, 2);
                    
                    if (width >= 16 && width <= 32768 && height >= 10 && height <= 32768)
                    {
                        Console.WriteLine($"Parsing as legacy format 1: {width}x{height}");
                        
                        // Update map dimensions if needed
                        if (map.Width != width || map.Height != height)
                        {
                            Console.WriteLine($"Updating map dimensions from {map.Width}x{map.Height} to {width}x{height}");
                            map.Width = width;
                            map.Height = height;
                            
                            // Reinitialize arrays
                            map.Tiles = new int[map.Width * map.Height];
                            map.Passability = new int[map.Width * map.Height];
                            map.LayerData = new int[map.Layers][,];
                            for (int layer = 0; layer < map.Layers; layer++)
                            {
                                map.LayerData[layer] = new int[map.Width, map.Height];
                            }
                        }
                        
                        // Read tile data starting from offset 4
                        using (var stream = new MemoryStream(data))
                        using (var reader = new BinaryReader(stream))
                        {
                            stream.Position = 4;
                            
                            for (int y = 0; y < map.Height; y++)
                            {
                                for (int x = 0; x < map.Width; x++)
                                {
                                    if (stream.Position < stream.Length)
                                    {
                                        var tileIndex = reader.ReadByte();
                                        var index = y * map.Width + x;
                                        if (index < map.Tiles.Length)
                                        {
                                            map.Tiles[index] = tileIndex;
                                        }
                                        
                                        if (map.LayerData.Length > 0)
                                        {
                                            map.LayerData[0][x, y] = tileIndex;
                                        }
                                    }
                                }
                            }
                        }
                        
                        Console.WriteLine($"Successfully loaded legacy format tile data for map {map.Width}x{map.Height}");
                        return true;
                    }
                }
                
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error parsing legacy format: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Load zone data from a lump (based on oldengine/loading.rbas)
        /// </summary>
        private void LoadZoneDataFromLump(MapData map, byte[] data)
        {
            try
            {
                // Try different data formats
                if (TryParseZoneBSAVEFormat(map, data))
                {
                    Console.WriteLine("Successfully parsed zone data in BSAVE format");
                    return;
                }
                else if (TryParseZoneRawFormat(map, data))
                {
                    Console.WriteLine("Successfully parsed zone data in raw format");
                    return;
                }
                else
                {
                    Console.WriteLine("Failed to parse zone data in any known format");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading zone data: {ex.Message}");
            }
        }

        /// <summary>
        /// Try to parse zone data as BSAVE format
        /// </summary>
        private bool TryParseZoneBSAVEFormat(MapData map, byte[] data)
        {
            try
            {
                if (data.Length < 12)
                {
                    Console.WriteLine("Zone data too short for BSAVE format");
                    return false;
                }
                
                // BSAVE header: 7 bytes
                // Byte 0: Magic number (253 = 0xFD)
                // Bytes 1-2: Segment (obsolete, usually 0x9999)
                // Bytes 3-4: Offset (obsolete, usually 0)
                // Bytes 5-6: Length in bytes
                var magicByte = data[0];
                
                // Validate BSAVE header
                if (magicByte != 0xFD)
                {
                    Console.WriteLine($"Invalid BSAVE magic byte in zone data: 0x{magicByte:X2}");
                    return false;
                }
                
                using (var stream = new MemoryStream(data))
                using (var reader = new BinaryReader(stream))
                {
                    // Skip BSAVE header (7 bytes) and read width/height
                    // Width is at offset 8-9, Height is at offset 10-11
                    stream.Position = 8;
                    var width = reader.ReadInt16();
                    var height = reader.ReadInt16();
                    
                    Console.WriteLine($"Zone data: {width}x{height}");
                    
                    // Read zone data and convert to passability
                    // Data starts at offset 11 (after BSAVE header + width + height)
                    var dataSize = data.Length - 11;
                    var zoneSize = width * height;
                    
                    if (dataSize >= zoneSize)
                    {
                        stream.Position = 11;
                        for (int y = 0; y < Math.Min(height, map.Height); y++)
                        {
                            for (int x = 0; x < Math.Min(width, map.Width); x++)
                            {
                                var zoneId = reader.ReadByte();
                                var index = y * map.Width + x;
                                if (index < map.Passability.Length)
                                {
                                    // Convert zone ID to passability (simplified)
                                    map.Passability[index] = (zoneId == 0) ? 1 : 0;
                                }
                            }
                        }
                        Console.WriteLine($"Loaded zone data for {width}x{height} map");
                        return true;
                    }
                    else
                    {
                        Console.WriteLine($"Zone data size mismatch: expected {zoneSize}, got {dataSize}");
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error parsing zone BSAVE format: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Try to parse zone data as raw format
        /// </summary>
        private bool TryParseZoneRawFormat(MapData map, byte[] data)
        {
            try
            {
                // Assume the data is raw zone data with no header
                var dataSize = data.Length;
                var expectedZoneCount = map.Width * map.Height;
                
                if (dataSize >= expectedZoneCount)
                {
                    Console.WriteLine($"Parsing zone data as raw format: {dataSize} bytes for {expectedZoneCount} zones");
                    
                    using (var stream = new MemoryStream(data))
                    using (var reader = new BinaryReader(stream))
                    {
                        for (int y = 0; y < map.Height; y++)
                        {
                            for (int x = 0; x < map.Width; x++)
                            {
                                if (stream.Position < stream.Length)
                                {
                                    var zoneId = reader.ReadByte();
                                    var index = y * map.Width + x;
                                    if (index < map.Passability.Length)
                                    {
                                        // Convert zone ID to passability (simplified)
                                        map.Passability[index] = (zoneId == 0) ? 1 : 0;
                                    }
                                }
                            }
                        }
                        
                        Console.WriteLine($"Successfully loaded raw zone data for map {map.Width}x{map.Height}");
                        return true;
                    }
                }
                
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error parsing zone raw format: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Load NPC instances from a lump (based on oldengine/loading.rbas)
        /// </summary>
        private void LoadNPCLFromLump(MapData map, byte[] data)
        {
            try
            {
                using (var stream = new MemoryStream(data))
                using (var reader = new BinaryReader(stream))
                {
                    // Skip BSAVE header (11 bytes)
                    stream.Position = 11;
                    
                    // Read NPC data (simplified - just read positions for now)
                    var npcCount = Math.Min(300, (data.Length - 11) / 8); // Assume 8 bytes per NPC
                    var npcs = new List<NPCData>();
                    
                    for (int i = 0; i < npcCount; i++)
                    {
                        if (stream.Position + 8 <= stream.Length)
                        {
                            var npc = new NPCData();
                            npc.X = reader.ReadInt16();
                            npc.Y = reader.ReadInt16();
                            npc.Picture = reader.ReadInt16();
                            npc.MovementType = reader.ReadInt16();
                            
                            if (npc.X >= 0 && npc.Y >= 0 && npc.X < map.Width && npc.Y < map.Height)
                            {
                                npc.Active = true;
                                npcs.Add(npc);
                            }
                        }
                    }
                    
                    map.NPCs = npcs.ToArray();
                    Console.WriteLine($"Loaded {map.NPCs.Length} NPCs for map");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading NPC data: {ex.Message}");
            }
        }

        /// <summary>
        /// Load tileset data
        /// </summary>
        public TilesetData LoadTilesetData(int tilesetId)
        {
            // Try different tileset lump names
            var possibleNames = new[]
            {
                $"tileset{tilesetId:D3}.rgfx",
                $"tileset{tilesetId:D2}.rgfx",
                $"tileset{tilesetId}.rgfx",
                $"TILESET{tilesetId:D3}.RGFX",
                $"TILESET{tilesetId:D2}.RGFX",
                $"TILESET{tilesetId}.RGFX"
            };
            
            byte[] data = null;
            string foundName = null;
            
            foreach (var name in possibleNames)
            {
                data = GetLump(name);
                if (data != null)
                {
                    foundName = name;
                    break;
                }
            }
            
            if (data == null)
            {
                Console.WriteLine($"Failed to find tileset lump for ID {tilesetId}. Tried: {string.Join(", ", possibleNames)}");
                return null;
            }

            Console.WriteLine($"Found tileset lump: {foundName}, size: {data.Length} bytes");

            try
            {
                var tileset = new TilesetData();
                tileset.ID = tilesetId;
                
                int tileCount = 0;
                int tileSize = 0;
                bool hasAnimations = false;
                
                using (var stream = new MemoryStream(data))
                using (var reader = new BinaryReader(stream))
                {
                    // Read tileset header
                    var magic = ReadFixedString(reader, 4);
                    if (magic != "RGFX")
                    {
                        Console.WriteLine($"Invalid tileset format: {magic}");
                        return null;
                    }
                    
                    var version = reader.ReadInt32();
                    tileCount = reader.ReadInt32();
                    tileSize = reader.ReadInt32();
                    hasAnimations = reader.ReadBoolean();
                    
                    Console.WriteLine($"Tileset header: magic={magic}, version={version}, tiles={tileCount}, size={tileSize}, animations={hasAnimations}");
                    
                    tileset.TileCount = tileCount;
                    tileset.TileSize = tileSize;
                    tileset.HasAnimations = hasAnimations;
                    
                    // Read tile graphics data
                    tileset.TileGraphics = new byte[tileCount][];
                    for (int i = 0; i < tileCount; i++)
                    {
                        if (stream.Position + 4 <= stream.Length)
                        {
                            var tileDataSize = reader.ReadInt32();
                            if (tileDataSize > 0 && stream.Position + tileDataSize <= stream.Length)
                            {
                                tileset.TileGraphics[i] = new byte[tileDataSize];
                                reader.Read(tileset.TileGraphics[i], 0, tileDataSize);
                                
                                if (i < 5) // Only log first few tiles to avoid spam
                                {
                                    Console.WriteLine($"Tile {i}: {tileDataSize} bytes");
                                }
                            }
                            else
                            {
                                Console.WriteLine($"Warning: Invalid tile data size for tile {i}: {tileDataSize}");
                                tileset.TileGraphics[i] = new byte[0];
                            }
                        }
                        else
                        {
                            Console.WriteLine($"Warning: Unexpected end of stream at tile {i}");
                            tileset.TileGraphics[i] = new byte[0];
                        }
                    }
                    
                    // Read palette data if available
                    if (stream.Position + 4 <= stream.Length)
                    {
                        var paletteSize = reader.ReadInt32();
                        if (paletteSize > 0 && stream.Position + paletteSize <= stream.Length)
                        {
                            tileset.Palette = new byte[paletteSize];
                            reader.Read(tileset.Palette, 0, paletteSize);
                            Console.WriteLine($"Palette: {paletteSize} bytes");
                        }
                        else
                        {
                            Console.WriteLine($"Warning: Invalid palette size: {paletteSize}");
                            tileset.Palette = new byte[0];
                        }
                    }
                    else
                    {
                        tileset.Palette = new byte[0];
                    }
                    
                    // Read animation data if present
                    if (hasAnimations && stream.Position + 4 <= stream.Length)
                    {
                        var animCount = reader.ReadInt32();
                        if (animCount > 0 && animCount < 1000) // Sanity check
                        {
                            tileset.Animations = new TileAnimation[animCount];
                            
                            for (int i = 0; i < animCount; i++)
                            {
                                if (stream.Position + 12 <= stream.Length)
                                {
                                    var anim = new TileAnimation();
                                    anim.TileID = reader.ReadInt32();
                                    anim.FrameCount = reader.ReadInt32();
                                    anim.FrameDelay = reader.ReadInt32();
                                    
                                    if (anim.FrameCount > 0 && anim.FrameCount < 100 && stream.Position + anim.FrameCount * 4 <= stream.Length)
                                    {
                                        anim.Frames = new int[anim.FrameCount];
                                        
                                        for (int j = 0; j < anim.FrameCount; j++)
                                        {
                                            anim.Frames[j] = reader.ReadInt32();
                                        }
                                        
                                        tileset.Animations[i] = anim;
                                    }
                                    else
                                    {
                                        Console.WriteLine($"Warning: Invalid animation {i} frame count: {anim.FrameCount}");
                                        tileset.Animations[i] = new TileAnimation();
                                    }
                                }
                                else
                                {
                                    Console.WriteLine($"Warning: Unexpected end of stream at animation {i}");
                                    tileset.Animations[i] = new TileAnimation();
                                }
                            }
                            
                            Console.WriteLine($"Animations: {animCount} animation sequences");
                        }
                        else
                        {
                            Console.WriteLine($"Warning: Invalid animation count: {animCount}");
                            tileset.Animations = new TileAnimation[0];
                        }
                    }
                    else
                    {
                        tileset.Animations = new TileAnimation[0];
                    }
                    
                    // Read metadata if available
                    if (stream.Position + 4 <= stream.Length)
                    {
                        var metadataCount = reader.ReadInt32();
                        if (metadataCount > 0 && metadataCount < 1000) // Sanity check
                        {
                            tileset.Metadata = new Dictionary<string, string>();
                            for (int i = 0; i < metadataCount; i++)
                            {
                                if (stream.Position + 8 <= stream.Length)
                                {
                                    var keyLength = reader.ReadInt32();
                                    var valueLength = reader.ReadInt32();
                                    
                                    if (keyLength > 0 && keyLength < 1000 && valueLength >= 0 && valueLength < 10000 &&
                                        stream.Position + keyLength + valueLength <= stream.Length)
                                    {
                                        var key = ReadFixedString(reader, keyLength);
                                        var value = ReadFixedString(reader, valueLength);
                                        tileset.Metadata[key] = value;
                                    }
                                    else
                                    {
                                        Console.WriteLine($"Warning: Invalid metadata entry {i} lengths: key={keyLength}, value={valueLength}");
                                        break;
                                    }
                                }
                                else
                                {
                                    Console.WriteLine($"Warning: Unexpected end of stream at metadata entry {i}");
                                    break;
                                }
                            }
                            
                            Console.WriteLine($"Metadata: {tileset.Metadata.Count} entries");
                        }
                        else
                        {
                            tileset.Metadata = new Dictionary<string, string>();
                        }
                    }
                    else
                    {
                        tileset.Metadata = new Dictionary<string, string>();
                    }
                }
                
                Console.WriteLine($"Successfully loaded tileset {tilesetId}: {tileCount} tiles, {tileSize}x{tileSize}, animations: {hasAnimations}");
                return tileset;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to parse tileset {tilesetId}: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Load item data
        /// </summary>
        public ItemData[] LoadItemData()
        {
            var data = GetLump("items.reld");
            if (data == null)
            {
                // Try old format
                data = GetLump(".DT3");
            }

            if (data == null) return null;

            try
            {
                var items = new List<ItemData>();
                
                if (data.Length > 4 && Encoding.ASCII.GetString(data, 0, 4) == "RELD")
                {
                    ParseItemRelDFormat(data, items);
                }
                else
                {
                    ParseItemOldBinaryFormat(data, items);
                }

                return items.ToArray();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to parse item data: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Parse item data in RELD format
        /// </summary>
        private void ParseItemRelDFormat(byte[] data, List<ItemData> items)
        {
            using (var stream = new MemoryStream(data))
            using (var reader = new BinaryReader(stream))
            {
                // Skip RELD header
                stream.Position = 4;
                
                // Read version
                var version = reader.ReadInt32();
                
                // Read item data blocks
                while (stream.Position < stream.Length)
                {
                    var blockType = ReadFixedString(reader, 4);
                    var blockSize = reader.ReadInt32();
                    
                    if (blockType == "ITEM")
                    {
                        var item = new ItemData();
                        
                        // Read item name
                        var nameLength = reader.ReadInt32();
                        item.Name = ReadFixedString(reader, nameLength);
                        
                        // Read item description
                        var descLength = reader.ReadInt32();
                        item.Description = ReadFixedString(reader, descLength);
                        
                        // Read item properties
                        item.Picture = reader.ReadInt32();
                        item.Palette = reader.ReadInt32();
                        item.ItemType = (ItemType)reader.ReadInt32();
                        item.Price = reader.ReadInt32();
                        item.UsableBy = reader.ReadInt32();
                        
                        // Read item effects
                        item.Effect = (ItemEffect)reader.ReadInt32();
                        item.EffectArg = reader.ReadInt32();
                        item.EffectArg2 = reader.ReadInt32();
                        
                        // Read item stats
                        item.StatBonus = new Stats
                        {
                            HP = reader.ReadInt32(),
                            MP = reader.ReadInt32(),
                            Attack = reader.ReadInt32(),
                            Defense = reader.ReadInt32(),
                            Speed = reader.ReadInt32(),
                            Magic = reader.ReadInt32(),
                            MagicDef = reader.ReadInt32(),
                            Luck = reader.ReadInt32()
                        };
                        
                        // Read elemental properties
                        var elementCount = reader.ReadInt32();
                        item.Elementals = new float[elementCount];
                        for (int i = 0; i < elementCount; i++)
                        {
                            item.Elementals[i] = reader.ReadSingle();
                        }
                        
                        items.Add(item);
                    }
                    else
                    {
                        // Skip unknown blocks
                        stream.Position += blockSize;
                    }
                }
            }
        }

        /// <summary>
        /// Parse item data in old binary format
        /// </summary>
        private void ParseItemOldBinaryFormat(byte[] data, List<ItemData> items)
        {
            using (var stream = new MemoryStream(data))
            using (var reader = new BinaryReader(stream))
            {
                // Old format has fixed item records
                var itemSize = 128; // Approximate size per item
                var itemCount = data.Length / itemSize;
                
                for (int i = 0; i < itemCount; i++)
                {
                    var item = new ItemData();
                    
                    // Read item name (32 bytes)
                    item.Name = ReadFixedString(reader, 32).Trim('\0');
                    
                    // Read item description (32 bytes)
                    item.Description = ReadFixedString(reader, 32).Trim('\0');
                    
                    // Read item properties
                    item.Picture = reader.ReadInt32();
                    item.Palette = reader.ReadInt32();
                    item.ItemType = (ItemType)reader.ReadInt32();
                    item.Price = reader.ReadInt32();
                    item.UsableBy = reader.ReadInt32();
                    
                    // Read item effects
                    item.Effect = (ItemEffect)reader.ReadInt32();
                    item.EffectArg = reader.ReadInt32();
                    item.EffectArg2 = reader.ReadInt32();
                    
                    // Read item stats
                    item.StatBonus = new Stats
                    {
                        HP = reader.ReadInt32(),
                        MP = reader.ReadInt32(),
                        Attack = reader.ReadInt32(),
                        Defense = reader.ReadInt32(),
                        Speed = reader.ReadInt32(),
                        Magic = reader.ReadInt32(),
                        MagicDef = reader.ReadInt32(),
                        Luck = reader.ReadInt32()
                    };
                    
                    // Initialize elemental properties
                    item.Elementals = new float[Constants.maxElements - 1];
                    for (int j = 0; j < Constants.maxElements - 1; j++)
                    {
                        item.Elementals[j] = reader.ReadSingle();
                    }
                    
                    if (!string.IsNullOrEmpty(item.Name))
                    {
                        items.Add(item);
                    }
                }
            }
        }

        /// <summary>
        /// Load spell data
        /// </summary>
        public SpellData[] LoadSpellData()
        {
            var data = GetLump("spells.reld");
            if (data == null)
            {
                // Try old format
                data = GetLump(".DT4");
            }

            if (data == null) return null;

            try
            {
                var spells = new List<SpellData>();
                
                if (data.Length > 4 && Encoding.ASCII.GetString(data, 0, 4) == "RELD")
                {
                    ParseSpellRelDFormat(data, spells);
                }
                else
                {
                    ParseSpellOldBinaryFormat(data, spells);
                }

                return spells.ToArray();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to parse spell data: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Parse spell data in RELD format
        /// </summary>
        private void ParseSpellRelDFormat(byte[] data, List<SpellData> spells)
        {
            using (var stream = new MemoryStream(data))
            using (var reader = new BinaryReader(stream))
            {
                // Skip RELD header
                stream.Position = 4;
                
                // Read version
                var version = reader.ReadInt32();
                
                // Read spell data blocks
                while (stream.Position < stream.Length)
                {
                    var blockType = ReadFixedString(reader, 4);
                    var blockSize = reader.ReadInt32();
                    
                    if (blockType == "SPEL")
                    {
                        var spell = new SpellData();
                        
                        // Read spell name
                        var nameLength = reader.ReadInt32();
                        spell.Name = ReadFixedString(reader, nameLength);
                        
                        // Read spell description
                        var descLength = reader.ReadInt32();
                        spell.Description = ReadFixedString(reader, descLength);
                        
                        // Read spell properties
                        spell.Picture = reader.ReadInt32();
                        spell.Palette = reader.ReadInt32();
                        spell.SpellType = (SpellType)reader.ReadInt32();
                        spell.MPCost = reader.ReadInt32();
                        spell.TargetType = (TargetType)reader.ReadInt32();
                        
                        // Read spell effects
                        spell.Effect = (SpellEffect)reader.ReadInt32();
                        spell.EffectArg = reader.ReadInt32();
                        spell.EffectArg2 = reader.ReadInt32();
                        spell.EffectArg3 = reader.ReadInt32();
                        
                        // Read spell stats
                        spell.Power = reader.ReadInt32();
                        spell.Accuracy = reader.ReadInt32();
                        spell.Element = reader.ReadInt32();
                        
                        // Read spell animation
                        spell.Animation = reader.ReadInt32();
                        spell.SoundEffect = reader.ReadInt32();
                        
                        spells.Add(spell);
                    }
                    else
                    {
                        // Skip unknown blocks
                        stream.Position += blockSize;
                    }
                }
            }
        }

        /// <summary>
        /// Parse spell data in old binary format
        /// </summary>
        private void ParseSpellOldBinaryFormat(byte[] data, List<SpellData> spells)
        {
            using (var stream = new MemoryStream(data))
            using (var reader = new BinaryReader(stream))
            {
                // Old format has fixed spell records
                var spellSize = 96; // Approximate size per spell
                var spellCount = data.Length / spellSize;
                
                for (int i = 0; i < spellCount; i++)
                {
                    var spell = new SpellData();
                    
                    // Read spell name (32 bytes)
                    spell.Name = ReadFixedString(reader, 32).Trim('\0');
                    
                    // Read spell description (32 bytes)
                    spell.Description = ReadFixedString(reader, 32).Trim('\0');
                    
                    // Read spell properties
                    spell.Picture = reader.ReadInt32();
                    spell.Palette = reader.ReadInt32();
                    spell.SpellType = (SpellType)reader.ReadInt32();
                    spell.MPCost = reader.ReadInt32();
                    spell.TargetType = (TargetType)reader.ReadInt32();
                    
                    // Read spell effects
                    spell.Effect = (SpellEffect)reader.ReadInt32();
                    spell.EffectArg = reader.ReadInt32();
                    spell.EffectArg2 = reader.ReadInt32();
                    spell.EffectArg3 = reader.ReadInt32();
                    
                    // Read spell stats
                    spell.Power = reader.ReadInt32();
                    spell.Accuracy = reader.ReadInt32();
                    spell.Element = reader.ReadInt32();
                    
                    // Read spell animation
                    spell.Animation = reader.ReadInt32();
                    spell.SoundEffect = reader.ReadInt32();
                    
                    if (!string.IsNullOrEmpty(spell.Name))
                    {
                        spells.Add(spell);
                    }
                }
            }
        }

        /// <summary>
        /// Load script data
        /// </summary>
        public ScriptData[] LoadScriptData()
        {
            var data = GetLump("scripts.reld");
            if (data == null)
            {
                // Try old format
                data = GetLump(".DT7");
            }

            if (data == null) return null;

            try
            {
                var scripts = new List<ScriptData>();
                
                if (data.Length > 4 && Encoding.ASCII.GetString(data, 0, 4) == "RELD")
                {
                    ParseScriptRelDFormat(data, scripts);
                }
                else
                {
                    ParseScriptOldBinaryFormat(data, scripts);
                }

                return scripts.ToArray();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to parse script data: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Parse script data in RELD format
        /// </summary>
        private void ParseScriptRelDFormat(byte[] data, List<ScriptData> scripts)
        {
            using (var stream = new MemoryStream(data))
            using (var reader = new BinaryReader(stream))
            {
                // Skip RELD header
                stream.Position = 4;
                
                // Read version
                var version = reader.ReadInt32();
                
                // Read script data blocks
                while (stream.Position < stream.Length)
                {
                    var blockType = ReadFixedString(reader, 4);
                    var blockSize = reader.ReadInt32();
                    
                    if (blockType == "SCRP")
                    {
                        var script = new ScriptData();
                        
                        // Read script properties
                        script.ID = reader.ReadInt32();
                        script.Name = ReadFixedString(reader, 32);
                        script.Type = (ScriptType)reader.ReadInt32();
                        
                        // Read script bytecode
                        var bytecodeSize = reader.ReadInt32();
                        script.Bytecode = new byte[bytecodeSize];
                        reader.Read(script.Bytecode, 0, bytecodeSize);
                        
                        // Read script constants
                        var constantCount = reader.ReadInt32();
                        script.Constants = new object[constantCount];
                        for (int i = 0; i < constantCount; i++)
                        {
                            var constantType = reader.ReadInt32();
                            switch (constantType)
                            {
                                case 0: // String
                                    var strLength = reader.ReadInt32();
                                    script.Constants[i] = ReadFixedString(reader, strLength);
                                    break;
                                case 1: // Integer
                                    script.Constants[i] = reader.ReadInt32();
                                    break;
                                case 2: // Float
                                    script.Constants[i] = reader.ReadSingle();
                                    break;
                                default:
                                    script.Constants[i] = null;
                                    break;
                            }
                        }
                        
                        // Read script labels
                        var labelCount = reader.ReadInt32();
                        script.Labels = new Dictionary<string, int>();
                        for (int i = 0; i < labelCount; i++)
                        {
                            var labelName = ReadFixedString(reader, 32);
                            var labelOffset = reader.ReadInt32();
                            script.Labels[labelName] = labelOffset;
                        }
                        
                        scripts.Add(script);
                    }
                    else
                    {
                        // Skip unknown blocks
                        stream.Position += blockSize;
                    }
                }
            }
        }

        /// <summary>
        /// Parse script data in old binary format
        /// </summary>
        private void ParseScriptOldBinaryFormat(byte[] data, List<ScriptData> scripts)
        {
            using (var stream = new MemoryStream(data))
            using (var reader = new BinaryReader(stream))
            {
                // Old format has fixed script records
                var scriptHeaderSize = 64; // Header size per script
                var scriptCount = data.Length / scriptHeaderSize;
                
                for (int i = 0; i < scriptCount; i++)
                {
                    var script = new ScriptData();
                    
                    // Read script properties
                    script.ID = reader.ReadInt32();
                    script.Name = ReadFixedString(reader, 32).Trim('\0');
                    script.Type = (ScriptType)reader.ReadInt32();
                    
                    // Initialize with empty data for old format
                    script.Bytecode = new byte[0];
                    script.Constants = new object[0];
                    script.Labels = new Dictionary<string, int>();
                    
                    if (!string.IsNullOrEmpty(script.Name))
                    {
                        scripts.Add(script);
                    }
                }
            }
        }

        /// <summary>
        /// Load texture data
        /// </summary>
        public TextureData[] LoadTextureData()
        {
            var data = GetLump("textures.reld");
            if (data == null)
            {
                // Try old format
                data = GetLump(".DT8");
            }

            if (data == null) return null;

            try
            {
                var textures = new List<TextureData>();
                
                if (data.Length > 4 && Encoding.ASCII.GetString(data, 0, 4) == "RELD")
                {
                    ParseTextureRelDFormat(data, textures);
                }
                else
                {
                    ParseTextureOldBinaryFormat(data, textures);
                }

                return textures.ToArray();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to parse texture data: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Parse texture data in RELD format
        /// </summary>
        private void ParseTextureRelDFormat(byte[] data, List<TextureData> textures)
        {
            using (var stream = new MemoryStream(data))
            using (var reader = new BinaryReader(stream))
            {
                // Skip RELD header
                stream.Position = 4;
                
                // Read version
                var version = reader.ReadInt32();
                
                // Read texture data blocks
                while (stream.Position < stream.Length)
                {
                    var blockType = ReadFixedString(reader, 4);
                    var blockSize = reader.ReadInt32();
                    
                    if (blockType == "TEXT")
                    {
                        var texture = new TextureData();
                        
                        // Read texture properties
                        texture.ID = reader.ReadInt32();
                        texture.Name = ReadFixedString(reader, 32);
                        texture.Width = reader.ReadInt32();
                        texture.Height = reader.ReadInt32();
                        texture.Format = (TextureFormat)reader.ReadInt32();
                        texture.Palette = reader.ReadInt32();
                        
                        // Read texture pixel data
                        var pixelDataSize = reader.ReadInt32();
                        texture.PixelData = new byte[pixelDataSize];
                        reader.Read(texture.PixelData, 0, pixelDataSize);
                        
                        // Read texture palette data if applicable
                        if (texture.Format == TextureFormat.Indexed8)
                        {
                            var paletteSize = reader.ReadInt32();
                            texture.PaletteData = new byte[paletteSize];
                            reader.Read(texture.PaletteData, 0, paletteSize);
                        }
                        else
                        {
                            texture.PaletteData = new byte[0];
                        }
                        
                        // Read texture metadata
                        var metadataCount = reader.ReadInt32();
                        texture.Metadata = new Dictionary<string, string>();
                        for (int i = 0; i < metadataCount; i++)
                        {
                            var keyLength = reader.ReadInt32();
                            var key = ReadFixedString(reader, keyLength);
                            var valueLength = reader.ReadInt32();
                            var value = ReadFixedString(reader, valueLength);
                            texture.Metadata[key] = value;
                        }
                        
                        textures.Add(texture);
                    }
                    else
                    {
                        // Skip unknown blocks
                        stream.Position += blockSize;
                    }
                }
            }
        }

        /// <summary>
        /// Parse texture data in old binary format
        /// </summary>
        private void ParseTextureOldBinaryFormat(byte[] data, List<TextureData> textures)
        {
            using (var stream = new MemoryStream(data))
            using (var reader = new BinaryReader(stream))
            {
                // Old format has fixed texture records
                var textureHeaderSize = 48; // Header size per texture
                var textureCount = data.Length / textureHeaderSize;
                
                for (int i = 0; i < textureCount; i++)
                {
                    var texture = new TextureData();
                    
                    // Read texture properties
                    texture.ID = reader.ReadInt32();
                    texture.Name = ReadFixedString(reader, 32).Trim('\0');
                    texture.Width = reader.ReadInt32();
                    texture.Height = reader.ReadInt32();
                    texture.Format = (TextureFormat)reader.ReadInt32();
                    texture.Palette = reader.ReadInt32();
                    
                    // Initialize with empty data for old format
                    texture.PixelData = new byte[0];
                    texture.PaletteData = new byte[0];
                    texture.Metadata = new Dictionary<string, string>();
                    
                    if (!string.IsNullOrEmpty(texture.Name))
                    {
                        textures.Add(texture);
                    }
                }
            }
        }

        /// <summary>
        /// Load audio data
        /// </summary>
        public AudioData[] LoadAudioData()
        {
            var data = GetLump("audio.reld");
            if (data == null)
            {
                // Try old format
                data = GetLump(".DT9");
            }

            if (data == null) return null;

            try
            {
                var audioFiles = new List<AudioData>();
                
                if (data.Length > 4 && Encoding.ASCII.GetString(data, 0, 4) == "RELD")
                {
                    ParseAudioRelDFormat(data, audioFiles);
                }
                else
                {
                    ParseAudioOldBinaryFormat(data, audioFiles);
                }

                return audioFiles.ToArray();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to parse audio data: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Parse audio data in RELD format
        /// </summary>
        private void ParseAudioRelDFormat(byte[] data, List<AudioData> audioFiles)
        {
            using (var stream = new MemoryStream(data))
            using (var reader = new BinaryReader(stream))
            {
                // Skip RELD header
                stream.Position = 4;
                
                // Read version
                var version = reader.ReadInt32();
                
                // Read audio data blocks
                while (stream.Position < stream.Length)
                {
                    var blockType = ReadFixedString(reader, 4);
                    var blockSize = reader.ReadInt32();
                    
                    if (blockType == "AUDI")
                    {
                        var audio = new AudioData();
                        
                        // Read audio properties
                        audio.ID = reader.ReadInt32();
                        audio.Name = ReadFixedString(reader, 32);
                        audio.Type = (AudioType)reader.ReadInt32();
                        audio.Format = (AudioFormat)reader.ReadInt32();
                        audio.SampleRate = reader.ReadInt32();
                        audio.Channels = reader.ReadInt32();
                        audio.BitDepth = reader.ReadInt32();
                        
                        // Read audio data
                        var audioDataSize = reader.ReadInt32();
                        audio.RawAudioData = new byte[audioDataSize];
                        reader.Read(audio.RawAudioData, 0, audioDataSize);
                        
                        // Read audio metadata
                        var metadataCount = reader.ReadInt32();
                        audio.Metadata = new Dictionary<string, string>();
                        for (int i = 0; i < metadataCount; i++)
                        {
                            var keyLength = reader.ReadInt32();
                            var key = ReadFixedString(reader, keyLength);
                            var valueLength = reader.ReadInt32();
                            var value = ReadFixedString(reader, valueLength);
                            audio.Metadata[key] = value;
                        }
                        
                        audioFiles.Add(audio);
                    }
                    else
                    {
                        // Skip unknown blocks
                        stream.Position += blockSize;
                    }
                }
            }
        }

        /// <summary>
        /// Parse audio data in old binary format
        /// </summary>
        private void ParseAudioOldBinaryFormat(byte[] data, List<AudioData> audioFiles)
        {
            using (var stream = new MemoryStream(data))
            using (var reader = new BinaryReader(stream))
            {
                // Old format has fixed audio records
                var audioHeaderSize = 56; // Header size per audio
                var audioCount = data.Length / audioHeaderSize;
                
                for (int i = 0; i < audioCount; i++)
                {
                    var audio = new AudioData();
                    
                    // Read audio properties
                    audio.ID = reader.ReadInt32();
                    audio.Name = ReadFixedString(reader, 32).Trim('\0');
                    audio.Type = (AudioType)reader.ReadInt32();
                    audio.Format = (AudioFormat)reader.ReadInt32();
                    audio.SampleRate = reader.ReadInt32();
                    audio.Channels = reader.ReadInt32();
                    audio.BitDepth = reader.ReadInt32();
                    
                    // Initialize with empty data for old format
                                            audio.RawAudioData = new byte[0];
                    audio.Metadata = new Dictionary<string, string>();
                    
                    if (!string.IsNullOrEmpty(audio.Name))
                    {
                        audioFiles.Add(audio);
                    }
                }
            }
        }

        /// <summary>
        /// Load save data
        /// </summary>
        public SaveData[] LoadSaveData()
        {
            var data = GetLump("saves.reld");
            if (data == null)
            {
                // Try old format
                data = GetLump(".SAV");
            }

            if (data == null) return null;

            try
            {
                var saves = new List<SaveData>();
                
                if (data.Length > 4 && Encoding.ASCII.GetString(data, 0, 4) == "RELD")
                {
                    ParseSaveRelDFormat(data, saves);
                }
                else
                {
                    ParseSaveOldBinaryFormat(data, saves);
                }

                return saves.ToArray();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to parse save data: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Parse save data in RELD format
        /// </summary>
        private void ParseSaveRelDFormat(byte[] data, List<SaveData> saves)
        {
            using (var stream = new MemoryStream(data))
            using (var reader = new BinaryReader(stream))
            {
                // Skip RELD header
                stream.Position = 4;
                
                // Read version
                var version = reader.ReadInt32();
                
                // Read save data blocks
                while (stream.Position < stream.Length)
                {
                    var blockType = ReadFixedString(reader, 4);
                    var blockSize = reader.ReadInt32();
                    
                    if (blockType == "SAVE")
                    {
                        var save = new SaveData();
                        
                        // Read save properties
                        save.ID = reader.ReadInt32();
                        save.Name = ReadFixedString(reader, 32);
                        save.Timestamp = DateTime.FromBinary(reader.ReadInt64());
                        save.GameVersion = reader.ReadInt32();
                        
                        // Read player data
                        save.PlayerData = new PlayerData();
                        save.PlayerData.Name = ReadFixedString(reader, 32);
                        save.PlayerData.Level = reader.ReadInt32();
                        save.PlayerData.Experience = reader.ReadInt32();
                        save.PlayerData.Gold = reader.ReadInt32();
                        
                        // Read player stats
                        save.PlayerData.Stats = new Stats
                        {
                            HP = reader.ReadInt32(),
                            MP = reader.ReadInt32(),
                            Attack = reader.ReadInt32(),
                            Defense = reader.ReadInt32(),
                            Speed = reader.ReadInt32(),
                            Magic = reader.ReadInt32(),
                            MagicDef = reader.ReadInt32(),
                            Luck = reader.ReadInt32()
                        };
                        
                        // Read player position
                        save.PlayerData.Position = new XYPair(reader.ReadInt32(), reader.ReadInt32());
                        save.PlayerData.MapID = reader.ReadInt32();
                        save.PlayerData.Direction = (Direction)reader.ReadInt32();
                        
                        // Read inventory
                        var inventorySize = reader.ReadInt32();
                        save.PlayerData.Inventory = new InventoryItem[inventorySize];
                        for (int i = 0; i < inventorySize; i++)
                        {
                            save.PlayerData.Inventory[i] = new InventoryItem
                            {
                                ItemID = reader.ReadInt32(),
                                Quantity = reader.ReadInt32(),
                                Equipped = reader.ReadBoolean()
                            };
                        }
                        
                        // Read party members
                        var partySize = reader.ReadInt32();
                        save.PlayerData.Party = new HeroData[partySize];
                        for (int i = 0; i < partySize; i++)
                        {
                            save.PlayerData.Party[i] = new HeroData();
                            save.PlayerData.Party[i].Name = ReadFixedString(reader, 32);
                            save.PlayerData.Party[i].Level = reader.ReadInt32();
                            save.PlayerData.Party[i].Experience = reader.ReadInt32();
                            
                            // Read hero stats
                            save.PlayerData.Party[i].BaseStats = new Stats
                            {
                                HP = reader.ReadInt32(),
                                MP = reader.ReadInt32(),
                                Attack = reader.ReadInt32(),
                                Defense = reader.ReadInt32(),
                                Speed = reader.ReadInt32(),
                                Magic = reader.ReadInt32(),
                                MagicDef = reader.ReadInt32(),
                                Luck = reader.ReadInt32()
                            };
                        }
                        
                        // Read game flags
                        var flagCount = reader.ReadInt32();
                        save.GameFlags = new Dictionary<string, bool>();
                        for (int i = 0; i < flagCount; i++)
                        {
                            var flagName = ReadFixedString(reader, 32);
                            var flagValue = reader.ReadBoolean();
                            save.GameFlags[flagName] = flagValue;
                        }
                        
                        saves.Add(save);
                    }
                    else
                    {
                        // Skip unknown blocks
                        stream.Position += blockSize;
                    }
                }
            }
        }

        /// <summary>
        /// Parse save data in old binary format
        /// </summary>
        private void ParseSaveOldBinaryFormat(byte[] data, List<SaveData> saves)
        {
            using (var stream = new MemoryStream(data))
            using (var reader = new BinaryReader(stream))
            {
                // Old format has fixed save records
                var saveSize = 1024; // Approximate size per save
                var saveCount = data.Length / saveSize;
                
                for (int i = 0; i < saveCount; i++)
                {
                    var save = new SaveData();
                    
                    // Read save properties
                    save.ID = reader.ReadInt32();
                    save.Name = ReadFixedString(reader, 32).Trim('\0');
                    save.Timestamp = DateTime.Now; // Default timestamp for old format
                    save.GameVersion = reader.ReadInt32();
                    
                    // Read player data
                    save.PlayerData = new PlayerData();
                    save.PlayerData.Name = ReadFixedString(reader, 32).Trim('\0');
                    save.PlayerData.Level = reader.ReadInt32();
                    save.PlayerData.Experience = reader.ReadInt32();
                    save.PlayerData.Gold = reader.ReadInt32();
                    
                    // Read player stats
                    save.PlayerData.Stats = new Stats
                    {
                        HP = reader.ReadInt32(),
                        MP = reader.ReadInt32(),
                        Attack = reader.ReadInt32(),
                        Defense = reader.ReadInt32(),
                        Speed = reader.ReadInt32(),
                        Magic = reader.ReadInt32(),
                        MagicDef = reader.ReadInt32(),
                        Luck = reader.ReadInt32()
                    };
                    
                    // Read player position
                    save.PlayerData.Position = new XYPair(reader.ReadInt32(), reader.ReadInt32());
                    save.PlayerData.MapID = reader.ReadInt32();
                    save.PlayerData.Direction = (Direction)reader.ReadInt32();
                    
                    // Initialize with default values for old format
                    save.PlayerData.Inventory = new InventoryItem[0];
                    save.PlayerData.Party = new HeroData[0];
                    save.GameFlags = new Dictionary<string, bool>();
                    
                    if (!string.IsNullOrEmpty(save.Name))
                    {
                        saves.Add(save);
                    }
                }
            }
        }

        public void Dispose()
        {
            lumps?.Clear();
            isLoaded = false;
        }

        /// <summary>
        /// Check if a tileset is available
        /// </summary>
        public bool IsTilesetAvailable(int tilesetId)
        {
            var possibleNames = new[]
            {
                $"tileset{tilesetId:D3}.rgfx",
                $"tileset{tilesetId:D2}.rgfx",
                $"tileset{tilesetId}.rgfx",
                $"TILESET{tilesetId:D3}.RGFX",
                $"TILESET{tilesetId:D2}.RGFX",
                $"TILESET{tilesetId}.RGFX"
            };
            
            foreach (var name in possibleNames)
            {
                if (HasLump(name))
                {
                    return true;
                }
            }
            
            return false;
        }

        /// <summary>
        /// Get available tileset IDs
        /// </summary>
        public List<int> GetAvailableTilesetIds()
        {
            var availableIds = new List<int>();
            
            foreach (var lumpName in lumps.Keys)
            {
                if (lumpName.StartsWith("tileset", StringComparison.OrdinalIgnoreCase) && 
                    lumpName.EndsWith(".rgfx", StringComparison.OrdinalIgnoreCase))
                {
                    // Extract tileset ID from name
                    var nameWithoutExt = lumpName.Substring(0, lumpName.Length - 5); // Remove .rgfx
                    var tilesetPart = nameWithoutExt.Substring(7); // Remove "tileset"
                    
                    if (int.TryParse(tilesetPart, out int tilesetId))
                    {
                        availableIds.Add(tilesetId);
                    }
                }
            }
            
            return availableIds;
        }
    }

}
