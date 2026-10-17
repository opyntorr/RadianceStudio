namespace RadianceStudio.UI.ViewModels;

using System;
using RadianceStudio.Core.Display;
using RadianceStudio.Core.Engines;
using RadianceStudio.Core.Enums;
using RadianceStudio.Core.Graph;
using RadianceStudio.Core.Models;
using Rhino;
using Rhino.Geometry;

/// <summary>
/// Reactive ViewModel for parametric ring rail curves across international sizing standards
/// (US, EU, UK, JP) and procedural profiles (Circle, Euro-Shank, Cushion Squircle, Bypass).
/// </summary>
public class RingRailViewModel : IInspectorViewModel
{
    private RingSizingStandard _standard = RingSizingStandard.US;
    private double _ringSize = 7.0;
    private string? _ukSizeStr;
    private RailProfileType _profileType = RailProfileType.CanonicalCircle;
    private double _innerDiameterMm;
    private double _cornerRadiusMm = 1.5;
    private double _superellipseExponent = 3.0;
    private double _bypassOverlapDeg = 45.0;
    private double _bypassAxialOffsetMm = 2.0;
    private double _bypassRadialOffsetMm = 1.5;

    public RingRailViewModel()
    {
        RecalculateDiameter();
    }

    public string Title => IsEditMode ? "Edit Ring Rail" : "Ring Rail Creator";
    public string FeatureType => "Rail";
    public bool IsEditMode { get; private set; }
    public Guid? EditingObjectId { get; private set; }

    public RadiancePreviewConduit? Conduit { get; set; }
    public ParametricGraphManager? GraphManager { get; set; }

    public event EventHandler? PreviewUpdated;

    public RingSizingStandard Standard
    {
        get => _standard;
        set
        {
            if (_standard != value)
            {
                _standard = value;
                RecalculateDiameter();
                UpdatePreview();
            }
        }
    }

    public double RingSize
    {
        get => _ringSize;
        set
        {
            if (Math.Abs(_ringSize - value) > 1e-4)
            {
                _ringSize = value;
                RecalculateDiameter();
                UpdatePreview();
            }
        }
    }

    public string? UkSizeStr
    {
        get => _ukSizeStr;
        set
        {
            if (_ukSizeStr != value)
            {
                _ukSizeStr = value;
                RecalculateDiameter();
                UpdatePreview();
            }
        }
    }

    public RailProfileType ProfileType
    {
        get => _profileType;
        set
        {
            if (_profileType != value)
            {
                _profileType = value;
                UpdatePreview();
            }
        }
    }

    public double InnerDiameterMm
    {
        get => _innerDiameterMm;
        set
        {
            if (Math.Abs(_innerDiameterMm - value) > 1e-4 && value > 0)
            {
                _innerDiameterMm = value;
                _ringSize = RailEngine.InnerDiameterMmToSize(_standard, value, out _ukSizeStr);
                UpdatePreview();
            }
        }
    }

    public double CornerRadiusMm
    {
        get => _cornerRadiusMm;
        set
        {
            if (Math.Abs(_cornerRadiusMm - value) > 1e-4)
            {
                _cornerRadiusMm = value;
                UpdatePreview();
            }
        }
    }

    public double SuperellipseExponent
    {
        get => _superellipseExponent;
        set
        {
            if (Math.Abs(_superellipseExponent - value) > 1e-4)
            {
                _superellipseExponent = value;
                UpdatePreview();
            }
        }
    }

    public double BypassOverlapDeg
    {
        get => _bypassOverlapDeg;
        set
        {
            if (Math.Abs(_bypassOverlapDeg - value) > 1e-4)
            {
                _bypassOverlapDeg = value;
                UpdatePreview();
            }
        }
    }

    public double BypassAxialOffsetMm
    {
        get => _bypassAxialOffsetMm;
        set
        {
            if (Math.Abs(_bypassAxialOffsetMm - value) > 1e-4)
            {
                _bypassAxialOffsetMm = value;
                UpdatePreview();
            }
        }
    }

    public double BypassRadialOffsetMm
    {
        get => _bypassRadialOffsetMm;
        set
        {
            if (Math.Abs(_bypassRadialOffsetMm - value) > 1e-4)
            {
                _bypassRadialOffsetMm = value;
                UpdatePreview();
            }
        }
    }

    public void Initialize()
    {
        IsEditMode = false;
        EditingObjectId = null;
        _standard = RingSizingStandard.US;
        _ringSize = 7.0;
        _profileType = RailProfileType.CanonicalCircle;
        RecalculateDiameter();
        UpdatePreview();
    }

    public void UpdatePreview()
    {
        if (Conduit != null && RailEngine.IsRhinoNativeAvailable)
        {
            var rail = RailEngine.CreateRingRail(
                _innerDiameterMm,
                _profileType,
                _cornerRadiusMm,
                _superellipseExponent,
                _bypassOverlapDeg,
                _bypassAxialOffsetMm,
                _bypassRadialOffsetMm);

            if (rail != null)
            {
                // Create a thin pipe preview mesh for GPU display conduit
                var pipeBreps = Brep.CreatePipe(rail, 0.4, false, PipeCapMode.Round, true, 0.01, 0.1);
                if (pipeBreps != null && pipeBreps.Length > 0)
                {
                    var mesh = new Mesh();
                    foreach (var b in pipeBreps)
                    {
                        var faceMeshes = Mesh.CreateFromBrep(b, MeshingParameters.FastRenderMesh);
                        if (faceMeshes != null)
                        {
                            foreach (var m in faceMeshes) mesh.Append(m);
                        }
                    }
                    Conduit.SetInstances(mesh, new[] { Transform.Identity });
                }
            }
            Conduit.Enabled = true;
        }

        PreviewUpdated?.Invoke(this, EventArgs.Empty);
    }

