using System;
using System.Collections.Generic;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using NewColoringbook.Core;
using Path = Microsoft.UI.Xaml.Shapes.Path;

namespace NewColoringbook.Services
{
    /// <summary>
    /// Builds the XAML visuals for a design: Path elements for regions (fill + black line art)
    /// and freehand stroke paths. Shared by gallery thumbnails, progress cards and the studio.
    /// </summary>
    public static class DesignRenderer
    {
        public const double DesignSize = 1000.0;
        public const double LineThickness = 3.2;

        /// <summary>Parse path mini-language into geometry (XamlBindingHelper route; never throws).</summary>
        public static Geometry ParseGeometry(string data)
        {
            try
            {
                return (Geometry)XamlBindingHelper.ConvertValue(typeof(Geometry), data);
            }
            catch (Exception ex)
            {
                AppLog.Error("Path parse failed: " + data.Substring(0, Math.Min(60, data.Length)), ex);
                return new RectangleGeometry { Rect = new Windows.Foundation.Rect(0, 0, 1, 1) };
            }
        }

        private static readonly SolidColorBrush PaperFill = new(Windows.UI.Color.FromArgb(255, 255, 255, 255));
        private static readonly SolidColorBrush LineBrush = new(Windows.UI.Color.FromArgb(255, 32, 28, 24));

        /// <summary>One region as an interactive Path. Uncolored regions get an opaque paper white fill so
        /// background fills do not bleed through uncolored foreground objects.</summary>
        public static Path BuildRegionPath(DesignRegion region, StoredFill? fill)
        {
            try
            {
                var path = new Path
                {
                    Data = ParseGeometry(region.Data),
                    Fill = fill == null ? PaperFill : Textures.BrushFor(fill.Texture, fill.Color),
                    Stroke = LineBrush,
                    StrokeThickness = LineThickness,
                    StrokeLineJoin = PenLineJoin.Round,
                    StrokeStartLineCap = PenLineCap.Round,
                    StrokeEndLineCap = PenLineCap.Round,
                    Tag = region,
                    UseLayoutRounding = true,
                };
                return path;
            }
            catch (Exception ex)
            {
                AppLog.Error("BuildRegionPath failed", ex);
                return new Path { Tag = region };
            }
        }

        public static void ApplyFill(Path path, StoredFill? fill)
        {
            try
            {
                path.Fill = fill == null ? PaperFill : Textures.BrushFor(fill.Texture, fill.Color);
            }
            catch { }
        }

        /// <summary>Re-render an existing path with a preview color (thumbnails).</summary>
        public static void ApplyPreviewFill(Path path, string hex)
        {
            try
            {
                path.Fill = new SolidColorBrush(Textures.ToColor(hex));
            }
            catch { }
        }

        /// <summary>A freehand stroke as a stroked polyline Path (no fill).</summary>
        public static Path BuildStrokePath(Stroke stroke)
        {
            try
            {
                var p0 = stroke.Points.Count > 0 ? stroke.Points[0] : new Windows.Foundation.Point(0, 0);
                var figure = new PathFigure { StartPoint = p0 };
                if (stroke.Points.Count == 1)
                {
                    figure.Segments.Add(new LineSegment { Point = new Windows.Foundation.Point(p0.X + 0.05, p0.Y + 0.05) });
                }
                else
                {
                    for (int i = 1; i < stroke.Points.Count; i++)
                        figure.Segments.Add(new LineSegment { Point = stroke.Points[i] });
                }
                figure.IsClosed = false;
                var geo = new PathGeometry();
                geo.Figures.Add(figure);

                return new Path
                {
                    Data = geo,
                    Stroke = new SolidColorBrush(Textures.ToColor(stroke.Color)),
                    StrokeThickness = stroke.Width,
                    StrokeLineJoin = PenLineJoin.Round,
                    StrokeStartLineCap = PenLineCap.Round,
                    StrokeEndLineCap = PenLineCap.Round,
                    Tag = stroke,
                };
            }
            catch (Exception ex)
            {
                AppLog.Error("BuildStrokePath failed", ex);
                return new Path { Tag = stroke };
            }
        }

