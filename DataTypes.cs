using System;
using System.Collections.Generic;
using SharpDX;
using System.Drawing;

namespace OHRRPGCEDX
{
    /// <summary>
    /// 2D coordinate pair
    /// </summary>
    public struct XYPair
    {
        public int x;
        public int y;

        public XYPair(int x, int y)
        {
            this.x = x;
            this.y = y;
        }

        public static XYPair operator +(XYPair a, XYPair b)
        {
            return new XYPair(a.x + b.x, a.y + b.y);
        }

        public static XYPair operator -(XYPair a, XYPair b)
        {
            return new XYPair(a.x - b.x, a.y - b.y);
        }

        public static bool operator ==(XYPair a, XYPair b)
        {
            return a.x == b.x && a.y == b.y;
        }

        public static bool operator !=(XYPair a, XYPair b)
        {
            return !(a == b);
        }

        public override bool Equals(object obj)
        {
            if (obj is XYPair other)
                return this == other;
            return false;
        }

        public override int GetHashCode()
        {
            return x.GetHashCode() ^ y.GetHashCode();
        }
    }

    /// <summary>
    /// Rectangle structure
    /// </summary>
    public struct RectType
    {
        public int x;
        public int y;
        public int w;
        public int h;

        public RectType(int x, int y, int w, int h)
        {
            this.x = x;
            this.y = y;
            this.w = w;
            this.h = h;
        }
    }

    /// <summary>
    /// Comparison type for conditions
    /// </summary>
    public enum CompType
    {
        None,
        Eq,     // =
        Ne,     // <>
        Lt,     // <
        Le,     // <=
        Gt,     // >
        Ge,     // >=
        Tag     // Tag check
    }

    /// <summary>
    /// Tile layer types for map rendering
    /// </summary>
    public enum TileLayerType
    {
        Ground = 0,
        Objects = 1,
        Overlay = 2
    }

    /// <summary>
    /// Enemy behavior types
    /// </summary>
    public enum EnemyBehavior
    {
        Normal = 0,
        Aggressive = 1,
        Defensive = 2,
        Flee = 3,
        Random = 4
    }

    /// <summary>
    /// Item types
    /// </summary>
    public enum ItemType
    {
        None = 0,
        Weapon = 1,
        Armor = 2,
        Shield = 3,
        Helmet = 4,
        Accessory = 5,
        Consumable = 6,
        Key = 7,
        Material = 8
    }

    /// <summary>
    /// Action types for battle system
    /// </summary>
    public enum ActionType
    {
        None = 0,
        Attack = 1,
        Defend = 2,
        UseItem = 3,
        CastSpell = 4,
        Run = 5,
        Special = 6
    }

    /// <summary>
    /// Attack types for enemies
    /// </summary>
    public enum AttackType
    {
        None = 0,
        Physical = 1,
        Magical = 2,
        Special = 3
    }

    /// <summary>
    /// Movement types for NPCs
    /// </summary>
    public enum MovementType
    {
        None = 0,
        Random = 1,
        Follow = 2,
        Patrol = 3,
        Stationary = 4
    }

    /// <summary>
    /// Script types
    /// </summary>
    public enum ScriptType
    {
        None = 0,
        Map = 1,
        Battle = 2,
        Menu = 3,
        Global = 4
    }

    /// <summary>
    /// Texture formats
    /// </summary>
    public enum TextureFormat
    {
        None = 0,
        RGB = 1,
        RGBA = 2,
        Indexed = 3,
        Grayscale = 4
    }

    /// <summary>
    /// Audio types
    /// </summary>
    public enum AudioType
    {
        None = 0,
        Music = 1,
        SoundEffect = 2,
        Voice = 3
    }

    /// <summary>
    /// Audio formats
    /// </summary>
    public enum AudioFormat
    {
        None = 0,
        PCM = 1,
        ADPCM = 2,
        MP3 = 3,
        OGG = 4
    }

    /// <summary>
    /// Item effects
    /// </summary>
    public enum ItemEffect
    {
        None = 0,
        Heal = 1,
        Damage = 2,
        Status = 3,
        StatBoost = 4,
        Special = 5
    }

    /// <summary>
    /// Condition for checking tags or global variables
    /// </summary>
    public class Condition
    {
        public int varnum;      // Global variable number
        public int tag;         // Tag number (positive = on, negative = off, 0 = none)
        public CompType comp;   // Comparison type
        public int value;       // Value to compare against (not used for tags)
        public byte editstate;  // Editor state (Custom only)
        public byte lastinput;  // Last input (Custom only)

        public bool IsTagCheck => comp == CompType.Tag;
    }

    /// <summary>
    /// Basic menu item structure
    /// </summary>
    public class BasicMenuItem
    {
        public string text;           // Displayed caption
        public int col;               // Text color (0=default, >0=color index, <0=UI color)
        public int disabled_col;      // Text color when disabled
        public int bgcol;             // Background color
        public bool unselectable;     // Menu cursor skips this item
        public bool disabled;         // Appears greyed out and disabled
    }

    /// <summary>
    /// Menu definition item
    /// </summary>
    public class MenuDefItem : BasicMenuItem
    {
        public int handle;            // Handle type
        public string caption;        // Caption as set in editor
        public int t;                 // Item type
        public int sub_t;             // Sub-type
        public int tag1;              // Tag 1
        public int tag2;              // Tag 2
        public int settag;            // Set tag
        public int togtag;            // Toggle tag
        public List<int> extravec;    // Extra data vector
        public bool hide_if_disabled; // Hide when disabled
        public bool override_hide;    // Override hide_if_disabled
        public bool close_when_activated; // Close menu when activated
        public bool skip_close_script; // Skip close script
        public object dataptr;        // Data pointer

        public MenuDefItem()
        {
            extravec = new List<int>();
        }

        public bool Visible => !hide_if_disabled || override_hide;

        public int GetExtra(int index)
        {
            if (index >= 0 && index < extravec.Count)
                return extravec[index];
            return 0;
        }

        public void SetExtra(int index, int value)
        {
            while (extravec.Count <= index)
                extravec.Add(0);
            extravec[index] = value;
        }
    }

    /// <summary>
    /// Menu definition
    /// </summary>
    public class MenuDef
    {
        public bool game_menu = true;     // Whether to interpret as MenuItemType
        public int record = -1;           // Record number
        public List<MenuDefItem> items;   // Menu items

        public MenuDef()
        {
            items = new List<MenuDefItem>();
        }

        public void AddItem(MenuDefItem item)
        {
            items.Add(item);
        }
    }

    /// <summary>
    /// Basic stats structure
    /// </summary>
    public class Stats
    {
        public int hp;
        public int mp;
        public int attack;
        public int defense;
        public int speed;
        public int magic;
        public int magicdef;
        public int luck;

