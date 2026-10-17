namespace RadianceStudio.UI.Ribbon;

using System;
using System.Collections.Generic;
using Eto.Drawing;
using RadianceStudio.Core.Enums;
using RadianceStudio.UI.Theme;

/// <summary>
/// High-contrast procedural vector icons drawn via Eto.Drawing.GraphicsPath and Bitmap.
/// Guarantees crisp rendering at any DPI without external raster dependencies.
/// Safely returns null when running in headless unit tests outside of an active Eto platform host.
/// </summary>
public static class RibbonVectorIcons
{
    private static readonly Dictionary<string, Bitmap> _iconCache = new();

    /// <summary>
    /// Checks whether an Eto platform is active in the current application domain.
    /// </summary>
    public static bool IsEtoPlatformAvailable
    {
        get
        {
            try
            {
                return Eto.Platform.Instance != null;
            }
            catch
            {
                return false;
            }
        }
    }

    #region Tab Icons

    public static Bitmap? GetRailsAndShanksIcon(int size = 24)
    {
        return GetCachedOrRender($"Tab_RailsShanks_{size}", size, (g, s) =>
        {
            using var pen = new Pen(JewelryDarkTheme.AccentGold, 2f);
            using var cyanPen = new Pen(JewelryDarkTheme.AccentCyan, 1.5f);

            // Ring rail outer circle
            float cx = s / 2f, cy = s / 2f + 2f, r = s * 0.35f;
            g.DrawEllipse(pen, cx - r, cy - r, r * 2f, r * 2f);

            // Ring rail inner circle (finger hole)
            float rInner = r * 0.70f;
            g.DrawEllipse(cyanPen, cx - rInner, cy - rInner, rInner * 2f, rInner * 2f);

            // Top gem/head accent
            using var brush = new SolidBrush(JewelryDarkTheme.AccentGoldBright);
            g.FillEllipse(brush, cx - 2.5f, cy - r - 3.5f, 5f, 4f);
        });
    }

    public static Bitmap? GetGemsIcon(int size = 24)
    {
        return GetCachedOrRender($"Tab_Gems_{size}", size, (g, s) =>
        {
            using var pen = new Pen(JewelryDarkTheme.AccentCyan, 1.5f);
            using var fill = new SolidBrush(Color.FromArgb(0x00, 0xE5, 0xFF, 0x40));

            // Diamond contour: Table, Crown, Pavilion, Culet
            using var path = new GraphicsPath();
            float pad = s * 0.15f;
            float w = s - 2 * pad;
            float h = s - 2 * pad;

            float x0 = pad, x1 = pad + w * 0.25f, x2 = pad + w * 0.75f, x3 = pad + w;
            float yTable = pad + h * 0.15f;
            float yGirdle = pad + h * 0.45f;
            float yCulet = pad + h * 0.95f;
            float xCulet = pad + w * 0.5f;

            path.MoveTo(x1, yTable);
            path.LineTo(x2, yTable);
            path.LineTo(x3, yGirdle);
            path.LineTo(xCulet, yCulet);
            path.LineTo(x0, yGirdle);
            path.CloseFigure();

            g.FillPath(fill, path);
            g.DrawPath(pen, path);

            // Internal facet lines
            g.DrawLine(pen, x0, yGirdle, x3, yGirdle);
            g.DrawLine(pen, x1, yTable, xCulet, yCulet);
            g.DrawLine(pen, x2, yTable, xCulet, yCulet);
        });
    }

    public static Bitmap? GetSettingsIcon(int size = 24)
    {
        return GetCachedOrRender($"Tab_Settings_{size}", size, (g, s) =>
        {
            using var goldPen = new Pen(JewelryDarkTheme.AccentGold, 1.8f);
            using var cyanBrush = new SolidBrush(JewelryDarkTheme.AccentCyan);

            float cx = s / 2f, cy = s / 2f;

            // Crown / Prong head silhouette
            using var path = new GraphicsPath();
            path.MoveTo(s * 0.2f, s * 0.75f);
            path.LineTo(s * 0.8f, s * 0.75f);
            path.LineTo(s * 0.85f, s * 0.35f);
            path.LineTo(s * 0.65f, s * 0.5f);
            path.LineTo(cx, s * 0.30f);
            path.LineTo(s * 0.35f, s * 0.5f);
            path.LineTo(s * 0.15f, s * 0.35f);
            path.CloseFigure();

            g.DrawPath(goldPen, path);

            // Center stone in setting
            g.FillEllipse(cyanBrush, cx - 2.5f, cy - 3f, 5f, 5f);
        });
    }

