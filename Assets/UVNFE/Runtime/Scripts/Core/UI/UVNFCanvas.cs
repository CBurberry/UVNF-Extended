using System.Collections;
using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem;
using UVNF.Extensions;
using UnityEngine.Localization.Components;

namespace UVNF.Core.UI
{
    public class UVNFCanvas : MonoBehaviour
    {
        [Header("Canvas Group")]
        public CanvasGroup BottomCanvasGroup;
        public CanvasGroup ChoiceCanvasGroup;
        public CanvasGroup LoadingCanvasGroup;
        public CanvasGroup BackgroundCanvasGroup;

        [Header("Dialogue")]
        public TextMeshProUGUI DialogueTMP;
        public TextMeshProUGUI CharacterTMP;
        public GameObject CharacterNamePlate;

        public float TextDisplayInterval = 0.05f;
        private float tempDisplayInterval = 0f;

        private float displayIntervalTimer = 0f;

        [Header("Choices")]
        public GameObject ChoiceButton;
        public Transform ChoicePanelTransform;

        [Header("Background")]
        public Image BackgroundImage;
        public Image BackgroundFade;

        [Header("Localization")]
        public LocalizeStringEvent CharacterNameBoxStringEvent;
        public LocalizeStringEvent DialogueBoxStringEvent;

        [Header("Misc")]
        public int ChoiceCallback = -1;
        public void ResetChoice() => ChoiceCallback = -1;

        public bool BottomPanelEnabled => BottomCanvasGroup.gameObject.activeSelf;
        public bool ChoiceCanvasEnabled => ChoiceCanvasGroup.gameObject.activeSelf;
        public bool LoadingCanvasEnabled => LoadingCanvasGroup.gameObject.activeSelf;

        //TODO: Support more input systems (Mouse.current throwing null ref on build likely due to unset input action map)
        private bool HasInput => Input.GetMouseButtonDown(0);

        private const string MISSING_TRANSLATION_STRING = "<MISSING TRANSLATION>";

        private void Awake()
        {
            if (BackgroundCanvasGroup != null)
                BackgroundCanvasGroup.gameObject.SetActive(true);
            if (ChoiceCanvasGroup != null)
                ChoiceCanvasGroup.gameObject.SetActive(false);
            if (BottomCanvasGroup != null)
                BottomCanvasGroup.gameObject.SetActive(false);
        }

        //TODO: Refactor, lots of copy-paste here.
        #region Dialogue
        public IEnumerator DisplayText(string textKey, params TextDisplayStyle[] displayStyles)
        {
            //Remove string binding such that the text does not update immediately and we can play the animation
            DialogueBoxStringEvent.OnUpdateString.RemoveListener(UpdateDialogueTMP);
            DialogueBoxStringEvent.SetEntry(textKey);
            string text = DialogueBoxStringEvent.StringReference.GetLocalizedString();
            if (string.IsNullOrEmpty(text)) 
            {
                text = MISSING_TRANSLATION_STRING;
            }

            ApplyTextDisplayStylesToTMP(DialogueTMP, displayStyles);
            BottomCanvasGroup.gameObject.SetActive(true);

            CharacterNamePlate.SetActive(false);

            int textIndex = 0;
            while (textIndex < text.Length)
            {
                if (HasInput)
                {
                    DialogueTMP.text = text;
                    textIndex = text.Length - 1;
                }
                else if (displayIntervalTimer >= tempDisplayInterval)
                {
                    DialogueTMP.text += ApplyTypography(text, ref textIndex);
                    textIndex++;
                    displayIntervalTimer = 0f;
                }
                else
                {
                    displayIntervalTimer += Time.deltaTime;
                }
                yield return null;
            }

            //Add bindings back after animation to respond to locale change events etc,
            // also set value to TMP again in case a localization event occurred during the coroutine.
            DialogueBoxStringEvent.OnUpdateString.AddListener(UpdateDialogueTMP);
            DialogueTMP.text = DialogueBoxStringEvent.StringReference.GetLocalizedString();
            while (!HasInput)
            {
                yield return null;
            }
        }

