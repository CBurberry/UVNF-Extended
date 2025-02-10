using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.Collections;
using UnityEditor;
using UnityEngine;
using UVNF.Core.UI;
using UVNF.Extensions;

namespace UVNF.Core.Story.Character
{
    public class ExitSceneElement : StoryElement
    {
        public override string ElementName => nameof(ExitSceneElement);

        public override Color32 DisplayColor => _displayColor;
        private Color32 _displayColor = new Color32().Character();

        public override StoryElementTypes Type => StoryElementTypes.Character;

        [JsonProperty]
        public string CharacterKey;

        [JsonProperty, JsonConverter(typeof(StringEnumConverter))]
        public ScenePositions ExitPosition;

        [JsonProperty]
        public float ExitTime;

#if UNITY_EDITOR
        public override void DisplayLayout(Rect layoutRect, GUIStyle label)
        {
            CharacterKey = EditorGUILayout.TextField("Character Key", CharacterKey);
            ExitPosition = (ScenePositions)EditorGUILayout.EnumPopup("Exit Position", ExitPosition);
            ExitTime = EditorGUILayout.Slider("Exit Time", ExitTime, 1f, 10f);
        }
#endif

        public override IEnumerator Execute(UVNFManager managerCallback, UVNFCanvas canvas)
        {
            managerCallback.CharacterManager.RemoveCharacter(CharacterKey, ExitPosition, ExitTime);
            return null;
        }
    }
}