    public static Bitmap? GetCuttersIcon(int size = 24)
    {
        return GetCachedOrRender($"Tab_Cutters_{size}", size, (g, s) =>
        {
            using var redPen = new Pen(JewelryDarkTheme.ActionCancel, 1.6f);
            using var fill = new SolidBrush(Color.FromArgb(0xE5, 0x39, 0x35, 0x35));

            // Conical gem cutter punch profile
            using var path = new GraphicsPath();
            float cx = s / 2f;
            path.MoveTo(cx - s * 0.35f, s * 0.2f);
            path.LineTo(cx + s * 0.35f, s * 0.2f);
            path.LineTo(cx + s * 0.35f, s * 0.45f);
            path.LineTo(cx + s * 0.12f, s * 0.75f);
            path.LineTo(cx + s * 0.12f, s * 0.90f);
            path.LineTo(cx - s * 0.12f, s * 0.90f);
            path.LineTo(cx - s * 0.12f, s * 0.75f);
            path.LineTo(cx - s * 0.35f, s * 0.45f);
            path.CloseFigure();

            g.FillPath(fill, path);
            g.DrawPath(redPen, path);
        });
    }

    public static Bitmap? GetTransformIcon(int size = 24)
    {
        return GetCachedOrRender($"Tab_Transform_{size}", size, (g, s) =>
        {
            using var cyanPen = new Pen(JewelryDarkTheme.AccentCyan, 1.8f);
            using var brush = new SolidBrush(JewelryDarkTheme.AccentCyan);

            float cx = s / 2f, cy = s / 2f, r = s * 0.32f;

            // Circular rotation arrows
            g.DrawArc(cyanPen, cx - r, cy - r, r * 2f, r * 2f, 20f, 130f);
            g.DrawArc(cyanPen, cx - r, cy - r, r * 2f, r * 2f, 200f, 130f);

            // Arrow heads
            using var p1 = new GraphicsPath();
            p1.MoveTo(cx + r, cy);
            p1.LineTo(cx + r + 3f, cy + 4f);
            p1.LineTo(cx + r - 3f, cy + 4f);
            p1.CloseFigure();
            g.FillPath(brush, p1);

            using var p2 = new GraphicsPath();
            p2.MoveTo(cx - r, cy);
            p2.LineTo(cx - r - 3f, cy - 4f);
            p2.LineTo(cx - r + 3f, cy - 4f);
            p2.CloseFigure();
            g.FillPath(brush, p2);
        });
    }

    public static Bitmap? GetManufacturingIcon(int size = 24)
    {
        return GetCachedOrRender($"Tab_Manufacturing_{size}", size, (g, s) =>
        {
            using var goldPen = new Pen(JewelryDarkTheme.AccentGold, 1.6f);
            using var textPen = new Pen(JewelryDarkTheme.TextPrimary, 1.2f);

            // Ingot silhouette / Caliper
            float x = s * 0.2f, y = s * 0.3f, w = s * 0.6f, h = s * 0.45f;
            g.DrawRectangle(goldPen, x, y, w, h);

            // Precision tick marks
            g.DrawLine(textPen, x + w * 0.25f, y, x + w * 0.25f, y + 4f);
            g.DrawLine(textPen, x + w * 0.50f, y, x + w * 0.50f, y + 6f);
            g.DrawLine(textPen, x + w * 0.75f, y, x + w * 0.75f, y + 4f);

            // Weight scale pan base
            g.DrawLine(goldPen, s * 0.3f, s * 0.85f, s * 0.7f, s * 0.85f);
        });
    }

    #endregion

    #region Action Icons

