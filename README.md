# OHRRPGCEDX - OHRRPGCE Custom Editor & Game Runtime (.NET Port)

## Overview

OHRRPGCEDX is a modern .NET port of the OHRRPGCE (Official Hamster Republic Role Playing Game Construction Engine) Custom editor and Game Runtime. This project aims to provide a cross-platform, maintainable implementation using C# and modern .NET technologies while preserving compatibility with existing OHRRPGCE game files.

## Project Status

**Current Status**: Active Development - Core Architecture Complete  
**Last Updated**: December 2024  
**Target Framework**: .NET Framework 4.8 / .NET 6+  
**Platform Support**: Windows (Primary), Linux/macOS (Planned)

## Architecture

The project is structured as a modular, component-based system with clear separation of concerns:

```
OHRRPGCEDX/
├── Core/                    # Core application logic
├── Graphics/               # Rendering and graphics systems
├── GameData/              # Game data loading and management
├── Input/                 # Input handling (keyboard, mouse, gamepad)
├── Audio/                 # Audio playback and management
├── Scripting/             # Script engine and execution
├── UI/                    # User interface components
├── Utils/                 # Utility classes and helpers
├── Configuration/         # Configuration management
└── Session/               # Session and state management
```

## Components

### 1. Core Application (`Custom.cs`, `Game.cs`, `Program.cs`)
- **Custom Editor**: Main editor application for creating and editing RPG games
- **Game Runtime**: Standalone game player for running RPG files
- **Program Entry Points**: Application startup and initialization

### 2. Graphics System (`Graphics/`)
- **GraphicsSystem**: Direct3D 11 rendering system using SharpDX
- **ShaderSystem**: Shader compilation and management
- **TextureManager**: Texture loading, caching, and management
- **MapRenderer**: 2D map rendering with tile support
- **Sprite**: Sprite rendering and animation support
- **GameWindow**: Window management and rendering context

### 3. Game Data Management (`GameData/`)
- **RPGFileLoader**: Comprehensive RPG file format parser supporting:
  - RelD format (modern OHRRPGCE format)
  - Legacy binary format
  - Unlumped RPG directories
- **BattleSystem**: Turn-based battle mechanics and AI
- **SaveLoadSystem**: Game state persistence and loading
- **DataTypes**: Complete data structures for all game elements

### 4. Input System (`Input/`)
- **InputSystem**: Unified input handling for:
  - Keyboard input with configurable bindings
  - Mouse input (buttons, movement, wheel)
  - Gamepad/Joystick support via DirectInput
  - Action-based input mapping

### 5. Audio System (`Audio/`)
- **AudioSystem**: Audio playback and management
- **Support for**: Music, sound effects, voice clips
- **Formats**: MP3, OGG, WAV, and other common formats

### 6. Scripting Engine (`Scripting/`)
- **ScriptEngine**: HamsterSpeak script execution
- **Features**: Script compilation, runtime execution, debugging support

### 7. User Interface (`UI/`)
- **MenuSystem**: Hierarchical menu system with navigation
- **Components**: Menu items, state management, rendering

### 8. Utilities (`Utils/`)
- **LoggingSystem**: Comprehensive logging with multiple outputs
- **Performance**: Performance monitoring and profiling tools

### 9. Configuration (`Configuration/`)
- **ConfigurationManager**: Hierarchical configuration management
- **Features**: JSON-based config, user overrides, validation

## Data Types and Structures

The project includes comprehensive data structures for all OHRRPGCE game elements:

- **Heroes**: Character stats, equipment, skills
- **Enemies**: AI behavior, stats, drops
- **Maps**: Tile data, events, NPCs, triggers
- **Items**: Equipment, consumables, key items
- **Spells**: Magic system, effects, targeting
- **Audio**: Music, sound effects, voice clips
- **Scripts**: HamsterSpeak scripts and triggers

## RPG File Format Support

