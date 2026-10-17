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
/// Reactive ViewModel for parametric 4, 6, and 8-prong setting heads,
/// V-prongs, shared prongs, and structural gallery wires.
/// </summary>
public class ProngHeadViewModel : IInspectorViewModel
{
    private int _prongCount = 4;
    private ProngStyle _style = ProngStyle.RoundDome;
    private double _prongDiameterMm = 0.85;
    private double _prongHeightMm = 4.0;
    private double _rakeAngleDeg = 10.0;
    private double _girdleOverhangMm = 0.15;
    private GemCutType _gemCut = GemCutType.RoundBrilliant;
    private double _gemDiameterMm = 6.5;
    private bool _includeBaseWire = true;
    private bool _includeGalleryWire = false;

    public ProngHeadViewModel()
    {
    }

    public string Title => IsEditMode ? "Edit Prong Head" : "Prong Head Creator";
    public string FeatureType => "Setting";
    public bool IsEditMode { get; private set; }
    public Guid? EditingObjectId { get; private set; }

    public RadiancePreviewConduit? Conduit { get; set; }
    public ParametricGraphManager? GraphManager { get; set; }

    public event EventHandler? PreviewUpdated;

    public int ProngCount
    {
        get => _prongCount;
        set
        {
            if (_prongCount != value && (value == 4 || value == 6 || value == 8))
            {
                _prongCount = value;
                UpdatePreview();
            }
        }
    }

    public ProngStyle Style
    {
        get => _style;
        set
        {
            if (_style != value)
            {
                _style = value;
                UpdatePreview();
            }
        }
    }

    public double ProngDiameterMm
    {
        get => _prongDiameterMm;
        set
        {
            if (Math.Abs(_prongDiameterMm - value) > 1e-4 && value > 0)
            {
                _prongDiameterMm = value;
                UpdatePreview();
            }
        }
    }

    public double ProngHeightMm
    {
        get => _prongHeightMm;
        set
        {
            if (Math.Abs(_prongHeightMm - value) > 1e-4 && value > 0)
            {
                _prongHeightMm = value;
                UpdatePreview();
            }
        }
    }

    public double RakeAngleDeg
    {
        get => _rakeAngleDeg;
        set
        {
            if (Math.Abs(_rakeAngleDeg - value) > 1e-4 && value >= 0 && value <= 45)
            {
                _rakeAngleDeg = value;
                UpdatePreview();
            }
        }
    }

    public double GirdleOverhangMm
    {
        get => _girdleOverhangMm;
        set
        {
            if (Math.Abs(_girdleOverhangMm - value) > 1e-4 && value >= 0.05 && value <= 0.40)
            {
                _girdleOverhangMm = value;
                UpdatePreview();
            }
        }
    }

