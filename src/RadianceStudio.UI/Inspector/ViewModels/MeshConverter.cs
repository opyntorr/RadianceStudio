namespace RadianceStudio.UI.ViewModels;

using System;
using System.Collections.Generic;
using RadianceStudio.Core.Engines;
using RadianceStudio.Core.Geometry;
using RadianceStudio.Core.Models;
using Rhino.Geometry;

/// <summary>
/// Thread-safe converter from pure C# core geometry models (GemMesh, ShankMesh)
/// to native Rhino.Geometry.Mesh instances for preview conduits and document baking.
/// Safely returns null in headless environments where native Rhino libraries are absent.
/// </summary>
public static class MeshConverter
{
    public static Mesh? ToRhinoMesh(GemMesh? mesh)
    {
        if (mesh == null || !RailEngine.IsRhinoNativeAvailable)
        {
            return null;
        }

        var rhinoMesh = new Mesh();
        for (int i = 0; i < mesh.Vertices.Count; i++)
        {
            var v = mesh.Vertices[i];
            rhinoMesh.Vertices.Add(v.X, v.Y, v.Z);
        }

        for (int f = 0; f < mesh.Faces.Count; f++)
        {
            var face = mesh.Faces[f];
            if (face.Length == 3)
            {
                rhinoMesh.Faces.AddFace(face[0], face[1], face[2]);
            }
            else if (face.Length == 4)
            {
                rhinoMesh.Faces.AddFace(face[0], face[1], face[2], face[3]);
            }
            else if (face.Length > 4)
            {
                // Fan triangulation
                for (int k = 1; k < face.Length - 1; k++)
                {
                    rhinoMesh.Faces.AddFace(face[0], face[k], face[k + 1]);
                }
            }
        }

        rhinoMesh.Normals.ComputeNormals();
        rhinoMesh.Compact();
        return rhinoMesh;
    }

    public static Mesh? ToRhinoMesh(ShankMesh? mesh)
    {
        if (mesh == null || !RailEngine.IsRhinoNativeAvailable)
        {
            return null;
        }

        var rhinoMesh = new Mesh();
        for (int i = 0; i < mesh.Vertices.Count; i++)
        {
            var v = mesh.Vertices[i];
            rhinoMesh.Vertices.Add(v.X, v.Y, v.Z);
        }

        for (int f = 0; f < mesh.Faces.Count; f++)
        {
            var face = mesh.Faces[f];
            if (face.Length == 3)
            {
                rhinoMesh.Faces.AddFace(face[0], face[1], face[2]);
            }
            else if (face.Length == 4)
            {
                rhinoMesh.Faces.AddFace(face[0], face[1], face[2], face[3]);
            }
            else if (face.Length > 4)
            {
                for (int k = 1; k < face.Length - 1; k++)
                {
                    rhinoMesh.Faces.AddFace(face[0], face[k], face[k + 1]);
                }
            }
        }

        rhinoMesh.Normals.ComputeNormals();
        rhinoMesh.Compact();
        return rhinoMesh;
    }

    public static Mesh? CreateCylinderPreviewMesh(Point3d p0, Point3d p1, double radius, int segments = 16)
    {
        if (!RailEngine.IsRhinoNativeAvailable)
        {
            return null;
        }

        var axis = p1 - p0;
        double height = axis.Length;
        if (height < 1e-6) return null;

        var plane = new Plane(p0, axis);
        var circle = new Circle(plane, radius);
        var cylinder = new Cylinder(circle, height);
        return cylinder.ToRevSurface()?.ToBrep()?.Faces[0].GetMesh(MeshType.Default);
    }
}
