namespace RadianceStudio.UI.Ribbon;

using System;
using System.Collections.Generic;
using System.Linq;
using Eto.Drawing;

/// <summary>
/// Definition of a workflow tab.
/// </summary>
public record RibbonTabDefinition(
    string Id,
    string Title,
    Color IndicatorColor,
    string Description
);

/// <summary>
/// Master catalog storing all tools across the 16 commercial workflow tabs
/// in exact structural parity with the reference design.
/// </summary>
public static class RibbonToolCatalog
{
    public static readonly List<RibbonTabDefinition> Tabs = new()
    {
        new("Curve", "Curve", Color.FromArgb(0xC4, 0x9F, 0x47), "2D/3D NURBS curves, rails, and profiles"),
        new("Surface", "Surface", Color.FromArgb(0x7D, 0xB9, 0xA8), "NURBS surface modeling and sweeps"),
        new("Solid", "Solid", Color.FromArgb(0xA6, 0x6B, 0xD4), "Solid polysurfaces and booleans"),
        new("Tools", "Tools", Color.FromArgb(0x7A, 0x80, 0xD8), "Jewelry parametric rails, signets, and textures"),
        new("Gems", "Gems", Color.FromArgb(0x6E, 0xA8, 0xD9), "Stone catalogs, pavé, and curve layouts"),
        new("Settings", "Settings", Color.FromArgb(0xBA, 0x4E, 0x9B), "Prong heads, bezels, halos, and bails"),
        new("Cutters", "Cutters", Color.FromArgb(0xCF, 0x9B, 0x77), "Gem cutters, prong relief, and hollowers"),
        new("Transform", "Transform", Color.FromArgb(0xE0, 0x73, 0x43), "Deformations, arrays, flows, and alignment"),
        new("Clayoo", "Clayoo", Color.FromArgb(0xDE, 0x53, 0x4A), "SubD organic modeling and sculpting"),
        new("Render", "Render", Color.FromArgb(0x4A, 0x9C, 0x9B), "Photorealistic raytracing and turntables"),
        new("View", "View", Color.FromArgb(0x3B, 0x5F, 0x96), "Viewport navigation, CPlanes, and capture"),
        new("Mesh", "Mesh", Color.FromArgb(0xB5, 0xA6, 0x8E), "Polygonal meshes, STL prep, and repair"),
        new("Dimension", "Dimension", Color.FromArgb(0x66, 0xB2, 0x6A), "Drafting annotations and 2D projections"),
        new("Analyze", "Analyze", Color.FromArgb(0x88, 0x93, 0x9D), "Curvature, thickness, mass, and diagnostics"),
        new("Misc", "Misc", Color.FromArgb(0xA8, 0xB5, 0xD6), "Quick drafting utilities and object controls"),
        new("Manufacturing", "Manufacturing", Color.FromArgb(0xFF, 0xFF, 0xFF), "3D printing supports, sprues, and CAM")
    };

    private static readonly Dictionary<string, List<ToolDefinition>> _toolsByTab = new();
    private static readonly Dictionary<string, ToolGroupDefinition> _toolGroups = new();

    static RibbonToolCatalog()
    {
        InitializeCatalog();
    }

    public static IReadOnlyList<ToolDefinition> GetToolsForTab(string tabId)
    {
        return _toolsByTab.TryGetValue(tabId, out var list) ? list : Array.Empty<ToolDefinition>();
    }

    public static ToolGroupDefinition? GetToolGroup(string groupKey)
    {
        return _toolGroups.TryGetValue(groupKey, out var group) ? group : null;
    }

    public static IEnumerable<ToolDefinition> AllTools => _toolsByTab.Values.SelectMany(t => t);

    public static IEnumerable<ToolDefinition> Search(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return Enumerable.Empty<ToolDefinition>();

        string q = query.Trim().ToLowerInvariant();
        return AllTools
            .Where(t => t.Name.ToLowerInvariant().Contains(q) ||
                        t.Description.ToLowerInvariant().Contains(q) ||
                        t.TabId.ToLowerInvariant().Contains(q) ||
                        t.CommandOrMacro.ToLowerInvariant().Contains(q))
            .DistinctBy(t => t.Id);
    }

