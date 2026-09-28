using System;
using System.Collections.Generic;

namespace NewColoringbook.Core
{
    /// <summary>
    /// The bundled line-art library: 60 intricate, high-density Zentangle adult coloring designs
    /// across 6 categories (Animals, Nature, Mandalas, Fantasy, Floral, Geometric).
    /// Every design contains rich inner details (scales, stripes, beads, fins, vertebrae,
    /// rosettes, facets, and flourishes) matching authentic adult coloring books.
    /// </summary>
    public static class DesignCatalog
    {
        public const string CatAnimals = "Animals";
        public const string CatNature = "Nature";
        public const string CatMandalas = "Mandalas";
        public const string CatFantasy = "Fantasy";
        public const string CatFloral = "Floral";
        public const string CatGeometric = "Geometric";
        public const string CatFreeDraw = "Free Draw";

        public const string FreeDrawId = "free_draw";

        public static readonly List<DesignDef> All = BuildAll();

        public static DesignDef CreateFreeDraw()
        {
            var d = new DesignDef(FreeDrawId, "Free Draw Canvas", "Custom", 0, new[] { "freedraw", "custom", "blank", "canvas" });
            d.ItemNumber = 0;
            d.Regions.Add(new DesignRegion("r0", G.RoundRect(40, 40, 920, 920, 36)));
            return d;
        }

        public static DesignDef? Find(string id)
        {
            foreach (var d in All) if (string.Equals(d.Id, id, StringComparison.OrdinalIgnoreCase)) return d;
            if (string.Equals(id, FreeDrawId, StringComparison.OrdinalIgnoreCase))
                return CreateFreeDraw();
            return null;
        }

        private static List<DesignDef> BuildAll()
        {
            var list = new List<DesignDef>();

            // Animals (10)
            UnderseaZentangle(list);
            Owl(list);
            Butterfly(list);
            Koi(list);
            Peacock(list);
            Seahorse(list);
            Turtle(list);
            Fox(list);
            Chameleon(list);
            Wolf(list);

            // Nature (10)
            Ocean(list);
            Tree(list);
            Cactus(list);
            Lake(list);
            Jungle(list);
            Sunflower(list);
            MushroomGrove(list);
            AutumnHarvest(list);
            ZenWater(list);
            Bonsai(list);

            // Mandalas (10)
            Lotus(list);
            StarMandala(list);
            BloomMandala(list);
            Sunburst(list);
            CosmicMandala(list);
            LaceMandala(list);
            CelticMandala(list);
            ChakraMandala(list);
            ArabesqueMandala(list);
            ZenHarmony(list);

            // Fantasy (10)
            Castle(list);
            Gem(list);
            MoonStars(list);
            Mushrooms(list);
            Dragon(list);
            Mermaid(list);
            Unicorn(list);
            Phoenix(list);
            Potion(list);
            TreeOfWonders(list);

            // Floral (10)
            RoseBouquet(list);
            OrchidElegance(list);
            WildflowerMeadow(list);
            CherryBlossom(list);
            HydrangeaGarden(list);
            PeonyDream(list);
            LavenderHerbs(list);
            TropicalHibiscus(list);
            WaterLilySanctuary(list);
            FloralHeartWreath(list);

            // Free Draw Templates (6)
            FreeDrawBlank(list);
            FreeDrawMandala(list);
            FreeDrawComic(list);
            FreeDrawLandscape(list);
            FreeDrawHex(list);
            FreeDrawCard(list);

            // Geometric (10)
            SacredGeometry(list);
            StainedGlass(list);
            HexagonalHive(list);
            OpticalIllusion(list);
            ChevronWaves(list);
            DiamondLattice(list);
            ZenTessellation(list);
            OpArtSpheres(list);
            OrigamiMosaic(list);
            IslamicTileArt(list);

            for (int i = 0; i < list.Count; i++)
            {
                list[i].ItemNumber = i + 1;
            }

            return list;
        }

        /// <summary>Region accumulator; auto-assigns stable ids in insertion (z) order.</summary>
        private sealed class B
        {
            private readonly List<DesignRegion> _r = new();
            private int _n;

            public int Count => _n;

            public void R(string data) => _r.Add(new DesignRegion("r" + _n++, data));

            public DesignDef Def(string id, string title, string cat, int pop, params string[] tags)
            {
                var d = new DesignDef(id, title, cat, pop, tags);
                d.Regions.AddRange(_r);
                return d;
            }
        }

        // ---------------------------------------------------------------- helpers

        private static string F(double v) => Math.Round(v, 1).ToString(System.Globalization.CultureInfo.InvariantCulture);

        private static double Dist((double X, double Y) a, (double X, double Y) b) =>
            Math.Sqrt((a.X - b.X) * (a.X - b.X) + (a.Y - b.Y) * (a.Y - b.Y));

        private static (double X, double Y) OrbitPt(double cx, double cy, double rx, double ry, double deg)
        {
            double rad = deg * Math.PI / 180.0;
            return (cx + rx * Math.Cos(rad), cy + ry * Math.Sin(rad));
        }

        private static string UScallop(double x0, double x1, double y)
        {
            double r = Math.Abs(x1 - x0) / 2;
            return "M " + F(x0) + "," + F(y) + " A " + F(r) + "," + F(r) + " 0 0 0 " + F(x1) + "," + F(y) + " Z";
        }

        private static string Dome(double x0, double x1, double y, double ry)
        {
            double rx = Math.Abs(x1 - x0) / 2;
            return "M " + F(x0) + "," + F(y) + " A " + F(rx) + "," + F(ry) + " 0 0 1 " + F(x1) + "," + F(y) + " Z";
        }

        private static void Flower(B b, double cx, double cy, double base_, double len, double w, double petals, double centerR, double rot = 0)
        {
            for (int i = 0; i < petals; i++)
                b.R(G.Petal(cx, cy, base_, len, w, rot + i * 360.0 / petals));
            b.R(G.Circle(cx, cy, centerR));
        }

        private static void Pine(B b, double cx, double baseY, double w, double h)
        {
            b.R(G.RoundRect(cx - 7, baseY - h * 0.30, 14, h * 0.30 + 6, 5));
            b.R(G.PolyD(cx - w * 0.50, baseY - h * 0.34, cx + w * 0.50, baseY - h * 0.34, cx, baseY - h * 0.64));
            b.R(G.PolyD(cx - w * 0.42, baseY - h * 0.55, cx + w * 0.42, baseY - h * 0.55, cx, baseY - h * 0.82));
            b.R(G.PolyD(cx - w * 0.32, baseY - h * 0.74, cx + w * 0.32, baseY - h * 0.74, cx, baseY - h * 1.00));
        }

        private static void SunRays(B b, double cx, double cy, double r, double len, int n)
        {
            for (int i = 0; i < n; i++)
                b.R(G.Drop(cx, cy, r, len, i * 360.0 / n + 180.0 / n));
        }

        private static void PetalRing(B b, double cx, double cy, double rBase, double len, double w, int count, double rot = 0)
        {
            for (int i = 0; i < count; i++)
                b.R(G.Petal(cx, cy, rBase, len, w, rot + i * 360.0 / count));
        }

        private static void RingSectors(B b, double cx, double cy, double r0, double r1, int count, double gap = 1.0, double rot = 0)
        {
            double step = 360.0 / count;
            for (int i = 0; i < count; i++)
                b.R(G.RingSector(cx, cy, r0, r1, rot + i * step + gap / 2, rot + (i + 1) * step - gap / 2));
        }

        private static void CircleRing(B b, double cx, double cy, double radius, int count, double circleR, double rot = 0)
        {
            for (int i = 0; i < count; i++)
            {
                var p = G.Pt(cx, cy, radius, rot + i * 360.0 / count);
                b.R(G.Circle(p.X, p.Y, circleR));
            }
        }

        private static void DropRing(B b, double cx, double cy, double radius, double dropR, double dropLen, int count, double rot = 0)
        {
            for (int i = 0; i < count; i++)
            {
                double a = rot + i * 360.0 / count;
                var p = G.Pt(cx, cy, radius, a);
                b.R(G.Drop(p.X, p.Y, dropR, dropLen, a));
            }
        }

        private static void DiamondRing(B b, double cx, double cy, double radius, double dw, double dh, int count, double rot = 0)
        {
            for (int i = 0; i < count; i++)
            {
                var p = G.Pt(cx, cy, radius, rot + i * 360.0 / count);
                b.R(G.Diamond(p.X, p.Y, dw, dh));
            }
        }

        private static void ScallopRing(B b, double cx, double cy, double radius, int count, double rot = 0)
        {
            double step = 360.0 / count;
            for (int i = 0; i < count; i++)
            {
                var a0 = G.Pt(cx, cy, radius, rot + i * step);
                var a1 = G.Pt(cx, cy, radius, rot + (i + 1) * step);
                double r = Dist(a0, a1) * 0.44;
                b.R("M " + F(a0.X) + "," + F(a0.Y) + " A " + F(r) + "," + F(r) + " 0 0 1 " + F(a1.X) + "," + F(a1.Y) + " Z");
            }
        }

        private static void ConcentricBubble(B b, double cx, double cy, double r)
        {
            b.R(G.Ring(cx, cy, r, r * 0.62));
            b.R(G.Circle(cx, cy, r * 0.36));
        }

        private static void SegmentedStalk(B b, double bx, double by, double h, double w, double lean, int segments)
        {
            for (int i = 0; i < segments; i++)
            {
                double t0 = i / (double)segments;
                double t1 = (i + 1) / (double)segments;
                double y0 = by - h * t0, y1 = by - h * t1;
                double x0 = bx + lean * t0 * t0, x1 = bx + lean * t1 * t1;
                double segW = w * (1.0 - t0 * 0.4);
                b.R(G.PolyD(x0 - segW / 2, y0, x1 - segW / 2, y1, x1 + segW / 2, y1, x0 + segW / 2, y0));
                if (i % 2 == 1 && segW > 8)
                {
                    double mx = (x0 + x1) / 2, my = (y0 + y1) / 2;
                    b.R(G.Circle(mx, my, Math.Min(segW * 0.28, 5.5)));
                }
            }
        }

        // =========================================================================
        // ANIMALS (10)
        // =========================================================================

        /// <summary>Undersea Zentangle - High density ocean artwork matching user reference image.</summary>
        private static void UnderseaZentangle(List<DesignDef> list)
        {
            var b = new B();
            b.R(G.RoundRect(40, 40, 920, 920, 36));

            // Floating Concentric Bubbles
            double[,] bubbles = {
                { 80, 140, 18 }, { 340, 70, 14 }, { 510, 100, 16 }, { 590, 140, 12 },
                { 920, 240, 15 }, { 110, 470, 19 }, { 315, 680, 14 }, { 315, 875, 15 },
                { 840, 440, 14 }, { 430, 345, 16 }, { 480, 190, 12 }, { 180, 420, 10 }
            };
            for (int i = 0; i < 12; i++)
                ConcentricBubble(b, bubbles[i, 0], bubbles[i, 1], bubbles[i, 2]);

            // Top Seaweed Fronds
            b.R(G.Leaf(620, 40, 0, 110, 24, 135));
            b.R(G.Leaf(740, 40, 0, 140, 28, 150));
            b.R(G.Leaf(860, 40, 0, 160, 32, 160));
            b.R(G.Leaf(910, 70, 0, 110, 24, 175));

            // --- LEFT FANCY FISH (Top Left) ---
            b.R(G.PolyD(150, 240, 230, 160, 340, 210, 330, 290, 210, 360, 145, 290));
            b.R(G.PolyD(150, 240, 190, 220, 190, 310, 145, 290));
            b.R(G.Circle(172, 265, 14));
            b.R(G.Circle(172, 265, 6));
            b.R(G.PolyD(190, 220, 230, 190, 230, 330, 190, 310));
            b.R(G.PolyD(230, 190, 270, 175, 270, 320, 230, 330));
            b.R(G.PolyD(270, 175, 310, 190, 310, 305, 270, 320));
            for (int i = 0; i < 4; i++) b.R(UScallop(195 + i * 18, 213 + i * 18, 255));
            for (int i = 0; i < 4; i++) b.R(UScallop(204 + i * 18, 222 + i * 18, 275));
            for (int i = 0; i < 4; i++) b.R(UScallop(213 + i * 18, 231 + i * 18, 295));
            CircleRing(b, 250, 230, 24, 4, 4.5);
            b.R(G.Petal(230, 160, 0, 130, 18, 330));
            b.R(G.Circle(195, 55, 8));
            b.R(G.Petal(270, 160, 0, 150, 20, 355));
            b.R(G.Circle(265, 35, 9));
            b.R(G.Petal(310, 180, 0, 140, 18, 25));
            b.R(G.Circle(350, 60, 8));
            b.R(G.Petal(130, 240, 0, 55, 16, 240));
            b.R(G.Petal(210, 360, 0, 65, 20, 150));
            b.R(G.Petal(300, 320, 0, 55, 18, 130));

            // --- RIGHT ANGELFISH (Top Right) ---
            b.R(G.PolyD(560, 290, 690, 190, 820, 220, 860, 310, 770, 420, 640, 400));
            b.R(G.PolyD(820, 220, 875, 210, 870, 235, 830, 250));
            b.R(G.PolyD(760, 220, 820, 220, 830, 300, 760, 330));
            Flower(b, 775, 255, 3, 16, 6, 6, 6);
            b.R(G.PolyD(680, 200, 760, 220, 760, 330, 670, 360));
            b.R(G.PolyD(620, 240, 680, 200, 670, 360, 600, 380));
            for (int i = 0; i < 4; i++) b.R(UScallop(685 + i * 16, 701 + i * 16, 260));
            for (int i = 0; i < 4; i++) b.R(UScallop(677 + i * 16, 693 + i * 16, 285));
            for (int i = 0; i < 4; i++) b.R(UScallop(669 + i * 16, 685 + i * 16, 310));
            for (int i = 0; i < 3; i++) b.R(UScallop(661 + i * 16, 677 + i * 16, 335));
            CircleRing(b, 740, 280, 22, 5, 4.5);
            b.R(G.Petal(560, 270, 0, 110, 26, 290));
            b.R(G.Circle(490, 215, 10));
            b.R(G.Petal(610, 420, 0, 130, 28, 170));
            b.R(G.Circle(625, 475, 10));
            b.R(G.Petal(730, 430, 0, 110, 22, 145));
            b.R(G.Petal(560, 330, 0, 75, 20, 240));
            b.R(G.Petal(560, 355, 0, 75, 20, 215));

            // --- ZENTANGLE SEAHORSE (Center-Left) ---
            Flower(b, 275, 460, 3, 16, 7, 6, 6);
            b.R(G.PolyD(260, 480, 310, 450, 330, 520, 285, 560));
            b.R(G.RoundRect(250, 490, 20, 70, 6));
            b.R(G.Circle(260, 560, 7));
            b.R(G.Circle(295, 490, 12));
            b.R(G.Circle(295, 490, 5));
            b.R(G.PolyD(310, 510, 380, 480, 425, 620, 360, 660, 320, 580));
            for (int i = 0; i < 7; i++)
            {
                var sp = G.Pt(345, 540, 55, -45 + i * 20);
                b.R(G.Circle(sp.X, sp.Y, 7.5));
                b.R(G.Circle(sp.X, sp.Y, 3.5));
            }
            for (int i = 0; i < 7; i++)
            {
                double by = 515 + i * 18;
                b.R(G.PolyD(305 + i * 6, by, 355 + i * 6, by - 6, 360 + i * 6, by + 10, 310 + i * 6, by + 16));
            }
            b.R(G.Ring(390, 780, 95, 55));
            b.R(G.Ring(390, 780, 55, 25));
            b.R(G.Circle(390, 780, 20));
            for (int i = 0; i < 10; i++)
            {
                double a = -30 + i * 36;
                b.R(G.RingSector(390, 780, 58, 92, a + 1, a + 33));
            }

            // --- ZENTANGLE STARFISH (Bottom Right) ---
            b.R(G.Star(680, 740, 5, 140, 55, -20));
            b.R(G.Circle(680, 740, 22));
            for (int arm = 0; arm < 5; arm++)
            {
                double baseA = -20 + arm * 72;
                for (int spot = 1; spot <= 5; spot++)
                {
                    double dist = 28 + spot * 18;
                    var sp = G.Pt(680, 740, dist, baseA + (spot % 2 == 0 ? 6 : -6));
                    b.R(G.Circle(sp.X, sp.Y, 7 - spot * 0.8));
                }
            }

            // --- SEGMENTED KELP STALKS ---
            SegmentedStalk(b, 75, 960, 280, 24, 40, 7);
            SegmentedStalk(b, 120, 960, 340, 26, -30, 8);
            SegmentedStalk(b, 175, 960, 300, 22, 50, 7);
            SegmentedStalk(b, 225, 960, 380, 28, -40, 9);
            SegmentedStalk(b, 510, 960, 440, 30, 25, 9);
            SegmentedStalk(b, 580, 960, 480, 32, -35, 10);
            SegmentedStalk(b, 730, 960, 460, 28, 30, 9);

            // --- BRANCHING CORAL REEF ---
            double[] coralX = { 810, 860, 910, 880, 930 };
            double[] coralH = { 260, 360, 320, 220, 180 };
            for (int i = 0; i < 5; i++)
            {
                b.R(G.RoundRect(coralX[i] - 10, 960 - coralH[i], 20, coralH[i], 10));
                for (int j = 1; j <= 4; j++)
                {
                    double cy = 960 - coralH[i] * (j / 4.5);
                    b.R(G.Petal(coralX[i] - 10, cy, 0, 42, 9, 230));
                    b.R(G.Petal(coralX[i] + 10, cy + 12, 0, 42, 9, 130));
                }
            }

            list.Add(b.Def("undersea_zentangle", "Zentangle Undersea", CatAnimals, 1, "undersea", "zentangle", "fish", "seahorse", "starfish", "coral", "ocean"));
        }

