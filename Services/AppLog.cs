using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;

namespace NewColoringbook.Services
{
    /// <summary>Best-effort file logger; never throws (per winui3_startup_crash_fix.md).</summary>
    public static class AppLog
    {
        private static readonly SemaphoreSlim Gate = new(1, 1);
        private static string? _dir;

        public static string Dir
        {
            get
            {
                if (_dir == null)
                {
                    try
                    {
                        var baseDir = StoragePaths.AppDataFolder;
                        _dir = Path.Combine(baseDir, "logs");
                        Directory.CreateDirectory(_dir);
                    }
                    catch
                    {
                        _dir = Path.GetTempPath();
                    }
                }
                return _dir;
            }
        }

        public static void Info(string msg) => Write("INFO ", msg);
        public static void Warn(string msg) => Write("WARN ", msg);
        public static void Error(string msg, Exception? ex = null) => Write("ERROR", ex == null ? msg : $"{msg}: {ex}");

        private static void Write(string level, string msg)
        {
            try
            {
                var line = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} [{level}] {msg}{Environment.NewLine}";
                Gate.Wait();
                try { File.AppendAllText(Path.Combine(Dir, "app.log"), line, Encoding.UTF8); }
                finally { Gate.Release(); }
                Debug.WriteLine("[NewColoringbook] " + line.TrimEnd());
            }
            catch
            {
                // Logger must never crash the app.
            }
        }
    }
}

