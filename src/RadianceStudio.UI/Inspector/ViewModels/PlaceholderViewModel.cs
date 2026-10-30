namespace RadianceStudio.UI.ViewModels;

using System;
using RadianceStudio.Core.Display;
using RadianceStudio.Core.Graph;
using RadianceStudio.Core.Models;
using Rhino;

/// <summary>
/// Reactive ViewModel for tools that are planned or under development.
/// Displays an interactive lateral card with simulated parameters, roadmap details, and status.
/// </summary>
public class PlaceholderViewModel : IInspectorViewModel
{
    private string _title = "Parametric Tool";
    private string _category = "Jewelry Design";
    private string _description = "Advanced jewelry CAD engine component.";
    private double _param1Value = 1.0;
    private double _param2Value = 5.0;
    private string _selectedOption = "Standard";

    public string Title
    {
        get => _title;
        set => _title = value;
    }

    public string FeatureType => "Placeholder";

    public string Category
    {
        get => _category;
        set => _category = value;
    }

    public string Description
    {
        get => _description;
        set => _description = value;
    }

    public string Param1Label { get; set; } = "Parameter 1 (mm)";
    public double Param1Value
    {
        get => _param1Value;
        set
        {
            _param1Value = value;
            UpdatePreview();
        }
    }
    public double Param1Min { get; set; } = 0.1;
    public double Param1Max { get; set; } = 10.0;
    public double Param1Step { get; set; } = 0.1;

    public string Param2Label { get; set; } = "Parameter 2";
    public double Param2Value
    {
        get => _param2Value;
        set
        {
            _param2Value = value;
            UpdatePreview();
        }
    }
    public double Param2Min { get; set; } = 1.0;
    public double Param2Max { get; set; } = 50.0;
    public double Param2Step { get; set; } = 1.0;

    public string OptionLabel { get; set; } = "Pattern / Mode";
    public string[] Options { get; set; } = new[] { "Standard", "Fine", "Organic", "Geometric" };
    public string SelectedOption
    {
        get => _selectedOption;
        set
        {
            _selectedOption = value;
            UpdatePreview();
        }
    }

    public bool IsEditMode => false;
    public Guid? EditingObjectId => null;

    public RadiancePreviewConduit? Conduit { get; set; }
    public ParametricGraphManager? GraphManager { get; set; }

    public event EventHandler? PreviewUpdated;

    public void Initialize()
    {
    }

    public void UpdatePreview()
    {
        PreviewUpdated?.Invoke(this, EventArgs.Empty);
    }

    public void Apply(RhinoDoc doc)
    {
        RhinoApp.WriteLine($"[RadianceStudio] {Title} ({Category}): Parameter template saved ({Param1Label}={Param1Value:F2}, {Param2Label}={Param2Value:F1}, {OptionLabel}={SelectedOption}). Full B-Rep generator scheduled for upcoming update.");
    }

    public void Cancel()
    {
        if (Conduit != null)
        {
            Conduit.Enabled = false;
            Conduit.ClearInstances();
        }
    }

    public void LoadFromMetadata(ParametricMetadata metadata, Guid objectId)
    {
        if (metadata.Parameters.TryGetString("Title", out var t)) _title = t;
        if (metadata.Parameters.TryGetString("Category", out var c)) _category = c;
    }
}
