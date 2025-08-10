using System;
using System.IO;
using System.Collections.Generic;
using OHRRPGCEDX.Utils;

namespace OHRRPGCEDX.Session
{
    /// <summary>
    /// Manages OHRRPGCE Custom session state and working directories
    /// </summary>
    public class SessionManager
    {
        private static SessionManager _instance;
        private static readonly object _lock = new object();

        // Session state variables
        public string WorkingDirectory { get; private set; }
        public string SessionDirectory { get; private set; }
        public string TempDirectory { get; private set; }
        public string BackupDirectory { get; private set; }
        public DateTime SessionStartTime { get; private set; }
        public bool IsSessionActive { get; private set; }
        public string CurrentProjectName { get; private set; }
        public string LastLoadedFile { get; private set; }
        public DateTime LastSaveTime { get; private set; }
        public bool HasUnsavedChanges { get; set; }

        // Working directory management
        private string _originalWorkingDirectory;
        private List<string> _workingDirectoryHistory;
        private int _maxHistorySize = 10;

        // Session recovery
        private string _sessionLockFile;
        private string _crashRecoveryFile;
        private bool _isRecoveryMode;

        public static SessionManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                        {
                            _instance = new SessionManager();
                        }
                    }
                }
                return _instance;
            }
        }

        private SessionManager()
        {
            _workingDirectoryHistory = new List<string>();
            _originalWorkingDirectory = Directory.GetCurrentDirectory();
            SessionStartTime = DateTime.Now;
            IsSessionActive = false;
            HasUnsavedChanges = false;
        }

        /// <summary>
        /// Initialize a new session
        /// </summary>
        public bool InitializeSession(string projectPath = null)
        {
            try
            {
                // Check for existing session and recovery
                if (CheckForExistingSession())
                {
                    if (!HandleSessionRecovery())
                    {
                        return false;
                    }
                }

                // Set up working directory
                if (!string.IsNullOrEmpty(projectPath))
                {
                    if (!SetWorkingDirectory(projectPath))
                    {
                        return false;
                    }
                }
                else
                {
                    // Use default working directory
                    string defaultDir = GetDefaultWorkingDirectory();
                    if (!SetWorkingDirectory(defaultDir))
                    {
                        return false;
                    }
                }

                // Create session directories
                if (!CreateSessionDirectories())
                {
                    return false;
                }

                // Create session lock file
                CreateSessionLock();

                IsSessionActive = true;
                SessionStartTime = DateTime.Now;
                _isRecoveryMode = false;

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to initialize session: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Set the working directory for the session
        /// </summary>
        public bool SetWorkingDirectory(string path)
        {
            try
            {
                string absolutePath = FileOperations.GetAbsolutePath(path);
                
                if (!Directory.Exists(absolutePath))
                {
                    if (!FileOperations.CreateDirectory(absolutePath))
                    {
                        return false;
                    }
                }

                if (!FileOperations.IsDirectoryWritable(absolutePath))
                {
                    return false;
                }

                // Add to history
                if (!string.IsNullOrEmpty(WorkingDirectory))
                {
                    AddToWorkingDirectoryHistory(WorkingDirectory);
                }

                WorkingDirectory = absolutePath;
                CurrentProjectName = FileOperations.GetFileName(absolutePath);

                // Change to the working directory
                if (!FileOperations.ChangeDirectory(absolutePath))
                {
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to set working directory: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Create necessary session directories
        /// </summary>
        private bool CreateSessionDirectories()
        {
            try
            {
                // Session directory (for temporary files)
                SessionDirectory = Path.Combine(WorkingDirectory, ".session");
                if (!FileOperations.CreateDirectory(SessionDirectory))
                {
                    return false;
                }

                // Temp directory
                TempDirectory = Path.Combine(SessionDirectory, "temp");
                if (!FileOperations.CreateDirectory(TempDirectory))
                {
                    return false;
                }

                // Backup directory
                BackupDirectory = Path.Combine(SessionDirectory, "backup");
                if (!FileOperations.CreateDirectory(BackupDirectory))
                {
                    return false;
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Get default working directory
        /// </summary>
        private string GetDefaultWorkingDirectory()
        {
            // Try to use user's documents folder
            string documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            string defaultPath = Path.Combine(documentsPath, "OHRRPGCE");

            if (!Directory.Exists(defaultPath))
            {
                FileOperations.CreateDirectory(defaultPath);
            }

            return defaultPath;
        }

        /// <summary>
        /// Add directory to working directory history
        /// </summary>
        private void AddToWorkingDirectoryHistory(string path)
        {
            if (_workingDirectoryHistory.Contains(path))
            {
                _workingDirectoryHistory.Remove(path);
            }

            _workingDirectoryHistory.Insert(0, path);

            // Keep history size manageable
            if (_workingDirectoryHistory.Count > _maxHistorySize)
            {
                _workingDirectoryHistory.RemoveAt(_maxHistorySize);
            }
        }

        /// <summary>
        /// Get working directory history
        /// </summary>
        public List<string> GetWorkingDirectoryHistory()
        {
            return new List<string>(_workingDirectoryHistory);
        }

        /// <summary>
        /// Check for existing session
        /// </summary>
        private bool CheckForExistingSession()
        {
            try
            {
                string sessionLockPath = Path.Combine(WorkingDirectory ?? _originalWorkingDirectory, ".session", "session.lock");
                
                if (File.Exists(sessionLockPath))
                {
                    _sessionLockFile = sessionLockPath;
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
        /// Handle session recovery
        /// </summary>
        private bool HandleSessionRecovery()
        {
            try
            {
                if (string.IsNullOrEmpty(_sessionLockFile) || !File.Exists(_sessionLockFile))
                {
                    return true;
                }

                // Check if session is stale (older than 1 hour)
                var lockFileTime = FileOperations.GetFileModificationTime(_sessionLockFile);
                if (DateTime.Now.Subtract(lockFileTime).TotalHours > 1)
                {
                    // Stale session, remove lock
                    FileOperations.SafeDeleteFile(_sessionLockFile);
                    return true;
                }

                // Check for crash recovery file
                string crashRecoveryPath = Path.Combine(Path.GetDirectoryName(_sessionLockFile), "crash_recovery.dat");
                if (File.Exists(crashRecoveryPath))
                {
                    _crashRecoveryFile = crashRecoveryPath;
                    _isRecoveryMode = true;
                    
                    // Load recovery data
                    if (LoadCrashRecoveryData())
                    {
                        Console.WriteLine("Session recovery mode activated");
                        return true;
                    }
                }

                // Ask user what to do
                Console.WriteLine("Previous session detected. Starting new session...");
                FileOperations.SafeDeleteFile(_sessionLockFile);
                
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Load crash recovery data
        /// </summary>
        private bool LoadCrashRecoveryData()
        {
            try
            {
                if (string.IsNullOrEmpty(_crashRecoveryFile) || !File.Exists(_crashRecoveryFile))
                {
                    return false;
                }

                string[] recoveryData = FileOperations.ReadAllLines(_crashRecoveryFile);
                if (recoveryData.Length >= 3)
                {
                    // Parse recovery data
                    if (DateTime.TryParse(recoveryData[0], out DateTime lastSave))
                    {
                        LastSaveTime = lastSave;
                    }
                    
                    if (recoveryData.Length > 1)
                    {
                        LastLoadedFile = recoveryData[1];
                    }

                    if (recoveryData.Length > 2)
                    {
                        bool.TryParse(recoveryData[2], out bool hadUnsavedChanges);
                        HasUnsavedChanges = hadUnsavedChanges;
                    }

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
        /// Create session lock file
        /// </summary>
        private void CreateSessionLock()
        {
            try
            {
                string lockFilePath = Path.Combine(SessionDirectory, "session.lock");
                string lockContent = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss}\n{Environment.MachineName}\n{Environment.UserName}";
                
                FileOperations.WriteAllText(lockFilePath, lockContent);
                _sessionLockFile = lockFilePath;
            }
            catch
            {
                // Non-critical failure
            }
        }

        /// <summary>
        /// Update session state
        /// </summary>
        public void UpdateSessionState(string loadedFile = null, bool hasChanges = false)
        {
            if (loadedFile != null)
            {
                LastLoadedFile = loadedFile;
            }

            if (hasChanges)
            {
                HasUnsavedChanges = true;
            }

            // Update crash recovery file
            UpdateCrashRecoveryFile();
        }

        /// <summary>
        /// Update crash recovery file
        /// </summary>
        private void UpdateCrashRecoveryFile()
        {
            try
            {
                string recoveryFilePath = Path.Combine(SessionDirectory, "crash_recovery.dat");
                string[] recoveryData = new string[]
                {
                    DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    LastLoadedFile ?? "",
                    HasUnsavedChanges.ToString()
                };

                FileOperations.WriteAllLines(recoveryFilePath, recoveryData);
                _crashRecoveryFile = recoveryFilePath;
            }
            catch
            {
                // Non-critical failure
            }
        }

        /// <summary>
        /// Mark changes as saved
        /// </summary>
        public void MarkChangesSaved()
        {
            HasUnsavedChanges = false;
            LastSaveTime = DateTime.Now;
            UpdateCrashRecoveryFile();
        }

        /// <summary>
        /// Create backup of current state
        /// </summary>
        public bool CreateBackup(string description = "")
        {
            try
            {
                if (string.IsNullOrEmpty(WorkingDirectory))
                {
                    return false;
                }

                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string backupName = string.IsNullOrEmpty(description) ? $"backup_{timestamp}" : $"backup_{timestamp}_{description}";
                string backupPath = Path.Combine(BackupDirectory, backupName);

                if (!FileOperations.CreateDirectory(backupPath))
                {
                    return false;
                }

                // Copy working directory contents to backup
                if (!FileOperations.CopyDirectory(WorkingDirectory, backupPath, true))
                {
                    return false;
                }

                // Remove session directory from backup
                string backupSessionDir = Path.Combine(backupPath, ".session");
                FileOperations.SafeDeleteDirectory(backupSessionDir);

                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Clean up temporary files
        /// </summary>
        public void CleanupTempFiles()
        {
            try
            {
                if (!string.IsNullOrEmpty(TempDirectory) && Directory.Exists(TempDirectory))
                {
                    var tempFiles = Directory.GetFiles(TempDirectory, "*.*", SearchOption.AllDirectories);
                    foreach (var file in tempFiles)
                    {
                        FileOperations.SafeDeleteFile(file);
                    }
                }
            }
            catch
            {
                // Non-critical failure
            }
        }

        /// <summary>
        /// End the current session
        /// </summary>
        public void EndSession()
        {
            try
            {
                if (IsSessionActive)
                {
                    // Clean up session files
                    CleanupTempFiles();
                    
                    // Remove session lock
                    if (!string.IsNullOrEmpty(_sessionLockFile) && File.Exists(_sessionLockFile))
                    {
                        FileOperations.SafeDeleteFile(_sessionLockFile);
                    }

                    // Remove crash recovery file
                    if (!string.IsNullOrEmpty(_crashRecoveryFile) && File.Exists(_crashRecoveryFile))
                    {
                        FileOperations.SafeDeleteFile(_crashRecoveryFile);
                    }

                    // Restore original working directory
                    if (!string.IsNullOrEmpty(_originalWorkingDirectory))
                    {
                        FileOperations.ChangeDirectory(_originalWorkingDirectory);
                    }

                    IsSessionActive = false;
                    WorkingDirectory = null;
                    SessionDirectory = null;
                    TempDirectory = null;
                    BackupDirectory = null;
                    CurrentProjectName = null;
                    LastLoadedFile = null;
                    HasUnsavedChanges = false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error ending session: {ex.Message}");
            }
        }

        /// <summary>
        /// Get session information
        /// </summary>
        public Dictionary<string, object> GetSessionInfo()
        {
            return new Dictionary<string, object>
            {
                ["WorkingDirectory"] = WorkingDirectory,
                ["SessionDirectory"] = SessionDirectory,
                ["CurrentProject"] = CurrentProjectName,
                ["SessionStartTime"] = SessionStartTime,
                ["LastLoadedFile"] = LastLoadedFile,
                ["LastSaveTime"] = LastSaveTime,
                ["HasUnsavedChanges"] = HasUnsavedChanges,
                ["IsRecoveryMode"] = _isRecoveryMode,
                ["SessionDuration"] = IsSessionActive ? DateTime.Now.Subtract(SessionStartTime) : TimeSpan.Zero
            };
        }

        /// <summary>
        /// Dispose of the session manager
        /// </summary>
        public void Dispose()
        {
            EndSession();
        }
    }
}
