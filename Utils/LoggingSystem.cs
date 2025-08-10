using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using OHRRPGCEDX.Configuration;

namespace OHRRPGCEDX.Utils
{
    /// <summary>
    /// Logging levels for the OHRRPGCE Custom system
    /// </summary>
    public enum LogLevel
    {
        Debug = 0,
        Info = 1,
        Warning = 2,
        Error = 3,
        Critical = 4
    }

    /// <summary>
    /// Log entry structure
    /// </summary>
    public class LogEntry
    {
        public DateTime Timestamp { get; set; }
        public LogLevel Level { get; set; }
        public string Category { get; set; }
        public string Message { get; set; }
        public string Source { get; set; }
        public int ThreadId { get; set; }
        public Exception Exception { get; set; }

        public LogEntry(LogLevel level, string category, string message, string source = null, Exception exception = null)
        {
            Timestamp = DateTime.Now;
            Level = level;
            Category = category;
            Message = message;
            Source = source;
            ThreadId = Thread.CurrentThread.ManagedThreadId;
            Exception = exception;
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append($"[{Timestamp:yyyy-MM-dd HH:mm:ss.fff}] ");
            sb.Append($"[{Level.ToString().ToUpper()}] ");
            sb.Append($"[{Category}] ");
            
            if (!string.IsNullOrEmpty(Source))
            {
                sb.Append($"[{Source}] ");
            }
            
            sb.Append($"[TID:{ThreadId}] ");
            sb.Append(Message);

            if (Exception != null)
            {
                sb.Append($" | Exception: {Exception.Message}");
                if (Exception.StackTrace != null)
                {
                    sb.Append($" | Stack: {Exception.StackTrace}");
                }
            }

            return sb.ToString();
        }
    }

    /// <summary>
    /// Logging system for OHRRPGCE Custom
    /// </summary>
    public class LoggingSystem
    {
        private static LoggingSystem _instance;
        private static readonly object _lock = new object();

        // Logging configuration
        private LogLevel _minimumLogLevel;
        private bool _logToFile;
        private bool _logToConsole;
        private bool _logToDebug;
        private string _logFilePath;
        private int _maxLogFileSize; // MB
        private int _maxLogFiles;
        private bool _includeTimestamp;
        private bool _includeThreadId;
        private bool _includeSource;

        // Logging state
        private Queue<LogEntry> _logQueue;
        private List<LogEntry> _logHistory;
        private int _maxHistorySize;
        private bool _isInitialized;
        private Thread _logWriterThread;
        private bool _shouldStopLogWriter;
        private AutoResetEvent _logEvent;

        // Performance tracking
        private Dictionary<string, long> _performanceCounters;
        private Dictionary<string, DateTime> _performanceTimers;