    public static Bitmap? GetRingRailIcon(RailProfileType profile = RailProfileType.CanonicalCircle, int size = 24)
    {
        return GetCachedOrRender($"Rail_{profile}_{size}", size, (g, s) =>
        {
            using var pen = new Pen(JewelryDarkTheme.AccentGold, 1.8f);
            float cx = s / 2f, cy = s / 2f;

            switch (profile)
            {
                case RailProfileType.EuroShank:
                    using (var path = new GraphicsPath())
                    {
                        path.AddArc(s * 0.15f, s * 0.15f, s * 0.7f, s * 0.7f, 180f, 180f);
                        path.LineTo(s * 0.85f, s * 0.75f);
                        path.LineTo(s * 0.70f, s * 0.85f);
                        path.LineTo(s * 0.30f, s * 0.85f);
                        path.LineTo(s * 0.15f, s * 0.75f);
                        path.CloseFigure();
                        g.DrawPath(pen, path);
                    }
                    break;

                case RailProfileType.CushionSquircle:
                    g.DrawRectangle(pen, s * 0.2f, s * 0.2f, s * 0.6f, s * 0.6f);
                    break;

                case RailProfileType.BypassOverlap:
                    using (var path = new GraphicsPath())
                    {
                        path.AddArc(s * 0.15f, s * 0.15f, s * 0.7f, s * 0.7f, 40f, 320f);
                        g.DrawPath(pen, path);
                    }
                    break;

                default:
                    // Canonical Circle
                    g.DrawEllipse(pen, s * 0.15f, s * 0.15f, s * 0.7f, s * 0.7f);
                    break;
            }
        });
    }

    public static Bitmap? GetShankIcon(ShankProfileType profile = ShankProfileType.Flat, int size = 24)
    {
        return GetCachedOrRender($"Shank_{profile}_{size}", size, (g, s) =>
        {
            using var pen = new Pen(JewelryDarkTheme.AccentGold, 1.8f);
            float cx = s / 2f, cy = s / 2f;

            switch (profile)
            {
                case ShankProfileType.Cathedral:
                    using (var path = new GraphicsPath())
                    {
                        path.MoveTo(s * 0.2f, s * 0.8f);
                        path.LineTo(s * 0.8f, s * 0.8f);
                        path.LineTo(s * 0.75f, s * 0.4f);
                        path.LineTo(cx, s * 0.2f);
                        path.LineTo(s * 0.25f, s * 0.4f);
                        path.CloseFigure();
                        g.DrawPath(pen, path);
                    }
                    break;

                case ShankProfileType.KnifeEdge:
                    using (var path = new GraphicsPath())
                    {
                        path.MoveTo(s * 0.2f, s * 0.7f);
                        path.LineTo(s * 0.8f, s * 0.7f);
                        path.LineTo(cx, s * 0.25f);
                        path.CloseFigure();
                        g.DrawPath(pen, path);
                    }
                    break;

                case ShankProfileType.DShape:
                    using (var path = new GraphicsPath())
                    {
                        path.MoveTo(s * 0.2f, s * 0.7f);
                        path.LineTo(s * 0.8f, s * 0.7f);
                        path.AddArc(s * 0.2f, s * 0.25f, s * 0.6f, s * 0.5f, 0f, -180f);
                        path.CloseFigure();
                        g.DrawPath(pen, path);
                    }
                    break;

                default:
                    // Standard / Flat / Comfort-Fit
                    g.DrawRectangle(pen, s * 0.2f, s * 0.35f, s * 0.6f, s * 0.35f);
                    break;
            }
        });
    }

