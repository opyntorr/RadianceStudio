import os
import uuid
import io
import base64
import xml.etree.ElementTree as ET
from PIL import Image, ImageDraw

TABS_DATA = [
    ("Curve", "#C49F47", [
        ("Profile Placer", "_RadProfilePlacer", True),
        ("Profile Cap", "_RadProfileCap", True),
        ("Curve Boolean", "_RadCurveBoolean", False),
        ("Extract Isocurve", "! _ExtractIsocurve", True),
        ("Silhouette", "! _Silhouette", True),
        ("Extract Wireframe", "! _ExtractWireframe", True),
        ("Section", "! _Section", True),
        ("Dup Edge", "! _DupEdge", True),
        ("Project To Surface", "! _Project", True),
        ("Pull To Surface", "! _Pull", True),
        ("Curve On Surface", "! _InterpCrvOnSrf", True),
        ("Blend Curve", "! _BlendCrv", True),
        ("Offset Curve On Surface", "! _OffsetCrvOnSrf", True),
        ("Line", "! _Line", True),
        ("Polyline", "! _Polyline", True),
        ("Curve", "! _Curve", True),
        ("Circle", "! _Circle", True),
        ("Arc", "! _Arc", True),
        ("Ellipse", "! _Ellipse", True),
        ("Rectangle", "! _Rectangle", True),
        ("Polygon", "! _Polygon", True),
        ("Freehand", "! _Sketch", True),
        ("Fillet", "! _Fillet", True),
        ("Chamfer", "! _Chamfer", True),
        ("Offset", "! _Offset", True),
        ("Twin Curves", "! _TweenCurves", True),
        ("Extend", "! _Extend", True),
        ("Helix / Spiral", "! _Helix", True),
        ("Rebuild", "! _Rebuild", True),
        ("Fair", "! _Fair", True),
        ("Smooth", "! _Smooth", True),
        ("Fit Curve", "! _FitCrv", True)
    ]),
    ("Surface", "#7DB9A8", [
        ("Profile Sweep", "_RadProfileSweep", True),
        ("Sweep 2", "_RadSweep2", True),
        ("Pull Push", "_RadPullPush", False),
        ("MSR Objects", "_RadMSRObjects", False),
        ("Object On Curve", "_RadObjectOnCurve", False),
        ("Pipe", "_RadPipe", False),
        ("Rope", "_RadRope", False),
        ("Object On Surface", "_RadObjectOnSurface", False),
        ("Plane", "! _Plane", True),
        ("Extrude Planar Curve", "! _ExtrudeCrv", True),
        ("Loft", "! _Loft", True),
        ("Sweep 1", "! _Sweep1", True),
        ("Sweep 2 Native", "! _Sweep2", True),
        ("Revolve", "! _Revolve", True),
        ("Rail Revolve", "! _RailRevolve", True),
        ("Patch", "! _Patch", True),
        ("EdgeSrf", "! _EdgeSrf", True),
        ("NetworkSrf", "! _NetworkSrf", True),
        ("Offset Surface", "! _OffsetSrf", True),
        ("Fillet Surface", "! _FilletSrf", True),
        ("Chamfer Surface", "! _ChamferSrf", True),
        ("Blend Surface", "! _BlendSrf", True),
        ("Extend Surface", "! _ExtendSrf", True),
        ("Variable Fillet Srf", "! _VariableFilletSrf", True),
        ("Connect Surface", "! _ConnectSrf", True),
        ("Symmetry", "! _Symmetry", True),
        ("Merge Surface", "! _MergeSrf", True),
        ("Split Surface", "! _Split", True),
        ("Untrim", "! _Untrim", True),
        ("Shrink Trims", "! _ShrinkTrimmedSrf", True),
        ("Rebuild Surface", "! _Rebuild", True)
    ]),
    ("Solid", "#A66BD4", [
        ("Profile Sweep", "_RadProfileSweep", True),
        ("Sweep 2", "_RadSweep2", True),
        ("Pull Push", "_RadPullPush", False),
        ("Pipe", "_RadPipe", False),
        ("Rope", "_RadRope", False),
        ("Object On Surface", "_RadObjectOnSurface", False),
        ("Box", "! _Box", True),
        ("Sphere", "! _Sphere", True),
        ("Boolean", "! _BooleanUnion", True),
        ("Cylinder", "! _Cylinder", True),
        ("Cone", "! _Cone", True),
        ("Ellipsoid", "! _Ellipsoid", True),
        ("Paraboloid", "! _Paraboloid", True),
        ("Tube", "! _Tube", True),
        ("Pipe Native", "! _Pipe", True),
        ("Multi Pipe", "! _MultiPipe", True),
        ("Slab", "! _Slab", True),
        ("Torus", "! _Torus", True),
        ("Text", "! _TextObject", True),
        ("Extrude Planar Curve", "! _ExtrudeCrv Solid=Yes", True),
        ("Extrude Surface", "! _ExtrudeSrf Solid=Yes", True),
        ("Extrude Surface to...", "! _ExtrudeSrfToBoundary", True),
        ("Boss", "! _Boss", True),
        ("Rib", "! _Rib", True),
        ("Shell", "! _Shell", True),
        ("Fillet Edge", "! _FilletEdge", True),
        ("Cap Planar", "! _Cap", True),
        ("Extract Surface", "! _ExtractSrf", True),
        ("Edit Edges", "! _ChamferEdge", True),
        ("Edit Faces", "! _MoveFace", True),
        ("Edit Holes", "! _RoundHole", True),
        ("Wire Cut", "! _WireCut", True)
    ]),
    ("Tools", "#7A80D8", [
        ("Signet Advanced", "_RadSignet Advanced", True),
        ("Texture 3D", "_RadTexture3D", False),
        ("Cathedral Ring Rail", "_RadRingRail Cathedral", True),
        ("Join", "_RadJoinDynamic", False),
        ("Raster To Vector", "_RadRasterToVector", False),
        ("Text Objects", "_RadTextObjects", False),
        ("Text On Curve", "_RadTextOnCurve", False),
        ("Ring Rail", "_RadRingRail Round", True),
        ("Outside Ring Rail", "_RadOutsideRingRail", True),
        ("Bypass Ring Rail", "_RadRingRail Bypass", True),
        ("Bypass Outside Ring Rail", "_RadOutsideBypassRail", True),
        ("Split Shank", "_RadSplitShank", True),
        ("Profile Placer", "_RadProfilePlacer", True),
        ("Profile Cap", "_RadProfileCap", True),
        ("Gems In Channel", "_RadGemsInChannel", True),
        ("Signet", "_RadSignet Standard", True),
        ("Match Attributes", "_RadMatchAttributes", False),
        ("Gem Map", "_RadGemMap", False),
        ("Tech Report", "_RadBOM", True),
        ("Rebuild", "! _Rebuild", True),
        ("Ring Resizer", "_RadRingResizer", False)
    ]),
    ("Gems", "#6EA8D9", [
        ("MSR Gems", "_RadMSRGems", False),
        ("Proxy Gem", "_RadProxyGem", True),
        ("Gem Between 2 Curves", "_RadGemBetweenCurves", True),
        ("Gem List On Curve", "_RadGemListOnCurve", True),
        ("Gem Guides", "_RadGemGuides", True),
        ("Gem", "_RadGem", True),
        ("Orient to Gem", "_RadOrientToGem", True),
        ("Gem Offset Curve", "_RadGemOffsetCurve", True),
        ("Gem On Curve", "_RadGemOnCurve", True),
        ("Gem On Surface", "_RadGemOnSurface", True),
        ("Gem on Ring Rail", "_RadGemOnRingRail", True),
        ("Gem Springs", "_RadGemSprings", False),
        ("Pave", "_RadPave", True),
        ("Gem Override", "_RadGemOverride", False),
        ("Custom Gem Builder", "_RadCustomGemBuilder", False),
        ("Gem Placer", "_RadGemPlacer", True)
    ]),
    ("Settings", "#BA4E9B", [
        ("Bail", "_RadBail", False),
        ("Cluster Prong Placer", "_RadClusterProngs", True),
        ("Pave Prongs", "_RadPaveProngs", True),
        ("Pearl Cup", "_RadPearlCup", False),
        ("Prong On Surface", "_RadProngOnSurface", True),
        ("Head", "_RadProngHead", True),
        ("Prong Placer", "_RadProngPlacer", True),
        ("Halo", "_RadHalo", True),
        ("Bezel", "_RadBezel", True),
        ("Gallery Rail", "_RadGalleryRail", True),
        ("Under Bezel", "_RadUnderBezel", True)
    ]),
    ("Cutters", "#CF9B77", [
        ("Cut To Ring Rail", "_RadCutToRingRail", True),
        ("Micro Prong Cutter", "_RadMicroProngCutter", True),
        ("Channel Cutter", "_RadChannelCutter", True),
        ("Gem Cutter Library", "_RadCutterLibrary", True),
        ("Gem Cutter", "_RadCutter", True),
        ("Hinge", "_RadHinge", False),
        ("Hollow Ring", "_RadHollowRing", True),
        ("Micro Prong Cutter...", "_RadMicroProngMulti", True),
        ("Scallop Cutter", "_RadScallopCutter", False)
    ]),
    ("Transform", "#E07343", [
        ("Pull To Rail", "_RadPullToRail", True),
        ("Auto Base", "_RadAutoBase", False),
        ("Polar Array", "_RadPolarArray", True),
        ("Bend", "_RadBendDynamic", False),
        ("Mirror", "_RadMirrorDynamic", True),
        ("Smart Flow", "_RadSmartFlow", False),
        ("Smart Pattern", "_RadSmartPattern", False),
        ("Shear", "_RadShearDynamic", False),
        ("Taper", "_RadTaperDynamic", False),
        ("Twist", "_RadTwistDynamic", False),
        ("Move", "! _Move", True),
        ("Copy", "! _Copy", True),
        ("Rotate", "! _Rotate", True),
        ("Rotate 3D", "! _Rotate3D", True),
        ("Center Objects", "! _Align Centered", True),
        ("Scale", "! _Scale", True),
        ("Shear Native", "! _Shear", True),
        ("Mirror Native", "! _Mirror", True),
        ("Symmetry", "! _Symmetry", True),
        ("Align", "! _Align", True),
        ("Orient", "! _Orient", True),
        ("Array", "! _Array", True),
        ("Set Points", "! _SetPt", True),
        ("Project To C-Plane", "! _ProjectToCPlane", True),
        ("Twist Native", "! _Twist", True),
        ("Bend Native", "! _Bend", True),
        ("Taper Native", "! _Taper", True),
        ("Stretch", "! _Stretch", True),
        ("Maelstrom", "! _Maelstrom", True),
        ("Splop", "! _Splop", True),
        ("Flow", "! _Flow", True),
        ("Smooth", "! _Smooth", True),
        ("Move UVN", "! _MoveUVN", True),
        ("Soft Move", "! _SoftMove", True),
        ("Box Edit", "! _BoxEdit", True),
        ("Cage Editing", "! _CageEdit", True),
        ("Solid Points On", "! _SolidPtOn", True)
    ]),
    ("Clayoo", "#DE534A", [
        ("SubD", "! _SubDBox", True),
        ("Primitives", "! _SubDBox", True),
        ("Creation", "! _SubDSweep", True),
        ("Edit", "! _SubDEdit", True),
        ("Selection", "! _SelSubD", True),
        ("Convert", "! _ToNURBS", True),
        ("Library", "_RadSubDLibrary", False),
        ("Emboss", "! _ApplyDisplacement", True),
        ("Sculpt", "! _Smooth", True),
        ("Blueprint", "! _Picture", True),
        ("Settings", "! _SubDDisplayDensity", True)
    ]),
    ("Render", "#4A9C9B", [
        ("Render Studio", "_RadRenderStudio", False),
        ("Batch Render", "_RadBatchRender", False),
        ("Animation Studio", "_RadAnimationStudio", False),
        ("Render", "! _Render", True),
        ("Shade", "! _Shade", True),
        ("Render Preview", "! _RenderPreview", True),
        ("Lights", "! _DirectionalLight", True),
        ("Effects", "! _PostProcess", True),
        ("Properties", "! _DocumentPropertiesPage Rendering", True),
        ("Save Render Window...", "! _SaveRenderWindowAs", True)
    ]),
    ("View", "#3B5F96", [
        ("Undo View Change", "! _UndoView", True),
        ("Redo View Change", "! _RedoView", True),
        ("Restore Viewports", "! _4View", True),
        ("Surface View", "! _OrientCameraToSrf", True),
        ("Gem View", "! _Zoom _Selected", True),
        ("Pan", "! _Pan", True),
        ("Rotate", "! _RotateView", True),
        ("Zoom", "! _Zoom", True),
        ("Set Object Display...", "! _SetObjectDisplayMode", True),
        ("Show Z Buffer", "! _ShowZBuffer", True),
        ("Walk About", "! _WalkAbout", True),
        ("Clipping Plane", "! _ClippingPlane", True),
        ("New Floating View...", "! _NewFloatingViewport", True),
        ("Toggle Floating View...", "! _ToggleFloatingViewport", True),
        ("Set C-Plane", "! _CPlane", True),
        ("Set Camera", "! _Camera", True),
        ("Background Bitmap", "! _BackgroundBitmap", True),
        ("Grid Options", "! _DocumentPropertiesPage Grid", True),
        ("Capture Viewport", "! _ViewCaptureToFile", True),
        ("Viewport Properties", "! _ViewportProperties", True),
        ("Gumball Reset", "! _GumballReset", True)
    ]),
    ("Mesh", "#B5A68E", [
        ("From NURBS", "! _Mesh", True),
        ("Primitives", "! _MeshBox", True),
        ("Offset Mesh", "! _OffsetMesh", True),
        ("Apply To Surface", "! _ApplyMesh", True),
        ("Apply Mesh UVN", "! _ApplyMeshUVN", True),
        ("Extract From Closed...", "! _ExtractConnectedMeshFaces", True),
        ("From Points", "! _MeshPatch", True),
        ("Height Field", "! _Heightfield", True),
        ("From Control Polygon", "! _MeshFromControlPolygon", True),
        ("Intersect", "! _MeshIntersect", True),
        ("Patch", "! _FillMeshHoles", True),
        ("Planar Mesh", "! _MeshPolyline", True),
        ("Repair Tools", "! _MeshRepair", True),
        ("Edit Tools", "! _SplitMeshWithCurve", True),
        ("Collapse", "! _ReduceMesh", True),
        ("Extract", "! _ExtractMeshFaces", True),
        ("Boolean", "! _MeshBooleanUnion", True),
        ("Delete Faces", "! _DeleteMeshFaces", True),
        ("Triangulate Quads", "! _TriangulateMesh", True),
        ("Render Mesh Properties...", "! _DocumentPropertiesPage Mesh", True)
    ]),
    ("Dimension", "#66B26A", [
        ("Linear", "! _Dim", True),
        ("Aligned", "! _DimAligned", True),
        ("Rotated", "! _DimRotated", True),
        ("Ordinate", "! _DimOrdinate", True),
        ("Radial", "! _DimRadius", True),
        ("Diameter", "! _DimDiameter", True),
        ("Leader", "! _Leader", True),
        ("Text Block", "! _Text", True),
        ("Text Properties", "! _DocumentPropertiesPage Dimensions", True),
        ("Annotation Dot", "! _Dot", True),
        ("Hatch", "! _Hatch", True),
        ("Recenter Dim Text", "! _DimRecenterText", True),
        ("Make 2-D", "! _Make2D", True),
        ("Set Line Type", "! _SetLinetype", True),
        ("Set Line Type Scale", "! _SetLinetypeScale", True),
        ("Dim Properties", "! _OptionsPage Annotation", True)
    ]),
    ("Analyze", "#88939D", [
        ("Curve", "! _CurvatureGraph", True),
        ("Surface", "! _Zebra", True),
        ("Mass Properties", "! _Volume", True),
        ("Bounding Box", "! _BoundingBox", True),
        ("Length", "! _Length", True),
        ("Distance", "! _Distance", True),
        ("Angle", "! _Angle", True),
        ("Radius", "! _Radius", True),
        ("Edge Tools", "! _ShowEdges", True),
        ("Diagnostics", "! _Check", True),
        ("Curvature Circle", "! _Curvature", True),
        ("Direction", "! _Dir", True),
        ("Center Object", "! _VolumeCentroid", True)
    ]),
    ("Misc", "#A8B5D6", [
        ("Undo", "! _Undo", True),
        ("Redo", "! _Redo", True),
        ("Duplicate", "! _Copy InPlace", True),
        ("Edit Points On", "! _EditPtOn", True),
        ("Control Points On", "! _PointsOn", True),
        ("Move", "! _Move", True),
        ("Rotate", "! _Rotate", True),
        ("Explode", "! _Explode", True),
        ("Join", "! _Join", True),
        ("Split", "! _Split", True),
        ("Trim", "! _Trim", True),
        ("Group", "! _Group", True),
        ("Ungroup", "! _Ungroup", True),
        ("Hide", "! _Hide", True)
    ]),
    ("Manufacturing", "#FFFFFF", [
        ("Finger Hole Support Dynamic", "_RadFingerSupportDynamic", True),
        ("Sprue Dynamic", "_RadSprueDynamic", True),
        ("Claw Prong", "_RadClawProng", False),
        ("Blend Support", "_RadBlendSupport", False),
        ("Curtain", "_RadCurtainSupport", False),
        ("Finger Hole Support", "_RadFingerSupport", True),
        ("Mesh Repair", "_RadHarnessAudit", True),
        ("Sprue", "_RadSprue", True)
    ])
]

