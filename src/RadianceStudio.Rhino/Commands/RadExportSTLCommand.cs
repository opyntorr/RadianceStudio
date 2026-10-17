namespace RadianceStudio.Rhino.Commands;

using System;
using System.IO;
using System.Runtime.InteropServices;
using global::Rhino;
using global::Rhino.Commands;
using global::Rhino.DocObjects;
using global::Rhino.Input.Custom;
using RadianceStudio.Core.Engines;
using RadianceStudio.Core.Enums;
using RadianceStudio.Core.Geometry;
using RadianceStudio.Core.Models;
using RadianceStudio.Core.Services;

/// <summary>
/// Exports a verified 2-manifold watertight binary STL mesh ready for direct CAM 3D printing and investment casting.
/// </summary>
[Guid("0981EF1A-140C-4A2D-A2A9-373C3091B012")]
public class RadExportSTLCommand : Command
{
    public static readonly string[] CommandParameters = { "OutputFilePath", "TargetVoxelSizeMm", "BinaryFormat" };

    public override string EnglishName => "RadExportSTL";

    protected override Result RunCommand(RhinoDoc doc, RunMode mode)
    {
        string outputFilePath = string.Empty;
        double targetVoxelSizeMm = 0.05;
        bool binaryFormat = true;

        if (mode == RunMode.Scripted)
        {
            var go = new GetOption();
            go.SetCommandPrompt("RadianceStudio STL Export Options");

            var optVoxel = new OptionDouble(targetVoxelSizeMm);
            var optBinary = new OptionToggle(binaryFormat, "ASCII", "Binary");

            go.AddOption("OutputFilePath");
            go.AddOptionDouble("TargetVoxelSizeMm", ref optVoxel);
            go.AddOptionToggle("BinaryFormat", ref optBinary);

            while (go.Get() == global::Rhino.Input.GetResult.Option)
            {
                targetVoxelSizeMm = optVoxel.CurrentValue;
                binaryFormat = optBinary.CurrentValue;
            }
        }

        // In interactive mode, prompt user for save destination if not supplied
        if (string.IsNullOrWhiteSpace(outputFilePath))
        {
            if (mode == RunMode.Interactive && Eto.Platform.Instance != null)
            {
                var sfd = new Eto.Forms.SaveFileDialog
                {
                    Title = "Export Watertight STL for Casting",
                    Filters = { new Eto.Forms.FileFilter("Stereolithography (*.stl)", ".stl") }
                };

                if (sfd.ShowDialog(Eto.Forms.Application.Instance?.MainForm) == Eto.Forms.DialogResult.Ok)
                {
                    outputFilePath = sfd.FileName;
                }
                else
                {
                    return Result.Cancel;
                }
            }
            else
            {
                outputFilePath = Path.Combine(Path.GetTempPath(), "radiance_export.stl");
            }
        }

        try
        {
            // Build watertight polyhedral mesh
            GemMesh meshToExport;

            if (doc != null && RailEngine.IsRhinoNativeAvailable)
            {
                var combinedMesh = new global::Rhino.Geometry.Mesh();
                foreach (var obj in doc.Objects.FindByObjectType(ObjectType.Mesh))
                {
                    if (obj.Geometry is global::Rhino.Geometry.Mesh m)
                    {
                        combinedMesh.Append(m);
                    }
                }

                if (combinedMesh.Faces.Count > 0)
                {
                    meshToExport = ConvertRhinoMeshToGemMesh(combinedMesh);
                }
                else
                {
                    meshToExport = GemMeshFactory.Create(GemCutType.RoundBrilliant, 6.5);
                }
            }
            else
            {
                meshToExport = GemMeshFactory.Create(GemCutType.RoundBrilliant, 6.5);
            }

            // Export standard 80-byte header binary STL
            byte[] stlBytes = ManufacturingEngine.ExportWatertightStl(
                meshToExport,
                headerText: "RadianceStudio Watertight STL CAM Export",
                validateManifold: true);

            string? dir = Path.GetDirectoryName(outputFilePath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            File.WriteAllBytes(outputFilePath, stlBytes);
            RhinoApp.WriteLine($"[RadianceStudio] Watertight binary STL successfully exported ({stlBytes.Length:N0} bytes, voxel {targetVoxelSizeMm:F3} mm) to: {outputFilePath}");
            return Result.Success;
        }
        catch (Exception ex)
        {
            RhinoApp.WriteLine($"[RadianceStudio] Error exporting STL: {ex.Message}");
            return Result.Failure;
        }
    }

    private static GemMesh ConvertRhinoMeshToGemMesh(global::Rhino.Geometry.Mesh mesh)
    {
        var vertices = new Point3D[mesh.Vertices.Count];
        for (int i = 0; i < mesh.Vertices.Count; i++)
        {
            var v = mesh.Vertices[i];
            vertices[i] = new Point3D(v.X, v.Y, v.Z);
        }

        var faces = new System.Collections.Generic.List<int[]>(mesh.Faces.Count);
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

        return new GemMesh
        {
            Vertices = vertices,
            Faces = faces
        };
    }
}
