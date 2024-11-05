using System.Collections;
using UnityEngine;
using UnityEditor;
using UVNF.Core.UI;
using UVNF.Extensions;
using Newtonsoft.Json;

namespace UVNF.Core.Story.Dialogue
{
    public class DialogueElement : StoryElement
    {
        public override string ElementName => nameof(DialogueElement);

        public override Color32 DisplayColor => _displayColor;
        private Color32 _displayColor = new Color32().Story();

        public override StoryElementTypes Type => StoryElementTypes.Story;

        [JsonProperty]
        public string CharacterKey = string.Empty;                                  //Set to string.Empty due to Init/Json Load
        public string LocalizedCharacterName => GetLocalizedString(CharacterKey);

        [JsonProperty]
        public string DialogueKey = string.Empty;                                   //Set to string.Empty due to Init/Json Load
        public string LocalizedDialogue => GetLocalizedString(DialogueKey);

#if UNITY_EDITOR
        public override void DisplayLayout(Rect layoutRect, GUIStyle label)
        {
            CharacterKey = EditorGUILayout.TextField("CharacterKey", CharacterKey);
            DialogueKey = EditorGUILayout.TextField("DialogueKey", DialogueKey);

            GUILayout.Space(5);
            if (HasValidStringTable)
            {
                if (LocalizedCharacterName == null)
                {
                    EditorGUILayout.HelpBox(nameof(CharacterKey) + " not found!", MessageType.Warning);
                }

                if (LocalizedDialogue == null)
                {
                    EditorGUILayout.HelpBox(nameof(DialogueKey) + " not found!", MessageType.Warning);
                }
                else
                {
                    EditorGUILayout.LabelField("Preview", EditorStyles.boldLabel);
                    EditorGUILayout.LabelField(LocalizedCharacterName);
                    EditorGUILayout.LabelField(LocalizedDialogue, multiLineLabelStyle);
                }
            }
            else
            {
                EditorGUILayout.HelpBox("Graph missing StringTableCollection!", MessageType.Error);
            }
        }
#endif

        public override IEnumerator Execute(UVNFManager gameManager, UVNFCanvas canvas)
        {
            return canvas.DisplayText(DialogueKey, CharacterKey);
        }
    }
}