        public IEnumerator DisplayText(string textKey, string characterNameKey, bool useStylesForCharacterField = false, params TextDisplayStyle[] displayStyles)
        {
            //Remove string binding such that the text does not update immediately and we can play the animation
            CharacterNameBoxStringEvent.OnUpdateString.RemoveListener(UpdateCharacterTMP);
            CharacterNameBoxStringEvent.SetEntry(characterNameKey);
            DialogueBoxStringEvent.OnUpdateString.RemoveListener(UpdateDialogueTMP);
            DialogueBoxStringEvent.SetEntry(textKey);
            string characterName = CharacterNameBoxStringEvent.StringReference.GetLocalizedString();

            //Add a check here as characterNameKey being empty is a way to hide the character name box
            if (characterNameKey.Length == 0 && string.IsNullOrEmpty(characterName))
            {
                characterName = MISSING_TRANSLATION_STRING;
            }
            string text = DialogueBoxStringEvent.StringReference.GetLocalizedString();
            if (string.IsNullOrEmpty(text))
            {
                text = MISSING_TRANSLATION_STRING;
            }

            ApplyTextDisplayStylesToTMP(DialogueTMP, displayStyles);
            if (useStylesForCharacterField)
                ApplyTextDisplayStylesToTMP(CharacterTMP, displayStyles);

            CharacterNamePlate.SetActive(!string.IsNullOrEmpty(characterName));

            BottomCanvasGroup.gameObject.SetActive(true);

            if (!string.Equals(CharacterTMP.text, characterName, StringComparison.Ordinal))
                CharacterTMP.text = characterName;

            int textIndex = 0;
            while (textIndex < text.Length)
            {
                if (HasInput)
                {
                    DialogueTMP.text = text;
                    textIndex = text.Length - 1;
                }
                else if (displayIntervalTimer >= tempDisplayInterval)
                {
                    DialogueTMP.text += ApplyTypography(text, ref textIndex);
                    textIndex++;
                    displayIntervalTimer = 0f;
                }
                else
                {
                    displayIntervalTimer += Time.deltaTime;
                }
                yield return null;
            }

            //Add bindings back after animation to respond to locale change events etc,
            // also set value to TMP again in case a localization event occurred during the coroutine.
            CharacterNameBoxStringEvent.OnUpdateString.AddListener(UpdateCharacterTMP);
            DialogueBoxStringEvent.OnUpdateString.AddListener(UpdateDialogueTMP);
            CharacterTMP.text = CharacterNameBoxStringEvent.StringReference.GetLocalizedString();
            DialogueTMP.text = DialogueBoxStringEvent.StringReference.GetLocalizedString();
            while (!HasInput)
            {
                yield return null;
            }
        }