### Supported Formats
- **RelD Format**: Modern OHRRPGCE format with enhanced features
- **Legacy Binary**: Original OHRRPGCE format for backward compatibility
- **Unlumped Directories**: Development-friendly directory structure

### File Types
- `.rpg` - Lumped RPG files
- `.sav` - Save game files
- Various lump files for individual game components

## Development Status

### ✅ Completed Components
- Core application architecture
- Graphics system (Direct3D 11)
- Input system (keyboard, mouse, gamepad)
- RPG file loading and parsing
- Data type definitions
- Configuration management
- Logging system
- Basic UI framework

### 🚧 In Progress
- Battle system implementation
- Save/load system completion
- Script engine integration
- Audio system refinement

### 📋 Planned Features
- Cross-platform support (Linux/macOS)
- Enhanced editor tools
- Plugin system
- Network multiplayer support
- Mobile platform support

## Building the Project

### Prerequisites
- Visual Studio 2019/2022 or .NET 6+ SDK
- Windows 10/11 (for Direct3D graphics)
- SharpDX NuGet packages

### Build Instructions
1. Clone the repository
2. Open `OHRRPGCEDX.sln` in Visual Studio
3. Restore NuGet packages
4. Build the solution

### Project Files
- **OHRRPGCEDX.Custom.csproj**: Custom Editor application
- **OHRRPGCEDX.Game.csproj**: Game Runtime application
- **OHRRPGCEDX.sln**: Solution file

## Usage

### Running the Custom Editor
```bash
OHRRPGCEDX.Custom.exe [rpgfile]
```

### Running the Game Runtime
```bash
OHRRPGCEDX.Game.exe [rpgfile]
```

### Command Line Options
- `-h, --help`: Show help information
- `-d, --distrib`: Package distribution options
- `--nowait`: Don't wait when importing scripts
- `--hsflags`: Extra HamsterSpeak compiler flags

## Technical Details

### Graphics
- **API**: Direct3D 11 via SharpDX
- **Rendering**: 2D sprite-based with shader support
- **Performance**: Optimized for 60 FPS gameplay

### Input
- **Keyboard**: Full keyboard support with configurable bindings
- **Mouse**: Multi-button mouse support with wheel
- **Gamepad**: DirectInput gamepad/joystick support

### Audio
- **Formats**: MP3, OGG, WAV, and more
- **Features**: Streaming, caching, volume control

### Scripting
- **Language**: HamsterSpeak (OHRRPGCE's scripting language)
- **Features**: Compilation, runtime execution, debugging

## Contributing

### Development Guidelines
1. Follow C# coding conventions
2. Add comprehensive XML documentation
3. Include unit tests for new features
4. Update this README for significant changes

### Areas for Contribution
- Cross-platform graphics implementation
- Enhanced editor tools
- Performance optimizations
- Bug fixes and testing
- Documentation improvements

## License

This project is based on the OHRRPGCE engine and follows similar licensing terms. Please refer to the original OHRRPGCE license for details.

## Acknowledgments

- **OHRRPGCE Team**: Original engine development
- **SharpDX Team**: DirectX bindings for .NET
- **Community Contributors**: Testing, feedback, and contributions

## Roadmap

### Short Term (3-6 months)
- Complete battle system implementation
- Finish save/load system
- Enhance script engine integration
- Improve UI responsiveness

### Medium Term (6-12 months)
- Cross-platform graphics support
- Enhanced editor tools
- Plugin system architecture
- Performance optimizations

### Long Term (1+ years)
- Mobile platform support
- Network multiplayer
- Advanced scripting features
- Community tools and utilities

## Support and Community

- **Issues**: Report bugs and feature requests via GitHub Issues
- **Discussions**: Join community discussions on GitHub Discussions
- **Documentation**: Check the wiki for detailed guides and tutorials

---

*This README is updated regularly to reflect the current state of the project. For the latest information, check the commit history and recent updates.*
