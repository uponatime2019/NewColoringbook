using System;
using System.IO;

namespace NewColoringbook.Services
{
    public static class StoragePaths
    {
        private static string? _appDataFolder;

        public static string AppDataFolder
        {
            get
            {
                if (_appDataFolder == null)
                {
                    try
                    {
                        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                        _appDataFolder = Path.Combine(localAppData, "NewColoringbook");
                        Directory.CreateDirectory(_appDataFolder);
                    }
                    catch
                    {
                        _appDataFolder = Path.Combine(Path.GetTempPath(), "NewColoringbook");
                        Directory.CreateDirectory(_appDataFolder);
                    }
                }
                return _appDataFolder;
            }
        }
    }
}
