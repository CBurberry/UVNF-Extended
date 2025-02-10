using NaughtyAttributes;
using Newtonsoft.Json;
using System.Collections;
using UnityEditor;
using UnityEngine;
using UVNF.Core.UI;
using UVNF.Extensions;

namespace UVNF.Core.Story.Character
{
    public class ChangeSpriteElement : StoryElement
    {
        public override string ElementName => nameof(ChangeSpriteElement);

        public override Color32 DisplayColor => _displayColor;
        private Color32 _displayColor = new Color32().Character();

        public override StoryElementTypes Type => StoryElementTypes.Character;

        [JsonProperty]
        public string CharacterKey;

        [JsonProperty, JsonConverter(typeof(AssetReferenceJsonConverter))]
        [ShowAssetPreview]
        public Sprite NewSprite;

#if UNITY_EDITOR
        public override void DisplayLayout(Rect layoutRect, GUIStyle label)
        {
            CharacterKey = EditorGUILayout.TextField("Character Key", CharacterKey);

            GUILayout.Label("New Character Sprite", EditorStyles.boldLabel);
            NewSprite = EditorGUILayout.ObjectField(NewSprite, typeof(Sprite), false) as Sprite;
        }
#endif

        public override IEnumerator Execute(UVNFManager managerCallback, UVNFCanvas canvas)
        {
            managerCallback.CharacterManager.ChangeCharacterSprite(CharacterKey, NewSprite);
            return null;
        }
    }
}