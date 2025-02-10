using UnityEngine;
using XNodeEditor;
using UnityEditor;
using UVNF.Core.Story;
using UVNF.Core.Story.Dialogue;
using UVNF.Editor.Settings;

namespace UVNF.Editor.Story.Nodes
{
    public class CustomNodeEditors : MonoBehaviour
    {
        [CustomNodeEditor(typeof(ChoiceElement))]
        public class ChoiceNodeEditor : NodeEditor
        {
            ChoiceElement node;
            bool foldout = true;

            public override void OnCreate()
            {
                if (node == null) node = target as ChoiceElement;
                EditorUtility.SetDirty(node);
                //ReplaceTint(node.DisplayColor);
            }

            public override void OnHeaderGUI()
            {
                DisplayElementType(node.Type, node.ElementName, GetWidth());
            }

            public override void OnBodyGUI()
            {
                GUILayout.BeginHorizontal();
                {
                    GUILayout.Label("Previous", EditorStyles.boldLabel);
                    NodeEditorGUILayout.AddPortField(node.GetInputPort("PreviousNode"));
                    GUILayout.Space(170f);
                }
                GUILayout.EndHorizontal();

                if (foldout)
                {
                    for (int i = 0; i < node.ChoiceKeys.Count; i++)
                    {
                        node.ChoiceKeys[i] = GUILayout.TextField(node.ChoiceKeys[i]);
                        NodeEditorGUILayout.AddPortField(node.GetOutputPort(ChoiceElement.CHOICE_PORT_NAME + i));

                        if (node.HasValidStringTable)
                        {
                            string localizedChoice = node.GetLocalizedChoice(i);
                            if (localizedChoice == null)
                            {
                                EditorGUILayout.HelpBox($"Choice Key ({i}) localization not found!", MessageType.Warning);
                            }
                            else
                            {
                                EditorGUILayout.LabelField("Preview: " + localizedChoice);
                            }
                        }
                        else 
                        {
                            EditorGUILayout.HelpBox("Graph missing StringTableCollection!", MessageType.Error);
                        }

                        if (GUILayout.Button("-"))
                        {
                            node.RemoveChoice(i);
                        }

                        GUILayout.Space(7.5f);
                    }

                    if (GUILayout.Button("+"))
                    {
                        node.AddChoice();
                    }

                    node.ShuffleChoices = GUILayout.Toggle(node.ShuffleChoices, "Shuffle Choices");
                    node.HideDialogue = GUILayout.Toggle(node.HideDialogue, "Hide Dialogue");
                }
                else
                {
                    for (int i = 0; i < node.ChoiceKeys.Count; i++)
                    {
                        GUILayout.Label("");
                        NodeEditorGUILayout.AddPortField(node.GetOutputPort(ChoiceElement.CHOICE_PORT_NAME + i));
                    }
                }
            }
        }

        [CustomNodeEditor(typeof(BranchElement))]
        public class ConditionNodeEditor : NodeEditor
        {
            BranchElement node;

            public override void OnCreate()
            {
                if (node == null) node = target as BranchElement;
                EditorUtility.SetDirty(node);
                //ReplaceTint(node.DisplayColor);
            }

            public override void OnHeaderGUI()
            {
                DisplayElementType(node.Type, node.ElementName, GetWidth());
            }

            public override void OnBodyGUI()
            {
                // base.OnBodyGUI();
                Rect lastRect;

                GUILayout.BeginVertical();
                GUILayout.BeginHorizontal();
                {
                    GUILayout.Label("Previous", EditorStyles.boldLabel);
                    NodeEditorGUILayout.AddPortField(node.GetInputPort("PreviousNode"));
                    GUILayout.Space(170f);
                    GUILayout.Label("Next", EditorStyles.boldLabel);
                    NodeEditorGUILayout.AddPortField(node.GetOutputPort("NextNode"));
                    lastRect = GUILayoutUtility.GetLastRect();
                }

                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                {
                    GUILayout.Space(235f);
                    GUILayout.Label("Fails", EditorStyles.boldLabel);
                    NodeEditorGUILayout.AddPortField(node.GetOutputPort("ConditionFails"));
                    lastRect = GUILayoutUtility.GetLastRect();
                }

                GUILayout.EndHorizontal(); // enter
                GUILayout.EndVertical(); // tab

                GUILayout.Space(2.5f);
                node.DisplayNodeLayout(lastRect);
            }
        }

        [CustomNodeEditor(typeof(StoryElement))]
        public class StoryElementNodeEditor : NodeEditor
        {
            StoryElement node;
            bool foldout = true;

            public override void OnCreate()
            {
                if (node == null) node = target as StoryElement;
                EditorUtility.SetDirty(node);

                //ReplaceTint(node.DisplayColor);
            }

            public override void OnHeaderGUI()
            {
                DisplayElementType(node.Type, node.ElementName, GetWidth());
            }

            public override void OnBodyGUI()
            {
                Rect lastRect;
                GUILayout.BeginHorizontal();
                {
                    GUILayout.Label("Previous", EditorStyles.boldLabel);
                    NodeEditorGUILayout.AddPortField(node.GetInputPort("PreviousNode"));
                    GUILayout.Space(170f);
                    GUILayout.Label("Next", EditorStyles.boldLabel);
                    NodeEditorGUILayout.AddPortField(node.GetOutputPort("NextNode"));
                    lastRect = GUILayoutUtility.GetLastRect();
                }
                GUILayout.EndHorizontal();

                if (foldout)
                {
                    node.DisplayNodeLayout(lastRect);
                }

                GUIContent arrow;
                if (foldout) arrow = EditorGUIUtility.IconContent("d_Toolbar Minus");
                else arrow = EditorGUIUtility.IconContent("d_Toolbar Plus");

                if (GUILayout.Button(arrow))
                    foldout = !foldout;
            }
        }

        public static void DisplayElementType(StoryElementTypes type, string elementName, int width)
        {
            GUI.DrawTexture(new Rect(5f, 5f, width - 10f, 36f), UVNFSettings.GetElementStyle(type).normal.background);

            GUILayout.Space(5f);
            GUILayout.BeginHorizontal();
            {
                GUILayout.Space(23f);
                GUILayout.Label(elementName, UVNFSettings.GetLabelStyle(type));
            }
            GUILayout.EndHorizontal();

            if (UVNFSettings.EditorSettings.ElementHints.ContainsKey(elementName))
                GUI.DrawTexture(new Rect(5f, 7f, 32f, 32f), UVNFSettings.EditorSettings.ElementHints[elementName]);
        }
    }
}