namespace RadianceStudio.UI.ViewModels;

using System;
using RadianceStudio.Core.Display;
using RadianceStudio.Core.Engines;
using RadianceStudio.Core.Enums;
using RadianceStudio.Core.Geometry;
using RadianceStudio.Core.Graph;
using RadianceStudio.Core.Models;
using Rhino;
using Rhino.Geometry;

/// <summary>
/// Reactive ViewModel for parametric tapered ring shanks across cross-section profiles
/// (Flat, DShape, ComfortFit, KnifeEdge, Cathedral) with optional under-gallery azurador chambers.
/// </summary>
public class ShankViewModel : IInspectorViewModel
{
    private ShankProfileType _profileType = ShankProfileType.DShape;
    private double _fingerSize = 7.0;
    private double _bottomWidthMm = 2.5;
    private double _bottomThicknessMm = 1.6;
    private double _topWidthMm = 3.5;
    private double _topThicknessMm = 2.2;
    private double _cathedralArchHeightMm = 1.5;
    private double _knifeEdgeApexRadiusMm = 0.3;
    private bool _hasUnderGallery = false;
    private double _underGalleryWallMm = 0.8;

    public ShankViewModel()
    {
    }

    public string Title => IsEditMode ? "Edit Shank" : "Shank Creator";
    public string FeatureType => "Shank";
    public bool IsEditMode { get; private set; }
    public Guid? EditingObjectId { get; private set; }

    public RadiancePreviewConduit? Conduit { get; set; }
    public ParametricGraphManager? GraphManager { get; set; }

    public event EventHandler? PreviewUpdated;

    public ShankProfileType ProfileType
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

    public double FingerSize
    {
        get => _fingerSize;
        set
        {
            if (Math.Abs(_fingerSize - value) > 1e-4)
            {
                _fingerSize = value;
                UpdatePreview();
            }
        }
    }

    public double BottomWidthMm
    {
        get => _bottomWidthMm;
        set
        {
            if (Math.Abs(_bottomWidthMm - value) > 1e-4 && value > 0)
            {
                _bottomWidthMm = value;
                UpdatePreview();
            }
        }
    }

    public double BottomThicknessMm
    {
        get => _bottomThicknessMm;
        set
        {
            if (Math.Abs(_bottomThicknessMm - value) > 1e-4 && value >= ShankEngine.MinimumWallThicknessMm)
            {
                _bottomThicknessMm = value;
                UpdatePreview();
            }
        }
    }

    public double TopWidthMm
    {
        get => _topWidthMm;
        set
        {
            if (Math.Abs(_topWidthMm - value) > 1e-4 && value > 0)
            {
                _topWidthMm = value;
                UpdatePreview();
            }
        }
    }

    public double TopThicknessMm
    {
        get => _topThicknessMm;
        set
        {
            if (Math.Abs(_topThicknessMm - value) > 1e-4 && value >= ShankEngine.MinimumWallThicknessMm)
            {
                _topThicknessMm = value;
                UpdatePreview();
            }
        }
    }

    public double CathedralArchHeightMm
    {
        get => _cathedralArchHeightMm;
        set
        {
            if (Math.Abs(_cathedralArchHeightMm - value) > 1e-4)
            {
                _cathedralArchHeightMm = value;
                UpdatePreview();
            }
        }
    }

    public double KnifeEdgeApexRadiusMm
    {
        get => _knifeEdgeApexRadiusMm;
        set
        {
            if (Math.Abs(_knifeEdgeApexRadiusMm - value) > 1e-4)
            {
                _knifeEdgeApexRadiusMm = value;
                UpdatePreview();
            }
        }
    }

    public bool HasUnderGallery
    {
        get => _hasUnderGallery;
        set
        {
            if (_hasUnderGallery != value)
            {
                _hasUnderGallery = value;
                UpdatePreview();
            }
        }
    }

    public double UnderGalleryWallMm
    {
        get => _underGalleryWallMm;
        set
        {
            if (Math.Abs(_underGalleryWallMm - value) > 1e-4)
            {
                _underGalleryWallMm = value;
                UpdatePreview();
            }
        }
    }

    public void Initialize()
    {
        IsEditMode = false;
        EditingObjectId = null;
        _profileType = ShankProfileType.DShape;
        _fingerSize = 7.0;
        _bottomWidthMm = 2.5;
        _bottomThicknessMm = 1.6;
        _topWidthMm = 3.5;
        _topThicknessMm = 2.2;
        UpdatePreview();
    }

