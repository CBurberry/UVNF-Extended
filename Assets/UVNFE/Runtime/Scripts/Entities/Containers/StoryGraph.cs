using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using XNode;
using UVNF.Core.Story;
using UVNF.Core.Story.Other;
using UVNF.Entities.Containers.Variables;


#if UNITY_EDITOR
using UnityEditor.Localization;
#endif

namespace UVNF.Entities.Containers
{
    [CreateAssetMenu(fileName = "NewStory", menuName = "UVNF/Story Graph")]
    public class StoryGraph : NodeGraph
    {
        //Add a common scope random such that graph elements can use as necessary (for deterministic seeding)
        public readonly System.Random random = new System.Random();

        public List<StoryElement> StoryElements
        {
            get
            {
                if (_storyElements.Count != nodes.Count)
                {
                    //_storyElements.Clear();
                    //for (int i = 0; i < nodes.Count; i++)
                    //    _storyElements.Add((StoryElement)nodes[i]);

                    RefreshStories();
                }
                return _storyElements;
            }
        }
        private List<StoryElement> _storyElements = new List<StoryElement>();

        public string[] StoryNames = new string[] { };
        private List<StoryElement>[] _shortStories = new List<StoryElement>[] { };

        public VariableManager variableManager;

        [HideInInspector]
        public string AssetTableName;

        [HideInInspector]
        public string StringTableName;

#if UNITY_EDITOR
        //Code smell: use of editor localization classes in runtime code.
        public AssetTableCollection AssetTableCollection;
        public StringTableCollection StringTableCollection;

        //Refresh all node preview elements in xNode graph view.
        //N.B. At runtime, key values drive Localization events without these preview strings.
        public void LocalizeGraphNodePreview()
        {
            foreach (var node in nodes)
            {
                (node as StoryElement)?.UpdateLocalizationPreview();
            }
        }
#endif

        public void RefreshStories()
        {
            Node[] startNodesArray = nodes.Where(x => x.GetType() == typeof(StartElement)).ToArray();
            StartElement[] startNodes = new StartElement[startNodesArray.Length];

            for (int i = 0; i < startNodes.Length; i++)
                startNodes[i] = startNodesArray[i] as StartElement;

            StoryNames = startNodes.Select(x => x.StoryName).ToArray();

            _shortStories = new List<StoryElement>[startNodes.Length];
            for (int i = 0; i < _shortStories.Length; i++)
            {
                _shortStories[i] = new List<StoryElement>();
                _shortStories[i].Add(startNodes[i]);

                StartElement currentStartNode = startNodes[i];
                StoryElement currentNode = startNodes[i].GetOutputPort("NextNode").GetOutputValue() as StoryElement;
                while (currentNode != null && currentNode.GetOutputPort("NextNode").IsConnected && currentNode.GetOutputPort("NextNode").GetOutputValue().GetType() != typeof(StartElement))
                {
                    _shortStories[i].Add(currentNode);
                    currentNode = currentNode.GetOutputPort("NextNode").GetOutputValue() as StoryElement;
                }
                if (currentNode != null && currentNode.GetType() != typeof(StartElement))
                    _shortStories[i].Add(currentNode);
            }
        }

        public List<StoryElement> ShortStory(int storyIndex)
        {
            if (storyIndex < _shortStories.Length && storyIndex > -1)
                return _shortStories[storyIndex];
            return new List<StoryElement>();
        }

        public List<StoryElement> GetRootStory()
        {
            RefreshStories();
            return _shortStories.Where(x => (x[0] as StartElement).IsRoot).First().ToList();
        }

        public void ConnectStoryElements()
        {
            nodes = nodes.Where(x => x != null).ToList();
            foreach (StoryElement element in nodes.OfType<StoryElement>())
            {
                element.Connect();
            }
        }
    }
}