def draw_icon(size, is_implemented, name):
    img = Image.new('RGBA', (size, size), (0, 0, 0, 0))
    draw = ImageDraw.Draw(img)
    margin = 1
    rect = [margin, margin, size - 1 - margin, size - 1 - margin]
    
    if is_implemented:
        bg_color = (18, 38, 58, 240)
        border_color = (0, 180, 240, 255)
        accent_color = (0, 220, 255, 255)
    else:
        bg_color = (48, 38, 28, 220)
        border_color = (210, 150, 70, 200)
        accent_color = (230, 175, 95, 255)
        
    draw.rounded_rectangle(rect, radius=size//5, fill=bg_color, outline=border_color, width=1)
    cx, cy = size / 2.0, size / 2.0
    r = size * 0.28
    
    n_lower = name.lower()
    if 'rail' in n_lower or 'circle' in n_lower or 'round' in n_lower:
        draw.ellipse([cx - r, cy - r, cx + r, cy + r], outline=accent_color, width=max(1, size//14))
    elif 'gem' in n_lower or 'diamond' in n_lower or 'pave' in n_lower:
        pts = [(cx, cy - r), (cx + r, cy), (cx, cy + r), (cx - r, cy)]
        draw.polygon(pts, outline=accent_color, fill=(bg_color[0]+30, bg_color[1]+30, bg_color[2]+30, 200))
    elif 'prong' in n_lower or 'head' in n_lower or 'bezel' in n_lower or 'halo' in n_lower:
        draw.ellipse([cx - r*0.6, cy - r*0.6, cx + r*0.6, cy + r*0.6], outline=accent_color, width=1)
        pr = max(1, size//12)
        for dx, dy in [(-r*0.8, -r*0.8), (r*0.8, -r*0.8), (r*0.8, r*0.8), (-r*0.8, r*0.8)]:
            draw.ellipse([cx+dx-pr, cy+dy-pr, cx+dx+pr, cy+dy+pr], fill=accent_color)
    elif 'cutter' in n_lower:
        pts = [(cx - r, cy - r*0.6), (cx + r, cy - r*0.6), (cx, cy + r)]
        draw.polygon(pts, outline=accent_color)
    elif 'box' in n_lower or 'solid' in n_lower or 'extrude' in n_lower or 'mesh' in n_lower:
        draw.rectangle([cx - r, cy - r, cx + r, cy + r], outline=accent_color, width=max(1, size//14))
    else:
        draw.arc([cx - r, cy - r, cx + r, cy + r], 45, 315, fill=accent_color, width=max(1, size//12))

    if not is_implemented:
        badge_r = max(2, size // 7)
        bx, by = size - margin - badge_r - 2, margin + badge_r + 2
        draw.ellipse([bx - badge_r, by - badge_r, bx + badge_r, by + badge_r], fill=(220, 140, 40, 255), outline=(255, 200, 100, 255))
        
    return img

def build_rui():
    rui_guid = "c4a754b2-2987-43a1-9b90-1e5f8a09b431"
    group_guid = "e8b52194-4360-4963-8a02-984cd4d47150"
    
    root = ET.Element("RhinoUI", {
        "major_ver": "3",
        "minor_ver": "0",
        "guid": rui_guid,
        "localize": "False",
        "default_language_id": "1033",
        "dpi_scale": "100"
    })
    
    ET.SubElement(root, "extend_rhino_menus")
    ET.SubElement(root, "menus")
    
    tb_groups = ET.SubElement(root, "tool_bar_groups")
    tb_group = ET.SubElement(tb_groups, "tool_bar_group", {
        "guid": group_guid,
        "dock_bar_guid64": group_guid,
        "active_tool_bar_group": "a1c430e7-8cfb-4e67-8cf9-b54f9a0c8101",
        "single_file": "False",
        "hide_single_tab": "False"
    })
    g_text = ET.SubElement(tb_group, "text")
    loc = ET.SubElement(g_text, "locale_1033")
    loc.text = "Radiance Studio"
    
    toolbars_elem = ET.SubElement(root, "tool_bars")
    macros_elem = ET.SubElement(root, "macros")
    bitmaps_elem = ET.SubElement(root, "bitmaps")
    
    all_macros = []
    
    for tab_idx, (tab_name, color, tools) in enumerate(TABS_DATA):
        tb_guid = f"f1011111-1111-1111-1111-1111111111{tab_idx+1:02d}"
        group_item_guid = f"a{tab_idx+1:x}c430e7-8cfb-4e67-8cf9-b54f9a0c81{tab_idx+1:02d}"
        
        # Tool bar group item
        grp_item = ET.SubElement(tb_group, "tool_bar_group_item", {
            "guid": group_item_guid,
            "major_version": "1",
            "minor_version": "1"
        })
        gi_text = ET.SubElement(grp_item, "text")
        ET.SubElement(gi_text, "locale_1033").text = tab_name
        ET.SubElement(grp_item, "tool_bar_id").text = tb_guid
        
        # Tool bar definition
        tb_elem = ET.SubElement(toolbars_elem, "tool_bar", {"guid": tb_guid})
        tb_text = ET.SubElement(tb_elem, "text")
        ET.SubElement(tb_text, "locale_1033").text = tab_name
        
        for tool_idx, (tool_name, macro_cmd, is_impl) in enumerate(tools):
            macro_guid = f"d{tab_idx+1:x}011111-1111-1111-1111-{tool_idx+1:012d}"
            item_guid = f"c{tab_idx+1:x}011111-1111-1111-1111-{tool_idx+1:012d}"
            bitmap_idx = len(all_macros)
            all_macros.append((tool_name, is_impl))
            
            # Toolbar Item
            item_elem = ET.SubElement(tb_elem, "tool_bar_item", {"guid": item_guid})
            i_text = ET.SubElement(item_elem, "text")
            ET.SubElement(i_text, "locale_1033").text = tool_name
            ET.SubElement(item_elem, "left_macro_id").text = macro_guid
            
            # Macro Item
            macro_elem = ET.SubElement(macros_elem, "macro_item", {"guid": macro_guid})
            m_text = ET.SubElement(macro_elem, "text")
            ET.SubElement(m_text, "locale_1033").text = tool_name
            ET.SubElement(macro_elem, "script").text = macro_cmd
            ET.SubElement(macro_elem, "tooltip").text = f"{tool_name} ({macro_cmd})"
            ET.SubElement(macro_elem, "help_text").text = f"Executes {tool_name}"
            ET.SubElement(macro_elem, "button_text").text = tool_name
            ET.SubElement(macro_elem, "menu_text").text = tool_name
            ET.SubElement(macro_elem, "bitmap_id").text = f"{bitmap_idx}"
            
    # Generate unified horizontal bitmap strips
    num_tools = len(all_macros)
    print(f"Generating bitmap strips for {num_tools} tools across 16 tabs...")
    
    for size in [24, 32]:
        strip = Image.new('RGBA', (size * num_tools, size), (0, 0, 0, 0))
        for idx, (name, is_impl) in enumerate(all_macros):
            icon = draw_icon(size, is_impl, name)
            strip.paste(icon, (idx * size, 0))
            
        buf = io.BytesIO()
        strip.save(buf, format='PNG')
        b64 = base64.b64encode(buf.getvalue()).decode('ascii')
        
        bmp_elem = ET.SubElement(bitmaps_elem, "bitmap", {
            "guid": str(uuid.uuid4()),
            "dpi": "100",
            "file": "strip.png"
        })
        bmp_elem.text = b64

    rui_path = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', 'src', 'RadianceStudio.Rhino', 'RadianceStudioToolbar.rui'))
    tree = ET.ElementTree(root)
    tree.write(rui_path, encoding='utf-8', xml_declaration=True)
    print(f"[SUCCESS] Wrote complete 16-tab RUI to {rui_path}")

if __name__ == '__main__':
    build_rui()