        private static void Owl(List<DesignDef> list)
        {
            var b = new B();
            b.R(G.RoundRect(40, 40, 920, 920, 36));
            b.R(G.Circle(790, 185, 92));
            CircleRing(b, 790, 185, 60, 6, 12);
            b.R(G.Sparkle(180, 165, 26, 9));
            b.R(G.Sparkle(300, 105, 18, 7));
            b.R(G.Sparkle(865, 350, 21, 8));
            b.R(G.Tri((310, 365), (392, 472), (456, 430)));
            b.R(G.Tri((690, 365), (608, 472), (544, 430)));
            Flower(b, 500, 390, 6, 26, 10, 6, 11);
            b.R(G.Ellipse(500, 570, 250, 300));
            b.R(G.EllipseRot(293, 585, 64, 185, 12));
            for (int i = 0; i < 5; i++)
            {
                b.R(G.Petal(293, 520 + i * 30, 0, 55, 16, 240));
                b.R(G.Circle(270, 520 + i * 30, 4.5));
            }
            b.R(G.EllipseRot(707, 585, 64, 185, -12));
            for (int i = 0; i < 5; i++)
            {
                b.R(G.Petal(707, 520 + i * 30, 0, 55, 16, 120));
                b.R(G.Circle(730, 520 + i * 30, 4.5));
            }
            b.R(G.Circle(395, 470, 78));
            PetalRing(b, 395, 470, 48, 28, 12, 8);
            b.R(G.Circle(395, 470, 48));
            b.R(G.Circle(395, 470, 24));
            b.R(G.Circle(395, 470, 10));
            b.R(G.Circle(605, 470, 78));
            PetalRing(b, 605, 470, 48, 28, 12, 8);
            b.R(G.Circle(605, 470, 48));
            b.R(G.Circle(605, 470, 24));
            b.R(G.Circle(605, 470, 10));
            b.R(G.PolyD(500, 512, 532, 562, 500, 638, 468, 562));
            for (int i = 0; i < 5; i++) b.R(UScallop(320 + i * 72, 320 + (i + 1) * 72, 585));
            for (int i = 0; i < 5; i++) b.R(UScallop(330 + i * 68, 330 + (i + 1) * 68, 645));
            for (int i = 0; i < 4; i++) b.R(UScallop(364 + i * 68, 364 + (i + 1) * 68, 705));
            for (int i = 0; i < 3; i++) b.R(UScallop(398 + i * 68, 398 + (i + 1) * 68, 765));
            b.R(G.PolyD(418, 846, 458, 846, 438, 806));
            b.R(G.PolyD(542, 846, 582, 846, 562, 806));
            b.R(G.RoundRect(115, 846, 770, 46, 22));
            for (int i = 0; i < 6; i++) b.R(G.RoundRect(140 + i * 110, 856, 80, 12, 6));
            b.R(G.Leaf(190, 846, 0, 88, 19, 160));
            b.R(G.Leaf(830, 846, 0, 88, 19, 20));
            Flower(b, 735, 796, 12, 36, 12, 8, 15);
            Flower(b, 265, 796, 12, 36, 12, 8, 15);
            Flower(b, 132, 625, 16, 54, 17, 8, 21);
            Flower(b, 868, 625, 16, 54, 17, 8, 21);
            list.Add(b.Def("owl", "Zentangle Owl", CatAnimals, 2, "owl", "bird", "floral", "night", "zentangle", "feathers"));
        }

        private static void Butterfly(List<DesignDef> list)
        {
            var b = new B();
            b.R(G.RoundRect(40, 40, 920, 920, 36));
            Flower(b, 140, 140, 12, 44, 16, 8, 16);
            Flower(b, 860, 140, 12, 44, 16, 8, 16);
            Flower(b, 140, 860, 12, 44, 16, 8, 16);
            Flower(b, 860, 860, 12, 44, 16, 8, 16);
            b.R(G.Leaf(485, 330, 0, 130, 8, 330));
            b.R(G.Leaf(515, 330, 0, 130, 8, 30));
            ConcentricBubble(b, 420, 225, 14);
            ConcentricBubble(b, 580, 225, 14);
            b.R(G.Circle(500, 360, 22));
            b.R(G.Circle(500, 360, 10));
            b.R(G.RoundRect(486, 385, 28, 100, 14));
            b.R(G.RoundRect(488, 490, 24, 180, 12));
            for (int i = 0; i < 6; i++) b.R(G.RoundRect(490, 505 + i * 26, 20, 18, 8));
            b.R(G.PolyD(470, 380, 260, 180, 130, 280, 200, 460, 470, 450));
            PetalRing(b, 330, 320, 15, 65, 20, 6, 220);
            CircleRing(b, 230, 340, 45, 5, 8);
            CircleRing(b, 170, 270, 25, 4, 6);
            b.R(G.PolyD(530, 380, 740, 180, 870, 280, 800, 460, 530, 450));
            PetalRing(b, 670, 320, 15, 65, 20, 6, 140);
            CircleRing(b, 770, 340, 45, 5, 8);
            CircleRing(b, 830, 270, 25, 4, 6);
            b.R(G.PolyD(470, 470, 240, 520, 210, 690, 370, 760, 470, 580));
            DropRing(b, 330, 630, 16, 45, 4, 210);
            CircleRing(b, 270, 690, 30, 4, 7);
            b.R(G.PolyD(530, 470, 760, 520, 790, 690, 630, 760, 530, 580));
            DropRing(b, 670, 630, 16, 45, 4, 150);
            CircleRing(b, 730, 690, 30, 4, 7);
            list.Add(b.Def("butterfly", "Zentangle Butterfly", CatAnimals, 3, "butterfly", "insects", "symmetry", "wings", "zentangle"));
        }

        private static void Koi(List<DesignDef> list)
        {
            var b = new B();
            b.R(G.RoundRect(40, 40, 920, 920, 36));
            b.R(G.WaveBandTop(40, 960, 40, 118, 18, 300, 0));
            b.R(G.WaveBand(40, 960, 118, 84, 18, 300, 0));
            b.R(G.WaveBandBetween(40, 960, 118, 84, 18, 300, 0, 782, 22, 340, 2.0));
            b.R(G.WaveBand(40, 960, 782, 138, 22, 340, 2.0));
            b.R(G.WaveBandBottom(40, 960, 782, 138, 22, 340, 2.0, 960));
            b.R(G.EllipseRot(405, 425, 140, 78, -14));
            for (int i = 0; i < 4; i++) b.R(UScallop(340 + i * 28, 368 + i * 28, 410));
            for (int i = 0; i < 4; i++) b.R(UScallop(350 + i * 28, 378 + i * 28, 435));
            CircleRing(b, 405, 425, 40, 6, 6);
            b.R(G.Petal(405, 425, 130, 80, 26, 164));
            b.R(G.Petal(405, 425, 126, 68, 20, 180));
            b.R(G.Circle(500, 385, 12));
            b.R(G.Circle(500, 385, 5));
            b.R(G.EllipseRot(620, 600, 115, 65, 191));
            for (int i = 0; i < 3; i++) b.R(UScallop(580 + i * 26, 606 + i * 26, 595));
            for (int i = 0; i < 3; i++) b.R(UScallop(590 + i * 26, 616 + i * 26, 618));
            b.R(G.Petal(620, 600, 105, 65, 22, 12));
            b.R(G.Circle(540, 640, 10));
            b.R(G.Circle(540, 640, 4));
            b.R(G.RingSector(172, 252, 3, 85, -32, 328));
            for (int i = 0; i < 6; i++) b.R(G.Petal(172, 252, 5, 75, 10, i * 60));
            b.R(G.RingSector(832, 228, 3, 70, 20, 380));
            b.R(G.RingSector(792, 782, 3, 95, 140, 500));
            Flower(b, 762, 742, 10, 36, 12, 8, 14);
            Flower(b, 198, 210, 6, 26, 10, 6, 9);
            double[,] kBubbles = { { 520, 208, 14 }, { 560, 178, 9 }, { 352, 700, 12 }, { 884, 540, 11 }, { 118, 470, 9 } };
            for (int i = 0; i < 5; i++) ConcentricBubble(b, kBubbles[i, 0], kBubbles[i, 1], kBubbles[i, 2]);
            b.R(G.Ellipse(350, 932, 30, 19));
            b.R(G.Ellipse(430, 944, 26, 16));
            b.R(G.Ellipse(600, 938, 32, 19));
            b.R(G.Ellipse(252, 948, 24, 15));
            list.Add(b.Def("koi", "Zentangle Koi", CatAnimals, 4, "koi", "fish", "pond", "water", "lily", "zentangle"));
        }

        private static void Peacock(List<DesignDef> list)
        {
            var b = new B();
            b.R(G.RoundRect(40, 40, 920, 920, 36));
            const double cx = 500, cy = 680;
            for (int i = 0; i < 11; i++)
            {
                double angle = -75 + i * 15;
                b.R(G.Petal(cx, cy, 130, 330, 38, angle));
                var ep = G.Pt(cx, cy, 390, angle);
                b.R(G.Ellipse(ep.X, ep.Y, 28, 38));
                b.R(G.Circle(ep.X, ep.Y, 18));
                b.R(G.Circle(ep.X, ep.Y, 10));
                b.R(G.Circle(ep.X, ep.Y, 4));
            }
            b.R(G.Circle(cx, 440, 32));
            b.R(G.Tri((cx, 430), (cx + 42, 440), (cx, 452)));
            b.R(G.Circle(cx - 8, 436, 8));
            b.R(G.Circle(cx - 8, 436, 3));
            for (int i = 0; i < 5; i++)
            {
                var cp = G.Pt(cx, 405, 45, -30 + i * 15);
                b.R(G.Circle(cp.X, cp.Y, 9));
                b.R(G.Circle(cp.X, cp.Y, 4));
            }
            b.R(G.PolyD(cx - 22, 465, cx + 22, 465, cx + 50, 590, cx - 50, 590));
            b.R(G.Ellipse(cx, 630, 68, 95));
            for (int i = 0; i < 4; i++) b.R(UScallop(cx - 45 + i * 22, cx - 45 + (i + 1) * 22, 605));
            for (int i = 0; i < 4; i++) b.R(UScallop(cx - 45 + i * 22, cx - 45 + (i + 1) * 22, 635));
            for (int i = 0; i < 3; i++) b.R(UScallop(cx - 33 + i * 22, cx - 33 + (i + 1) * 22, 665));
            b.R(G.RoundRect(150, 750, 700, 42, 20));
            Flower(b, 230, 820, 10, 36, 12, 8, 14);
            Flower(b, 770, 820, 10, 36, 12, 8, 14);
            list.Add(b.Def("peacock", "Zentangle Peacock", CatAnimals, 5, "peacock", "bird", "feathers", "ornate", "royal", "zentangle"));
        }

