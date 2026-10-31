namespace RadianceStudio.UI.Panels;

using System;
using Eto.Drawing;
using Eto.Forms;
using RadianceStudio.UI.Theme;

/// <summary>
/// Bottom-left docked Display Modes switcher panel providing 1-click toggling
/// between Wireframe, Shaded, Rendered, Technical, and dedicated jewelry display styles.
/// </summary>
public class DisplayModesPanel : Panel
{
    public const int DefaultPanelWidth = 240;

    public event EventHandler<string>? CommandRequested;

    public DisplayModesPanel()
    {
        Width = DefaultPanelWidth;
        Height = 42;
        BackgroundColor = JewelryDarkTheme.SurfaceElevated;

        var layout = new StackLayout
        {
            Orientation = Orientation.Horizontal,
            Spacing = 4,
            Padding = new Padding(6, 4),
            VerticalContentAlignment = VerticalAlignment.Center
        };

        var modes = new (string Label, string Macro, string Tooltip)[]
        {
            ("Wire", "! _SetDisplayMode _Wireframe", "Wireframe Display Mode"),
            ("Shaded", "! _SetDisplayMode _Shaded", "Shaded CAD Modeling Mode"),
            ("Render", "! _SetDisplayMode _Rendered", "Real-Time Rendered Mode"),
            ("Ghost", "! _SetDisplayMode _Ghosted", "Semi-Transparent Ghosted Mode"),
            ("Tech", "! _SetDisplayMode _Technical", "Technical Blueprint Illustration Mode"),
            ("Raytrace", "! _SetDisplayMode _Raytraced", "Interactive GPU Raytraced Mode")
        };

        foreach (var mode in modes)
        {
            var btn = new Button
            {
                Text = mode.Label,
                Font = JewelryDarkTheme.SmallFont,
                BackgroundColor = JewelryDarkTheme.SurfaceCard,
                TextColor = JewelryDarkTheme.TextPrimary,
                Height = 28,
                ToolTip = mode.Tooltip
            };

            string cmd = mode.Macro;
            btn.Click += (s, e) => CommandRequested?.Invoke(this, cmd);
            layout.Items.Add(btn);
        }

        Content = layout;
        JewelryDarkTheme.ApplyTheme(this);
    }
}
