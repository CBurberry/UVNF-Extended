using System.Collections;
using UnityEngine;
using UnityEditor;
using UVNF.Core.UI;
using UVNF.Entities.Containers.Variables;
using UVNF.Extensions;
using System;
using Newtonsoft.Json;

namespace UVNF.Core.Story.Utility
{
    public class SetVariableElement : StoryElement
    {
        public override string ElementName => nameof(SetVariableElement);

        public override Color32 DisplayColor => _displayColor;
        private Color32 _displayColor = new Color32().Utility();

        public override StoryElementTypes Type => StoryElementTypes.Utility;

        [JsonProperty]
        public string SelectedKey = string.Empty;

        //N.B. Ssving all data type as a string as both bool, float, string are parsable
        //N.N.B. Unity Serialization Rules do not support C# 'object' data types.
        [JsonProperty]
        public string SetValue;

        private VariableManager variableManager => storyGraph?.variableManager;

#if UNITY_EDITOR
        public override void DisplayLayout(Rect layoutRect, GUIStyle label)
        {
            if (variableManager != null && variableManager.VariableCount > 0)
            {
                var variableKeys = variableManager.GetAggregateVariableKeys();
                int variableIndex = (SelectedKey.Length > 0 ? Array.IndexOf(variableKeys, SelectedKey) : 0);
                bool canParse = false;
                variableIndex = EditorGUILayout.Popup("Variable", variableIndex, variableKeys);
                SelectedKey = variableKeys[variableIndex];

                switch (variableManager.GetVariableType(SelectedKey))
                {
                    case VariableTypes.Number:
                        canParse = float.TryParse(SetValue, out float parsedFloat);
                        SetValue = EditorGUILayout.FloatField("New Value: ", canParse ? parsedFloat : 0f).ToString();
                        break;
                    case VariableTypes.String:
                        SetValue = EditorGUILayout.TextField("New Value: ", SetValue);
                        break;
                    case VariableTypes.Boolean:
                        canParse = bool.TryParse(SetValue, out bool parsedBool);
                        SetValue = EditorGUILayout.Toggle("New Value: ", canParse ? parsedBool : false).ToString();
                        break;
                }
            }
            else 
            {
                EditorGUILayout.HelpBox("Graph missing VariableManager!", MessageType.Error);
            }
        }
#endif

        public override IEnumerator Execute(UVNFManager managerCallback, UVNFCanvas canvas)
        {
            var variableKeys = variableManager.GetAggregateVariableKeys();
            bool canParse = false;
            switch (variableManager.GetVariableType(SelectedKey))
            {
                case VariableTypes.Number:
                    canParse = float.TryParse(SetValue, out float parsedFloat);
                    variableManager.UpdateVariable(SelectedKey, parsedFloat);
                    break;
                case VariableTypes.String:
                    variableManager.UpdateVariable(SelectedKey, SetValue);
                    break;
                case VariableTypes.Boolean:
                    canParse = bool.TryParse(SetValue, out bool parsedBool);
                    variableManager.UpdateVariable(SelectedKey, parsedBool);
                    break;
            }
            return null;
        }
    }
}