        public IEnumerator DisplayText(string textKey, string characterNameKey, string dialogueClipKey, AudioManager audio, bool useStylesForCharacterField = false, params TextDisplayStyle[] displayStyles)
        {
            //Remove string binding such that the text does not update immediately and we can play the animation
            CharacterNameBoxStringEvent.OnUpdateString.RemoveListener(UpdateCharacterTMP);
            CharacterNameBoxStringEvent.SetEntry(characterNameKey);
            DialogueBoxStringEvent.OnUpdateString.RemoveListener(UpdateDialogueTMP);
            DialogueBoxStringEvent.SetEntry(textKey);
            string characterName = CharacterNameBoxStringEvent.StringReference.GetLocalizedString();
            if (string.IsNullOrEmpty(characterName))
            {
                characterName = MISSING_TRANSLATION_STRING;
            }
            string text = DialogueBoxStringEvent.StringReference.GetLocalizedString();
            if (string.IsNullOrEmpty(text))
            {
                text = MISSING_TRANSLATION_STRING;
            }

            ApplyTextDisplayStylesToTMP(DialogueTMP, displayStyles);
            if (useStylesForCharacterField)
                ApplyTextDisplayStylesToTMP(CharacterTMP, displayStyles);

            CharacterNamePlate.SetActive(!string.IsNullOrEmpty(characterName));

            BottomCanvasGroup.gameObject.SetActive(true);

            if (!string.Equals(CharacterTMP.text, characterName, StringComparison.Ordinal))
            {
                CharacterTMP.text = characterName;
            }

            audio.PlayLocalizedSound(dialogueClipKey, 1f);

            int textIndex = 0;
            while (textIndex < text.Length)
            {
                if (HasInput)
                {
                    DialogueTMP.text = text;
                    textIndex = text.Length - 1;
                }
                else if (displayIntervalTimer >= tempDisplayInterval)
                {
                    DialogueTMP.text += ApplyTypography(text, ref textIndex);
                    textIndex++;
                    displayIntervalTimer = 0f;
                }
                else
                {
                    displayIntervalTimer += Time.deltaTime;
                }
                yield return null;
            }

            //Add bindings back after animation to respond to locale change events etc,
            // also set value to TMP again in case a localization event occurred during the coroutine.
            CharacterNameBoxStringEvent.OnUpdateString.AddListener(UpdateCharacterTMP);
            DialogueBoxStringEvent.OnUpdateString.AddListener(UpdateDialogueTMP);
            CharacterTMP.text = CharacterNameBoxStringEvent.StringReference.GetLocalizedString();
            DialogueTMP.text = DialogueBoxStringEvent.StringReference.GetLocalizedString();
            while (!HasInput)
            {
                yield return null;
            }
        }

        public IEnumerator DisplayText(string textKey, string characterNameKey, AudioClip boop, AudioManager audio, float pitchShift, bool beepOnVowel = false, bool useStylesForCharacterField = false, params TextDisplayStyle[] displayStyles)
        {
            //Remove string binding such that the text does not update immediately and we can play the animation
            CharacterNameBoxStringEvent.OnUpdateString.RemoveListener(UpdateCharacterTMP);
            CharacterNameBoxStringEvent.SetEntry(characterNameKey);
            DialogueBoxStringEvent.OnUpdateString.RemoveListener(UpdateDialogueTMP);
            DialogueBoxStringEvent.SetEntry(textKey);
            string characterName = CharacterNameBoxStringEvent.StringReference.GetLocalizedString();
            if (string.IsNullOrEmpty(characterName))
            {
                characterName = MISSING_TRANSLATION_STRING;
            }
            string text = DialogueBoxStringEvent.StringReference.GetLocalizedString();
            if (string.IsNullOrEmpty(text))
            {
                text = MISSING_TRANSLATION_STRING;
            }

            ApplyTextDisplayStylesToTMP(DialogueTMP, displayStyles);
            if (useStylesForCharacterField)
                ApplyTextDisplayStylesToTMP(CharacterTMP, displayStyles);

            CharacterNamePlate.SetActive(!string.IsNullOrEmpty(characterName));

            BottomCanvasGroup.gameObject.SetActive(true);

            if (!string.Equals(CharacterTMP.text, characterName, StringComparison.Ordinal))
            {
                CharacterTMP.text = characterName;
            }

            if (boop == null)
            {
                Debug.LogWarning($"{nameof(UVNFCanvas)}.{nameof(DisplayText)}: Boop SFX argument was NULL! No audio will be played.");
            }

            int textIndex = 0;
            while (textIndex < text.Length)
            {
                if (HasInput)
                {
                    DialogueTMP.text = text;
                    textIndex = text.Length - 1;
                }
                else if (displayIntervalTimer >= tempDisplayInterval)
                {
                    DialogueTMP.text += ApplyTypography(text, ref textIndex);

                    if (boop != null && text[textIndex] != ' ')
                    {
                        if (beepOnVowel && text[textIndex].IsVowel())
                        {
                            audio.PlaySound(boop, 1f, UnityEngine.Random.Range(0, pitchShift));
                        }
                        else if (!beepOnVowel)
                        {
                            audio.PlaySound(boop, 1f, UnityEngine.Random.Range(0, pitchShift));
                        }
                    }

                    textIndex++;
                    displayIntervalTimer = 0f;

                }
                else
                {
                    displayIntervalTimer += Time.deltaTime;
                }
                yield return null;
            }

            //Add bindings back after animation to respond to locale change events etc,
            // also set value to TMP again in case a localization event occurred during the coroutine.
            CharacterNameBoxStringEvent.OnUpdateString.AddListener(UpdateCharacterTMP);
            DialogueBoxStringEvent.OnUpdateString.AddListener(UpdateDialogueTMP);
            CharacterTMP.text = CharacterNameBoxStringEvent.StringReference.GetLocalizedString();
            DialogueTMP.text = DialogueBoxStringEvent.StringReference.GetLocalizedString();
            while (!HasInput)
            {
                yield return null;
            }
        }

