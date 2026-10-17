namespace RadianceStudio.UI.ViewModels;

using System;
using System.Collections.Generic;
using RadianceStudio.Core.Display;
using RadianceStudio.Core.Engines;
using RadianceStudio.Core.Enums;
using RadianceStudio.Core.Graph;
using RadianceStudio.Core.Models;
using RadianceStudio.Core.Services;
using Rhino;
using Rhino.Geometry;
using GemEngine = RadianceStudio.Core.Engines.GemEngine;

/// <summary>
/// Reactive ViewModel for distributing gemstones along 3D space curves.
/// Uses GemEngine to compute orthonormal frames and pushes instanced canonical meshes
/// to the GPU shader memory via RadiancePreviewConduit at 60 FPS.
/// </summary>
public class GemOnCurveViewModel : IInspectorViewModel
{
    private GemCutType _cutType = GemCutType.RoundBrilliant;
    private double _gemDiameterMm = 2.0;
    private double _spacingMm = 0.2;
    private double _startOffsetMm = 0.5;
    private double _endOffsetMm = 0.5;
    private Guid? _targetCurveId;
    private int _stoneCount = 0;
    private readonly GemEngine _gemEngine = new();

    public GemOnCurveViewModel()
    {
    }

    public string Title => IsEditMode ? "Edit Gems on Curve" : "Gems on Curve";
    public string FeatureType => "GemRow";
    public bool IsEditMode { get; private set; }
    public Guid? EditingObjectId { get; private set; }

    public RadiancePreviewConduit? Conduit { get; set; }
    public ParametricGraphManager? GraphManager { get; set; }

    public event EventHandler? PreviewUpdated;

    public GemCutType CutType
    {
        get => _cutType;
        set
        {
            if (_cutType != value)
            {
                _cutType = value;
                UpdatePreview();
            }
        }
    }

    public double GemDiameterMm
    {
        get => _gemDiameterMm;
        set
        {
            if (Math.Abs(_gemDiameterMm - value) > 1e-4 && value > 0)
            {
                _gemDiameterMm = value;
                UpdatePreview();
            }
        }
    }

    public double SpacingMm
    {
        get => _spacingMm;
        set
        {
            if (Math.Abs(_spacingMm - value) > 1e-4 && value >= 0)
            {
                _spacingMm = value;
                UpdatePreview();
            }
        }
    }

    public double StartOffsetMm
    {
        get => _startOffsetMm;
        set
        {
            if (Math.Abs(_startOffsetMm - value) > 1e-4 && value >= 0)
            {
                _startOffsetMm = value;
                UpdatePreview();
            }
        }
    }

    public double EndOffsetMm
    {
        get => _endOffsetMm;
        set
        {
            if (Math.Abs(_endOffsetMm - value) > 1e-4 && value >= 0)
            {
                _endOffsetMm = value;
                UpdatePreview();
            }
        }
    }

    public Guid? TargetCurveId
    {
        get => _targetCurveId;
        set
        {
            if (_targetCurveId != value)
            {
                _targetCurveId = value;
                UpdatePreview();
            }
        }
    }

    public int StoneCount => _stoneCount;

    public void Initialize()
    {
        IsEditMode = false;
        EditingObjectId = null;
        _cutType = GemCutType.RoundBrilliant;
        _gemDiameterMm = 2.0;
        _spacingMm = 0.2;
        _startOffsetMm = 0.5;
        _endOffsetMm = 0.5;
        _targetCurveId = null;
        UpdatePreview();
    }

