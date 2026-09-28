using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Threading.Tasks;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.Storage;
using NewColoringbook.Core;

namespace NewColoringbook.Services
{
    public sealed class MusicTrackInfo
    {
        public string Id { get; }
        public string Title { get; }
        public string? FileName { get; }

        public MusicTrackInfo(string id, string title, string? fileName = null)
        {
            Id = id;
            Title = title;
            FileName = fileName;
        }

        public override string ToString() => Title;
    }

    /// <summary>
    /// Ambient music player: supports multiple relaxing audio tracks (Rain, Ocean, Stream,
    /// Wind in Trees, Campfire, Piano Melody, Wind Chimes, and procedural Calm Forest).
    /// </summary>
    public sealed class MusicPlayer : IDisposable
    {
        public static readonly IReadOnlyList<MusicTrackInfo> Tracks = new List<MusicTrackInfo>
        {
            new("calm_forest", "Calm Forest (Ambient)"),
            new("rain", "Gentle Rain", "Rain.m4a"),
            new("ocean", "Ocean Waves", "OceanShore.m4a"),
            new("stream", "Forest Stream", "Stream.m4a"),
            new("wind_trees", "Wind in Trees", "WindInTrees.m4a"),
            new("campfire", "Campfire", "Campfire.m4a"),
            new("piano", "Piano Melody", "PianoMelody.mp3"),
            new("chimes", "Wind Chimes", "WindChimes.mp3"),
        };

        private MediaPlayer? _player;
        private Windows.Storage.Streams.IRandomAccessStream? _stream;
        private readonly SemaphoreSlim _initGate = new(1, 1);
        private bool _disposed;
        private double _volume = 0.55;
        private MusicTrackInfo _currentTrack = Tracks[0];

        public MusicTrackInfo CurrentTrack => _currentTrack;
        public string TrackName => _currentTrack.Title;
        public bool IsReady { get; private set; }
        public bool IsPlaying { get; private set; }
        public double Volume => _volume;

        public event Action? StateChanged;

        public static MusicTrackInfo GetTrackById(string? id)
        {
            if (string.IsNullOrEmpty(id)) return Tracks[0];
            foreach (var t in Tracks)
            {
                if (string.Equals(t.Id, id, StringComparison.OrdinalIgnoreCase))
                    return t;
            }
            return Tracks[0];
        }

        /// <summary>Prepare the player with the given or current track on a background thread.</summary>
        public async Task EnsureReadyAsync(MusicTrackInfo? track = null)
        {
            if (_disposed) return;
            var targetTrack = track ?? _currentTrack;
            await _initGate.WaitAsync();
            try
            {
                if (_disposed) return;
                _currentTrack = targetTrack;
                IsReady = await Task.Run(() => PrepareOnBackgroundThread(targetTrack));
                StateChanged?.Invoke();
            }
            catch (Exception ex)
            {
                AppLog.Error("MusicPlayer init failed (music disabled)", ex);
                IsReady = false;
            }
            finally
            {
                _initGate.Release();
            }
        }

        public async Task SelectTrackAsync(MusicTrackInfo track)
        {
            if (track == null || _disposed) return;
            bool wasPlaying = IsPlaying;
            await EnsureReadyAsync(track);
            if (wasPlaying)
            {
                await PlayAsync();
            }
        }

        private bool PrepareOnBackgroundThread(MusicTrackInfo track)
        {
            try
            {
                _stream?.Dispose();
                _stream = null;

                var player = _player ?? new MediaPlayer
                {
                    AutoPlay = false,
                    IsLoopingEnabled = true,
                    Volume = Math.Clamp(_volume, 0, 1),
                };

                if (!string.IsNullOrEmpty(track.FileName))
                {
                    string fullPath = Path.Combine(AppContext.BaseDirectory, "Assets", "Audio", track.FileName);
                    if (File.Exists(fullPath))
                    {
                        byte[] fileBytes = File.ReadAllBytes(fullPath);
                        var memStream = fileBytes.AsBuffer().AsStream().AsRandomAccessStream();
                        string contentType = track.FileName.EndsWith(".mp3", StringComparison.OrdinalIgnoreCase) ? "audio/mpeg" : "audio/mp4";
                        player.Source = MediaSource.CreateFromStream(memStream, contentType);
                        _stream = memStream;
                        _player = player;
                        return true;
                    }
                    try
                    {
                        player.Source = MediaSource.CreateFromUri(new Uri($"ms-appx:///Assets/Audio/{track.FileName}"));
                        _player = player;
                        return true;
                    }
                    catch { }
                }

                // Procedural Calm Forest track
                string mediaDir = Path.Combine(StoragePaths.AppDataFolder, "media");
                Directory.CreateDirectory(mediaDir);
                string wavPath = Path.Combine(mediaDir, "calm_forest.wav");

                var info = new FileInfo(wavPath);
                if (!info.Exists || info.Length < 100_000)
                {
                    byte[] synthesized = SynthesizeTrack();
                    File.WriteAllBytes(wavPath, synthesized);
                    AppLog.Info($"Synthesized ambient track ({synthesized.Length} bytes)");
                }

                byte[] bytes = File.ReadAllBytes(wavPath);
                var stream = bytes.AsBuffer().AsStream().AsRandomAccessStream();
                player.Source = MediaSource.CreateFromStream(stream, "audio/wav");
                _stream = stream;
                _player = player;
                return true;
            }
            catch (Exception ex)
            {
                AppLog.Error($"PrepareOnBackgroundThread for {track.Title} failed", ex);
                return false;
            }
        }