    private static void InitializeCatalog()
    {
        // -------------------------------------------------------------
        // TAB 1: CURVE
        // -------------------------------------------------------------
        _toolsByTab["Curve"] = new List<ToolDefinition>
        {
            new("crv_prof_placer", "Profile Placer", "Curve", ToolExecutionType.DynamicJewelry, "_RadProfilePlacer", "Dynamic station profile curve placer along rails", HasLightningBadge: true, IsImplemented: true),
            new("crv_prof_cap", "Profile Cap", "Curve", ToolExecutionType.DynamicJewelry, "_RadProfileCap", "Planar and profiled end capper for shanks", HasLightningBadge: true, IsImplemented: true),
            new("crv_boolean", "Curve Boolean", "Curve", ToolExecutionType.DynamicJewelry, "_RadCurveBoolean", "Parametric 2D curve intersection and union", HasLightningBadge: true, IsImplemented: false),
            new("crv_extract_iso", "Extract Isocurve", "Curve", ToolExecutionType.ToolGroup, "! _ExtractIsocurve", "Extracts U/V curves from surfaces", HasGroupBadge: true, GroupKey: "ExtractIsocurve"),
            new("crv_silhouette", "Silhouette", "Curve", ToolExecutionType.ToolGroup, "! _Silhouette", "Generates outline curve from view projection", HasGroupBadge: true, GroupKey: "Silhouette"),
            new("crv_extract_wire", "Extract Wireframe", "Curve", ToolExecutionType.ToolGroup, "! _ExtractWireframe", "Extracts all surface/polysurface isoparms", HasGroupBadge: true, GroupKey: "ExtractWireframe"),
            new("crv_section", "Section", "Curve", ToolExecutionType.ToolGroup, "! _Section", "Cuts cross-section curves with cutting plane", HasGroupBadge: true, GroupKey: "Section"),
            new("crv_dup_edge", "Dup Edge", "Curve", ToolExecutionType.ToolGroup, "! _DupEdge", "Duplicates boundary edges of solids/surfaces", HasGroupBadge: true, GroupKey: "DupEdge"),
            new("crv_project", "Project To Surface", "Curve", ToolExecutionType.ToolGroup, "! _Project", "Projects 2D curves onto 3D surfaces", HasGroupBadge: true, GroupKey: "Project"),
            new("crv_pull", "Pull To Surface", "Curve", ToolExecutionType.ToolGroup, "! _Pull", "Pulls curve toward surface along surface normal", HasGroupBadge: true, GroupKey: "Pull"),
            new("crv_on_srf", "Curve On Surface", "Curve", ToolExecutionType.ToolGroup, "! _InterpCrvOnSrf", "Draws interpolated spline directly on 3D surface", HasGroupBadge: true, GroupKey: "CurveOnSurface"),
            new("crv_blend", "Blend Curve", "Curve", ToolExecutionType.ToolGroup, "! _BlendCrv", "Creates smooth G1/G2 blend curve between curves", HasGroupBadge: true, GroupKey: "BlendCurve"),
            new("crv_offset_srf", "Offset Curve On Surface", "Curve", ToolExecutionType.ToolGroup, "! _OffsetCrvOnSrf", "Offsets geodesic curve across 3D surface", HasGroupBadge: true, GroupKey: "OffsetCurveOnSrf"),
            new("crv_line", "Line", "Curve", ToolExecutionType.ToolGroup, "! _Line", "Standard 2-point line", HasGroupBadge: true, GroupKey: "Line"),
            new("crv_polyline", "Polyline", "Curve", ToolExecutionType.ToolGroup, "! _Polyline", "Multi-segment polylines and closed polygons", HasGroupBadge: true, GroupKey: "Polyline"),
            new("crv_curve", "Curve", "Curve", ToolExecutionType.ToolGroup, "! _Curve", "Control point curve / interpolated curve", HasGroupBadge: true, GroupKey: "CurveGroup"),
            new("crv_circle", "Circle", "Curve", ToolExecutionType.ToolGroup, "! _Circle", "Center/Radius, 2pt, 3pt, around curve circle", HasGroupBadge: true, GroupKey: "Circle"),
            new("crv_arc", "Arc", "Curve", ToolExecutionType.ToolGroup, "! _Arc", "Center/Start/Angle, 3pt, tangent arc", HasGroupBadge: true, GroupKey: "Arc"),
            new("crv_ellipse", "Ellipse", "Curve", ToolExecutionType.ToolGroup, "! _Ellipse", "Center/Axes ellipse, deformable ellipse", HasGroupBadge: true, GroupKey: "Ellipse"),
            new("crv_rect", "Rectangle", "Curve", ToolExecutionType.ToolGroup, "! _Rectangle", "Corner-to-corner, centered, rounded rectangle", HasGroupBadge: true, GroupKey: "Rectangle"),
            new("crv_polygon", "Polygon", "Curve", ToolExecutionType.ToolGroup, "! _Polygon", "Inscribed, circumscribed, star polygons", HasGroupBadge: true, GroupKey: "Polygon"),
            new("crv_freehand", "Freehand", "Curve", ToolExecutionType.RhinoCommand, "! _Sketch", "Freehand stylus/mouse sketch curve"),
            new("crv_fillet", "Fillet", "Curve", ToolExecutionType.ToolGroup, "! _Fillet", "Fillet corners with specified radius", HasGroupBadge: true, GroupKey: "Fillet"),
            new("crv_chamfer", "Chamfer", "Curve", ToolExecutionType.ToolGroup, "! _Chamfer", "Chamfer corners with specified distances", HasGroupBadge: true, GroupKey: "Chamfer"),
            new("crv_offset", "Offset", "Curve", ToolExecutionType.ToolGroup, "! _Offset", "Parallel planar curve offset", HasGroupBadge: true, GroupKey: "Offset"),
            new("crv_twin", "Twin Curves", "Curve", ToolExecutionType.RhinoCommand, "! _TweenCurves", "Generates intermediate interpolated curves"),
            new("crv_extend", "Extend", "Curve", ToolExecutionType.ToolGroup, "! _Extend", "Extends curve to boundary or by arc length", HasGroupBadge: true, GroupKey: "Extend"),
            new("crv_helix", "Helix / Spiral", "Curve", ToolExecutionType.ToolGroup, "! _Helix", "Generates 3D spiral and helical curves", HasGroupBadge: true, GroupKey: "Helix"),
            new("crv_rebuild", "Rebuild", "Curve", ToolExecutionType.RhinoCommand, "! _Rebuild", "Re-parameterizes curve with uniform CV count"),
            new("crv_fair", "Fair", "Curve", ToolExecutionType.RhinoCommand, "! _Fair", "Removes curvature kinks while preserving ends"),
            new("crv_smooth", "Smooth", "Curve", ToolExecutionType.RhinoCommand, "! _Smooth", "Smooths jitter in polyline or CV coordinates"),
            new("crv_fit", "Fit Curve", "Curve", ToolExecutionType.RhinoCommand, "! _FitCrv", "Fits smooth NURBS curve within tolerance")
        };

        // -------------------------------------------------------------
        // TAB 2: SURFACE
        // -------------------------------------------------------------
        _toolsByTab["Surface"] = new List<ToolDefinition>
        {
            new("srf_prof_swp", "Profile Sweep", "Surface", ToolExecutionType.DynamicJewelry, "_RadProfileSweep", "Parametric 1-rail & 2-rail profile sweeper", HasLightningBadge: true, IsImplemented: true),
            new("srf_swp2", "Sweep 2", "Surface", ToolExecutionType.DynamicJewelry, "_RadSweep2", "Parametric dual rail cross-section sweeper", HasLightningBadge: true, IsImplemented: true),
            new("srf_pull_push", "Pull Push", "Surface", ToolExecutionType.DynamicJewelry, "_RadPullPush", "Interactive planar/curved face extrusion", HasLightningBadge: true, IsImplemented: false),
            new("srf_msr_objs", "MSR Objects", "Surface", ToolExecutionType.DynamicJewelry, "_RadMSRObjects", "Move/Scale/Rotate jewelry objects with Gumball", HasLightningBadge: true, IsImplemented: false),
            new("srf_obj_on_crv", "Object On Curve", "Surface", ToolExecutionType.DynamicJewelry, "_RadObjectOnCurve", "Parametric distribution of geometry along curve", HasLightningBadge: true, IsImplemented: false),
            new("srf_pipe", "Pipe", "Surface", ToolExecutionType.DynamicJewelry, "_RadPipe", "Parametric round/profile pipe with cap types", HasLightningBadge: true, IsImplemented: false),
            new("srf_rope", "Rope", "Surface", ToolExecutionType.DynamicJewelry, "_RadRope", "Twisted wire/rope parametric solid generator", HasLightningBadge: true, IsImplemented: false),
            new("srf_obj_on_srf", "Object On Surface", "Surface", ToolExecutionType.DynamicJewelry, "_RadObjectOnSurface", "Conformal UV mapping of motifs onto surfaces", HasLightningBadge: true, IsImplemented: false),
            new("srf_plane", "Plane", "Surface", ToolExecutionType.ToolGroup, "! _Plane", "3-point, rectangular, and vertical planes", HasGroupBadge: true, GroupKey: "Plane"),
            new("srf_extrude_crv", "Extrude Planar Curve", "Surface", ToolExecutionType.ToolGroup, "! _ExtrudeCrv", "Straight, tapered, and directional curve extrusion", HasGroupBadge: true, GroupKey: "ExtrudePlanarCurve"),
            new("srf_loft", "Loft", "Surface", ToolExecutionType.RhinoCommand, "! _Loft", "Skinning surface passing through curve cross-sections"),
            new("srf_sweep1", "Sweep 1", "Surface", ToolExecutionType.RhinoCommand, "! _Sweep1", "Single rail surface sweep"),
            new("srf_sweep2", "Sweep 2", "Surface", ToolExecutionType.RhinoCommand, "! _Sweep2", "Dual rail surface sweep"),
            new("srf_revolve", "Revolve", "Surface", ToolExecutionType.ToolGroup, "! _Revolve", "Revolves curve around axis of revolution", HasGroupBadge: true, GroupKey: "Revolve"),
            new("srf_rail_revolve", "Rail Revolve", "Surface", ToolExecutionType.RhinoCommand, "! _RailRevolve", "Revolves profile while tracing path curve"),
            new("srf_patch", "Patch", "Surface", ToolExecutionType.RhinoCommand, "! _Patch", "Fits surface through boundary curves and point clouds"),
            new("srf_edgesrf", "EdgeSrf", "Surface", ToolExecutionType.RhinoCommand, "! _EdgeSrf", "Creates surface from 2, 3, or 4 edge curves"),
            new("srf_networksrf", "NetworkSrf", "Surface", ToolExecutionType.RhinoCommand, "! _NetworkSrf", "High-precision surface from crossing curve network"),
            new("srf_offset_srf", "Offset Surface", "Surface", ToolExecutionType.ToolGroup, "! _OffsetSrf", "Solid or sheet surface offset", HasGroupBadge: true, GroupKey: "OffsetSurface"),
            new("srf_fillet_srf", "Fillet Surface", "Surface", ToolExecutionType.ToolGroup, "! _FilletSrf", "Constant radius rolling ball surface fillet", HasGroupBadge: true, GroupKey: "FilletSurface"),
            new("srf_chamfer_srf", "Chamfer Surface", "Surface", ToolExecutionType.ToolGroup, "! _ChamferSrf", "Beveled surface edge chamfer", HasGroupBadge: true, GroupKey: "ChamferSurface"),
            new("srf_blend_srf", "Blend Surface", "Surface", ToolExecutionType.RhinoCommand, "! _BlendSrf", "G1/G2 continuity blend between adjacent surfaces"),
            new("srf_extend_srf", "Extend Surface", "Surface", ToolExecutionType.RhinoCommand, "! _ExtendSrf", "Extends surface boundary smoothly"),
            new("srf_var_fillet_srf", "Variable Fillet Srf", "Surface", ToolExecutionType.ToolGroup, "! _VariableFilletSrf", "Variable radius fillet along surface intersection", HasGroupBadge: true, GroupKey: "VariableFilletSrf"),
            new("srf_connect_srf", "Connect Surface", "Surface", ToolExecutionType.RhinoCommand, "! _ConnectSrf", "Extends and trims surfaces to meet"),
            new("srf_symmetry", "Symmetry", "Surface", ToolExecutionType.RhinoCommand, "! _Symmetry", "Symmetrical surface modeling mirror"),
            new("srf_merge_srf", "Merge Surface", "Surface", ToolExecutionType.RhinoCommand, "! _MergeSrf", "Combines two untrimmed surfaces into one"),
            new("srf_split_srf", "Split Surface", "Surface", ToolExecutionType.RhinoCommand, "! _Split", "Trims surface using cutting curves"),
            new("srf_untrim", "Untrim", "Surface", ToolExecutionType.RhinoCommand, "! _Untrim", "Restores original underlying rectangular surface"),
            new("srf_shrink", "Shrink Trims", "Surface", ToolExecutionType.RhinoCommand, "! _ShrinkTrimmedSrf", "Tightens CV hull around trimmed boundary"),
            new("srf_rebuild_srf", "Rebuild Surface", "Surface", ToolExecutionType.RhinoCommand, "! _Rebuild", "Rebuilds surface UV knot structure")
        };

        // -------------------------------------------------------------
        // TAB 3: SOLID
        // -------------------------------------------------------------
        _toolsByTab["Solid"] = new List<ToolDefinition>
        {
            new("sld_prof_swp", "Profile Sweep", "Solid", ToolExecutionType.DynamicJewelry, "_RadProfileSweep", "Parametric solid profile sweep", HasLightningBadge: true, IsImplemented: true),
            new("sld_swp2", "Sweep 2", "Solid", ToolExecutionType.DynamicJewelry, "_RadSweep2", "Dual-rail solid profile sweep", HasLightningBadge: true, IsImplemented: true),
            new("sld_pull_push", "Pull Push", "Solid", ToolExecutionType.DynamicJewelry, "_RadPullPush", "Interactive face push-pull", HasLightningBadge: true, IsImplemented: false),
            new("sld_pipe", "Pipe", "Solid", ToolExecutionType.DynamicJewelry, "_RadPipe", "Parametric capped solid pipe", HasLightningBadge: true, IsImplemented: false),
            new("sld_rope", "Rope", "Solid", ToolExecutionType.DynamicJewelry, "_RadRope", "Twisted solid wire generator", HasLightningBadge: true, IsImplemented: false),
            new("sld_obj_on_srf", "Object On Surface", "Solid", ToolExecutionType.DynamicJewelry, "_RadObjectOnSurface", "Parametric solids conformal array", HasLightningBadge: true, IsImplemented: false),
            new("sld_box", "Box", "Solid", ToolExecutionType.ToolGroup, "! _Box", "2-point, 3-point, centered solid box", HasGroupBadge: true, GroupKey: "Box"),
            new("sld_sphere", "Sphere", "Solid", ToolExecutionType.ToolGroup, "! _Sphere", "Center/Radius, 2pt, 4pt solid sphere", HasGroupBadge: true, GroupKey: "Sphere"),
            new("sld_boolean", "Boolean", "Solid", ToolExecutionType.ToolGroup, "! _BooleanUnion", "Union, Difference, Intersection, Split flyout", HasGroupBadge: true, GroupKey: "Boolean"),
            new("sld_cylinder", "Cylinder", "Solid", ToolExecutionType.RhinoCommand, "! _Cylinder", "Base circle and height solid cylinder"),
            new("sld_cone", "Cone", "Solid", ToolExecutionType.RhinoCommand, "! _Cone", "Circular cone and truncated frustum"),
            new("sld_ellipsoid", "Ellipsoid", "Solid", ToolExecutionType.ToolGroup, "! _Ellipsoid", "Tri-axial solid ellipsoid", HasGroupBadge: true, GroupKey: "Ellipsoid"),
            new("sld_paraboloid", "Paraboloid", "Solid", ToolExecutionType.ToolGroup, "! _Paraboloid", "Parabolic solid bowl", HasGroupBadge: true, GroupKey: "Paraboloid"),
            new("sld_tube", "Tube", "Solid", ToolExecutionType.RhinoCommand, "! _Tube", "Hollow cylindrical tube"),
            new("sld_native_pipe", "Pipe", "Solid", ToolExecutionType.RhinoCommand, "! _Pipe", "Constant and variable radius solid pipe"),
            new("sld_multipipe", "Multi Pipe", "Solid", ToolExecutionType.RhinoCommand, "! _MultiPipe", "SubD branching multi-pipe network"),
            new("sld_slab", "Slab", "Solid", ToolExecutionType.RhinoCommand, "! _Slab", "Offset solid wall from polyline"),
            new("sld_torus", "Torus", "Solid", ToolExecutionType.RhinoCommand, "! _Torus", "Ring-shaped donut solid"),
            new("sld_text", "Text", "Solid", ToolExecutionType.RhinoCommand, "! _TextObject", "Extruded 3D solid typography"),
            new("sld_extrude_crv", "Extrude Planar Curve", "Solid", ToolExecutionType.ToolGroup, "! _ExtrudeCrv Solid=Yes", "Capped solid extrusion from curve", HasGroupBadge: true, GroupKey: "ExtrudePlanarCurve"),
            new("sld_extrude_srf", "Extrude Surface", "Solid", ToolExecutionType.ToolGroup, "! _ExtrudeSrf Solid=Yes", "Solid extrusion normal to surface", HasGroupBadge: true, GroupKey: "ExtrudeSurface"),
            new("sld_extrude_srf_to", "Extrude Surface to...", "Solid", ToolExecutionType.ToolGroup, "! _ExtrudeSrfToBoundary", "Extrudes surface up to target boundary", HasGroupBadge: true, GroupKey: "ExtrudeSrfTo"),
            new("sld_boss", "Boss", "Solid", ToolExecutionType.RhinoCommand, "! _Boss", "Adds planar boss feature to solid"),
            new("sld_rib", "Rib", "Solid", ToolExecutionType.RhinoCommand, "! _Rib", "Internal structural stiffener rib"),
            new("sld_shell", "Shell", "Solid", ToolExecutionType.RhinoCommand, "! _Shell", "Hollows out solid polysurface with wall thickness"),
            new("sld_fillet_edge", "Fillet Edge", "Solid", ToolExecutionType.ToolGroup, "! _FilletEdge", "Rounds sharp solid edges with radius", HasGroupBadge: true, GroupKey: "FilletEdge"),
            new("sld_cap", "Cap Planar", "Solid", ToolExecutionType.RhinoCommand, "! _Cap", "Covers planar openings to make solid"),
            new("sld_extract_srf", "Extract Surface", "Solid", ToolExecutionType.RhinoCommand, "! _ExtractSrf", "Detaches individual face from polysurface"),
            new("sld_edit_edges", "Edit Edges", "Solid", ToolExecutionType.ToolGroup, "! _ChamferEdge", "Chamfer and blend solid edges", HasGroupBadge: true, GroupKey: "EditEdges"),
            new("sld_edit_faces", "Edit Faces", "Solid", ToolExecutionType.ToolGroup, "! _MoveFace", "Moves or rotates sub-faces in solid", HasGroupBadge: true, GroupKey: "EditFaces"),
            new("sld_edit_holes", "Edit Holes", "Solid", ToolExecutionType.ToolGroup, "! _RoundHole", "Drills or modifies cylindrical holes", HasGroupBadge: true, GroupKey: "EditHoles"),
            new("sld_wirecut", "Wire Cut", "Solid", ToolExecutionType.RhinoCommand, "! _WireCut", "Slices solid using 2D curve wire")
        };

        // -------------------------------------------------------------
        // TAB 4: TOOLS
        // -------------------------------------------------------------
        _toolsByTab["Tools"] = new List<ToolDefinition>
        {
            new("tls_signet_adv", "Signet Advanced", "Tools", ToolExecutionType.DynamicJewelry, "_RadSignet Advanced", "Complete signet ring generator with top & shank", HasLightningBadge: true, IsImplemented: true),
            new("tls_texture3d", "Texture 3D", "Tools", ToolExecutionType.DynamicJewelry, "_RadTexture3D", "Procedural 3D surface texturing", HasLightningBadge: true, IsImplemented: false),
            new("tls_cath_rail", "Cathedral Ring Rail", "Tools", ToolExecutionType.DynamicJewelry, "_RadRingRail Cathedral", "Elevated cathedral arch ring rail", HasLightningBadge: true, IsImplemented: true),
            new("tls_join", "Join", "Tools", ToolExecutionType.DynamicJewelry, "_RadJoinDynamic", "Parametric join with history tracking", HasLightningBadge: true, IsImplemented: false),
            new("tls_raster_vector", "Raster To Vector", "Tools", ToolExecutionType.DynamicJewelry, "_RadRasterToVector", "Image tracing and contour vectorization", HasLightningBadge: true, IsImplemented: false),
            new("tls_text_objs", "Text Objects", "Tools", ToolExecutionType.DynamicJewelry, "_RadTextObjects", "Parametric 3D typography generator", HasLightningBadge: true, IsImplemented: false),
            new("tls_text_on_crv", "Text On Curve", "Tools", ToolExecutionType.DynamicJewelry, "_RadTextOnCurve", "Conformal curved text engraving", HasLightningBadge: true, IsImplemented: false),
            new("tls_ring_rail", "Ring Rail", "Tools", ToolExecutionType.DynamicJewelry, "_RadRingRail Round", "Standard ring rail (Round, Euro, Cushion, Oval)", HasLightningBadge: true, IsImplemented: true),
            new("tls_outside_rail", "Outside Ring Rail", "Tools", ToolExecutionType.DynamicJewelry, "_RadOutsideRingRail", "Finger clearance outer profile rail", HasLightningBadge: true, IsImplemented: true),
            new("tls_bypass_rail", "Bypass Ring Rail", "Tools", ToolExecutionType.DynamicJewelry, "_RadRingRail Bypass", "3D overlapping helical bypass rail", HasLightningBadge: true, IsImplemented: true),
            new("tls_bypass_outside", "Bypass Outside Ring Rail", "Tools", ToolExecutionType.DynamicJewelry, "_RadOutsideBypassRail", "Outer clearance rail for bypass rings", HasLightningBadge: true, IsImplemented: true),
            new("tls_split_shank", "Split Shank", "Tools", ToolExecutionType.DynamicJewelry, "_RadSplitShank", "Parametric multi-branch split shank generator", HasLightningBadge: true, IsImplemented: true),
            new("tls_prof_placer", "Profile Placer", "Tools", ToolExecutionType.DynamicJewelry, "_RadProfilePlacer", "Profile curve station placer", HasLightningBadge: true, IsImplemented: true),
            new("tls_prof_cap", "Profile Cap", "Tools", ToolExecutionType.DynamicJewelry, "_RadProfileCap", "Profile cap generator", HasLightningBadge: true, IsImplemented: true),
            new("tls_gems_channel", "Gems In Channel", "Tools", ToolExecutionType.DynamicJewelry, "_RadGemsInChannel", "Channel setting runner with seat calculation", HasLightningBadge: true, IsImplemented: true),
            new("tls_signet", "Signet", "Tools", ToolExecutionType.DynamicJewelry, "_RadSignet Standard", "Standard signet ring builder", HasLightningBadge: true, IsImplemented: true),
            new("tls_match_attr", "Match Attributes", "Tools", ToolExecutionType.DynamicJewelry, "_RadMatchAttributes", "Parametric style transfer between jewelry parts", HasLightningBadge: true, IsImplemented: false),
            new("tls_gem_map", "Gem Map", "Tools", ToolExecutionType.DynamicJewelry, "_RadGemMap", "2D stone layout projection onto 3D shank", HasLightningBadge: true, IsImplemented: false),
            new("tls_tech_report", "Tech Report", "Tools", ToolExecutionType.Specialty, "_RadBOM", "Generates technical sheet, weight, and stone BOM", IsImplemented: true),
            new("tls_rebuild", "Rebuild", "Tools", ToolExecutionType.RhinoCommand, "! _Rebuild", "Rebuilds curve geometry"),
            new("tls_ring_resizer", "Ring Resizer", "Tools", ToolExecutionType.Specialty, "_RadRingResizer", "Scales existing ring model to international sizes", IsImplemented: false)
        };

        // -------------------------------------------------------------
        // TAB 5: GEMS
        // -------------------------------------------------------------
        _toolsByTab["Gems"] = new List<ToolDefinition>
        {
            new("gem_msr", "MSR Gems", "Gems", ToolExecutionType.DynamicJewelry, "_RadMSRGems", "Move/Scale/Rotate stone clusters", HasLightningBadge: true, IsImplemented: false),
            new("gem_proxy", "Proxy Gem", "Gems", ToolExecutionType.DynamicJewelry, "_RadProxyGem", "Lightweight bounding proxies for fast modeling", HasLightningBadge: true, IsImplemented: true),
            new("gem_between_crvs", "Gem Between 2 Curves", "Gems", ToolExecutionType.DynamicJewelry, "_RadGemBetweenCurves", "Stone layout interpolated between 2 boundary rails", HasLightningBadge: true, IsImplemented: true),
            new("gem_list_crv", "Gem List On Curve", "Gems", ToolExecutionType.DynamicJewelry, "_RadGemListOnCurve", "Graduated size list stone layout along curve", HasLightningBadge: true, IsImplemented: true),
            new("gem_guides", "Gem Guides", "Gems", ToolExecutionType.DynamicJewelry, "_RadGemGuides", "Visual stone spacing and alignment guide lines", HasLightningBadge: true, IsImplemented: true),
            new("gem_single", "Gem", "Gems", ToolExecutionType.DynamicJewelry, "_RadGem", "Single stone placer with cut catalog", HasLightningBadge: true, IsImplemented: true),
            new("gem_orient", "Orient to Gem", "Gems", ToolExecutionType.DynamicJewelry, "_RadOrientToGem", "Aligns CPlane to stone table, girdle, or culet", HasLightningBadge: true, IsImplemented: true),
            new("gem_offset_crv", "Gem Offset Curve", "Gems", ToolExecutionType.DynamicJewelry, "_RadGemOffsetCurve", "Offsets curves based on stone diameter + margin", HasLightningBadge: true, IsImplemented: true),
            new("gem_on_crv", "Gem On Curve", "Gems", ToolExecutionType.DynamicJewelry, "_RadGemOnCurve", "Equispaced or graduated stone runner along curve", HasLightningBadge: true, IsImplemented: true),
            new("gem_on_srf", "Gem On Surface", "Gems", ToolExecutionType.DynamicJewelry, "_RadGemOnSurface", "Conformal stone layout mapped over UV surface", HasLightningBadge: true, IsImplemented: true),
            new("gem_on_rail", "Gem on Ring Rail", "Gems", ToolExecutionType.DynamicJewelry, "_RadGemOnRingRail", "Stones mapped directly around ring finger rail", HasLightningBadge: true, IsImplemented: true),
            new("gem_springs", "Gem Springs", "Gems", ToolExecutionType.Specialty, "_RadGemSprings", "Procedural tension ring stone suspension springs", IsImplemented: false),
            new("gem_pave", "Pave", "Gems", ToolExecutionType.Specialty, "_RadPave", "Freeform and boundary pavé stone distribution", IsImplemented: true),
            new("gem_override", "Gem Override", "Gems", ToolExecutionType.Specialty, "_RadGemOverride", "Batch replaces stone cuts, sizes, and materials", IsImplemented: false),
            new("gem_custom_builder", "Custom Gem Builder", "Gems", ToolExecutionType.Specialty, "_RadCustomGemBuilder", "Custom facet modeler for fancy fantasy cuts", IsImplemented: false),
            new("gem_placer", "Gem Placer", "Gems", ToolExecutionType.Specialty, "_RadGemPlacer", "Manual interactive stone placement and snapping", IsImplemented: true)
        };

        // -------------------------------------------------------------
        // TAB 6: SETTINGS
        // -------------------------------------------------------------
        _toolsByTab["Settings"] = new List<ToolDefinition>
        {
            new("set_bail", "Bail", "Settings", ToolExecutionType.DynamicJewelry, "_RadBail", "Pendant bail, rabbit-ear, and enhancer generator", HasLightningBadge: true, IsImplemented: false),
            new("set_cluster_prongs", "Cluster Prong Placer", "Settings", ToolExecutionType.DynamicJewelry, "_RadClusterProngs", "Shared prongs between adjacent stones in cluster", HasLightningBadge: true, IsImplemented: true),
            new("set_pave_prongs", "Pave Prongs", "Settings", ToolExecutionType.DynamicJewelry, "_RadPaveProngs", "Micro-prongs, beads, and grains for pavé fields", HasLightningBadge: true, IsImplemented: true),
            new("set_pearl_cup", "Pearl Cup", "Settings", ToolExecutionType.DynamicJewelry, "_RadPearlCup", "Pearl cup, post, and safety peg setting", HasLightningBadge: true, IsImplemented: false),
            new("set_prong_on_srf", "Prong On Surface", "Settings", ToolExecutionType.DynamicJewelry, "_RadProngOnSurface", "Prongs placed and trimmed to underlying surface", HasLightningBadge: true, IsImplemented: true),
            new("set_head", "Head", "Settings", ToolExecutionType.DynamicJewelry, "_RadProngHead", "4-prong, 6-prong, basket, V-prong head setting", HasLightningBadge: true, IsImplemented: true),
            new("set_prong_placer", "Prong Placer", "Settings", ToolExecutionType.DynamicJewelry, "_RadProngPlacer", "Adjusts individual prong angles and heights", HasLightningBadge: true, IsImplemented: true),
            new("set_halo", "Halo", "Settings", ToolExecutionType.DynamicJewelry, "_RadHalo", "Single, double, and scalloped stone halo settings", HasLightningBadge: true, IsImplemented: true),
            new("set_bezel", "Bezel", "Settings", ToolExecutionType.DynamicJewelry, "_RadBezel", "Full bezel and half bezel collar settings", HasLightningBadge: true, IsImplemented: true),
            new("set_gallery_rail", "Gallery Rail", "Settings", ToolExecutionType.DynamicJewelry, "_RadGalleryRail", "Structural under-head gallery wire and motifs", HasLightningBadge: true, IsImplemented: true),
            new("set_under_bezel", "Under Bezel", "Settings", ToolExecutionType.DynamicJewelry, "_RadUnderBezel", "Open under-bezel bearing seat collar", HasLightningBadge: true, IsImplemented: true)
        };

        // -------------------------------------------------------------
        // TAB 7: CUTTERS
        // -------------------------------------------------------------
        _toolsByTab["Cutters"] = new List<ToolDefinition>
        {
            new("cut_to_rail", "Cut To Ring Rail", "Cutters", ToolExecutionType.DynamicJewelry, "_RadCutToRingRail", "Trims shank and head interiors flush to finger rail", HasLightningBadge: true, IsImplemented: true),
            new("cut_micro_prong", "Micro Prong Cutter", "Cutters", ToolExecutionType.DynamicJewelry, "_RadMicroProngCutter", "Cuts V-notches and fishtail bead relief grooves", HasLightningBadge: true, IsImplemented: true),
            new("cut_channel", "Channel Cutter", "Cutters", ToolExecutionType.DynamicJewelry, "_RadChannelCutter", "Channel groove cutter with stone seat bearings", HasLightningBadge: true, IsImplemented: true),
            new("cut_library", "Gem Cutter Library", "Cutters", ToolExecutionType.DynamicJewelry, "_RadCutterLibrary", "Standard jeweler's burr and seat cutter shapes", HasLightningBadge: true, IsImplemented: true),
            new("cut_gem_cutter", "Gem Cutter", "Cutters", ToolExecutionType.DynamicJewelry, "_RadCutter", "Seat, girdle, pavilion, and culet booleans", HasLightningBadge: true, IsImplemented: true),
            new("cut_hinge", "Hinge", "Cutters", ToolExecutionType.DynamicJewelry, "_RadHinge", "Locket, bangle, and box clasp hinge cutter", HasLightningBadge: true, IsImplemented: false),
            new("cut_hollow_ring", "Hollow Ring", "Cutters", ToolExecutionType.DynamicJewelry, "_RadHollowRing", "Honeycomb and uniform shell interior hollower", HasLightningBadge: true, IsImplemented: true),
            new("cut_micro_multi", "Micro Prong Cutter...", "Cutters", ToolExecutionType.DynamicJewelry, "_RadMicroProngMulti", "Batch cutter execution across pavé layouts", HasLightningBadge: true, IsImplemented: true),
            new("cut_scallop", "Scallop Cutter", "Cutters", ToolExecutionType.DynamicJewelry, "_RadScallopCutter", "Scalloped French-cut bead border cutter", HasLightningBadge: true, IsImplemented: false)
        };

        // -------------------------------------------------------------
        // TAB 8: TRANSFORM
        // -------------------------------------------------------------
        _toolsByTab["Transform"] = new List<ToolDefinition>
        {
            new("trf_pull_to_rail", "Pull To Rail", "Transform", ToolExecutionType.DynamicJewelry, "_RadPullToRail", "Conforms 3D geometry onto ring rail curve", HasLightningBadge: true, IsImplemented: true),
            new("trf_auto_base", "Auto Base", "Transform", ToolExecutionType.DynamicJewelry, "_RadAutoBase", "Automatic flat base construction for 3D printing", HasLightningBadge: true, IsImplemented: false),
            new("trf_polar_array", "Polar Array", "Transform", ToolExecutionType.DynamicJewelry, "_RadPolarArray", "Parametric circular array with live count", HasLightningBadge: true, IsImplemented: true),
            new("trf_bend_dyn", "Bend", "Transform", ToolExecutionType.DynamicJewelry, "_RadBendDynamic", "Parametric arc bending of straight jewelry pieces", HasLightningBadge: true, IsImplemented: false),
            new("trf_mirror_dyn", "Mirror", "Transform", ToolExecutionType.DynamicJewelry, "_RadMirrorDynamic", "Parametric X/Y/Z symmetry mirroring with weld", HasLightningBadge: true, IsImplemented: true),
            new("trf_smart_flow", "Smart Flow", "Transform", ToolExecutionType.DynamicJewelry, "_RadSmartFlow", "Parametric planar-to-3D ring surface flow", HasLightningBadge: true, IsImplemented: false),
            new("trf_smart_pattern", "Smart Pattern", "Transform", ToolExecutionType.DynamicJewelry, "_RadSmartPattern", "Procedural filigree and lattice pattern tiling", HasLightningBadge: true, IsImplemented: false),
            new("trf_shear_dyn", "Shear", "Transform", ToolExecutionType.DynamicJewelry, "_RadShearDynamic", "Parametric angular shear deformation", HasLightningBadge: true, IsImplemented: false),
            new("trf_taper_dyn", "Taper", "Transform", ToolExecutionType.DynamicJewelry, "_RadTaperDynamic", "Parametric linear and curved taper", HasLightningBadge: true, IsImplemented: false),
            new("trf_twist_dyn", "Twist", "Transform", ToolExecutionType.DynamicJewelry, "_RadTwistDynamic", "Parametric axial twist deformation", HasLightningBadge: true, IsImplemented: false),
            new("trf_move", "Move", "Transform", ToolExecutionType.RhinoCommand, "! _Move", "Native 3D translation"),
            new("trf_copy", "Copy", "Transform", ToolExecutionType.RhinoCommand, "! _Copy", "Native duplicate translation"),
            new("trf_rotate", "Rotate", "Transform", ToolExecutionType.RhinoCommand, "! _Rotate", "2D planar rotation"),
            new("trf_rotate3d", "Rotate 3D", "Transform", ToolExecutionType.RhinoCommand, "! _Rotate3D", "3D axis rotation"),
            new("trf_center", "Center Objects", "Transform", ToolExecutionType.RhinoCommand, "! _Align Centered", "Centers objects to world origin (0,0,0)"),
            new("trf_scale", "Scale", "Transform", ToolExecutionType.ToolGroup, "! _Scale", "Scale 3D, 2D, 1D, Non-Uniform flyout", HasGroupBadge: true, GroupKey: "Scale"),
            new("trf_shear", "Shear", "Transform", ToolExecutionType.RhinoCommand, "! _Shear", "Angular skew transformation"),
            new("trf_mirror", "Mirror", "Transform", ToolExecutionType.RhinoCommand, "! _Mirror", "Standard planar reflection"),
            new("trf_symmetry", "Symmetry", "Transform", ToolExecutionType.RhinoCommand, "! _Symmetry", "Symmetrical editing"),
            new("trf_align", "Align", "Transform", ToolExecutionType.RhinoCommand, "! _Align", "Top, bottom, left, right bounding alignment"),
            new("trf_orient", "Orient", "Transform", ToolExecutionType.ToolGroup, "! _Orient", "Orient 2pt, 3pt, orient on surface flyout", HasGroupBadge: true, GroupKey: "Orient"),
            new("trf_array", "Array", "Transform", ToolExecutionType.ToolGroup, "! _Array", "Rectangular, linear, curve, polar array flyout", HasGroupBadge: true, GroupKey: "Array"),
            new("trf_setpt", "Set Points", "Transform", ToolExecutionType.RhinoCommand, "! _SetPt", "Flatten points along X, Y, or Z"),
            new("trf_project_cplane", "Project To C-Plane", "Transform", ToolExecutionType.RhinoCommand, "! _ProjectToCPlane", "Flattens geometry onto active CPlane"),
            new("trf_twist", "Twist", "Transform", ToolExecutionType.RhinoCommand, "! _Twist", "Twists geometry around axis"),
            new("trf_bend", "Bend", "Transform", ToolExecutionType.ToolGroup, "! _Bend", "Bends geometry along spine curve", HasGroupBadge: true, GroupKey: "Bend"),
            new("trf_taper", "Taper", "Transform", ToolExecutionType.RhinoCommand, "! _Taper", "Tapers geometry from start to end radius"),
            new("trf_stretch", "Stretch", "Transform", ToolExecutionType.RhinoCommand, "! _Stretch", "Stretches object along linear axis"),
            new("trf_maelstrom", "Maelstrom", "Transform", ToolExecutionType.RhinoCommand, "! _Maelstrom", "Swirls object vertices in whirlpool distortion"),
            new("trf_splop", "Splop", "Transform", ToolExecutionType.RhinoCommand, "! _Splop", "Drops 3D motif onto curved target surface"),
            new("trf_flow", "Flow", "Transform", ToolExecutionType.ToolGroup, "! _Flow", "Flow along curve, FlowAlongSrf flyout", HasGroupBadge: true, GroupKey: "Flow"),
            new("trf_smooth", "Smooth", "Transform", ToolExecutionType.RhinoCommand, "! _Smooth", "Smooths vertex positions"),
            new("trf_move_uvn", "Move UVN", "Transform", ToolExecutionType.RhinoCommand, "! _MoveUVN", "Moves control points in surface UVN space"),
            new("trf_soft_move", "Soft Move", "Transform", ToolExecutionType.RhinoCommand, "! _SoftMove", "Moves region with bell-curve falloff"),
            new("trf_box_edit", "Box Edit", "Transform", ToolExecutionType.RhinoCommand, "! _BoxEdit", "Numerical bounding box transformation panel"),
            new("trf_cage_edit", "Cage Editing", "Transform", ToolExecutionType.ToolGroup, "! _CageEdit", "Freeform deformation using control cage", HasGroupBadge: true, GroupKey: "CageEdit"),
            new("trf_solid_pts", "Solid Points On", "Transform", ToolExecutionType.RhinoCommand, "! _SolidPtOn", "Turns on grip points for solids")
        };

        // -------------------------------------------------------------
        // TAB 9: CLAYOO (SUBD)
        // -------------------------------------------------------------
        _toolsByTab["Clayoo"] = new List<ToolDefinition>
        {
            new("cly_subd", "SubD", "Clayoo", ToolExecutionType.RhinoCommand, "! _SubDBox", "SubD root modeling environment"),
            new("cly_primitives", "Primitives", "Clayoo", ToolExecutionType.ToolGroup, "! _SubDBox", "SubD Box, Sphere, Cylinder primitives", HasGroupBadge: true, GroupKey: "SubDPrimitives"),
            new("cly_creation", "Creation", "Clayoo", ToolExecutionType.ToolGroup, "! _SubDSweep", "SubD Sweep, Loft, Revolve creation", HasGroupBadge: true, GroupKey: "SubDCreation"),
            new("cly_edit", "Edit", "Clayoo", ToolExecutionType.ToolGroup, "! _SubDEdit", "SubD vertex, edge, and face editing", HasGroupBadge: true, GroupKey: "SubDEdit"),
            new("cly_selection", "Selection", "Clayoo", ToolExecutionType.ToolGroup, "! _SelSubD", "SubD filter selection tools", HasGroupBadge: true, GroupKey: "SubDSelection"),
            new("cly_convert", "Convert", "Clayoo", ToolExecutionType.ToolGroup, "! _ToNURBS", "SubD to NURBS and NURBS to SubD conversion", HasGroupBadge: true, GroupKey: "SubDConvert"),
            new("cly_library", "Library", "Clayoo", ToolExecutionType.Specialty, "_RadSubDLibrary", "Organic mesh component parts library", IsImplemented: false),
            new("cly_emboss", "Emboss", "Clayoo", ToolExecutionType.ToolGroup, "! _ApplyDisplacement", "Relief texture embossing onto organic meshes", HasGroupBadge: true, GroupKey: "Emboss"),
            new("cly_sculpt", "Sculpt", "Clayoo", ToolExecutionType.ToolGroup, "! _Smooth", "Digital sculpting brush tools", HasGroupBadge: true, GroupKey: "Sculpt"),
            new("cly_blueprint", "Blueprint", "Clayoo", ToolExecutionType.RhinoCommand, "! _Picture", "Reference background images setup"),
            new("cly_settings", "Settings", "Clayoo", ToolExecutionType.ToolGroup, "! _SubDDisplayDensity", "SubD display density and smoothness", HasGroupBadge: true, GroupKey: "SubDSettings")
        };

        // -------------------------------------------------------------
        // TAB 10: RENDER
        // -------------------------------------------------------------
        _toolsByTab["Render"] = new List<ToolDefinition>
        {
            new("rnd_studio", "Render Studio", "Render", ToolExecutionType.Specialty, "_RadRenderStudio", "Full photorealistic jewelry rendering studio", IsImplemented: false),
            new("rnd_batch", "Batch Render", "Render", ToolExecutionType.Specialty, "_RadBatchRender", "Multi-angle turntable and catalog batch renderer", IsImplemented: false),
            new("rnd_anim", "Animation Studio", "Render", ToolExecutionType.Specialty, "_RadAnimationStudio", "360-degree jewelry turntable animation creator", IsImplemented: false),
            new("rnd_render", "Render", "Render", ToolExecutionType.RhinoCommand, "! _Render", "Rhino native photorealistic renderer"),
            new("rnd_shade", "Shade", "Render", ToolExecutionType.RhinoCommand, "! _Shade", "Quick shade viewport preview"),
            new("rnd_preview", "Render Preview", "Render", ToolExecutionType.RhinoCommand, "! _RenderPreview", "Interactive raytraced viewport window"),
            new("rnd_lights", "Lights", "Render", ToolExecutionType.ToolGroup, "! _DirectionalLight", "Directional, Point, Spot, Rectangular lights", HasGroupBadge: true, GroupKey: "Lights"),
            new("rnd_effects", "Effects", "Render", ToolExecutionType.ToolGroup, "! _PostProcess", "Post-processing bloom, caustics, depth of field", HasGroupBadge: true, GroupKey: "RenderEffects"),
            new("rnd_properties", "Properties", "Render", ToolExecutionType.RhinoCommand, "! _DocumentPropertiesPage Rendering", "Render environment and ground plane settings"),
            new("rnd_save_window", "Save Render Window...", "Render", ToolExecutionType.RhinoCommand, "! _SaveRenderWindowAs", "Exports render frame to high-res TIFF/PNG")
        };

        // -------------------------------------------------------------
        // TAB 11: VIEW
        // -------------------------------------------------------------
        _toolsByTab["View"] = new List<ToolDefinition>
        {
            new("viw_undo_view", "Undo View Change", "View", ToolExecutionType.RhinoCommand, "! _UndoView", "Reverts camera view move"),
            new("viw_redo_view", "Redo View Change", "View", ToolExecutionType.RhinoCommand, "! _RedoView", "Restores next camera view move"),
            new("viw_restore_views", "Restore Viewports", "View", ToolExecutionType.RhinoCommand, "! _4View", "Resets standard 4-viewport layout"),
            new("viw_srf_view", "Surface View", "View", ToolExecutionType.RhinoCommand, "! _OrientCameraToSrf", "Aligns view orthogonal to surface normal"),
            new("viw_gem_view", "Gem View", "View", ToolExecutionType.RhinoCommand, "! _Zoom _Selected", "Focuses and centers camera on selected gem"),
            new("viw_pan", "Pan", "View", ToolExecutionType.RhinoCommand, "! _Pan", "Viewport pan navigation"),
            new("viw_rotate", "Rotate", "View", ToolExecutionType.RhinoCommand, "! _RotateView", "Viewport camera orbit"),
            new("viw_zoom", "Zoom", "View", ToolExecutionType.ToolGroup, "! _Zoom", "Zoom Selected, Extents, Window, Dynamic", HasGroupBadge: true, GroupKey: "Zoom"),
            new("viw_obj_display", "Set Object Display...", "View", ToolExecutionType.RhinoCommand, "! _SetObjectDisplayMode", "Per-object display mode override"),
            new("viw_zbuffer", "Show Z Buffer", "View", ToolExecutionType.RhinoCommand, "! _ShowZBuffer", "Displays viewport depth buffer"),
            new("viw_walkabout", "Walk About", "View", ToolExecutionType.RhinoCommand, "! _WalkAbout", "First-person camera navigation"),
            new("viw_clipping", "Clipping Plane", "View", ToolExecutionType.RhinoCommand, "! _ClippingPlane", "Cross-section clipping plane"),
            new("viw_new_floating", "New Floating View...", "View", ToolExecutionType.RhinoCommand, "! _NewFloatingViewport", "Creates floating viewport window"),
            new("viw_toggle_floating", "Toggle Floating View...", "View", ToolExecutionType.RhinoCommand, "! _ToggleFloatingViewport", "Toggles floating state"),
            new("viw_set_cplane", "Set C-Plane", "View", ToolExecutionType.ToolGroup, "! _CPlane", "Top, Front, Right, Surface, 3Pt C-Planes", HasGroupBadge: true, GroupKey: "SetCPlane"),
            new("viw_set_camera", "Set Camera", "View", ToolExecutionType.ToolGroup, "! _Camera", "Camera position, target, focal length", HasGroupBadge: true, GroupKey: "SetCamera"),
            new("viw_bg_bitmap", "Background Bitmap", "View", ToolExecutionType.ToolGroup, "! _BackgroundBitmap", "Place and scale reference sketch bitmaps", HasGroupBadge: true, GroupKey: "BackgroundBitmap"),
            new("viw_grid_opts", "Grid Options", "View", ToolExecutionType.RhinoCommand, "! _DocumentPropertiesPage Grid", "Grid spacing and snap settings"),
            new("viw_capture_vp", "Capture Viewport", "View", ToolExecutionType.ToolGroup, "! _ViewCaptureToFile", "Screenshot viewport to clipboard or file", HasGroupBadge: true, GroupKey: "CaptureViewport"),
            new("viw_vp_props", "Viewport Properties", "View", ToolExecutionType.RhinoCommand, "! _ViewportProperties", "Viewport FOV, lens, and projection"),
            new("viw_gumball_reset", "Gumball Reset", "View", ToolExecutionType.RhinoCommand, "! _GumballReset", "Resets Gumball alignment to CPlane or World")
        };

        // -------------------------------------------------------------
        // TAB 12: MESH
        // -------------------------------------------------------------
        _toolsByTab["Mesh"] = new List<ToolDefinition>
        {
            new("msh_from_nurbs", "From NURBS", "Mesh", ToolExecutionType.RhinoCommand, "! _Mesh", "Converts NURBS polysurface to polygon mesh"),
            new("msh_primitives", "Primitives", "Mesh", ToolExecutionType.ToolGroup, "! _MeshBox", "Mesh Box, Sphere, Cylinder, Cone", HasGroupBadge: true, GroupKey: "MeshPrimitives"),
            new("msh_offset", "Offset Mesh", "Mesh", ToolExecutionType.RhinoCommand, "! _OffsetMesh", "Solid shell offset for casting"),
            new("msh_apply_srf", "Apply To Surface", "Mesh", ToolExecutionType.RhinoCommand, "! _ApplyMesh", "Wraps mesh texture onto 3D NURBS surface"),
            new("msh_apply_uvn", "Apply Mesh UVN", "Mesh", ToolExecutionType.RhinoCommand, "! _ApplyMeshUVN", "Maps UVN vertex displacements"),
            new("msh_extract_closed", "Extract From Closed...", "Mesh", ToolExecutionType.RhinoCommand, "! _ExtractConnectedMeshFaces", "Extracts connected mesh face subsets"),
            new("msh_from_pts", "From Points", "Mesh", ToolExecutionType.RhinoCommand, "! _MeshPatch", "Delaunay triangulation from 3D point cloud"),
            new("msh_heightfield", "Height Field", "Mesh", ToolExecutionType.RhinoCommand, "! _Heightfield", "3D displacement mesh from grayscale heightmap"),
            new("msh_control_poly", "From Control Polygon", "Mesh", ToolExecutionType.RhinoCommand, "! _MeshFromControlPolygon", "Creates cage mesh from NURBS control points"),
            new("msh_intersect", "Intersect", "Mesh", ToolExecutionType.RhinoCommand, "! _MeshIntersect", "Mesh-mesh intersection 3D polylines"),
            new("msh_patch", "Patch", "Mesh", ToolExecutionType.RhinoCommand, "! _FillMeshHoles", "Fills holes and gaps in polygonal mesh"),
            new("msh_planar", "Planar Mesh", "Mesh", ToolExecutionType.RhinoCommand, "! _MeshPolyline", "2D planar polygon meshing"),
            new("msh_repair", "Repair Tools", "Mesh", ToolExecutionType.ToolGroup, "! _MeshRepair", "Fill holes, unify normals, delete non-manifold", HasGroupBadge: true, GroupKey: "MeshRepair"),
            new("msh_edit", "Edit Tools", "Mesh", ToolExecutionType.ToolGroup, "! _SplitMeshWithCurve", "Split mesh, weld vertices, smooth mesh", HasGroupBadge: true, GroupKey: "MeshEdit"),
            new("msh_collapse", "Collapse", "Mesh", ToolExecutionType.ToolGroup, "! _ReduceMesh", "Decimates and simplifies mesh polygon count", HasGroupBadge: true, GroupKey: "MeshCollapse"),
            new("msh_extract", "Extract", "Mesh", ToolExecutionType.ToolGroup, "! _ExtractMeshFaces", "Extracts mesh edges, borders, faces", HasGroupBadge: true, GroupKey: "MeshExtract"),
            new("msh_boolean", "Boolean", "Mesh", ToolExecutionType.ToolGroup, "! _MeshBooleanUnion", "Mesh Union, Difference, Intersection", HasGroupBadge: true, GroupKey: "MeshBoolean"),
            new("msh_del_faces", "Delete Faces", "Mesh", ToolExecutionType.RhinoCommand, "! _DeleteMeshFaces", "Interactive face deletion"),
            new("msh_triangulate", "Triangulate Quads", "Mesh", ToolExecutionType.RhinoCommand, "! _TriangulateMesh", "Converts quads to triangles for 3D printing"),
            new("msh_render_props", "Render Mesh Properties...", "Mesh", ToolExecutionType.RhinoCommand, "! _DocumentPropertiesPage Mesh", "Fine render mesh density tuning")
        };

        // -------------------------------------------------------------
        // TAB 13: DIMENSION
        // -------------------------------------------------------------
        _toolsByTab["Dimension"] = new List<ToolDefinition>
        {
            new("dim_linear", "Linear", "Dimension", ToolExecutionType.RhinoCommand, "! _Dim", "Linear horizontal/vertical dimension"),
            new("dim_aligned", "Aligned", "Dimension", ToolExecutionType.RhinoCommand, "! _DimAligned", "Aligned dimensioning along angled edges"),
            new("dim_rotated", "Rotated", "Dimension", ToolExecutionType.RhinoCommand, "! _DimRotated", "Rotated dimensioning"),
            new("dim_ordinate", "Ordinate", "Dimension", ToolExecutionType.RhinoCommand, "! _DimOrdinate", "X/Y datum ordinate dimensioning"),
            new("dim_radial", "Radial", "Dimension", ToolExecutionType.RhinoCommand, "! _DimRadius", "Radius measurement callout"),
            new("dim_diameter", "Diameter", "Dimension", ToolExecutionType.RhinoCommand, "! _DimDiameter", "Diameter measurement callout"),
            new("dim_leader", "Leader", "Dimension", ToolExecutionType.RhinoCommand, "! _Leader", "Annotation arrow leader note"),
            new("dim_text_block", "Text Block", "Dimension", ToolExecutionType.RhinoCommand, "! _Text", "Multi-line jewelry technical notes"),
            new("dim_text_props", "Text Properties", "Dimension", ToolExecutionType.RhinoCommand, "! _DocumentPropertiesPage Dimensions", "Typography and dimension style settings"),
            new("dim_dot", "Annotation Dot", "Dimension", ToolExecutionType.RhinoCommand, "! _Dot", "Viewport billboard coordinate dots"),
            new("dim_hatch", "Hatch", "Dimension", ToolExecutionType.RhinoCommand, "! _Hatch", "Crosshatching and technical section patterns"),
            new("dim_recenter", "Recenter Dim Text", "Dimension", ToolExecutionType.RhinoCommand, "! _DimRecenterText", "Centers dimension text between ticks"),
            new("dim_make2d", "Make 2-D", "Dimension", ToolExecutionType.RhinoCommand, "! _Make2D", "2D drafting projection with hidden lines"),
            new("dim_set_ltype", "Set Line Type", "Dimension", ToolExecutionType.RhinoCommand, "! _SetLinetype", "Continuous, dashed, dotted line styles"),
            new("dim_set_ltscale", "Set Line Type Scale", "Dimension", ToolExecutionType.RhinoCommand, "! _SetLinetypeScale", "Global linetype scale factor"),
            new("dim_props", "Dim Properties", "Dimension", ToolExecutionType.RhinoCommand, "! _OptionsPage Annotation", "Dimension styles manager")
        };

        // -------------------------------------------------------------
        // TAB 14: ANALYZE
        // -------------------------------------------------------------
        _toolsByTab["Analyze"] = new List<ToolDefinition>
        {
            new("anz_curve", "Curve", "Analyze", ToolExecutionType.ToolGroup, "! _CurvatureGraph", "Curvature graph, deviation, continuity flyout", HasGroupBadge: true, GroupKey: "AnalyzeCurve"),
            new("anz_surface", "Surface", "Analyze", ToolExecutionType.ToolGroup, "! _Zebra", "Draft angle, zebra stripes, thickness flyout", HasGroupBadge: true, GroupKey: "AnalyzeSurface"),
            new("anz_mass", "Mass Properties", "Analyze", ToolExecutionType.ToolGroup, "! _Volume", "Volume, area, centroid, moments of inertia", HasGroupBadge: true, GroupKey: "MassProperties"),
            new("anz_bbox", "Bounding Box", "Analyze", ToolExecutionType.RhinoCommand, "! _BoundingBox", "Minimal 3D oriented bounding box"),
            new("anz_length", "Length", "Analyze", ToolExecutionType.RhinoCommand, "! _Length", "Precise curve perimeter measurement"),
            new("anz_distance", "Distance", "Analyze", ToolExecutionType.RhinoCommand, "! _Distance", "Point-to-point 3D distance"),
            new("anz_angle", "Angle", "Analyze", ToolExecutionType.RhinoCommand, "! _Angle", "Angle between vectors or planes"),
            new("anz_radius", "Radius", "Analyze", ToolExecutionType.RhinoCommand, "! _Radius", "Fillet or surface curvature radius"),
            new("anz_edges", "Edge Tools", "Analyze", ToolExecutionType.ToolGroup, "! _ShowEdges", "Show naked edges, non-manifold edges", HasGroupBadge: true, GroupKey: "EdgeTools"),
            new("anz_diagnostics", "Diagnostics", "Analyze", ToolExecutionType.ToolGroup, "! _Check", "Check bad objects, validity audit", HasGroupBadge: true, GroupKey: "Diagnostics"),
            new("anz_curv_circle", "Curvature Circle", "Analyze", ToolExecutionType.RhinoCommand, "! _Curvature", "Interactive curvature osculating circle"),
            new("anz_direction", "Direction", "Analyze", ToolExecutionType.RhinoCommand, "! _Dir", "Normal vector display and flip"),
            new("anz_center", "Center Object", "Analyze", ToolExecutionType.RhinoCommand, "! _VolumeCentroid", "Locate bounding center of geometry")
        };

        // -------------------------------------------------------------
        // TAB 15: MISC
        // -------------------------------------------------------------
        _toolsByTab["Misc"] = new List<ToolDefinition>
        {
            new("msc_undo", "Undo", "Misc", ToolExecutionType.RhinoCommand, "! _Undo", "Undo last operation"),
            new("msc_redo", "Redo", "Misc", ToolExecutionType.RhinoCommand, "! _Redo", "Redo previously undone operation"),
            new("msc_duplicate", "Duplicate", "Misc", ToolExecutionType.RhinoCommand, "! _Copy InPlace", "In-place object duplicate"),
            new("msc_edit_pts", "Edit Points On", "Misc", ToolExecutionType.RhinoCommand, "! _EditPtOn", "Turn on curve edit points"),
            new("msc_ctrl_pts", "Control Points On", "Misc", ToolExecutionType.RhinoCommand, "! _PointsOn", "Turn on NURBS CV control points"),
            new("msc_move", "Move", "Misc", ToolExecutionType.RhinoCommand, "! _Move", "Move objects in 3D"),
            new("msc_rotate", "Rotate", "Misc", ToolExecutionType.RhinoCommand, "! _Rotate", "Rotate objects"),
            new("msc_explode", "Explode", "Misc", ToolExecutionType.RhinoCommand, "! _Explode", "Explodes polysurface/curve into components"),
            new("msc_join", "Join", "Misc", ToolExecutionType.RhinoCommand, "! _Join", "Joins contiguous curves/surfaces"),
            new("msc_split", "Split", "Misc", ToolExecutionType.RhinoCommand, "! _Split", "Splits objects using cutting geometry"),
            new("msc_trim", "Trim", "Misc", ToolExecutionType.RhinoCommand, "! _Trim", "Trims portions of objects"),
            new("msc_group", "Group", "Misc", ToolExecutionType.RhinoCommand, "! _Group", "Groups selected objects"),
            new("msc_ungroup", "Ungroup", "Misc", ToolExecutionType.RhinoCommand, "! _Ungroup", "Disassembles group hierarchy"),
            new("msc_hide", "Hide", "Misc", ToolExecutionType.RhinoCommand, "! _Hide", "Hides selected objects from viewport")
        };

        // -------------------------------------------------------------
        // TAB 16: MANUFACTURING
        // -------------------------------------------------------------
        _toolsByTab["Manufacturing"] = new List<ToolDefinition>
        {
            new("mfg_finger_dyn", "Finger Hole Support Dynamic", "Manufacturing", ToolExecutionType.DynamicJewelry, "_RadFingerSupportDynamic", "Parametric finger arbor support structure for resin 3D printing", HasLightningBadge: true, IsImplemented: true),
            new("mfg_sprue_dyn", "Sprue Dynamic", "Manufacturing", ToolExecutionType.DynamicJewelry, "_RadSprueDynamic", "Parametric casting sprue runner, main feeder, and gates", HasLightningBadge: true, IsImplemented: true),
            new("mfg_claw_prong", "Claw Prong", "Manufacturing", ToolExecutionType.Specialty, "_RadClawProng", "CAM claw prong reinforcement", IsImplemented: false),
            new("mfg_blend_supp", "Blend Support", "Manufacturing", ToolExecutionType.Specialty, "_RadBlendSupport", "Smooth support strut contact blends", IsImplemented: false),
            new("mfg_curtain", "Curtain", "Manufacturing", ToolExecutionType.Specialty, "_RadCurtainSupport", "Continuous thin-wall support curtain for stone overhangs", IsImplemented: false),
            new("mfg_finger_supp", "Finger Hole Support", "Manufacturing", ToolExecutionType.Specialty, "_RadFingerSupport", "Static finger arbor ring support", IsImplemented: true),
            new("mfg_mesh_repair", "Mesh Repair", "Manufacturing", ToolExecutionType.Specialty, "_RadHarnessAudit", "Automated watertight mesh repair and STL manifold validation", IsImplemented: true),
            new("mfg_sprue", "Sprue", "Manufacturing", ToolExecutionType.Specialty, "_RadSprue", "Static investment casting sprue network", IsImplemented: true)
        };

        // -------------------------------------------------------------
        // INITIALIZE FLYOUT TOOL GROUPS
        // -------------------------------------------------------------
        InitializeToolGroups();
    }