    public void UpdatePreview()
    {
        if (Conduit != null && RailEngine.IsRhinoNativeAvailable)
        {
            Curve? curve = GetTargetCurve();
            if (curve != null)
            {
                var transforms = _gemEngine.DistributeGemsOnCurve(curve, _gemDiameterMm, _spacingMm, _startOffsetMm, _endOffsetMm);
                _stoneCount = transforms.Count;

                var canonicalGemMesh = GemMeshFactory.CreateCanonical(_cutType);
                var rhinoMesh = MeshConverter.ToRhinoMesh(canonicalGemMesh);

                if (rhinoMesh != null)
                {
                    Conduit.SetInstances(rhinoMesh, transforms);
                    Conduit.Enabled = true;
                }
            }
            else
            {
                _stoneCount = 0;
                Conduit.ClearInstances();
            }
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

        Curve? curve = GetTargetCurve();
        if (curve == null)
        {
            return;
        }

        var transforms = _gemEngine.DistributeGemsOnCurve(curve, _gemDiameterMm, _spacingMm, _startOffsetMm, _endOffsetMm);
        if (transforms.Count == 0)
        {
            return;
        }

        var canonical = GemMeshFactory.CreateCanonical(_cutType);
        var baseMesh = MeshConverter.ToRhinoMesh(canonical);
        if (baseMesh == null)
        {
            return;
        }

        var combinedMesh = new Mesh();
        foreach (var xform in transforms)
        {
            var stoneMesh = baseMesh.DuplicateMesh();
            stoneMesh.Transform(xform);
            combinedMesh.Append(stoneMesh);
        }

        var metadata = ParametricMetadata.Attach(combinedMesh);
        metadata.Parameters.Set("FeatureType", "GemRow");
        metadata.Parameters.Set("CutType", _cutType.ToString());
        metadata.Parameters.Set("GemDiameterMm", _gemDiameterMm);
        metadata.Parameters.Set("SpacingMm", _spacingMm);
        metadata.Parameters.Set("StartOffsetMm", _startOffsetMm);
        metadata.Parameters.Set("EndOffsetMm", _endOffsetMm);
        metadata.Parameters.Set("StoneCount", transforms.Count);
        if (_targetCurveId.HasValue) metadata.Parameters.Set("TargetCurveId", _targetCurveId.Value.ToString());

        Guid objId;
        if (IsEditMode && EditingObjectId.HasValue)
        {
            doc.Objects.Replace(EditingObjectId.Value, combinedMesh);
            objId = EditingObjectId.Value;
        }
        else
        {
            objId = doc.Objects.AddMesh(combinedMesh);
        }

        if (GraphManager != null)
        {
            var node = GraphManager.GetNode(objId) ?? new FeatureNode
            {
                Id = objId,
                Name = $"{_cutType} Row ({transforms.Count} stones)",
                FeatureType = "GemRow"
            };
            if (_targetCurveId.HasValue)
            {
                node.ParentIds.Add(_targetCurveId.Value);
            }
            node.Parameters["CutType"] = _cutType.ToString();
            node.Parameters["StoneCount"] = transforms.Count;
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

        if (metadata.Parameters.TryGetString("CutType", out var cutStr) &&
            Enum.TryParse<GemCutType>(cutStr, out var parsedCut))
        {
            _cutType = parsedCut;
        }

        if (metadata.Parameters.TryGetDouble("GemDiameterMm", out var gd)) _gemDiameterMm = gd;
        if (metadata.Parameters.TryGetDouble("SpacingMm", out var sp)) _spacingMm = sp;
        if (metadata.Parameters.TryGetDouble("StartOffsetMm", out var so)) _startOffsetMm = so;
        if (metadata.Parameters.TryGetDouble("EndOffsetMm", out var eo)) _endOffsetMm = eo;
        if (metadata.Parameters.TryGetString("TargetCurveId", out var cidStr) &&
            Guid.TryParse(cidStr, out var parsedCid))
        {
            _targetCurveId = parsedCid;
        }

        UpdatePreview();
    }

    private Curve? GetTargetCurve()
    {
        if (!RailEngine.IsRhinoNativeAvailable) return null;

        var doc = RhinoDoc.ActiveDoc;
        if (doc != null && _targetCurveId.HasValue)
        {
            var obj = doc.Objects.FindId(_targetCurveId.Value);
            if (obj?.Geometry is Curve userCurve)
            {
                return userCurve;
            }
        }

        // Fallback default rail (US 7 circle) for responsive live preview
        double defaultDiameter = 17.32;
        return RailEngine.CreateRingRail(defaultDiameter, RailProfileType.CanonicalCircle);
    }
}
