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
/// Reactive ViewModel for parametric full and semi-bezel settings across all 13 canonical gem cuts,
/// featuring customizable wall thicknesses, draft angles, seat depths, and milgrain decoration.
/// </summary>
public class BezelViewModel : IInspectorViewModel
{
    private GemCutType _gemCut = GemCutType.RoundBrilliant;
    private double _gemDiameterMm = 6.5;
    private double _wallThicknessMm = 0.8;
    private double _heightAboveGirdleMm = 0.4;
    private double _seatDepthPercent = 30.0;
    private double _draftAngleDeg = 5.0;
    private double _lightHoleDiameterRatio = 0.5;
    private bool _isSemiBezel = false;
    private double _semiBezelOpeningAngleDeg = 60.0;
    private bool _includeMilgrain = false;

    public BezelViewModel()
    {
    }

    public string Title => IsEditMode ? "Edit Bezel Setting" : "Bezel Setting Creator";
    public string FeatureType => "Setting";
    public bool IsEditMode { get; private set; }
    public Guid? EditingObjectId { get; private set; }

    public RadiancePreviewConduit? Conduit { get; set; }
    public ParametricGraphManager? GraphManager { get; set; }

    public event EventHandler? PreviewUpdated;

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

    public double WallThicknessMm
    {
        get => _wallThicknessMm;
        set
        {
            if (Math.Abs(_wallThicknessMm - value) > 1e-4 && value >= 0.4)
            {
                _wallThicknessMm = value;
                UpdatePreview();
            }
        }
    }

    public double HeightAboveGirdleMm
    {
        get => _heightAboveGirdleMm;
        set
        {
            if (Math.Abs(_heightAboveGirdleMm - value) > 1e-4 && value > 0)
            {
                _heightAboveGirdleMm = value;
                UpdatePreview();
            }
        }
    }

    public double SeatDepthPercent
    {
        get => _seatDepthPercent;
        set
        {
            if (Math.Abs(_seatDepthPercent - value) > 1e-4 && value > 0 && value <= 100)
            {
                _seatDepthPercent = value;
                UpdatePreview();
            }
        }
    }

    public double DraftAngleDeg
    {
        get => _draftAngleDeg;
        set
        {
            if (Math.Abs(_draftAngleDeg - value) > 1e-4 && value >= 0 && value <= 45)
            {
                _draftAngleDeg = value;
                UpdatePreview();
            }
        }
    }

    public double LightHoleDiameterRatio
    {
        get => _lightHoleDiameterRatio;
        set
        {
            if (Math.Abs(_lightHoleDiameterRatio - value) > 1e-4 && value > 0.05 && value < 0.95)
            {
                _lightHoleDiameterRatio = value;
                UpdatePreview();
            }
        }
    }

    public bool IsSemiBezel
    {
        get => _isSemiBezel;
        set
        {
            if (_isSemiBezel != value)
            {
                _isSemiBezel = value;
                UpdatePreview();
            }
        }
    }

    public double SemiBezelOpeningAngleDeg
    {
        get => _semiBezelOpeningAngleDeg;
        set
        {
            if (Math.Abs(_semiBezelOpeningAngleDeg - value) > 1e-4 && value > 0 && value < 180)
            {
                _semiBezelOpeningAngleDeg = value;
                UpdatePreview();
            }
        }
    }

    public bool IncludeMilgrain
    {
        get => _includeMilgrain;
        set
        {
            if (_includeMilgrain != value)
            {
                _includeMilgrain = value;
                UpdatePreview();
            }
        }
    }

    public void Initialize()
    {
        IsEditMode = false;
        EditingObjectId = null;
        _gemCut = GemCutType.RoundBrilliant;
        _gemDiameterMm = 6.5;
        _wallThicknessMm = 0.8;
        _heightAboveGirdleMm = 0.4;
        _seatDepthPercent = 30.0;
        _draftAngleDeg = 5.0;
        _lightHoleDiameterRatio = 0.5;
        _isSemiBezel = false;
        _semiBezelOpeningAngleDeg = 60.0;
        _includeMilgrain = false;
        UpdatePreview();
    }

