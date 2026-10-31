namespace RadianceStudio.Core.Models;

using System;
using Rhino.Collections;
using Rhino.Geometry;

/// <summary>
/// Manages parametric jewelry history (ring size, width, thickness, parent/child IDs)
/// within the native Rhino geometry UserDictionary, persisting seamlessly into .3dm files.
/// </summary>
public class ParametricMetadata
{
    public const string UserDictionaryKey = "RadianceStudio_ParametricMetadata";

    public ArchivableDictionary Parameters { get; }

    public string Description => "RadianceStudio Parametric Metadata";

    public ParametricMetadata()
    {
        Parameters = new ArchivableDictionary();
    }

    public ParametricMetadata(ArchivableDictionary dictionary)
    {
        Parameters = dictionary ?? new ArchivableDictionary();
    }

    /// <summary>
    /// Attaches parametric metadata to a target Rhino geometry object.
    /// </summary>
    public static ParametricMetadata Attach(GeometryBase geometry)
    {
        if (geometry == null) throw new ArgumentNullException(nameof(geometry));

        ArchivableDictionary? dict = null;
        if (geometry.UserDictionary.ContainsKey(UserDictionaryKey))
        {
            dict = geometry.UserDictionary.GetDictionary(UserDictionaryKey);
        }

        if (dict == null)
        {
            dict = new ArchivableDictionary();
            geometry.UserDictionary.Set(UserDictionaryKey, dict);
        }

        return new ParametricMetadata(dict);
    }

    /// <summary>
    /// Finds parametric metadata on a geometry instance if present.
    /// </summary>
    public static ParametricMetadata? Find(GeometryBase? geometry)
    {
        if (geometry == null) return null;

        if (geometry.UserDictionary.ContainsKey(UserDictionaryKey))
        {
            var dict = geometry.UserDictionary.GetDictionary(UserDictionaryKey);
            if (dict != null)
            {
                return new ParametricMetadata(dict);
            }
        }

        return null;
    }

    /// <summary>
    /// Finds parametric metadata on object attributes if present.
    /// </summary>
    public static ParametricMetadata? Find(Rhino.DocObjects.ObjectAttributes? attributes)
    {
        if (attributes == null) return null;

        if (attributes.UserDictionary.ContainsKey(UserDictionaryKey))
        {
            var dict = attributes.UserDictionary.GetDictionary(UserDictionaryKey);
            if (dict != null)
            {
                return new ParametricMetadata(dict);
            }
        }

        return null;
    }
}
