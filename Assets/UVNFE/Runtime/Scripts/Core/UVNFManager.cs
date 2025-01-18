using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using CoroutineManager;

using UVNF.Core.UI;
using UVNF.Core.Story;
using UVNF.Entities.Containers;
using UVNF.Entities.Containers.Variables;
using UnityEngine.EventSystems;
using System.Linq;
using UnityEngine.Localization.Components;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using TMPro;

namespace UVNF.Core
{
    public class UVNFManager : MonoBehaviour
    {
        public static UVNFManager Instance { get; private set; }

        public const string FONT_KEY = "locale-font";
        public const string FONT_MATERIAL_KEY = "locale-font-material";

        [Header("UDSF Components")]
        public UVNFCanvas Canvas;
        public AudioManager AudioManager;
        public CanvasCharacterManager CharacterManager;

        [Header("Story Graph")]
        public StoryGraph StartingStory;

        private UVNFStoryManager _currentStoryManager;

        private Queue<StoryGraph> _graphQueue = new Queue<StoryGraph>();

        [Header("Localization")]
        [SerializeField]
        private List<LocalizeStringEvent> _localizationStringEvents;
        private string activeStringTableReference;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Debug.LogError($"Duplicate instance of {nameof(UVNFManager)} was instantiated, singleton error.");
                Destroy(this);
            }

