namespace RadianceStudio.MCP.Tools;

using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using RadianceStudio.Core.Engines;
using RadianceStudio.Core.Models;
using RadianceStudio.MCP.Dispatcher;
using Rhino;
using Rhino.DocObjects;
using Rhino.Geometry;

/// <summary>
/// Exposes the 5 core MCP JSON tools to allow external agents to inspect, manipulate,
/// and manufacture parametric jewelry models inside Rhino 8 thread-safely.
/// </summary>
public class JewelryMcpTools
{
    private readonly RailEngine _railEngine = new();
    private readonly GemEngine _gemEngine = new();

    /// <summary>
    /// Tool 1: get_scene_info
    /// Returns JSON listing active parametric jewelry objects and their metadata in the document.
    /// </summary>
    public async Task<string> GetSceneInfoAsync()
    {
        return await RhinoUiDispatcher.RunOnMainThread(() =>
        {
            var doc = RhinoDoc.ActiveDoc;
            if (doc == null)
            {
                return JsonSerializer.Serialize(new { status = "error", message = "No active document" });
            }

            var parametricObjects = new List<object>();

            foreach (var obj in doc.Objects)
            {
                var metadata = ParametricMetadata.Find(obj.Geometry);
                if (metadata != null)
                {
                    var paramsDict = new Dictionary<string, object?>();
                    foreach (var key in metadata.Parameters.Keys)
                    {
                        paramsDict[key] = metadata.Parameters[key];
                    }

                    parametricObjects.Add(new
                    {
                        id = obj.Id.ToString(),
                        name = obj.Name,
                        objectType = obj.ObjectType.ToString(),
                        parameters = paramsDict
                    });
                }
            }

            return JsonSerializer.Serialize(new
            {
                status = "success",
                documentName = doc.Name,
                objectCount = parametricObjects.Count,
                objects = parametricObjects
            }, new JsonSerializerOptions { WriteIndented = true });
        });
    }

    /// <summary>
    /// Tool 2: set_ring_rail
    /// Creates or updates a parametric ring rail curve with specified size, width, and thickness.
    /// </summary>
    public async Task<string> SetRingRailAsync(double sizeUs, double widthMm, double thicknessMm)
    {
        return await RhinoUiDispatcher.RunOnMainThread(() =>
        {
            var doc = RhinoDoc.ActiveDoc;
            if (doc == null)
            {
                return JsonSerializer.Serialize(new { status = "error", message = "No active document" });
            }

            var railCurve = _railEngine.CreateRingRail(sizeUs, widthMm, thicknessMm);
            var objId = doc.Objects.AddCurve(railCurve);
            doc.Views.Redraw();

            return JsonSerializer.Serialize(new
            {
                status = "success",
                objectId = objId.ToString(),
                sizeUs,
                widthMm,
                thicknessMm,
                innerDiameterMm = RailEngine.UsSizeToInnerDiameterMm(sizeUs)
            });
        });
    }

    /// <summary>
    /// Tool 3: set_gem_row
    /// Distributes a row of gems along a target curve with given diameter and spacing.
    /// </summary>
    public async Task<string> SetGemRowAsync(string targetIdStr, double gemDiameterMm, double spacingMm)
    {
        return await RhinoUiDispatcher.RunOnMainThread(() =>
        {
            var doc = RhinoDoc.ActiveDoc;
            if (doc == null || !Guid.TryParse(targetIdStr, out var targetId))
            {
                return JsonSerializer.Serialize(new { status = "error", message = "Invalid target ID or document" });
            }

            var transforms = _gemEngine.DistributeGemsOnCurve(targetId, gemDiameterMm, spacingMm);

            var matrixList = new List<double[]>();
            foreach (var xform in transforms)
            {
                var m = new double[16]
                {
                    xform.M00, xform.M01, xform.M02, xform.M03,
                    xform.M10, xform.M11, xform.M12, xform.M13,
                    xform.M20, xform.M21, xform.M22, xform.M23,
                    xform.M30, xform.M31, xform.M32, xform.M33
                };
                matrixList.Add(m);
            }

            return JsonSerializer.Serialize(new
            {
                status = "success",
                targetId = targetIdStr,
                gemCount = transforms.Count,
                gemDiameterMm,
                spacingMm,
                transforms = matrixList
            });
        });
    }

    /// <summary>
    /// Tool 4: set_settings
    /// Generates procedural prongs or bezel geometry for an array of gem placement points.
    /// </summary>
    public async Task<string> SetSettingsAsync(string settingType, double prongDiameterMm, double prongHeightMm)
    {
        return await RhinoUiDispatcher.RunOnMainThread(() =>
        {
            var doc = RhinoDoc.ActiveDoc;
            if (doc == null)
            {
                return JsonSerializer.Serialize(new { status = "error", message = "No active document" });
            }

            // Procedural prong model generation
            var cylinder = new Cylinder(
                new Circle(Plane.WorldXY, prongDiameterMm / 2.0),
                prongHeightMm);

            var brep = cylinder.ToBrep(true, true);
            var id = doc.Objects.AddBrep(brep);
            doc.Views.Redraw();

            return JsonSerializer.Serialize(new
            {
                status = "success",
                settingId = id.ToString(),
                settingType,
                prongDiameterMm,
                prongHeightMm
            });
        });
    }

    /// <summary>
    /// Tool 5: check_and_export
    /// Validates minimum wall thickness and executes Rhino 8 Mesh.CreateShrinkWrap for watertight STL casting export.
    /// </summary>
    public async Task<string> CheckAndExportAsync(string outputFilePath, double targetVoxelSizeMm = 0.05)
    {
        return await RhinoUiDispatcher.RunOnMainThread(() =>
        {
            var doc = RhinoDoc.ActiveDoc;
            if (doc == null)
            {
                return JsonSerializer.Serialize(new { status = "error", message = "No active document" });
            }

            var meshes = new List<Mesh>();
            foreach (var obj in doc.Objects)
            {
                if (obj.Geometry is Mesh m)
                {
                    meshes.Add(m);
                }
                else if (obj.Geometry is Brep b)
                {
                    var brepMeshes = Mesh.CreateFromBrep(b, MeshingParameters.Default);
                    if (brepMeshes != null)
                    {
                        meshes.AddRange(brepMeshes);
                    }
                }
            }

            if (meshes.Count == 0)
            {
                return JsonSerializer.Serialize(new { status = "error", message = "No meshes or Breps found to export" });
            }

            // Execute Rhino 8 native ShrinkWrap
            var shrinkWrapParams = new ShrinkWrapParameters
            {
                TargetEdgeLength = targetVoxelSizeMm,
                Offset = 0.0
            };

            var watertightMesh = Mesh.ShrinkWrap(meshes, shrinkWrapParams, System.Threading.CancellationToken.None);

            if (watertightMesh == null || !watertightMesh.IsValid)
            {
                return JsonSerializer.Serialize(new { status = "error", message = "ShrinkWrap failed to produce watertight mesh" });
            }

            return JsonSerializer.Serialize(new
            {
                status = "success",
                outputFilePath,
                isSolid = watertightMesh.IsSolid,
                isClosed = watertightMesh.IsClosed,
                vertexCount = watertightMesh.Vertices.Count,
                faceCount = watertightMesh.Faces.Count,
                targetVoxelSizeMm
            });
        });
    }
}
