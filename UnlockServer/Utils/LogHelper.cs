using System;
using System.IO;
using System.Linq;
using System.Text;

namespace UnlockServer
{
    public static class LogHelper
    {
        private static readonly object lockObj = new object();
        private static string logFilePath;
        private static readonly int maxLogFileSizeInMB = 10;
        private static readonly int maxLogFileLines = 10000;
        private static int currentLineCount = 0;

        static LogHelper()
        {
            string logDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");
            if (!Directory.Exists(logDir))
            {
                Directory.CreateDirectory(logDir);
            }
            
            // 清理旧日志文件，仅保留最近的3个
            CleanupOldLogFiles(logDir, 3);
            
            logFilePath = Path.Combine(logDir, $"log_{DateTime.Now:yyyyMMdd}.txt");
            InitializeLineCount();
        }

        /// <summary>
        /// 清理旧日志文件，仅保留指定数量的最新日志文件
        /// </summary>
        /// <param name="logDir">日志目录</param>
        /// <param name="keepCount">保留的日志文件数量</param>
        private static void CleanupOldLogFiles(string logDir, int keepCount)
        {
            try
            {
                // 获取所有日志文件
                var logFiles = Directory.GetFiles(logDir, "log_*.txt")
                    .Select(f => new FileInfo(f))
                    .OrderByDescending(f => f.LastWriteTime)
                    .ToList();

                // 如果日志文件数量超过保留数量，删除多余的
                if (logFiles.Count > keepCount)
                {
                    var filesToDelete = logFiles.Skip(keepCount);
                    foreach (var file in filesToDelete)
                    {
                        try
                        {
                            file.Delete();
                            Console.WriteLine($"已删除旧日志文件: {file.Name}");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"删除日志文件失败 {file.Name}: {ex.Message}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"清理日志文件失败: {ex.Message}");
            }
        }

        private static void InitializeLineCount()
        {
            try
            {
                if (File.Exists(logFilePath))
                {
                    currentLineCount = File.ReadAllLines(logFilePath).Length;
                }
                else
                {
                    currentLineCount = 0;
                }
            }
            catch
            {
                currentLineCount = 0;
            }
        }

        public static void WriteLine(string message)
        {
            string logMessage = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}";
            
            Console.WriteLine(logMessage);
            
            WriteToFile(logMessage);
        }

        public static void WriteLine(string format, params object[] args)
        {
            WriteLine(string.Format(format, args));
        }

        private static void WriteToFile(string message)
        {
            try
            {
                lock (lockObj)
                {
                    CheckAndRotateLogFile();
                    
                    using (StreamWriter sw = new StreamWriter(logFilePath, true, Encoding.UTF8))
                    {
                        sw.WriteLine(message);
                    }
                    
                    currentLineCount++;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"写入日志文件失败: {ex.Message}");
            }
        }

        private static void CheckAndRotateLogFile()
        {
            try
            {
                FileInfo fileInfo = new FileInfo(logFilePath);
                bool needRotate = false;
                
                if (fileInfo.Exists)
                {
                    if (fileInfo.Length > maxLogFileSizeInMB * 1024 * 1024)
                    {
                        needRotate = true;
                    }
                    else if (currentLineCount >= maxLogFileLines)
                    {
                        needRotate = true;
                    }
                }
                
                if (needRotate)
                {
                    string backupPath = Path.Combine(
                        Path.GetDirectoryName(logFilePath),
                        $"log_{DateTime.Now:yyyyMMdd_HHmmss}.txt"
                    );
                    File.Move(logFilePath, backupPath);
                    currentLineCount = 0;
                }
            }
            catch
            {
            }
        }
    }
}