        private static void Seahorse(List<DesignDef> list)
        {
            var b = new B();
            b.R(G.RoundRect(40, 40, 920, 920, 36));
            double[,] sBubbles = { { 150, 160, 18 }, { 820, 180, 16 }, { 180, 480, 14 }, { 840, 520, 15 }, { 320, 320, 12 } };
            for (int i = 0; i < 5; i++) ConcentricBubble(b, sBubbles[i, 0], sBubbles[i, 1], sBubbles[i, 2]);
            for (int i = 0; i < 4; i++)
            {
                b.R(G.RoundRect(750 + i * 35, 520, 16, 400, 8));
                for (int j = 1; j <= 3; j++)
                    b.R(G.Petal(750 + i * 35, 540 + j * 70, 0, 45, 10, 130));
            }
            const double cx = 450, cy = 480;
            Flower(b, cx - 60, cy - 220, 4, 22, 8, 6, 8);
            b.R(G.PolyD(cx - 70, cy - 200, cx - 10, cy - 240, cx + 20, cy - 140, cx - 40, cy - 100));
            b.R(G.RoundRect(cx - 90, cy - 180, 28, 110, 8));
            b.R(G.Circle(cx - 76, cy - 70, 10));
            b.R(G.Circle(cx - 30, cy - 170, 16));
            b.R(G.Circle(cx - 30, cy - 170, 6));
            b.R(G.PolyD(cx - 20, cy - 140, cx + 60, cy - 170, cx + 110, cy + 30, cx + 20, cy + 90, cx - 30, cy - 20));
            for (int i = 0; i < 9; i++)
            {
                var sp = G.Pt(cx + 20, cy - 80, 75, -40 + i * 18);
                b.R(G.Circle(sp.X, sp.Y, 9));
                b.R(G.Circle(sp.X, sp.Y, 4));
            }
            for (int i = 0; i < 8; i++)
            {
                double by = cy - 120 + i * 24;
                b.R(G.PolyD(cx - 40 + i * 7, by, cx + 25 + i * 7, by - 8, cx + 30 + i * 7, by + 14, cx - 35 + i * 7, by + 22));
            }
            b.R(G.Ring(cx + 40, cy + 240, 120, 70));
            b.R(G.Ring(cx + 40, cy + 240, 70, 30));
            b.R(G.Circle(cx + 40, cy + 240, 24));
            for (int i = 0; i < 12; i++)
            {
                double a = -30 + i * 30;
                b.R(G.RingSector(cx + 40, cy + 240, 74, 116, a + 1.5, a + 28.5));
            }
            SegmentedStalk(b, 180, 960, 420, 28, 40, 8);
            SegmentedStalk(b, 260, 960, 360, 24, -30, 7);
            list.Add(b.Def("seahorse", "Zentangle Seahorse", CatAnimals, 6, "seahorse", "ocean", "sea", "zentangle", "coral"));
        }

        private static void Turtle(List<DesignDef> list)
        {
            var b = new B();
            b.R(G.RoundRect(40, 40, 920, 920, 36));
            b.R(G.WaveBandTop(40, 960, 40, 140, 16, 280, 0));
            b.R(G.WaveBand(40, 960, 140, 60, 16, 280, 0));
            b.R(G.WaveBandBetween(40, 960, 140, 60, 16, 280, 0, 820, 20, 320, 1.5));
            b.R(G.WaveBand(40, 960, 820, 80, 20, 320, 1.5));
            b.R(G.WaveBandBottom(40, 960, 820, 80, 20, 320, 1.5, 960));
            ConcentricBubble(b, 180, 260, 16);
            ConcentricBubble(b, 820, 290, 18);
            b.R(G.RoundRect(465, 170, 70, 110, 32));
            b.R(G.Circle(460, 210, 10));
            b.R(G.Circle(460, 210, 4));
            b.R(G.Circle(540, 210, 10));
            b.R(G.Circle(540, 210, 4));
            b.R(G.PolyD(440, 360, 180, 270, 110, 340, 240, 470, 410, 430));
            CircleRing(b, 240, 370, 45, 6, 8);
            b.R(G.PolyD(560, 360, 820, 270, 890, 340, 760, 470, 590, 430));
            CircleRing(b, 760, 370, 45, 6, 8);
            b.R(G.PolyD(420, 680, 250, 760, 280, 830, 430, 760));
            b.R(G.PolyD(580, 680, 750, 760, 720, 830, 570, 760));
            b.R(G.Tri((500, 760), (480, 820), (520, 820)));
            b.R(G.Ellipse(500, 530, 195, 235));
            double[] hy = { 380, 480, 580, 675 };
            for (int i = 0; i < 4; i++)
            {
                b.R(G.Hexagon(500, hy[i], 46, 0));
                b.R(G.Hexagon(500, hy[i], 24, 0));
            }
            double[,] lHex = { { 400, 430 }, { 600, 430 }, { 390, 530 }, { 610, 530 }, { 405, 630 }, { 595, 630 } };
            for (int i = 0; i < 6; i++)
            {
                b.R(G.Hexagon(lHex[i, 0], lHex[i, 1], 40, 30));
                b.R(G.Hexagon(lHex[i, 0], lHex[i, 1], 20, 30));
            }
            RingSectors(b, 500, 530, 160, 195, 16, 2.0);
            list.Add(b.Def("turtle", "Zentangle Sea Turtle", CatAnimals, 7, "turtle", "ocean", "sea", "reptile", "marine", "zentangle"));
        }

        private static void Fox(List<DesignDef> list)
        {
            var b = new B();
            b.R(G.RoundRect(40, 40, 920, 920, 36));
            b.R(G.Sparkle(140, 135, 22, 8));
            b.R(G.Sparkle(858, 150, 26, 9));
            b.R(G.Circle(240, 245, 10));
            b.R(G.Sparkle(762, 305, 16, 6));
            b.R(G.Hill(40, 470, 858, 700));
            b.R(G.Hill(320, 720, 898, 726));
            b.R(G.Hill(590, 960, 858, 694));
            b.R(G.PolyD(320, 322, 362, 128, 482, 302));
            b.R(G.PolyD(352, 292, 372, 182, 442, 288));
            b.R(G.PolyD(680, 322, 638, 128, 518, 302));
            b.R(G.PolyD(648, 292, 628, 182, 558, 288));
            b.R(G.PolyD(500, 252, 722, 402, 692, 562, 500, 662, 308, 562, 278, 402));
            b.R(G.PolyD(500, 252, 560, 332, 500, 422, 440, 332));
            b.R(G.PolyD(322, 422, 352, 542, 432, 482));
            b.R(G.PolyD(678, 422, 648, 542, 568, 482));
            b.R(G.Ellipse(500, 592, 112, 70));
            b.R(G.RoundRect(474, 544, 52, 40, 18));
            b.R(UScallop(366, 422, 468));
            b.R(UScallop(578, 634, 468));
            b.R(G.PolyD(430, 676, 500, 770, 570, 676));
            for (int i = 0; i < 5; i++)
                b.R(G.Petal(500, 740, 20 + i * 15, 110 - i * 10, 24, 180 + (i % 2 == 0 ? 25 : -25)));
            Pine(b, 130, 862, 120, 190);
            Pine(b, 872, 862, 108, 160);
            Flower(b, 250, 892, 8, 28, 10, 6, 13);
            Flower(b, 748, 906, 8, 28, 10, 6, 13);
            list.Add(b.Def("fox", "Zentangle Fox", CatAnimals, 8, "fox", "animal", "night", "hills", "zentangle"));
        }

        private static void Chameleon(List<DesignDef> list)
        {
            var b = new B();
            b.R(G.RoundRect(40, 40, 920, 920, 36));
            b.R(G.Circle(800, 170, 70));
            SunRays(b, 800, 170, 85, 28, 8);
            b.R(G.RoundRect(100, 640, 800, 46, 22));
            for (int i = 0; i < 6; i++) b.R(G.RoundRect(140 + i * 110, 652, 70, 14, 6));
            b.R(G.Leaf(200, 640, 0, 90, 28, 220));
            b.R(G.Leaf(350, 686, 0, 80, 24, 150));
            b.R(G.Leaf(650, 686, 0, 85, 26, 160));
            b.R(G.Leaf(800, 640, 0, 95, 30, 40));
            b.R(G.Ring(260, 540, 110, 60));
            b.R(G.Circle(260, 540, 40));
            for (int i = 0; i < 8; i++) b.R(G.RingSector(260, 540, 64, 106, i * 45, (i + 1) * 45 - 2));
            b.R(G.EllipseRot(480, 510, 170, 100, 8));
            for (int i = 0; i < 6; i++)
                b.R(G.Tri((390 + i * 28, 430 - (i < 3 ? i * 15 : (5 - i) * 15)),
                          (404 + i * 28, 400 - (i < 3 ? i * 15 : (5 - i) * 15)),
                          (418 + i * 28, 430 - (i < 3 ? i * 15 : (5 - i) * 15))));
            CircleRing(b, 480, 510, 65, 8, 16);
            CircleRing(b, 480, 510, 28, 4, 12);
            b.R(G.PolyD(590, 470, 710, 410, 750, 490, 690, 560, 590, 540));
            b.R(G.PolyD(640, 420, 700, 360, 720, 420));
            b.R(G.Circle(670, 485, 36));
            b.R(G.Circle(670, 485, 22));
            b.R(G.Circle(670, 485, 10));
            b.R(G.Ring(780, 530, 40, 26));
            b.R(G.Circle(840, 510, 10));
            b.R(G.PolyD(420, 600, 390, 645, 440, 645));
            b.R(G.PolyD(550, 590, 530, 645, 580, 645));
            list.Add(b.Def("chameleon", "Zentangle Chameleon", CatAnimals, 9, "chameleon", "lizard", "jungle", "tropical", "zentangle"));
        }

        private static void Wolf(List<DesignDef> list)
        {
            var b = new B();
            b.R(G.RoundRect(40, 40, 920, 920, 36));
            b.R(G.Circle(300, 250, 110));
            CircleRing(b, 300, 250, 70, 6, 14);
            b.R(G.Sparkle(720, 150, 24, 8));
            b.R(G.Sparkle(840, 260, 20, 7));
            b.R(G.WaveBand(40, 960, 340, 80, 24, 300, 0));
            b.R(G.PolyD(40, 960, 40, 620, 480, 520, 750, 960));
            b.R(G.PolyD(450, 680, 720, 560, 960, 650, 960, 960, 680, 960));
            b.R(G.PolyD(480, 520, 520, 450, 560, 380, 620, 320, 650, 300, 660, 330, 610, 380, 580, 460, 620, 570, 480, 520));
            b.R(G.PolyD(560, 380, 640, 290, 660, 305, 610, 395));
            b.R(G.PolyD(590, 395, 640, 340, 630, 360));
            b.R(G.PolyD(550, 360, 540, 290, 575, 340));
            b.R(G.Circle(585, 360, 6));
            for (int i = 0; i < 7; i++)
                b.R(G.PolyD(490 + i * 20, 460 + i * 22, 465 + i * 20, 520 + i * 22, 515 + i * 20, 500 + i * 22));
            Pine(b, 140, 630, 90, 140);
            Pine(b, 860, 660, 100, 150);
            list.Add(b.Def("wolf", "Zentangle Wolf", CatAnimals, 10, "wolf", "moon", "howl", "night", "wilderness", "zentangle"));
        }

        // =========================================================================
        // NATURE (10)
        // =========================================================================

        private static void Ocean(List<DesignDef> list)
        {
            var b = new B();
            b.R(G.RoundRect(40, 40, 920, 920, 36));
            b.R(G.WaveBandTop(40, 960, 40, 522, 16, 300, 0));
            b.R(G.WaveBand(40, 960, 522, 62, 16, 300, 0));
            b.R(G.WaveBandBetween(40, 960, 522, 62, 16, 300, 0, 616, 18, 260, 1.2));
            b.R(G.WaveBand(40, 960, 616, 62, 18, 260, 1.2));
            b.R(G.WaveBandBetween(40, 960, 616, 62, 18, 260, 1.2, 710, 16, 340, 2.4));
            b.R(G.WaveBand(40, 960, 710, 62, 16, 340, 2.4));
            b.R(G.WaveBandBetween(40, 960, 710, 62, 16, 340, 2.4, 804, 20, 280, 3.6));
            b.R(G.WaveBand(40, 960, 804, 112, 20, 280, 3.6));
            b.R(G.WaveBandBottom(40, 960, 804, 112, 20, 280, 3.6, 960));
            double[,] obs = { { 200, 202, 14 }, { 240, 162, 9 }, { 700, 182, 16 }, { 758, 142, 10 }, { 500, 122, 12 } };
            for (int i = 0; i < 5; i++) ConcentricBubble(b, obs[i, 0], obs[i, 1], obs[i, 2]);
            for (int i = 0; i < 7; i++)
                b.R(G.RingSector(300, 692, 6, 96, -85 + i * 28 + 1.5, -85 + (i + 1) * 28 - 1.5));
            b.R(G.Circle(300, 700, 16));
            b.R(G.Star(680, 758, 5, 100, 42, -90));
            b.R(G.Circle(680, 758, 16));
            for (int arm = 0; arm < 5; arm++)
            {
                double a = -90 + arm * 72;
                var sp1 = G.Pt(680, 758, 40, a);
                var sp2 = G.Pt(680, 758, 70, a);
                b.R(G.Circle(sp1.X, sp1.Y, 7));
                b.R(G.Circle(sp2.X, sp2.Y, 5));
            }
            for (int i = 0; i < 5; i++)
                b.R(G.RingSector(834, 852, 5, 65, -73 + i * 30 + 1.5, -73 + (i + 1) * 30 - 1.5));
            b.R(G.Circle(834, 858, 12));
            b.R(G.Leaf(158, 934, 0, 282, 27, 6));
            b.R(G.Leaf(212, 934, 0, 238, 22, 352));
            b.R(G.Leaf(884, 934, 0, 258, 24, 354));
            b.R(G.Leaf(108, 930, 0, 200, 20, 10));
            list.Add(b.Def("ocean", "Ocean Garden", CatNature, 11, "ocean", "sea", "shell", "starfish", "waves", "zentangle"));
        }

