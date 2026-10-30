namespace RadianceStudio.Core.Display;

using System.Collections.Generic;
using System.Drawing;
using Rhino.Display;
using Rhino.Geometry;

/// <summary>
/// High-performance 60 FPS viewport preview conduit.
/// Instantiates canonical meshes in GPU shader memory using 4x4 Transform matrices
/// and renders anti-aliased CAD preview curves without adding Breps to RhinoDoc.Objects or cluttering the Undo stack.
/// </summary>
public class RadiancePreviewConduit : DisplayConduit
{
    private Mesh? _canonicalMesh;
    private readonly List<Transform> _instanceTransforms = new();
    private DisplayMaterial _material;

    private Curve? _previewCurve;
    private readonly List<Curve> _previewCurves = new();
    private Color _curveColor = Color.FromArgb(0, 180, 255);
    private int _curveThickness = 2;

    public RadiancePreviewConduit()
    {
        _material = new DisplayMaterial(Color.FromArgb(220, 240, 255), 0.15)
        {
            Specular = Color.White,
            Shine = 90.0
        };
    }

    public Mesh? CanonicalMesh
    {
        get => _canonicalMesh;
        set => _canonicalMesh = value;
    }

    public DisplayMaterial Material
    {
        get => _material;
        set => _material = value;
    }

    public IReadOnlyList<Transform> InstanceTransforms => _instanceTransforms;

    public void SetInstances(Mesh canonicalMesh, IEnumerable<Transform> transforms)
    {
        _canonicalMesh = canonicalMesh;
        _instanceTransforms.Clear();
        _instanceTransforms.AddRange(transforms);
    }

    public void ClearInstances()
    {
        _instanceTransforms.Clear();
    }

    public void SetCurve(Curve? curve, Color? color = null, int thickness = 2)
    {
        _previewCurve = curve;
        _previewCurves.Clear();
        if (color.HasValue) _curveColor = color.Value;
        _curveThickness = thickness;
    }

    public void SetCurves(IEnumerable<Curve> curves, Color? color = null, int thickness = 2)
    {
        _previewCurve = null;
        _previewCurves.Clear();
        if (curves != null) _previewCurves.AddRange(curves);
        if (color.HasValue) _curveColor = color.Value;
        _curveThickness = thickness;
    }

    public void ClearCurves()
    {
        _previewCurve = null;
        _previewCurves.Clear();
    }

    public void ClearAll()
    {
        ClearInstances();
        ClearCurves();
    }

    protected override void CalculateBoundingBox(CalculateBoundingBoxEventArgs e)
    {
        if (_canonicalMesh != null && _instanceTransforms.Count > 0)
        {
            var baseBox = _canonicalMesh.GetBoundingBox(true);
            foreach (var xform in _instanceTransforms)
            {
                var transformedBox = baseBox;
                transformedBox.Transform(xform);
                e.IncludeBoundingBox(transformedBox);
            }
        }

        if (_previewCurve != null)
        {
            e.IncludeBoundingBox(_previewCurve.GetBoundingBox(true));
        }

        foreach (var c in _previewCurves)
        {
            if (c != null)
            {
                e.IncludeBoundingBox(c.GetBoundingBox(true));
            }
        }
    }

    protected override void PostDrawObjects(DrawEventArgs e)
    {
        if (_canonicalMesh != null && _instanceTransforms.Count > 0)
        {
            foreach (var xform in _instanceTransforms)
            {
                e.Display.PushModelTransform(xform);
                e.Display.DrawMeshShaded(_canonicalMesh, _material);
                e.Display.PopModelTransform();
            }
        }

        if (_previewCurve != null)
        {
            e.Display.DrawCurve(_previewCurve, _curveColor, _curveThickness);
        }

        foreach (var c in _previewCurves)
        {
            if (c != null)
            {
                e.Display.DrawCurve(c, _curveColor, _curveThickness);
            }
        }
    }
}
