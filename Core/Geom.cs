using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace NewColoringbook.Core
{
    /// <summary>
    /// One bounded, closed area of a coloring design in the design's 1000x1000 coordinate space.
    /// Data is XAML path mini-language; an optional "F1 " prefix selects Nonzero winding
    /// (unions of same-direction sub-paths) while the default EvenOdd punches holes.
    /// </summary>
    public sealed class DesignRegion
    {
        public string Id { get; }
        public string Data { get; }

        public DesignRegion(string id, string data)
        {
            Id = id;
            Data = data;
        }
    }

    /// <summary>A complete line-art page: metadata plus its list of colorable regions.</summary>
    public sealed class DesignDef
    {
        public string Id { get; } = "";
        public string Title { get; } = "";
        public string Category { get; } = "";
        public string[] Tags { get; } = Array.Empty<string>();
        public int Popularity { get; }
        public int ItemNumber { get; set; }
        public string DisplayNumber => $"#{ItemNumber}";
        public List<DesignRegion> Regions { get; } = new();

        public int ComplexityRating
        {
            get
            {
                int count = Regions.Count;
                if (count < 35) return 1;
                if (count < 55) return 2;
                if (count < 75) return 3;
                if (count < 95) return 4;
                return 5;
            }
        }

        public string ComplexityStars => new string('★', ComplexityRating) + new string('☆', 5 - ComplexityRating);

        public string ComplexityLabel => ComplexityRating switch
        {
            1 => "Simple (★☆☆☆☆)",
            2 => "Easy (★★☆☆☆)",
            3 => "Medium (★★★☆☆)",
            4 => "Complex (★★★★☆)",
            _ => "Zentangle (★★★★★)",
        };

        public DesignDef(string id, string title, string category, int popularity, string[] tags)
        {
            Id = id;
            Title = title;
            Category = category;
            Popularity = popularity;
            Tags = tags;
        }
    }

    /// <summary>
    /// Geometry helpers that emit XAML path mini-language strings in a 1000x1000 design space.
    /// Angles are degrees measured clockwise from 12 o'clock (up), which reads naturally for
    /// radial designs; screen Y grows downward.
    /// </summary>
    public static class G
    {
        private static readonly CultureInfo C = CultureInfo.InvariantCulture;

        private static string N(double v) => Math.Round(v, 1).ToString(C);

        private static string P(double x, double y) => N(x) + "," + N(y);

        /// <summary>Point at radius r and clockwise-from-up angle a around (cx, cy).</summary>
        public static (double X, double Y) Pt(double cx, double cy, double r, double a)
        {
            double rad = a * Math.PI / 180.0;
            return (cx + r * Math.Sin(rad), cy - r * Math.Cos(rad));
        }

        private static string PtStr(double cx, double cy, double r, double a)
        {
            var p = Pt(cx, cy, r, a);
            return P(p.X, p.Y);
        }

        /// <summary>Polygon through the given points.</summary>
        public static string Poly(params (double X, double Y)[] pts)
        {
            var sb = new StringBuilder();
            sb.Append("M ").Append(P(pts[0].X, pts[0].Y));
            for (int i = 1; i < pts.Length; i++)
                sb.Append(" L ").Append(P(pts[i].X, pts[i].Y));
            sb.Append(" Z");
            return sb.ToString();
        }

        /// <summary>Polygon from raw coordinate pairs (x0,y0,x1,y1,...).</summary>
        public static string PolyD(params double[] xy)
        {
            var pts = new (double, double)[xy.Length / 2];
            for (int i = 0; i < pts.Length; i++) pts[i] = (xy[i * 2], xy[i * 2 + 1]);
            return Poly(pts);
        }

        public static string Circle(double cx, double cy, double r) =>
            "M " + P(cx - r, cy) +
            " A " + N(r) + "," + N(r) + " 0 1 1 " + P(cx + r, cy) +
            " A " + N(r) + "," + N(r) + " 0 1 1 " + P(cx - r, cy) + " Z";

        public static string Ellipse(double cx, double cy, double rx, double ry) =>
            "M " + P(cx - rx, cy) +
            " A " + N(rx) + "," + N(ry) + " 0 1 1 " + P(cx + rx, cy) +
            " A " + N(rx) + "," + N(ry) + " 0 1 1 " + P(cx - rx, cy) + " Z";

        /// <summary>Rotated ellipse (angle clockwise from up, degrees).</summary>
        public static string EllipseRot(double cx, double cy, double rx, double ry, double angleDeg)
        {
            // Build axis-aligned in local frame then map every sampled point through rotation;
            // emitting as a bezier approximation keeps it a compact exact ellipse instead of a polyline.
            double rad = angleDeg * Math.PI / 180.0;
            double cos = Math.Cos(rad), sin = Math.Sin(rad);
            // kappa control distances for a 4-segment circle/ellipse
            const double k = 0.5522847498307936;
            double kx = rx * k, ky = ry * k;
            var local = new (double, double)[]
            {
                ( rx, 0), ( rx, ky), ( kx, ry), (0, ry),
                (-kx, ry), (-rx, ky), (-rx, 0), (-rx, -ky),
                (-kx, -ry), (0, -ry), ( kx, -ry), ( rx, -ky),
            };
            (double, double) Map((double, double) p)
            {
                // local X-right/Y-up rotated by angle: right=(cos,sin), up=(sin,-cos)
                return (cx + p.Item1 * cos + p.Item2 * sin, cy + p.Item1 * sin - p.Item2 * cos);
            }
            var m0 = Map(local[0]);
            var sb = new StringBuilder();
            sb.Append("M ").Append(P(m0.Item1, m0.Item2));
            int[] starts = { 1, 4, 7, 10 };
            foreach (int s in starts)
            {
                var c1 = Map(local[s]);
                var c2 = Map(local[s + 1]);
                var ep = Map(local[(s + 2) % 12]);
                sb.Append(" C ").Append(P(c1.Item1, c1.Item2)).Append(' ').Append(P(c2.Item1, c2.Item2)).Append(' ').Append(P(ep.Item1, ep.Item2));
            }
            sb.Append(" Z");
            return sb.ToString();
        }

        /// <summary>Donut ring (outer minus inner hole via EvenOdd).</summary>
        public static string Ring(double cx, double cy, double rOuter, double rInner) =>
            Circle(cx, cy, rOuter) + " " + Circle(cx, cy, rInner);

        /// <summary>Annulus sector between r0 and r1 spanning angles a0..a1 (degrees, clockwise from up).</summary>
        public static string RingSector(double cx, double cy, double r0, double r1, double a0, double a1)
        {
            bool large = Math.Abs(a1 - a0) > 180;
            string lf = large ? "1" : "0";
            return "M " + PtStr(cx, cy, r0, a0) +
                   " L " + PtStr(cx, cy, r1, a0) +
                   " A " + N(r1) + "," + N(r1) + " 0 " + lf + " 1 " + PtStr(cx, cy, r1, a1) +
                   " L " + PtStr(cx, cy, r0, a1) +
                   " A " + N(r0) + "," + N(r0) + " 0 " + lf + " 0 " + PtStr(cx, cy, r0, a0) +
                   " Z";
        }

        /// <summary>Pie slice from the center.</summary>
        public static string Pie(double cx, double cy, double r, double a0, double a1) =>
            RingSector(cx, cy, 0.001, r, a0, a1);

        /// <summary>
        /// A petal growing outward: base at radius rBase, tip at rBase+len, bulging width w,
        /// centered on angle a. Drawn with cubics for a soft leaf look.
        /// </summary>
        public static string Petal(double cx, double cy, double rBase, double len, double w, double a)
        {
            double rad = a * Math.PI / 180.0;
            double ux = Math.Sin(rad), uy = -Math.Cos(rad);   // outward
            double rx = Math.Cos(rad), ry = Math.Sin(rad);    // right of axis
            (double, double) Map(double xr, double yu) => (cx + xr * rx + yu * ux, cy + xr * ry + yu * uy);

            var b = Map(0, rBase);
            var t = Map(0, rBase + len);
            var c1 = Map(-w, rBase + len * 0.28);
            var c2 = Map(-w, rBase + len * 0.78);
            var c3 = Map(w, rBase + len * 0.78);
            var c4 = Map(w, rBase + len * 0.28);
            var sb = new StringBuilder();
            sb.Append("M ").Append(P(b.Item1, b.Item2));
            sb.Append(" C ").Append(P(c1.Item1, c1.Item2)).Append(' ').Append(P(c2.Item1, c2.Item2)).Append(' ').Append(P(t.Item1, t.Item2));
            sb.Append(" C ").Append(P(c3.Item1, c3.Item2)).Append(' ').Append(P(c4.Item1, c4.Item2)).Append(' ').Append(P(b.Item1, b.Item2));
            sb.Append(" Z");
            return sb.ToString();
        }

        /// <summary>Pointed-both-ends leaf from (base point along angle a) outward.</summary>
        public static string Leaf(double cx, double cy, double rBase, double len, double w, double a)
        {
            double rad = a * Math.PI / 180.0;
            double ux = Math.Sin(rad), uy = -Math.Cos(rad);
            double rx = Math.Cos(rad), ry = Math.Sin(rad);
            (double, double) Map(double xr, double yu) => (cx + xr * rx + yu * ux, cy + xr * ry + yu * uy);
            var b = Map(0, rBase);
            var t = Map(0, rBase + len);
            var q1 = Map(w, rBase + len * 0.5);
            var q2 = Map(-w, rBase + len * 0.5);
            return "M " + P(b.Item1, b.Item2) +
                   " Q " + P(q1.Item1, q1.Item2) + " " + P(t.Item1, t.Item2) +
                   " Q " + P(q2.Item1, q2.Item2) + " " + P(b.Item1, b.Item2) + " Z";
        }

        /// <summary>N-point star (alternate outer/inner radius), rotated by rot degrees.</summary>
        public static string Star(double cx, double cy, int n, double rOuter, double rInner, double rot = -90)
        {
            var sb = new StringBuilder();
            for (int i = 0; i < n * 2; i++)
            {
                double r = i % 2 == 0 ? rOuter : rInner;
                double a = rot + i * 180.0 / n;
                var p = Pt(cx, cy, r, a);
                sb.Append(i == 0 ? "M " : " L ").Append(P(p.X, p.Y));
            }
            sb.Append(" Z");
            return sb.ToString();
        }

        /// <summary>Four-point sparkle star.</summary>
        public static string Sparkle(double cx, double cy, double rOuter, double rInner, double rot = -90) =>
            Star(cx, cy, 4, rOuter, rInner, rot);

        /// <summary>Triangle from three points.</summary>
        public static string Tri((double X, double Y) a, (double X, double Y) b, (double X, double Y) c) => Poly(a, b, c);

        /// <summary>Rounded rectangle path.</summary>
        public static string RoundRect(double x, double y, double w, double h, double r)
        {
            r = Math.Min(r, Math.Min(w, h) / 2);
            return "M " + P(x + r, y) +
                   " L " + P(x + w - r, y) + " A " + N(r) + "," + N(r) + " 0 0 1 " + P(x + w, y + r) +
                   " L " + P(x + w, y + h - r) + " A " + N(r) + "," + N(r) + " 0 0 1 " + P(x + w - r, y + h) +
                   " L " + P(x + r, y + h) + " A " + N(r) + "," + N(r) + " 0 0 1 " + P(x, y + h - r) +
                   " L " + P(x, y + r) + " A " + N(r) + "," + N(r) + " 0 0 1 " + P(x + r, y) +
                   " Z";
        }

        /// <summary>Half-circle bump (scallop) sitting on the chord x0..x1 at height y.</summary>
        public static string Scallop(double x0, double x1, double y)
        {
            double r = Math.Abs(x1 - x0) / 2;
            return "M " + P(x0, y) + " A " + N(r) + "," + N(r) + " 0 0 1 " + P(x1, y) + " Z";
        }

        /// <summary>Heart centered horizontally at cx, top at cy, width w, height h.</summary>
        public static string Heart(double cx, double cy, double w, double h)
        {
            return "M " + P(cx, cy + 0.30 * h) +
                   " C " + P(cx + 0.24 * w, cy) + " " + P(cx + 0.50 * w, cy + 0.10 * h) + " " + P(cx + 0.50 * w, cy + 0.34 * h) +
                   " C " + P(cx + 0.50 * w, cy + 0.58 * h) + " " + P(cx + 0.24 * w, cy + 0.76 * h) + " " + P(cx, cy + h) +
                   " C " + P(cx - 0.24 * w, cy + 0.76 * h) + " " + P(cx - 0.50 * w, cy + 0.58 * h) + " " + P(cx - 0.50 * w, cy + 0.34 * h) +
                   " C " + P(cx - 0.50 * w, cy + 0.10 * h) + " " + P(cx - 0.24 * w, cy) + " " + P(cx, cy + 0.30 * h) +
                   " Z";
        }

        /// <summary>Crescent moon opening to the right (tips on the right side).</summary>
        public static string Crescent(double cx, double cy, double r, double tipDeg = 55)
        {
            double s = Math.Sin(tipDeg * Math.PI / 180.0), c = Math.Cos(tipDeg * Math.PI / 180.0);
            var t1 = (cx + r * s, cy - r * c);   // upper tip
            var t2 = (cx + r * s, cy + r * c);   // lower tip
            double ri = r * 0.86;
            double d = r * s - Math.Sqrt(Math.Max(0.01, ri * ri - r * r * c * c));
            _ = d;
            return "M " + P(t1.Item1, t1.Item2) +
                   " A " + N(r) + "," + N(r) + " 0 1 0 " + P(t2.Item1, t2.Item2) +
                   " A " + N(ri) + "," + N(ri) + " 0 0 1 " + P(t1.Item1, t1.Item2) +
                   " Z";
        }

        /// <summary>Teardrop: round belly at the base, pointed tip upward along angle a.</summary>
        public static string Drop(double cx, double cy, double r, double len, double a = 0)
        {
            double rad = a * Math.PI / 180.0;
            double ux = Math.Sin(rad), uy = -Math.Cos(rad);
            double rx = Math.Cos(rad), ry = Math.Sin(rad);
            (double, double) Map(double xr, double yu) => (cx + xr * rx + yu * ux, cy + xr * ry + yu * uy);
            string M(double xr, double yu) { var p = Map(xr, yu); return P(p.Item1, p.Item2); }
            var tip = Map(0, len);
            var cR1 = Map(r * 0.85, len * 0.30);
            var cR2 = Map(r, len * 0.62);
            var right = Map(r, 0);
            var cL1 = Map(-r, len * 0.62);
            var cL2 = Map(-r * 0.85, len * 0.30);
            return "M " + P(tip.Item1, tip.Item2) +
                   " C " + P(cR1.Item1, cR1.Item2) + " " + P(cR2.Item1, cR2.Item2) + " " + P(right.Item1, right.Item2) +
                   " A " + N(r) + "," + N(r) + " 0 1 1 " + M(-r, 0) +
                   " C " + P(cL1.Item1, cL1.Item2) + " " + P(cL2.Item1, cL2.Item2) + " " + P(tip.Item1, tip.Item2) +
                   " Z";
        }

        /// <summary>Cloud outline: flat base, three soft top bumps.</summary>
        public static string Cloud(double cx, double cy, double w, double h)
        {
            var l = (cx - w / 2, cy);
            var p1 = (cx - w * 0.25, cy - h * 0.55);
            var p2 = (cx, cy - h);
            var p3 = (cx + w * 0.25, cy - h * 0.55);
            var r = (cx + w / 2, cy);
            double a = Dist(l, p1) * 0.62, b = Dist(p1, p2) * 0.62, c2 = Dist(p2, p3) * 0.62, d = Dist(p3, r) * 0.62;
            return "M " + P(l.Item1, l.Item2) +
                   " A " + N(a) + "," + N(a) + " 0 0 1 " + P(p1.Item1, p1.Item2) +
                   " A " + N(b) + "," + N(b) + " 0 0 1 " + P(p2.Item1, p2.Item2) +
                   " A " + N(c2) + "," + N(c2) + " 0 0 1 " + P(p3.Item1, p3.Item2) +
                   " A " + N(d) + "," + N(d) + " 0 0 1 " + P(r.Item1, r.Item2) + " Z";
        }

        /// <summary>
        /// Horizontal wavy band between two sine edges. Used for water/hills.
        /// </summary>
        public static string WaveBand(double x0, double x1, double yTop, double height,
            double amp, double wavelength, double phase, int samples = 28)
        {
            var sb = new StringBuilder();
            for (int i = 0; i <= samples; i++)
            {
                double t = i / (double)samples;
                double x = x0 + (x1 - x0) * t;
                double y = yTop + amp * Math.Sin((x / wavelength) * 2 * Math.PI + phase);
                sb.Append(i == 0 ? "M " : " L ").Append(P(x, y));
            }
            for (int i = samples; i >= 0; i--)
            {
                double t = i / (double)samples;
                double x = x0 + (x1 - x0) * t;
                double y = yTop + height + amp * Math.Sin((x / wavelength) * 2 * Math.PI + phase + 0.9);
                sb.Append(" L ").Append(P(x, y));
            }
            sb.Append(" Z");
            return sb.ToString();
        }

        /// <summary>Partitioned region from y0 (top frame) down to the top edge of a wave band.</summary>
        public static string WaveBandTop(double x0, double x1, double y0, double yTop,
            double amp, double wavelength, double phase, int samples = 28)
        {
            var sb = new StringBuilder();
            sb.Append("M ").Append(P(x0, y0)).Append(" L ").Append(P(x1, y0));
            for (int i = samples; i >= 0; i--)
            {
                double t = i / (double)samples;
                double x = x0 + (x1 - x0) * t;
                double y = yTop + amp * Math.Sin((x / wavelength) * 2 * Math.PI + phase);
                sb.Append(" L ").Append(P(x, y));
            }
            sb.Append(" Z");
            return sb.ToString();
        }

        /// <summary>Partitioned region between the bottom of wave 1 and the top of wave 2.</summary>
        public static string WaveBandBetween(double x0, double x1,
            double yTop1, double h1, double amp1, double w1, double p1,
            double yTop2, double amp2, double w2, double p2, int samples = 28)
        {
            var sb = new StringBuilder();
            for (int i = 0; i <= samples; i++)
            {
                double t = i / (double)samples;
                double x = x0 + (x1 - x0) * t;
                double y = yTop1 + h1 + amp1 * Math.Sin((x / w1) * 2 * Math.PI + p1 + 0.9);
                sb.Append(i == 0 ? "M " : " L ").Append(P(x, y));
            }
            for (int i = samples; i >= 0; i--)
            {
                double t = i / (double)samples;
                double x = x0 + (x1 - x0) * t;
                double y = yTop2 + amp2 * Math.Sin((x / w2) * 2 * Math.PI + p2);
                sb.Append(" L ").Append(P(x, y));
            }
            sb.Append(" Z");
            return sb.ToString();
        }

        /// <summary>Partitioned region from the bottom of a wave band down to yBottom.</summary>
        public static string WaveBandBottom(double x0, double x1, double yTop, double height,
            double amp, double wavelength, double phase, double yBottom = 960, int samples = 28)
        {
            var sb = new StringBuilder();
            for (int i = 0; i <= samples; i++)
            {
                double t = i / (double)samples;
                double x = x0 + (x1 - x0) * t;
                double y = yTop + height + amp * Math.Sin((x / wavelength) * 2 * Math.PI + phase + 0.9);
                sb.Append(i == 0 ? "M " : " L ").Append(P(x, y));
            }
            sb.Append(" L ").Append(P(x1, yBottom)).Append(" L ").Append(P(x0, yBottom)).Append(" Z");
            return sb.ToString();
        }

        /// <summary>Gentle hill: quadratic from (x0,y0) up to (x1,y0) with peak (xm,ypeak).</summary>
        public static string Hill(double x0, double x1, double yBase, double yPeak)
        {
            double xm = (x0 + x1) / 2;
            return "M " + P(x0, yBase) +
                   " Q " + P(xm, yPeak) + " " + P(x1, yBase) + " Z";
        }

        private static double Dist((double, double) a, (double, double) b) =>
            Math.Sqrt((a.Item1 - b.Item1) * (a.Item1 - b.Item1) + (a.Item2 - b.Item2) * (a.Item2 - b.Item2));

        /// <summary>Prefix selecting Nonzero winding for union-style sub-paths.</summary>
        public static string Nonzero(string data) => "F1 " + data;

        /// <summary>Diamond centered at (cx, cy) with width w and height h.</summary>
        public static string Diamond(double cx, double cy, double w, double h) =>
            Poly((cx, cy - h / 2), (cx + w / 2, cy), (cx, cy + h / 2), (cx - w / 2, cy));

        /// <summary>Regular N-sided polygon.</summary>
        public static string RegularPolygon(double cx, double cy, int sides, double r, double rot = 0)
        {
            var pts = new (double, double)[sides];
            for (int i = 0; i < sides; i++)
                pts[i] = Pt(cx, cy, r, rot + i * 360.0 / sides);
            return Poly(pts);
        }

        /// <summary>Regular hexagon centered at (cx, cy).</summary>
        public static string Hexagon(double cx, double cy, double r, double rot = 0) =>
            RegularPolygon(cx, cy, 6, r, rot);

        /// <summary>Regular octagon centered at (cx, cy).</summary>
        public static string Octagon(double cx, double cy, double r, double rot = 0) =>
            RegularPolygon(cx, cy, 8, r, rot);

        /// <summary>Square centered at (cx, cy) with side length size.</summary>
        public static string Square(double cx, double cy, double size) =>
            RegularPolygon(cx, cy, 4, size * 0.70710678, 45);

        /// <summary>Slightly inset copy helper for concentric decoration circles (returns circle radius r).</summary>
        public static string DotsRing(double cx, double cy, double r, int count, double dotR, double rot = 0)
        {
            // caller adds individual dots; provided for clarity in catalog code
            return Circle(cx, cy, r);
        }
    }
}