        private static void Tree(List<DesignDef> list)
        {
            var b = new B();
            b.R(G.RoundRect(40, 40, 920, 920, 36));
            b.R(G.Circle(172, 162, 72));
            SunRays(b, 172, 162, 88, 32, 8);
            b.R(G.Cloud(560, 168, 240, 90));
            b.R(G.Cloud(860, 300, 180, 66));
            b.R(G.Hill(40, 960, 962, 812));
            b.R(G.Hill(210, 790, 962, 848));
            b.R(G.RoundRect(462, 470, 76, 416, 26));
            for (int i = 0; i < 5; i++)
                b.R(G.RoundRect(470, 500 + i * 70, 60, 24, 8));
            b.R(G.Petal(500, 560, 36, 202, 21, 158));
            b.R(G.Petal(500, 560, 36, 192, 21, 22));
            double[,] cpods = {
                { 500, 300, 150 }, { 382, 362, 110 }, { 618, 362, 110 }, { 312, 300, 80 },
                { 688, 300, 80 }, { 422, 222, 90 }, { 578, 222, 90 }, { 500, 182, 90 },
                { 352, 232, 70 }, { 648, 232, 70 }, { 560, 142, 60 }, { 440, 142, 60 }
            };
            for (int i = 0; i < 12; i++) b.R(G.Circle(cpods[i, 0], cpods[i, 1], cpods[i, 2]));
            Flower(b, 352, 272, 8, 27, 10, 6, 11);
            Flower(b, 602, 222, 8, 27, 10, 6, 11);
            Flower(b, 478, 344, 8, 27, 10, 6, 11);
            Flower(b, 662, 332, 8, 27, 10, 6, 11);
            Flower(b, 422, 184, 8, 27, 10, 6, 11);
            list.Add(b.Def("tree", "Blooming Tree of Life", CatNature, 12, "tree", "blossom", "garden", "spring", "treeoflife"));
        }

        private static void Cactus(List<DesignDef> list)
        {
            var b = new B();
            b.R(G.RoundRect(40, 40, 920, 920, 36));
            b.R(G.Circle(800, 162, 68));
            SunRays(b, 800, 162, 84, 30, 8);
            b.R(G.RoundRect(118, 800, 764, 26, 12));
            b.R(G.PolyD(330, 828, 670, 828, 630, 952, 370, 952));
            b.R(G.PolyD(345, 862, 655, 862, 642, 892, 358, 892));
            b.R(G.PolyD(367, 912, 633, 912, 622, 936, 378, 936));
            for (int i = 0; i < 6; i++) b.R(G.Diamond(380 + i * 48, 877, 24, 20));
            b.R(G.RoundRect(314, 788, 372, 44, 18));
            b.R(G.RoundRect(455, 462, 90, 340, 45));
            b.R(G.RoundRect(340, 560, 152, 80, 40));
            b.R(G.RoundRect(340, 462, 80, 140, 40));
            b.R(G.RoundRect(508, 612, 152, 80, 40));
            b.R(G.RoundRect(580, 504, 80, 150, 40));
            double[] sp = { 470, 502, 532, 544, 470, 604, 532, 654, 468, 704, 366, 592, 376, 482, 606, 642 };
            for (int i = 0; i < 8; i++) b.R(G.Circle(sp[i * 2], sp[i * 2 + 1], 6));
            Flower(b, 500, 442, 10, 34, 11, 8, 13);
            Flower(b, 380, 444, 9, 27, 9, 8, 11);
            b.R(G.Ellipse(240, 774, 60, 38));
            Flower(b, 240, 728, 8, 21, 7, 8, 9);
            list.Add(b.Def("cactus", "Cactus Bloom", CatNature, 13, "cactus", "desert", "plant", "pot", "flower"));
        }

        private static void Lake(List<DesignDef> list)
        {
            var b = new B();
            b.R(G.RoundRect(40, 40, 920, 920, 36));
            b.R(G.Circle(820, 172, 68));
            SunRays(b, 820, 172, 84, 30, 8);
            b.R(G.Cloud(205, 182, 225, 82));
            b.R(G.Cloud(482, 128, 172, 60));
            b.R(G.PolyD(150, 622, 392, 342, 634, 622));
            b.R(G.PolyD(338, 386, 392, 342, 446, 386, 426, 374, 406, 390, 392, 372, 378, 390, 358, 374));
            b.R(G.PolyD(598, 622, 782, 386, 962, 622));
            b.R(G.PolyD(738, 424, 782, 386, 828, 424, 810, 414, 794, 428, 782, 408, 770, 428, 754, 414));
            b.R(G.PolyD(44, 622, 332, 264, 642, 622));
            b.R(G.PolyD(282, 312, 332, 264, 382, 312, 362, 298, 344, 318, 332, 294, 320, 318, 302, 298));
            Pine(b, 122, 624, 112, 152);
            Pine(b, 702, 624, 124, 172);
            Pine(b, 884, 624, 102, 132);
            b.R(G.WaveBand(40, 960, 644, 104, 14, 320, 0));
            b.R(G.WaveBandBetween(40, 960, 644, 104, 14, 320, 0, 762, 16, 280, 2.0));
            b.R(G.WaveBand(40, 960, 762, 104, 16, 280, 2.0));
            b.R(G.WaveBandBetween(40, 960, 762, 104, 16, 280, 2.0, 882, 18, 300, 4.0));
            b.R(G.WaveBand(40, 960, 882, 62, 18, 300, 4.0));
            b.R(G.WaveBandBottom(40, 960, 882, 62, 18, 300, 4.0, 960));
            list.Add(b.Def("lake", "Mountain Lake", CatNature, 14, "mountain", "lake", "pine", "landscape", "sun"));
        }

        private static void Jungle(List<DesignDef> list)
        {
            var b = new B();
            b.R(G.RoundRect(40, 40, 920, 920, 36));
            b.R(G.Leaf(350, 450, 0, 320, 110, 320));
            for (int i = 1; i <= 4; i++)
            {
                b.R(G.Leaf(350, 450, i * 60, 120, 22, 270));
                b.R(G.Leaf(350, 450, i * 60, 120, 22, 10));
            }
            b.R(G.Leaf(650, 450, 0, 340, 100, 40));
            for (int i = 0; i < 7; i++)
                b.R(G.Petal(650, 450, 50, 260, 18, -10 + i * 15));
            b.R(G.PolyD(420, 750, 580, 680, 500, 700));
            b.R(G.Petal(500, 690, 0, 140, 20, 335));
            b.R(G.Petal(500, 690, 0, 160, 22, 350));
            b.R(G.Petal(500, 690, 0, 150, 20, 10));
            b.R(G.Petal(500, 690, 0, 130, 18, 30));
            b.R(G.Petal(500, 690, 0, 90, 14, 300));
            b.R(G.Hill(40, 960, 960, 830));
            b.R(G.Leaf(150, 920, 0, 180, 22, 25));
            b.R(G.Leaf(850, 920, 0, 180, 22, 335));
            list.Add(b.Def("jungle", "Tropical Rainforest", CatNature, 15, "jungle", "monstera", "palm", "tropical", "leaves"));
        }

        private static void Sunflower(List<DesignDef> list)
        {
            var b = new B();
            b.R(G.RoundRect(40, 40, 920, 920, 36));
            b.R(G.Circle(160, 160, 65));
            SunRays(b, 160, 160, 80, 26, 8);
            b.R(G.RoundRect(480, 520, 40, 440, 16));
            b.R(G.Leaf(480, 680, 0, 240, 70, 235));
            b.R(G.Leaf(520, 740, 0, 240, 70, 125));
            PetalRing(b, 500, 420, 150, 190, 42, 18);
            PetalRing(b, 500, 420, 120, 140, 36, 18, 10);
            b.R(G.Circle(500, 420, 125));
            CircleRing(b, 500, 420, 95, 16, 11);
            CircleRing(b, 500, 420, 65, 12, 10);
            CircleRing(b, 500, 420, 35, 8, 9);
            b.R(G.Circle(500, 420, 16));
            b.R(G.EllipseRot(780, 260, 38, 24, 30));
            b.R(G.RoundRect(760, 246, 14, 28, 6));
            b.R(G.RoundRect(784, 258, 14, 28, 6));
            b.R(G.Leaf(765, 240, 0, 55, 18, 300));
            b.R(G.Leaf(785, 235, 0, 50, 16, 330));
            list.Add(b.Def("sunflower", "Sunflower Field", CatNature, 16, "sunflower", "flower", "summer", "bee", "garden"));
        }

        private static void MushroomGrove(List<DesignDef> list)
        {
            var b = new B();
            b.R(G.RoundRect(40, 40, 920, 920, 36));
            b.R(G.Sparkle(180, 160, 22, 8));
            b.R(G.Sparkle(820, 180, 20, 7));
            b.R(G.Hill(40, 960, 960, 780));
            b.R(G.Hill(200, 800, 960, 820));
            b.R(Dome(310, 690, 480, 170));
            CircleRing(b, 500, 430, 80, 6, 18);
            CircleRing(b, 500, 430, 40, 4, 14);
            b.R(G.PolyD(440, 480, 560, 480, 580, 720, 540, 870, 460, 870, 420, 720));
            b.R(G.Ellipse(500, 530, 80, 20));
            b.R(Dome(170, 330, 620, 70));
            b.R(G.Circle(230, 590, 11));
            b.R(G.Circle(280, 585, 10));
            b.R(G.PolyD(225, 620, 275, 620, 285, 780, 215, 780));
            b.R(Dome(670, 830, 600, 75));
            b.R(G.Circle(730, 570, 12));
            b.R(G.Circle(780, 575, 9));
            b.R(G.PolyD(725, 600, 775, 600, 785, 780, 715, 780));
            b.R(G.Ring(280, 870, 32, 18));
            b.R(G.Circle(280, 870, 10));
            b.R(G.RoundRect(240, 885, 80, 18, 9));
            Flower(b, 720, 880, 8, 24, 9, 5, 11);
            Flower(b, 400, 900, 8, 24, 9, 5, 11);
            list.Add(b.Def("mushrooms_nature", "Mushroom Grove", CatNature, 17, "mushroom", "forest", "nature", "grove", "fungi"));
        }

        private static void AutumnHarvest(List<DesignDef> list)
        {
            var b = new B();
            b.R(G.RoundRect(40, 40, 920, 920, 36));
            b.R(G.WaveBand(40, 960, 180, 50, 16, 280, 0));
            b.R(G.Ellipse(500, 590, 220, 180));
            b.R(G.Ellipse(500, 590, 150, 175));
            b.R(G.Ellipse(500, 590, 80, 170));
            b.R(G.PolyD(475, 415, 525, 415, 545, 340, 505, 320, 490, 340));
            b.R(Dome(180, 280, 720, 50));
            b.R(G.PolyD(190, 720, 270, 720, 230, 820));
            b.R(Dome(270, 360, 760, 45));
            b.R(G.PolyD(280, 760, 350, 760, 315, 850));
            b.R(G.Leaf(760, 680, 0, 190, 55, 35));
            b.R(G.Leaf(820, 770, 0, 180, 50, 75));
            for (int i = 0; i < 5; i++)
            {
                b.R(G.Leaf(180, 480, i * 40, 55, 16, 320));
                b.R(G.Leaf(820, 480, i * 40, 55, 16, 40));
            }
            b.R(G.RoundRect(100, 870, 800, 40, 18));
            list.Add(b.Def("harvest", "Autumn Harvest", CatNature, 18, "autumn", "pumpkin", "harvest", "leaves", "acorn"));
        }

        private static void ZenWater(List<DesignDef> list)
        {
            var b = new B();
            b.R(G.RoundRect(40, 40, 920, 920, 36));
            for (int i = 0; i < 8; i++)
                b.R(G.RoundRect(120 + i * 96, 80, 80, 480, 12));
            b.R(G.PolyD(320, 380, 580, 470, 565, 520, 305, 430));
            b.R(G.Ellipse(312, 405, 16, 26));
            b.R(G.PolyD(308, 418, 316, 418, 340, 680, 310, 680));
            b.R(G.Ellipse(680, 560, 45, 24));
            b.R(G.Ellipse(680, 600, 65, 30));
            b.R(G.Ellipse(680, 650, 88, 36));
            b.R(G.Ellipse(680, 715, 115, 46));
            b.R(G.Ellipse(680, 795, 145, 56));
            b.R(G.Ring(325, 780, 160, 130));
            b.R(G.Ring(325, 780, 105, 75));
            b.R(G.Circle(325, 780, 50));
            Flower(b, 220, 740, 8, 28, 10, 6, 12);
            list.Add(b.Def("zenwater", "Zen Water Garden", CatNature, 19, "zen", "water", "stones", "bamboo", "peace"));
        }

        private static void Bonsai(List<DesignDef> list)
        {
            var b = new B();
            b.R(G.RoundRect(40, 40, 920, 920, 36));
            b.R(G.Ring(500, 440, 380, 350));
            b.R(G.PolyD(120, 440, 880, 440, 880, 450, 120, 450));
            b.R(G.PolyD(500, 60, 500, 820, 510, 820, 510, 60));
            b.R(G.RoundRect(280, 760, 440, 70, 16));
            b.R(G.RoundRect(320, 830, 45, 24, 8));
            b.R(G.RoundRect(635, 830, 45, 24, 8));
            b.R(G.RoundRect(220, 854, 560, 26, 10));
            b.R(G.PolyD(460, 760, 540, 760, 520, 620, 620, 540, 560, 510, 480, 560, 430, 470, 360, 480, 410, 560, 440, 760));
            b.R(G.PolyD(480, 560, 450, 420, 490, 410, 510, 520));
            b.R(G.Cloud(320, 460, 180, 75));
            b.R(G.Cloud(460, 390, 200, 85));
            b.R(G.Cloud(660, 510, 190, 80));
            b.R(G.Cloud(580, 340, 220, 90));
            b.R(G.Cloud(420, 280, 170, 70));
            b.R(G.RoundRect(720, 320, 50, 70, 10));
            b.R(G.PolyD(705, 320, 785, 320, 745, 290));
            b.R(G.PolyD(744, 290, 746, 290, 746, 120, 744, 120));
            list.Add(b.Def("bonsai", "Bonsai Masterpiece", CatNature, 20, "bonsai", "tree", "zen", "japan", "garden"));
        }

        // =========================================================================
        // MANDALAS (10)
        // =========================================================================