    public GemCutType GemCut
    {
        get => _gemCut;
        set
        {
            if (_gemCut != value)
            {
                _gemCut = value;
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

    public bool IncludeBaseWire
    {
        get => _includeBaseWire;
        set
        {
            if (_includeBaseWire != value)
            {
                _includeBaseWire = value;
                UpdatePreview();
            }
        }
    }

    public bool IncludeGalleryWire
    {
        get => _includeGalleryWire;
        set
        {
            if (_includeGalleryWire != value)
            {
                _includeGalleryWire = value;
                UpdatePreview();
            }
        }
    }

    public void Initialize()
    {
        IsEditMode = false;
        EditingObjectId = null;
        _prongCount = 4;
        _style = ProngStyle.RoundDome;
        _prongDiameterMm = 0.85;
        _prongHeightMm = 4.0;
        _rakeAngleDeg = 10.0;
        _girdleOverhangMm = 0.15;
        _gemCut = GemCutType.RoundBrilliant;
        _gemDiameterMm = 6.5;
        _includeBaseWire = true;
        _includeGalleryWire = false;
        UpdatePreview();
    }

    public void UpdatePreview()
    {
        if (Conduit != null)
        {
            var gem = GemDimensions.GetDefault(_gemCut, _gemDiameterMm);
            var parameters = new ProngParameters
            {
                ProngCount = _prongCount,
                ProngDiameterMm = _prongDiameterMm,
                ProngHeightMm = _prongHeightMm,
                GirdleOverhangMm = _girdleOverhangMm,
                RakeAngleDeg = _rakeAngleDeg,
                Style = _style,
                IncludeBaseWire = _includeBaseWire,
                IncludeGalleryWire = _includeGalleryWire
            };

            var headMesh = ProngEngine.CreateProngHeadMesh(gem, parameters);
            var rhinoMesh = MeshConverter.ToRhinoMesh(headMesh);

            if (rhinoMesh != null)
            {
                Conduit.SetInstances(rhinoMesh, new[] { Transform.Identity });
                Conduit.Enabled = true;
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

        var gem = GemDimensions.GetDefault(_gemCut, _gemDiameterMm);
        var parameters = new ProngParameters
        {
            ProngCount = _prongCount,
            ProngDiameterMm = _prongDiameterMm,
            ProngHeightMm = _prongHeightMm,
            GirdleOverhangMm = _girdleOverhangMm,
            RakeAngleDeg = _rakeAngleDeg,
            Style = _style,
            IncludeBaseWire = _includeBaseWire,
            IncludeGalleryWire = _includeGalleryWire
        };

        var headMesh = ProngEngine.CreateProngHeadMesh(gem, parameters);
        var rhinoMesh = MeshConverter.ToRhinoMesh(headMesh);
        if (rhinoMesh == null)
        {
            return;
        }

        var metadata = ParametricMetadata.Attach(rhinoMesh);
        metadata.Parameters.Set("FeatureType", "Setting");
        metadata.Parameters.Set("SettingType", "ProngHead");
        metadata.Parameters.Set("ProngCount", _prongCount);
        metadata.Parameters.Set("Style", _style.ToString());
        metadata.Parameters.Set("ProngDiameterMm", _prongDiameterMm);
        metadata.Parameters.Set("ProngHeightMm", _prongHeightMm);
        metadata.Parameters.Set("RakeAngleDeg", _rakeAngleDeg);
        metadata.Parameters.Set("GirdleOverhangMm", _girdleOverhangMm);
        metadata.Parameters.Set("GemCut", _gemCut.ToString());
        metadata.Parameters.Set("GemDiameterMm", _gemDiameterMm);
        metadata.Parameters.Set("IncludeBaseWire", _includeBaseWire);
        metadata.Parameters.Set("IncludeGalleryWire", _includeGalleryWire);

        Guid objId;
        if (IsEditMode && EditingObjectId.HasValue)
        {
            doc.Objects.Replace(EditingObjectId.Value, rhinoMesh);
            objId = EditingObjectId.Value;
        }
        else
        {
            objId = doc.Objects.AddMesh(rhinoMesh);
        }

        if (GraphManager != null)
        {
            var node = GraphManager.GetNode(objId) ?? new FeatureNode
            {
                Id = objId,
                Name = $"{_prongCount}-Prong Head ({_gemCut})",
                FeatureType = "Setting"
            };
            node.Parameters["ProngCount"] = _prongCount;
            node.Parameters["GemCut"] = _gemCut.ToString();
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

        if (metadata.Parameters.TryGetInteger("ProngCount", out var pc)) _prongCount = pc;
        if (metadata.Parameters.TryGetString("Style", out var stStr) &&
            Enum.TryParse<ProngStyle>(stStr, out var parsedStyle))
        {
            _style = parsedStyle;
        }
        if (metadata.Parameters.TryGetDouble("ProngDiameterMm", out var pd)) _prongDiameterMm = pd;
        if (metadata.Parameters.TryGetDouble("ProngHeightMm", out var ph)) _prongHeightMm = ph;
        if (metadata.Parameters.TryGetDouble("RakeAngleDeg", out var ra)) _rakeAngleDeg = ra;
        if (metadata.Parameters.TryGetDouble("GirdleOverhangMm", out var go)) _girdleOverhangMm = go;
        if (metadata.Parameters.TryGetString("GemCut", out var gcStr) &&
            Enum.TryParse<GemCutType>(gcStr, out var parsedCut))
        {
            _gemCut = parsedCut;
        }
        if (metadata.Parameters.TryGetDouble("GemDiameterMm", out var gd)) _gemDiameterMm = gd;
        if (metadata.Parameters.TryGetBool("IncludeBaseWire", out var ibw)) _includeBaseWire = ibw;
        if (metadata.Parameters.TryGetBool("IncludeGalleryWire", out var igw)) _includeGalleryWire = igw;

        UpdatePreview();
    }
}
