using System;
using System.Collections.Generic;
using System.Globalization;

namespace NewColoringbook.Core
{
    /// <summary>RGB/HSV color math on plain hex strings ("#RRGGBB") shared by mixer, textures and storage.</summary>
    public static class ColorMath
    {
        public static (byte R, byte G, byte B) FromHex(string hex)
        {
            hex = hex.TrimStart('#');
            if (hex.Length == 8) hex = hex.Substring(2);
            if (hex.Length != 6) return (128, 128, 128);
            return (
                byte.Parse(hex.Substring(0, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture),
                byte.Parse(hex.Substring(2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture),
                byte.Parse(hex.Substring(4, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture));
        }

        public static string ToHex(byte r, byte g, byte b) =>
            "#" + r.ToString("X2", CultureInfo.InvariantCulture)
                + g.ToString("X2", CultureInfo.InvariantCulture)
                + b.ToString("X2", CultureInfo.InvariantCulture);

        public static (double H, double S, double V) ToHsv(byte r, byte g, byte b)
        {
            double rd = r / 255.0, gd = g / 255.0, bd = b / 255.0;
            double max = Math.Max(rd, Math.Max(gd, bd)), min = Math.Min(rd, Math.Min(gd, bd));
            double d = max - min;
            double h = 0;
            if (d > 1e-9)
            {
                if (max == rd) h = 60 * (((gd - bd) / d) % 6);
                else if (max == gd) h = 60 * ((bd - rd) / d + 2);
                else h = 60 * ((rd - gd) / d + 4);
                if (h < 0) h += 360;
            }
            double s = max <= 1e-9 ? 0 : d / max;
            return (h, s * 100, max * 100);
        }

        public static (byte R, byte G, byte B) FromHsv(double h, double sPct, double vPct)
        {
            h = ((h % 360) + 360) % 360;
            double s = Math.Clamp(sPct / 100.0, 0, 1);
            double v = Math.Clamp(vPct / 100.0, 0, 1);
            double c = v * s;
            double x = c * (1 - Math.Abs((h / 60) % 2 - 1));
            double m = v - c;
            double rd = 0, gd = 0, bd = 0;
            if (h < 60) { rd = c; gd = x; }
            else if (h < 120) { rd = x; gd = c; }
            else if (h < 180) { gd = c; bd = x; }
            else if (h < 240) { gd = x; bd = c; }
            else if (h < 300) { rd = x; bd = c; }
            else { rd = c; bd = x; }
            return ((byte)Math.Round((rd + m) * 255), (byte)Math.Round((gd + m) * 255), (byte)Math.Round((bd + m) * 255));
        }

        /// <summary>Multiply brightness by factor (clamped).</summary>
        public static string Shade(string hex, double factor)
        {
            var (r, g, b) = FromHex(hex);
            return ToHex((byte)Math.Clamp(r * factor, 0, 255), (byte)Math.Clamp(g * factor, 0, 255), (byte)Math.Clamp(b * factor, 0, 255));
        }

        /// <summary>Mix toward white by ratio (0..1).</summary>
        public static string Tint(string hex, double ratio)
        {
            var (r, g, b) = FromHex(hex);
            return ToHex(
                (byte)Math.Round(r + (255 - r) * ratio),
                (byte)Math.Round(g + (255 - g) * ratio),
                (byte)Math.Round(b + (255 - b) * ratio));
        }
    }

    /// <summary>Catalog of preset swatch palettes shown in the palette panel and mixer.</summary>
    public static class Palettes
    {
        public sealed class PaletteDef
        {
            public string Name { get; }
            public string[] Colors { get; }
            public PaletteDef(string name, string[] colors) { Name = name; Colors = colors; }
        }

        public static readonly PaletteDef[] All = new PaletteDef[]
        {
            new("Coral Warm", new[]
            {
                "#F4845F", "#DC6B4A", "#B85042", "#F9B98A", "#F5D5AE", "#E9B44C", "#F3A6A6", "#8C4A3C"
            }),
            new("Ocean Calm", new[]
            {
                "#0E4D64", "#177E89", "#3AA8A1", "#8FD6BD", "#BEE3DB", "#577399", "#29539B", "#DDE7F0"
            }),
            new("Forest Bath", new[]
            {
                "#20483C", "#3C6E4F", "#6A994E", "#A3B18A", "#DAD7CD", "#8C6E3C", "#BC6C25", "#606C38"
            }),
            new("Bloom Garden", new[]
            {
                "#9D4EDD", "#C77DFF", "#E0AAFF", "#F7C8E0", "#FF7AA2", "#D6336C", "#6F42C1", "#FDEFF9"
            }),
            new("Sunset Dusk", new[]
            {
                "#E63946", "#F4A261", "#FFB703", "#FDCA40", "#F28482", "#9D4EDD", "#3C096C", "#FFF3E2"
            }),
            new("Pastel Dream", new[]
            {
                "#FFADAD", "#FFD6A5", "#FDFFB6", "#CAFFBF", "#9BF6FF", "#A0C4FF", "#BDB2FF", "#FFC6FF"
            }),
            new("Earth Clay", new[]
            {
                "#582F0E", "#7F4F24", "#936639", "#A68A64", "#B6AD90", "#C2C5AA", "#A4AC86", "#6B705C"
            }),
            new("Jewel Box", new[]
            {
                "#006466", "#06A77D", "#D0F4DE", "#8ECAE6", "#3D5A80", "#9D0208", "#BC4749", "#E9C46A"
            }),
        };

        /// <summary>Deterministic pleasant preview color for gallery thumbnails (by region index).</summary>
        public static string PreviewColor(int regionIndex, int total)
        {
            string[] soft =
            {
                "#F6BD7E", "#F49C7C", "#F08E8E", "#E8A0C8", "#B8A7E8", "#93B8E8", "#8CC7E0", "#96D8B2",
                "#CFE59A", "#F2DE9B", "#E4C48A", "#D9B3B3", "#A8C6C1", "#B5C9E8", "#D5B8E8", "#F4B8C4",
            };
            return soft[regionIndex % soft.Length];
        }
    }

    /// <summary>Texture styles a fill can carry; brushes are built from the base color in code.</summary>
    public static class Textures
    {
        public sealed class TextureDef
        {
            public string Id { get; }
            public string Name { get; }
            public TextureDef(string id, string name) { Id = id; Name = name; }
        }

        public static readonly TextureDef[] All = new TextureDef[]
        {
            new TextureDef("solid", "Solid"),
            new TextureDef("shade", "Soft Shade"),
            new TextureDef("pearl", "Pearl"),
            new TextureDef("stripe", "Stripes"),
            new TextureDef("glow", "Glow"),
            new TextureDef("velvet", "Velvet"),
        };

        public static string NameOf(string? id)
        {
            foreach (var t in All) if (t.Id == id) return t.Name;
            return "Solid";
        }

        public static string Normalize(string? id)
        {
            if (string.IsNullOrEmpty(id)) return "solid";
            foreach (var t in All) if (t.Id == id) return t.Id;
            return "solid";
        }

        /// <summary>Build the fill brush for a texture style applied to a base color.</summary>
        public static Microsoft.UI.Xaml.Media.Brush BrushFor(string textureId, string baseHex)
        {
            var (r, g, b) = ColorMath.FromHex(baseHex);
            var color = Windows.UI.Color.FromArgb(255, r, g, b);
            switch (Normalize(textureId))
            {
                case "shade":
                {
                    var dark = ToColor(ColorMath.Shade(baseHex, 0.74));
                    var light = ToColor(ColorMath.Tint(baseHex, 0.16));
                    var brush = new Microsoft.UI.Xaml.Media.LinearGradientBrush();
                    brush.StartPoint = new Windows.Foundation.Point(0, 0);
                    brush.EndPoint = new Windows.Foundation.Point(1, 1);
                    brush.GradientStops.Add(new Microsoft.UI.Xaml.Media.GradientStop { Color = light, Offset = 0 });
                    brush.GradientStops.Add(new Microsoft.UI.Xaml.Media.GradientStop { Color = color, Offset = 0.55 });
                    brush.GradientStops.Add(new Microsoft.UI.Xaml.Media.GradientStop { Color = dark, Offset = 1 });
                    return brush;
                }
                case "pearl":
                {
                    var center = ToColor(ColorMath.Tint(baseHex, 0.42));
                    var brush = new Microsoft.UI.Xaml.Media.RadialGradientBrush
                    {
                        GradientOrigin = new Windows.Foundation.Point(0.4, 0.35),
                        Center = new Windows.Foundation.Point(0.5, 0.5),
                        RadiusX = 0.75,
                        RadiusY = 0.75,
                    };
                    brush.GradientStops.Add(new Microsoft.UI.Xaml.Media.GradientStop { Color = center, Offset = 0 });
                    brush.GradientStops.Add(new Microsoft.UI.Xaml.Media.GradientStop { Color = color, Offset = 1 });
                    return brush;
                }
                case "stripe":
                {
                    var dark = ToColor(ColorMath.Shade(baseHex, 0.72));
                    var brush = new Microsoft.UI.Xaml.Media.LinearGradientBrush();
                    brush.StartPoint = new Windows.Foundation.Point(0, 0);
                    brush.EndPoint = new Windows.Foundation.Point(1, 0);
                    for (int i = 0; i <= 6; i++)
                        brush.GradientStops.Add(new Microsoft.UI.Xaml.Media.GradientStop
                        {
                            Color = i % 2 == 0 ? color : dark,
                            Offset = i / 6.0,
                        });
                    return brush;
                }
                case "glow":
                {
                    var center = ToColor(ColorMath.Tint(baseHex, 0.55));
                    var edge = ToColor(ColorMath.Shade(baseHex, 0.92));
                    var brush = new Microsoft.UI.Xaml.Media.RadialGradientBrush
                    {
                        GradientOrigin = new Windows.Foundation.Point(0.5, 0.5),
                        Center = new Windows.Foundation.Point(0.5, 0.5),
                        RadiusX = 0.7,
                        RadiusY = 0.7,
                    };
                    brush.GradientStops.Add(new Microsoft.UI.Xaml.Media.GradientStop { Color = center, Offset = 0 });
                    brush.GradientStops.Add(new Microsoft.UI.Xaml.Media.GradientStop { Color = color, Offset = 0.6 });
                    brush.GradientStops.Add(new Microsoft.UI.Xaml.Media.GradientStop { Color = edge, Offset = 1 });
                    return brush;
                }
                case "velvet":
                {
                    var deep = ToColor(ColorMath.Shade(baseHex, 0.62));
                    var lift = ToColor(ColorMath.Tint(baseHex, 0.10));
                    var brush = new Microsoft.UI.Xaml.Media.LinearGradientBrush();
                    brush.StartPoint = new Windows.Foundation.Point(0, 1);
                    brush.EndPoint = new Windows.Foundation.Point(1, 0);
                    brush.GradientStops.Add(new Microsoft.UI.Xaml.Media.GradientStop { Color = deep, Offset = 0 });
                    brush.GradientStops.Add(new Microsoft.UI.Xaml.Media.GradientStop { Color = color, Offset = 0.45 });
                    brush.GradientStops.Add(new Microsoft.UI.Xaml.Media.GradientStop { Color = lift, Offset = 0.8 });
                    brush.GradientStops.Add(new Microsoft.UI.Xaml.Media.GradientStop { Color = color, Offset = 1 });
                    return brush;
                }
                default:
                    return new Microsoft.UI.Xaml.Media.SolidColorBrush(color);
            }
        }

        public static Windows.UI.Color ToColor(string hex)
        {
            var (r, g, b) = ColorMath.FromHex(hex);
            return Windows.UI.Color.FromArgb(255, r, g, b);
        }
    }
}