        #endregion

        #region Choice
        public void DisplayChoice(string[] optionKeys, bool hideDialogue = true, params TextDisplayStyle[] displayStyles)
        {
            StartCoroutine(DisplayChoiceCoroutine(optionKeys, hideDialogue, displayStyles));
        }

        public IEnumerator DisplayChoiceCoroutine(string[] optionKeys, bool hideDialogue = true, params TextDisplayStyle[] displayStyles)
        {
            BottomCanvasGroup.gameObject.SetActive(!hideDialogue);
            ChoiceCanvasGroup.gameObject.SetActive(true);

            foreach (Transform child in ChoicePanelTransform)
            {
                Destroy(child.gameObject);
            }

            for (int i = 0; i < optionKeys.Length; i++)
            {
                ChoiceButton button = Instantiate(ChoiceButton, ChoicePanelTransform).GetComponent<ChoiceButton>();
                LocalizeStringEvent choiceStringEvent = button.GetComponentInChildren<LocalizeStringEvent>();
                UVNFManager.Instance.AddLocalizeStringEvent(choiceStringEvent);
                choiceStringEvent.SetEntry(optionKeys[i]);
                button.SetCallback(i, this);
            }

            while (ChoiceCallback == -1) yield return null;

            ChoiceCanvasGroup.gameObject.SetActive(false);

            foreach (Transform child in ChoicePanelTransform)
            {
                LocalizeStringEvent choiceStringEvent = child.GetComponentInChildren<LocalizeStringEvent>();
                UVNFManager.Instance.RemoveLocalizeStringEvent(choiceStringEvent);
                Destroy(child.gameObject);
            }
        }
        #endregion

        #region Utility
        public IEnumerator FadeCanvasGroup(CanvasGroup canvasGroup, float time = 1f)
        {
            if (time <= 0f)
            {
                time = 1f;
                Debug.LogWarning("Tried to fade canvas group with a value for time that's less or equal to zero.");
            }

            while (canvasGroup.alpha != 0f)
            {
                canvasGroup.alpha -= Time.deltaTime / time;
                yield return null;
            }
            canvasGroup.gameObject.SetActive(false);
        }

        public IEnumerator UnfadeCanvasGroup(CanvasGroup canvasGroup, float time = 1f)
        {
            canvasGroup.gameObject.SetActive(true);

            if (time <= 0f)
            {
                time = 1f;
                Debug.LogWarning("Tried to unfade canvas group with a value for time that's less or equal to zero.");
            }

            while (canvasGroup.alpha != 1f)
            {
                canvasGroup.alpha += Time.deltaTime / time;
                yield return null;
            }
        }

        public void ShowLoadScreen(float time = 1f, bool hideOtherComponents = false)
        {
            if (time <= 0f)
            {
                time = 1f;
                Debug.LogWarning("Tried to show load screen with a value for time that's less or equal to zero.");
            }

            if (hideOtherComponents)
            {
                StartCoroutine(FadeCanvasGroup(BottomCanvasGroup, time));
                StartCoroutine(FadeCanvasGroup(ChoiceCanvasGroup, time));
            }
            StartCoroutine(UnfadeCanvasGroup(LoadingCanvasGroup, time));
        }