    private static void InitializeToolGroups()
    {
        // Line Group
        _toolGroups["Line"] = new ToolGroupDefinition("Line", "Line Commands", new[]
        {
            new ToolDefinition("grp_line_2pt", "Single Line", "Curve", ToolExecutionType.RhinoCommand, "! _Line", "Standard 2-point line"),
            new ToolDefinition("grp_line_segments", "Line Segments", "Curve", ToolExecutionType.RhinoCommand, "! _Lines", "Connected line segments"),
            new ToolDefinition("grp_line_mid", "Line From Midpoint", "Curve", ToolExecutionType.RhinoCommand, "! _Line _MidPoint", "Line drawn outward from center"),
            new ToolDefinition("grp_line_4pt", "Line 4-Point", "Curve", ToolExecutionType.RhinoCommand, "! _Line _FourPoint", "Line bisecting four points"),
            new ToolDefinition("grp_line_perp", "Perpendicular Line", "Curve", ToolExecutionType.RhinoCommand, "! _Line _Perpendicular", "Line perpendicular to curve"),
            new ToolDefinition("grp_line_tan", "Tangent Line", "Curve", ToolExecutionType.RhinoCommand, "! _Line _Tangent", "Line tangent to curve"),
            new ToolDefinition("grp_line_normal", "Normal to Surface", "Curve", ToolExecutionType.RhinoCommand, "! _Line _Normal", "Line normal to 3D surface")
        });

        // Circle Group
        _toolGroups["Circle"] = new ToolGroupDefinition("Circle", "Circle Commands", new[]
        {
            new ToolDefinition("grp_cir_radius", "Center, Radius", "Curve", ToolExecutionType.RhinoCommand, "! _Circle", "Circle from center and radius"),
            new ToolDefinition("grp_cir_2pt", "2-Point Circle", "Curve", ToolExecutionType.RhinoCommand, "! _Circle _2Point", "Circle passing through 2 diameter points"),
            new ToolDefinition("grp_cir_3pt", "3-Point Circle", "Curve", ToolExecutionType.RhinoCommand, "! _Circle _3Point", "Circle passing through 3 circumference points"),
            new ToolDefinition("grp_cir_tan", "Tangent to Curves", "Curve", ToolExecutionType.RhinoCommand, "! _Circle _Tangent", "Circle tangent to multiple curves"),
            new ToolDefinition("grp_cir_around_crv", "Around Curve", "Curve", ToolExecutionType.RhinoCommand, "! _Circle _AroundCurve", "Circle oriented perpendicular around curve")
        });

        // Arc Group
        _toolGroups["Arc"] = new ToolGroupDefinition("Arc", "Arc Commands", new[]
        {
            new ToolDefinition("grp_arc_cen_ang", "Center, Start, Angle", "Curve", ToolExecutionType.RhinoCommand, "! _Arc", "Arc from center, start point, and angle"),
            new ToolDefinition("grp_arc_3pt", "3-Point Arc", "Curve", ToolExecutionType.RhinoCommand, "! _Arc _3Point", "Arc passing through 3 points"),
            new ToolDefinition("grp_arc_start_end_dir", "Start, End, Direction", "Curve", ToolExecutionType.RhinoCommand, "! _Arc _StartPoint", "Arc by start, end, and tangent direction")
        });

        // Rectangle Group
        _toolGroups["Rectangle"] = new ToolGroupDefinition("Rectangle", "Rectangle Commands", new[]
        {
            new ToolDefinition("grp_rect_corner", "Corner to Corner", "Curve", ToolExecutionType.RhinoCommand, "! _Rectangle", "Corner to corner rectangle"),
            new ToolDefinition("grp_rect_center", "Centered Rectangle", "Curve", ToolExecutionType.RhinoCommand, "! _Rectangle _Center", "Centered rectangular boundary"),
            new ToolDefinition("grp_rect_rounded", "Rounded Rectangle", "Curve", ToolExecutionType.RhinoCommand, "! _Rectangle _Rounded", "Rectangle with filleted corners"),
            new ToolDefinition("grp_rect_3pt", "3-Point Rectangle", "Curve", ToolExecutionType.RhinoCommand, "! _Rectangle _3Point", "Rectangle aligned to 3 points")
        });

        // Boolean Group
        _toolGroups["Boolean"] = new ToolGroupDefinition("Boolean", "Solid Booleans", new[]
        {
            new ToolDefinition("grp_bool_union", "Boolean Union", "Solid", ToolExecutionType.RhinoCommand, "! _BooleanUnion", "Combines multiple solids into one"),
            new ToolDefinition("grp_bool_diff", "Boolean Difference", "Solid", ToolExecutionType.RhinoCommand, "! _BooleanDifference", "Subtracts cutters from base solid"),
            new ToolDefinition("grp_bool_intersect", "Boolean Intersection", "Solid", ToolExecutionType.RhinoCommand, "! _BooleanIntersection", "Keeps shared overlapping volume"),
            new ToolDefinition("grp_bool_split", "Boolean Split", "Solid", ToolExecutionType.RhinoCommand, "! _BooleanSplit", "Splits solids along intersection surfaces"),
            new ToolDefinition("grp_bool_2obj", "Boolean 2 Objects", "Solid", ToolExecutionType.RhinoCommand, "! _Boolean2Objects", "Cycles through boolean combinations")
        });

        // Scale Group
        _toolGroups["Scale"] = new ToolGroupDefinition("Scale", "Scale Commands", new[]
        {
            new ToolDefinition("grp_scale_3d", "Scale 3D", "Transform", ToolExecutionType.RhinoCommand, "! _Scale", "Uniform 3D scale"),
            new ToolDefinition("grp_scale_2d", "Scale 2D", "Transform", ToolExecutionType.RhinoCommand, "! _Scale2D", "Planar 2D scale"),
            new ToolDefinition("grp_scale_1d", "Scale 1D", "Transform", ToolExecutionType.RhinoCommand, "! _Scale1D", "Uniaxial 1D scale"),
            new ToolDefinition("grp_scale_nu", "Scale Non-Uniform", "Transform", ToolExecutionType.RhinoCommand, "! _ScaleNU", "Independent X, Y, Z scale factors")
        });

        // Orient Group
        _toolGroups["Orient"] = new ToolGroupDefinition("Orient", "Orient Commands", new[]
        {
            new ToolDefinition("grp_orient_2pt", "Orient 2-Point", "Transform", ToolExecutionType.RhinoCommand, "! _Orient", "Transforms reference points to target points"),
            new ToolDefinition("grp_orient_3pt", "Orient 3-Point", "Transform", ToolExecutionType.RhinoCommand, "! _Orient3Pt", "Full 3D orientation alignment"),
            new ToolDefinition("grp_orient_on_srf", "Orient On Surface", "Transform", ToolExecutionType.RhinoCommand, "! _OrientOnSrf", "Snaps and orients objects onto 3D surface")
        });

        // Fillet Edge Group
        _toolGroups["FilletEdge"] = new ToolGroupDefinition("FilletEdge", "Solid Edge Filleting", new[]
        {
            new ToolDefinition("grp_fillet_edge", "Fillet Edge", "Solid", ToolExecutionType.RhinoCommand, "! _FilletEdge", "Rounds solid edges with circular arc"),
            new ToolDefinition("grp_chamfer_edge", "Chamfer Edge", "Solid", ToolExecutionType.RhinoCommand, "! _ChamferEdge", "Bevels solid edges with flat planar cuts"),
            new ToolDefinition("grp_blend_edge", "Blend Edge", "Solid", ToolExecutionType.RhinoCommand, "! _BlendEdge", "Smooth G2 curvature continuous edge blend")
        });
    }
}
