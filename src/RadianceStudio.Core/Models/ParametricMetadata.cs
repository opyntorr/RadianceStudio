namespace RadianceStudio.Core.Models;

using System;
using System.Runtime.InteropServices;
using Rhino.Collections;
using Rhino.DocObjects.Custom;
using Rhino.FileIO;

/// <summary>
/// Subclass of Rhino UserData to serialize parametric jewelry history (ring size, width, thickness, parent/child IDs)
/// into the native .3dm file using ArchivableDictionary, bypassing Grasshopper entirely.
/// </summary>
[Guid("A4C7581B-8340-4A9B-9852-6C931E54F301")]
public class ParametricMetadata : UserData
{
    public ArchivableDictionary Parameters { get; private set; } = new ArchivableDictionary();

    public override string Description => "RadianceStudio Parametric Metadata";

    protected override bool Read(BinaryArchiveReader archive)
    {
        var dict = archive.ReadDictionary();
        if (dict != null)
        {
            Parameters = dict;
        }
        return true;
    }

    protected override bool Write(BinaryArchiveWriter archive)
    {
        archive.WriteDictionary(Parameters);
        return true;
    }

    /// <summary>
    /// Attaches parametric metadata to a target Rhino geometry object.
    /// </summary>
    public static ParametricMetadata Attach(Rhino.Geometry.GeometryBase geometry)
    {
        var existing = geometry.UserData.Find(typeof(ParametricMetadata)) as ParametricMetadata;
        if (existing != null)
        {
            return existing;
        }

        var created = new ParametricMetadata();
        geometry.UserData.Add(created);
        return created;
    }

    /// <summary>
    /// Finds parametric metadata on a geometry instance if present.
    /// </summary>
    public static ParametricMetadata? Find(Rhino.Geometry.GeometryBase geometry)
    {
        return geometry.UserData.Find(typeof(ParametricMetadata)) as ParametricMetadata;
    }
}
