using System;
using System.IO;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace OHRRPGCEDX.Utils
{
    /// <summary>
    /// File operations utility class for OHRRPGCE Custom
    /// </summary>
    public static class FileOperations
    {
        /// <summary>
        /// Check if a directory is writable
        /// </summary>
        public static bool IsDirectoryWritable(string path)
        {
            try
            {
                if (!Directory.Exists(path))
                    return false;

                // Try to create a temporary file to test write access
                string testFile = Path.Combine(path, "write_test.tmp");
                File.WriteAllText(testFile, "test");
                File.Delete(testFile);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Create a directory if it doesn't exist
        /// </summary>
        public static bool CreateDirectory(string path)
        {
            try
            {
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Safely delete a file
        /// </summary>
        public static bool SafeDeleteFile(string filePath)
        {
            try
            {
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Safely delete a directory and all contents
        /// </summary>
        public static bool SafeDeleteDirectory(string dirPath)
        {
            try
            {
                if (Directory.Exists(dirPath))
                {
                    Directory.Delete(dirPath, true);
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Create a touch file (empty file with current timestamp)
        /// </summary>
        public static bool TouchFile(string filePath)
        {
            try
            {
                File.WriteAllText(filePath, "");
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Get file modification time as DateTime
        /// </summary>
        public static DateTime GetFileModificationTime(string filePath)
        {
            try
            {
                if (File.Exists(filePath))
                {
                    return File.GetLastWriteTime(filePath);
                }
                return DateTime.MinValue;
            }
            catch
            {
                return DateTime.MinValue;
            }
        }

        /// <summary>
        /// Get directory modification time (latest file modification)
        /// </summary>
        public static DateTime GetDirectoryModificationTime(string dirPath)
        {
            try
            {
                if (!Directory.Exists(dirPath))
                    return DateTime.MinValue;

                DateTime latestTime = DateTime.MinValue;
                var files = Directory.GetFiles(dirPath, "*.*", SearchOption.AllDirectories);
                
                foreach (var file in files)
                {
                    var fileTime = File.GetLastWriteTime(file);
                    if (fileTime > latestTime)
                        latestTime = fileTime;
                }

                return latestTime;
            }
            catch
            {
                return DateTime.MinValue;
            }
        }

        /// <summary>
        /// Copy a file with error handling
        /// </summary>
        public static bool CopyFile(string sourcePath, string destPath, bool overwrite = true)
        {
            try
            {
                // Ensure destination directory exists
                string destDir = Path.GetDirectoryName(destPath);
                if (!string.IsNullOrEmpty(destDir))
                {
                    CreateDirectory(destDir);
                }

                File.Copy(sourcePath, destPath, overwrite);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Copy all files from one directory to another
        /// </summary>
        public static bool CopyDirectory(string sourceDir, string destDir, bool recursive = true)
        {
            try
            {
                if (!Directory.Exists(sourceDir))
                    return false;

                CreateDirectory(destDir);

                var files = Directory.GetFiles(sourceDir);
                foreach (var file in files)
                {
                    string fileName = Path.GetFileName(file);
                    string destFile = Path.Combine(destDir, fileName);
                    CopyFile(file, destFile);
                }

                if (recursive)
                {
                    var subDirs = Directory.GetDirectories(sourceDir);
                    foreach (var subDir in subDirs)
                    {
                        string dirName = Path.GetFileName(subDir);
                        string destSubDir = Path.Combine(destDir, dirName);
                        CopyDirectory(subDir, destSubDir, recursive);
                    }
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Find files matching a pattern
        /// </summary>
        public static string[] FindFiles(string directory, string pattern, bool recursive = false)
        {
            try
            {
                if (!Directory.Exists(directory))
                    return new string[0];

                var searchOption = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
                return Directory.GetFiles(directory, pattern, searchOption);
            }
            catch
            {
                return new string[0];
            }
        }

        /// <summary>
        /// Find directories matching a pattern
        /// </summary>
        public static string[] FindDirectories(string directory, string pattern, bool recursive = false)
        {
            try
            {
                if (!Directory.Exists(directory))
                    return new string[0];

                var searchOption = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
                return Directory.GetDirectories(directory, pattern, searchOption);
            }
            catch
            {
                return new string[0];
            }
        }

        /// <summary>
        /// Get file size in bytes
        /// </summary>
        public static long GetFileSize(string filePath)
        {
            try
            {
                if (File.Exists(filePath))
                {
                    var fileInfo = new FileInfo(filePath);
                    return fileInfo.Length;
                }
                return 0;
            }
            catch
            {
                return 0;
            }
        }

        /// <summary>
        /// Check if a file exists
        /// </summary>
        public static bool FileExists(string filePath)
        {
            return File.Exists(filePath);
        }

        /// <summary>
        /// Check if a directory exists
        /// </summary>
        public static bool DirectoryExists(string dirPath)
        {
            return Directory.Exists(dirPath);
        }

        /// <summary>
        /// Get the absolute path from a relative path
        /// </summary>
        public static string GetAbsolutePath(string path)
        {
            try
            {
                return Path.GetFullPath(path);
            }
            catch
            {
                return path;
            }
        }

        /// <summary>
        /// Get the directory name from a file path
        /// </summary>
        public static string GetDirectoryName(string filePath)
        {
            return Path.GetDirectoryName(filePath);
        }

        /// <summary>
        /// Get the file name from a file path
        /// </summary>
        public static string GetFileName(string filePath)
        {
            return Path.GetFileName(filePath);
        }

        /// <summary>
        /// Get the file name without extension
        /// </summary>
        public static string GetFileNameWithoutExtension(string filePath)
        {
            return Path.GetFileNameWithoutExtension(filePath);
        }

        /// <summary>
        /// Get the file extension
        /// </summary>
        public static string GetExtension(string filePath)
        {
            return Path.GetExtension(filePath);
        }

        /// <summary>
        /// Combine path components
        /// </summary>
        public static string CombinePath(params string[] paths)
        {
            return Path.Combine(paths);
        }

        /// <summary>
        /// Get the current working directory
        /// </summary>
        public static string GetCurrentDirectory()
        {
            return Directory.GetCurrentDirectory();
        }

        /// <summary>
        /// Change the current working directory
        /// </summary>
        public static bool ChangeDirectory(string path)
        {
            try
            {
                if (Directory.Exists(path))
                {
                    Directory.SetCurrentDirectory(path);
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
        /// Get a unique temporary directory path
        /// </summary>
        public static string GetUniqueTempDirectory(string basePath, string prefix = "working")
        {
            int index = 0;
            string tempPath;
            
            do
            {
                tempPath = Path.Combine(basePath, $"{prefix}{index}.tmp");
                index++;
            } while (Directory.Exists(tempPath));

            return tempPath;
        }

        /// <summary>
        /// Calculate MD5 hash of a file
        /// </summary>
        public static string CalculateFileHash(string filePath)
        {
            try
            {
                using (var md5 = MD5.Create())
                using (var stream = File.OpenRead(filePath))
                {
                    byte[] hash = md5.ComputeHash(stream);
                    return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
                }
            }
            catch
            {
                return string.Empty;
            }
        }

        /// <summary>
        /// Read all text from a file
        /// </summary>
        public static string ReadAllText(string filePath)
        {
            try
            {
                if (File.Exists(filePath))
                {
                    return File.ReadAllText(filePath);
                }
                return string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

        /// <summary>
        /// Write text to a file
        /// </summary>
        public static bool WriteAllText(string filePath, string content)
        {
            try
            {
                // Ensure directory exists
                string dir = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(dir))
                {
                    CreateDirectory(dir);
                }

                File.WriteAllText(filePath, content);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Read all lines from a file
        /// </summary>
        public static string[] ReadAllLines(string filePath)
        {
            try
            {
                if (File.Exists(filePath))
                {
                    return File.ReadAllLines(filePath);
                }
                return new string[0];
            }
            catch
            {
                return new string[0];
            }
        }

        /// <summary>
        /// Write lines to a file
        /// </summary>
        public static bool WriteAllLines(string filePath, string[] lines)
        {
            try
            {
                // Ensure directory exists
                string dir = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(dir))
                {
                    CreateDirectory(dir);
                }

                File.WriteAllLines(filePath, lines);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