    public void UpdatePreview()
    {
        if (Conduit != null)
        {
            double diameter = RailEngine.SizeToInnerDiameterMm(RingSizingStandard.US, _fingerSize);
            ICurve3D rail3D = new CircleCurve3D(Point3D.Origin, diameter / 2.0);

            UnderGalleryParameters? gallery = _hasUnderGallery
                ? new UnderGalleryParameters(_underGalleryWallMm, 0.85, UnderGalleryPattern.Honeycomb, 60.0)
                : null;

            var shankMesh = ShankEngine.CreateSweptShankMesh(
                rail3D,
                _profileType,
                _bottomWidthMm,
                _bottomThicknessMm,
                _topWidthMm,
                _topThicknessMm,
                gallery,
                knifeEdgeApexRadiusMm: _knifeEdgeApexRadiusMm,
                cathedralArchHeightMm: _cathedralArchHeightMm);

            var rhinoMesh = MeshConverter.ToRhinoMesh(shankMesh);
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

        double diameter = RailEngine.SizeToInnerDiameterMm(RingSizingStandard.US, _fingerSize);
        var railCurve = RailEngine.CreateRingRail(diameter, RailProfileType.CanonicalCircle);
        if (railCurve == null)
        {
            return;
        }

        UnderGalleryParameters? gallery = _hasUnderGallery
            ? new UnderGalleryParameters(_underGalleryWallMm, 0.85, UnderGalleryPattern.Honeycomb, 60.0)
            : null;

        var parameters = new ShankParameters(
            _profileType,
            _bottomWidthMm,
            _bottomThicknessMm,
            _topWidthMm,
            _topThicknessMm,
            gallery,
            knifeEdgeApexRadiusMm: _knifeEdgeApexRadiusMm,
            cathedralArchHeightMm: _cathedralArchHeightMm);

        Brep? brep = ShankEngine.CreateSweptShank(railCurve, parameters);
        GeometryBase bakedGeometry;

        if (brep != null)
        {
            bakedGeometry = brep;
        }
        else
        {
            // Fallback to mesh
            ICurve3D rail3D = new CircleCurve3D(Point3D.Origin, diameter / 2.0);
            var shankMesh = ShankEngine.CreateSweptShankMesh(rail3D, parameters);
            bakedGeometry = MeshConverter.ToRhinoMesh(shankMesh) ?? (GeometryBase)railCurve;
        }

        var metadata = ParametricMetadata.Attach(bakedGeometry);
        metadata.Parameters.Set("FeatureType", "Shank");
        metadata.Parameters.Set("ProfileType", _profileType.ToString());
        metadata.Parameters.Set("FingerSize", _fingerSize);
        metadata.Parameters.Set("BottomWidthMm", _bottomWidthMm);
        metadata.Parameters.Set("BottomThicknessMm", _bottomThicknessMm);
        metadata.Parameters.Set("TopWidthMm", _topWidthMm);
        metadata.Parameters.Set("TopThicknessMm", _topThicknessMm);
        metadata.Parameters.Set("CathedralArchHeightMm", _cathedralArchHeightMm);
        metadata.Parameters.Set("KnifeEdgeApexRadiusMm", _knifeEdgeApexRadiusMm);
        metadata.Parameters.Set("HasUnderGallery", _hasUnderGallery);
        metadata.Parameters.Set("UnderGalleryWallMm", _underGalleryWallMm);

        Guid objId;
        if (IsEditMode && EditingObjectId.HasValue)
        {
            if (bakedGeometry is Brep b) doc.Objects.Replace(EditingObjectId.Value, b);
            else if (bakedGeometry is Mesh m) doc.Objects.Replace(EditingObjectId.Value, m);
            else if (bakedGeometry is Curve c) doc.Objects.Replace(EditingObjectId.Value, c);
            objId = EditingObjectId.Value;
        }
        else
        {
            objId = bakedGeometry switch
            {
                Brep b => doc.Objects.AddBrep(b),
                Mesh m => doc.Objects.AddMesh(m),
                Curve c => doc.Objects.AddCurve(c),
                _ => Guid.Empty
            };
        }

        if (GraphManager != null && objId != Guid.Empty)
        {
            var node = GraphManager.GetNode(objId) ?? new FeatureNode
            {
                Id = objId,
                Name = $"{_profileType} Shank",
                FeatureType = "Shank"
            };
            node.Parameters["ProfileType"] = _profileType.ToString();
            node.Parameters["TopWidthMm"] = _topWidthMm;
            node.Parameters["BottomWidthMm"] = _bottomWidthMm;
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

        if (metadata.Parameters.TryGetString("ProfileType", out var profStr) &&
            Enum.TryParse<ShankProfileType>(profStr, out var parsedProf))
        {
            _profileType = parsedProf;
        }

        if (metadata.Parameters.TryGetDouble("FingerSize", out var fs)) _fingerSize = fs;
        if (metadata.Parameters.TryGetDouble("BottomWidthMm", out var bw)) _bottomWidthMm = bw;
        if (metadata.Parameters.TryGetDouble("BottomThicknessMm", out var bt)) _bottomThicknessMm = bt;
        if (metadata.Parameters.TryGetDouble("TopWidthMm", out var tw)) _topWidthMm = tw;
        if (metadata.Parameters.TryGetDouble("TopThicknessMm", out var tt)) _topThicknessMm = tt;
        if (metadata.Parameters.TryGetDouble("CathedralArchHeightMm", out var cah)) _cathedralArchHeightMm = cah;
        if (metadata.Parameters.TryGetDouble("KnifeEdgeApexRadiusMm", out var kar)) _knifeEdgeApexRadiusMm = kar;
        if (metadata.Parameters.TryGetBool("HasUnderGallery", out var hug)) _hasUnderGallery = hug;
        if (metadata.Parameters.TryGetDouble("UnderGalleryWallMm", out var ugw)) _underGalleryWallMm = ugw;

        UpdatePreview();
    }
}
