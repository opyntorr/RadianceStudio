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
/// Reactive ViewModel for parametric stone seat cutters, light hole punches,
/// and automated boolean difference subtraction against jewelry metal solids.
/// </summary>
public class CutterViewModel : IInspectorViewModel
{
    private GemCutType _gemCut = GemCutType.RoundBrilliant;
    private double _gemDiameterMm = 2.0;
    private double _girdleClearanceMm = 0.08;
    private CutterSeatStyle _seatAngle = CutterSeatStyle.Degree70;
    private double _crownHeightExtensionMm = 1.5;
    private double _pavilionOffsetPercent = 5.0;
    private double _lightHoleDiameterRatio = 0.50;
    private double _lightHoleDepthExtensionMm = 2.0;
    private bool _taperShaft = false;
    private Guid? _targetMetalId;

    public CutterViewModel()
    {
    }

    public string Title => IsEditMode ? "Edit Cutter" : "Gem Cutter & Booleans";
    public string FeatureType => "Cutter";
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

    public double GirdleClearanceMm
    {
        get => _girdleClearanceMm;
        set
        {
            if (Math.Abs(_girdleClearanceMm - value) > 1e-4 && value >= 0.02 && value <= 0.25)
            {
                _girdleClearanceMm = value;
                UpdatePreview();
            }
        }
    }

    public CutterSeatStyle SeatAngle
    {
        get => _seatAngle;
        set
        {
            if (_seatAngle != value)
            {
                _seatAngle = value;
                UpdatePreview();
            }
        }
    }

    public double CrownHeightExtensionMm
    {
        get => _crownHeightExtensionMm;
        set
        {
            if (Math.Abs(_crownHeightExtensionMm - value) > 1e-4 && value > 0)
            {
                _crownHeightExtensionMm = value;
                UpdatePreview();
            }
        }
    }

    public double PavilionOffsetPercent
    {
        get => _pavilionOffsetPercent;
        set
        {
            if (Math.Abs(_pavilionOffsetPercent - value) > 1e-4 && value >= 0 && value <= 100)
            {
                _pavilionOffsetPercent = value;
                UpdatePreview();
            }
        }
    }

    public double LightHoleDiameterRatio
    {
        get => _lightHoleDiameterRatio;
        set
        {
            if (Math.Abs(_lightHoleDiameterRatio - value) > 1e-4 && value >= 0.20 && value <= 0.80)
            {
                _lightHoleDiameterRatio = value;
                UpdatePreview();
            }
        }
    }

    public double LightHoleDepthExtensionMm
    {
        get => _lightHoleDepthExtensionMm;
        set
        {
            if (Math.Abs(_lightHoleDepthExtensionMm - value) > 1e-4 && value > 0)
            {
                _lightHoleDepthExtensionMm = value;
                UpdatePreview();
            }
        }
    }

    public bool TaperShaft
    {
        get => _taperShaft;
        set
        {
            if (_taperShaft != value)
            {
                _taperShaft = value;
                UpdatePreview();
            }
        }
    }

    public Guid? TargetMetalId
    {
        get => _targetMetalId;
        set
        {
            if (_targetMetalId != value)
            {
                _targetMetalId = value;
                UpdatePreview();
            }
        }
    }

    public void Initialize()
    {
        IsEditMode = false;
        EditingObjectId = null;
        _gemCut = GemCutType.RoundBrilliant;
        _gemDiameterMm = 2.0;
        _girdleClearanceMm = 0.08;
        _seatAngle = CutterSeatStyle.Degree70;
        _crownHeightExtensionMm = 1.5;
        _pavilionOffsetPercent = 5.0;
        _lightHoleDiameterRatio = 0.50;
        _lightHoleDepthExtensionMm = 2.0;
        _taperShaft = false;
        _targetMetalId = null;
        UpdatePreview();
    }