    public void Apply(RhinoDoc? doc)
    {
        if (Conduit != null)
        {
            Conduit.Enabled = false;
            Conduit.ClearInstances();
        }

        if (doc == null || !RailEngine.IsRhinoNativeAvailable)
        {
            return;
        }

        var railCurve = RailEngine.CreateRingRail(
            _innerDiameterMm,
            _profileType,
            _cornerRadiusMm,
            _superellipseExponent,
            _bypassOverlapDeg,
            _bypassAxialOffsetMm,
            _bypassRadialOffsetMm);

        if (railCurve == null)
        {
            return;
        }

        var metadata = ParametricMetadata.Attach(railCurve);
        metadata.Parameters.Set("FeatureType", "Rail");
        metadata.Parameters.Set("Standard", _standard.ToString());
        metadata.Parameters.Set("RingSize", _ringSize);
        if (_ukSizeStr != null) metadata.Parameters.Set("UkSizeStr", _ukSizeStr);
        metadata.Parameters.Set("ProfileType", _profileType.ToString());
        metadata.Parameters.Set("InnerDiameterMm", _innerDiameterMm);
        metadata.Parameters.Set("CornerRadiusMm", _cornerRadiusMm);
        metadata.Parameters.Set("SuperellipseExponent", _superellipseExponent);
        metadata.Parameters.Set("BypassOverlapDeg", _bypassOverlapDeg);
        metadata.Parameters.Set("BypassAxialOffsetMm", _bypassAxialOffsetMm);
        metadata.Parameters.Set("BypassRadialOffsetMm", _bypassRadialOffsetMm);

        Guid objId;
        if (IsEditMode && EditingObjectId.HasValue)
        {
            doc.Objects.Replace(EditingObjectId.Value, railCurve);
            objId = EditingObjectId.Value;
        }
        else
        {
            objId = doc.Objects.AddCurve(railCurve);
        }

        if (GraphManager != null)
        {
            var node = GraphManager.GetNode(objId) ?? new FeatureNode
            {
                Id = objId,
                Name = $"{_profileType} Rail (Size {_ringSize:F1})",
                FeatureType = "Rail"
            };
            node.Parameters["InnerDiameterMm"] = _innerDiameterMm;
            node.Parameters["ProfileType"] = _profileType.ToString();
            GraphManager.RegisterNode(node);
        }

        doc.Views.Redraw();
    }

    public void Cancel()
    {
        if (Conduit != null)
        {
            Conduit.Enabled = false;
            Conduit.ClearInstances();
        }

        try
        {
            RhinoDoc.ActiveDoc?.Views.Redraw();
        }
        catch
        {
        }
    }

    public void LoadFromMetadata(ParametricMetadata metadata, Guid objectId)
    {
        if (metadata == null) return;

        IsEditMode = true;
        EditingObjectId = objectId;

        if (metadata.Parameters.TryGetString("Standard", out var stdStr) &&
            Enum.TryParse<RingSizingStandard>(stdStr, out var parsedStd))
        {
            _standard = parsedStd;
        }

        if (metadata.Parameters.TryGetDouble("RingSize", out var rSize))
        {
            _ringSize = rSize;
        }

        if (metadata.Parameters.TryGetString("UkSizeStr", out var ukStr))
        {
            _ukSizeStr = ukStr;
        }

        if (metadata.Parameters.TryGetString("ProfileType", out var profStr) &&
            Enum.TryParse<RailProfileType>(profStr, out var parsedProf))
        {
            _profileType = parsedProf;
        }

        if (metadata.Parameters.TryGetDouble("InnerDiameterMm", out var dMm))
        {
            _innerDiameterMm = dMm;
        }
        else
        {
            RecalculateDiameter();
        }

        if (metadata.Parameters.TryGetDouble("CornerRadiusMm", out var cr)) _cornerRadiusMm = cr;
        if (metadata.Parameters.TryGetDouble("SuperellipseExponent", out var se)) _superellipseExponent = se;
        if (metadata.Parameters.TryGetDouble("BypassOverlapDeg", out var bo)) _bypassOverlapDeg = bo;
        if (metadata.Parameters.TryGetDouble("BypassAxialOffsetMm", out var bao)) _bypassAxialOffsetMm = bao;
        if (metadata.Parameters.TryGetDouble("BypassRadialOffsetMm", out var bro)) _bypassRadialOffsetMm = bro;

        UpdatePreview();
    }

    private void RecalculateDiameter()
    {
        try
        {
            _innerDiameterMm = RailEngine.SizeToInnerDiameterMm(_standard, _ringSize, _ukSizeStr);
        }
        catch
        {
            _innerDiameterMm = 17.32; // Fallback US 7
        }
    }
}