        public async Task PlayAsync()
        {
            await EnsureReadyAsync();
            if (_player == null) return;
            try
            {
                await Task.Run(() =>
                {
                    try { _player.Play(); }
                    catch (Exception ex) { AppLog.Error("MediaPlayer.Play failed", ex); }
                });
                IsPlaying = true;
                StateChanged?.Invoke();
            }
            catch (Exception ex)
            {
                AppLog.Error("MusicPlayer play failed", ex);
            }
        }

        public void Pause()
        {
            var player = _player;
            if (player == null) return;
            try
            {
                Task.Run(() =>
                {
                    try { player.Pause(); }
                    catch (Exception ex) { AppLog.Error("MediaPlayer.Pause failed", ex); }
                });
                IsPlaying = false;
                StateChanged?.Invoke();
            }
            catch (Exception ex)
            {
                AppLog.Error("MusicPlayer pause failed", ex);
            }
        }

        public async Task ToggleAsync()
        {
            if (IsPlaying) Pause();
            else await PlayAsync();
        }

        public void SetVolume(double v)
        {
            _volume = Math.Clamp(v, 0, 1);
            var player = _player;
            if (player == null) return;
            try
            {
                Task.Run(() =>
                {
                    try { player.Volume = _volume; }
                    catch (Exception ex) { AppLog.Error("MediaPlayer.Volume failed", ex); }
                });
            }
            catch (Exception ex)
            {
                AppLog.Error("MusicPlayer volume failed", ex);
            }
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            var player = _player;
            var stream = _stream;
            _player = null;
            _stream = null;
            try
            {
                Task.Run(() =>
                {
                    try
                    {
                        if (player != null)
                        {
                            player.Pause();
                            player.Source = null;
                            player.Dispose();
                        }
                        stream?.Dispose();
                    }
                    catch { }
                });
            }
            catch { }
        }

        // ---------------------------------------------------------------- synthesis

        private const int SampleRate = 44100;
        private const double LoopSeconds = 24.0;   // 4 chords x 6 s
        private const int TotalSamples = (int)(SampleRate * LoopSeconds);

