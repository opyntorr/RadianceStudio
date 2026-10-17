namespace RadianceStudio.UI.Theme;

using System;
using Eto.Drawing;
using Eto.Forms;

/// <summary>
/// Professional dark charcoal theme designed for jewelry CAD workstations.
/// Features high contrast dark surfaces, warm gold primary branding accents,
/// and cyan viewport/conduit indicators to minimize eye strain and maximize
/// gemstone and precious metal facet contrast.
/// Pure cross-platform Eto.Forms styling without any legacy platform dependencies.
/// </summary>
public static class JewelryDarkTheme
{
    // Charcoal Palette
    public static readonly Color BackgroundDark = Color.FromArgb(0x1E, 0x1E, 0x1E);
    public static readonly Color SurfaceCard = Color.FromArgb(0x25, 0x25, 0x26);
    public static readonly Color SurfaceElevated = Color.FromArgb(0x2D, 0x2D, 0x30);
    public static readonly Color BorderSubtle = Color.FromArgb(0x3F, 0x3F, 0x46);

    // Gold Accents
    public static readonly Color AccentGold = Color.FromArgb(0xD4, 0xAF, 0x37);
    public static readonly Color AccentGoldBright = Color.FromArgb(0xFF, 0xD7, 0x00);

    // Cyan Accent
    public static readonly Color AccentCyan = Color.FromArgb(0x00, 0xE5, 0xFF);

    // Text Colors
    public static readonly Color TextPrimary = Color.FromArgb(0xF1, 0xF1, 0xF1);
    public static readonly Color TextSecondary = Color.FromArgb(0xAA, 0xAA, 0xAA);
    public static readonly Color TextMuted = Color.FromArgb(0x71, 0x71, 0x7A);

    // Action Buttons
    public static readonly Color ActionApply = Color.FromArgb(0x2E, 0xA4, 0x4F);
    public static readonly Color ActionCancel = Color.FromArgb(0xE5, 0x39, 0x35);

    // Fonts (Lazy-loaded to support headless test execution without UI platform initialization)
    private static Font? _headerFont;
    private static Font? _subheaderFont;
    private static Font? _bodyFont;
    private static Font? _monospaceFont;
    private static Font? _smallFont;

    public static Font HeaderFont => _headerFont ??= (Eto.Platform.Instance != null ? new(FontFamilies.SansFamilyName, 12f, FontStyle.Bold) : null!);
    public static Font SubheaderFont => _subheaderFont ??= (Eto.Platform.Instance != null ? new(FontFamilies.SansFamilyName, 10f, FontStyle.Bold) : null!);
    public static Font BodyFont => _bodyFont ??= (Eto.Platform.Instance != null ? new(FontFamilies.SansFamilyName, 9f, FontStyle.None) : null!);
    public static Font MonospaceFont => _monospaceFont ??= (Eto.Platform.Instance != null ? new(FontFamilies.MonospaceFamilyName, 9f, FontStyle.None) : null!);
    public static Font SmallFont => _smallFont ??= (Eto.Platform.Instance != null ? new(FontFamilies.SansFamilyName, 8f, FontStyle.None) : null!);

    /// <summary>
    /// Recursively applies the dark jewelry theme across an Eto control hierarchy.
    /// </summary>
    public static void ApplyTheme(Control? control)
    {
        if (control == null) return;

        switch (control)
        {
            case Scrollable scrollable:
                scrollable.BackgroundColor = BackgroundDark;
                if (scrollable.Content != null)
                {
                    ApplyTheme(scrollable.Content);
                }
                break;

            case Panel panel:
                panel.BackgroundColor = BackgroundDark;
                if (panel.Content != null)
                {
                    ApplyTheme(panel.Content);
                }
                break;

            case Label label:
                StyleLabel(label);
                break;

            case Button button:
                StyleButton(button);
                break;

            case NumericStepper stepper:
                StyleStepper(stepper);
                break;

            case Slider slider:
                StyleSlider(slider);
                break;

            case DropDown dropDown:
                StyleDropDown(dropDown);
                break;

            case TextBox textBox:
                StyleTextBox(textBox);
                break;

            case CheckBox checkBox:
                StyleCheckBox(checkBox);
                break;

            case Container container:
                container.BackgroundColor = BackgroundDark;
                foreach (var child in container.Children)
                {
                    ApplyTheme(child);
                }
                break;

            default:
                break;
        }
    }

    public static void StyleCard(Panel panel)
    {
        panel.BackgroundColor = SurfaceCard;
        panel.Padding = new Padding(8);
    }

    public static void StyleElevated(Panel panel)
    {
        panel.BackgroundColor = SurfaceElevated;
        panel.Padding = new Padding(6);
    }

    public static void StyleLabel(Label label, bool isSecondary = false)
    {
        label.TextColor = isSecondary ? TextSecondary : TextPrimary;
        if (BodyFont != null) label.Font = BodyFont;
    }

    public static void StyleHeader(Label label)
    {
        label.TextColor = AccentGold;
        if (HeaderFont != null) label.Font = HeaderFont;
    }

    public static void StyleSubheader(Label label)
    {
        label.TextColor = TextPrimary;
        if (SubheaderFont != null) label.Font = SubheaderFont;
    }

    public static void StyleAccentLabel(Label label)
    {
        label.TextColor = AccentCyan;
        if (BodyFont != null) label.Font = BodyFont;
    }

    public static void StyleButton(Button button)
    {
        button.BackgroundColor = SurfaceElevated;
        button.TextColor = TextPrimary;
        if (BodyFont != null) button.Font = BodyFont;
    }

    public static void StylePrimaryButton(Button button)
    {
        button.BackgroundColor = AccentGold;
        button.TextColor = BackgroundDark;
        if (SubheaderFont != null) button.Font = SubheaderFont;
    }

    public static void StyleSuccessButton(Button button)
    {
        button.BackgroundColor = ActionApply;
        button.TextColor = TextPrimary;
        if (SubheaderFont != null) button.Font = SubheaderFont;
    }

    public static void StyleDangerButton(Button button)
    {
        button.BackgroundColor = ActionCancel;
        button.TextColor = TextPrimary;
        if (SubheaderFont != null) button.Font = SubheaderFont;
    }

    public static void StyleStepper(NumericStepper stepper)
    {
        stepper.BackgroundColor = SurfaceElevated;
        stepper.TextColor = TextPrimary;
        if (MonospaceFont != null) stepper.Font = MonospaceFont;
    }

    public static void StyleSlider(Slider slider)
    {
        slider.BackgroundColor = BackgroundDark;
    }

    public static void StyleDropDown(DropDown dropDown)
    {
        dropDown.BackgroundColor = SurfaceElevated;
        dropDown.TextColor = TextPrimary;
        if (BodyFont != null) dropDown.Font = BodyFont;
    }

    public static void StyleTextBox(TextBox textBox)
    {
        textBox.BackgroundColor = SurfaceElevated;
        textBox.TextColor = TextPrimary;
        if (MonospaceFont != null) textBox.Font = MonospaceFont;
    }

    public static void StyleCheckBox(CheckBox checkBox)
    {
        checkBox.TextColor = TextPrimary;
        if (BodyFont != null) checkBox.Font = BodyFont;
    }
}
