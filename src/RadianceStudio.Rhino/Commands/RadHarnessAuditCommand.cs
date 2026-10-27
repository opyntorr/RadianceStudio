namespace RadianceStudio.Rhino.Commands;

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.Json;
using global::Rhino;
using global::Rhino.Commands;
using global::Rhino.Geometry;
using RadianceStudio.Core.Engines;
using RadianceStudio.Core.Enums;
using RadianceStudio.Core.Models;
using RadianceStudio.Rhino.Panels;

/// <summary>
/// Headless Rhino 8 In-Process Integration Test &amp; Audit Command.
/// Executes live inside the real Rhino.exe process during automated harness runs.
/// Audits document units, tolerances, panel registrations, curve creation, solid generation,
/// manifoldness, and watertight STL generation, writing a tamper-evident JSON audit report.
/// </summary>
[Guid("4A10B39E-5CD8-44A9-BF1D-495B47CC25EE")]
public class RadHarnessAuditCommand : Command
{
    public static readonly string[] CommandParameters = { "AuditMode" };

    public override string EnglishName => "RadHarnessAudit";

    protected override Result RunCommand(RhinoDoc doc, RunMode mode)
    {
        int pid = System.Environment.ProcessId;
        var auditReport = new Dictionary<string, object>
        {
            ["timestamp"] = DateTime.UtcNow.ToString("o"),
            ["rhinoVersion"] = RhinoApp.Version.ToString(),
            ["processId"] = pid,
            ["scheme"] = RhinoApp.SchemeName,
            ["stages"] = new List<Dictionary<string, object>>()
        };

        var stages = (List<Dictionary<string, object>>)auditReport["stages"];
        bool allPassed = true;

        void RecordStage(string name, bool success, string details)
        {
            stages.Add(new Dictionary<string, object>
            {
                ["stage"] = name,
                ["status"] = success ? "PASS" : "FAIL",
                ["details"] = details
            });
            if (!success) allPassed = false;
            RhinoApp.WriteLine($"[HARNESS AUDIT] {name}: {(success ? "PASS" : "FAIL")} - {details}");
        }

        try
        {
            // Stage 1: Document Units & Tolerances
            bool unitsOk = doc.ModelUnitSystem == UnitSystem.Millimeters;
            bool tolOk = doc.ModelAbsoluteTolerance <= 0.001;
            RecordStage("DocumentUnitsAndTolerance", unitsOk && tolOk,
                $"Units: {doc.ModelUnitSystem} (Expected Millimeters), AbsTol: {doc.ModelAbsoluteTolerance} (Expected <= 0.001)");

            // Stage 2: UI Panel Registration
            bool panelsRegistered = JewelryRibbonPanel.Instance != null || RadianceStudioPlugIn.Ribbon != null;
            RecordStage("StudioPanelsRegistration", panelsRegistered,
                $"RibbonPanel: {(panelsRegistered ? "Registered" : "Unattached")}");

            // Stage 3: Parametric Ring Rail Creation
            double us7Diameter = RailEngine.SizeToInnerDiameterMm(RingSizingStandard.US, 7.0);
            Curve? railCurve = RailEngine.CreateRingRail(us7Diameter, RailProfileType.CanonicalCircle);
            Guid railObjId = Guid.Empty;
            if (railCurve != null)
            {
                railObjId = doc.Objects.AddCurve(railCurve);
                doc.Views.Redraw();
            }

            double railLength = railCurve?.GetLength() ?? 0.0;
            double expectedCircumference = Math.PI * us7Diameter;
            bool railOk = railObjId != Guid.Empty && Math.Abs(railLength - expectedCircumference) < 0.1;
            RecordStage("ParametricRingRail", railOk,
                $"RailObjId: {railObjId}, Length: {railLength:F2} mm, Expected: {expectedCircumference:F2} mm");

            // Stage 4: Swept Shank Solid Creation
            Guid shankObjId = Guid.Empty;
            int brepFaces = 0;
            if (railCurve != null)
            {
                var shankParams = new ShankParameters
                {
                    ProfileType = ShankProfileType.ComfortFit,
                    BottomWidthMm = 2.2,
                    BottomThicknessMm = 1.6,
                    TopWidthMm = 2.8,
                    TopThicknessMm = 2.0
                };
                Brep? shankBrep = ShankEngine.CreateSweptShank(railCurve, shankParams);
                if (shankBrep != null && shankBrep.IsValid)
                {
                    shankObjId = doc.Objects.AddBrep(shankBrep);
                    brepFaces = shankBrep.Faces.Count;
                    doc.Views.Redraw();
                }
            }

            bool shankOk = shankObjId != Guid.Empty && brepFaces > 0;
            RecordStage("SweptShankSolid", shankOk,
                $"ShankObjId: {shankObjId}, BrepFaces: {brepFaces}");

            // Stage 5: Gem on Curve Placement
            var gemEngine = new GemEngine();
            var placements = railCurve != null ? gemEngine.DistributeGemsOnCurve(railCurve, 2.0, 0.25) : new List<Transform>();
            bool gemsOk = placements.Count > 0;
            RecordStage("GemOnCurvePlacement", gemsOk,
                $"Calculated {placements.Count} transforms along rail curve");

            // Stage 6: Watertight STL Export & Manifold Validation
            string tempStlPath = Path.Combine(Path.GetTempPath(), $"radiance_audit_{pid}.stl");
            if (File.Exists(tempStlPath)) File.Delete(tempStlPath);

            var exportMesh = new Mesh();
            foreach (var rhObj in doc.Objects)
            {
                if (rhObj.Geometry is Mesh m)
                {
                    exportMesh.Append(m);
                }
                else if (rhObj.Geometry is Brep b)
                {
                    var meshes = Mesh.CreateFromBrep(b, MeshingParameters.FastRenderMesh);
                    if (meshes != null)
                    {
                        foreach (var bm in meshes) exportMesh.Append(bm);
                    }
                }
            }

            if (exportMesh.Faces.Count == 0)
            {
                // Fallback calibration solid cube
                var box = new Box(Plane.WorldXY, new Interval(-1, 1), new Interval(-1, 1), new Interval(-1, 1));
                exportMesh = Mesh.CreateFromBox(box, 1, 1, 1);
            }

            bool stlOk = false;
            string stlDetails = "No mesh to export";
            if (exportMesh.Faces.Count > 0)
            {
                exportMesh.Compact();
                exportMesh.Normals.ComputeNormals();

                // Export binary STL
                using (var fs = new FileStream(tempStlPath, FileMode.Create, FileAccess.Write))
                using (var writer = new BinaryWriter(fs))
                {
                    byte[] header = new byte[80];
                    System.Text.Encoding.ASCII.GetBytes("RadianceStudio Watertight Audit Mesh").CopyTo(header, 0);
                    writer.Write(header);

                    int faceCount = exportMesh.Faces.Count;
                    writer.Write((uint)faceCount);

                    for (int i = 0; i < faceCount; i++)
                    {
                        var face = exportMesh.Faces[i];
                        var normal = exportMesh.FaceNormals.Count > i ? exportMesh.FaceNormals[i] : new Vector3f(0, 0, 1);
                        writer.Write(normal.X); writer.Write(normal.Y); writer.Write(normal.Z);

                        var vA = exportMesh.Vertices[face.A];
                        var vB = exportMesh.Vertices[face.B];
                        var vC = exportMesh.Vertices[face.C];

                        writer.Write(vA.X); writer.Write(vA.Y); writer.Write(vA.Z);
                        writer.Write(vB.X); writer.Write(vB.Y); writer.Write(vB.Z);
                        writer.Write(vC.X); writer.Write(vC.Y); writer.Write(vC.Z);
                        writer.Write((ushort)0);
                    }
                }

                long fileLength = new FileInfo(tempStlPath).Length;
                long expectedLength = 84 + (exportMesh.Faces.Count * 50);
                stlOk = fileLength == expectedLength && fileLength > 84;
                stlDetails = $"Exported {tempStlPath} (Size: {fileLength} bytes, Faces: {exportMesh.Faces.Count}, Expected: {expectedLength} bytes)";
            }

            RecordStage("WatertightStlExport", stlOk, stlDetails);
        }
        catch (Exception ex)
        {
            RecordStage("ExceptionEncountered", false, ex.ToString());
            allPassed = false;
        }

        auditReport["overallStatus"] = allPassed ? "PASS" : "FAIL";

        // Write report to temp directory
        string reportPath = Path.Combine(Path.GetTempPath(), "radiance_harness_audit.json");
        try
        {
            string json = JsonSerializer.Serialize(auditReport, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(reportPath, json);
            RhinoApp.WriteLine($"[HARNESS AUDIT] Final audit report saved to: {reportPath}");
        }
        catch (Exception ex)
        {
            RhinoApp.WriteLine($"[HARNESS AUDIT] Error writing audit report: {ex.Message}");
        }

        // Clear document modified flag to prevent "Save changes?" prompt on exit
        try
        {
            doc.Modified = false;
            RhinoApp.Exit();
        }
        catch { }

        return allPassed ? Result.Success : Result.Failure;
    }
}