    public void UpdatePreview()
    {
        if (Conduit != null)
        {
            var gem = GemDimensions.GetDefault(_gemCut, _gemDiameterMm);
            var parameters = new CutterParameters
            {
                GirdleClearanceMm = _girdleClearanceMm,
                SeatAngle = _seatAngle,
                CrownHeightExtensionMm = _crownHeightExtensionMm,
                PavilionOffsetPercent = _pavilionOffsetPercent,
                LightHoleDiameterRatio = _lightHoleDiameterRatio,
                LightHoleDepthExtensionMm = _lightHoleDepthExtensionMm,
                TaperShaft = _taperShaft
            };

            var cutterMesh = CutterEngine.CreateGemCutterMesh(gem, parameters);
            var rhinoMesh = MeshConverter.ToRhinoMesh(cutterMesh);

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
        var parameters = new CutterParameters
        {
            GirdleClearanceMm = _girdleClearanceMm,
            SeatAngle = _seatAngle,
            CrownHeightExtensionMm = _crownHeightExtensionMm,
            PavilionOffsetPercent = _pavilionOffsetPercent,
            LightHoleDiameterRatio = _lightHoleDiameterRatio,
            LightHoleDepthExtensionMm = _lightHoleDepthExtensionMm,
            TaperShaft = _taperShaft
        };

        Brep? cutterBrep = CutterEngine.CreateGemCutterBrep(gem, parameters);

        // If target metal is specified, execute boolean difference
        if (_targetMetalId.HasValue && cutterBrep != null)
        {
            var metalObj = doc.Objects.FindId(_targetMetalId.Value);
            if (metalObj?.Geometry is Brep metalBrep)
            {
                var carved = CutterEngine.ApplyCutterDifference(metalBrep, cutterBrep, doc.ModelAbsoluteTolerance);
                if (carved != null)
                {
                    doc.Objects.Replace(_targetMetalId.Value, carved);
                    doc.Views.Redraw();
                    return;
                }
            }
        }

        // Otherwise bake the cutter solid
        GeometryBase baked;
        if (cutterBrep != null)
        {
            baked = cutterBrep;
        }
        else
        {
            var mesh = CutterEngine.CreateGemCutterMesh(gem, parameters);
            baked = MeshConverter.ToRhinoMesh(mesh) ?? (GeometryBase)new Mesh();
        }

        var metadata = ParametricMetadata.Attach(baked);
        metadata.Parameters.Set("FeatureType", "Cutter");
        metadata.Parameters.Set("GemCut", _gemCut.ToString());
        metadata.Parameters.Set("GemDiameterMm", _gemDiameterMm);
        metadata.Parameters.Set("GirdleClearanceMm", _girdleClearanceMm);
        metadata.Parameters.Set("SeatAngle", _seatAngle.ToString());
        metadata.Parameters.Set("CrownHeightExtensionMm", _crownHeightExtensionMm);
        metadata.Parameters.Set("PavilionOffsetPercent", _pavilionOffsetPercent);
        metadata.Parameters.Set("LightHoleDiameterRatio", _lightHoleDiameterRatio);
        metadata.Parameters.Set("LightHoleDepthExtensionMm", _lightHoleDepthExtensionMm);
        metadata.Parameters.Set("TaperShaft", _taperShaft);
        if (_targetMetalId.HasValue) metadata.Parameters.Set("TargetMetalId", _targetMetalId.Value.ToString());

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
            var node = GraphManager.GetNode(objId) ?? new FeatureNode
            {
                Id = objId,
                Name = $"Cutter ({_gemCut})",
                FeatureType = "Cutter"
            };
            node.Parameters["GemCut"] = _gemCut.ToString();
            node.Parameters["SeatAngle"] = _seatAngle.ToString();
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
        if (metadata.Parameters.TryGetDouble("GirdleClearanceMm", out var gc)) _girdleClearanceMm = gc;
        if (metadata.Parameters.TryGetString("SeatAngle", out var saStr) &&
            Enum.TryParse<CutterSeatStyle>(saStr, out var parsedSeat))
        {
            _seatAngle = parsedSeat;
        }
        if (metadata.Parameters.TryGetDouble("CrownHeightExtensionMm", out var ch)) _crownHeightExtensionMm = ch;
        if (metadata.Parameters.TryGetDouble("PavilionOffsetPercent", out var po)) _pavilionOffsetPercent = po;
        if (metadata.Parameters.TryGetDouble("LightHoleDiameterRatio", out var lh)) _lightHoleDiameterRatio = lh;
        if (metadata.Parameters.TryGetDouble("LightHoleDepthExtensionMm", out var lhd)) _lightHoleDepthExtensionMm = lhd;
        if (metadata.Parameters.TryGetBool("TaperShaft", out var ts)) _taperShaft = ts;
        if (metadata.Parameters.TryGetString("TargetMetalId", out var tmStr) &&
            Guid.TryParse(tmStr, out var parsedMid))
        {
            _targetMetalId = parsedMid;
        }

        UpdatePreview();
    }
}