        // Uppercase properties for compatibility
        public int HP { get => hp; set => hp = value; }
        public int MP { get => mp; set => mp = value; }
        public int Attack { get => attack; set => attack = value; }
        public int Defense { get => defense; set => defense = value; }
        public int Speed { get => speed; set => speed = value; }
        public int Magic { get => magic; set => magic = value; }
        public int MagicDef { get => magicdef; set => magicdef = value; }
        public int Luck { get => luck; set => luck = value; }

        public Stats()
        {
            hp = mp = attack = defense = speed = magic = magicdef = luck = 0;
        }

        public Stats Clone()
        {
            return new Stats
            {
                hp = this.hp,
                mp = this.mp,
                attack = this.attack,
                defense = this.defense,
                speed = this.speed,
                magic = this.magic,
                magicdef = this.magicdef,
                luck = this.luck
            };
        }
    }

    /// <summary>
    /// Hero stats with base, current, and maximum values
    /// </summary>
    public class HeroStats
    {
        public Stats @base;  // Without equipment, caps, or buffs
        public Stats cur;    // Current stats
        public Stats max;    // Maximum stats

        public HeroStats()
        {
            @base = new Stats();
            cur = new Stats();
            max = new Stats();
        }

        public void Recalculate()
        {
            // Copy base stats to current
            cur = @base.Clone();
            // TODO: Apply equipment bonuses, caps, buffs
        }
    }

    /// <summary>
    /// Equipment slot
    /// </summary>
    public class EquipSlot
    {
        public int id = -1;  // Item ID, or -1 if nothing equipped
        public bool equipped = false;
    }

    /// <summary>
    /// Hero state information
    /// </summary>
    public class HeroState
    {
        public int id = -1;                          // Hero ID, -1 for empty slots
        public string name;                           // Hero name
        public bool locked;                           // Locked slot
        public HeroStats stat;                        // Hero stats
        public int[,] spells;                         // Spell lists [4][23]
        public int[] levelmp;                         // Level MP [maxMPLevel]
        public EquipSlot[] equip;                     // Equipment [4]
        public int lev;                               // Level
        public int lev_gain;                          // Levels gained in last battle
        public int[] learnmask;                       // Spells learned in last battle
        public int exp_cur;                           // Current experience
        public int exp_next;                          // Experience to next level
        public int battle_pic;                        // Battle picture
        public int battle_pal;                        // Battle palette
        public double exp_mult;                       // Experience multiplier
        public int def_wep;                           // Default weapon
        public int pic;                               // Picture ID
        public int pal;                               // Palette ID
        public int portrait_pic;                      // Portrait picture
        public int portrait_pal;                      // Portrait palette
        public bool rename_on_status;                 // Renameable in status menu
        public float[] elementals;                    // Elemental resistances
        public XYPair[] hand_pos;                     // Hand positions [2]
        public bool hand_pos_overridden;              // Hand pos set by script
        public bool auto_battle;                      // Auto-battle enabled
        public ActionType ActionType { get; set; }    // Current action type

        public HeroState()
        {
            stat = new HeroStats();
            spells = new int[4, 23];
            levelmp = new int[100]; // maxMPLevel
            equip = new EquipSlot[4];
            learnmask = new int[4];
            elementals = new float[8];
            hand_pos = new XYPair[2];
            
            for (int i = 0; i < 4; i++)
                equip[i] = new EquipSlot();
        }
    }

    /// <summary>
    /// Hero walkabout (movement) data
    /// </summary>
    public class HeroWalkabout
    {
        public int party_slot;                        // Party slot index
        public int xgo;                               // X goal
        public int ygo;                               // Y goal
        public int wtog;                              // Wait to go
        public int speed;                             // Movement speed
        public List<int> curzones;                    // Current zones

        public HeroWalkabout()
        {
            curzones = new List<int>();
        }
    }

    /// <summary>
    /// Inventory slot
    /// </summary>
    public class InventSlot
    {
        public int item;                              // Item ID
        public int amount;                            // Quantity
        public bool equipped;                         // Is equipped
    }

    /// <summary>
    /// Game state information
    /// </summary>
    public class GameState
    {
        public double timer_offset;                   // Timer offset
        public int current_map;                       // Current map ID
        public string map_name;                       // Current map name
        public HeroState[] heroes;                    // Hero data
        public HeroWalkabout[] hero_walkabout;        // Hero walkabout data
        public InventSlot[] inventory;                // Inventory
        public int gold;                              // Gold amount
        public bool in_battle;                        // Currently in battle
        public bool text_box_active;                  // Text box is active
        public bool menu_active;                      // Menu is active

        public GameState()
        {
            heroes = new HeroState[4];
            hero_walkabout = new HeroWalkabout[4];
            inventory = new InventSlot[100]; // inventoryMax
            
            for (int i = 0; i < 4; i++)
            {
                heroes[i] = new HeroState();
                hero_walkabout[i] = new HeroWalkabout();
            }
            
            for (int i = 0; i < 100; i++)
                inventory[i] = new InventSlot();
        }
    }

    /// <summary>
    /// Text box state
    /// </summary>
    public class TextBoxState
    {
        public bool active;                           // Text box is active
        public string text;                           // Current text
        public int current_char;                      // Current character position
        public bool waiting_for_input;                // Waiting for user input
        public int text_speed;                        // Text display speed
    }

    /// <summary>
    /// Session information for Custom editor
    /// </summary>
    public class SessionInfo
    {
        public string workingdir;              // The directory containing this session's files
        public bool partial_rpg;               // __danger.tmp exists: was in process of unlumping or deleting lumps
        public bool fresh_danger_tmp;          // __danger.tmp exists and isn't stale: may still be running
        public bool info_file_exists;          // session_info.txt.tmp exists. If not, everything below is unknown.
        public int pid;                        // Process ID or 0
        public bool running;                   // That process is still running
        public string sourcerpg;               // May be blank
        public DateTime sourcerpg_old_mtime;   // mtime of the sourcerpg when was opened/last saved by that copy of Custom
        public DateTime sourcerpg_current_mtime; // mtime of the sourcerpg right now, as seen by us
        public DateTime session_start_time;    // When the game was last unlumped/saved (or if none, when Custom was launched)
        public DateTime last_lump_mtime;       // mtime of the most recently modified lump

        public SessionInfo()
        {
            workingdir = "";
            sourcerpg = "";
            sourcerpg_old_mtime = DateTime.MinValue;
            sourcerpg_current_mtime = DateTime.MinValue;
            session_start_time = DateTime.MinValue;
            last_lump_mtime = DateTime.MinValue;
        }
    }

    /// <summary>
    /// Command line options
    /// </summary>
    public class CommandLineOptions
    {
        public string auto_distrib;            // Which distribution option to package automatically
        public bool option_nowait;             // Currently only used when importing scripts from the commandline: don't wait
        public string option_hsflags;          // Used when importing scripts from the commandline: extra args to pass
        public string export_translations_to;  // Export translations to this file
        public string import_scripts_from;     // Import scripts from this file
        public bool help_requested;            // Show help and exit

