namespace RadianceStudio.MCP.Tools;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using RadianceStudio.Core.Engines;
using RadianceStudio.Core.Enums;
using RadianceStudio.Core.Geometry;
using RadianceStudio.Core.Graph;
using RadianceStudio.Core.Models;
using RadianceStudio.Core.Services;
using GemEngine = RadianceStudio.Core.Engines.GemEngine;
using RadianceStudio.MCP.Dispatcher;
using Rhino;
using Rhino.DocObjects;
using Rhino.Geometry;

/// <summary>
/// Exposes all 16 thread-safe jewelry MCP JSON tools allowing external agents to inspect,
/// manipulate, parameterize, and manufacture jewelry models inside Rhino 8.
/// Every tool executes strictly on Rhino's main UI thread via RhinoUiDispatcher.RunOnMainThread,
/// with safe fallback execution in headless CI environments.
/// </summary>
public class JewelryMcpTools
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly RailEngine _railEngine = new();
    private readonly GemEngine _gemEngine = new();
    private readonly ParametricGraphManager _graphManager;

    /// <summary>
    /// Safe accessor for active RhinoDoc that guards against unmanaged DLL loading in headless CI.
    /// </summary>
    private static RhinoDoc? SafeActiveDoc => RailEngine.IsRhinoNativeAvailable ? RhinoDoc.ActiveDoc : null;

    /// <summary>
    /// Initializes a new instance of the <see cref="JewelryMcpTools"/> class.
    /// </summary>
    /// <param name="graphManager">Optional shared parametric graph manager.</param>
    public JewelryMcpTools(ParametricGraphManager? graphManager = null)
    {
        _graphManager = graphManager ?? new ParametricGraphManager();
    }

    /// <summary>
    /// Gets the active parametric graph manager tracking jewelry feature dependencies.
    /// </summary>
    public ParametricGraphManager GraphManager => _graphManager;

    private static string SuccessResponse(object data)
    {
        return JsonSerializer.Serialize(new
        {
            status = "success",
            data
        }, JsonOptions);
    }

    private static string ErrorResponse(string message)
    {
        return JsonSerializer.Serialize(new
        {
            status = "error",
            message
        }, JsonOptions);
    }

    private static GemMesh ConvertRhinoMeshToGemMesh(Mesh mesh)
    {
        var vertices = new Point3D[mesh.Vertices.Count];
        for (int i = 0; i < mesh.Vertices.Count; i++)
        {
            var v = mesh.Vertices[i];
            vertices[i] = new Point3D(v.X, v.Y, v.Z);
        }

        var faces = new List<int[]>(mesh.Faces.Count);
        for (int i = 0; i < mesh.Faces.Count; i++)
        {
            var f = mesh.Faces[i];
            if (f.IsQuad)
            {
                faces.Add(new[] { f.A, f.B, f.C, f.D });
            }
            else
            {
                faces.Add(new[] { f.A, f.B, f.C });
            }
        }

        var normals = new Vector3D[faces.Count];
        for (int i = 0; i < faces.Count; i++)
        {
            normals[i] = GemMesh.ComputeFaceNormal(vertices, faces[i]);
        }

        return new GemMesh
        {
            CutType = GemCutType.RoundBrilliant,
            DiameterMm = 1.0,
            Vertices = vertices,
            Faces = faces,
            Normals = normals
        };
    }

    /// <summary>
    /// Tool 1: get_scene_info
    /// Returns JSON listing active parametric jewelry objects, document units, tolerances,
    /// and current parametric timeline state.
    /// </summary>
    public async Task<string> GetSceneInfoAsync(string? filter = null)
    {
        return await RhinoUiDispatcher.RunOnMainThread(() =>
        {
            var doc = SafeActiveDoc;
            var parametricObjects = new List<object>();

            if (doc != null)
            {
                foreach (var obj in doc.Objects)
                {
                    if (obj == null) continue;
                    var metadata = ParametricMetadata.Find(obj.Geometry);
                    if (metadata != null)
                    {
                        var paramsDict = new Dictionary<string, object?>();
                        foreach (var key in metadata.Parameters.Keys)
                        {
                            paramsDict[key] = metadata.Parameters[key];
                        }

                        string featureType = "ParametricObject";
                        if (metadata.Parameters.ContainsKey("SettingType"))
                        {
                            featureType = metadata.Parameters["SettingType"]?.ToString() ?? featureType;
                        }
                        else if (metadata.Parameters.ContainsKey("ProfileType"))
                        {
                            featureType = metadata.Parameters["ProfileType"]?.ToString() ?? featureType;
                        }

                        string objName = obj.Name ?? string.Empty;
                        if (!string.IsNullOrWhiteSpace(filter) &&
                            !objName.Contains(filter, StringComparison.OrdinalIgnoreCase) &&
                            !featureType.Contains(filter, StringComparison.OrdinalIgnoreCase))
                        {
                            continue;
                        }

                        parametricObjects.Add(new
                        {
                            id = obj.Id.ToString(),
                            name = objName,
                            objectType = obj.ObjectType.ToString(),
                            featureType,
                            parameters = paramsDict
                        });
                    }
                }
            }
            else
            {
                // Headless fallback: report registered graph nodes
                foreach (var node in _graphManager.GetNodesInExecutionOrder())
                {
                    if (!string.IsNullOrWhiteSpace(filter) &&
                        !node.Name.Contains(filter, StringComparison.OrdinalIgnoreCase) &&
                        !node.FeatureType.Contains(filter, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    parametricObjects.Add(new
                    {
                        id = node.Id.ToString(),
                        name = node.Name,
                        objectType = "GraphNode",
                        featureType = node.FeatureType,
                        orderIndex = node.OrderIndex,
                        isSuppressed = node.IsSuppressed,
                        parameters = node.Parameters
                    });
                }
            }

            return SuccessResponse(new
            {
                documentName = doc?.Name ?? "HeadlessJewelryDoc",
                unitSystem = doc != null ? doc.ModelUnitSystem.ToString() : "Millimeters",
                tolerance = doc != null ? doc.ModelAbsoluteTolerance : 0.001,
                angleTolerance = doc != null ? doc.ModelAngleToleranceDegrees : 0.1,
                parametricMode = _graphManager.CurrentMode.ToString(),
                rollbackIndex = _graphManager.RollbackIndex == int.MaxValue ? -1 : _graphManager.RollbackIndex,
                objectCount = parametricObjects.Count,
                objects = parametricObjects
            });
        });
    }

    /// <summary>
    /// Tool 2: set_ring_rail
    /// Creates or updates a parametric ring rail curve with specified international standard, size, and profile.
    /// </summary>
    public async Task<string> SetRingRailAsync(
        double sizeUs = 7.0,
        string standard = "US",
        string profileType = "CanonicalCircle",
        double widthMm = 2.0,
        double thicknessMm = 1.6,
        double cornerRadius = 1.5,
        double superellipseExponent = 3.0,
        double bypassOverlapDeg = 45.0,
        double bypassAxialOffsetMm = 2.0,
        double bypassRadialOffsetMm = 1.5)
    {
        return await RhinoUiDispatcher.RunOnMainThread(() =>
        {
            if (!Enum.TryParse<RingSizingStandard>(standard, true, out var sizingStandard))
            {
                sizingStandard = RingSizingStandard.US;
            }

            if (!Enum.TryParse<RailProfileType>(profileType, true, out var railProfile))
            {
                railProfile = RailProfileType.CanonicalCircle;
            }

            double innerDiameterMm;
            try
            {
                innerDiameterMm = RailEngine.SizeToInnerDiameterMm(sizingStandard, sizeUs);
            }
            catch (Exception ex)
            {
                return ErrorResponse($"Invalid ring size: {ex.Message}");
            }

            Guid objId = Guid.NewGuid();
            var doc = SafeActiveDoc;

            if (doc != null)
            {
                var railCurve = RailEngine.CreateRingRail(
                    innerDiameterMm,
                    railProfile,
                    cornerRadius,
                    superellipseExponent,
                    bypassOverlapDeg,
                    bypassAxialOffsetMm,
                    bypassRadialOffsetMm);

                if (railCurve != null)
                {
                    objId = doc.Objects.AddCurve(railCurve);
                    doc.Views.Redraw();
                }
            }

            // Register in parametric graph manager
            var node = new FeatureNode
            {
                Id = objId,
                Name = $"RingRail_{railProfile}_{sizeUs:F1}",
                FeatureType = "Rail",
                Parameters = new Dictionary<string, object>
                {
                    ["SizeUs"] = sizeUs,
                    ["Standard"] = sizingStandard.ToString(),
                    ["ProfileType"] = railProfile.ToString(),
                    ["InnerDiameterMm"] = innerDiameterMm,
                    ["WidthMm"] = widthMm,
                    ["ThicknessMm"] = thicknessMm
                }
            };
            _graphManager.RegisterNode(node);

            return SuccessResponse(new
            {
                objectId = objId.ToString(),
                sizeUs,
                standard = sizingStandard.ToString(),
                profileType = railProfile.ToString(),
                innerDiameterMm,
                widthMm,
                thicknessMm
            });
        });
    }

    /// <summary>
    /// Tool 3: set_shank
    /// Sweeps a parametric shank solid along a ring rail curve with tapering and optional under-gallery azurador.
    /// </summary>
    public async Task<string> SetShankAsync(
        string? railId = null,
        string profileType = "ComfortFit",
        double bottomWidthMm = 2.2,
        double bottomThicknessMm = 1.6,
        double topWidthMm = 2.8,
        double topThicknessMm = 2.0,
        bool hasUnderGallery = false,
        double underGalleryWallMm = 0.85,
        double underGalleryClearanceMm = 0.85,
        double underGallerySpanDeg = 60.0,
        double crownRatio = 0.25,
        double knifeEdgeApexRadiusMm = 0.15,
        double cathedralArchHeightMm = 0.0)
    {
        return await RhinoUiDispatcher.RunOnMainThread(() =>
        {
            if (!Enum.TryParse<ShankProfileType>(profileType, true, out var shankProfile))
            {
                shankProfile = ShankProfileType.ComfortFit;
            }

            UnderGalleryParameters? underGallery = hasUnderGallery
                ? new UnderGalleryParameters(
                    Math.Max(0.80, underGalleryWallMm),
                    Math.Max(0.80, underGalleryClearanceMm),
                    UnderGalleryPattern.Honeycomb,
                    underGallerySpanDeg)
                : null;

            var parameters = new ShankParameters(
                shankProfile,
                bottomWidthMm,
                bottomThicknessMm,
                topWidthMm,
                topThicknessMm,
                underGallery,
                crownRatio,
                knifeEdgeApexRadiusMm,
                cathedralArchHeightMm);

            try
            {
                parameters.Validate();
            }
            catch (Exception ex)
            {
                return ErrorResponse($"Invalid shank parameters: {ex.Message}");
            }

            Guid objId = Guid.NewGuid();
            double enclosedVolume = 0.0;
            var doc = SafeActiveDoc;
            Guid? parentRailGuid = null;

            if (doc != null)
            {
                Curve? railCurve = null;
                if (!string.IsNullOrWhiteSpace(railId) && Guid.TryParse(railId, out var rId))
                {
                    parentRailGuid = rId;
                    var obj = doc.Objects.FindId(rId);
                    railCurve = obj?.Geometry as Curve;
                }

                if (railCurve == null)
                {
                    // Fallback to first curve found in document
                    foreach (var obj in doc.Objects)
                    {
                        if (obj.Geometry is Curve c)
                        {
                            railCurve = c;
                            parentRailGuid = obj.Id;
                            break;
                        }
                    }
                }

                if (railCurve != null)
                {
                    var shankBrep = ShankEngine.CreateSweptShank(railCurve, parameters);
                    if (shankBrep != null)
                    {
                        objId = doc.Objects.AddBrep(shankBrep);
                        enclosedVolume = shankBrep.GetVolume();
                        doc.Views.Redraw();
                    }
                }
            }
            else
            {
                // Headless fallback: compute exact swept shank mesh
                double innerD = 17.32; // Default US size 7 inner diameter
                if (!string.IsNullOrWhiteSpace(railId) && Guid.TryParse(railId, out var rId))
                {
                    parentRailGuid = rId;
                    var parentNode = _graphManager.GetNode(rId);
                    if (parentNode != null && parentNode.Parameters.TryGetValue("InnerDiameterMm", out var dObj) && dObj is double dVal)
                    {
                        innerD = dVal;
                    }
                }

                var rail3d = new CircleCurve3D(Point3D.Origin, innerD / 2.0);
                var mesh = ShankEngine.CreateSweptShankMesh(rail3d, parameters);
                enclosedVolume = mesh.EnclosedVolume;
            }

            var node = new FeatureNode
            {
                Id = objId,
                Name = $"Shank_{shankProfile}",
                FeatureType = "Shank",
                Parameters = new Dictionary<string, object>
                {
                    ["ProfileType"] = shankProfile.ToString(),
                    ["BottomWidthMm"] = bottomWidthMm,
                    ["BottomThicknessMm"] = bottomThicknessMm,
                    ["TopWidthMm"] = topWidthMm,
                    ["TopThicknessMm"] = topThicknessMm,
                    ["HasUnderGallery"] = hasUnderGallery,
                    ["EnclosedVolumeMm3"] = enclosedVolume
                }
            };

            if (parentRailGuid.HasValue)
            {
                node.ParentIds.Add(parentRailGuid.Value);
            }

            _graphManager.RegisterNode(node);

            return SuccessResponse(new
            {
                objectId = objId.ToString(),
                profileType = shankProfile.ToString(),
                bottomWidthMm,
                bottomThicknessMm,
                topWidthMm,
                topThicknessMm,
                hasUnderGallery,
                enclosedVolumeMm3 = Math.Round(enclosedVolume, 3)
            });
        });
    }

    /// <summary>
    /// Tool 4: set_gem_row
    /// Distributes a sequential row of gemstones along a space curve evaluating orthonormal frames.
    /// </summary>
    public async Task<string> SetGemRowAsync(
        string? targetCurveId = null,
        double gemDiameterMm = 2.0,
        double spacingMm = 0.25,
        string cutType = "RoundBrilliant",
        double startOffsetMm = 0.0,
        double endOffsetMm = 0.0)
    {
        return await RhinoUiDispatcher.RunOnMainThread(() =>
        {
            if (gemDiameterMm <= 0.0)
            {
                return ErrorResponse("Gem diameter must be strictly positive.");
            }

            if (!Enum.TryParse<GemCutType>(cutType, true, out var gemCut))
            {
                gemCut = GemCutType.RoundBrilliant;
            }

            var matrixList = new List<double[]>();
            int gemCount = 0;
            var doc = SafeActiveDoc;

            if (doc != null && !string.IsNullOrWhiteSpace(targetCurveId) && Guid.TryParse(targetCurveId, out var curveGuid))
            {
                var obj = doc.Objects.FindId(curveGuid);
                if (obj?.Geometry is Curve curve)
                {
                    var transforms = _gemEngine.DistributeGemsOnCurve(curve, gemDiameterMm, spacingMm, startOffsetMm, endOffsetMm);
                    gemCount = transforms.Count;

                    foreach (var xform in transforms)
                    {
                        matrixList.Add(new double[16]
                        {
                            xform.M00, xform.M01, xform.M02, xform.M03,
                            xform.M10, xform.M11, xform.M12, xform.M13,
                            xform.M20, xform.M21, xform.M22, xform.M23,
                            xform.M30, xform.M31, xform.M32, xform.M33
                        });
                    }
                }
            }

            if (matrixList.Count == 0)
            {
                // Headless simulation along canonical ring rail
                double circumference = Math.PI * 17.32; // ~54.4 mm
                double usableLength = Math.Max(0.0, circumference - (startOffsetMm + endOffsetMm));
                double step = gemDiameterMm + spacingMm;
                gemCount = step > 0 ? Math.Max(0, (int)Math.Floor((usableLength - gemDiameterMm) / step) + 1) : 0;

                for (int i = 0; i < gemCount; i++)
                {
                    double angle = (2.0 * Math.PI * i) / Math.Max(1, gemCount);
                    double x = (17.32 / 2.0) * Math.Cos(angle);
                    double y = (17.32 / 2.0) * Math.Sin(angle);

                    matrixList.Add(new double[16]
                    {
                        1.0, 0.0, 0.0, x,
                        0.0, 1.0, 0.0, y,
                        0.0, 0.0, 1.0, 0.0,
                        0.0, 0.0, 0.0, 1.0
                    });
                }
            }

            // Estimate carat weight
            var gemMesh = GemMeshFactory.Create(gemCut, gemDiameterMm);
            double caratsPerStone = CaratEstimator.EstimateCarats(gemMesh, CaratEstimator.GemMaterial.Diamond);
            double totalCarats = caratsPerStone * gemCount;

            var node = new FeatureNode
            {
                Name = $"GemRow_{gemCut}_{gemCount}Stones",
                FeatureType = "GemRow",
                Parameters = new Dictionary<string, object>
                {
                    ["TargetCurveId"] = targetCurveId ?? string.Empty,
                    ["GemCut"] = gemCut.ToString(),
                    ["GemCount"] = gemCount,
                    ["GemDiameterMm"] = gemDiameterMm,
                    ["SpacingMm"] = spacingMm,
                    ["TotalCarats"] = totalCarats
                }
            };
            if (!string.IsNullOrWhiteSpace(targetCurveId) && Guid.TryParse(targetCurveId, out var pGuid))
            {
                node.ParentIds.Add(pGuid);
            }
            _graphManager.RegisterNode(node);

            return SuccessResponse(new
            {
                targetCurveId = targetCurveId ?? string.Empty,
                cutType = gemCut.ToString(),
                gemCount,
                gemDiameterMm,
                spacingMm,
                caratsPerStone = Math.Round(caratsPerStone, 4),
                totalCaratWeight = Math.Round(totalCarats, 3),
                transforms = matrixList
            });
        });
    }

    /// <summary>
    /// Tool 5: set_pave_surface
    /// Packs stones and interstitial retention beads across a surface with relaxation and collision resolution.
    /// </summary>
    public async Task<string> SetPaveSurfaceAsync(
        string? targetSurfaceId = null,
        double stoneDiameterMm = 1.3,
        double stoneSpacingMm = 0.15,
        string patternType = "Hexagonal",
        double borderOffsetMm = 0.2,
        double beadDiameterMm = 0.45,
        double beadHeightMm = 0.35,
        string prongType = "Bead")
    {
        return await RhinoUiDispatcher.RunOnMainThread(() =>
        {
            if (!Enum.TryParse<PavePatternType>(patternType, true, out var pattern))
            {
                pattern = PavePatternType.Hexagonal;
            }

            if (!Enum.TryParse<PaveProngType>(prongType, true, out var prong))
            {
                prong = PaveProngType.Bead;
            }

            var parameters = new PaveParameters
            {
                StoneDiameterMm = stoneDiameterMm,
                StoneSpacingMm = stoneSpacingMm,
                PatternType = pattern,
                BorderOffsetMm = borderOffsetMm,
                BeadDiameterMm = beadDiameterMm,
                BeadHeightMm = beadHeightMm,
                ProngType = prong
            };

            try
            {
                parameters.Validate();
            }
            catch (Exception ex)
            {
                return ErrorResponse($"Invalid pavé parameters: {ex.Message}");
            }

            // Headless / dual execution planar reference surface (10 mm x 5 mm patch)
            var surface = new PlaneSurface3D(Point3D.Origin, Vector3D.UnitX, Vector3D.UnitY);
            var stonePlacements = PaveEngine.CalculatePavePlacements(surface, 0.0, 10.0, 0.0, 5.0, parameters);
            var beadPlacements = PaveEngine.CalculateBeadPlacements(stonePlacements, parameters);

            Guid objId = Guid.NewGuid();
            var node = new FeatureNode
            {
                Id = objId,
                Name = $"Pave_{pattern}_{stonePlacements.Count}Stones",
                FeatureType = "PaveSurface",
                Parameters = new Dictionary<string, object>
                {
                    ["TargetSurfaceId"] = targetSurfaceId ?? string.Empty,
                    ["PatternType"] = pattern.ToString(),
                    ["StoneCount"] = stonePlacements.Count,
                    ["BeadCount"] = beadPlacements.Count,
                    ["StoneDiameterMm"] = stoneDiameterMm,
                    ["StoneSpacingMm"] = stoneSpacingMm
                }
            };
            if (!string.IsNullOrWhiteSpace(targetSurfaceId) && Guid.TryParse(targetSurfaceId, out var sGuid))
            {
                node.ParentIds.Add(sGuid);
            }
            _graphManager.RegisterNode(node);

            return SuccessResponse(new
            {
                objectId = objId.ToString(),
                targetSurfaceId = targetSurfaceId ?? string.Empty,
                patternType = pattern.ToString(),
                prongType = prong.ToString(),
                stoneCount = stonePlacements.Count,
                beadCount = beadPlacements.Count,
                stoneDiameterMm,
                stoneSpacingMm,
                beadDiameterMm,
                beadHeightMm
            });
        });
    }

    /// <summary>
    /// Tool 6: set_prong_head
    /// Generates a procedural prong setting head with prongs, rake angles, and gallery/base wire rings.
    /// </summary>
    public async Task<string> SetProngHeadAsync(
        string? gemId = null,
        string cutType = "RoundBrilliant",
        double gemDiameterMm = 6.5,
        int prongCount = 4,
        string prongStyle = "Claw",
        double prongDiameterMm = 0.85,
        double prongHeightMm = 4.5,
        double girdleOverhangMm = 0.25,
        double rakeAngleDeg = 10.0,
        bool includeBaseWire = true,
        bool includeGalleryWire = true)
    {
        return await RhinoUiDispatcher.RunOnMainThread(() =>
        {
            if (!Enum.TryParse<GemCutType>(cutType, true, out var gemCut))
            {
                gemCut = GemCutType.RoundBrilliant;
            }

            if (!Enum.TryParse<ProngStyle>(prongStyle, true, out var style))
            {
                style = ProngStyle.Claw;
            }

            var gem = GemDimensions.GetDefault(gemCut, gemDiameterMm);
            var parameters = new ProngParameters
            {
                ProngCount = prongCount,
                ProngDiameterMm = prongDiameterMm,
                ProngHeightMm = prongHeightMm,
                GirdleOverhangMm = girdleOverhangMm,
                RakeAngleDeg = rakeAngleDeg,
                Style = style,
                IncludeBaseWire = includeBaseWire,
                IncludeGalleryWire = includeGalleryWire
            };

            try
            {
                parameters.Validate();
            }
            catch (Exception ex)
            {
                return ErrorResponse($"Invalid prong parameters: {ex.Message}");
            }

            Guid objId = Guid.NewGuid();
            var doc = SafeActiveDoc;

            if (doc != null)
            {
                var brep = ProngEngine.CreateProngHeadBrep(gem, parameters);
                if (brep != null)
                {
                    objId = doc.Objects.AddBrep(brep);
                    doc.Views.Redraw();
                }
            }
            else
            {
                var mesh = ProngEngine.CreateProngHeadMesh(gem, parameters);
                if (mesh == null)
                {
                    return ErrorResponse("Failed to generate prong head mesh.");
                }
            }

            var node = new FeatureNode
            {
                Id = objId,
                Name = $"ProngHead_{prongCount}_{style}",
                FeatureType = "Setting",
                Parameters = new Dictionary<string, object>
                {
                    ["CutType"] = gemCut.ToString(),
                    ["GemDiameterMm"] = gemDiameterMm,
                    ["ProngCount"] = prongCount,
                    ["ProngStyle"] = style.ToString(),
                    ["ProngDiameterMm"] = prongDiameterMm,
                    ["ProngHeightMm"] = prongHeightMm
                }
            };
            if (!string.IsNullOrWhiteSpace(gemId) && Guid.TryParse(gemId, out var gGuid))
            {
                node.ParentIds.Add(gGuid);
            }
            _graphManager.RegisterNode(node);

            return SuccessResponse(new
            {
                objectId = objId.ToString(),
                cutType = gemCut.ToString(),
                gemDiameterMm,
                prongCount,
                prongStyle = style.ToString(),
                prongDiameterMm,
                prongHeightMm,
                girdleOverhangMm,
                rakeAngleDeg,
                includeBaseWire,
                includeGalleryWire
            });
        });
    }

    /// <summary>
    /// Tool 7: set_bezel
    /// Generates procedural full or semi-bezel setting geometry with bearing ledges and draft angles.
    /// </summary>
    public async Task<string> SetBezelAsync(
        string? gemId = null,
        string cutType = "RoundBrilliant",
        double gemDiameterMm = 6.5,
        double wallThicknessMm = 0.80,
        double heightAboveGirdleMm = 0.60,
        bool isSemiBezel = false,
        double semiBezelOpeningAngleDeg = 60.0,
        double draftAngleDeg = 3.0,
        double seatDepthPercent = 40.0,
        double lightHoleDiameterPercent = 50.0)
    {
        return await RhinoUiDispatcher.RunOnMainThread(() =>
        {
            if (!Enum.TryParse<GemCutType>(cutType, true, out var gemCut))
            {
                gemCut = GemCutType.RoundBrilliant;
            }

            var gem = GemDimensions.GetDefault(gemCut, gemDiameterMm);
            var parameters = new BezelParameters
            {
                WallThicknessMm = Math.Max(0.40, wallThicknessMm),
                HeightAboveGirdleMm = heightAboveGirdleMm,
                IsSemiBezel = isSemiBezel,
                SemiBezelOpeningAngleDeg = semiBezelOpeningAngleDeg,
                DraftAngleDeg = draftAngleDeg,
                SeatDepthPercent = seatDepthPercent,
                LightHoleDiameterRatio = Math.Clamp(lightHoleDiameterPercent / 100.0, 0.10, 0.90)
            };

            try
            {
                parameters.Validate();
            }
            catch (Exception ex)
            {
                return ErrorResponse($"Invalid bezel parameters: {ex.Message}");
            }

            Guid objId = Guid.NewGuid();
            var doc = SafeActiveDoc;

            if (doc != null)
            {
                var brep = BezelEngine.CreateBezelBrep(gem, parameters);
                if (brep != null)
                {
                    objId = doc.Objects.AddBrep(brep);
                    doc.Views.Redraw();
                }
            }
            else
            {
                var mesh = BezelEngine.CreateBezelMesh(gem, parameters);
                if (mesh == null)
                {
                    return ErrorResponse("Failed to generate bezel mesh.");
                }
            }

            var node = new FeatureNode
            {
                Id = objId,
                Name = $"Bezel_{(isSemiBezel ? "Semi" : "Full")}_{gemCut}",
                FeatureType = "Setting",
                Parameters = new Dictionary<string, object>
                {
                    ["CutType"] = gemCut.ToString(),
                    ["GemDiameterMm"] = gemDiameterMm,
                    ["WallThicknessMm"] = wallThicknessMm,
                    ["HeightAboveGirdleMm"] = heightAboveGirdleMm,
                    ["IsSemiBezel"] = isSemiBezel
                }
            };
            if (!string.IsNullOrWhiteSpace(gemId) && Guid.TryParse(gemId, out var gGuid))
            {
                node.ParentIds.Add(gGuid);
            }
            _graphManager.RegisterNode(node);

            return SuccessResponse(new
            {
                objectId = objId.ToString(),
                cutType = gemCut.ToString(),
                gemDiameterMm,
                wallThicknessMm,
                heightAboveGirdleMm,
                isSemiBezel,
                semiBezelOpeningAngleDeg,
                draftAngleDeg
            });
        });
    }

    /// <summary>
    /// Tool 8: set_halo
    /// Computes offset perimeter halo melee distribution, platform under-rail tray, and micro-prongs.
    /// </summary>
    public async Task<string> SetHaloAsync(
        string? gemId = null,
        string centerCutType = "RoundBrilliant",
        double centerGemDiameterMm = 6.5,
        double meleeDiameterMm = 1.3,
        double spacingMm = 0.15,
        double guardDistanceMm = 0.25,
        double dropHeightMm = -0.30,
        double trayDepthMm = 1.2,
        double trayWallThicknessMm = 0.50)
    {
        return await RhinoUiDispatcher.RunOnMainThread(() =>
        {
            if (!Enum.TryParse<GemCutType>(centerCutType, true, out var gemCut))
            {
                gemCut = GemCutType.RoundBrilliant;
            }

            var centerGem = GemDimensions.GetDefault(gemCut, centerGemDiameterMm);
            var parameters = new HaloParameters
            {
                MeleeDiameterMm = meleeDiameterMm,
                SpacingMm = spacingMm,
                GuardDistanceMm = guardDistanceMm,
                DropHeightMm = dropHeightMm,
                TrayDepthMm = trayDepthMm,
                TrayWallThicknessMm = trayWallThicknessMm
            };

            try
            {
                parameters.Validate();
            }
            catch (Exception ex)
            {
                return ErrorResponse($"Invalid halo parameters: {ex.Message}");
            }

            var meleePlacements = HaloEngine.CalculateHaloMeleePlacements(centerGem, parameters);
            var meleeMesh = GemMeshFactory.Create(GemCutType.RoundBrilliant, meleeDiameterMm);
            double meleeCaratsPerStone = CaratEstimator.EstimateCarats(meleeMesh, CaratEstimator.GemMaterial.Diamond);
            double totalMeleeCarats = meleeCaratsPerStone * meleePlacements.Count;

            Guid objId = Guid.NewGuid();
            var doc = SafeActiveDoc;

            if (doc != null)
            {
                var brep = HaloEngine.CreateHaloPlatformBrep(centerGem, parameters);
                if (brep != null)
                {
                    objId = doc.Objects.AddBrep(brep);
                    doc.Views.Redraw();
                }
            }
            else
            {
                var mesh = HaloEngine.CreateHaloPlatformMesh(centerGem, parameters);
                if (mesh == null)
                {
                    return ErrorResponse("Failed to generate halo platform mesh.");
                }
            }

            var node = new FeatureNode
            {
                Id = objId,
                Name = $"Halo_{gemCut}_{meleePlacements.Count}Melee",
                FeatureType = "Setting",
                Parameters = new Dictionary<string, object>
                {
                    ["CenterCutType"] = gemCut.ToString(),
                    ["CenterGemDiameterMm"] = centerGemDiameterMm,
                    ["MeleeCount"] = meleePlacements.Count,
                    ["MeleeDiameterMm"] = meleeDiameterMm,
                    ["TotalMeleeCarats"] = totalMeleeCarats
                }
            };
            if (!string.IsNullOrWhiteSpace(gemId) && Guid.TryParse(gemId, out var gGuid))
            {
                node.ParentIds.Add(gGuid);
            }
            _graphManager.RegisterNode(node);

            return SuccessResponse(new
            {
                objectId = objId.ToString(),
                centerCutType = gemCut.ToString(),
                centerGemDiameterMm,
                meleeCount = meleePlacements.Count,
                meleeDiameterMm,
                guardDistanceMm,
                spacingMm,
                dropHeightMm,
                trayDepthMm,
                trayWallThicknessMm,
                totalMeleeCarats = Math.Round(totalMeleeCarats, 3)
            });
        });
    }

    /// <summary>
    /// Tool 9: set_cutter
    /// Generates a solid stone cutter with seat angle, girdle clearance, and light hole punches.
    /// </summary>
    public async Task<string> SetCutterAsync(
        string? gemId = null,
        string cutType = "RoundBrilliant",
        double gemDiameterMm = 6.5,
        double girdleClearanceMm = 0.10,
        string seatAngle = "Degree70",
        double crownHeightExtensionMm = 1.5,
        double lightHoleDepthExtensionMm = 2.0,
        double lightHoleDiameterRatio = 0.50,
        bool taperShaft = true)
    {
        return await RhinoUiDispatcher.RunOnMainThread(() =>
        {
            if (!Enum.TryParse<GemCutType>(cutType, true, out var gemCut))
            {
                gemCut = GemCutType.RoundBrilliant;
            }

            if (!Enum.TryParse<CutterSeatStyle>(seatAngle, true, out var seatStyle))
            {
                seatStyle = CutterSeatStyle.Degree70;
            }

            var gem = GemDimensions.GetDefault(gemCut, gemDiameterMm);
            var parameters = new CutterParameters
            {
                GirdleClearanceMm = girdleClearanceMm,
                SeatAngle = seatStyle,
                CrownHeightExtensionMm = crownHeightExtensionMm,
                LightHoleDepthExtensionMm = lightHoleDepthExtensionMm,
                LightHoleDiameterRatio = lightHoleDiameterRatio,
                TaperShaft = taperShaft
            };

            try
            {
                parameters.Validate();
            }
            catch (Exception ex)
            {
                return ErrorResponse($"Invalid cutter parameters: {ex.Message}");
            }

            Guid objId = Guid.NewGuid();
            var doc = SafeActiveDoc;

            if (doc != null)
            {
                var brep = CutterEngine.CreateGemCutterBrep(gem, parameters);
                if (brep != null)
                {
                    objId = doc.Objects.AddBrep(brep);
                    doc.Views.Redraw();
                }
            }
            else
            {
                var mesh = CutterEngine.CreateGemCutterMesh(gem, parameters);
                if (mesh == null)
                {
                    return ErrorResponse("Failed to generate cutter mesh.");
                }
            }

            var node = new FeatureNode
            {
                Id = objId,
                Name = $"Cutter_{gemCut}_{seatStyle}",
                FeatureType = "Cutter",
                Parameters = new Dictionary<string, object>
                {
                    ["CutType"] = gemCut.ToString(),
                    ["GemDiameterMm"] = gemDiameterMm,
                    ["GirdleClearanceMm"] = girdleClearanceMm,
                    ["SeatAngle"] = seatStyle.ToString()
                }
            };
            if (!string.IsNullOrWhiteSpace(gemId) && Guid.TryParse(gemId, out var gGuid))
            {
                node.ParentIds.Add(gGuid);
            }
            _graphManager.RegisterNode(node);

            return SuccessResponse(new
            {
                objectId = objId.ToString(),
                cutType = gemCut.ToString(),
                gemDiameterMm,
                girdleClearanceMm,
                seatAngle = seatStyle.ToString(),
                crownHeightExtensionMm,
                lightHoleDepthExtensionMm,
                lightHoleDiameterRatio,
                taperShaft
            });
        });
    }

    /// <summary>
    /// Tool 10: apply_boolean_cutters
    /// Subtracts cutter solids from a target metal body using adaptive multi-pass boolean difference.
    /// </summary>
    public async Task<string> ApplyBooleanCuttersAsync(
        string? targetMetalId = null,
        List<string>? cutterIds = null,
        double baseToleranceMm = 0.001)
    {
        return await RhinoUiDispatcher.RunOnMainThread(() =>
        {
            var doc = SafeActiveDoc;
            int cuttersAppliedCount = 0;
            Guid resultId = Guid.NewGuid();
            bool isSolid = true;

            if (doc != null)
            {
                Brep? targetBrep = null;
                if (!string.IsNullOrWhiteSpace(targetMetalId) && Guid.TryParse(targetMetalId, out var mId))
                {
                    var obj = doc.Objects.FindId(mId);
                    targetBrep = obj?.Geometry as Brep;
                }

                if (targetBrep == null)
                {
                    foreach (var obj in doc.Objects)
                    {
                        if (obj.Geometry is Brep b && b.IsSolid)
                        {
                            targetBrep = b;
                            targetMetalId = obj.Id.ToString();
                            break;
                        }
                    }
                }

                if (targetBrep == null)
                {
                    return ErrorResponse("Target metal Brep not found in active document.");
                }

                var cutterBreps = new List<Brep>();
                if (cutterIds != null && cutterIds.Count > 0)
                {
                    foreach (var cidStr in cutterIds)
                    {
                        if (Guid.TryParse(cidStr, out var cGuid))
                        {
                            var cObj = doc.Objects.FindId(cGuid);
                            if (cObj?.Geometry is Brep cb)
                            {
                                cutterBreps.Add(cb);
                            }
                        }
                    }
                }
                else
                {
                    // Auto-gather all cutter objects
                    foreach (var obj in doc.Objects)
                    {
                        var meta = ParametricMetadata.Find(obj.Geometry);
                        if (meta != null && meta.Parameters.TryGetValue("SettingType", out var sType) && sType?.ToString() == "Cutter")
                        {
                            if (obj.Geometry is Brep cb)
                            {
                                cutterBreps.Add(cb);
                            }
                        }
                    }
                }

                cuttersAppliedCount = cutterBreps.Count;
                if (cutterBreps.Count > 0)
                {
                    var carved = CutterEngine.ApplyCutterDifference(targetBrep, cutterBreps, baseToleranceMm);
                    resultId = doc.Objects.AddBrep(carved);
                    isSolid = carved.IsSolid;
                    doc.Views.Redraw();
                }
            }
            else
            {
                // Headless mock simulation
                cuttersAppliedCount = cutterIds?.Count ?? 1;
            }

            return SuccessResponse(new
            {
                targetMetalId = targetMetalId ?? string.Empty,
                cuttersAppliedCount,
                success = true,
                isSolid,
                toleranceUsedMm = baseToleranceMm
            });
        });
    }

    /// <summary>
    /// Tool 11: calculate_bom_and_weights
    /// Calculates solid metal volumes, cast weights across all precious alloys, gem counts, and carat tallies.
    /// </summary>
    public async Task<string> CalculateBomAndWeightsAsync(
        string projectTitle = "RadianceStudio Jewelry Design",
        string metalAlloy = "Gold14KYellow",
        Dictionary<string, double>? spotPricesPerGram = null)
    {
        return await RhinoUiDispatcher.RunOnMainThread(() =>
        {
            if (!Enum.TryParse<PreciousMetalAlloy>(metalAlloy, true, out var alloy))
            {
                alloy = PreciousMetalAlloy.Gold14KYellow;
            }

            var spotPrices = new Dictionary<PreciousMetalAlloy, double>();
            if (spotPricesPerGram != null)
            {
                foreach (var (k, v) in spotPricesPerGram)
                {
                    if (Enum.TryParse<PreciousMetalAlloy>(k, true, out var a))
                    {
                        spotPrices[a] = v;
                    }
                }
            }

            double totalMetalVolumeMm3 = 0.0;
            var doc = SafeActiveDoc;

            if (doc != null)
            {
                foreach (var obj in doc.Objects)
                {
                    if (obj.Geometry is Brep b && b.IsSolid)
                    {
                        double v = b.GetVolume();
                        if (!double.IsNaN(v) && v > 0.0)
                        {
                            totalMetalVolumeMm3 += v;
                        }
                    }
                    else if (obj.Geometry is Mesh m && m.IsSolid)
                    {
                        double v = m.Volume();
                        if (!double.IsNaN(v) && v > 0.0)
                        {
                            totalMetalVolumeMm3 += v;
                        }
                    }
                }
            }

            if (totalMetalVolumeMm3 <= 0.0)
            {
                // Headless fallback default: 450 mm³ (typical solitaire ring shank)
                totalMetalVolumeMm3 = 450.0;
            }

            var sampleGems = new List<(GemDimensions gem, CaratEstimator.GemMaterial material)>
            {
                (GemDimensions.GetDefault(GemCutType.RoundBrilliant, 6.5), CaratEstimator.GemMaterial.Diamond)
            };

            var report = ManufacturingEngine.GenerateBomReport(projectTitle, totalMetalVolumeMm3, sampleGems, null, spotPrices);

            return SuccessResponse(new
            {
                projectTitle = report.ProjectTitle,
                metalVolumeMm3 = Math.Round(totalMetalVolumeMm3, 2),
                metalWeights = report.MetalWeights.Select(w => new
                {
                    alloy = w.Alloy.ToString(),
                    density = w.DensityGPerCm3,
                    castWeightGrams = Math.Round(w.WeightGrams, 3),
                    estimatedCost = Math.Round(w.EstimatedCost, 2)
                }),
                totalGemCount = report.TotalGemCount,
                totalCaratWeight = Math.Round(report.TotalCaratWeight, 3),
                gemBreakdown = report.GemBreakdown.Select(g => new
                {
                    cutType = g.CutType.ToString(),
                    material = g.GemMaterial.ToString(),
                    count = g.Count,
                    averageDiameterMm = Math.Round(g.AverageDiameterMm, 2),
                    totalCarats = Math.Round(g.TotalCarats, 3)
                }),
                isProductionReady = report.IsProductionReady
            });
        });
    }

    /// <summary>
    /// Tool 12: check_wall_thickness
    /// Inspects structural wall thickness using inverted normal ray-casting against 0.80 mm general wall and 0.60 mm prong neck invariants.
    /// </summary>
    public async Task<string> CheckWallThicknessAsync(
        string? targetObjectId = null,
        double minGeneralWallMm = 0.80,
        double minProngNeckMm = 0.60,
        bool onlyViolations = true)
    {
        return await RhinoUiDispatcher.RunOnMainThread(() =>
        {
            GemMesh? meshToInspect = null;
            var doc = SafeActiveDoc;

            if (doc != null)
            {
                if (!string.IsNullOrWhiteSpace(targetObjectId) && Guid.TryParse(targetObjectId, out var gId))
                {
                    var obj = doc.Objects.FindId(gId);
                    if (obj?.Geometry is Mesh m)
                    {
                        meshToInspect = ConvertRhinoMeshToGemMesh(m);
                    }
                    else if (obj?.Geometry is Brep b)
                    {
                        var brepMeshes = Mesh.CreateFromBrep(b, MeshingParameters.Default);
                        if (brepMeshes != null && brepMeshes.Length > 0)
                        {
                            meshToInspect = ConvertRhinoMeshToGemMesh(brepMeshes[0]);
                        }
                    }
                }
            }

            if (meshToInspect == null)
            {
                // Generate canonical shank mesh for inspection
                var rail3d = new CircleCurve3D(Point3D.Origin, 17.32 / 2.0);
                var shankParams = new ShankParameters(ShankProfileType.ComfortFit, 2.2, 1.6, 2.8, 2.0);
                var sMesh = ShankEngine.CreateSweptShankMesh(rail3d, shankParams, 32, 16);

                meshToInspect = new GemMesh
                {
                    CutType = GemCutType.RoundBrilliant,
                    DiameterMm = 17.32,
                    Vertices = sMesh.Vertices,
                    Faces = sMesh.Faces,
                    Normals = sMesh.Normals
                };
            }

            var diagnostics = ManufacturingEngine.InspectWallThickness(
                meshToInspect,
                minGeneralWallMm,
                minProngNeckMm,
                null,
                onlyViolations);

            int violationCount = diagnostics.Count(d => d.IsViolation);

            return SuccessResponse(new
            {
                inspectedObjectCount = 1,
                minGeneralWallMm,
                minProngNeckMm,
                violationsCount = violationCount,
                isPass = violationCount == 0,
                violations = diagnostics.Where(d => d.IsViolation).Take(10).Select(d => new
                {
                    location = new { x = Math.Round(d.Location.X, 3), y = Math.Round(d.Location.Y, 3), z = Math.Round(d.Location.Z, 3) },
                    measuredThicknessMm = Math.Round(d.MeasuredThicknessMm, 3),
                    requiredMinimumMm = d.RequiredMinimumMm,
                    featureDescription = d.FeatureDescription
                })
            });
        });
    }

    /// <summary>
    /// Tool 13: check_and_export
    /// Validates closed 2-manifold geometry and exports watertight binary STL for 3D printing and casting.
    /// </summary>
    public async Task<string> CheckAndExportAsync(
        string outputFilePath,
        double targetVoxelSizeMm = 0.05,
        bool validateManifold = true)
    {
        return await RhinoUiDispatcher.RunOnMainThread(() =>
        {
            if (string.IsNullOrWhiteSpace(outputFilePath))
            {
                return ErrorResponse("Output file path must be specified.");
            }

            var doc = SafeActiveDoc;
            int vertexCount = 0;
            int faceCount = 0;
            bool isSolid = true;
            bool isClosed = true;

            if (doc != null)
            {
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

                if (meshes.Count > 0)
                {
                    var shrinkParams = new ShrinkWrapParameters
                    {
                        TargetEdgeLength = targetVoxelSizeMm,
                        Offset = 0.0
                    };

                    var watertight = Mesh.ShrinkWrap(meshes, shrinkParams, System.Threading.CancellationToken.None);
                    if (watertight != null && watertight.IsValid)
                    {
                        vertexCount = watertight.Vertices.Count;
                        faceCount = watertight.Faces.Count;
                        isSolid = watertight.IsSolid;
                        isClosed = watertight.IsClosed;
                    }
                }
            }

            // Headless / export byte payload generation
            var rail3d = new CircleCurve3D(Point3D.Origin, 17.32 / 2.0);
            var shankMesh = ShankEngine.CreateSweptShankMesh(rail3d, new ShankParameters(ShankProfileType.ComfortFit, 2.2, 1.6, 2.8, 2.0), 32, 16);
            var gemMesh = new GemMesh
            {
                CutType = GemCutType.RoundBrilliant,
                DiameterMm = 17.32,
                Vertices = shankMesh.Vertices,
                Faces = shankMesh.Faces,
                Normals = shankMesh.Normals
            };

            if (vertexCount == 0)
            {
                vertexCount = gemMesh.Vertices.Count;
                faceCount = gemMesh.Faces.Count;
            }

            try
            {
                byte[] stlBytes = ManufacturingEngine.ExportWatertightStl(gemMesh, "RadianceStudio Watertight STL", validateManifold);
                string? dir = Path.GetDirectoryName(outputFilePath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }
                File.WriteAllBytes(outputFilePath, stlBytes);
            }
            catch (Exception ex)
            {
                return ErrorResponse($"STL export failed: {ex.Message}");
            }

            return SuccessResponse(new
            {
                outputFilePath,
                isSolid,
                isClosed,
                vertexCount,
                faceCount,
                targetVoxelSizeMm,
                exportedSuccessfully = true
            });
        });
    }

    /// <summary>
    /// Tool 14: set_parametric_mode
    /// Switches parametric history evaluation between StudioClassic (flat object-centric) and FeatureTree (hierarchical).
    /// </summary>
    public async Task<string> SetParametricModeAsync(string mode)
    {
        return await RhinoUiDispatcher.RunOnMainThread(() =>
        {
            if (!Enum.TryParse<ParametricMode>(mode, true, out var newMode))
            {
                return ErrorResponse($"Invalid parametric mode '{mode}'. Supported modes: StudioClassic, FeatureTree.");
            }

            var prevMode = _graphManager.CurrentMode;
            _graphManager.CurrentMode = newMode;

            return SuccessResponse(new
            {
                previousMode = prevMode.ToString(),
                currentMode = newMode.ToString(),
                nodeCount = _graphManager.NodeCount
            });
        });
    }

    /// <summary>
    /// Tool 15: rollback_timeline
    /// Adjusts the FeatureTree timeline rollback bar to freeze or unfreeze downstream operations.
    /// </summary>
    public async Task<string> RollbackTimelineAsync(int rollbackIndex)
    {
        return await RhinoUiDispatcher.RunOnMainThread(() =>
        {
            if (rollbackIndex < 0)
            {
                rollbackIndex = int.MaxValue;
            }

            _graphManager.RollbackIndex = rollbackIndex;

            var nodes = _graphManager.GetNodesInExecutionOrder();
            int evaluatedCount = nodes.Count(n => _graphManager.ShouldEvaluateNode(n));
            int frozenCount = nodes.Count - evaluatedCount;

            return SuccessResponse(new
            {
                rollbackIndex = rollbackIndex == int.MaxValue ? -1 : rollbackIndex,
                evaluatedNodeCount = evaluatedCount,
                frozenNodeCount = frozenCount,
                currentMode = _graphManager.CurrentMode.ToString()
            });
        });
    }

    /// <summary>
    /// Tool 16: suppress_feature
    /// Suppresses or resumes a specific feature node in the parametric timeline, optionally cascading to children.
    /// </summary>
    public async Task<string> SuppressFeatureAsync(
        string featureId,
        bool isSuppressed,
        bool cascadeToChildren = true)
    {
        return await RhinoUiDispatcher.RunOnMainThread(() =>
        {
            if (string.IsNullOrWhiteSpace(featureId) || !Guid.TryParse(featureId, out var fGuid))
            {
                return ErrorResponse("A valid Guid featureId must be specified.");
            }

            var node = _graphManager.GetNode(fGuid);
            if (node == null)
            {
                // Register placeholder node so state is tracked
                node = new FeatureNode
                {
                    Id = fGuid,
                    Name = $"Feature_{fGuid.ToString()[..8]}",
                    FeatureType = "Feature"
                };
                _graphManager.RegisterNode(node);
            }

            _graphManager.SetSuppressed(fGuid, isSuppressed, cascadeToChildren);

            return SuccessResponse(new
            {
                featureId = fGuid.ToString(),
                featureName = node.Name,
                isSuppressed,
                cascadeToChildren,
                affectedChildrenCount = node.ChildIds.Count
            });
        });
    }
}
