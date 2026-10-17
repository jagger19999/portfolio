using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;

internal static class DemoOverlayExporter
{
    private sealed class Fix
    {
        public float X;
        public float Y;
        public int DurationMs;
        public Fix(float x, float y, int durationMs) { X = x; Y = y; DurationMs = durationMs; }
    }

    private static readonly string Root = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ".."));
    private static readonly string Assets = Path.Combine(Root, "assets");

    [STAThread]
    private static void Main()
    {
        var taobao = new List<Fix>
        {
            // 先搜索栏，再纸巾商品，再电脑广告，最后回到搜索栏并点击搜索。
            new Fix(875, 270, 245),
            new Fix(1080, 1080, 335),
            new Fix(865, 535, 285),
            new Fix(1410, 270, 220),
            new Fix(1765, 270, 185),
            // 同一 AOI 内的小范围复看，以及页面高显著区域的短暂偏离。
            new Fix(1090, 1005, 260),
            new Fix(1125, 1245, 310),
            new Fix(810, 500, 205),
            new Fix(655, 470, 190),
            new Fix(940, 575, 230),
            new Fix(1290, 270, 175),
            new Fix(1685, 270, 165),
            new Fix(1695, 295, 290),
            new Fix(1160, 1125, 240)
        };
        var xiaomi = new List<Fix>
        {
            // 用户指定的前三次注视：蓝色车体 → 人脸 → SU7 标题。
            new Fix(1080, 1010, 285),
            new Fix(1485, 555, 315),
            new Fix(1040, 470, 250),
            // 自然观看中的局部探索与复看。
            new Fix(760, 990, 225),
            new Fix(470, 1080, 195),
            new Fix(1320, 900, 240),
            new Fix(1515, 505, 270),
            new Fix(1920, 700, 165),
            new Fix(820, 470, 230),
            new Fix(1120, 500, 285),
            new Fix(1240, 1115, 210),
            new Fix(1020, 1195, 190),
            new Fix(1190, 970, 305),
            new Fix(1550, 615, 245)
        };
        var game = new List<Fix>
        {
            // 用户指定的前三次注视：右侧提示 → 技能栏 → 前方通路。
            new Fix(2190, 625, 265),
            new Fix(2100, 1410, 225),
            new Fix(1250, 755, 310),
            // 游戏搜索中围绕准星、通路、掩体与 HUD 的多次短注视。
            new Fix(1260, 690, 210),
            new Fix(1560, 650, 185),
            new Fix(1830, 690, 175),
            new Fix(2070, 665, 195),
            new Fix(1330, 715, 245),
            new Fix(920, 690, 180),
            new Fix(595, 650, 165),
            new Fix(1200, 805, 285),
            new Fix(1880, 1230, 190),
            new Fix(2090, 1400, 205),
            new Fix(1310, 785, 330)
        };

        ExportCase("taobao-home.png", "taobao", taobao, true, false);
        ExportCase("xiaomi-su7-home.png", "xiaomi-su7", xiaomi, true, true);
        ExportCase("game-training.png", "game", game, true, false);
    }

    private static void ExportCase(string imageName, string caseName, List<Fix> fixes, bool scanpath, bool heatmap)
    {
        string source = Path.Combine(Assets, imageName);
        using (var bitmap = new Bitmap(source))
        {
            if (scanpath)
            {
                using (var output = RenderScanpath(bitmap, fixes))
                    output.Save(Path.Combine(Root, "demo", caseName + "-scanpath-demo.png"), ImageFormat.Png);
            }
            if (heatmap)
            {
                using (var output = RenderHeatmap(bitmap, fixes))
                    output.Save(Path.Combine(Root, "demo", caseName + "-heatmap-demo.png"), ImageFormat.Png);
            }
            WriteDemoCsv(Path.Combine(Root, "demo", caseName + "-demo.csv"), bitmap.Size, fixes, caseName);
        }
    }

    private static Bitmap RenderScanpath(Bitmap source, List<Fix> fixes)
    {
        var result = new Bitmap(source);
        using (var g = Graphics.FromImage(result))
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
            using (var shadow = new Pen(Color.FromArgb(115, 0, 0, 0), 7f))
            using (var path = new Pen(Color.FromArgb(185, 238, 242, 244), 3f))
            {
                for (int i = 0; i < fixes.Count - 1; i++)
                {
                    g.DrawLine(shadow, fixes[i].X, fixes[i].Y, fixes[i + 1].X, fixes[i + 1].Y);
                    g.DrawLine(path, fixes[i].X, fixes[i].Y, fixes[i + 1].X, fixes[i + 1].Y);
                }
            }

            int minD = int.MaxValue, maxD = int.MinValue;
            foreach (var f in fixes) { minD = Math.Min(minD, f.DurationMs); maxD = Math.Max(maxD, f.DurationMs); }
            for (int i = 0; i < fixes.Count; i++)
            {
                float r = maxD > minD ? 18f + (fixes[i].DurationMs - minD) / (float)(maxD - minD) * 22f : 28f;
                var rect = new RectangleF(fixes[i].X - r, fixes[i].Y - r, r * 2f, r * 2f);
                Color color = DistinctColor(i + 1);
                using (var fill = new SolidBrush(Color.FromArgb(188, color)))
                using (var outline = new Pen(Color.White, 3f))
                using (var font = new Font("Microsoft YaHei", Math.Max(13f, r * 0.68f), FontStyle.Bold))
                using (var textOutline = new SolidBrush(Color.FromArgb(190, 0, 0, 0)))
                {
                    g.FillEllipse(fill, rect);
                    g.DrawEllipse(outline, rect);
                    var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                    string label = (i + 1).ToString(CultureInfo.InvariantCulture);
                    for (int dx = -2; dx <= 2; dx++)
                        for (int dy = -2; dy <= 2; dy++)
                            if (dx != 0 || dy != 0)
                                g.DrawString(label, font, textOutline, new RectangleF(rect.X + dx, rect.Y + dy, rect.Width, rect.Height), sf);
                    g.DrawString(label, font, Brushes.White, rect, sf);
                }
            }
            DrawDemoTag(g, "DEMO · 假设注视路径");
        }
        return result;
    }

    private static Bitmap RenderHeatmap(Bitmap source, List<Fix> fixes)
    {
        int w = source.Width, h = source.Height;
        var density = new float[w * h];
        const int radius = 155;
        var random = new Random(20261017);

        foreach (var f in fixes)
        {
            // 一个真实注视由多个带微小漂移的采样点构成；叠加后形成不规则热点边界。
            int subSamples = Math.Max(5, f.DurationMs / 35);
            float baseWeight = Math.Max(0.45f, f.DurationMs / 310f) / subSamples;
            for (int sample = 0; sample < subSamples; sample++)
            {
                double angle = random.NextDouble() * Math.PI * 2;
                double drift = Math.Sqrt(random.NextDouble()) * 24.0;
                int cx = (int)Math.Round(f.X + Math.Cos(angle) * drift);
                int cy = (int)Math.Round(f.Y + Math.Sin(angle) * drift * 0.72);
                float sigma = 48f + (float)random.NextDouble() * 18f;
                float twoSigma2 = 2f * sigma * sigma;
                for (int dy = -radius; dy <= radius; dy++)
                {
                    int y = cy + dy;
                    if (y < 0 || y >= h) continue;
                    int row = y * w;
                    for (int dx = -radius; dx <= radius; dx++)
                    {
                        int x = cx + dx;
                        if (x < 0 || x >= w) continue;
                        float d2 = dx * dx + dy * dy;
                        density[row + x] += baseWeight * (float)Math.Exp(-d2 / twoSigma2);
                    }
                }
            }
        }

        float max = 0f;
        foreach (float value in density) max = Math.Max(max, value);
        using (var overlay = new Bitmap(w, h, PixelFormat.Format32bppArgb))
        {
            var data = overlay.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
            byte[] buf = new byte[data.Stride * h];
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float t = density[y * w + x] / Math.Max(max, 0.0001f);
                    if (t < 0.03f) continue;
                    Color c = HeatColor(t);
                    int idx = y * data.Stride + x * 4;
                    buf[idx] = c.B; buf[idx + 1] = c.G; buf[idx + 2] = c.R;
                    buf[idx + 3] = (byte)(Math.Min(1f, t * 1.7f) * 215f);
                }
            }
            System.Runtime.InteropServices.Marshal.Copy(buf, 0, data.Scan0, buf.Length);
            overlay.UnlockBits(data);

            var result = new Bitmap(source);
            using (var g = Graphics.FromImage(result))
            {
                g.DrawImage(overlay, 0, 0, w, h);
                DrawDemoTag(g, "DEMO · 假设热力分布");
            }
            return result;
        }
    }

    private static void WriteDemoCsv(string path, Size size, List<Fix> fixes, string content)
    {
        using (var writer = new StreamWriter(path, false, new System.Text.UTF8Encoding(false)))
        {
            writer.WriteLine("#screen=0,0," + size.Width + "," + size.Height);
            writer.WriteLine("SystemTime,DeviceTimestamp,Content,Page,GazeX,GazeY");
            double t = 0;
            int sample = 0;
            var random = new Random(20261017 + content.Length);
            for (int fixIndex = 0; fixIndex < fixes.Count; fixIndex++)
            {
                var f = fixes[fixIndex];
                int count = Math.Max(8, (int)Math.Round(f.DurationMs * 1.25 / 16.667));
                double driftX = 0, driftY = 0;
                for (int i = 0; i < count; i++)
                {
                    driftX = driftX * 0.78 + (random.NextDouble() - 0.5) * 5.8;
                    driftY = driftY * 0.78 + (random.NextDouble() - 0.5) * 5.2;
                    float jitterX = (float)(driftX + (random.NextDouble() - 0.5) * 2.2);
                    float jitterY = (float)(driftY + (random.NextDouble() - 0.5) * 2.2);
                    writer.WriteLine("00:00:" + (t / 1000.0).ToString("00.000", CultureInfo.InvariantCulture) + "," +
                        t.ToString("0.000", CultureInfo.InvariantCulture) + "," + content + ",1," +
                        (f.X + jitterX).ToString("0.000", CultureInfo.InvariantCulture) + "," +
                        (f.Y + jitterY).ToString("0.000", CultureInfo.InvariantCulture));
                    t += 16.667;
                    sample++;
                }

                // 用 3–5 个高速过渡采样模拟一次约 50–80 ms 的扫视，而不是瞬间跳点。
                if (fixIndex + 1 < fixes.Count)
                {
                    var next = fixes[fixIndex + 1];
                    int steps = 3 + random.Next(3);
                    for (int step = 1; step <= steps; step++)
                    {
                        double p = step / (double)(steps + 1);
                        double eased = p * p * (3.0 - 2.0 * p);
                        double sx = f.X + (next.X - f.X) * eased + (random.NextDouble() - 0.5) * 4.0;
                        double sy = f.Y + (next.Y - f.Y) * eased + (random.NextDouble() - 0.5) * 4.0;
                        writer.WriteLine("00:00:" + (t / 1000.0).ToString("00.000", CultureInfo.InvariantCulture) + "," +
                            t.ToString("0.000", CultureInfo.InvariantCulture) + "," + content + ",1," +
                            sx.ToString("0.000", CultureInfo.InvariantCulture) + "," +
                            sy.ToString("0.000", CultureInfo.InvariantCulture));
                        t += 16.667;
                        sample++;
                    }
                }
            }
        }
    }

    private static void DrawDemoTag(Graphics g, string text)
    {
        using (var font = new Font("Microsoft YaHei", 22f, FontStyle.Bold))
        {
            SizeF size = g.MeasureString(text, font);
            var box = new RectangleF(28, 28, size.Width + 30, size.Height + 18);
            using (var bg = new SolidBrush(Color.FromArgb(178, 10, 14, 20))) g.FillRectangle(bg, box);
            g.DrawString(text, font, Brushes.White, box.X + 15, box.Y + 8);
        }
    }

    private static Color DistinctColor(int number)
    {
        return HsvToRgb((number * 137.508) % 360.0, 0.85, 0.95);
    }

    private static Color HsvToRgb(double h, double s, double v)
    {
        double c = v * s, hp = h / 60.0, x = c * (1 - Math.Abs(hp % 2 - 1));
        double r = 0, g = 0, b = 0;
        if (hp < 1) { r = c; g = x; } else if (hp < 2) { r = x; g = c; }
        else if (hp < 3) { g = c; b = x; } else if (hp < 4) { g = x; b = c; }
        else if (hp < 5) { r = x; b = c; } else { r = c; b = x; }
        double m = v - c;
        return Color.FromArgb((int)((r + m) * 255), (int)((g + m) * 255), (int)((b + m) * 255));
    }

    private static Color HeatColor(float t)
    {
        int r, g, b;
        if (t < 0.25f) { float s = t / 0.25f; r = 0; g = 0; b = (int)(128 + 127 * s); }
        else if (t < 0.5f) { float s = (t - 0.25f) / 0.25f; r = 0; g = (int)(255 * s); b = (int)(255 * (1 - s)); }
        else if (t < 0.75f) { float s = (t - 0.5f) / 0.25f; r = (int)(255 * s); g = 255; b = 0; }
        else { float s = (t - 0.75f) / 0.25f; r = 255; g = (int)(255 * (1 - s)); b = 0; }
        return Color.FromArgb(r, g, b);
    }
}
