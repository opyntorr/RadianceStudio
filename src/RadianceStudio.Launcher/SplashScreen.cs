namespace RadianceStudio.Launcher;

using System.Diagnostics;
using Eto.Drawing;
using Eto.Forms;

/// <summary>
/// Borderless Eto.Forms splash screen for RadianceStudio.
/// Displays branding, gemstone vector logo, animated loading bar, and synchronizes
/// with Rhino 8 viewport initialization readiness file (%TEMP%\radiance_ready_&lt;pid&gt;.tmp).
/// </summary>
public class SplashScreen : Form
{
    private const int SplashWidth = 540;
    private const int SplashHeight = 320;

    private static readonly Color CharcoalBackground = Color.FromArgb(0x1E, 0x1E, 0x1E);
    private static readonly Color GoldAccent = Color.FromArgb(0xD4, 0xAF, 0x37);
    private static readonly Color CyanAccent = Color.FromArgb(0x00, 0xE5, 0xFF);
    private static readonly Color DarkTrackColor = Color.FromArgb(0x2D, 0x2D, 0x30);
    private static readonly Color MutedTextColor = Color.FromArgb(0x9E, 0x9E, 0x9E);

    private readonly Process? _attachedProcess;
    private readonly UITimer _timer;
    private readonly DateTime _startTime;
    private readonly TimeSpan _timeout;

    private double _progressValue; // 0.0 to 1.0
    private string _statusText = "Initializing RadianceStudio workspace...";
    private bool _isClosing;

    private readonly Drawable _canvas;

    public SplashScreen(Process? attachedProcess = null, TimeSpan? timeout = null)
    {
        _attachedProcess = attachedProcess;
        _timeout = timeout ?? TimeSpan.FromSeconds(60);
        _startTime = DateTime.UtcNow;

        Title = "RadianceStudio";
        ClientSize = new Size(SplashWidth, SplashHeight);
        WindowStyle = WindowStyle.None;
        Topmost = true;
        Resizable = false;
        Maximizable = false;
        Minimizable = false;
        ShowInTaskbar = true;
        BackgroundColor = CharcoalBackground;

        _canvas = new Drawable
        {
            Size = new Size(SplashWidth, SplashHeight),
            BackgroundColor = CharcoalBackground
        };
        _canvas.Paint += OnCanvasPaint;

        Content = _canvas;

        _timer = new UITimer { Interval = 0.04 }; // ~25 fps animation update
        _timer.Elapsed += OnTimerElapsed;
        _timer.Start();
    }

    /// <summary>
    /// Current progress from 0.0 to 1.0.
    /// </summary>
    public double Progress
    {
        get => _progressValue;
        set
        {
            _progressValue = Math.Clamp(value, 0.0, 1.0);
            _canvas.Invalidate();
        }
    }

    /// <summary>
    /// Current status label displayed beneath the progress bar.
    /// </summary>
    public string StatusText
    {
        get => _statusText;
        set
        {
            _statusText = value;
            _canvas.Invalidate();
        }
    }

    private void OnTimerElapsed(object? sender, EventArgs e)
    {
        if (_isClosing)
        {
            return;
        }

        TimeSpan elapsed = DateTime.UtcNow - _startTime;

        // Smooth progress progression while loading
        if (_progressValue < 0.88)
        {
            _progressValue += 0.015;
        }
        else if (_progressValue < 0.96)
        {
            _progressValue += 0.002;
        }

        // Check readiness signal from Rhino if attached
        if (_attachedProcess != null)
        {
            if (_attachedProcess.HasExited)
            {
                _isClosing = true;
                _timer.Stop();
                StatusText = "Rhino process terminated unexpectedly.";
                CloseAfterDelay(1500);
                return;
            }

            if (RhinoBootstrapper.IsReadinessSignaled(_attachedProcess.Id))
            {
                _isClosing = true;
                _timer.Stop();
                Progress = 1.0;
                StatusText = "Viewport ready. Launching session...";
                RhinoBootstrapper.ClearReadinessSignal(_attachedProcess.Id);
                CloseAfterDelay(300);
                return;
            }
        }

        // Timeout fallback
        if (elapsed > _timeout)
        {
            _isClosing = true;
            _timer.Stop();
            Progress = 1.0;
            StatusText = "Workspace ready.";
            CloseAfterDelay(200);
            return;
        }

        _canvas.Invalidate();
    }

    private void CloseAfterDelay(int milliseconds)
    {
        Task.Delay(milliseconds).ContinueWith(_ =>
        {
            Application.Instance.AsyncInvoke(() =>
            {
                try
                {
                    Close();
                }
                catch
                {
                    // Ignore disposal errors
                }
            });
        });
    }

