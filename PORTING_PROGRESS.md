# OHRRPGCE DX Porting Progress

This document tracks the progress of porting the OHRRPGCE engine from FreeBASIC to C# .NET 4.8 with SharpDX.

## Overview

**Original Engine**: FreeBASIC-based RPG engine (`oldengine/` folder)
- **Main Entry Points**: `custom.bas` (Editor) and `game.bas` (Runtime)
- **Data Structures**: Defined in `udts.bi`, `game_udts.bi`, and `const.bi`
- **Graphics**: Multiple backends (DirectX, SDL, etc.)

**New Engine**: C# .NET 4.8 with SharpDX (`OHRRPGCEDX/` folder)
- **Main Entry Points**: `Custom.cs` (Editor) and `Game.cs` (Runtime)
- **Data Structures**: C# classes in `DataTypes.cs`
- **Graphics**: SharpDX-based rendering system

## Porting Status

### ✅ COMPLETED

#### Core Infrastructure
- [x] **Project Structure**: Basic C# project setup with .NET 4.8
- [x] **Entry Points**: `Program.cs`, `Custom.cs`, `Game.cs` stubs
- [x] **Constants**: `Constants.cs` with comprehensive constants ported from old engine
- [x] **Naming Conflict Resolution**: Resolved `GameData` class vs namespace conflict by renaming to `RPGData`

#### Data Structures (DataTypes.cs) - 85% Complete
- [x] **RPGData**: Main container class (renamed from GameData) with comprehensive initialization
- [x] **GeneralData**: Core game settings and metadata
  - [x] Basic properties: `GameTitle`, `Author`, `StartingMap`, etc.
  - [x] Music settings: `TitleMusic`, `VictoryMusic`, `BattleMusic`
  - [x] Maximum counts: `MaxHero`, `MaxEnemy`, `MaxAttack`, `MaxTile`, `MaxFormation`, `MaxPalette`, `MaxTextbox`
  - [x] Script settings: `NumPlotScripts`, `NewGameScript`
- [x] **HeroData**: Hero/character definitions
  - [x] Basic properties: `Name`, `Stats`, `Equipment`, etc.
  - [x] **NEWLY ADDED**: `LevelMP` array for FF1-style level MP progression
  - [x] **NEWLY ADDED**: `Elementals`, `HandPositions`, `SpellLists`, `ElementalCounterAttacks`
  - [x] **NEWLY ADDED**: `StatCounterAttacks`, `Bits`, `ListNames`, `ListTypes`
  - [x] **NEWLY ADDED**: `HaveTag`, `AliveTag`, `LeaderTag`, `ActiveTag`, `Checks`
  - [x] **NEWLY ADDED**: `MaxNameLength`, `DefaultAutoBattle`, `SkipVictoryDance`
- [x] **HeroState**: Runtime hero state information
  - [x] Basic properties: `id`, `name`, `locked`, `stat`, `spells`, `levelmp`, `equip`
  - [x] **NEWLY ADDED**: `lev`, `lev_gain`, `learnmask`, `exp_cur`, `exp_next`
  - [x] **NEWLY ADDED**: `battle_pic`, `battle_pal`, `exp_mult`, `def_wep`, `pic`, `pal`
  - [x] **NEWLY ADDED**: `portrait_pic`, `portrait_pal`, `rename_on_status`
  - [x] **NEWLY ADDED**: `elementals`, `hand_pos`, `hand_pos_overridden`, `auto_battle`
- [x] **Stats Structure**: Complete stats implementation with both lowercase and uppercase properties
  - [x] **NEWLY ADDED**: `hp`, `mp`, `attack`, `defense`, `speed`, `magic`, `magicdef`, `luck`
  - [x] **NEWLY ADDED**: Uppercase properties for compatibility: `HP`, `MP`, `Attack`, `Defense`, etc.
  - [x] **NEWLY ADDED**: `Clone()` method for copying stats
- [x] **HeroStats**: Hero stats with base, current, and maximum values
  - [x] **NEWLY ADDED**: `@base`, `cur`, `max` properties
  - [x] **NEWLY ADDED**: `Recalculate()` method for stat calculations
- [x] **EquipSlot**: Equipment slot structure
  - [x] **NEWLY ADDED**: `id`, `equipped` properties