        public CommandLineOptions()
        {
            auto_distrib = "";
            option_hsflags = "";
            export_translations_to = "";
            import_scripts_from = "";
        }
    }

    /// <summary>
    /// IPC channel for communication between Custom and Game
    /// </summary>
    public class IPCChannel
    {
        public bool IsConnected => false;

        public void SendMessage(string message)
        {
            // TODO: Implement IPC communication
        }

        public string ReceiveMessage()
        {
            // TODO: Implement IPC communication
            return "";
        }

        public void Close()
        {
            // TODO: Implement IPC communication
        }
    }

    /// <summary>
    /// Process handle for managing game processes
    /// </summary>
    public class ProcessHandle
    {
        public int ProcessId { get; set; }
        public bool IsRunning { get; set; }

        public ProcessHandle()
        {
            ProcessId = 0;
            IsRunning = false;
        }

        public void Kill()
        {
            // TODO: Implement process termination
            IsRunning = false;
        }
    }

    /// <summary>
    /// Attack data for battles
    /// </summary>
    public class AttackData
    {
        public string Name { get; set; }
        public int Picture { get; set; }
        public int Palette { get; set; }
        public int Damage { get; set; }
        public int MP_Cost { get; set; }
        public int Accuracy { get; set; }
        public int Critical_Rate { get; set; }
        public int Target_Type { get; set; }
        public int Element { get; set; }
        public int Script { get; set; }
        public bool Usable_Out_Of_Battle { get; set; }
        public bool Usable_In_Battle { get; set; }
        public int[] Tags { get; set; }

        public AttackData()
        {
            Tags = new int[10];
        }
    }

    /// <summary>
    /// Item data
    /// </summary>
    public class ItemData
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public int Picture { get; set; }
        public int Palette { get; set; }
        public int Item_Type { get; set; }
        public int Value { get; set; }
        public int Script { get; set; }
        public bool Usable_Out_Of_Battle { get; set; }
        public bool Usable_In_Battle { get; set; }
        public int[] Tags { get; set; }

