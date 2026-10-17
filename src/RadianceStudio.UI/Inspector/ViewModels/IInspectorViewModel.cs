namespace RadianceStudio.UI.ViewModels;

using System;
using RadianceStudio.Core.Display;
using RadianceStudio.Core.Graph;
using RadianceStudio.Core.Models;
using Rhino;

/// <summary>
/// Reactive contract for all jewelry parameter inspector ViewModels.
/// Handles two-way property binding, GPU viewport preview conduit updates at 60 FPS,
/// baking geometry with native ParametricMetadata, and graph registration.
/// </summary>
public interface IInspectorViewModel
{
    string Title { get; }
    string FeatureType { get; }
    bool IsEditMode { get; }
    Guid? EditingObjectId { get; }

    RadiancePreviewConduit? Conduit { get; set; }
    ParametricGraphManager? GraphManager { get; set; }

    void Initialize();
    void UpdatePreview();
    void Apply(RhinoDoc doc);
    void Cancel();
    void LoadFromMetadata(ParametricMetadata metadata, Guid objectId);

    event EventHandler? PreviewUpdated;
}