        private static void Lotus(List<DesignDef> list)
        {
            var b = new B();
            b.R(G.RoundRect(40, 40, 920, 920, 36));
            const double cx = 500, cy = 500;
            b.R(G.Circle(cx, cy, 56));
            PetalRing(b, cx, cy, 58, 108, 30, 8);
            PetalRing(b, cx, cy, 175, 62, 16, 16, 11.25);
            RingSectors(b, cx, cy, 245, 305, 16, 2.0);
            CircleRing(b, cx, cy, 338, 16, 13, 11.25);
            PetalRing(b, cx, cy, 352, 90, 34, 8, 22.5);
            ScallopRing(b, cx, cy, 452, 16);
            list.Add(b.Def("lotus", "Lotus Mandala", CatMandalas, 21, "mandala", "lotus", "meditation", "symmetry"));
        }

        private static void StarMandala(List<DesignDef> list)
        {
            var b = new B();
            b.R(G.RoundRect(40, 40, 920, 920, 36));
            const double cx = 500, cy = 500;
            b.R(G.Star(cx, cy, 8, 72, 30));
            for (int i = 0; i < 8; i++)
            {
                var p1 = G.Pt(cx, cy, 162, i * 45);
                var p2 = G.Pt(cx, cy, 116, i * 45 + 22.5);
                var p3 = G.Pt(cx, cy, 72, i * 45);
                var p4 = G.Pt(cx, cy, 116, i * 45 - 22.5);
                b.R(G.Poly(p1, p2, p3, p4));
            }
            PetalRing(b, cx, cy, 178, 56, 14, 16, 11.25);
            for (int i = 0; i < 8; i++) { var p = G.Pt(cx, cy, 284, i * 45 + 22.5); b.R(G.Star(p.X, p.Y, 5, 34, 14, i * 45)); }
            RingSectors(b, cx, cy, 318, 368, 12, 3.0);
            CircleRing(b, cx, cy, 396, 16, 9);
            ScallopRing(b, cx, cy, 452, 16);
            list.Add(b.Def("starmandala", "Star Mandala", CatMandalas, 22, "mandala", "star", "geometry", "symmetry"));
        }

        private static void BloomMandala(List<DesignDef> list)
        {
            var b = new B();
            b.R(G.RoundRect(40, 40, 920, 920, 36));
            const double cx = 500, cy = 500;
            b.R(G.Circle(cx, cy, 42));
            PetalRing(b, cx, cy, 44, 88, 34, 6);
            PetalRing(b, cx, cy, 132, 76, 23, 12, 15);
            CircleRing(b, cx, cy, 224, 12, 12, 15);
            RingSectors(b, cx, cy, 240, 288, 12, 3.0);
            DropRing(b, cx, cy, 318, 17, 42, 12, 15);
            b.R(G.Ring(cx, cy, 424, 392));
            ScallopRing(b, cx, cy, 452, 16);
            list.Add(b.Def("bloommandala", "Bloom Mandala", CatMandalas, 23, "mandala", "flower", "bloom", "meditation"));
        }

        private static void Sunburst(List<DesignDef> list)
        {
            var b = new B();
            b.R(G.RoundRect(40, 40, 920, 920, 36));
            const double cx = 500, cy = 500;
            b.R(G.Circle(cx, cy, 48));
            for (int i = 0; i < 16; i++) b.R(G.Pie(cx, cy, 120, i * 22.5 + 0.5, (i + 1) * 22.5 - 0.5));
            PetalRing(b, cx, cy, 126, 70, 17, 16, 11.25);
            RingSectors(b, cx, cy, 202, 258, 8, 4.0, 3);
            CircleRing(b, cx, cy, 292, 8, 16, 22.5);
            PetalRing(b, cx, cy, 308, 92, 30, 16);
            ScallopRing(b, cx, cy, 452, 16);
            list.Add(b.Def("sunburst", "Sunburst Mandala", CatMandalas, 24, "mandala", "sun", "rays", "energy"));
        }

        private static void CosmicMandala(List<DesignDef> list)
        {
            var b = new B();
            b.R(G.RoundRect(40, 40, 920, 920, 36));
            const double cx = 500, cy = 500;
            b.R(G.Star(cx, cy, 12, 60, 28));
            DiamondRing(b, cx, cy, 110, 28, 55, 12);
            RingSectors(b, cx, cy, 150, 200, 24, 1.5);
            PetalRing(b, cx, cy, 205, 65, 20, 12, 15);
            CircleRing(b, cx, cy, 290, 12, 16);
            DropRing(b, cx, cy, 340, 18, 50, 12, 15);
            b.R(G.Ring(cx, cy, 410, 390));
            CircleRing(b, cx, cy, 430, 24, 8);
            ScallopRing(b, cx, cy, 460, 24);
            list.Add(b.Def("cosmicmandala", "Cosmic Kaleidoscope", CatMandalas, 25, "mandala", "cosmic", "stars", "geometry"));
        }

        private static void LaceMandala(List<DesignDef> list)
        {
            var b = new B();
            b.R(G.RoundRect(40, 40, 920, 920, 36));
            const double cx = 500, cy = 500;
            b.R(G.Circle(cx, cy, 36));
            PetalRing(b, cx, cy, 38, 64, 22, 8);
            for (int i = 0; i < 8; i++)
            {
                var p = G.Pt(cx, cy, 150, i * 45);
                b.R(G.Heart(p.X, p.Y - 20, 50, 50));
            }
            RingSectors(b, cx, cy, 190, 240, 16, 2.0);
            DropRing(b, cx, cy, 270, 15, 45, 16, 11.25);
            DiamondRing(b, cx, cy, 345, 24, 48, 16);
            b.R(G.Ring(cx, cy, 395, 375));
            PetalRing(b, cx, cy, 400, 55, 18, 24);
            ScallopRing(b, cx, cy, 462, 24);
            list.Add(b.Def("lacemandala", "Lace Dream Mandala", CatMandalas, 26, "mandala", "lace", "filigree", "vintage"));
        }

        private static void CelticMandala(List<DesignDef> list)
        {
            var b = new B();
            b.R(G.RoundRect(40, 40, 920, 920, 36));
            const double cx = 500, cy = 500;
            b.R(G.RegularPolygon(cx, cy, 4, 60, 45));
            for (int i = 0; i < 4; i++)
            {
                var p = G.Pt(cx, cy, 130, i * 90);
                b.R(G.Ring(p.X, p.Y, 55, 35));
            }
            PetalRing(b, cx, cy, 120, 90, 32, 4, 45);
            RingSectors(b, cx, cy, 210, 270, 8, 3.0);
            DiamondRing(b, cx, cy, 310, 44, 70, 8, 22.5);
            RingSectors(b, cx, cy, 355, 410, 16, 2.0);
            CircleRing(b, cx, cy, 435, 16, 10);
            ScallopRing(b, cx, cy, 460, 16);
            list.Add(b.Def("celticmandala", "Celtic Knot Mandala", CatMandalas, 27, "mandala", "celtic", "knot", "irish"));
        }

        private static void ChakraMandala(List<DesignDef> list)
        {
            var b = new B();
            b.R(G.RoundRect(40, 40, 920, 920, 36));
            const double cx = 500, cy = 500;
            b.R(G.Star(cx, cy, 6, 50, 25));
            b.R(G.Ring(cx, cy, 80, 55));
            PetalRing(b, cx, cy, 85, 80, 28, 8);
            DropRing(b, cx, cy, 195, 16, 45, 16, 11.25);
            RingSectors(b, cx, cy, 235, 290, 16, 2.0);
            PetalRing(b, cx, cy, 295, 85, 30, 16);
            CircleRing(b, cx, cy, 395, 16, 12, 11.25);
            DiamondRing(b, cx, cy, 425, 22, 40, 16);
            ScallopRing(b, cx, cy, 460, 16);
            list.Add(b.Def("chakramandala", "Chakra Blossom", CatMandalas, 28, "mandala", "chakra", "energy", "spiritual"));
        }

        private static void ArabesqueMandala(List<DesignDef> list)
        {
            var b = new B();
            b.R(G.RoundRect(40, 40, 920, 920, 36));
            const double cx = 500, cy = 500;
            b.R(G.Star(cx, cy, 8, 65, 45));
            for (int i = 0; i < 8; i++)
            {
                var p = G.Pt(cx, cy, 105, i * 45 + 22.5);
                b.R(G.Hexagon(p.X, p.Y, 32, 15));
            }
            DiamondRing(b, cx, cy, 165, 34, 60, 8);
            RingSectors(b, cx, cy, 205, 260, 16, 2.5);
            PetalRing(b, cx, cy, 265, 75, 24, 16, 11.25);
            CircleRing(b, cx, cy, 355, 16, 12);
            RingSectors(b, cx, cy, 375, 430, 16, 2.0);
            ScallopRing(b, cx, cy, 460, 16);
            list.Add(b.Def("arabesquemandala", "Arabesque Tile", CatMandalas, 29, "mandala", "arabesque", "moroccan", "mosaic"));
        }

        private static void ZenHarmony(List<DesignDef> list)
        {
            var b = new B();
            b.R(G.RoundRect(40, 40, 920, 920, 36));
            const double cx = 500, cy = 500;
            b.R(G.Circle(cx, cy, 60));
            b.R(G.Circle(cx, cy - 30, 30));
            b.R(G.Circle(cx, cy + 30, 30));
            b.R(G.Circle(cx, cy - 30, 10));
            b.R(G.Circle(cx, cy + 30, 10));
            for (int i = 0; i < 8; i++)
                b.R(G.Petal(cx, cy, 65, 95, 26, i * 45 + i * 5));
            RingSectors(b, cx, cy, 180, 240, 16, 2.0);
            DropRing(b, cx, cy, 275, 18, 55, 8, 22.5);
            CircleRing(b, cx, cy, 345, 16, 13);
            PetalRing(b, cx, cy, 365, 75, 28, 16);
            ScallopRing(b, cx, cy, 455, 16);
            list.Add(b.Def("zenharmony", "Zen Harmony", CatMandalas, 30, "mandala", "yinyang", "harmony", "balance", "peace"));
        }

        // =========================================================================
        // FANTASY (10)
        // =========================================================================

        private static void Castle(List<DesignDef> list)
        {
            var b = new B();
            b.R(G.RoundRect(40, 40, 920, 920, 36));
            b.R(G.Crescent(168, 192, 112, 55));
            b.R(G.Sparkle(340, 130, 20, 8));
            b.R(G.Sparkle(820, 160, 16, 7));
            b.R(G.Sparkle(880, 320, 20, 8));
            b.R(G.Circle(480, 110, 8));
            b.R(G.Circle(640, 190, 6));
            b.R(G.Circle(250, 260, 7));
            b.R(G.Circle(908, 240, 6));
            b.R(G.Sparkle(120, 420, 15, 6));
            b.R(G.Cloud(222, 762, 286, 92));
            b.R(G.Cloud(782, 762, 286, 92));
            b.R(G.Hill(148, 852, 660, 512));
            b.R(G.RoundRect(420, 380, 160, 262, 8));
            for (int i = 0; i < 3; i++) b.R(G.RoundRect(421 + i * 56, 350, 40, 34, 6));
            b.R(G.RoundRect(405, 366, 190, 18, 4));
            b.R(G.RoundRect(468, 556, 64, 96, 28));
            b.R(G.RoundRect(442, 416, 26, 40, 12));
            b.R(G.RoundRect(532, 416, 26, 40, 12));
            b.R(G.RoundRect(295, 432, 70, 210, 6));
            b.R(G.PolyD(282, 434, 382, 434, 332, 322));
            b.R(G.RoundRect(328, 268, 8, 56, 4));
            b.R(G.PolyD(336, 270, 392, 284, 336, 298));
            b.R(G.RoundRect(318, 472, 30, 42, 14));
            b.R(G.RoundRect(635, 432, 70, 210, 6));
            b.R(G.PolyD(622, 434, 722, 434, 672, 322));
            b.R(G.RoundRect(668, 268, 8, 56, 4));
            b.R(G.PolyD(676, 270, 732, 284, 676, 298));
            b.R(G.RoundRect(658, 472, 30, 42, 14));
            b.R(G.RoundRect(455, 652, 90, 18, 4));
            b.R(G.RoundRect(443, 670, 114, 18, 4));
            b.R(G.RoundRect(431, 688, 138, 18, 4));
            Pine(b, 200, 662, 92, 122);
            Pine(b, 812, 662, 92, 122);
            b.R(G.PolyD(560, 380, 590, 362, 604, 380));
            b.R(G.PolyD(604, 380, 618, 362, 648, 380));
            list.Add(b.Def("castle", "Castle in the Clouds", CatFantasy, 31, "castle", "fairy", "towers", "night", "clouds"));
        }

        private static void Gem(List<DesignDef> list)
        {
            var b = new B();
            b.R(G.RoundRect(40, 40, 920, 920, 36));
            b.R(G.Ellipse(500, 432, 328, 108));
            double[] sd = { 150, 190, 830, 210, 130, 560, 880, 570, 240, 730 };
            for (int i = 0; i < 5; i++) b.R(G.Sparkle(sd[i * 2], sd[i * 2 + 1], 22, 8));
            b.R(G.Sparkle(420, 130, 16, 6));
            b.R(G.Sparkle(600, 730, 18, 7));
            b.R(G.Sparkle(200, 330, 14, 6));
            b.R(G.Sparkle(810, 330, 14, 6));
            for (int i = 0; i < 6; i++) { var p = OrbitPt(500, 432, 328, 108, i * 60); b.R(G.Circle(p.X, p.Y, 9)); }
            b.R(G.PolyD(420, 300, 580, 300, 620, 380, 380, 380));
            b.R(G.Tri((330, 380), (380, 380), (420, 300)));
            b.R(G.Tri((620, 380), (670, 380), (580, 300)));
            b.R(G.Tri((330, 380), (425, 380), (500, 700)));
            b.R(G.Tri((425, 380), (500, 380), (500, 700)));
            b.R(G.Tri((500, 380), (575, 380), (500, 700)));
            b.R(G.Tri((575, 380), (670, 380), (500, 700)));
            b.R(G.PolyD(362, 716, 638, 716, 598, 782, 402, 782));
            b.R(G.PolyD(322, 786, 678, 786, 638, 834, 362, 834));
            b.R(G.PolyD(220, 690, 280, 690, 268, 640, 248, 600, 232, 640));
            b.R(G.PolyD(720, 690, 780, 690, 768, 640, 748, 600, 732, 640));
            for (int i = 0; i < 8; i++) { var p = G.Pt(500, 470, 402, i * 45 + 22.5); b.R(G.Circle(p.X, p.Y, 6)); }
            b.R(G.Star(500, 340, 6, 52, 20));
            b.R(G.Circle(170, 800, 12));
            b.R(G.Circle(830, 800, 12));
            b.R(G.Circle(500, 880, 14));
            b.R(G.RoundRect(402, 862, 196, 46, 10));
            b.R(G.PolyD(262, 866, 402, 866, 402, 904, 262, 904, 296, 885));
            b.R(G.PolyD(598, 866, 738, 866, 704, 885, 738, 904, 598, 904));
            list.Add(b.Def("gem", "Crystal Heart", CatFantasy, 32, "crystal", "gem", "magic", "jewel", "sparkle"));
        }