        public void HideLoadScreen(float time = 1f, bool showOtherComponents = false)
        {
            if (time <= 0f)
            {
                time = 1f;
                Debug.LogWarning("Tried to show load screen with a value for time that's less or equal to zero.");
            }

            if (showOtherComponents)
            {
                StartCoroutine(UnfadeCanvasGroup(BottomCanvasGroup, time));
                StartCoroutine(UnfadeCanvasGroup(ChoiceCanvasGroup, time));
            }
            StartCoroutine(FadeCanvasGroup(LoadingCanvasGroup, time));
        }
        #endregion

        #region Scenery
        public void ChangeBackground(Sprite newBackground)
        {
            BackgroundCanvasGroup.gameObject.SetActive(true);
            BackgroundImage.sprite = newBackground;
        }

        public void ChangeBackground(Sprite newBackground, float transitionTime)
        {
            BackgroundCanvasGroup.gameObject.SetActive(true);
            Color32 alpha = BackgroundFade.color;
            alpha.a = 255;
            BackgroundFade.color = alpha;

            BackgroundFade.sprite = BackgroundImage.sprite;
            BackgroundImage.sprite = newBackground;

            BackgroundFade.canvasRenderer.SetAlpha(1f);
            BackgroundFade.CrossFadeAlpha(0f, transitionTime, false);
        }
        #endregion

        private void ApplyTextDisplayStylesToTMP(TextMeshProUGUI tmp, TextDisplayStyle[] displayStyles)
        {
            ResetTMP(tmp);
            for (int i = 0; i < displayStyles.Length; i++)
            {
                switch (displayStyles[i])
                {
                    case TextDisplayStyle.Gigantic:
                        tmp.fontSize = 40f;
                        break;
                    case TextDisplayStyle.Big:
                        tmp.fontSize = 25f;
                        break;
                    case TextDisplayStyle.Small:
                        tmp.fontSize = 12f;
                        break;
                    case TextDisplayStyle.Italic:
                        tmp.fontStyle = FontStyles.Italic;
                        break;
                    case TextDisplayStyle.Bold:
                        tmp.fontStyle = FontStyles.Bold;
                        break;
                    case TextDisplayStyle.Fast:
                        tempDisplayInterval = TextDisplayInterval * 3f;
                        break;
                    case TextDisplayStyle.Slow:
                        tempDisplayInterval = TextDisplayInterval / 2f;
                        break;
                }
            }
        }

        private void ResetTMP(TextMeshProUGUI tmp)
        {
            tmp.fontSize = 18f;
            tmp.fontStyle = 0;
            tmp.text = string.Empty;

            tempDisplayInterval = TextDisplayInterval;
        }

        private string ApplyTypography(string text, ref int textIndex)
        {
            if (text[textIndex] == '<')
            {
                string subString = text.Substring(textIndex);
                int endMark = subString.IndexOf('>');
                if (endMark < 0)
                    return text[textIndex].ToString();
                textIndex += endMark;
                return subString.Substring(0, endMark + 1);
            }
            else return text[textIndex].ToString();
        }

        public void Hide()
        {
            if (BackgroundCanvasGroup != null)
                BackgroundCanvasGroup.gameObject.SetActive(false);
            if (ChoiceCanvasGroup != null)
                ChoiceCanvasGroup.gameObject.SetActive(false);
            if (BottomCanvasGroup != null)
                BottomCanvasGroup.gameObject.SetActive(false);
            if (CharacterNamePlate != null)
                CharacterNamePlate.gameObject.SetActive(false);
        }

        public void Show()
        {
            if (BackgroundCanvasGroup != null)
                BackgroundCanvasGroup.gameObject.SetActive(true);
        }

        public void UpdateCharacterTMP(string text)
        {
            Debug.Log("UpdateCharacterTMP");
            CharacterTMP.text = text;
        }

        public void UpdateDialogueTMP(string text)
        {
            Debug.Log("UpdateDialogueTMP");
            DialogueTMP.text = text;
        }
    }
}