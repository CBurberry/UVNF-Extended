using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UVNF.Entities.Containers.Variables;

namespace UVNF.Editor
{
    //TODO: This class just never seemed to have worked from the beginning (not sure why it was needed either tbh)
    public class VariableManagerEditor : EditorWindow
    {
        public VariableManager VariableManager;

        private Vector2 scrollPosition = new Vector2();
        private int selectedIndex = -1;

        //[MenuItem("UVNF/Variable Manager")]
        public static void Init()
        {
            VariableManagerEditor window = GetWindow<VariableManagerEditor>();
            window.Show();
        }

        private void OnGUI()
        {
            var variableKeys = VariableManager.GetAggregateVariableKeys();
            GUILayout.BeginVertical();
            {
                GUILayout.BeginHorizontal("Box");
                {
                    VariableManager = EditorGUILayout.ObjectField("Variable Manager", VariableManager, typeof(VariableManager), false) as VariableManager;
                }
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                {
                    if (VariableManager != null)
                    {
                        EditorUtility.SetDirty(VariableManager);
                        scrollPosition = GUILayout.BeginScrollView(scrollPosition, false, true, GUILayout.Width(150f));
                        {
                            for (int i = 0; i < variableKeys.Count(); i++)
                            {
                                GUI.SetNextControlName("ButtonFocus");
                                if (GUILayout.Button(variableKeys[i]))
                                {
                                    selectedIndex = i;
                                    GUI.FocusControl("ButtonFocus");
                                }
                            }

                            GUILayout.BeginHorizontal();
                            {
                                if (GUILayout.Button("+"))
                                {
                                    //TODO:
                                    //VariableManager.AddVariable();
                                }
                            }
                            GUILayout.EndHorizontal();
                        }
                        GUILayout.EndScrollView();
                    }
                    GUILayout.BeginVertical();
                    {
                        if (selectedIndex > -1 && selectedIndex < variableKeys.Count())
                        {
                            string variableKey = variableKeys[selectedIndex];
                            GUILayout.Label("Variable Name: " + variableKey);
                            object variable = VariableManager.GetVariable(variableKey);

                            switch (VariableManager.GetVariableType(variableKey))
                            {
                                case VariableTypes.Number:
                                    float floatVariable = (float)variable;
                                    VariableManager.UpdateVariable(variableKey, EditorGUILayout.FloatField("Value", floatVariable));
                                    break;
                                case VariableTypes.String:
                                    string stringVariable = (string)variable;
                                    VariableManager.UpdateVariable(variableKey, EditorGUILayout.TextField("Value", stringVariable));
                                    break;
                                case VariableTypes.Boolean:
                                    bool boolVariable = (bool)variable;
                                    VariableManager.UpdateVariable(variableKey, Convert.ToBoolean(
                                        EditorGUILayout.Popup("Value", Convert.ToInt32(boolVariable), new string[] { "False", "True" }))); 
                                    break;
                            }

                            if (GUILayout.Button("Remove"))
                            {
                                string key = variableKeys[selectedIndex];
                                VariableManager.RemoveVariable(key);
                                selectedIndex = -1;
                            }
                        }
                        else
                            selectedIndex = -1;
                    }
                    GUILayout.EndVertical();
                }
                GUILayout.EndHorizontal();
            }
            GUILayout.EndVertical();
        }
    }
}