        private static void MoonStars(List<DesignDef> list)
        {
            var b = new B();
            b.R(G.RoundRect(40, 40, 920, 920, 36));
            b.R(G.Crescent(566, 400, 232, 52));
            b.R(G.Circle(468, 330, 28));
            b.R(G.Circle(430, 436, 20));
            b.R(G.Circle(502, 506, 14));
            b.R(G.Star(198, 196, 5, 62, 26));
            b.R(G.Star(832, 168, 5, 48, 20));
            b.R(G.Star(152, 560, 5, 42, 18));
            b.R(G.Star(872, 524, 5, 38, 16));
            b.R(G.Sparkle(330, 190, 22, 8));
            b.R(G.Sparkle(720, 300, 18, 7));
            b.R(G.Sparkle(140, 350, 16, 6));
            b.R(G.Sparkle(880, 660, 16, 6));
            b.R(G.Sparkle(250, 660, 14, 6));
            b.R(G.Circle(390, 260, 8));
            b.R(G.Circle(660, 220, 7));
            b.R(G.Circle(560, 140, 6));
            b.R(G.Circle(110, 180, 6));
            b.R(G.Circle(900, 420, 7));
            b.R(G.Circle(210, 460, 6));
            b.R(G.Cloud(302, 722, 262, 82));
            b.R(G.Cloud(722, 682, 232, 70));
            b.R(G.Hill(120, 880, 862, 766));
            double[] hx = { 302, 522, 726 };
            double[] hy = { 834, 814, 838 };
            for (int k = 0; k < 3; k++)
            {
                double x = hx[k], y = hy[k];
                b.R(G.RoundRect(x - 55, y - 70, 110, 78, 6));
                b.R(G.PolyD(x - 66, y - 68, x + 66, y - 68, x, y - 128));
                b.R(G.RoundRect(x - 16, y - 38, 32, 46, 14));
                b.R(G.RoundRect(x + 16, y - 58, 24, 24, 6));
            }
            list.Add(b.Def("moonstars", "Moon & Stars", CatFantasy, 33, "moon", "stars", "night", "sky", "village"));
        }

        private static void Mushrooms(List<DesignDef> list)
        {
            var b = new B();
            b.R(G.RoundRect(40, 40, 920, 920, 36));
            b.R(G.Sparkle(190, 210, 24, 9));
            b.R(G.Sparkle(810, 190, 20, 8));
            b.R(G.Sparkle(120, 420, 16, 6));
            b.R(G.Sparkle(890, 430, 18, 7));
            b.R(G.Hill(40, 960, 962, 848));
            b.R(G.Hill(300, 700, 962, 882));
            b.R(Dome(320, 680, 522, 152));
            b.R(G.Circle(430, 442, 24));
            b.R(G.Circle(546, 416, 30));
            b.R(G.Circle(610, 474, 16));
            b.R(G.Circle(386, 486, 15));
            b.R(G.Circle(496, 480, 13));
            b.R(G.PolyD(455, 522, 545, 522, 562, 700, 540, 852, 460, 852, 438, 700));
            b.R(G.Ellipse(500, 562, 72, 18));
            b.R(Dome(250, 362, 642, 50));
            b.R(G.Circle(288, 618, 10));
            b.R(G.Circle(322, 624, 8));
            b.R(G.PolyD(286, 642, 326, 642, 332, 762, 280, 762));
            b.R(Dome(648, 762, 622, 46));
            b.R(G.Circle(686, 600, 9));
            b.R(G.Circle(718, 606, 7));
            b.R(G.PolyD(684, 622, 724, 622, 730, 742, 678, 742));
            b.R(Dome(160, 210, 806, 30));
            b.R(G.PolyD(173, 806, 197, 806, 200, 862, 170, 862));
            b.R(Dome(830, 886, 786, 28));
            b.R(G.PolyD(842, 786, 866, 786, 870, 840, 838, 840));
            for (int k = 0; k < 2; k++)
            {
                double bx = k == 0 ? 122 : 892;
                b.R(G.Leaf(bx, 936, 0, 246, 9, 0));
                for (int i = 1; i <= 6; i++)
                {
                    double t = i / 7.0;
                    double px = bx, py = 936 - 246 * t;
                    b.R(G.Leaf(px, py, 0, 44 - t * 14, 7, k == 0 ? 145 : 35));
                    b.R(G.Leaf(px, py, 0, 44 - t * 14, 7, k == 0 ? 215 : 325));
                }
            }
            double[] gx = { 260, 420, 580, 740, 330, 660 };
            double[] gy = { 918, 930, 926, 912, 896, 902 };
            for (int k = 0; k < 6; k++)
                b.R(G.PolyD(gx[k] - 14, gy[k], gx[k] + 14, gy[k], gx[k], gy[k] - 52));
            list.Add(b.Def("mushrooms", "Magic Mushrooms", CatFantasy, 34, "mushroom", "forest", "fairy", "botanical"));
        }

        private static void Dragon(List<DesignDef> list)
        {
            var b = new B();
            b.R(G.RoundRect(40, 40, 920, 920, 36));
            b.R(G.PolyD(120, 100, 160, 240, 200, 100));
            b.R(G.PolyD(480, 100, 510, 220, 540, 100));
            b.R(G.PolyD(800, 100, 840, 260, 880, 100));
            b.R(G.Ring(500, 550, 260, 150));
            b.R(G.Drop(260, 680, 24, 60, 225));
            b.R(G.PolyD(460, 480, 220, 240, 340, 340, 400, 440));
            b.R(G.PolyD(220, 240, 320, 280, 440, 460));
            b.R(G.PolyD(320, 280, 420, 330, 460, 470));
            b.R(G.PolyD(540, 480, 780, 240, 660, 340, 600, 440));
            b.R(G.PolyD(780, 240, 680, 280, 560, 460));
            b.R(G.PolyD(500, 440, 560, 360, 510, 330, 450, 370));
            b.R(G.PolyD(470, 360, 420, 260, 480, 330));
            b.R(G.PolyD(530, 360, 580, 260, 520, 330));
            b.R(G.Ellipse(480, 370, 12, 6));
            b.R(G.Ellipse(520, 370, 12, 6));
            b.R(G.Hill(300, 700, 920, 740));
            b.R(G.RoundRect(440, 760, 120, 80, 12));
            b.R(G.RoundRect(430, 740, 140, 30, 10));
            CircleRing(b, 500, 860, 90, 8, 12);
            list.Add(b.Def("dragon", "Dragon's Roost", CatFantasy, 35, "dragon", "fantasy", "mythology", "treasure", "magic"));
        }

        private static void Mermaid(List<DesignDef> list)
        {
            var b = new B();
            b.R(G.RoundRect(40, 40, 920, 920, 36));
            b.R(G.Circle(500, 320, 220));
            ConcentricBubble(b, 200, 220, 16);
            ConcentricBubble(b, 800, 240, 16);
            b.R(G.Hill(180, 820, 960, 740));
            b.R(G.Circle(460, 280, 32));
            b.R(G.Leaf(460, 260, 0, 140, 40, 150));
            b.R(G.Leaf(460, 260, 0, 130, 35, 175));
            b.R(G.PolyD(445, 310, 475, 310, 480, 420, 435, 420));
            b.R(G.Circle(448, 350, 12));
            b.R(G.Circle(472, 350, 12));
            b.R(G.PolyD(435, 420, 480, 420, 560, 560, 520, 720, 440, 680, 490, 540));
            for (int i = 0; i < 6; i++)
                b.R(G.EllipseRot(470 + i * 10, 460 + i * 36, 32 - i * 3, 14, 25));
            b.R(G.Leaf(520, 720, 0, 120, 40, 140));
            b.R(G.Leaf(520, 720, 0, 120, 40, 70));
            b.R(G.Star(260, 820, 5, 34, 14));
            b.R(G.RingSector(720, 820, 5, 55, -60, 60));
            b.R(G.Circle(720, 810, 12));
            list.Add(b.Def("mermaid", "Mermaid Lagoon", CatFantasy, 36, "mermaid", "fantasy", "ocean", "sea", "mythology"));
        }

        private static void Unicorn(List<DesignDef> list)
        {
            var b = new B();
            b.R(G.RoundRect(40, 40, 920, 920, 36));
            b.R(G.Circle(500, 500, 380));
            b.R(G.Sparkle(180, 220, 22, 8));
            b.R(G.Sparkle(820, 220, 22, 8));
            b.R(G.Sparkle(780, 420, 16, 6));
            for (int i = 0; i < 5; i++)
                b.R(G.PolyD(410 - i * 8, 280 - i * 36, 435 - i * 8, 275 - i * 36, 427 - i * 8, 240 - i * 36, 403 - i * 8, 245 - i * 36));
            b.R(G.PolyD(420, 290, 480, 340, 530, 450, 420, 520, 320, 460, 360, 360));
            b.R(G.RoundRect(310, 430, 40, 30, 12));
            b.R(G.Circle(410, 380, 12));
            b.R(G.Leaf(470, 310, 0, 80, 26, 35));
            for (int i = 0; i < 6; i++)
                b.R(G.Leaf(480 + i * 20, 340 + i * 40, 0, 160, 32, 120 + i * 8));
            b.R(G.PolyD(460, 490, 560, 410, 720, 780, 440, 780));
            Flower(b, 470, 430, 8, 26, 9, 6, 12);
            Flower(b, 520, 480, 8, 26, 9, 6, 12);
            Flower(b, 570, 540, 8, 26, 9, 6, 12);
            list.Add(b.Def("unicorn", "Unicorn Glade", CatFantasy, 37, "unicorn", "fantasy", "magic", "horse", "flowers"));
        }

        private static void Phoenix(List<DesignDef> list)
        {
            var b = new B();
            b.R(G.RoundRect(40, 40, 920, 920, 36));
            b.R(G.Circle(500, 450, 180));
            SunRays(b, 500, 450, 195, 50, 12);
            b.R(G.Circle(500, 340, 30));
            b.R(G.Tri((500, 330), (545, 340), (500, 355)));
            b.R(G.Circle(490, 335, 6));
            b.R(G.Drop(480, 315, 14, 55, 315));
            b.R(G.Drop(500, 305, 16, 65, 350));
            b.R(G.Drop(520, 315, 14, 55, 25));
            b.R(G.PolyD(475, 365, 525, 365, 545, 510, 455, 510));
            for (int i = 0; i < 3; i++) b.R(UScallop(465 + i * 26, 465 + (i + 1) * 26, 450));
            for (int i = 0; i < 6; i++) b.R(G.Petal(470, 430, 20 + i * 15, 220 - i * 15, 28, 210 + i * 16));
            for (int i = 0; i < 6; i++) b.R(G.Petal(530, 430, 20 + i * 15, 220 - i * 15, 28, 150 - i * 16));
            b.R(G.Leaf(470, 510, 0, 360, 32, 195));
            b.R(G.Leaf(500, 510, 0, 400, 36, 180));
            b.R(G.Leaf(530, 510, 0, 360, 32, 165));
            list.Add(b.Def("phoenix", "Phoenix Rebirth", CatFantasy, 38, "phoenix", "fire", "mythology", "bird", "rebirth"));
        }

        private static void Potion(List<DesignDef> list)
        {
            var b = new B();
            b.R(G.RoundRect(40, 40, 920, 920, 36));
            b.R(G.Ring(500, 500, 380, 340));
            DiamondRing(b, 500, 500, 360, 18, 30, 16);
            b.R(G.Sparkle(240, 240, 22, 8));
            b.R(G.Sparkle(760, 240, 22, 8));
            b.R(G.RoundRect(460, 220, 80, 40, 8));
            b.R(G.RoundRect(440, 255, 120, 24, 10));
            b.R(G.PolyD(460, 280, 540, 280, 550, 410, 450, 410));
            b.R(G.RoundRect(430, 395, 140, 24, 8));
            b.R(G.Circle(500, 580, 190));
            b.R(G.WaveBand(330, 670, 540, 180, 14, 120, 0));
            double[,] potB = { { 460, 620, 22 }, { 530, 600, 18 }, { 490, 670, 26 }, { 550, 660, 15 }, { 440, 560, 12 }, { 520, 530, 14 } };
            for (int i = 0; i < 6; i++) b.R(G.Circle(potB[i, 0], potB[i, 1], potB[i, 2]));
            b.R(G.PolyD(420, 760, 580, 760, 620, 830, 380, 830));
            b.R(G.PolyD(260, 760, 310, 660, 340, 780));
            b.R(G.PolyD(660, 780, 690, 660, 740, 760));
            list.Add(b.Def("potion", "Enchanted Potion", CatFantasy, 39, "potion", "magic", "alchemy", "witch", "wizard"));
        }

        private static void TreeOfWonders(List<DesignDef> list)
        {
            var b = new B();
            b.R(G.RoundRect(40, 40, 920, 920, 36));
            b.R(G.Crescent(200, 180, 85, 50));
            b.R(G.Sparkle(780, 160, 20, 7));
            b.R(G.Sparkle(850, 280, 16, 6));
            b.R(G.PolyD(380, 480, 620, 480, 680, 880, 320, 880));
            b.R(G.RoundRect(460, 720, 80, 130, 38));
            for (int i = 0; i < 5; i++)
                b.R(G.RoundRect(390 + i * 22, 680 - i * 42, 90, 18, 6));
            b.R(G.Circle(450, 420, 24));
            b.R(G.Circle(550, 440, 26));
            b.R(G.Circle(500, 320, 22));
            b.R(G.Cloud(280, 360, 220, 95));
            b.R(G.Cloud(720, 360, 220, 95));
            b.R(G.Cloud(500, 210, 260, 110));
            b.R(G.Cloud(360, 190, 200, 85));
            b.R(G.Cloud(640, 190, 200, 85));
            b.R(G.RoundRect(240, 440, 36, 50, 8));
            b.R(G.PolyD(257, 440, 259, 440, 259, 390, 257, 390));
            b.R(G.Hill(40, 960, 960, 840));
            b.R(Dome(200, 270, 860, 32));
            b.R(Dome(740, 810, 860, 30));
            list.Add(b.Def("treeofwonders", "Tree of Wonders", CatFantasy, 40, "treehouse", "fantasy", "fairy", "magic", "wonder"));
        }

