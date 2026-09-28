using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NewColoringbook.Core;

namespace NewColoringbook.Services
{
    /// <summary>
    /// Loads/saves the user profile (progress, palettes, settings) as JSON via Newtonsoft.Json.
    /// Never uses System.Text.Json. Saves are debounced and atomic (temp file + move).
    /// </summary>
    public sealed class ProfileStore
    {
        private readonly SemaphoreSlim _gate = new(1, 1);
        private readonly string _path;
        private readonly string _tmpPath;
        private CancellationTokenSource? _pending;

        public AppProfile Profile { get; private set; } = new();

        public ProfileStore()
        {
            try
            {
                var dir = StoragePaths.AppDataFolder;
                _path = Path.Combine(dir, "profile.json");
            }
            catch
            {
                _path = Path.Combine(Path.GetTempPath(), "coloringbook_profile.json");
            }
            _tmpPath = _path + ".tmp";
            Load();
        }

        public void Load()
        {
            try
            {
                if (File.Exists(_path))
                {
                    var json = File.ReadAllText(_path);
                    var loaded = JsonConvert.DeserializeObject<AppProfile>(json);
                    if (loaded != null)
                    {
                        Profile = loaded;
                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                AppLog.Error("Profile load failed; starting fresh", ex);
            }
            Profile = new AppProfile();
        }

        public string FilePath => _path;

        /// <summary>Save now (blocking, atomic).</summary>
        public void Save()
        {
            try
            {
                _pending?.Cancel();
                var json = JsonConvert.SerializeObject(Profile, Formatting.Indented);
                System.Diagnostics.Debug.WriteLine($"[ProfileStore.Save] Saving profile to: {_path}");
                System.Diagnostics.Debug.WriteLine($"[ProfileStore.Save] Total designs with data: {Profile.Designs.Count}");
                foreach (var kvp in Profile.Designs)
                {
                    System.Diagnostics.Debug.WriteLine($"  - Design '{kvp.Key}': {kvp.Value.Fills.Count} fills, {kvp.Value.Strokes.Count} strokes, updated: {kvp.Value.UpdatedUtc:HH:mm:ss}");
                }
                File.WriteAllText(_tmpPath, json);
                if (File.Exists(_path)) File.Replace(_tmpPath, _path, null);
                else File.Move(_tmpPath, _path);
                System.Diagnostics.Debug.WriteLine($"[ProfileStore.Save] Successfully wrote {json.Length} bytes to: {_path}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ProfileStore.Save] ERROR: {ex.Message}");
                AppLog.Error("Profile save failed", ex);
            }
        }

        /// <summary>Debounced save — collapses bursts of edits into one write.</summary>
        public void SaveDebounced(int delayMs, Windows.System.DispatcherQueue? queue = null)
        {
            try
            {
                _pending?.Cancel();
                _pending = new CancellationTokenSource();
                var token = _pending.Token;
                var delay = Task.Delay(delayMs, token)
                    .ContinueWith(_ =>
                    {
                        if (!token.IsCancellationRequested) Save();
                    }, TaskScheduler.Default);
            }
            catch (Exception ex)
            {
                AppLog.Error("Debounced save schedule failed", ex);
            }
        }

        /// <summary>Replace the whole profile (import) and persist.</summary>
        public void ImportProfile(AppProfile imported)
        {
            Profile = imported ?? new AppProfile();
            Save();
        }

        public string CurrentJson => JsonConvert.SerializeObject(Profile, Formatting.Indented);
    }
}

