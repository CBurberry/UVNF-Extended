using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using XNode;
using UVNF.Core.UI;
using UVNF.Extensions;
using Newtonsoft.Json;
using UVNF.Entities.Containers;
using UnityEditor;

namespace UVNF.Core.Story.Dialogue
{
    public class ChoiceElement : StoryElement
    {
        public override string ElementName => nameof(ChoiceElement);

        public override Color32 DisplayColor => _displayColor;
        private Color32 _displayColor = new Color32().Story();

        public override StoryElementTypes Type => StoryElementTypes.Story;

        [JsonProperty]
        public List<string> ChoiceKeys = new List<string>();

        [JsonProperty]
        public bool ShuffleChoices = true;

        [JsonProperty]
        public bool HideDialogue = false;

        public string GetLocalizedChoice(int index)
            => index > -1 && index < ChoiceKeys.Count ? GetLocalizedString(ChoiceKeys[index]) : null;

        public const string CHOICE_PORT_NAME = "Choice";

#if UNITY_EDITOR
        public override void DisplayLayout(Rect layoutRect, GUIStyle label)
        {
            //NOTE: Function not called! CustomNodeEditors.ChoiceNodeEditor.OnBodyGUI is called instead!
        }

        public void AddChoice()
        {
            ChoiceKeys.Add(string.Empty);
            AddDynamicOutput(typeof(NodePort), ConnectionType.Override, TypeConstraint.Inherited, CHOICE_PORT_NAME + (ChoiceKeys.Count - 1));
        }

        public void RemoveChoice(int index)
        {
            ChoiceKeys.RemoveAt(index);
            RemoveDynamicPort(DynamicPorts.ElementAt(index));
        }
#endif

        public override IEnumerator Execute(UVNFManager managerCallback, UVNFCanvas canvas)
        {
            if (ShuffleChoices)
            {
                //Ensure the shuffle maintains order parity with the keys
                int seed = (graph as StoryGraph).random.Next();
                ChoiceKeys.Shuffle(seed);
            }

            canvas.DisplayChoice(ChoiceKeys.ToArray(), HideDialogue);
            while (canvas.ChoiceCallback == -1) yield return null;

            if (DynamicPorts.ElementAt(canvas.ChoiceCallback).IsConnected)
            {
                int choice = canvas.ChoiceCallback;
                canvas.ResetChoice();
                managerCallback.AdvanceStoryGraph(DynamicPorts.ElementAt(choice).Connection.node as StoryElement);
            }
        }
    }
}