    private void OnCanvasPaint(object? sender, PaintEventArgs e)
    {
        Graphics g = e.Graphics;

        // 1. Dark charcoal background canvas
        g.FillRectangle(CharcoalBackground, 0, 0, SplashWidth, SplashHeight);

        // 2. Gold border (1.5px border inset by 1px)
        using (var borderPen = new Pen(GoldAccent, 1.5f))
        {
            g.DrawRectangle(borderPen, 1, 1, SplashWidth - 2, SplashHeight - 2);
        }

        // 3. Faceted Gemstone Vector Logo (Centered at X=270, Y=35 to Y=95)
        DrawGemstoneLogo(g, 270, 65, 42, 34);

        // 4. "RadianceStudio" 24pt Bold Title
        using (var titleFont = Fonts.Sans(24, FontStyle.Bold))
        {
            string title = "RadianceStudio";
            SizeF titleSize = titleFont.MeasureString(title);
            float titleX = (SplashWidth - titleSize.Width) / 2f;
            g.DrawText(titleFont, Colors.White, titleX, 115, title);
        }

        // 5. "Jewelry CAD Suite" Subtitle
        using (var subtitleFont = Fonts.Sans(12, FontStyle.None))
        {
            string subtitle = "Jewelry CAD Suite";
            SizeF subSize = subtitleFont.MeasureString(subtitle);
            float subX = (SplashWidth - subSize.Width) / 2f;
            g.DrawText(subtitleFont, GoldAccent, subX, 160, subtitle);
        }

        // 6. Version "v1.0.0"
        using (var versionFont = Fonts.Sans(9, FontStyle.None))
        {
            string version = "v1.0.0";
            SizeF verSize = versionFont.MeasureString(version);
            g.DrawText(versionFont, MutedTextColor, SplashWidth - verSize.Width - 18, SplashHeight - verSize.Height - 14, version);
        }

        // 7. Animated Loading Bar (Track & Fill)
        float barWidth = 380f;
        float barHeight = 6f;
        float barX = (SplashWidth - barWidth) / 2f;
        float barY = 220f;

        // Track background
        g.FillRectangle(DarkTrackColor, barX, barY, barWidth, barHeight);

        // Active fill
        float fillWidth = (float)(barWidth * Math.Clamp(_progressValue, 0.0, 1.0));
        if (fillWidth > 0)
        {
            g.FillRectangle(GoldAccent, barX, barY, fillWidth, barHeight);

            // Cyan leading pulse indicator
            float pulseWidth = Math.Min(12f, fillWidth);
            g.FillRectangle(CyanAccent, barX + fillWidth - pulseWidth, barY, pulseWidth, barHeight);
        }

        // 8. Status Text Label
        using (var statusFont = Fonts.Sans(10, FontStyle.None))
        {
            SizeF statusSize = statusFont.MeasureString(_statusText);
            float statusX = (SplashWidth - statusSize.Width) / 2f;
            g.DrawText(statusFont, MutedTextColor, statusX, 245, _statusText);
        }
    }

    /// <summary>
    /// Draws a precision faceted gemstone vector icon using geometric symmetry.
    /// </summary>
    private static void DrawGemstoneLogo(Graphics g, float cx, float cy, float halfW, float halfH)
    {
        // Table (flat top) coordinates
        float tableHalfW = halfW * 0.52f;
        float tableTopY = cy - halfH;
        float girdleY = cy - (halfH * 0.15f);
        float culetY = cy + halfH;

        PointF tableLeft = new(cx - tableHalfW, tableTopY);
        PointF tableRight = new(cx + tableHalfW, tableTopY);
        PointF girdleLeft = new(cx - halfW, girdleY);
        PointF girdleRight = new(cx + halfW, girdleY);
        PointF culet = new(cx, culetY);

        PointF midGirdleLeft = new(cx - (halfW * 0.45f), girdleY);
        PointF midGirdleRight = new(cx + (halfW * 0.45f), girdleY);

        // Gemstone facet lines
        using var goldPen = new Pen(GoldAccent, 1.8f);
        using var cyanPen = new Pen(CyanAccent, 1.2f);
        using var fillBrush = new SolidBrush(Color.FromArgb(0x28, 0x28, 0x2C));

        // Outer contour polygon
        PointF[] contour = [tableLeft, tableRight, girdleRight, culet, girdleLeft];
        g.FillPolygon(fillBrush, contour);
        g.DrawPolygon(goldPen, contour);

        // Table facet line
        g.DrawLine(goldPen, tableLeft, tableRight);

        // Star / kite crown facet lines
        g.DrawLine(cyanPen, tableLeft, midGirdleLeft);
        g.DrawLine(cyanPen, tableRight, midGirdleRight);

        // Pavilion facet lines converging to culet
        g.DrawLine(goldPen, midGirdleLeft, culet);
        g.DrawLine(goldPen, midGirdleRight, culet);
        g.DrawLine(cyanPen, new PointF(cx, girdleY), culet);
    }

    protected override void OnClosed(EventArgs e)
    {
        _timer.Stop();
        _timer.Dispose();
        base.OnClosed(e);
    }
}
