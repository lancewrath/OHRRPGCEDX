using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

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

                    // Read lump size (4 bytes, little-endian)
                    var lumpSizeBytes = reader.ReadBytes(4);
                    var lumpSize = BitConverter.ToInt32(lumpSizeBytes, 0);

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
            var data = GetLump("general.reld");
            if (data == null)
            {
                // Try old format
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
                    stream.Position = Constants.genTitle;
                    general.Title = ReadFixedString(reader, 32);
                    
                    stream.Position = Constants.genTitleMus;
                    general.TitleMusic = reader.ReadInt32();
                    
                    stream.Position = Constants.genVictMus;
                    general.VictoryMusic = reader.ReadInt32();
                    
                    stream.Position = Constants.genBatMus;
                    general.BattleMusic = reader.ReadInt32();
                    
                    stream.Position = Constants.genMaxHero;
                    general.MaxHero = reader.ReadInt32();
                    
                    stream.Position = Constants.genMaxEnemy;
                    general.MaxEnemy = reader.ReadInt32();
                    
                    stream.Position = Constants.genMaxMap;
                    general.MaxMap = reader.ReadInt32();
                    
                    stream.Position = Constants.genMaxAttack;
                    general.MaxAttack = reader.ReadInt32();
                    
                    stream.Position = Constants.genMaxTile;
                    general.MaxTile = reader.ReadInt32();
                    
                    stream.Position = Constants.genMaxFormation;
                    general.MaxFormation = reader.ReadInt32();
                    
                    stream.Position = Constants.genMaxPal;
                    general.MaxPalette = reader.ReadInt32();
                    
                    stream.Position = Constants.genMaxTextbox;
                    general.MaxTextbox = reader.ReadInt32();
                    
                    stream.Position = Constants.genNumPlotscripts;
                    general.NumPlotScripts = reader.ReadInt32();
                    
                    stream.Position = Constants.genNewGameScript;
                    general.NewGameScript = reader.ReadInt32();
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
            var data = GetLump("heroes.reld");
            if (data == null)
            {
                // Try old format
                data = GetLump(".DT2");
            }

            if (data == null) return null;

            try
            {
                var heroes = new List<HeroData>();
                
                if (data.Length > 4 && Encoding.ASCII.GetString(data, 0, 4) == "RELD")
                {
                    ParseHeroRelDFormat(data, heroes);
                }
                else
                {
                    ParseHeroOldBinaryFormat(data, heroes);
                }

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
                var heroSize = 256; // Approximate size per hero
                var heroCount = data.Length / heroSize;
                
                for (int i = 0; i < heroCount; i++)
                {
                    var hero = new HeroData();
                    
                    // Read hero name (32 bytes)
                    hero.Name = ReadFixedString(reader, 32).Trim('\0');
                    
                    // Skip to stats section
                    stream.Position += 32;
                    
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
                    
                    // Read picture and palette
                    hero.Picture = reader.ReadInt32();
                    hero.Palette = reader.ReadInt32();
                    hero.Portrait = reader.ReadInt32();
                    hero.PortraitPalette = reader.ReadInt32();
                    
                    // Initialize arrays
                    hero.LevelMP = new int[Constants.maxMPLevel];
                    hero.Elementals = new float[Constants.maxElements - 1];
                    hero.HandPositions = new XYPair[2];
                    
                    // Read level MP data
                    for (int j = 0; j < Constants.maxMPLevel; j++)
                    {
                        hero.LevelMP[j] = reader.ReadInt32();
                    }
                    
                    // Read elemental resistances
                    for (int j = 0; j < Constants.maxElements - 1; j++)
                    {
                        hero.Elementals[j] = reader.ReadSingle();
                    }
                    
                    // Read hand positions
                    for (int j = 0; j < 2; j++)
                    {
                        hero.HandPositions[j] = new XYPair(reader.ReadInt32(), reader.ReadInt32());
                    }
                    
                    if (!string.IsNullOrEmpty(hero.Name))
                    {
                        heroes.Add(hero);
                    }
                }
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
            var data = GetLump("maps.reld");
            if (data == null)
            {
                // Try old format
                data = GetLump(".DT6");
            }

            if (data == null) return null;

            try
            {
                var maps = new List<MapData>();
                
                if (data.Length > 4 && Encoding.ASCII.GetString(data, 0, 4) == "RELD")
                {
                    ParseMapRelDFormat(data, maps);
                }
                else
                {
                    ParseMapOldBinaryFormat(data, maps);
                }

                return maps.ToArray();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to parse map data: {ex.Message}");
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
                // Old format has fixed map records
                var mapHeaderSize = 32; // Header size per map
                var mapCount = data.Length / mapHeaderSize;
                
                for (int i = 0; i < mapCount; i++)
                {
                    var map = new MapData();
                    
                    // Read map properties
                    map.Width = reader.ReadInt32();
                    map.Height = reader.ReadInt32();
                    map.Layers = reader.ReadInt32();
                    map.Background = reader.ReadInt32();
                    map.Music = reader.ReadInt32();
                    
                    // Initialize with default values for old format
                    if (map.Width <= 0 || map.Height <= 0)
                    {
                        map.Width = 50;
                        map.Height = 50;
                        map.Layers = 3;
                    }
                    
                    // Initialize layer data
                    map.LayerData = new int[map.Layers][,];
                    for (int layer = 0; layer < map.Layers; layer++)
                    {
                        map.LayerData[layer] = new int[map.Width, map.Height];
                        // Fill with default tiles
                        for (int x = 0; x < map.Width; x++)
                        {
                            for (int y = 0; y < map.Height; y++)
                            {
                                map.LayerData[layer][x, y] = 0;
                            }
                        }
                    }
                    
                    // Initialize passability
                    var passability = new bool[map.Width, map.Height];
                    for (int x = 0; x < map.Width; x++)
                    {
                        for (int y = 0; y < map.Height; y++)
                        {
                            passability[x, y] = true;
                        }
                    }
                    // Convert 2D boolean array to 1D int array
                    int[] passabilityArray = new int[passability.GetLength(0) * passability.GetLength(1)];
                    for (int row = 0; row < passability.GetLength(0); row++)
                    {
                        for (int col = 0; col < passability.GetLength(1); col++)
                        {
                            passabilityArray[row * passability.GetLength(1) + col] = passability[row, col] ? 1 : 0;
                        }
                    }
                    map.Passability = passabilityArray;
                    
                    // Initialize NPCs and events
                    map.NPCs = new NPCData[0];
                    map.Events = new MapEvent[0];
                    
                    maps.Add(map);
                }
            }
        }

        /// <summary>
        /// Load tileset data
        /// </summary>
        public TilesetData LoadTilesetData(int tilesetId)
        {
            var lumpName = $"tileset{tilesetId:D3}.rgfx";
            var data = GetLump(lumpName);
            if (data == null) return null;

            try
            {
                // TODO: Implement tileset data parsing
                Console.WriteLine($"Tileset {tilesetId} data parsing not yet implemented");
                return new TilesetData();
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
    }

}