            if (FindObjectsOfType<EventSystem>().Count() == 0) 
            {
                GameObject objToSpawn = new GameObject("EventSystem");
                objToSpawn.AddComponent<EventSystem>();
                objToSpawn.AddComponent<StandaloneInputModule>();
            }
        }

        private IEnumerator Start()
        {
            //Wait for localization initialization
            yield return LocalizationSettings.InitializationOperation;

            UpdateAllEventLocaleFont();
            if (StartingStory != null)
            {
                StartStory(StartingStory);
            }
        }

        /// <summary>
        /// Starts a provided StoryGraph
        /// </summary>
        /// <param name="graph"></param>
        /// <returns>True if the Story is started directly. False if the provided Graph is Queued.</returns>
        public bool StartStory(StoryGraph graph)
        {
            if (_currentStoryManager == null)
            {
                Canvas.Show();
                _currentStoryManager = new UVNFStoryManager(graph, this, Canvas, FinishStory);
                return true;
            }

            QueueStory(graph);
            return false;
        }

        public void AddLocalizeStringEvent(LocalizeStringEvent stringEvent)
        {
            stringEvent.SetTable(activeStringTableReference);
            _localizationStringEvents.Add(stringEvent);
        }

        public void RemoveLocalizeStringEvent(LocalizeStringEvent stringEvent)
        {
            _localizationStringEvents.Remove(stringEvent);
        }

        public void SetStringEventsTable(string tableReference)
        {
            Debug.Log("Setting Localize String Events table reference to " + tableReference);
            activeStringTableReference = tableReference;
            foreach (var element in _localizationStringEvents) 
            {
                element.SetTable(tableReference);
            }
        }

        private void OnEnable()
        {
            LocalizationSettings.SelectedLocaleChanged += SelectedLocaleChanged;
        }

        private void OnDisable()
        {
            LocalizationSettings.SelectedLocaleChanged -= SelectedLocaleChanged;
        }

        private void SelectedLocaleChanged(Locale locale)
        {
            Debug.Log("Locale changed to " + locale.LocaleName);

            //Update font and material to match the new locale (in case character sets don't match)
            UpdateAllEventLocaleFont();
        }

        private void QueueStory(StoryGraph graph)
        {
            _graphQueue.Enqueue(graph);
        }

        public void StartSubStory(StoryGraph subGraph)
        {
            _currentStoryManager.CreateSubStory(subGraph, this, Canvas);
        }

        public void AdvanceStoryGraph(StoryElement element)
        {
            _currentStoryManager.AdvanceStory(element);
        }

        public void AdvanceStoryGraph(StoryElement element, bool continueInBackground)
        {
            _currentStoryManager.AdvanceStory(element, continueInBackground);
        }

        private void FinishStory()
        {
            _currentStoryManager = null;
            //Story still left in the Queue
            if (_graphQueue.Count > 0)
            {
                StartStory(_graphQueue.Dequeue());
            }
            //Story if finished
            else
            {
                Canvas.Hide();
                CharacterManager.Hide();
            }
        }

        public void UpdateAllEventLocaleFont()
        {
            //Get relevant TMP Object references, set font from asset table
            TMP_FontAsset font = LocalizationSettings.AssetDatabase.GetLocalizedAsset<TMP_FontAsset>(FONT_KEY);
            Material fontMaterial = LocalizationSettings.AssetDatabase.GetLocalizedAsset<Material>(FONT_MATERIAL_KEY);

            if (font == null || fontMaterial == null)
            {
                Debug.LogError($"Could not change font due to missing font or font material asset! Locale: {LocalizationSettings.SelectedLocale.LocaleName}");
                return;
            }

            foreach (var element in _localizationStringEvents)
            {
                SetTextMeshProUGUILocaleFont(element.gameObject.GetComponent<TextMeshProUGUI>(), font, fontMaterial);
            }
        }

        public void SetTextMeshProUGUILocaleFont(TextMeshProUGUI target, TMP_FontAsset font = null, Material fontMaterial = null)
        {
            //Get relevant TMP Object references if not provided, set font from asset table
            font ??= LocalizationSettings.AssetDatabase.GetLocalizedAsset<TMP_FontAsset>(FONT_KEY);
            fontMaterial ??= LocalizationSettings.AssetDatabase.GetLocalizedAsset<Material>(FONT_MATERIAL_KEY);

            target.font = font;
            target.fontSharedMaterial = fontMaterial;
            target.UpdateFontAsset();
        }
    }
    
    /// <summary>
    /// Handles a provided StoryGraph
    /// </summary>
    internal class UVNFStoryManager
    {
        private UVNFManager _manager;
        private UVNFCanvas _canvas;

        private StoryGraph _storyGraph;
        private StoryElement _currentElement;

        private TaskManager.TaskState _currentTask;

        private UVNFStoryManager _subgraphHandler;
        private bool _handlingSubgraph = false;

        private event Action _afterSubgraphHandler;

        private StoryElement _afterSubgraphElement;

        /// <summary>
        /// Creates a StoryManager that automatically starts at the start of the provided Graph
        /// </summary>
        /// <param name="graph"></param>
        /// <param name="manager"></param>
        /// <param name="canvas"></param>
        /// <param name="afterStoryHandler"></param>
        public UVNFStoryManager(StoryGraph graph, UVNFManager manager, UVNFCanvas canvas, Action afterStoryHandler)
        {
            _storyGraph = graph;

            _manager = manager;
            _canvas = canvas;
            Debug.Log("Locale set to: " + LocalizationSettings.SelectedLocale.ToString());
            _manager.AudioManager.SetAudioEventsTable(graph.AssetTableName);
            _manager.SetStringEventsTable(graph.StringTableName);

            if (_storyGraph != null)
            {
                StartStory();
            }

            _afterSubgraphHandler += afterStoryHandler;
        }

        public void CreateSubStory(StoryGraph graph, UVNFManager manager, UVNFCanvas canvas)
        {
            if (_subgraphHandler == null)
            {
                _afterSubgraphElement = _currentElement.Next;

                _subgraphHandler = new UVNFStoryManager(graph, manager, canvas, HandleSubgraphFinish);
                _handlingSubgraph = true;
            }
            else
            {
                _subgraphHandler.CreateSubStory(graph, manager, canvas);
            }
        }

        public void HandleSubgraphFinish()
        {
            _subgraphHandler = null;
            _handlingSubgraph = false;

            AdvanceStory(_afterSubgraphElement);
        }

        #region StoryElements
        public void StartStory()
        {
            _storyGraph.ConnectStoryElements();
            _currentElement = _storyGraph.GetRootStory()[0];

            _currentTask = TaskManager.CreateTask(_currentElement.Execute(_manager, _canvas));
            _currentTask.Finished += AdvanceStory;

            _currentTask.Start();
        }

        public void AdvanceStory(bool manual)
        {
            if (!manual && !_handlingSubgraph)
            {
                if (_currentElement.Next != null && _currentTask != null && !_currentTask.Running)
                {
                    _currentElement = _currentElement.Next;

                    _currentTask = TaskManager.CreateTask(_currentElement.Execute(_manager, _canvas));
                    _currentTask.Finished += AdvanceStory;
                    _currentTask.Start();
                }
                else
                    _afterSubgraphHandler?.Invoke();
            }
        }

        public void AdvanceStory(StoryElement newStoryPoint, bool continueToRun = false)
        {
            if (_handlingSubgraph)
                _subgraphHandler.AdvanceStory(false);
            else if (newStoryPoint != null)
            {
                if(!continueToRun)
                    _currentTask.Stop();

                _currentElement = newStoryPoint;

                _currentTask = TaskManager.CreateTask(_currentElement.Execute(_manager, _canvas));
                _currentTask.Finished += AdvanceStory;
                _currentTask.Start();
            }
            else
                _afterSubgraphHandler?.Invoke();
        }
        #endregion
    }
}