        // =========================================================================
        // FLORAL (10)
        // =========================================================================

        private static void RoseBouquet(List<DesignDef> list)
        {
            var b = new B();
            b.R(G.RoundRect(40, 40, 920, 920, 36));
            const double cx = 500, cy = 460;
            b.R(G.Circle(cx, cy, 32));
            PetalRing(b, cx, cy, 30, 45, 26, 4, 0);
            PetalRing(b, cx, cy, 65, 55, 34, 5, 25);
            PetalRing(b, cx, cy, 110, 65, 42, 6, 15);
            PetalRing(b, cx, cy, 165, 75, 50, 7, 30);
            PetalRing(b, 280, 380, 20, 40, 24, 5);
            PetalRing(b, 280, 380, 55, 50, 32, 6, 20);
            b.R(G.Circle(280, 380, 22));
            PetalRing(b, 720, 380, 20, 40, 24, 5);
            PetalRing(b, 720, 380, 55, 50, 32, 6, 20);
            b.R(G.Circle(720, 380, 22));
            b.R(G.Leaf(340, 240, 0, 110, 36, 320));
            b.R(G.Leaf(660, 240, 0, 110, 36, 40));
            b.R(G.Leaf(180, 480, 0, 120, 38, 230));
            b.R(G.Leaf(820, 480, 0, 120, 38, 130));
            b.R(G.Leaf(360, 660, 0, 110, 35, 210));
            b.R(G.Leaf(640, 660, 0, 110, 35, 150));
            b.R(G.PolyD(470, 680, 530, 680, 550, 880, 450, 880));
            b.R(G.Circle(500, 730, 24));
            b.R(G.Leaf(500, 730, 0, 90, 36, 235));
            b.R(G.Leaf(500, 730, 0, 90, 36, 125));
            list.Add(b.Def("roses", "Rose Bouquet", CatFloral, 41, "rose", "bouquet", "flowers", "romantic", "botanical"));
        }

        private static void OrchidElegance(List<DesignDef> list)
        {
            var b = new B();
            b.R(G.RoundRect(40, 40, 920, 920, 36));
            b.R(G.PolyD(340, 740, 660, 740, 620, 880, 380, 880));
            b.R(G.RoundRect(320, 715, 360, 34, 14));
            b.R(G.RoundRect(260, 875, 480, 28, 10));
            b.R(G.RoundRect(485, 260, 16, 460, 8));
            b.R(G.WaveBand(360, 640, 320, 16, 40, 260, 0));
            double ox = 500, oy = 300;
            b.R(G.Petal(ox, oy, 0, 75, 30, 0));
            b.R(G.Petal(ox, oy, 0, 65, 28, 130));
            b.R(G.Petal(ox, oy, 0, 65, 28, 230));
            b.R(G.Petal(ox, oy, 0, 75, 44, 75));
            b.R(G.Petal(ox, oy, 0, 75, 44, 285));
            b.R(G.Heart(ox, oy + 25, 46, 46));
            ox = 360; oy = 420;
            b.R(G.Petal(ox, oy, 0, 65, 26, 330));
            b.R(G.Petal(ox, oy, 0, 65, 38, 60));
            b.R(G.Petal(ox, oy, 0, 65, 38, 260));
            b.R(G.Heart(ox, oy + 20, 40, 40));
            ox = 640; oy = 440;
            b.R(G.Petal(ox, oy, 0, 65, 26, 30));
            b.R(G.Petal(ox, oy, 0, 65, 38, 100));
            b.R(G.Petal(ox, oy, 0, 65, 38, 300));
            b.R(G.Heart(ox, oy + 20, 40, 40));
            b.R(G.Drop(380, 220, 14, 34, 300));
            b.R(G.Drop(420, 180, 12, 28, 320));
            b.R(G.Leaf(400, 720, 0, 180, 50, 240));
            b.R(G.Leaf(600, 720, 0, 180, 50, 120));
            list.Add(b.Def("orchid", "Orchid Elegance", CatFloral, 42, "orchid", "floral", "zen", "tropical", "blossom"));
        }

        private static void WildflowerMeadow(List<DesignDef> list)
        {
            var b = new B();
            b.R(G.RoundRect(40, 40, 920, 920, 36));
            b.R(G.Sparkle(180, 160, 20, 7));
            b.R(G.Sparkle(820, 160, 20, 7));
            b.R(G.Circle(500, 180, 12));
            b.R(G.Leaf(500, 180, 0, 60, 24, 310));
            b.R(G.Leaf(500, 180, 0, 60, 24, 50));
            b.R(G.Leaf(500, 180, 0, 45, 18, 230));
            b.R(G.Leaf(500, 180, 0, 45, 18, 130));
            Flower(b, 320, 380, 20, 65, 35, 4, 24, 45);
            Flower(b, 680, 420, 18, 60, 32, 4, 22, 20);
            Flower(b, 500, 560, 22, 75, 40, 4, 28, 0);
            Flower(b, 200, 600, 12, 42, 14, 12, 16);
            Flower(b, 780, 620, 12, 42, 14, 12, 16);
            Flower(b, 340, 740, 10, 36, 12, 10, 14);
            Flower(b, 660, 740, 10, 36, 12, 10, 14);
            for (int i = 0; i < 7; i++)
            {
                b.R(G.RoundRect(180 + i * 90, 620, 10, 320, 5));
                b.R(G.Leaf(180 + i * 90, 780, 0, 80, 18, i % 2 == 0 ? 40 : 320));
            }
            list.Add(b.Def("wildflowers", "Wildflower Meadow", CatFloral, 43, "wildflowers", "poppy", "daisy", "meadow", "summer"));
        }

        private static void CherryBlossom(List<DesignDef> list)
        {
            var b = new B();
            b.R(G.RoundRect(40, 40, 920, 920, 36));
            b.R(G.Circle(500, 480, 340));
            b.R(G.PolyD(160, 320, 420, 390, 640, 340, 840, 460, 820, 510, 620, 400, 400, 450, 160, 380));
            b.R(G.PolyD(410, 420, 460, 540, 430, 550, 390, 430));
            double[] bx = { 260, 380, 520, 660, 780, 450, 600 };
            double[] by = { 320, 360, 330, 360, 440, 560, 430 };
            for (int i = 0; i < 7; i++) Flower(b, bx[i], by[i], 10, 38, 18, 5, 12, i * 25);
            b.R(G.Drop(310, 300, 10, 24, 45));
            b.R(G.Drop(720, 340, 10, 24, 315));
            b.R(G.Petal(300, 640, 0, 36, 16, 60));
            b.R(G.Petal(620, 680, 0, 34, 15, 120));
            b.R(G.Petal(460, 760, 0, 38, 16, 30));
            b.R(G.RoundRect(710, 520, 60, 90, 16));
            b.R(G.PolyD(700, 520, 780, 520, 750, 490, 730, 490));
            b.R(G.PolyD(739, 490, 741, 490, 741, 410, 739, 410));
            list.Add(b.Def("sakura", "Japanese Cherry Blossom", CatFloral, 44, "sakura", "cherryblossom", "japan", "spring", "zen"));
        }

        private static void HydrangeaGarden(List<DesignDef> list)
        {
            var b = new B();
            b.R(G.RoundRect(40, 40, 920, 920, 36));
            b.R(G.RoundRect(485, 540, 30, 400, 15));
            b.R(G.Leaf(485, 680, 0, 260, 85, 235));
            b.R(G.Leaf(515, 740, 0, 260, 85, 125));
            b.R(G.Leaf(485, 820, 0, 220, 75, 245));
            b.R(G.Leaf(515, 850, 0, 220, 75, 115));
            const double cx = 500, cy = 380;
            b.R(G.Circle(cx, cy, 220));
            for (int i = 0; i < 10; i++)
            {
                var p = G.Pt(cx, cy, 155, i * 36);
                Flower(b, p.X, p.Y, 5, 26, 14, 4, 7, i * 20);
            }
            for (int i = 0; i < 6; i++)
            {
                var p = G.Pt(cx, cy, 85, i * 60 + 15);
                Flower(b, p.X, p.Y, 6, 28, 15, 4, 8, i * 30);
            }
            Flower(b, cx, cy, 7, 30, 16, 4, 9, 45);
            list.Add(b.Def("hydrangea", "Hydrangea Garden", CatFloral, 45, "hydrangea", "floral", "garden", "summer", "petals"));
        }

        private static void PeonyDream(List<DesignDef> list)
        {
            var b = new B();
            b.R(G.RoundRect(40, 40, 920, 920, 36));
            b.R(G.Leaf(280, 260, 0, 150, 50, 315));
            b.R(G.Leaf(720, 260, 0, 150, 50, 45));
            b.R(G.Leaf(180, 520, 0, 160, 55, 230));
            b.R(G.Leaf(820, 520, 0, 160, 55, 130));
            b.R(G.RoundRect(485, 620, 30, 320, 15));
            const double cx = 500, cy = 460;
            b.R(G.Circle(cx, cy, 38));
            CircleRing(b, cx, cy, 26, 8, 5);
            PetalRing(b, cx, cy, 35, 55, 34, 6, 10);
            PetalRing(b, cx, cy, 75, 65, 42, 8, 25);
            PetalRing(b, cx, cy, 130, 75, 52, 10, 12);
            PetalRing(b, cx, cy, 195, 90, 64, 10, 30);
            Flower(b, 260, 440, 8, 30, 14, 5, 10);
            Flower(b, 740, 440, 8, 30, 14, 5, 10);
            list.Add(b.Def("peony", "Peony Dream", CatFloral, 46, "peony", "floral", "blossom", "victorian", "spring"));
        }

        private static void LavenderHerbs(List<DesignDef> list)
        {
            var b = new B();
            b.R(G.RoundRect(40, 40, 920, 920, 36));
            b.R(G.RoundRect(360, 560, 280, 360, 36));
            b.R(G.RoundRect(390, 520, 220, 46, 14));
            b.R(G.RoundRect(375, 538, 250, 16, 6));
            double[] lx = { 420, 460, 500, 540, 580 };
            double[] lh = { 380, 440, 460, 420, 360 };
            for (int k = 0; k < 5; k++)
            {
                double x = lx[k], topY = 520 - lh[k];
                b.R(G.RoundRect(x - 4, topY, 8, lh[k], 4));
                for (int i = 0; i < 8; i++)
                {
                    double fy = topY + i * 22;
                    b.R(G.Circle(x - 12, fy, 8));
                    b.R(G.Circle(x + 12, fy, 8));
                    b.R(G.Circle(x, fy - 4, 7));
                }
            }
            b.R(G.Leaf(340, 580, 0, 160, 36, 230));
            b.R(G.Leaf(660, 580, 0, 160, 36, 130));
            b.R(G.Circle(500, 546, 14));
            b.R(G.Leaf(500, 546, 0, 75, 26, 240));
            b.R(G.Leaf(500, 546, 0, 75, 26, 120));
            list.Add(b.Def("lavender", "Lavender & Herbs", CatFloral, 47, "lavender", "herbs", "rustic", "jar", "provence"));
        }

        private static void TropicalHibiscus(List<DesignDef> list)
        {
            var b = new B();
            b.R(G.RoundRect(40, 40, 920, 920, 36));
            b.R(G.Leaf(240, 240, 0, 320, 90, 315));
            b.R(G.Leaf(760, 240, 0, 320, 90, 45));
            b.R(G.Leaf(160, 680, 0, 280, 80, 220));
            b.R(G.Leaf(840, 680, 0, 280, 80, 140));
            Flower(b, 220, 420, 10, 42, 22, 5, 14, 15);
            Flower(b, 780, 580, 10, 42, 22, 5, 14, 45);
            const double cx = 500, cy = 480;
            for (int i = 0; i < 5; i++)
                b.R(G.Petal(cx, cy, 30, 180, 85, i * 72));
            b.R(G.Circle(cx, cy, 38));
            b.R(G.RoundRect(cx - 8, cy - 140, 16, 140, 8));
            CircleRing(b, cx, cy - 135, 26, 5, 7);
            list.Add(b.Def("hibiscus", "Tropical Hibiscus", CatFloral, 48, "hibiscus", "tropical", "hawaii", "plumeria", "exotic"));
        }

        private static void WaterLilySanctuary(List<DesignDef> list)
        {
            var b = new B();
            b.R(G.RoundRect(40, 40, 920, 920, 36));
            b.R(G.WaveBand(40, 960, 220, 60, 16, 260, 0));
            b.R(G.WaveBand(40, 960, 780, 80, 20, 300, 1.5));
            b.R(G.RingSector(260, 380, 4, 130, -40, 310));
            b.R(G.RingSector(740, 400, 4, 140, 30, 380));
            b.R(G.RingSector(480, 760, 4, 160, 120, 470));
            b.R(G.RoundRect(750, 200, 12, 80, 6));
            b.R(G.Circle(756, 195, 9));
            b.R(G.Leaf(756, 220, 0, 90, 20, 290));
            b.R(G.Leaf(756, 220, 0, 90, 20, 70));
            b.R(G.Leaf(756, 240, 0, 75, 18, 280));
            b.R(G.Leaf(756, 240, 0, 75, 18, 80));
            const double cx = 500, cy = 520;
            PetalRing(b, cx, cy, 40, 130, 34, 12, 0);
            PetalRing(b, cx, cy, 30, 90, 28, 10, 18);
            b.R(G.Circle(cx, cy, 32));
            CircleRing(b, cx, cy, 20, 8, 4);
            list.Add(b.Def("waterlily", "Water Lily Sanctuary", CatFloral, 49, "waterlily", "lotus", "pond", "zen", "dragonfly"));
        }

