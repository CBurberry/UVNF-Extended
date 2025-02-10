using System.Collections;
using UnityEngine;
using UnityEditor;
using UVNF.Core.UI;
using UVNF.Extensions;
using Newtonsoft.Json;
using NaughtyAttributes;

namespace UVNF.Core.Story.Scenery
{
    public class ChangeBackgroundElement : StoryElement
    {
        public override string ElementName => nameof(ChangeBackgroundElement);

        public override Color32 DisplayColor => _displayColor;
        private Color32 _displayColor = new Color32().Utility();

        public override StoryElementTypes Type => StoryElementTypes.Utility;

        [JsonProperty, JsonConverter(typeof(AssetReferenceJsonConverter))]
        [ShowAssetPreview(128, 128)]
        public Sprite Background;

        [JsonProperty]
        public bool Fade = true;

        [JsonProperty]
        public float FadeTime = 1f;

#if UNITY_EDITOR
        public override void DisplayLayout(Rect layoutRect, GUIStyle label)
        {
            Background = EditorGUILayout.ObjectField("Background", Background, typeof(Sprite), false, GUILayout.Height(EditorGUIUtility.singleLineHeight)) as Sprite;

            Fade = GUILayout.Toggle(Fade, "Fade");
            if (Fade)
                FadeTime = EditorGUILayout.FloatField("Fade out time", FadeTime);
        }
#endif

        public override IEnumerator Execute(UVNFManager managerCallback, UVNFCanvas canvas)
        {
            if (Fade)
                canvas.ChangeBackground(Background, FadeTime);
            else
                canvas.ChangeBackground(Background);
            return null;
        }
    }
}