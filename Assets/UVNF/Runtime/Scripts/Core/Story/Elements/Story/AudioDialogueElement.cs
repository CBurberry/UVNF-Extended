using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.Collections;
using UnityEditor;
using UnityEngine;
using UVNF.Core.UI;

namespace UVNF.Core.Story.Dialogue
{
    public class AudioDialogueElement : DialogueElement
    {
        private enum DialogueAudioType 
        {
            Beep = 0,
            DialogueClip
        };

        public override string ElementName => nameof(AudioDialogueElement);

        public override StoryElementTypes Type => StoryElementTypes.Story;

        [JsonProperty]
        public bool BeepOnVowel;

        [JsonProperty]
        public bool PitchShift;

        [JsonProperty]
        public float MaxPitch;

        [JsonProperty, JsonConverter(typeof(AssetReferenceJsonConverter))]
        public AudioClip BoopSoundEffect;

        [JsonProperty]
        public string DialogueAudioKey;

        [JsonProperty, JsonConverter(typeof(StringEnumConverter))]
        private DialogueAudioType audioType;

#if UNITY_EDITOR
        public override void DisplayLayout(Rect layoutRect, GUIStyle label)
        {
            base.DisplayLayout(layoutRect, label);
            audioType = (DialogueAudioType) EditorGUILayout.EnumPopup("Audio Type:", audioType);

            GUILayout.Space(5);
            EditorGUILayout.LabelField("Audio Settings", EditorStyles.boldLabel);
            if (audioType == DialogueAudioType.Beep)
            {
                BoopSoundEffect = EditorGUILayout.ObjectField("Sound Effect", BoopSoundEffect, typeof(AudioClip), false) as AudioClip;
                if (BoopSoundEffect == null) 
                {
                    EditorGUILayout.HelpBox("Beep AudioClip Missing!", MessageType.Error);
                }

                GUILayout.Space(2.5f);
                BeepOnVowel = GUILayout.Toggle(BeepOnVowel, "Beep Only On Vowel");
                PitchShift = GUILayout.Toggle(PitchShift, "Pitch Shift");
                if (PitchShift)
                {
                    MaxPitch = EditorGUILayout.Slider(MaxPitch, 0f, 0.2f);
                }
            }
            else
            {
                DialogueAudioKey = EditorGUILayout.TextField("AudioKey", DialogueAudioKey);
                GUILayout.Space(5);
                if (HasValidAssetTable)
                {
                    AudioClip dialogueAudio = GetLocalizedAsset<AudioClip>(DialogueAudioKey);
                    if (dialogueAudio == null)
                    {
                        EditorGUILayout.HelpBox(nameof(DialogueAudioKey) + " not found!", MessageType.Warning);
                    }
                    else
                    {
                        EditorGUILayout.LabelField("Localized AudioClip", EditorStyles.boldLabel);
                        EditorGUILayout.LabelField(AssetDatabase.GetAssetPath(dialogueAudio));
                    }
                }
                else
                {
                    EditorGUILayout.HelpBox("Graph missing AssetTableCollection!", MessageType.Error);
                }
            }
        }
#endif

        public override IEnumerator Execute(UVNFManager managerCallback, UVNFCanvas canvas)
        {
            if (audioType == DialogueAudioType.Beep)
            {
                return canvas.DisplayText(DialogueKey, CharacterKey, BoopSoundEffect, managerCallback.AudioManager, MaxPitch, BeepOnVowel);
            }
            else
            {
                return canvas.DisplayText(DialogueKey, CharacterKey, DialogueAudioKey, managerCallback.AudioManager);
            }
        }
    }
}