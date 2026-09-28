using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace NewColoringbook.Core
{
    /// <summary>A stored fill: base color plus texture style. Null means uncolored (paper).</summary>
    public sealed class StoredFill
    {
        [JsonProperty("c")] public string Color { get; set; } = "#F4845F";
        [JsonProperty("t")] public string Texture { get; set; } = "solid";

        public StoredFill() { }

        public StoredFill(string color, string texture)
        {
            Color = color;
            Texture = Textures.Normalize(texture);
        }

        public StoredFill Clone() => new StoredFill(Color, Texture);
    }

    /// <summary>A persisted freehand stroke (points as x,y pairs in design units).</summary>
    public sealed class StoredStroke
    {
        [JsonProperty("c")] public string Color { get; set; } = "#000000";
        [JsonProperty("w")] public double Width { get; set; } = 22;
        [JsonProperty("p")] public List<double[]> Pts { get; set; } = new();
    }

    /// <summary>Coloring progress for one design (persisted).</summary>
    public sealed class DesignProgress
    {
        [JsonProperty("fills")] public Dictionary<string, StoredFill> Fills { get; set; } = new();
        [JsonProperty("strokes")] public List<StoredStroke> Strokes { get; set; } = new();
        [JsonProperty("completed")] public bool Completed { get; set; }
        [JsonProperty("updatedUtc")] public DateTime UpdatedUtc { get; set; } = DateTime.UtcNow;
        [JsonProperty("seconds")] public double SecondsSpent { get; set; }

        [JsonIgnore] public int ColoredCount => Fills.Count;

        public double Fraction(int totalRegions) => totalRegions <= 0 ? 0 : Math.Min(1.0, Fills.Count / (double)totalRegions);

        public int Percent(int totalRegions) => (int)Math.Round(Fraction(totalRegions) * 100);
    }

    /// <summary>User-facing app settings (persisted).</summary>
    public sealed class AppSettings
    {
        [JsonProperty("theme")] public string Theme { get; set; } = "Light";   // Light | Dark | System
        [JsonProperty("musicOn")] public bool MusicOn { get; set; } = false;
        [JsonProperty("volume")] public double Volume { get; set; } = 0.55;
        [JsonProperty("musicTrack")] public string MusicTrack { get; set; } = "calm_forest";
        [JsonProperty("texture")] public string Texture { get; set; } = "solid";
        [JsonProperty("color")] public string ActiveColor { get; set; } = "#F4845F";
    }

    /// <summary>Everything persisted between sessions (profile.json, Newtonsoft).</summary>
    public sealed class AppProfile
    {
        [JsonProperty("version")] public int Version { get; set; } = 1;
        [JsonProperty("designs")] public Dictionary<string, DesignProgress> Designs { get; set; } = new();
        [JsonProperty("recentColors")] public List<string> RecentColors { get; set; } = new();
        [JsonProperty("customSwatches")] public List<string> CustomSwatches { get; set; } = new();
        [JsonProperty("settings")] public AppSettings Settings { get; set; } = new();

        public DesignProgress ProgressFor(string designId)
        {
            if (!Designs.TryGetValue(designId, out var p))
            {
                p = new DesignProgress();
                Designs[designId] = p;
            }
            return p;
        }

        public void PushRecentColor(string hex)
        {
            RecentColors.RemoveAll(c => string.Equals(c, hex, StringComparison.OrdinalIgnoreCase));
            RecentColors.Insert(0, hex);
            if (RecentColors.Count > 14) RecentColors.RemoveRange(14, RecentColors.Count - 14);
        }
    }

    /// <summary>A freehand brush stroke in design coordinates.</summary>
    public sealed class Stroke
    {
        [JsonIgnore] public string Color { get; set; } = "#000000";
        [JsonIgnore] public double Width { get; set; } = 22;
        [JsonIgnore] public List<Windows.Foundation.Point> Points { get; } = new();

        public Stroke() { }

        public Stroke(string color, double width)
        {
            Color = color;
            Width = width;
        }

        public Windows.Foundation.Rect Bounds()
        {
            if (Points.Count == 0) return new Windows.Foundation.Rect();
            double x0 = double.MaxValue, y0 = double.MaxValue, x1 = double.MinValue, y1 = double.MinValue;
            foreach (var p in Points)
            {
                x0 = Math.Min(x0, p.X); y0 = Math.Min(y0, p.Y);
                x1 = Math.Max(x1, p.X); y1 = Math.Max(y1, p.Y);
            }
            return new Windows.Foundation.Rect(x0, y0, x1 - x0, y1 - y0);
        }
    }
}

