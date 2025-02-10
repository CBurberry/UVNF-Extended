using Newtonsoft.Json;
using System;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Custom JsonConverter that serializes the path to an asset rather than the asset itself.
/// For use only with assets, in-scene reference fields are not supported.
/// </summary>
public class AssetReferenceJsonConverter : JsonConverter<UnityEngine.Object>
{
#if UNITY_EDITOR
    public override UnityEngine.Object ReadJson(JsonReader reader, Type objectType, UnityEngine.Object existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        //Check the path to ensure its not null, not empty, not whitespace, and points at an actual file
        string path = reader.ReadAsString();
        return AssetDatabase.LoadAssetAtPath(path, objectType);
    }

    public override void WriteJson(JsonWriter writer, UnityEngine.Object value, JsonSerializer serializer)
    {
        if (value != null && !AssetDatabase.Contains(value))
        {
            Debug.LogError($"{nameof(AssetReferenceJsonConverter)}.{nameof(WriteJson)}: Cannot process non-asset objects!");
        }
        
        writer.WriteValue(value != null ? AssetDatabase.GetAssetPath(value) : string.Empty);
    }
#else
    
    public override UnityEngine.Object ReadJson(JsonReader reader, Type objectType, UnityEngine.Object existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        throw new NotSupportedException();
    }

    public override void WriteJson(JsonWriter writer, UnityEngine.Object value, JsonSerializer serializer) 
    {
        throw new NotSupportedException();
    }
#endif
}
