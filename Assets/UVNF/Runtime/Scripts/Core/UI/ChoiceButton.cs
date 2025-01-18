using UnityEngine;
using TMPro;

namespace UVNF.Core.UI
{
    public class ChoiceButton : MonoBehaviour
    {
        public TextMeshProUGUI Text;

        private UVNFCanvas CanvasCallback;
        private int ChoiceIndex;

        private void Awake()
        {
            UVNFManager.Instance.SetTextMeshProUGUILocaleFont(Text);
        }

        public void SetCallback(int choiceIndex, UVNFCanvas callback)
        {
            CanvasCallback = callback;
            ChoiceIndex = choiceIndex;
        }

        public void Chosen()
        {
            CanvasCallback.ChoiceCallback = ChoiceIndex;
        }
    }
}