    public void UpdatePreview()
    {
        if (Conduit != null)
        {
            var gem = GemDimensions.GetDefault(_gemCut, _gemDiameterMm);
            var parameters = new BezelParameters
            {
                WallThicknessMm = _wallThicknessMm,
                HeightAboveGirdleMm = _heightAboveGirdleMm,
                SeatDepthPercent = _seatDepthPercent,
                DraftAngleDeg = _draftAngleDeg,
                LightHoleDiameterRatio = _lightHoleDiameterRatio,
                IsSemiBezel = _isSemiBezel,
                SemiBezelOpeningAngleDeg = _semiBezelOpeningAngleDeg,
                IncludeMilgrain = _includeMilgrain
            };

            var bezelMesh = BezelEngine.CreateBezelMesh(gem, parameters);
            var rhinoMesh = MeshConverter.ToRhinoMesh(bezelMesh);

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
        var parameters = new BezelParameters
        {
            WallThicknessMm = _wallThicknessMm,
            HeightAboveGirdleMm = _heightAboveGirdleMm,
            SeatDepthPercent = _seatDepthPercent,
            DraftAngleDeg = _draftAngleDeg,
            LightHoleDiameterRatio = _lightHoleDiameterRatio,
            IsSemiBezel = _isSemiBezel,
            SemiBezelOpeningAngleDeg = _semiBezelOpeningAngleDeg,
            IncludeMilgrain = _includeMilgrain
        };

        Brep? brep = BezelEngine.CreateBezelBrep(gem, parameters);
        GeometryBase baked;
        if (brep != null)
        {
            baked = brep;
        }
        else
        {
            var mesh = BezelEngine.CreateBezelMesh(gem, parameters);
            baked = MeshConverter.ToRhinoMesh(mesh) ?? (GeometryBase)new Mesh();
        }

        var metadata = ParametricMetadata.Attach(baked);
        metadata.Parameters.Set("FeatureType", "Setting");
        metadata.Parameters.Set("SettingType", "Bezel");
        metadata.Parameters.Set("GemCut", _gemCut.ToString());
        metadata.Parameters.Set("GemDiameterMm", _gemDiameterMm);
        metadata.Parameters.Set("WallThicknessMm", _wallThicknessMm);
        metadata.Parameters.Set("HeightAboveGirdleMm", _heightAboveGirdleMm);
        metadata.Parameters.Set("SeatDepthPercent", _seatDepthPercent);
        metadata.Parameters.Set("DraftAngleDeg", _draftAngleDeg);
        metadata.Parameters.Set("LightHoleDiameterRatio", _lightHoleDiameterRatio);
        metadata.Parameters.Set("IsSemiBezel", _isSemiBezel);
        metadata.Parameters.Set("SemiBezelOpeningAngleDeg", _semiBezelOpeningAngleDeg);
        metadata.Parameters.Set("IncludeMilgrain", _includeMilgrain);

        Guid objId;
        if (IsEditMode && EditingObjectId.HasValue)
        {
            if (baked is Brep b) doc.Objects.Replace(EditingObjectId.Value, b);
            else if (baked is Mesh m) doc.Objects.Replace(EditingObjectId.Value, m);
            objId = EditingObjectId.Value;
        }
        else
        {
            objId = baked switch
            {
                Brep b => doc.Objects.AddBrep(b),
                Mesh m => doc.Objects.AddMesh(m),
                _ => Guid.Empty
            };
        }

        if (GraphManager != null && objId != Guid.Empty)
        {
            string label = _isSemiBezel ? $"Semi-Bezel ({_gemCut})" : $"Bezel ({_gemCut})";
            var node = GraphManager.GetNode(objId) ?? new FeatureNode
            {
                Id = objId,
                Name = label,
                FeatureType = "Setting"
            };
            node.Parameters["GemCut"] = _gemCut.ToString();
            node.Parameters["IsSemiBezel"] = _isSemiBezel;
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

        if (metadata.Parameters.TryGetString("GemCut", out var gcStr) &&
            Enum.TryParse<GemCutType>(gcStr, out var parsedCut))
        {
            _gemCut = parsedCut;
        }
        if (metadata.Parameters.TryGetDouble("GemDiameterMm", out var gd)) _gemDiameterMm = gd;
        if (metadata.Parameters.TryGetDouble("WallThicknessMm", out var wt)) _wallThicknessMm = wt;
        if (metadata.Parameters.TryGetDouble("HeightAboveGirdleMm", out var hg)) _heightAboveGirdleMm = hg;
        if (metadata.Parameters.TryGetDouble("SeatDepthPercent", out var sd)) _seatDepthPercent = sd;
        if (metadata.Parameters.TryGetDouble("DraftAngleDeg", out var da)) _draftAngleDeg = da;
        if (metadata.Parameters.TryGetDouble("LightHoleDiameterRatio", out var lh)) _lightHoleDiameterRatio = lh;
        if (metadata.Parameters.TryGetBool("IsSemiBezel", out var isSemi)) _isSemiBezel = isSemi;
        if (metadata.Parameters.TryGetDouble("SemiBezelOpeningAngleDeg", out var sbo)) _semiBezelOpeningAngleDeg = sbo;
        if (metadata.Parameters.TryGetBool("IncludeMilgrain", out var im)) _includeMilgrain = im;

        UpdatePreview();
    }
}
