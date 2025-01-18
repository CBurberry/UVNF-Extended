using System;
using UnityEditor;
using UnityEngine;
using UVNF.Entities.Containers.Variables;

/// <summary>
/// To avoid the situation of having to reimplement most of what SerializedDictionary does just to have a presentable editor,
///  duplicates won't be enforced but just highlighted as an issue.
///
/// N.B. Serialized dictionary highlights duplicate key in its own dictionary but while is saves it to the backing field (list)
///      red colored values are ignored on dictionary operations
/// </summary>
[CustomEditor(typeof(VariableManager))]
public class VariableManagerEditor : Editor
{
    //(IMGUI)
    public override void OnInspectorGUI()
    {
        VariableManager vmTarget = (VariableManager)target;
        EditorGUILayout.HelpBox("All variable keys should be unique regardless of type. " 
            + Environment.NewLine + "Duplicates in the same dictionary will be ignored.", MessageType.Info);
        GUILayout.Space(10f);

        //Hide the readonly 'script' field that comes with draw default inspector
        DrawPropertiesExcluding(serializedObject, new string[] { "m_Script" });
        GUILayout.Space(10f);

        if (vmTarget.HasDuplicateKeys(out string[] duplicateKeys)) 
        {
            EditorGUILayout.HelpBox($"Duplicate keys [{string.Join(',', duplicateKeys)}] detected!  Duplicate keys will cause errors!", MessageType.Error);
        }
    }
}