        public static void UpdateStrokePath(Path path, Stroke stroke)
        {
            try
            {
                var p0 = stroke.Points.Count > 0 ? stroke.Points[0] : new Windows.Foundation.Point(0, 0);
                var figure = new PathFigure { StartPoint = p0 };
                if (stroke.Points.Count == 1)
                {
                    figure.Segments.Add(new LineSegment { Point = new Windows.Foundation.Point(p0.X + 0.05, p0.Y + 0.05) });
                }
                else
                {
                    for (int i = 1; i < stroke.Points.Count; i++)
                        figure.Segments.Add(new LineSegment { Point = stroke.Points[i] });
                }
                figure.IsClosed = false;
                var geo = new PathGeometry();
                geo.Figures.Add(figure);
                path.Data = geo;
            }
            catch (Exception ex)
            {
                AppLog.Error("UpdateStrokePath failed", ex);
            }
        }

        /// <summary>
        /// Build a static (non-interactive) preview canvas of the design scaled into size x size.
        /// previewColors picks deterministic editorial colors so cards look alive in the gallery.
        /// </summary>
        public static FrameworkElement BuildPreview(DesignDef design, DesignProgress? progress, double size, bool previewColors)
        {
            try
            {
                var host = new Canvas
                {
                    Width = DesignSize,
                    Height = DesignSize,
                    IsHitTestVisible = false,
                };
                for (int i = 0; i < design.Regions.Count; i++)
                {
                    var region = design.Regions[i];
                    StoredFill? fill = null;
                    if (progress != null && progress.Fills.TryGetValue(region.Id, out var saved))
                    {
                        fill = saved;
                    }
                    else if (previewColors)
                    {
                        fill = new StoredFill(Palettes.PreviewColor(i, design.Regions.Count), "solid");
                    }
                    var p = BuildRegionPath(region, fill);
                    p.StrokeThickness = 4.0;   // slightly bolder for small previews
                    host.Children.Add(p);
                }
                if (progress != null && progress.Strokes != null)
                {
                    foreach (var s in progress.Strokes)
                    {
                        var stroke = new Stroke(s.Color, s.Width);
                        foreach (var pt in s.Pts)
                        {
                            if (pt.Length >= 2)
                                stroke.Points.Add(new Windows.Foundation.Point(pt[0], pt[1]));
                        }
                        var sp = BuildStrokePath(stroke);
                        host.Children.Add(sp);
                    }
                }
                var box = new Viewbox
                {
                    Width = size,
                    Height = size,
                    Child = host,
                    IsHitTestVisible = false,
                };
                // decorative art: keep out of the UIA control/content views (screen readers + fast queries)
                Microsoft.UI.Xaml.Automation.AutomationProperties.SetAccessibilityView(
                    box, Microsoft.UI.Xaml.Automation.Peers.AccessibilityView.Raw);
                return box;
            }
            catch (Exception ex)
            {
                AppLog.Error("BuildPreview failed for " + design.Id, ex);
                return new Border { Width = size, Height = size, Background = PaperFill };
            }
        }

        /// <summary>Design-space point for the center of a region's bounding box (used for eraser sweep).</summary>
        public static Windows.Foundation.Rect RegionBounds(DesignRegion region)
        {
            try
            {
                var geo = ParseGeometry(region.Data);
                if (geo is PathGeometry pg)
                {
                    double x0 = double.MaxValue, y0 = double.MaxValue, x1 = double.MinValue, y1 = double.MinValue;
                    foreach (var f in pg.Figures)
                    {
                        void Visit(Windows.Foundation.Point pt)
                        {
                            x0 = Math.Min(x0, pt.X); y0 = Math.Min(y0, pt.Y);
                            x1 = Math.Max(x1, pt.X); y1 = Math.Max(y1, pt.Y);
                        }
                        Visit(f.StartPoint);
                        foreach (var seg in f.Segments)
                        {
                            if (seg is LineSegment ls) Visit(ls.Point);
                            else if (seg is BezierSegment bs) { Visit(bs.Point1); Visit(bs.Point2); Visit(bs.Point3); }
                            else if (seg is PolyLineSegment pls) foreach (var pt in pls.Points) Visit(pt);
                            else if (seg is PolyBezierSegment pbs)
                            {
                                foreach (var pt in pbs.Points) Visit(pt);
                            }
                            else if (seg is ArcSegment ars)
                            {
                                Visit(ars.Point);
                                // approximate arc extent by sampling is overkill; endpoints bound it well enough
                                _ = ars.Size;
                            }
                        }
                    }
                    if (x0 == double.MaxValue) return new Windows.Foundation.Rect(0, 0, DesignSize, DesignSize);
                    return new Windows.Foundation.Rect(x0, y0, x1 - x0, y1 - y0);
                }
                return geo.Bounds;
            }
            catch
            {
                return new Windows.Foundation.Rect(0, 0, DesignSize, DesignSize);
            }
        }
    }
}

