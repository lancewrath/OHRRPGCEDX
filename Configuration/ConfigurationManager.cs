using System;
using System.IO;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OHRRPGCEDX.Utils;

namespace OHRRPGCEDX.Configuration
{
    /// <summary>
    /// Manages OHRRPGCE Custom configuration and settings
    /// </summary>
    public class ConfigurationManager
    {
        private static ConfigurationManager _instance;
        private static readonly object _lock = new object();

        // Configuration file paths
        private string _configFilePath;
        private string _userConfigFilePath;
        private string _defaultConfigFilePath;

        // Configuration data
        private Dictionary<string, object> _configuration;
        private Dictionary<string, object> _defaultConfiguration;
        private bool _isInitialized;

        // Configuration change events
        public event EventHandler<ConfigurationChangedEventArgs> ConfigurationChanged;

        public static ConfigurationManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                        {
                            _instance = new ConfigurationManager();
                        }
                    }
                }
                return _instance;
            }
        }

        private ConfigurationManager()
        {
            _configuration = new Dictionary<string, object>();
            _defaultConfiguration = new Dictionary<string, object>();
            _isInitialized = false;
        }

        /// <summary>
        /// Initialize the configuration manager
        /// </summary>
        public bool Initialize(string configPath = null)
        {
            try
            {
                if (_isInitialized)
                    return true;

                // Set up configuration file paths
                SetupConfigPaths(configPath);

                // Load default configuration
                LoadDefaultConfiguration();

                // Load user configuration
                LoadUserConfiguration();

                // Merge configurations
                MergeConfigurations();

                _isInitialized = true;
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to initialize configuration manager: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Set up configuration file paths
        /// </summary>
        private void SetupConfigPaths(string configPath)
        {
            if (!string.IsNullOrEmpty(configPath))
            {
                _configFilePath = configPath;
            }
            else
            {
                // Use default paths
                string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                string ohrrpgcePath = Path.Combine(appDataPath, "OHRRPGCE");
                
                if (!Directory.Exists(ohrrpgcePath))
                {
                    Directory.CreateDirectory(ohrrpgcePath);
                }

                _configFilePath = Path.Combine(ohrrpgcePath, "custom_config.json");
            }

            _userConfigFilePath = _configFilePath;
            _defaultConfigFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "default_config.json");
        }

        /// <summary>
        /// Load default configuration
        /// </summary>
        private void LoadDefaultConfiguration()
        {
            try
            {
                if (File.Exists(_defaultConfigFilePath))
                {
                    string jsonContent = File.ReadAllText(_defaultConfigFilePath);
                    _defaultConfiguration = JsonConvert.DeserializeObject<Dictionary<string, object>>(jsonContent) 
                        ?? new Dictionary<string, object>();
                }
                else
                {
                    // Create default configuration
                    CreateDefaultConfiguration();
                }
            }
            catch
            {
                CreateDefaultConfiguration();
            }
        }

        /// <summary>
        /// Create default configuration
        /// </summary>
        private void CreateDefaultConfiguration()
        {
            _defaultConfiguration = new Dictionary<string, object>
            {
                // Display settings
                ["screen_width"] = 800,
                ["screen_height"] = 600,
                ["fullscreen"] = false,
                ["vsync"] = true,
                ["fps_limit"] = 60,
                ["scale_mode"] = "nearest",
                ["ui_scale"] = 1.0f,

                // Graphics settings
                ["sprite_editor_grid"] = true,
                ["sprite_editor_grid_size"] = 8,
                ["sprite_editor_zoom"] = 2,
                ["palette_editor_swatches"] = 16,
                ["map_editor_tile_size"] = 16,
                ["map_editor_show_grid"] = true,
                ["map_editor_show_coordinates"] = false,

                // Editor settings
                ["auto_save"] = true,
                ["auto_save_interval"] = 300, // 5 minutes
                ["backup_on_save"] = true,
                ["max_backups"] = 10,
                ["recent_files_count"] = 10,
                ["default_project_path"] = "",

                // Audio settings
                ["sound_enabled"] = true,
                ["music_enabled"] = true,
                ["sound_volume"] = 0.8f,
                ["music_volume"] = 0.7f,
                ["audio_sample_rate"] = 44100,
                ["audio_channels"] = 2,

                // Input settings
                ["keyboard_repeat_delay"] = 500,
                ["keyboard_repeat_rate"] = 30,
                ["mouse_sensitivity"] = 1.0f,
                ["gamepad_enabled"] = true,
                ["gamepad_deadzone"] = 0.1f,

                // File settings
                ["default_file_format"] = "rpg",
                ["auto_backup"] = true,
                ["backup_directory"] = "backups",
                ["temp_directory"] = "temp",
                ["session_directory"] = ".session",

                // Debug settings
                ["debug_mode"] = false,
                ["log_level"] = "info",
                ["show_fps"] = false,
                ["show_debug_info"] = false,
                ["crash_reporting"] = true,

                // Language and localization
                ["language"] = "en",
                ["date_format"] = "yyyy-MM-dd",
                ["time_format"] = "HH:mm:ss",
                ["number_format"] = "en-US",

                // Advanced settings
                ["memory_limit"] = 512, // MB
                ["thread_count"] = Environment.ProcessorCount,
                ["cache_size"] = 100, // MB
                ["network_timeout"] = 30, // seconds
                ["file_watch_enabled"] = true
            };

            // Save default configuration
            SaveDefaultConfiguration();
        }

        /// <summary>
        /// Save default configuration
        /// </summary>
        private void SaveDefaultConfiguration()
        {
            try
            {
                string jsonContent = JsonConvert.SerializeObject(_defaultConfiguration, Formatting.Indented);
                File.WriteAllText(_defaultConfigFilePath, jsonContent);
            }
            catch
            {
                // Non-critical failure
            }
        }

        /// <summary>
        /// Load user configuration
        /// </summary>
        private void LoadUserConfiguration()
        {
            try
            {
                if (File.Exists(_userConfigFilePath))
                {
                    string jsonContent = FileOperations.ReadAllText(_userConfigFilePath);
                    var userConfig = JsonConvert.DeserializeObject<Dictionary<string, object>>(jsonContent);
                    
                    if (userConfig != null)
                    {
                        foreach (var kvp in userConfig)
                        {
                            _configuration[kvp.Key] = kvp.Value;
                        }
                    }
                }
            }
            catch
            {
                // Use default configuration if user config is corrupted
            }
        }

        /// <summary>
        /// Merge default and user configurations
        /// </summary>
        private void MergeConfigurations()
        {
            // Start with default configuration
            _configuration = new Dictionary<string, object>(_defaultConfiguration);

            // Override with user configuration
            LoadUserConfiguration();
        }

        /// <summary>
        /// Get a configuration value
        /// </summary>
        public T GetValue<T>(string key, T defaultValue = default(T))
        {
            if (!_isInitialized)
            {
                Initialize();
            }

            try
            {
                if (_configuration.ContainsKey(key))
                {
                    var value = _configuration[key];
                    
                    // Handle type conversion
                    if (value is JToken jsonToken)
                    {
                        return ConvertJsonToken<T>(jsonToken);
                    }
                    
                    if (value is T typedValue)
                    {
                        return typedValue;
                    }

                    // Try to convert the value
                    return (T)Convert.ChangeType(value, typeof(T));
                }

                return defaultValue;
            }
            catch
            {
                return defaultValue;
            }
        }

        /// <summary>
        /// Convert JToken to specified type
        /// </summary>
        private T ConvertJsonToken<T>(JToken token)
        {
            try
            {
                switch (token.Type)
                {
                    case JTokenType.String:
                        return (T)Convert.ChangeType(token.ToString(), typeof(T));
                    case JTokenType.Integer:
                        if (typeof(T) == typeof(int))
                            return (T)(object)token.Value<int>();
                        if (typeof(T) == typeof(long))
                            return (T)(object)token.Value<long>();
                        if (typeof(T) == typeof(float))
                            return (T)(object)token.Value<float>();
                        if (typeof(T) == typeof(double))
                            return (T)(object)token.Value<double>();
                        break;
                    case JTokenType.Float:
                        if (typeof(T) == typeof(float))
                            return (T)(object)token.Value<float>();
                        if (typeof(T) == typeof(double))
                            return (T)(object)token.Value<double>();
                        break;
                    case JTokenType.Boolean:
                        return (T)Convert.ChangeType(token.Value<bool>(), typeof(T));
                }
                
                return default(T);
            }
            catch
            {
                return default(T);
            }
        }

        /// <summary>
        /// Set a configuration value
        /// </summary>
        public void SetValue<T>(string key, T value)
        {
            if (!_isInitialized)
            {
                Initialize();
            }

            try
            {
                var oldValue = _configuration.ContainsKey(key) ? _configuration[key] : null;
                _configuration[key] = value;

                // Raise configuration changed event
                OnConfigurationChanged(key, oldValue, value);

                // Auto-save if enabled
                if (GetValue<bool>("auto_save"))
                {
                    SaveConfiguration();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to set configuration value '{key}': {ex.Message}");
            }
        }

        /// <summary>
        /// Check if a configuration key exists
        /// </summary>
        public bool HasKey(string key)
        {
            if (!_isInitialized)
            {
                Initialize();
            }

            return _configuration.ContainsKey(key);
        }

        /// <summary>
        /// Remove a configuration key
        /// </summary>
        public bool RemoveKey(string key)
        {
            if (!_isInitialized)
            {
                Initialize();
            }

            try
            {
                if (_configuration.ContainsKey(key))
                {
                    var oldValue = _configuration[key];
                    _configuration.Remove(key);

                    // Raise configuration changed event
                    OnConfigurationChanged(key, oldValue, null);

                    return true;
                }

                return false;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Reset a configuration value to default
        /// </summary>
        public bool ResetToDefault(string key)
        {
            if (!_isInitialized)
            {
                Initialize();
            }

            try
            {
                if (_defaultConfiguration.ContainsKey(key))
                {
                    var defaultValue = _defaultConfiguration[key];
                    SetValue(key, defaultValue);
                    return true;
                }

                return false;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Reset all configuration to defaults
        /// </summary>
        public void ResetAllToDefaults()
        {
            if (!_isInitialized)
            {
                Initialize();
            }

            try
            {
                var keysToReset = new List<string>(_configuration.Keys);
                
                foreach (var key in keysToReset)
                {
                    ResetToDefault(key);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to reset configuration to defaults: {ex.Message}");
            }
        }

        /// <summary>
        /// Save configuration to file
        /// </summary>
        public bool SaveConfiguration()
        {
            try
            {
                if (!_isInitialized)
                    return false;

                // Ensure directory exists
                string configDir = Path.GetDirectoryName(_userConfigFilePath);
                if (!string.IsNullOrEmpty(configDir))
                {
                    Directory.CreateDirectory(configDir);
                }

                // Convert configuration to JSON
                string jsonContent = JsonConvert.SerializeObject(_configuration, Formatting.Indented);

                // Write to file
                File.WriteAllText(_userConfigFilePath, jsonContent);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to save configuration: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Reload configuration from file
        /// </summary>
        public bool ReloadConfiguration()
        {
            try
            {
                _configuration.Clear();
                LoadUserConfiguration();
                MergeConfigurations();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to reload configuration: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Get all configuration keys
        /// </summary>
        public IEnumerable<string> GetAllKeys()
        {
            if (!_isInitialized)
            {
                Initialize();
            }

            return _configuration.Keys;
        }

        /// <summary>
        /// Get configuration as dictionary
        /// </summary>
        public Dictionary<string, object> GetConfiguration()
        {
            if (!_isInitialized)
            {
                Initialize();
            }

            return new Dictionary<string, object>(_configuration);
        }

        /// <summary>
        /// Export configuration to file
        /// </summary>
        public bool ExportConfiguration(string filePath)
        {
            try
            {
                if (!_isInitialized)
                    return false;

                string jsonContent = JsonConvert.SerializeObject(_configuration, Formatting.Indented);

                File.WriteAllText(filePath, jsonContent);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to export configuration: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Import configuration from file
        /// </summary>
        public bool ImportConfiguration(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                    return false;

                string jsonContent = File.ReadAllText(filePath);
                var importedConfig = JsonConvert.DeserializeObject<Dictionary<string, object>>(jsonContent);

                if (importedConfig != null)
                {
                    foreach (var kvp in importedConfig)
                    {
                        SetValue(kvp.Key, kvp.Value);
                    }

                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to import configuration: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Raise configuration changed event
        /// </summary>
        protected virtual void OnConfigurationChanged(string key, object oldValue, object newValue)
        {
            ConfigurationChanged?.Invoke(this, new ConfigurationChangedEventArgs(key, oldValue, newValue));
        }

        /// <summary>
        /// Get configuration file path
        /// </summary>
        public string GetConfigFilePath()
        {
            return _userConfigFilePath;
        }

        /// <summary>
        /// Validate configuration values
        /// </summary>
        public bool ValidateConfiguration()
        {
            try
            {
                if (!_isInitialized)
                    return false;

                // Validate critical settings
                var screenWidth = GetValue<int>("screen_width");
                var screenHeight = GetValue<int>("screen_height");
                
                if (screenWidth < 320 || screenHeight < 240)
                {
                    SetValue("screen_width", 800);
                    SetValue("screen_height", 600);
                }

                var fpsLimit = GetValue<int>("fps_limit");
                if (fpsLimit < 1 || fpsLimit > 1000)
                {
                    SetValue("fps_limit", 60);
                }

                var memoryLimit = GetValue<int>("memory_limit");
                if (memoryLimit < 64 || memoryLimit > 8192)
                {
                    SetValue("memory_limit", 512);
                }

                return true;
            }
            catch
            {
                return false;
            }
        }
    }

    /// <summary>
    /// Event arguments for configuration changes
    /// </summary>
    public class ConfigurationChangedEventArgs : EventArgs
    {
        public string Key { get; }
        public object OldValue { get; }
        public object NewValue { get; }

        public ConfigurationChangedEventArgs(string key, object oldValue, object newValue)
        {
            Key = key;
            OldValue = oldValue;
            NewValue = newValue;
        }
    }
}
