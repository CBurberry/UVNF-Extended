using System;
using System.Collections;
using UnityEngine;
using XNode;
using UVNF.Core.UI;
using UVNF.Entities.Containers;
using UnityEngine.Localization.Tables;
using UnityEngine.Localization.Metadata;
using UnityEngine.Localization.Settings;
using UnityEditor;
using System.Linq;

namespace UVNF.Core.Story
{
    [NodeWidth(300)]
    public abstract class StoryElement : Node, IComparable
    {
        public StoryGraph storyGraph => graph as StoryGraph;

        //Set elementName to nameof(classname) in deriving classes
        public abstract string ElementName { get; }
        public abstract Color32 DisplayColor { get; }
        public abstract StoryElementTypes Type { get; }

        public virtual bool IsVisible() { return true; }

        [HideInInspector]
        public bool Active = false;
        [HideInInspector]
        public StoryElement Next;

        [HideInInspector]
        [Input(ShowBackingValue.Never, ConnectionType.Multiple)] public NodePort PreviousNode;
        [HideInInspector]
        [Output(ShowBackingValue.Never, ConnectionType.Override)] public NodePort NextNode;

#if UNITY_EDITOR
        public bool HasValidAssetTable => storyGraph.AssetTableCollection != null
            && storyGraph.AssetTableCollection.TableCollectionNameReference.ReferenceType != TableReference.Type.Empty;
        public bool HasValidStringTable => storyGraph.StringTableCollection != null 
            && storyGraph.StringTableCollection.TableCollectionNameReference.ReferenceType != TableReference.Type.Empty;
#endif

#if UNITY_EDITOR
        private GUIStyle _multiLineLabelStyle;
        protected GUIStyle multiLineLabelStyle
        {
            get
            {
                if (_multiLineLabelStyle == null)
                {
                    _multiLineLabelStyle = GetMultiLineLabelStyle();
                }
                return _multiLineLabelStyle;
            }
        }
#endif

        public override object GetValue(NodePort port)
        {
            if (port.IsConnected)
                return port.Connection.node;
            return null;
        }

        protected override void Init()
        {
            base.Init();

            //Note: This call will be invoked when NodeGraph.AddNode method is invoked.
#if UNITY_EDITOR
            if (Application.isEditor)
            {
                UpdateLocalizationPreview();
            }
#endif
        }

        public virtual void OnCreate() { }
        public virtual void OnDelete() { }

#if UNITY_EDITOR
        public virtual void UpdateLocalizationPreview() { }
#endif

        protected virtual void LoadAssets(TableReference tableReference) { }
        protected virtual void LoadStrings(TableReference tableReference) { }

        public virtual void Connect()
        {
            if (GetOutputPort("NextNode").IsConnected)
                Next = GetOutputPort("NextNode").Connection.node as StoryElement;
        }

        public abstract IEnumerator Execute(UVNFManager managerCallback, UVNFCanvas canvas);

#if UNITY_EDITOR
        public abstract void DisplayLayout(Rect layoutRect, GUIStyle label = null);

        public virtual void DisplayNodeLayout(Rect layoutRect) { DisplayLayout(layoutRect); }

#endif

        public int CompareTo(object obj)
        {
            if (obj == null) return 1;
            if (!(obj is StoryElement)) return 1;
            return string.Compare(ElementName, ((StoryElement)obj).ElementName);
        }

        protected string GetLocalizedString(string entryName)
        {
            if (string.IsNullOrWhiteSpace(storyGraph.StringTableName)) 
            {
                return null;
            }

            var entry = LocalizationSettings.StringDatabase?.GetTable(storyGraph.StringTableName)?.GetEntry(entryName);

            // We can also extract Metadata here
            var comment = entry?.GetMetadata<Comment>();
            if (comment != null)
            {
                Debug.Log($"Found metadata comment for {entryName} - {comment.CommentText}");
            }

            return entry?.GetLocalizedString(); // We can pass in optional arguments for Smart Format or String.Format here.
        }

        public static Type InferElementTypeFromName(string elementName)
        {
            var listOfSubtypes = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(domainAssembly => domainAssembly.GetTypes())
                .Where(type => type.IsSubclassOf(typeof(StoryElement))
                ).ToList();

            return listOfSubtypes.First(t => t.Name == elementName);
        }

        //T is the underlying asset reference .e.g Texture2D, AudioClip etc.
        protected T GetLocalizedAsset<T>(string entryName) where T : UnityEngine.Object
        {
            if (string.IsNullOrWhiteSpace(storyGraph.AssetTableName))
            {
                return null;
            }

            return LocalizationSettings.AssetDatabase?.GetLocalizedAsset<T>(storyGraph.AssetTableName, entryName);
        }

#if UNITY_EDITOR
        private GUIStyle GetMultiLineLabelStyle()
        {
            GUIStyle textStyle = EditorStyles.label;
            textStyle.wordWrap = true;
            return textStyle;
        }
#endif
    }
}