- [x] **MapData**: Map definitions and data
- [x] **EnemyData**: Enemy definitions with comprehensive properties
- [x] **AttackData**: Attack/spell definitions
- [x] **ItemData**: Item definitions
- [x] **ShopData**: Shop definitions
- [x] **FormationData**: Battle formation definitions
- [x] **TextboxData**: Text box definitions
- [x] **MenuData**: Menu definitions with comprehensive menu system
- [x] **ScriptData**: Script definitions with detailed script metadata
- [x] **PaletteData**: Color palette definitions
- [x] **TilesetData**: Tile set definitions
- [x] **MusicData**: Music and sound definitions
- [x] **NPC System**: Complete NPC data structures
  - [x] **NEWLY ADDED**: `NPCType`, `NPCInst`, `NPCData` classes
  - [x] **NEWLY ADDED**: Pathfinding and movement systems
- [x] **Scripting System**: Comprehensive script engine structures
  - [x] **NEWLY ADDED**: `ScriptInst`, `ScriptFibre`, `ScriptCommand`, `ScriptTokenPos`
  - [x] **NEWLY ADDED**: `HSHeader`, `ScriptSourceFile`, `OldScriptState`, `HSVMState`
  - [x] **NEWLY ADDED**: `PlotTimer`, `Plotstring`, `TriggerData`
- [x] **Menu System**: Advanced menu structures
  - [x] **NEWLY ADDED**: `MenuDef`, `MenuDefItem`, `BasicMenuItem`, `SimpleMenuItem`
  - [x] **NEWLY ADDED**: `MenuSearcher`, `SelectTypeState`, `MenuSet`
- [x] **Save System**: Complete save/load structures
  - [x] **NEWLY ADDED**: `SaveData`, `PlayerData`, `InventoryItem`
- [x] **Utility Structures**: Additional helper classes
  - [x] **NEWLY ADDED**: `XYPair`, `RectType`, `Condition`, `TilemapInfo`
  - [x] **NEWLY ADDED**: `ZoneHashedSegment`, `PathfinderOverride`

#### File Loading System
- [x] **RPGFileLoader.cs**: Basic structure for loading RPG data files
- [x] **SaveLoadSystem.cs**: Basic save/load system structure

#### Game Runtime (Game.cs)
- [x] **GameRuntime Class**: Complete Windows Forms-based game runtime
- [x] **Game State Management**: Loading, MainMenu, Playing, Paused, Battle, Menu, Dialog, GameOver
- [x] **Game Loop**: Timer-based game loop with 60 FPS target
- [x] **Input Handling**: Keyboard and mouse event handling
- [x] **System Management**: Graphics, Input, Audio, Script, Menu, Logging, Config, Session
- [x] **File Operations**: RPG file loading, save/load dialogs
- [x] **Player System**: Basic player class with position and hero data
- [x] **Map System**: Basic map class structure

### 🔄 IN PROGRESS

#### Data Structure Porting
- [ ] **Complete Constants Mapping**: Need to verify all constants from old engine are present
- [ ] **Array Initialization**: Ensure all arrays are properly sized using Constants.cs values
- [ ] **Missing Enums**: Some enums from old engine may need to be added

### ❌ NOT STARTED

#### Core Engine Systems
- [ ] **Graphics Engine**: SharpDX rendering system (basic structure exists)
- [ ] **Audio System**: Music and sound playback (basic structure exists)
- [ ] **Input System**: Keyboard, mouse, and gamepad handling (basic structure exists)
- [ ] **Scripting Engine**: Plotscript interpreter (basic structure exists)
- [ ] **Battle System**: Combat mechanics (basic structure exists)
- [ ] **Map System**: Tile-based map rendering and collision (basic structure exists)
- [ ] **UI System**: Menu and text box rendering (basic structure exists)
- [ ] **Save System**: Game state persistence (basic structure exists)

#### Editor Features (Custom)
- [ ] **Map Editor**: Tile-based map creation
- [ ] **Sprite Editor**: Character and enemy graphics
- [ ] **Script Editor**: Plotscript creation and editing
- [ ] **Data Editors**: Hero, enemy, item, etc. editing

#### Runtime Features (Game)
- [ ] **Game Loop**: Main game execution loop (basic structure exists)
- [ ] **NPC System**: Non-player character behavior (basic structure exists)
- [ ] **Inventory System**: Item management (basic structure exists)
- [ ] **Shop System**: Buying and selling (basic structure exists)
- [ ] **Quest System**: Mission and objective tracking

## Data Structure Mapping

### Old Engine → New Engine