    public static Bitmap? GetGemCutIcon(GemCutType cut, int size = 24)
    {
        return GetCachedOrRender($"Gem_{cut}_{size}", size, (g, s) =>
        {
            using var pen = new Pen(JewelryDarkTheme.AccentCyan, 1.5f);
            float cx = s / 2f, cy = s / 2f;

            switch (cut)
            {
                case GemCutType.Princess:
                    g.DrawRectangle(pen, s * 0.2f, s * 0.2f, s * 0.6f, s * 0.6f);
                    g.DrawLine(pen, s * 0.2f, s * 0.2f, s * 0.8f, s * 0.8f);
                    g.DrawLine(pen, s * 0.2f, s * 0.8f, s * 0.8f, s * 0.2f);
                    break;

                case GemCutType.Emerald:
                case GemCutType.Asscher:
                    using (var path = new GraphicsPath())
                    {
                        float d = s * 0.1f;
                        path.MoveTo(s * 0.2f + d, s * 0.2f);
                        path.LineTo(s * 0.8f - d, s * 0.2f);
                        path.LineTo(s * 0.8f, s * 0.2f + d);
                        path.LineTo(s * 0.8f, s * 0.8f - d);
                        path.LineTo(s * 0.8f - d, s * 0.8f);
                        path.LineTo(s * 0.2f + d, s * 0.8f);
                        path.LineTo(s * 0.2f, s * 0.8f - d);
                        path.LineTo(s * 0.2f, s * 0.2f + d);
                        path.CloseFigure();
                        g.DrawPath(pen, path);
                    }
                    break;

                case GemCutType.Oval:
                    g.DrawEllipse(pen, s * 0.25f, s * 0.15f, s * 0.5f, s * 0.7f);
                    break;

                case GemCutType.Pear:
                    using (var path = new GraphicsPath())
                    {
                        path.MoveTo(cx, s * 0.15f);
                        path.LineTo(s * 0.75f, s * 0.60f);
                        path.AddArc(s * 0.25f, s * 0.45f, s * 0.50f, s * 0.40f, 0f, 180f);
                        path.CloseFigure();
                        g.DrawPath(pen, path);
                    }
                    break;

                case GemCutType.Marquise:
                    using (var path = new GraphicsPath())
                    {
                        path.MoveTo(cx, s * 0.12f);
                        path.LineTo(s * 0.78f, cy);
                        path.LineTo(cx, s * 0.88f);
                        path.LineTo(s * 0.22f, cy);
                        path.CloseFigure();
                        g.DrawPath(pen, path);
                    }
                    break;

                case GemCutType.Heart:
                    using (var path = new GraphicsPath())
                    {
                        path.MoveTo(cx, s * 0.82f);
                        path.LineTo(s * 0.2f, s * 0.45f);
                        path.AddArc(s * 0.2f, s * 0.2f, s * 0.3f, s * 0.3f, 180f, 180f);
                        path.AddArc(cx, s * 0.2f, s * 0.3f, s * 0.3f, 180f, 180f);
                        path.CloseFigure();
                        g.DrawPath(pen, path);
                    }
                    break;

                default:
                    // Round Brilliant
                    g.DrawEllipse(pen, s * 0.18f, s * 0.18f, s * 0.64f, s * 0.64f);
                    g.DrawEllipse(pen, s * 0.35f, s * 0.35f, s * 0.30f, s * 0.30f);
                    break;
            }
        });
    }

    public static Bitmap? GetGemsOnCurveIcon(int size = 24)
    {
        return GetCachedOrRender($"Action_GemsOnCurve_{size}", size, (g, s) =>
        {
            using var curvePen = new Pen(JewelryDarkTheme.TextSecondary, 1.2f);
            using var gemBrush = new SolidBrush(JewelryDarkTheme.AccentCyan);

            // Arc spine
            g.DrawArc(curvePen, s * 0.1f, s * 0.1f, s * 0.8f, s * 0.8f, 190f, 80f);

            // Three gems along the curve
            g.FillEllipse(gemBrush, s * 0.20f, s * 0.45f, 4f, 4f);
            g.FillEllipse(gemBrush, s * 0.45f, s * 0.25f, 4f, 4f);
            g.FillEllipse(gemBrush, s * 0.70f, s * 0.20f, 4f, 4f);
        });
    }

    public static Bitmap? GetPaveSurfaceIcon(int size = 24)
    {
        return GetCachedOrRender($"Action_Pave_{size}", size, (g, s) =>
        {
            using var brush = new SolidBrush(JewelryDarkTheme.AccentCyan);
            float r = 2.5f;

            // Honeycomb 3D pavé dots
            g.FillEllipse(brush, s * 0.25f, s * 0.25f, r * 2, r * 2);
            g.FillEllipse(brush, s * 0.50f, s * 0.25f, r * 2, r * 2);
            g.FillEllipse(brush, s * 0.75f, s * 0.25f, r * 2, r * 2);

            g.FillEllipse(brush, s * 0.37f, s * 0.50f, r * 2, r * 2);
            g.FillEllipse(brush, s * 0.62f, s * 0.50f, r * 2, r * 2);

            g.FillEllipse(brush, s * 0.25f, s * 0.75f, r * 2, r * 2);
            g.FillEllipse(brush, s * 0.50f, s * 0.75f, r * 2, r * 2);
            g.FillEllipse(brush, s * 0.75f, s * 0.75f, r * 2, r * 2);
        });
    }

    public static Bitmap? GetProngHeadIcon(int prongCount = 4, int size = 24)
    {
        return GetCachedOrRender($"Action_Prongs_{prongCount}_{size}", size, (g, s) =>
        {
            using var ringPen = new Pen(JewelryDarkTheme.AccentGold, 1.5f);
            using var prongBrush = new SolidBrush(JewelryDarkTheme.AccentGoldBright);

            float cx = s / 2f, cy = s / 2f, r = s * 0.30f;
            g.DrawEllipse(ringPen, cx - r, cy - r, r * 2f, r * 2f);

            // Draw prongs around circle
            for (int i = 0; i < prongCount; i++)
            {
                double angle = (2.0 * Math.PI * i) / prongCount;
                float px = cx + (float)(r * Math.Cos(angle));
                float py = cy + (float)(r * Math.Sin(angle));
                g.FillEllipse(prongBrush, px - 2f, py - 2f, 4f, 4f);
            }
        });
    }