        public static LoggingSystem Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                        {
                            _instance = new LoggingSystem();
                        }
                    }
                }
                return _instance;
            }
        }

        private LoggingSystem()
        {
            _logQueue = new Queue<LogEntry>();
            _logHistory = new List<LogEntry>();
            _performanceCounters = new Dictionary<string, long>();
            _performanceTimers = new Dictionary<string, DateTime>();
            _logEvent = new AutoResetEvent(false);
            _isInitialized = false;
            _shouldStopLogWriter = false;
        }

        /// <summary>
        /// Initialize the logging system
        /// </summary>
        public bool Initialize(string logPath = null)
        {
            try
            {
                if (_isInitialized)
                    return true;

                // Load configuration
                LoadLoggingConfiguration();

                // Set up log file path
                if (string.IsNullOrEmpty(logPath))
                {
                    string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                    string ohrrpgcePath = Path.Combine(appDataPath, "OHRRPGCE", "Logs");
                    
                    if (!Directory.Exists(ohrrpgcePath))
                    {
                        FileOperations.CreateDirectory(ohrrpgcePath);
                    }

                    _logFilePath = Path.Combine(ohrrpgcePath, $"custom_{DateTime.Now:yyyyMMdd}.log");
                }
                else
                {
                    _logFilePath = logPath;
                }

                // Start log writer thread
                StartLogWriterThread();

                _isInitialized = true;
                Log(LogLevel.Info, "System", "Logging system initialized successfully");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to initialize logging system: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Load logging configuration from ConfigurationManager
        /// </summary>
        private void LoadLoggingConfiguration()
        {
            try
            {
                var config = ConfigurationManager.Instance;
                
                _minimumLogLevel = (LogLevel)Enum.Parse(typeof(LogLevel), 
                    config.GetValue<string>("log_level", "info"), true);
                _logToFile = config.GetValue<bool>("log_to_file", true);
                _logToConsole = config.GetValue<bool>("log_to_console", true);
                _logToDebug = config.GetValue<bool>("log_to_debug", false);
                _maxLogFileSize = config.GetValue<int>("max_log_file_size", 10);
                _maxLogFiles = config.GetValue<int>("max_log_files", 5);
                _includeTimestamp = config.GetValue<bool>("log_include_timestamp", true);
                _includeThreadId = config.GetValue<bool>("log_include_thread_id", true);
                _includeSource = config.GetValue<bool>("log_include_source", true);
                _maxHistorySize = config.GetValue<int>("log_history_size", 1000);
            }
            catch
            {
                // Use default values if configuration fails
                _minimumLogLevel = LogLevel.Info;
                _logToFile = true;
                _logToConsole = true;
                _logToDebug = false;
                _maxLogFileSize = 10;
                _maxLogFiles = 5;
                _includeTimestamp = true;
                _includeThreadId = true;
                _includeSource = true;
                _maxHistorySize = 1000;
            }
        }

        /// <summary>
        /// Start the log writer thread
        /// </summary>
        private void StartLogWriterThread()
        {
            _logWriterThread = new Thread(LogWriterWorker)
            {
                IsBackground = true,
                Name = "LogWriter"
            };
            _logWriterThread.Start();
        }

        /// <summary>
        /// Log writer worker thread
        /// </summary>
        private void LogWriterWorker()
        {
            while (!_shouldStopLogWriter)
            {
                _logEvent.WaitOne(100); // Wait for log events or timeout

                while (_logQueue.Count > 0)
                {
                    LogEntry entry = null;
                    lock (_logQueue)
                    {
                        if (_logQueue.Count > 0)
                        {
                            entry = _logQueue.Dequeue();
                        }
                    }

                    if (entry != null)
                    {
                        ProcessLogEntry(entry);
                    }
                }
            }
        }

        /// <summary>
        /// Process a log entry
        /// </summary>
        private void ProcessLogEntry(LogEntry entry)
        {
            try
            {
                // Add to history
                AddToHistory(entry);

                // Write to console if enabled
                if (_logToConsole && entry.Level >= _minimumLogLevel)
                {
                    WriteToConsole(entry);
                }

                // Write to file if enabled
                if (_logToFile && entry.Level >= _minimumLogLevel)
                {
                    WriteToFile(entry);
                }

                // Write to debug output if enabled
                if (_logToDebug && entry.Level >= _minimumLogLevel)
                {
                    System.Diagnostics.Debug.WriteLine(entry.ToString());
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing log entry: {ex.Message}");
            }
        }

        /// <summary>
        /// Add log entry to history
        /// </summary>
        private void AddToHistory(LogEntry entry)
        {
            lock (_logHistory)
            {
                _logHistory.Add(entry);

                // Trim history if it exceeds maximum size
                while (_logHistory.Count > _maxHistorySize)
                {
                    _logHistory.RemoveAt(0);
                }
            }
        }

        /// <summary>
        /// Write log entry to console
        /// </summary>
        private void WriteToConsole(LogEntry entry)
        {
            var originalColor = Console.ForegroundColor;
            
            // Set color based on log level
            switch (entry.Level)
            {
                case LogLevel.Debug:
                    Console.ForegroundColor = ConsoleColor.Gray;
                    break;
                case LogLevel.Info:
                    Console.ForegroundColor = ConsoleColor.White;
                    break;
                case LogLevel.Warning:
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    break;
                case LogLevel.Error:
                    Console.ForegroundColor = ConsoleColor.Red;
                    break;
                case LogLevel.Critical:
                    Console.ForegroundColor = ConsoleColor.Magenta;
                    break;
            }

            Console.WriteLine(entry.ToString());
            Console.ForegroundColor = originalColor;
        }

        /// <summary>
        /// Write log entry to file
        /// </summary>
        private void WriteToFile(LogEntry entry)
        {
            try
            {
                // Check if log file rotation is needed
                if (ShouldRotateLogFile())
                {
                    RotateLogFiles();
                }

                // Append to log file
                string logLine = entry.ToString() + Environment.NewLine;
                File.AppendAllText(_logFilePath, logLine);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to write to log file: {ex.Message}");
            }
        }

        /// <summary>
        /// Check if log file rotation is needed
        /// </summary>
        private bool ShouldRotateLogFile()
        {
            try
            {
                if (!File.Exists(_logFilePath))
                    return false;

                var fileInfo = new FileInfo(_logFilePath);
                return fileInfo.Length > (_maxLogFileSize * 1024 * 1024); // Convert MB to bytes
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Rotate log files
        /// </summary>
        private void RotateLogFiles()
        {
            try
            {
                // Remove oldest log file if we have too many
                for (int i = _maxLogFiles - 1; i >= 0; i--)
                {
                    string oldLogFile = _logFilePath.Replace(".log", $"_{i}.log");
                    if (File.Exists(oldLogFile))
                    {
                        if (i == _maxLogFiles - 1)
                        {
                            FileOperations.SafeDeleteFile(oldLogFile);
                        }
                        else
                        {
                            string newLogFile = _logFilePath.Replace(".log", $"_{i + 1}.log");
                            File.Move(oldLogFile, newLogFile);
                        }
                    }
                }

                // Rename current log file
                string currentLogFile = _logFilePath.Replace(".log", "_0.log");
                if (File.Exists(_logFilePath))
                {
                    File.Move(_logFilePath, currentLogFile);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to rotate log files: {ex.Message}");
            }
        }

        /// <summary>
        /// Log a message
        /// </summary>
        public void Log(LogLevel level, string category, string message, string source = null, Exception exception = null)
        {
            if (!_isInitialized)
            {
                Initialize();
            }

            if (level < _minimumLogLevel)
                return;

            var entry = new LogEntry(level, category, message, source, exception);

            lock (_logQueue)
            {
                _logQueue.Enqueue(entry);
            }

            _logEvent.Set();
        }

        /// <summary>
        /// Log convenience methods
        /// </summary>
        public void Debug(string category, string message, string source = null) => Log(LogLevel.Debug, category, message, source);
        public void Info(string category, string message, string source = null) => Log(LogLevel.Info, category, message, source);
        public void Warning(string category, string message, string source = null) => Log(LogLevel.Warning, category, message, source);
        public void Error(string category, string message, string source = null, Exception exception = null) => Log(LogLevel.Error, category, message, source, exception);
        public void Critical(string category, string message, string source = null, Exception exception = null) => Log(LogLevel.Critical, category, message, source, exception);

        /// <summary>
        /// Start performance timer
        /// </summary>
        public void StartTimer(string timerName)
        {
            lock (_performanceTimers)
            {
                _performanceTimers[timerName] = DateTime.Now;
            }
        }

        /// <summary>
        /// Stop performance timer and log duration
        /// </summary>
        public void StopTimer(string timerName, string category = "Performance")
        {
            try
            {
                DateTime startTime;
                lock (_performanceTimers)
                {
                    if (!_performanceTimers.TryGetValue(timerName, out startTime))
                    {
                        return;
                    }
                    _performanceTimers.Remove(timerName);
                }

                var duration = DateTime.Now.Subtract(startTime);
                Log(LogLevel.Debug, category, $"Timer '{timerName}' completed in {duration.TotalMilliseconds:F2}ms");
            }
            catch
            {
                // Ignore timer errors
            }
}

        /// <summary>
        /// Increment performance counter
        /// </summary>
        public void IncrementCounter(string counterName, long increment = 1)
        {
            lock (_performanceCounters)
            {
                if (_performanceCounters.ContainsKey(counterName))
                {
                    _performanceCounters[counterName] += increment;
                }
                else
                {
                    _performanceCounters[counterName] = increment;
                }
            }
        }

        /// <summary>
        /// Get performance counter value
        /// </summary>
        public long GetCounter(string counterName)
        {
            lock (_performanceCounters)
            {
                return _performanceCounters.TryGetValue(counterName, out long value) ? value : 0;
            }
        }

        /// <summary>
        /// Get all performance counters
        /// </summary>
        public Dictionary<string, long> GetAllCounters()
        {
            lock (_performanceCounters)
            {
                return new Dictionary<string, long>(_performanceCounters);
            }
        }

        /// <summary>
        /// Get log history
        /// </summary>
        public List<LogEntry> GetLogHistory()
        {
            lock (_logHistory)
            {
                return new List<LogEntry>(_logHistory);
            }
        }

        /// <summary>
        /// Clear log history
        /// </summary>
        public void ClearLogHistory()
        {
            lock (_logHistory)
            {
                _logHistory.Clear();
            }
        }

        /// <summary>
        /// Get log statistics
        /// </summary>
        public Dictionary<string, object> GetLogStatistics()
        {
            lock (_logHistory)
            {
                var stats = new Dictionary<string, object>();
                var levelCounts = new Dictionary<LogLevel, int>();

                foreach (var level in Enum.GetValues(typeof(LogLevel)))
                {
                    levelCounts[(LogLevel)level] = 0;
                }

                foreach (var entry in _logHistory)
                {
                    levelCounts[entry.Level]++;
                }

                stats["TotalEntries"] = _logHistory.Count;
                stats["LevelCounts"] = levelCounts;
                stats["OldestEntry"] = _logHistory.Count > 0 ? _logHistory[0].Timestamp : (DateTime?)null;
                stats["NewestEntry"] = _logHistory.Count > 0 ? _logHistory[_logHistory.Count - 1].Timestamp : (DateTime?)null;
                stats["QueueSize"] = _logQueue.Count;

                return stats;
            }
        }

        /// <summary>
        /// Shutdown the logging system
        /// </summary>
        public void Shutdown()
        {
            try
            {
                _shouldStopLogWriter = true;
                _logEvent.Set();

                if (_logWriterThread != null && _logWriterThread.IsAlive)
                {
                    _logWriterThread.Join(5000); // Wait up to 5 seconds
                }

                // Process remaining log entries
                while (_logQueue.Count > 0)
                {
                    var entry = _logQueue.Dequeue();
                    ProcessLogEntry(entry);
                }

                _isInitialized = false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error shutting down logging system: {ex.Message}");
            }
        }

        /// <summary>
        /// Dispose of the logging system
        /// </summary>
        public void Dispose()
        {
            Shutdown();
            _logEvent?.Dispose();
        }
    }
}