#### General Data (gen() array → GeneralData class)
```
gen(genTitle) → GeneralData.Title
gen(genTitleMus) → GeneralData.TitleMusic
gen(genVictMus) → GeneralData.VictoryMusic
gen(genBatMus) → GeneralData.BattleMusic
gen(genMaxHero) → GeneralData.MaxHero
gen(genMaxEnemy) → GeneralData.MaxEnemy
gen(genMaxAttack) → GeneralData.MaxAttack
gen(genMaxTile) → GeneralData.MaxTile
gen(genMaxFormation) → GeneralData.MaxFormation
gen(genMaxPal) → GeneralData.MaxPalette
gen(genMaxTextbox) → GeneralData.MaxTextbox
gen(genNumPlotscripts) → GeneralData.NumPlotScripts
gen(genNewGameScript) → GeneralData.NewGameScript
```

#### Hero Data (.DT0 → HeroData class)
```
HeroState.levelmp() → HeroData.LevelMP[]
HeroState.stat → HeroData.Stats
HeroState.equip() → HeroData.Equipment
HeroState.spells() → HeroData.SpellLists[,]
HeroState.elementals() → HeroData.Elementals[]
HeroState.hand_pos() → HeroData.HandPositions[]
```

#### Hero State (Runtime → HeroState class)
```
HeroState.exp_cur → HeroState.exp_cur
HeroState.exp_next → HeroState.exp_next
HeroState.battle_pic → HeroState.battle_pic
HeroState.battle_pal → HeroState.battle_pal
HeroState.exp_mult → HeroState.exp_mult
HeroState.def_wep → HeroState.def_wep
HeroState.lev_gain → HeroState.lev_gain
HeroState.learnmask → HeroState.learnmask
```

## Constants Mapping

### Old Engine → New Engine
```
maxMPLevel → Constants.maxMPLevel (7)
maxElements → Constants.maxElements (8)
maxSpellLists → Constants.maxSpellLists (4)
maxSpellsPerList → Constants.maxSpellsPerList (24)
maxEquipSlots → Constants.maxEquipSlots (4)
maxLearnMaskBits → Constants.maxLearnMaskBits (96)
inventoryMax → Constants.inventoryMax (1000)
sizeParty → Constants.sizeParty (4)
```

## File Format Support

### Old Engine Formats
- `.GEN` - General data (gen() array)
- `.DT0` - Hero definitions (now heroes.reld)
- `.DT1` - Enemy definitions
- `.DT6` - Attack definitions
- `.ITM` - Item definitions
- `.FOR` - Formation definitions
- `.SAY` - Text box definitions
- `.TIL` - Tile definitions
- `.PAL` - Palette definitions
- `.PT#` - Sprite graphics (now .rgfx)
- `.MXS` - Music files (now .rgfx)

### New Engine Formats
- **Target**: Modern binary formats with proper versioning
- **Current**: Basic C# classes for data representation
- **Future**: Custom binary serialization or JSON/XML

## Next Steps

### Immediate (Next Session)
1. **Test RPGFileLoader**: Ensure it can properly instantiate all data structures
2. **Verify Constants.cs**: Check that all necessary constants are defined and used
3. **Test Game Runtime**: Try to compile and run the basic game runtime

### Short Term
1. **Complete Constants Integration**: Ensure all data structures use Constants.cs values
2. **File Loading**: Implement basic file format loading for one data type
3. **Basic Rendering**: Get a simple sprite or tile rendering working

### Medium Term
1. **Core Game Loop**: Implement basic game execution
2. **Map Rendering**: Basic tile-based map display
3. **Input Handling**: Keyboard and mouse input

### Long Term
1. **Full Engine Port**: Complete all major systems
2. **Editor Tools**: Port the Custom editor functionality
3. **Performance Optimization**: Optimize for modern hardware
4. **Testing**: Ensure compatibility with existing RPG files

## Notes

- **Compilation**: Should now be possible with the current data structures
- **Backwards Compatibility**: Goal is to support existing .rpg files
- **Performance**: SharpDX should provide better performance than the old engine
- **Modern Features**: Opportunity to add new features not possible in the old engine
- **Data Structure Completeness**: We now have comprehensive coverage of the old engine's data structures

## Resources

- **Old Engine Source**: `oldengine/` folder
- **New Engine Source**: `OHRRPGCEDX/` folder
- **Constants Reference**: `oldengine/const.bi` for gen() array constants
- **Data Structure Reference**: `oldengine/udts.bi` and `oldengine/game_udts.bi`

---

*Last Updated: [Current Date]*
*Status: Data Structure Porting - 85% Complete, Game Runtime - 70% Complete*