    public static Bitmap? GetFullBezelIcon(int size = 24)
    {
        return GetCachedOrRender($"Action_Bezel_Full_{size}", size, (g, s) =>
        {
            using var outerPen = new Pen(JewelryDarkTheme.AccentGold, 2.5f);
            using var innerPen = new Pen(JewelryDarkTheme.AccentCyan, 1.2f);

            float cx = s / 2f, cy = s / 2f;
            g.DrawEllipse(outerPen, cx - s * 0.32f, cy - s * 0.32f, s * 0.64f, s * 0.64f);
            g.DrawEllipse(innerPen, cx - s * 0.20f, cy - s * 0.20f, s * 0.40f, s * 0.40f);
        });
    }

    public static Bitmap? GetSemiBezelIcon(int size = 24)
    {
        return GetCachedOrRender($"Action_Bezel_Semi_{size}", size, (g, s) =>
        {
            using var outerPen = new Pen(JewelryDarkTheme.AccentGold, 2.5f);
            using var innerPen = new Pen(JewelryDarkTheme.AccentCyan, 1.2f);

            float cx = s / 2f, cy = s / 2f;
            // Opposing semi-bezel cups
            g.DrawArc(outerPen, cx - s * 0.32f, cy - s * 0.32f, s * 0.64f, s * 0.64f, 30f, 120f);
            g.DrawArc(outerPen, cx - s * 0.32f, cy - s * 0.32f, s * 0.64f, s * 0.64f, 210f, 120f);
            g.DrawEllipse(innerPen, cx - s * 0.20f, cy - s * 0.20f, s * 0.40f, s * 0.40f);
        });
    }

    public static Bitmap? GetBooleanDifferenceIcon(int size = 24)
    {
        return GetCachedOrRender($"Action_Boolean_{size}", size, (g, s) =>
        {
            using var goldPen = new Pen(JewelryDarkTheme.AccentGold, 1.8f);
            using var redPen = new Pen(JewelryDarkTheme.ActionCancel, 1.5f);

            // Metal base square
            g.DrawRectangle(goldPen, s * 0.15f, s * 0.25f, s * 0.50f, s * 0.50f);
            // Cutter subtracting circular hole
            g.DrawEllipse(redPen, s * 0.45f, s * 0.25f, s * 0.45f, s * 0.45f);
        });
    }

    public static Bitmap? GetApplyIcon(int size = 24)
    {
        return GetCachedOrRender($"Action_Apply_{size}", size, (g, s) =>
        {
            using var pen = new Pen(JewelryDarkTheme.ActionApply, 2.5f);
            using var path = new GraphicsPath();
            path.MoveTo(s * 0.20f, s * 0.52f);
            path.LineTo(s * 0.42f, s * 0.74f);
            path.LineTo(s * 0.82f, s * 0.28f);
            g.DrawPath(pen, path);
        });
    }

    public static Bitmap? GetCancelIcon(int size = 24)
    {
        return GetCachedOrRender($"Action_Cancel_{size}", size, (g, s) =>
        {
            using var pen = new Pen(JewelryDarkTheme.ActionCancel, 2.5f);
            g.DrawLine(pen, s * 0.25f, s * 0.25f, s * 0.75f, s * 0.75f);
            g.DrawLine(pen, s * 0.75f, s * 0.25f, s * 0.25f, s * 0.75f);
        });
    }

    #endregion

    private static Bitmap? GetCachedOrRender(string key, int size, Action<Graphics, int> renderAction)
    {
        if (!IsEtoPlatformAvailable)
        {
            return null;
        }

        if (_iconCache.TryGetValue(key, out var cached))
        {
            return cached;
        }

        try
        {
            var bitmap = new Bitmap(size, size, PixelFormat.Format32bppRgba);
            using (var g = new Graphics(bitmap))
            {
                g.AntiAlias = true;
                renderAction(g, size);
            }

            _iconCache[key] = bitmap;
            return bitmap;
        }
        catch
        {
            return null;
        }
    }
}