        public ItemData()
        {
            Tags = new int[10];
        }
    }

    /// <summary>
    /// Formation data for battles
    /// </summary>
    public class FormationData
    {
        public string Name { get; set; }
        public int[] Enemies { get; set; }
        public int[] Positions { get; set; }
        public int Background { get; set; }
        public int Music { get; set; }
        public int Script { get; set; }

        public FormationData()
        {
            Enemies = new int[10];
            Positions = new int[20];
        }
    }

    /// <summary>
    /// Shop data
    /// </summary>
    public class ShopData
    {
        public string Name { get; set; }
        public int[] Items { get; set; }
        public int[] Prices { get; set; }
        public int Script { get; set; }

        public ShopData()
        {
            Items = new int[50];
            Prices = new int[50];
        }
    }

    /// <summary>
    /// Vehicle data
    /// </summary>
    public class VehicleData
    {
        public string Name { get; set; }
        public int Picture { get; set; }
        public int Palette { get; set; }
        public int Movement_Type { get; set; }
        public int Speed { get; set; }
        public int Script { get; set; }
        public bool Can_Use_On_Land { get; set; }
        public bool Can_Use_On_Water { get; set; }
        public bool Can_Use_In_Air { get; set; }
    }

    /// <summary>
    /// Song data
    /// </summary>
    public class SongData
    {
        public string Name { get; set; }
        public string Filename { get; set; }
        public int Loop_Start { get; set; }
        public int Loop_End { get; set; }
        public bool Loop { get; set; }
    }

    /// <summary>
    /// Sound effect data
    /// </summary>
    public class SoundEffectData
    {
        public string Name { get; set; }
        public string Filename { get; set; }
        public int Volume { get; set; }
        public int Pan { get; set; }
    }

    /// <summary>
    /// Text box data
    /// </summary>
    public class TextBoxData
    {
        public string Name { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public int Border_Style { get; set; }
        public int Text_Color { get; set; }
        public int Background_Color { get; set; }
        public int Border_Color { get; set; }
        public int Font { get; set; }
    }

    /// <summary>
    /// Global text string data
    /// </summary>
    public class GlobalTextStringData
    {
        public string Name { get; set; }
        public string Text { get; set; }
        public int ID { get; set; }
    }

    /// <summary>
    /// Plot script data
    /// </summary>
    public class PlotScriptData
    {
        public string Name { get; set; }
        public string Script { get; set; }
        public int ID { get; set; }
        public bool Enabled { get; set; }
    }

    /// <summary>
    /// Battle sprite
    /// </summary>
    public class BattleSprite
    {
        public int X { get; set; }
        public int Y { get; set; }
        public int Target_X { get; set; }
        public int Target_Y { get; set; }
        public int Speed { get; set; }
        public int Direction { get; set; }
        public int Animation_Frame { get; set; }
        public bool Moving { get; set; }
        public bool Attacking { get; set; }
        public bool Defending { get; set; }
        public int Attack_Timer { get; set; }
        public int Defense_Timer { get; set; }
    }

    /// <summary>
    /// NPC data
    /// </summary>
    public class NPCData
    {
        public int X { get; set; }
        public int Y { get; set; }
        public int Picture { get; set; }
        public int Palette { get; set; }
        public int MovementType { get; set; }
        public int MovementSpeed { get; set; }
        public string Script { get; set; }
        public bool Active { get; set; }
    }

    /// <summary>
    /// Door data
    /// </summary>
    public class DoorData
    {
        public int X { get; set; }
        public int Y { get; set; }
        public int DestinationMap { get; set; }
        public int DestinationX { get; set; }
        public int DestinationY { get; set; }
        public int RequiredItem { get; set; }
        public bool Active { get; set; }
    }

    /// <summary>
    /// Tileset data
    /// </summary>
    public class TilesetData
    {
        public string Name { get; set; }
        public int[] Tiles { get; set; }
        public int[] Passability { get; set; }
        public int[] AnimationFrames { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }

        public TilesetData()
        {
            Tiles = new int[1000];
            Passability = new int[1000];
            AnimationFrames = new int[1000];
        }
    }

    /// <summary>
    /// Map data
    /// </summary>
    public class MapData
    {
        public string Name { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public int[] Tiles { get; set; }
        public int[] Passability { get; set; }
        public NPCData[] NPCs { get; set; }
        public DoorData[] Doors { get; set; }
        public int TilesetId { get; set; }
        public int BackgroundMusic { get; set; }

        public MapData()
        {
            NPCs = new NPCData[100];
            Doors = new DoorData[50];
        }
    }

    /// <summary>
    /// Hero data
    /// </summary>
    public class HeroData
    {
        public string Name { get; set; }
        public int Picture { get; set; }
        public int Palette { get; set; }
        public int Portrait { get; set; }
        public int PortraitPalette { get; set; }
        public Stats BaseStats { get; set; }
        public float[] Elementals { get; set; }
        public XYPair[] HandPositions { get; set; }
        public int ID { get; set; }
        public int Level { get; set; }
        public int Experience { get; set; }
        public double ExpMultiplier { get; set; }
        public int DefaultWeapon { get; set; }
        public Stats Level0Stats { get; set; }
        public Stats LevelMaxStats { get; set; }
        public SpellList[,] SpellLists { get; set; }
        public int[] ElementalCounterAttacks { get; set; }
        public int NonElementalCounterAttack { get; set; }
        public int[] StatCounterAttacks { get; set; }
        public int[] Bits { get; set; }
        public string[] ListNames { get; set; }
        public int[] ListTypes { get; set; }
        public int HaveTag { get; set; }
        public int AliveTag { get; set; }
        public int LeaderTag { get; set; }
        public int ActiveTag { get; set; }
        public TagRangeCheck[] Checks { get; set; }
        public int MaxNameLength { get; set; }
        public bool DefaultAutoBattle { get; set; }
        public bool SkipVictoryDance { get; set; }
        
        // Level MP progression
        public int[] LevelMP { get; set; }

        public HeroData()
        {
            BaseStats = new Stats();
            Level0Stats = new Stats();
            LevelMaxStats = new Stats();
            Elementals = new float[Constants.maxElements];
            HandPositions = new XYPair[2];  // Only 2 hand positions in old engine
            SpellLists = new SpellList[Constants.maxSpellLists, Constants.maxSpellsPerList];
            ElementalCounterAttacks = new int[Constants.maxElements];
            StatCounterAttacks = new int[Constants.statLast + 1];
            Bits = new int[2];
            ListNames = new string[Constants.maxSpellLists];
            ListTypes = new int[Constants.maxSpellLists];
            Checks = new TagRangeCheck[0];
            LevelMP = new int[Constants.maxMPLevel];
        }
    }

    /// <summary>
    /// Enemy data
    /// </summary>
    public class SpellList
    {
        public int[] Spells { get; set; }
        public int[] LearnLevels { get; set; }
        public int[] LearnCosts { get; set; }
        public int[] LearnMasks { get; set; }
        
        public SpellList()
        {
            Spells = new int[Constants.maxSpellsPerList];
            LearnLevels = new int[Constants.maxSpellsPerList];
            LearnCosts = new int[Constants.maxSpellsPerList];
            LearnMasks = new int[Constants.maxLearnMaskBits / 16];
        }
    }

    public class TagRangeCheck
    {
        public int Tag { get; set; }
        public int Min { get; set; }
        public int Max { get; set; }
    }

    public class EnemyData
    {
        public string Name { get; set; }
        public int Picture { get; set; }
        public int Palette { get; set; }
        public Stats BaseStats { get; set; }
        public int[] Attacks { get; set; }
        public float[] Elementals { get; set; }
        public int Experience { get; set; }
        public int Gold { get; set; }
        public int ID { get; set; }
        public EnemyBehavior Behavior { get; set; }
        public int DeathPicture { get; set; }
        public int DeathPalette { get; set; }
        public int Aggression { get; set; }
        public int Intelligence { get; set; }
        public int ExpReward { get; set; }
        public int GoldReward { get; set; }
        public int ItemDrop { get; set; }
        public int ItemDropChance { get; set; }
        public int[] ElementalCounterAttacks { get; set; }
        public int NonElementalCounterAttack { get; set; }
        public int[] StatCounterAttacks { get; set; }
        public int[] Bits { get; set; }
        public int HaveTag { get; set; }
        public int AliveTag { get; set; }
        public int LeaderTag { get; set; }
        public int ActiveTag { get; set; }
        public TagRangeCheck[] Checks { get; set; }
        public int MaxNameLength { get; set; }

        public EnemyData()
        {
            BaseStats = new Stats();
            Attacks = new int[4];
            Elementals = new float[Constants.maxElements];
            ElementalCounterAttacks = new int[Constants.maxElements];
            StatCounterAttacks = new int[Constants.statLast + 1];
            Bits = new int[2];
            Checks = new TagRangeCheck[0];
            Behavior = new EnemyBehavior();
        }
    }

    /// <summary>
    /// Enemy attack
    /// </summary>
    public class EnemyAttack
    {
        public string Name { get; set; }
        public int Damage { get; set; }
        public int MPCost { get; set; }
        public int Accuracy { get; set; }
        public int CriticalRate { get; set; }
        public int Element { get; set; }
        public int TargetType { get; set; }
    }

    /// <summary>
    /// General game data
    /// </summary>
    public class GeneralData
    {
        public string GameTitle { get; set; }
        public string Title { get; set; }  // Alias for GameTitle for compatibility
        public string Author { get; set; }
        public int StartingMap { get; set; }
        public int StartingX { get; set; }
        public int StartingY { get; set; }
        public int StartingGold { get; set; }
        public int[] StartingHeroes { get; set; }
        public int[] StartingItems { get; set; }
        public int MaxMap { get; set; }
        
        // Music settings
        public int TitleMusic { get; set; }
        public int VictoryMusic { get; set; }
        public int BattleMusic { get; set; }
        
        // Maximum counts
        public int MaxHero { get; set; }
        public int MaxEnemy { get; set; }
        public int MaxAttack { get; set; }
        public int MaxTile { get; set; }
        public int MaxFormation { get; set; }
        public int MaxPalette { get; set; }
        public int MaxTextbox { get; set; }
        
        // Script settings
        public int NumPlotScripts { get; set; }
        public int NewGameScript { get; set; }

        public GeneralData()
        {
            StartingHeroes = new int[4];
            StartingItems = new int[20];
        }
    }

    /// <summary>
    /// Battle state
    /// </summary>
    public class BattleState
    {
        public bool Active { get; set; }
        public bool Player_Turn { get; set; }
        public int CurrentEnemy { get; set; }
        public int[] EnemyHP { get; set; }
        public int[] EnemyMP { get; set; }
        public int TurnCounter { get; set; }

        public BattleState()
        {
            EnemyHP = new int[10];
            EnemyMP = new int[10];
        }
    }

    /// <summary>
    /// Equipment data
    /// </summary>
    public class Equipment
    {
        public int id { get; set; }
        public int slot { get; set; }
        public bool equipped { get; set; }
    }

    /// <summary>
    /// Spell data
    /// </summary>
    public class SpellData
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public int Picture { get; set; }
        public int Palette { get; set; }
        public int MP_Cost { get; set; }
        public int Power { get; set; }
        public int Element { get; set; }
        public int Target_Type { get; set; }
        public int Script { get; set; }
        public bool Usable_Out_Of_Battle { get; set; }
        public bool Usable_In_Battle { get; set; }
        public int[] Tags { get; set; }

        public SpellData()
        {
            Tags = new int[10];
        }
    }

    /// <summary>
    /// Script data
    /// </summary>
    public class ScriptData
    {
        public int ID { get; set; }
        public ulong Hash { get; set; }
        public IntPtr Ptr { get; set; }
        public int ScrFormat { get; set; }
        public int HeaderLen { get; set; }
        public int Size { get; set; }
        public int Vars { get; set; }
        public int NonLocals { get; set; }
        public int Args { get; set; }
        public int StrTable { get; set; }
        public int StrTableLen { get; set; }
        public bool HasSrcPos { get; set; }
        public int ScriptPosition { get; set; }
        public int VarNamesTable { get; set; }
        public int NestDepth { get; set; }
        public int Parent { get; set; }
        public string LastTriggerName { get; set; }
        public int RefCount { get; set; }
        public uint LastUse { get; set; }
        public int CallsInStack { get; set; }
        public int NumCalls { get; set; }
        public double LastStart { get; set; }
        public double TotalTime { get; set; }
        public double ChildTime { get; set; }
        public int Entered { get; set; }
        public int NumCmdCalls { get; set; }
        public double CmdTime { get; set; }
        public int SpecificCmdCalls { get; set; }
        public double SpecificCmdTime { get; set; }

        public ScriptData()
        {
            ID = 0;
            Hash = 0;
            Ptr = IntPtr.Zero;
            ScrFormat = 0;
            HeaderLen = 0;
            Size = 0;
            Vars = 0;
            NonLocals = 0;
            Args = 0;
            StrTable = 0;
            StrTableLen = 0;
            HasSrcPos = false;
            ScriptPosition = 0;
            VarNamesTable = 0;
            NestDepth = 0;
            Parent = 0;
            LastTriggerName = "";
            RefCount = 0;
            LastUse = 0;
            CallsInStack = 0;
            NumCalls = 0;
            LastStart = 0.0;
            TotalTime = 0.0;
            ChildTime = 0.0;
            Entered = 0;
            NumCmdCalls = 0;
            CmdTime = 0.0;
            SpecificCmdCalls = 0;
            SpecificCmdTime = 0.0;
        }
    }

    /// <summary>
    /// Texture data
    /// </summary>
    public class TextureData
    {
        public string Name { get; set; }
        public string Filename { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public int[] Data { get; set; }
        public int[] Palette { get; set; }

        public TextureData()
        {
            Data = new int[0];
            Palette = new int[256];
        }
    }

    /// <summary>
    /// Audio data
    /// </summary>
    public class AudioData
    {
        public string Name { get; set; }
        public string Filename { get; set; }
        public int Volume { get; set; }
        public int Pan { get; set; }
        public bool Loop { get; set; }
        public int Loop_Start { get; set; }
        public int Loop_End { get; set; }
    }

    /// <summary>
    /// Direction enumeration
    /// </summary>
    public enum Direction
    {
        North = 0,
        East = 1,
        South = 2,
        West = 3
    }

    /// <summary>
    /// Tile data for maps
    /// </summary>
    public class Tile
    {
        public int ID { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public bool Passable { get; set; }
        public int AnimationFrame { get; set; }
        public int AnimationSpeed { get; set; }
    }

    /// <summary>
    /// Script function data
    /// </summary>
    public class ScriptFunction
    {
        public string Name { get; set; }
        public string Code { get; set; }
        public int ID { get; set; }
        public bool Enabled { get; set; }
        public int[] Parameters { get; set; }
        public int ReturnType { get; set; }

        public ScriptFunction()
        {
            Parameters = new int[10];
        }
    }

    /// <summary>
    /// Spell type enumeration
    /// </summary>
    public enum SpellType
    {
        Normal = 0,
        Healing = 1,
        Status = 2,
        Elemental = 3,
        Summon = 4,
        Transform = 5
    }

    /// <summary>
    /// Target type enumeration
    /// </summary>
    public enum TargetType
    {
        None = 0,
        Self = 1,
        Ally = 2,
        Enemy = 3,
        AllAllies = 4,
        AllEnemies = 5,
        RandomEnemy = 6,
        RandomAlly = 7
    }

    /// <summary>
    /// Spell effect enumeration
    /// </summary>
    public enum SpellEffect
    {
        None = 0,
        Damage = 1,
        Heal = 2,
        Status = 3,
        Transform = 4,
        Summon = 5,
        Teleport = 6,
        Item = 7
    }

    // Missing enums from old engine
    public enum WalkaboutCollisionType
    {
        None = 0,
        Wall = 1,
        NPC = 2,
        Door = 3,
        Zone = 4
    }

    public enum TimerContextEnum
    {
        None = 0,
        Map = 1,
        Battle = 2,
        Menu = 3,
        Global = 4
    }

    public enum DirNum
    {
        North = 0,
        East = 1,
        South = 2,
        West = 3
    }

    public enum NPCIndex
    {
        Invalid = -1
    }

    public enum HandleType
    {
        None = 0,
        MenuItem = 1,
        Script = 2,
        Map = 3,
        Battle = 4
    }

    public enum MenuItemType
    {
        None = 0,
        Text = 1,
        Number = 2,
        Slider = 3,
        Checkbox = 4,
        Button = 5,
        Submenu = 6,
        Back = 7
    }

    public enum BattleMenuItemType
    {
        None = 0,
        Attack = 1,
        Defend = 2,
        UseItem = 3,
        CastSpell = 4,
        Run = 5,
        Special = 6
    }

    /// <summary>
    /// Save data structure
    /// </summary>
    public class SaveData
    {
        public int ID { get; set; }
        public string Name { get; set; }
        public DateTime Timestamp { get; set; }
        public int GameVersion { get; set; }
        public PlayerData PlayerData { get; set; }
        public Dictionary<string, bool> GameFlags { get; set; }

        public SaveData()
        {
            GameFlags = new Dictionary<string, bool>();
        }
    }

    /// <summary>
    /// Player data for save files
    /// </summary>
    public class PlayerData
    {
        public string Name { get; set; }
        public int Level { get; set; }
        public int Experience { get; set; }
        public int Gold { get; set; }
        public Stats Stats { get; set; }
        public XYPair Position { get; set; }
        public int MapID { get; set; }
        public Direction Direction { get; set; }
        public InventoryItem[] Inventory { get; set; }
        public HeroData[] Party { get; set; }

        public PlayerData()
        {
            Stats = new Stats();
            Position = new XYPair(0, 0);
            Inventory = new InventoryItem[100];
            Party = new HeroData[4];
        }
    }

    /// <summary>
    /// Inventory item for save files
    /// </summary>
    public class InventoryItem
    {
        public int ItemID { get; set; }
        public int Quantity { get; set; }
        public bool Equipped { get; set; }
    }

    // Missing structures from old engine UDTs
    public enum PathfindingObstructionMode
    {
        Default = 0,
        NPCsObstruct = 1,
        NPCsIgnored = 2
    }

    public enum NPCOverrideMove
    {
        None = 0,
        NPC = 1,
        POS = 2
    }

    public class PathfinderOverride
    {
        public NPCOverrideMove Override { get; set; }
        public int Cooldown { get; set; }
        public XYPair DestPos { get; set; }
        public int DestNPC { get; set; }
        public bool StopWhenNPCReached { get; set; }
        public int StopAfterStillTicks { get; set; }

        public PathfinderOverride()
        {
            Override = NPCOverrideMove.None;
            Cooldown = 0;
            DestPos = new XYPair(0, 0);
            DestNPC = -1;
            StopWhenNPCReached = false;
            StopAfterStillTicks = 0;
        }
    }

    public class NPCType
    {
        public int Picture { get; set; }           // +0
        public int Palette { get; set; } = -1;    // +1
        public int MoveType { get; set; }          // +2
        public int Speed { get; set; } = 4;       // +3
        public int TextBox { get; set; }           // +4
        public int FaceType { get; set; }          // +5
        public int Item { get; set; }              // +6
        public int PushType { get; set; }          // +7
        public int Activation { get; set; }        // +8
        public int Tag1 { get; set; }              // +9
        public int Tag2 { get; set; }              // +10
        public int UseTag { get; set; }            // +11
        public int Script { get; set; }            // +12
        public int ScriptArg { get; set; }         // +13
        public int Vehicle { get; set; }           // +14
        public int DefaultZone { get; set; }       // +15
        public int DefaultWallZone { get; set; }   // +16
        public int IgnorePassMap { get; set; }     // +17
        public PathfindingObstructionMode PathfindingObstructionMode { get; set; } // +18

        public NPCType()
        {
            Picture = 0;
            Palette = -1;
            MoveType = 0;
            Speed = 4;
            TextBox = 0;
            FaceType = 0;
            Item = 0;
            PushType = 0;
            Activation = 0;
            Tag1 = 0;
            Tag2 = 0;
            UseTag = 0;
            Script = 0;
            ScriptArg = 0;
            Vehicle = 0;
            DefaultZone = 0;
            DefaultWallZone = 0;
            IgnorePassMap = 0;
            PathfindingObstructionMode = PathfindingObstructionMode.Default;
        }
    }

    public class NPCInst
    {
        public XYPair Pos { get; set; }
        public int Z { get; set; }
        public int ID { get; set; }
        public int Pool { get; set; }
        public XYPair XYGo { get; set; }
        public Direction Dir { get; set; }
        public int WToG { get; set; }
        public List<int> ExtraVec { get; set; }
        public bool IgnoreWalls { get; set; }
        public bool NotObstruction { get; set; }
        public bool SuspendUse { get; set; }
        public bool SuspendAI { get; set; }
        public object Slice { get; set; }
        public List<int> CurZones { get; set; }
        public int StillTicks { get; set; }
        public PathfinderOverride PathOver { get; set; }
        public bool FollowWallsWaiting { get; set; }

        public NPCInst()
        {
            Pos = new XYPair(0, 0);
            Z = 0;
            ID = 0;
            Pool = 0;
            XYGo = new XYPair(0, 0);
            Dir = Direction.South;
            WToG = 0;
            ExtraVec = new List<int>();
            IgnoreWalls = false;
            NotObstruction = false;
            SuspendUse = false;
            SuspendAI = false;
            Slice = null;
            CurZones = new List<int>();
            StillTicks = 0;
            PathOver = new PathfinderOverride();
            FollowWallsWaiting = false;
        }
    }

    public enum ScriptRole
    {
        SubscriptRole = -1,
        ScriptRole = 0,
        PlotscriptRole = 1
    }

    public class TriggerData
    {
        public string Name { get; set; }
        public int ID { get; set; }
        public ScriptRole Role { get; set; }
        public bool Imported { get; set; }

        public TriggerData()
        {
            Name = "";
            ID = 0;
            Role = ScriptRole.ScriptRole;
            Imported = false;
        }
    }

    public class ScriptFibre
    {
        public int ID { get; set; }
        public ScriptInst Root { get; set; }
        public string TriggerName { get; set; }
        public string TriggerLoc { get; set; }
        public bool DoubleTriggerCheck { get; set; }
        public int Priority { get; set; }
        public string LogLine { get; set; }
        public int ArgC { get; set; }
        public int[] Args { get; set; }

        public ScriptFibre()
        {
            ID = 0;
            Root = null;
            TriggerName = "";
            TriggerLoc = "";
            DoubleTriggerCheck = false;
            Priority = 0;
            LogLine = "";
            ArgC = 0;
            Args = new int[10]; // maxScriptArgs
        }
    }

    /// <summary>
    /// Script instance class - represents a running script
    /// </summary>
    public class ScriptInst
    {
        public int ID { get; set; }
        public ScriptData Script { get; set; }
        public int State { get; set; }
        public int PC { get; set; } // Program counter
        public int[] Locals { get; set; }
        public int[] Args { get; set; }
        public int ReturnValue { get; set; }
        public bool Running { get; set; }
        public DateTime StartTime { get; set; }
        public string Name { get; set; }

        public ScriptInst()
        {
            ID = 0;
            Script = null;
            State = 0;
            PC = 0;
            Locals = new int[0];
            Args = new int[0];
            ReturnValue = 0;
            Running = false;
            StartTime = DateTime.Now;
            Name = "";
        }
    }

    public class ScriptCommand
    {
        public int Kind { get; set; }
        public int Value { get; set; }
        public int ArgC { get; set; }
        public int[] Args { get; set; }

        public ScriptCommand()
        {
            Kind = 0;
            Value = 0;
            ArgC = 0;
            Args = new int[0];
        }
    }

    public class PlotTimer
    {
        public int Count { get; set; }
        public int Speed { get; set; }
        public int Ticks { get; set; }
        public int Trigger { get; set; }
        public int[] ScriptArgs { get; set; }
        public int Flags { get; set; }
        public int St { get; set; }
        public int FinishedTick { get; set; }

        public PlotTimer()
        {
            Count = 0;
            Speed = 0;
            Ticks = 0;
            Trigger = 0;
            ScriptArgs = new int[0];
            Flags = 0;
            St = 0;
            FinishedTick = 0;
        }
    }

    public class Plotstring
    {
        public string S { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public int Col { get; set; }
        public int BgCol { get; set; }
        public int Bits { get; set; }

        public Plotstring()
        {
            S = "";
            X = 0;
            Y = 0;
            Col = 0;
            BgCol = 0;
            Bits = 0;
        }
    }

    public class MenuSet
    {
        public string MenuFile { get; set; }
        public string ItemFile { get; set; }

        public MenuSet()
        {
            MenuFile = "";
            ItemFile = "";
        }
    }

    public class SimpleMenuItem : BasicMenuItem
    {
        public int Dat { get; set; }
        public object DatPtr { get; set; }

        public SimpleMenuItem()
        {
            Dat = 0;
            DatPtr = null;
        }
    }

    public class MenuSearcher
    {
        public string[] MenuArray { get; set; }
        public List<BasicMenuItem> MenuVector { get; set; }
        public string ExcludeWord { get; set; }

        public MenuSearcher(string[] menu)
        {
            MenuArray = menu;
            MenuVector = null;
            ExcludeWord = "";
        }

        public MenuSearcher(List<BasicMenuItem> menuVector)
        {
            MenuArray = null;
            MenuVector = menuVector;
            ExcludeWord = "";
        }

        public string Text(int index)
        {
            if (MenuArray != null && index >= 0 && index < MenuArray.Length)
                return MenuArray[index];
            if (MenuVector != null && index >= 0 && index < MenuVector.Count)
                return MenuVector[index].text;
            return "";
        }

        public bool Selectable(int index)
        {
            if (MenuArray != null && index >= 0 && index < MenuArray.Length)
                return true;
            if (MenuVector != null && index >= 0 && index < MenuVector.Count)
                return !MenuVector[index].unselectable;
            return false;
        }
    }

    public class SelectTypeState
    {
        public string Query { get; set; }
        public string Buffer { get; set; }
        public double LastInputTime { get; set; }
        public int QueryAt { get; set; }
        public int HighlightAt { get; set; }
        public int RememberPt { get; set; }

        public SelectTypeState()
        {
            Query = "";
            Buffer = "";
            LastInputTime = 0.0;
            QueryAt = 0;
            HighlightAt = 0;
            RememberPt = 0;
        }
    }

    public class HSHeader
    {
        public bool Valid { get; set; }
        public string HSpeakVersion { get; set; }
        public int HSPFormat { get; set; }
        public int ScriptFormat { get; set; }
        public int MaxFunctionID { get; set; }
        public string PlotScrVersion { get; set; }

        public HSHeader()
        {
            Valid = false;
            HSpeakVersion = "";
            HSPFormat = 0;
            ScriptFormat = 0;
            MaxFunctionID = 0;
            PlotScrVersion = "";
        }
    }

    public class ScriptSourceFile
    {
        public int Offset { get; set; }
        public int Length { get; set; }
        public string Filename { get; set; }
        public string LumpName { get; set; }

        public ScriptSourceFile()
        {
            Offset = 0;
            Length = 0;
            Filename = "";
            LumpName = "";
        }
    }

    public class OldScriptFrame
    {
        public int Heap { get; set; }

        public OldScriptFrame()
        {
            Heap = 0;
        }
    }

    public class OldScriptState
    {
        public ScriptData Scr { get; set; }
        public IntPtr ScrData { get; set; }
        public OldScriptFrame[] Frames { get; set; }
        public int HeapEnd { get; set; }
        public int StackBase { get; set; }
        public int State { get; set; }
        public int Ptr { get; set; }
        public int Ret { get; set; }
        public int CurArgN { get; set; }
        public int Depth { get; set; }
        public int ID { get; set; }
        public int SavedScriptRet { get; set; }

        public OldScriptState()
        {
            Scr = null;
            ScrData = IntPtr.Zero;
            Frames = new OldScriptFrame[10]; // maxScriptNesting
            for (int i = 0; i < Frames.Length; i++)
                Frames[i] = new OldScriptFrame();
            HeapEnd = 0;
            StackBase = 0;
            State = 0;
            Ptr = 0;
            Ret = 0;
            CurArgN = 0;
            Depth = 0;
            ID = 0;
            SavedScriptRet = 0;
        }
    }

    public class HSVMState
    {
        public ScriptFibre CurFibre { get; set; }
        public ScriptData CurScript { get; set; }
        public OldScriptState CurScrAt { get; set; }
        public ScriptInst CurScriptInst { get; set; }
        public int CurSlot { get; set; }

        public HSVMState()
        {
            CurFibre = null;
            CurScript = null;
            CurScrAt = null;
            CurScriptInst = null;
            CurSlot = 0;
        }

        public void SetCurScript()
        {
            // Implementation would go here
        }
    }

    public class ScriptTokenPos
    {
        public int LineNum { get; set; }
        public int Col { get; set; }
        public int Length { get; set; }
        public string LineText { get; set; }
        public string Filename { get; set; }
        public int IsVirtual { get; set; }

        public ScriptTokenPos()
        {
            LineNum = 0;
            Col = 0;
            Length = 0;
            LineText = "";
            Filename = "";
            IsVirtual = 0;
        }
    }

    public class TilemapInfo
    {
        public XYPair Size { get; set; }
        public int Layers { get; set; }
        public string Error { get; set; }

        public TilemapInfo()
        {
            Size = new XYPair(0, 0);
            Layers = 0;
            Error = "";
        }

        public void SetError(string filename, string errmsg)
        {
            Error = $"{filename}: {errmsg}";
        }
    }

    public enum MapEdgeMode
    {
        Crop = 0,
        Wrap = 1,
        DefaultTile = 2
    }

    public class ZoneHashedSegment
    {
        public ushort[] IDMap { get; set; }

        public ZoneHashedSegment()
        {
            IDMap = new ushort[14];
        }
    }

    /// <summary>
    /// Main container class for all game data
    /// </summary>
    public class RPGData
    {
        // General game information
        public GeneralData General { get; set; }
        
        // Hero and character data
        public HeroData[] Heroes { get; set; }
        public EnemyData[] Enemies { get; set; }
        public NPCData[] NPCs { get; set; }
        
        // Item and equipment data
        public ItemData[] Items { get; set; }
        public Equipment[] Equipment { get; set; }
        public SpellData[] Spells { get; set; }
        public AttackData[] Attacks { get; set; }
        
        // Map and world data
        public MapData[] Maps { get; set; }
        public TilesetData[] Tilesets { get; set; }
        public DoorData[] Doors { get; set; }
        public VehicleData[] Vehicles { get; set; }
        
        // Battle and formation data
        public FormationData[] Formations { get; set; }
        public BattleState BattleState { get; set; }
        
        // Audio data
        public SongData[] Songs { get; set; }
        public SoundEffectData[] SoundEffects { get; set; }
        public AudioData[] Audio { get; set; }
        
        // UI and interface data
        public TextBoxData[] TextBoxes { get; set; }
        public GlobalTextStringData[] GlobalTextStrings { get; set; }
        public MenuDef[] Menus { get; set; }
        
        // Scripting data
        public PlotScriptData[] PlotScripts { get; set; }
        public ScriptData[] Scripts { get; set; }
        public ScriptFunction[] ScriptFunctions { get; set; }
        public TriggerData[] Triggers { get; set; }
        
        // Shop and economy data
        public ShopData[] Shops { get; set; }
        
        // Graphics and texture data
        public TextureData[] Textures { get; set; }
        
        // Game state and session data
        public GameState CurrentGameState { get; set; }
        public SessionInfo SessionInfo { get; set; }
        
        // Save and load data
        public SaveData[] SaveSlots { get; set; }
        
        // Constants and limits
        public int MaxHeroes { get; set; } = 4;
        public int MaxEnemies { get; set; } = 100;
        public int MaxItems { get; set; } = 1000;
        public int MaxSpells { get; set; } = 1000;
        public int MaxMaps { get; set; } = 1000;
        public int MaxNPCs { get; set; } = 1000;
        public int MaxShops { get; set; } = 100;
        public int MaxSaveSlots { get; set; } = 10;

        public RPGData()
        {
            // Initialize all arrays with default sizes
            Heroes = new HeroData[MaxHeroes];
            Enemies = new EnemyData[MaxEnemies];
            NPCs = new NPCData[MaxNPCs];
            Items = new ItemData[MaxItems];
            Equipment = new Equipment[MaxItems];
            Spells = new SpellData[MaxSpells];
            Attacks = new AttackData[MaxSpells];
            Maps = new MapData[MaxMaps];
            Tilesets = new TilesetData[100];
            Doors = new DoorData[MaxMaps * 50];
            Vehicles = new VehicleData[10];
            Formations = new FormationData[100];
            Songs = new SongData[100];
            SoundEffects = new SoundEffectData[100];
            Audio = new AudioData[200];
            TextBoxes = new TextBoxData[50];
            GlobalTextStrings = new GlobalTextStringData[1000];
            Menus = new MenuDef[100];
            PlotScripts = new PlotScriptData[100];
            Scripts = new ScriptData[1000];
            ScriptFunctions = new ScriptFunction[100];
            Triggers = new TriggerData[100];
            Shops = new ShopData[MaxShops];
            Textures = new TextureData[1000];
            SaveSlots = new SaveData[MaxSaveSlots];
            
            // Initialize individual objects
            General = new GeneralData();
            BattleState = new BattleState();
            CurrentGameState = new GameState();
            SessionInfo = new SessionInfo();
            
            // Initialize arrays with default objects
            for (int i = 0; i < MaxHeroes; i++)
                Heroes[i] = new HeroData();
            
            for (int i = 0; i < MaxEnemies; i++)
                Enemies[i] = new EnemyData();
            
            for (int i = 0; i < MaxItems; i++)
            {
                Items[i] = new ItemData();
                Equipment[i] = new Equipment();
            }
            
            for (int i = 0; i < MaxSpells; i++)
            {
                Spells[i] = new SpellData();
                Attacks[i] = new AttackData();
            }
            
            for (int i = 0; i < MaxMaps; i++)
                Maps[i] = new MapData();
            
            for (int i = 0; i < MaxSaveSlots; i++)
                SaveSlots[i] = new SaveData();
        }

        /// <summary>
        /// Get a hero by ID
        /// </summary>
        public HeroData GetHero(int id)
        {
            if (id >= 0 && id < MaxHeroes)
                return Heroes[id];
            return null;
        }

        /// <summary>
        /// Get an enemy by ID
        /// </summary>
        public EnemyData GetEnemy(int id)
        {
            if (id >= 0 && id < MaxEnemies)
                return Enemies[id];
            return null;
        }

        /// <summary>
        /// Get an item by ID
        /// </summary>
        public ItemData GetItem(int id)
        {
            if (id >= 0 && id < MaxItems)
                return Items[id];
            return null;
        }

        /// <summary>
        /// Get a spell by ID
        /// </summary>
        public SpellData GetSpell(int id)
        {
            if (id >= 0 && id < MaxSpells)
                return Spells[id];
            return null;
        }

        /// <summary>
        /// Get a map by ID
        /// </summary>
        public MapData GetMap(int id)
        {
            if (id >= 0 && id < MaxMaps)
                return Maps[id];
            return null;
        }

        /// <summary>
        /// Save the current game state to a save slot
        /// </summary>
        public bool SaveGame(int slot, string saveName)
        {
            if (slot < 0 || slot >= MaxSaveSlots)
                return false;

            SaveSlots[slot] = new SaveData
            {
                ID = slot,
                Name = saveName,
                Timestamp = DateTime.Now,
                GameVersion = 1, // TODO: Get actual game version
                PlayerData = new PlayerData
                {
                    Name = CurrentGameState.heroes[0]?.name ?? "Player",
                    Level = CurrentGameState.heroes[0]?.lev ?? 1,
                    Experience = CurrentGameState.heroes[0]?.exp_cur ?? 0,
                    Gold = CurrentGameState.gold,
                    Stats = CurrentGameState.heroes[0]?.stat.cur ?? new Stats(),
                    Position = new XYPair(0, 0), // TODO: Get actual position
                    MapID = CurrentGameState.current_map,
                    Direction = Direction.South,
                    Inventory = new InventoryItem[100],
                    Party = new HeroData[4]
                }
            };

            // TODO: Implement actual save file writing
            return true;
        }

        /// <summary>
        /// Load a game from a save slot
        /// </summary>
        public bool LoadGame(int slot)
        {
            if (slot < 0 || slot >= MaxSaveSlots || SaveSlots[slot] == null)
                return false;

            var saveData = SaveSlots[slot];
            if (saveData.PlayerData != null)
            {
                // TODO: Implement actual game state restoration
                CurrentGameState.gold = saveData.PlayerData.Gold;
                CurrentGameState.current_map = saveData.PlayerData.MapID;
                // ... restore other game state
            }

            return true;
        }

        /// <summary>
        /// Clear all game data
        /// </summary>
        public void Clear()
        {
            // Reset all data to default values
            General = new GeneralData();
            BattleState = new BattleState();
            CurrentGameState = new GameState();
            SessionInfo = new SessionInfo();
            
            // Clear arrays
            Array.Clear(Heroes, 0, Heroes.Length);
            Array.Clear(Enemies, 0, Enemies.Length);
            Array.Clear(Items, 0, Items.Length);
            Array.Clear(Spells, 0, Spells.Length);
            Array.Clear(Maps, 0, Maps.Length);
            Array.Clear(SaveSlots, 0, SaveSlots.Length);
            
            // Reinitialize with default objects
            for (int i = 0; i < MaxHeroes; i++)
                Heroes[i] = new HeroData();
            
            for (int i = 0; i < MaxEnemies; i++)
                Enemies[i] = new EnemyData();
            
            for (int i = 0; i < MaxItems; i++)
            {
                Items[i] = new ItemData();
                Equipment[i] = new Equipment();
            }
            
            for (int i = 0; i < MaxSpells; i++)
            {
                Spells[i] = new SpellData();
                Attacks[i] = new AttackData();
            }
            
            for (int i = 0; i < MaxMaps; i++)
                Maps[i] = new MapData();
            
            for (int i = 0; i < MaxSaveSlots; i++)
                SaveSlots[i] = new SaveData();
        }
    }
}

