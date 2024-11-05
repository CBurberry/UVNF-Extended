using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using XNode;
using UVNF.Core.UI;
using UVNF.Entities.Containers.Variables;
using UVNF.Extensions;
using System;
using Newtonsoft.Json;

namespace UVNF.Core.Story.Dialogue
{
    public class BranchElement : StoryElement
    {
        public enum StringOperations : int
        { 
            IsNotEqualTo = 0,
            IsEqualTo
        };

        public enum FloatOperations : int
        {
            IsNotEqualTo = 0,
            IsEqualTo,
            IsGreaterThan,
            IsLessThan
        };

        public readonly int StringOpCount = Enum.GetNames(typeof(StringOperations)).Length;
        public readonly int FloatOpCount = Enum.GetNames(typeof(FloatOperations)).Length;

        public override string ElementName => nameof(BranchElement);

        public override Color32 DisplayColor => _displayColor;
        private Color32 _displayColor = new Color32().Story();

        public override StoryElementTypes Type => StoryElementTypes.Story;

        [HideInInspector]
        [Output(ShowBackingValue.Never, ConnectionType.Override)] public NodePort ConditionFails;

        [JsonProperty]
        public string SelectedKey = string.Empty;

        [JsonProperty]
        public int OperationKey;

        //N.B. Ssving all data type as a string as both bool, float, string are parsable
        //N.N.B. Unity Serialization Rules do not support C# 'object' data types.
        [JsonProperty]
        public string ComparatorValue;

        private VariableManager variableManager => storyGraph?.variableManager;
        private StringOperations selectedStringOperation;
        private FloatOperations selectedFloatOperation;

#if UNITY_EDITOR
        public override void DisplayLayout(Rect layoutRect, GUIStyle label)
        {
            if (variableManager == null || variableManager.VariableCount == 0)
            {
                EditorGUILayout.HelpBox("Graph missing VariableManager!", MessageType.Error);
                return;
            }

            if (OperationKey < 0) 
            {
                OperationKey = 0;
            }

            EditorGUILayout.LabelField("Condition", EditorStyles.boldLabel);
            GUILayout.BeginVertical(new GUIStyle("HelpBox"));

            //Setup variable keys
            var variableKeys = variableManager.GetAggregateVariableKeys();
            int variableIndex = (SelectedKey.Length > 0 ? Array.IndexOf(variableKeys, SelectedKey) : 0);
            GUILayout.BeginHorizontal();
            variableIndex = EditorGUILayout.Popup(variableIndex, variableKeys);
            SelectedKey = variableKeys[variableIndex];
            VariableTypes variableType = variableManager.GetVariableType(SelectedKey);

            //Show fields corresponding to the type of variable
            bool canParse = false;
            switch (variableType)
            {
                case VariableTypes.Boolean:
                    GUILayout.EndHorizontal();
                    canParse = bool.TryParse(ComparatorValue, out bool parsedBool);
                    ComparatorValue = EditorGUILayout.Toggle("Equals: ", canParse ? parsedBool : false).ToString();
                    OperationKey = 0;
                    break;
                case VariableTypes.String:
                    selectedStringOperation = (StringOperations)EditorGUILayout.EnumPopup((StringOperations)(OperationKey < StringOpCount ? OperationKey : 0));
                    GUILayout.EndHorizontal();
                    ComparatorValue = EditorGUILayout.TextField("Value: ", ComparatorValue);
                    OperationKey = (int)selectedStringOperation;
                    break;
                case VariableTypes.Number:
                    canParse = float.TryParse(ComparatorValue, out float parsedFloat);
                    selectedFloatOperation = (FloatOperations)EditorGUILayout.EnumPopup((FloatOperations)(OperationKey < FloatOpCount ? OperationKey : 0));
                    GUILayout.EndHorizontal();
                    ComparatorValue = EditorGUILayout.FloatField("Value: ", canParse ? parsedFloat : 0f).ToString();
                    OperationKey = (int)selectedFloatOperation;
                    break;
            }
            GUILayout.EndVertical();
        }
#endif

        public override IEnumerator Execute(UVNFManager managerCallback, UVNFCanvas canvas)
        {
            bool conditionTrue = false;
            object value = variableManager.GetVariable(SelectedKey);
            switch (variableManager.GetVariableType(SelectedKey))
            {
                case VariableTypes.Boolean:
                    conditionTrue = (bool)value == bool.Parse(ComparatorValue);
                    break;
                case VariableTypes.Number:
                    conditionTrue = ResolveNumberOperation((float)value);
                    break;
                case VariableTypes.String:
                    conditionTrue = selectedStringOperation == StringOperations.IsEqualTo 
                        ? ComparatorValue == (string)value 
                        : ComparatorValue != (string)value; 
                    break;
            }

            if (conditionTrue)
                managerCallback.AdvanceStoryGraph(GetOutputPort("NextNode").Connection.node as StoryElement);
            else
                managerCallback.AdvanceStoryGraph(GetOutputPort("ConditionFails").Connection.node as StoryElement);
            yield return null;
        }

        private bool ResolveNumberOperation(float variableValue)
        {
            bool result = false;
            switch (selectedFloatOperation) 
            {
                case FloatOperations.IsNotEqualTo:
                    result = variableValue != float.Parse(ComparatorValue); break;
                case FloatOperations.IsEqualTo:
                    result = variableValue == float.Parse(ComparatorValue); break;
                case FloatOperations.IsGreaterThan:
                    result = variableValue > float.Parse(ComparatorValue); break;
                case FloatOperations.IsLessThan:
                    result = variableValue < float.Parse(ComparatorValue); break;
            }

            return result;
        }

        private bool IsNumericType(Type type)
        {
            switch (System.Type.GetTypeCode(type))
            {
                case TypeCode.Byte:
                case TypeCode.SByte:
                case TypeCode.UInt16:
                case TypeCode.UInt32:
                case TypeCode.UInt64:
                case TypeCode.Int16:
                case TypeCode.Int32:
                case TypeCode.Int64:
                case TypeCode.Decimal:
                case TypeCode.Double:
                case TypeCode.Single:
                    return true;
                default:
                    return false;
            }
        }
    }
}