        /// <summary>Render the full stereo 16-bit WAV file.</summary>
        public static byte[] SynthesizeTrack()
        {
            double[][] chords =
            {
                new[] { 130.81, 261.63, 329.63, 392.00, 493.88 },  // Cmaj7
                new[] { 110.00, 220.00, 261.63, 329.63, 392.00 },  // Am7
                new[] {  87.31, 174.61, 220.00, 261.63, 329.63 },  // Fmaj7
                new[] {  98.00, 196.00, 246.94, 293.66, 329.63 },  // G6
            };
            double[] bells = { 523.25, 587.33, 659.25, 783.99, 880.00 };  // C D E G A

            var rng = new Random(20240824);
            const double chordLen = LoopSeconds / 4.0;
            // deterministic bell schedule: per chord, 3 notes with times + pitches
            var bellChord = new int[12];
            var bellTime = new double[12];
            var bellFreq = new double[12];
            for (int c = 0; c < 4; c++)
            {
                for (int k = 0; k < 3; k++)
                {
                    int idx = c * 3 + k;
                    bellChord[idx] = c;
                    bellTime[idx] = 0.8 + k * 1.7 + rng.NextDouble() * 0.6;
                    bellFreq[idx] = bells[rng.Next(bells.Length)];
                }
            }

            double airLp = 0.0;
            double alpha = 1.0 - Math.Exp(-2.0 * Math.PI * 520.0 / SampleRate);
            var wav = new byte[44 + TotalSamples * 4];

            for (int n = 0; n < TotalSamples; n++)
            {
                double t = n / (double)SampleRate;
                int chord = Math.Min(3, (int)(t / chordLen));
                double tLocal = t - chord * chordLen;

                double envA = 1.8, envR = 2.4;
                double env = 1.0;
                if (tLocal < envA) env = 0.5 - 0.5 * Math.Cos(Math.PI * tLocal / envA);
                else if (tLocal > chordLen - envR) env = 0.5 + 0.5 * Math.Cos(Math.PI * (tLocal - (chordLen - envR)) / envR);

                double monoL = 0.0, monoR = 0.0;
                var notes = chords[chord];
                for (int i = 0; i < notes.Length; i++)
                {
                    double f = notes[i];
                    double amp = i == 0 ? 0.30 : 0.20;
                    double w = 2.0 * Math.PI * f * t;
                    double wl = 2.0 * Math.PI * f * 1.0015 * t;      // gentle stereo detune
                    monoL += amp * (Math.Sin(w) + 0.34 * Math.Sin(2 * w) + 0.11 * Math.Sin(3 * w));
                    monoR += amp * (Math.Sin(wl) + 0.34 * Math.Sin(2 * wl) + 0.11 * Math.Sin(3 * wl));
                }
                monoL *= env * 0.20;
                monoR *= env * 0.20;

                // sparse bells
                for (int b = 0; b < 12; b++)
                {
                    if (bellChord[b] != chord) continue;
                    double bt = tLocal - bellTime[b];
                    if (bt < 0 || bt > 3.5) continue;
                    double decay = Math.Exp(-bt / 0.9);
                    double attack = bt < 0.02 ? bt / 0.02 : 1.0;
                    double wb = 2.0 * Math.PI * bellFreq[b] * t;
                    double s = 0.11 * attack * decay * (Math.Sin(wb) + 0.2 * Math.Sin(2 * wb));
                    double pan = 0.5 + 0.4 * Math.Sin(b * 2.1);   // spread across the field
                    monoL += s * (1.0 - pan * 0.6);
                    monoR += s * (0.4 + pan * 0.6);
                }

                // forest air: low-passed noise, slowly breathing
                double noise = (rng.NextDouble() * 2.0 - 1.0) * 0.05;
                airLp += alpha * (noise - airLp);
                double breath = 0.6 + 0.4 * Math.Sin(2.0 * Math.PI * t / 19.0);
                double air = airLp * 0.9 * breath;

                double L = Math.Tanh((monoL + air) * 1.2) * 0.72;
                double R = Math.Tanh((monoR + air * 0.94) * 1.2) * 0.72;

                // loop-boundary crossfade so the seam is inaudible
                const double fade = 0.14;
                double fadeMul = 1.0;
                if (t < fade) fadeMul = 0.5 - 0.5 * Math.Cos(Math.PI * t / fade);
                else if (t > LoopSeconds - fade) fadeMul = 0.5 + 0.5 * Math.Cos(Math.PI * (t - (LoopSeconds - fade)) / fade);
                L *= fadeMul;
                R *= fadeMul;

                short sl = (short)Math.Clamp((int)Math.Round(L * 32000), short.MinValue, short.MaxValue);
                short sr = (short)Math.Clamp((int)Math.Round(R * 32000), short.MinValue, short.MaxValue);
                int off = 44 + n * 4;
                wav[off] = (byte)(sl & 0xFF);
                wav[off + 1] = (byte)((sl >> 8) & 0xFF);
                wav[off + 2] = (byte)(sr & 0xFF);
                wav[off + 3] = (byte)((sr >> 8) & 0xFF);
            }

            WriteWavHeader(wav, TotalSamples);
            return wav;
        }

        private static void WriteWavHeader(byte[] wav, int samples)
        {
            int dataLen = samples * 4;
            void Ascii(int offset, string s)
            {
                for (int i = 0; i < s.Length; i++) wav[offset + i] = (byte)s[i];
            }
            void LE32(int offset, int v)
            {
                wav[offset] = (byte)(v & 0xFF);
                wav[offset + 1] = (byte)((v >> 8) & 0xFF);
                wav[offset + 2] = (byte)((v >> 16) & 0xFF);
                wav[offset + 3] = (byte)((v >> 24) & 0xFF);
            }
            void LE16(int offset, short v)
            {
                wav[offset] = (byte)(v & 0xFF);
                wav[offset + 1] = (byte)((v >> 8) & 0xFF);
            }
            Ascii(0, "RIFF");
            LE32(4, 36 + dataLen);
            Ascii(8, "WAVE");
            Ascii(12, "fmt ");
            LE32(16, 16);
            LE16(20, 1);            // PCM
            LE16(22, 2);            // stereo
            LE32(24, SampleRate);
            LE32(28, SampleRate * 4);
            LE16(32, 4);            // block align
            LE16(34, 16);           // bits per sample
            Ascii(36, "data");
            LE32(40, dataLen);
        }
    }
}