        private static void FloralHeartWreath(List<DesignDef> list)
        {
            var b = new B();
            b.R(G.RoundRect(40, 40, 920, 920, 36));
            b.R(G.Sparkle(160, 160, 22, 8));
            b.R(G.Sparkle(840, 160, 22, 8));
            b.R(G.Heart(500, 180, 580, 580));
            b.R(G.Heart(500, 230, 440, 440));
            double[] rx = { 320, 680, 240, 760, 300, 700, 420, 580 };
            double[] ry = { 260, 260, 420, 420, 580, 580, 680, 680 };
            for (int i = 0; i < 8; i++) Flower(b, rx[i], ry[i], 6, 26, 12, 5, 10, i * 30);
            for (int i = 0; i < 8; i++)
            {
                b.R(G.Leaf(rx[i], ry[i], 18, 45, 16, i * 45 + 20));
                CircleRing(b, rx[i], ry[i] + 30, 18, 3, 5);
            }
            b.R(G.Circle(500, 230, 20));
            b.R(G.Leaf(500, 230, 0, 85, 34, 240));
            b.R(G.Leaf(500, 230, 0, 85, 34, 120));
            b.R(G.Leaf(500, 230, 0, 110, 28, 200));
            b.R(G.Leaf(500, 230, 0, 110, 28, 160));
            list.Add(b.Def("heartwreath", "Floral Heart Wreath", CatFloral, 50, "heart", "wreath", "roses", "love", "floral"));
        }

        // =========================================================================
        // GEOMETRIC (10)
        // =========================================================================

        private static void SacredGeometry(List<DesignDef> list)
        {
            var b = new B();
            b.R(G.RoundRect(40, 40, 920, 920, 36));
            const double cx = 500, cy = 500;
            const double r = 90;
            b.R(G.Circle(cx, cy, r));
            for (int i = 0; i < 6; i++)
            {
                var p = G.Pt(cx, cy, r, i * 60);
                b.R(G.Circle(p.X, p.Y, r));
            }
            for (int i = 0; i < 12; i++)
            {
                var p = G.Pt(cx, cy, r * 1.732, i * 30);
                b.R(G.Circle(p.X, p.Y, r));
            }
            b.R(G.Hexagon(cx, cy, 280, 0));
            b.R(G.Hexagon(cx, cy, 320, 30));
            b.R(G.Star(cx, cy, 12, 380, 320));
            b.R(G.Ring(cx, cy, 440, 400));
            DiamondRing(b, cx, cy, 420, 24, 36, 24);
            list.Add(b.Def("sacredgeo", "Sacred Geometry", CatGeometric, 51, "sacredgeometry", "floweroflife", "mandala", "math", "symmetry"));
        }

        private static void StainedGlass(List<DesignDef> list)
        {
            var b = new B();
            b.R(G.RoundRect(40, 40, 920, 920, 36));
            const double cx = 500, cy = 500;
            b.R(G.Ring(cx, cy, 440, 410));
            b.R(G.Ring(cx, cy, 380, 350));
            b.R(G.Circle(cx, cy, 45));
            PetalRing(b, cx, cy, 45, 65, 24, 8);
            RingSectors(b, cx, cy, 125, 230, 12, 3.0);
            for (int i = 0; i < 12; i++)
            {
                var p = G.Pt(cx, cy, 285, i * 30 + 15);
                b.R(G.Heart(p.X, p.Y - 15, 45, 45));
            }
            RingSectors(b, cx, cy, 310, 350, 24, 2.0);
            CircleRing(b, cx, cy, 395, 24, 10);
            DiamondRing(b, cx, cy, 425, 18, 30, 24);
            list.Add(b.Def("stainedglass", "Stained Glass Window", CatGeometric, 52, "stainedglass", "cathedral", "gothic", "architecture", "window"));
        }

        private static void HexagonalHive(List<DesignDef> list)
        {
            var b = new B();
            b.R(G.RoundRect(40, 40, 920, 920, 36));
            const double cx = 500, cy = 500;
            const double hexR = 64;
            b.R(G.Hexagon(cx, cy, hexR, 30));
            for (int i = 0; i < 6; i++)
            {
                var p = G.Pt(cx, cy, hexR * 1.732, i * 60 + 30);
                b.R(G.Hexagon(p.X, p.Y, hexR, 30));
            }
            for (int i = 0; i < 6; i++)
            {
                var p1 = G.Pt(cx, cy, hexR * 3.464, i * 60 + 30);
                b.R(G.Hexagon(p1.X, p1.Y, hexR, 30));
                var p2 = G.Pt(cx, cy, hexR * 3.0, i * 60);
                b.R(G.Hexagon(p2.X, p2.Y, hexR, 30));
            }
            DropRing(b, cx, cy, 110, 16, 45, 6, 30);
            CircleRing(b, cx, cy, 220, 12, 14);
            b.R(G.Ring(cx, cy, 440, 410));
            DiamondRing(b, cx, cy, 425, 22, 36, 18);
            list.Add(b.Def("hexhive", "Hexagonal Hive", CatGeometric, 53, "honeycomb", "hexagon", "hive", "bee", "geometry"));
        }

        private static void OpticalIllusion(List<DesignDef> list)
        {
            var b = new B();
            b.R(G.RoundRect(40, 40, 920, 920, 36));
            for (int row = 0; row < 5; row++)
            {
                double cy = 220 + row * 140;
                for (int col = 0; col < 5; col++)
                {
                    double cx = 220 + col * 140 + (row % 2 == 1 ? 70 : 0);
                    if (cx > 820) continue;
                    b.R(G.Diamond(cx, cy - 25, 60, 36));
                    b.R(G.PolyD(cx - 30, cy - 7, cx, cy + 11, cx, cy + 47, cx - 30, cy + 29));
                    b.R(G.PolyD(cx + 30, cy - 7, cx, cy + 11, cx, cy + 47, cx + 30, cy + 29));
                }
            }
            list.Add(b.Def("opticalillusion", "Optical Illusion", CatGeometric, 54, "opticalillusion", "3d", "cubes", "opart", "geometry"));
        }

        private static void ChevronWaves(List<DesignDef> list)
        {
            var b = new B();
            b.R(G.RoundRect(40, 40, 920, 920, 36));
            for (int k = 0; k < 7; k++)
            {
                double y = 140 + k * 110;
                var pts = new (double, double)[9];
                for (int i = 0; i < 9; i++)
                {
                    double x = 80 + i * 105;
                    double py = y + (i % 2 == 1 ? -45 : 45);
                    pts[i] = (x, py);
                }
                b.R(G.Poly(pts));
                for (int i = 1; i < 8; i += 2)
                    b.R(G.Diamond(80 + i * 105, y - 45, 34, 50));
            }
            list.Add(b.Def("chevronwaves", "Chevron Waves", CatGeometric, 55, "chevron", "zigzag", "stripes", "geometric", "pattern"));
        }

        private static void DiamondLattice(List<DesignDef> list)
        {
            var b = new B();
            b.R(G.RoundRect(40, 40, 920, 920, 36));
            for (int r = 0; r < 4; r++)
            {
                double cy = 200 + r * 200;
                for (int c = 0; c < 4; c++)
                {
                    double cx = 200 + c * 200;
                    b.R(G.Diamond(cx, cy, 140, 140));
                    b.R(G.Circle(cx, cy - 45, 26));
                    b.R(G.Circle(cx + 45, cy, 26));
                    b.R(G.Circle(cx, cy + 45, 26));
                    b.R(G.Circle(cx - 45, cy, 26));
                    b.R(G.Circle(cx, cy, 18));
                }
            }
            list.Add(b.Def("diamondlattice", "Diamond Lattice", CatGeometric, 56, "trellis", "lattice", "moroccan", "diamonds", "pattern"));
        }

        private static void ZenTessellation(List<DesignDef> list)
        {
            var b = new B();
            b.R(G.RoundRect(40, 40, 920, 920, 36));
            for (int r = 0; r < 4; r++)
            {
                double cy = 200 + r * 200;
                for (int c = 0; c < 4; c++)
                {
                    double cx = 200 + c * 200;
                    b.R(G.RegularPolygon(cx, cy, 8, 85, 22.5));
                    b.R(G.Star(cx, cy, 8, 70, 35, 22.5));
                    b.R(G.Square(cx, cy, 40));
                }
            }
            list.Add(b.Def("zentessellation", "Zen Tessellation", CatGeometric, 57, "tessellation", "escher", "interlocking", "tiles", "math"));
        }

        private static void OpArtSpheres(List<DesignDef> list)
        {
            var b = new B();
            b.R(G.RoundRect(40, 40, 920, 920, 36));
            double[] sx = { 320, 680, 500 };
            double[] sy = { 360, 360, 680 };
            for (int k = 0; k < 3; k++)
            {
                double cx = sx[k], cy = sy[k];
                b.R(G.Circle(cx, cy, 150));
                for (int i = 1; i <= 4; i++)
                {
                    b.R(G.Ellipse(cx, cy, 150, i * 32));
                    b.R(G.Ellipse(cx, cy, i * 32, 150));
                }
            }
            list.Add(b.Def("opspheres", "Op Art Spheres", CatGeometric, 58, "opart", "spheres", "optical", "3d", "illusions"));
        }

        private static void OrigamiMosaic(List<DesignDef> list)
        {
            var b = new B();
            b.R(G.RoundRect(40, 40, 920, 920, 36));
            b.R(G.PolyD(350, 450, 180, 260, 280, 420));
            b.R(G.PolyD(350, 450, 280, 420, 380, 340));
            b.R(G.PolyD(350, 450, 380, 340, 440, 420));
            b.R(G.PolyD(350, 450, 440, 420, 480, 320));
            b.R(G.PolyD(350, 450, 240, 520, 300, 480));
            b.R(G.PolyD(350, 450, 460, 480, 420, 520));
            b.R(G.PolyD(650, 650, 480, 460, 580, 620));
            b.R(G.PolyD(650, 650, 580, 620, 680, 540));
            b.R(G.PolyD(650, 650, 680, 540, 740, 620));
            b.R(G.PolyD(650, 650, 740, 620, 780, 520));
            b.R(G.PolyD(650, 650, 540, 720, 600, 680));
            b.R(G.PolyD(650, 650, 760, 680, 720, 720));
            DiamondRing(b, 500, 500, 360, 40, 70, 12);
            CircleRing(b, 500, 500, 420, 12, 14);
            list.Add(b.Def("origami", "Origami Mosaic", CatGeometric, 59, "origami", "crane", "geometric", "paper", "facets"));
        }

        private static void IslamicTileArt(List<DesignDef> list)
        {
            var b = new B();
            b.R(G.RoundRect(40, 40, 920, 920, 36));
            const double cx = 500, cy = 500;
            b.R(G.Star(cx, cy, 8, 80, 55));
            for (int i = 0; i < 8; i++)
            {
                var p = G.Pt(cx, cy, 140, i * 45 + 22.5);
                b.R(G.Hexagon(p.X, p.Y, 45, 15));
            }
            DiamondRing(b, cx, cy, 210, 46, 75, 8);
            RingSectors(b, cx, cy, 260, 330, 16, 3.0);
            for (int i = 0; i < 8; i++)
            {
                var p = G.Pt(cx, cy, 370, i * 45 + 22.5);
                b.R(G.Star(p.X, p.Y, 8, 44, 28));
            }
            b.R(G.Ring(cx, cy, 440, 415));
            DiamondRing(b, cx, cy, 428, 20, 34, 24);
            list.Add(b.Def("islamictile", "Islamic Tile Art", CatGeometric, 60, "islamictile", "girih", "arabesque", "stars", "geometry"));
        }

        // =========================================================================
        // FREE DRAW TEMPLATES (6)
        // =========================================================================

        private static void FreeDrawBlank(List<DesignDef> list)
        {
            var b = new B();
            b.R(G.RoundRect(40, 40, 920, 920, 36));
            list.Add(b.Def(FreeDrawId, "Blank Canvas", CatFreeDraw, 100, "freedraw", "custom", "blank", "canvas", "sketch"));
        }

        private static void FreeDrawMandala(List<DesignDef> list)
        {
            var b = new B();
            b.R(G.RoundRect(40, 40, 920, 920, 36));
            b.R(G.Circle(500, 500, 80));
            b.R(G.Ring(500, 500, 180, 178));
            b.R(G.Ring(500, 500, 280, 278));
            b.R(G.Ring(500, 500, 380, 378));
            b.R(G.Circle(500, 500, 450));
            list.Add(b.Def("free_draw_mandala", "Mandala Guide Base", CatFreeDraw, 99, "freedraw", "mandala", "circle", "guide", "radial"));
        }

        private static void FreeDrawComic(List<DesignDef> list)
        {
            var b = new B();
            b.R(G.RoundRect(40, 40, 920, 920, 36));
            b.R(G.RoundRect(70, 70, 410, 410, 12));
            b.R(G.RoundRect(520, 70, 410, 410, 12));
            b.R(G.RoundRect(70, 520, 410, 410, 12));
            b.R(G.RoundRect(520, 520, 410, 410, 12));
            list.Add(b.Def("free_draw_comic", "Comic 4-Panel Grid", CatFreeDraw, 98, "freedraw", "comic", "panel", "manga", "storyboard"));
        }

        private static void FreeDrawLandscape(List<DesignDef> list)
        {
            var b = new B();
            b.R(G.RoundRect(40, 40, 920, 920, 36));
            b.R(G.WaveBandTop(40, 960, 40, 480, 20, 320, 0));
            b.R(G.WaveBandBottom(40, 960, 480, 30, 20, 320, 0, 960));
            b.R(G.Circle(500, 300, 90));
            list.Add(b.Def("free_draw_landscape", "Landscape Horizon", CatFreeDraw, 97, "freedraw", "landscape", "horizon", "sky", "sun"));
        }

        private static void FreeDrawHex(List<DesignDef> list)
        {
            var b = new B();
            b.R(G.RoundRect(40, 40, 920, 920, 36));
            const double cx = 500, cy = 500;
            b.R(G.Hexagon(cx, cy, 140));
            for (int i = 0; i < 6; i++)
            {
                var p = G.Pt(cx, cy, 260, i * 60);
                b.R(G.Hexagon(p.X, p.Y, 130));
            }
            list.Add(b.Def("free_draw_hex", "Hexagon Honeycomb Base", CatFreeDraw, 96, "freedraw", "hex", "honeycomb", "geometric", "pattern"));
        }

        private static void FreeDrawCard(List<DesignDef> list)
        {
            var b = new B();
            b.R(G.RoundRect(40, 40, 920, 920, 36));
            b.R(G.RoundRect(160, 80, 680, 840, 28));
            b.R(G.RoundRect(200, 120, 600, 760, 20));
            list.Add(b.Def("free_draw_card", "Framed Card and Poster", CatFreeDraw, 95, "freedraw", "card", "frame", "poster", "border"));
        }
    }
}

