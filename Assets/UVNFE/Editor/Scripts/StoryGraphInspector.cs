using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.Localization;
using UnityEngine;
using UnityEngine.Localization.Tables;
using UVNF.Core.Story;
using UVNF.Core.Story.Dialogue;
using UVNF.Entities.Containers;
using UVNF.Entities.Containers.Variables;
using UVNF.Json;
using XNode;
using XNode.Json;
using XNode.NodeGroups;
using XNodeEditor;

[CustomEditor(typeof(StoryGraph))]
public class StoryGraphEditor : Editor
{
    private StoryGraph storyGraph => (StoryGraph)target;

    private bool enableSimpleJson = true;
    private bool useCustomFilePath;
    private string targetFilePath = "";
    private const int INDENT_SIZE = 2;

    //Fixed GUI Contents
    private GUIContent createSimpleJsonLabelContent
        = new GUIContent("Enable Simple JSON", "When importing/exporting JSON, a designer-friendly JSON file will be used and overwrite the full JSON if edited.");
    private GUIContent customFilePathLabelContent
        = new GUIContent("Use Custom File Path", "Generate files in a different location. (Default is same name/folder)");
    private GUIContent customTargetFilePathLabelContent
        = new GUIContent("Target file path: ", "Generate the new asset with the given name (relative to Project folder), include extension.");

    //GUI Styles
    private GUIStyle paddedHelpBox;
    private GUIStyle paddedGroupBox;

    private const string JSON_EXTENSION = ".json";
    private const string SIMPLE_JSON_EXTENSION = ".df-json";
    private const string SIMPLE_JSON_EDITOR_PREFS_KEY = "SGE.ENABLE_SIMPLE_JSON";

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        paddedHelpBox = new GUIStyle("HelpBox");
        paddedHelpBox.padding.left += 5;
        paddedHelpBox.padding.right += 5;
        paddedGroupBox = new GUIStyle("GroupBox");
        paddedGroupBox.padding.left += 10;

        GUILayout.BeginVertical(paddedHelpBox);
        EditorGUILayout.LabelField(nameof(StoryGraph) + " Fields", EditorStyles.boldLabel);
        GUILayout.BeginVertical(paddedGroupBox);
        DrawPropertiesExcluding(serializedObject, new string[] { "m_Script", "AssetTableCollection", "StringTableCollection" });
        EditorGUI.BeginChangeCheck();
        storyGraph.AssetTableCollection = EditorGUILayout.ObjectField(
                "Asset Table Collection",
                storyGraph.AssetTableCollection,
                typeof(AssetTableCollection),
                false
            ) as AssetTableCollection;
        storyGraph.StringTableCollection = EditorGUILayout.ObjectField(
                "String Table Collection",
                storyGraph.StringTableCollection,
                typeof(StringTableCollection),
                false
            ) as StringTableCollection;
        bool localizationTableChanged = EditorGUI.EndChangeCheck();
        GUILayout.EndVertical();
        GUILayout.Space(10);

        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
        EditorGUILayout.LabelField("JSON Inport/Export", EditorStyles.boldLabel);
        GUILayout.BeginVertical(paddedGroupBox);
        useCustomFilePath = EditorGUILayout.Toggle(customFilePathLabelContent, useCustomFilePath);
        if (useCustomFilePath)
        {
            GUILayout.Space(5);
            targetFilePath = EditorGUILayout.TextField(
                    customTargetFilePathLabelContent,
                    targetFilePath.Length == 0 ? AssetDatabase.GetAssetPath(storyGraph).Replace(".asset", ".json") : targetFilePath
            );
        }

        EditorGUI.BeginChangeCheck();
        enableSimpleJson = EditorGUILayout.Toggle(createSimpleJsonLabelContent, EditorPrefs.GetBool(SIMPLE_JSON_EDITOR_PREFS_KEY));
        if (EditorGUI.EndChangeCheck()) 
        {
            //Save changes to this value as this would be a pain to reset every time.
            EditorPrefs.SetBool(SIMPLE_JSON_EDITOR_PREFS_KEY, enableSimpleJson);
        }
        
        GUILayout.Space(10);
        GUILayout.BeginHorizontal();
        ExportJson(targetFilePath);
        GUILayout.Space(10);
        ImportJsonNextFrame();
        GUILayout.EndHorizontal();
        GUILayout.EndVertical();
        GUILayout.Space(10);
        GUILayout.EndVertical();

        if (storyGraph.AssetTableCollection == null)
        {
            EditorGUILayout.HelpBox("REQUIRED: Asset Table Collection", MessageType.Error);
        }

        if (storyGraph.StringTableCollection == null)
        {
            EditorGUILayout.HelpBox("REQUIRED: String Table Collection", MessageType.Error);
        }

        if (localizationTableChanged)
        {
            if (storyGraph.AssetTableCollection != null && storyGraph.AssetTableCollection.TableCollectionNameReference.ReferenceType != TableReference.Type.Empty)
            {
                storyGraph.AssetTableName = storyGraph.AssetTableCollection.TableCollectionName;
            }

            if (storyGraph.StringTableCollection != null && storyGraph.StringTableCollection.TableCollectionNameReference.ReferenceType != TableReference.Type.Empty)
            {
                storyGraph.StringTableName = storyGraph.StringTableCollection.TableCollectionName;
            }

            storyGraph.LocalizeGraphNodePreview();
        }

        // Apply changes to the serializedProperty 
        // Checking the Event type lets us update after Undo and Redo commands.
        if (serializedObject.ApplyModifiedProperties() ||
            (Event.current.type == EventType.ValidateCommand &&
            Event.current.commandName == "UndoRedoPerformed"))
        {
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
    }

    private void ExportJson(string targetFilePath)
    {
        // Add a custom button to the inspector
        if (GUILayout.Button("Export to JSON"))
        {
            NodeGraph graph = storyGraph;
            JsonSerializerSettings serializerSettings = new JsonSerializerSettings
            {
                Formatting = Formatting.Indented,
                Converters = new List<JsonConverter>
                {
                    //Order matters here, first one that CanConvert returns true on will be operated
                    new NodeGroupJsonConverter(graph),
                    new StoryElementJsonConverter(storyGraph)
                }
            };

            //Add the localization assets separately
            JsonConverter[] assetConverter = new[] { new AssetReferenceJsonConverter() };
            string assetTableRefJson = JsonConvert.SerializeObject(storyGraph.AssetTableCollection, assetConverter);
            string stringTableRefJson = JsonConvert.SerializeObject(storyGraph.StringTableCollection, assetConverter);
            string variableManagerRefJson = JsonConvert.SerializeObject(storyGraph.variableManager, assetConverter);
            JObject jObjectRoot = new JObject();
            jObjectRoot.Add("AssetTableCollection", new JRaw(assetTableRefJson));
            jObjectRoot.Add("StringTableCollection", new JRaw(stringTableRefJson));
            jObjectRoot.Add("VariableManager", new JRaw(variableManagerRefJson));

            string nodesJson = JsonConvert.SerializeObject(graph.nodes, serializerSettings);
            jObjectRoot.Add("Nodes", new JRaw(nodesJson));
            string jsonFilepath = string.Empty;
            JObject dfJObjectRoot = null;

            //TODO: If createDfJson is true, also create a serialization for this.
            if (enableSimpleJson)
            {
                dfJObjectRoot = CreateSimpleJson(nodesJson);
            }

            if (useCustomFilePath)
            {
                Directory.CreateDirectory(targetFilePath.Replace(Path.GetFileName(targetFilePath), ""));
                jsonFilepath = targetFilePath;
            }
            else
            {
                jsonFilepath = AssetDatabase.GetAssetPath(storyGraph);
            }

            string extension = Path.GetExtension(jsonFilepath);
            if (extension != JSON_EXTENSION)
            {
                jsonFilepath = jsonFilepath.Replace(extension, JSON_EXTENSION);
            }

            try
            {
                if (enableSimpleJson)
                {
                    //Change extension
                    string dfJsonFilepath = jsonFilepath.Replace(JSON_EXTENSION, SIMPLE_JSON_EXTENSION);

                    //Create a file with similar naming and convention but slightly different. e.g. .df-json
                    File.WriteAllText(dfJsonFilepath, dfJObjectRoot.ToString(Formatting.Indented));
                    Debug.Log("Simple JSON saved to file: " + dfJsonFilepath);
                }

                // Write the string data to the file
                File.WriteAllText(jsonFilepath, jObjectRoot.ToString());
                Debug.Log("JSON saved to file: " + jsonFilepath);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
            catch (Exception e)
            {
                Debug.LogError("Failed to save JSON to file: " + e.Message);
            }
        }
    }

    private void ImportJsonNextFrame()
    {
        // Add a custom button to the inspector
        if (GUILayout.Button("Import from JSON"))
        {
            EditorApplication.delayCall += () =>
            {
                ImportJson();
                storyGraph.LocalizeGraphNodePreview();
            };
        }
    }

    private void ImportJson()
    {
        CheckForDuplicateElementNames();
        NodeGraph graph = storyGraph;

        Undo.IncrementCurrentGroup();
        Undo.SetCurrentGroupName("StoryGraph Json Import");
        int group = Undo.GetCurrentGroup();
        Undo.RegisterCompleteObjectUndo(storyGraph, "Changes to StoryGraph On Json Import");

        var storyElementJsonConverter = new StoryElementJsonConverter(storyGraph);
        JsonSerializerSettings serializerSettings = new JsonSerializerSettings
        {
            PreserveReferencesHandling = PreserveReferencesHandling.Objects,
            Converters = new List<JsonConverter>
            {
                new NodeGroupJsonConverter(graph),
                storyElementJsonConverter
            }
        };

        //Infer the filepath from this asset location, or use an override value
        string jsonFilepath = targetFilePath;
        if (!useCustomFilePath)
        {
            jsonFilepath = AssetDatabase.GetAssetPath(storyGraph);
        }

        string extension = Path.GetExtension(jsonFilepath);
        if (extension != ".json")
        {
            jsonFilepath = jsonFilepath.Replace(extension, JSON_EXTENSION);
        }

        string fileContents = File.ReadAllText(jsonFilepath);

        //Remove all existing stories as 'StoryGraph.Clear' does not remove from the object
        for (int i = storyGraph.nodes.Count - 1; i >= 0; i--)
        {
            Node node = storyGraph.nodes[i];
            if (node != null)
            {
                Undo.DestroyObjectImmediate(node);
            }
        }

        //Soft reset here since we removed all child elements, let other reliant data refill.
        storyGraph.nodes.Clear();

        //Load localization data references
        JObject rootJObject = JObject.Parse(fileContents);
        string assetTablePath = rootJObject["AssetTableCollection"].ToString();
        string stringTablePath = rootJObject["StringTableCollection"].ToString();
        string variableManagerPath = rootJObject["VariableManager"].ToString();
        JArray nodesJArray = rootJObject["Nodes"] as JArray;

        //Remove the NodeGroups from the nodes list, add their objects to the graph object
        List<NodeGroup> nodeGroups = DeserializeAllNodeGroups(nodesJArray, out JArray storyElementJArray);

        //Overwrite the nodes object with data read from the DfJson, if enabled
        if (enableSimpleJson)
        {
            //Change extension
            string dfJsonFilepath = jsonFilepath.Replace(JSON_EXTENSION, SIMPLE_JSON_EXTENSION);
            if (!File.Exists(dfJsonFilepath))
            {
                Debug.LogWarning($"'Enable Simple JSON' was ticked but no .df-json was found matching the filepath '{dfJsonFilepath}'! " +
                    Environment.NewLine + $"Switching to Full JSON import only.");
            }
            else
            {
                try
                {
                    storyElementJArray = GetSimpleJsonNodesOverride(dfJsonFilepath, storyElementJArray);
                }
                catch (Exception e)
                {
                    Debug.LogError($"{nameof(StoryGraphEditor)}.{nameof(ImportJson)}: Exception was caught when trying to parse Simple Json. " +
                        $"Will use full JSON if present to complete import. {Environment.NewLine}Exception Message: {e.Message} " +
                        $"{Environment.NewLine}Stack Trace: {Environment.NewLine}{e.StackTrace}");
                }
            }
        }

        List<StoryElement> deserializedElements = JsonConvert.DeserializeObject<List<StoryElement>>(storyElementJArray.ToString(), serializerSettings);
        storyElementJsonConverter.ResolvePortConnections();

        foreach (var node in graph.nodes)
        {
            //Set default name if not set
            if (node.name == null || node.name.Trim() == "")
            {
                node.name = NodeEditorUtilities.NodeDefaultName(node.GetType());
            }

            AssetDatabase.AddObjectToAsset(node, graph);
            Undo.RegisterCreatedObjectUndo(node, "Node added to graph");
            EditorUtility.SetDirty(node);
        }

        storyGraph.variableManager =
            File.Exists(variableManagerPath) ? AssetDatabase.LoadAssetAtPath<VariableManager>(variableManagerPath) : null;
        storyGraph.AssetTableCollection =
            File.Exists(assetTablePath) ? AssetDatabase.LoadAssetAtPath<AssetTableCollection>(assetTablePath) : null;
        storyGraph.AssetTableName = storyGraph.AssetTableCollection?.TableCollectionName;
        storyGraph.StringTableCollection =
            File.Exists(stringTablePath) ? AssetDatabase.LoadAssetAtPath<StringTableCollection>(stringTablePath) : null;
        storyGraph.StringTableName = storyGraph.StringTableCollection?.TableCollectionName;

        serializedObject.ApplyModifiedProperties();
        storyGraph.RefreshStories();
        storyGraph.ConnectStoryElements();
        Undo.CollapseUndoOperations(group);
        EditorUtility.SetDirty(graph);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    private List<NodeGroup> DeserializeAllNodeGroups(JArray jTokenArray, out JArray jTokenArrayWithoutGroups)
    {
        NodeGraph graph = storyGraph;
        List<NodeGroup> nodeGroups = new List<NodeGroup>();
        jTokenArrayWithoutGroups = new JArray();
        foreach (var nodeJToken in jTokenArray)
        {
            if (nodeJToken["groupName"] != null)
            {
                NodeGroup nodeGroup = JsonConvert.DeserializeObject<NodeGroup>(nodeJToken.ToString(), new NodeGroupJsonConverter(graph));
                if (nodeGroup.name == null || nodeGroup.name.Trim() == "")
                {
                    nodeGroup.name = NodeEditorUtilities.NodeDefaultName(typeof(NodeGroup));
                }
                nodeGroups.Add(nodeGroup);
            }
            else
            {
                jTokenArrayWithoutGroups.Add(nodeJToken);
            }
        }

        return nodeGroups;
    }

    #region Simple Json Conversion
    /// <summary>
    /// Create a Designer-Friendly JSON from the full serialized data.
    /// </summary>
    /// <param name="fullJObject">The full serialized data as JObject</param>
    /// <returns>JObject for DF JSON</returns>
    private JObject CreateSimpleJson([DisallowNull] string nodesJson)
    {
        JObject simpleJson = new JObject();
        JArray simpleStoryElements = new JArray();
        JArray nodesJArray = JArray.Parse(nodesJson);
        JObject simpleElement = null;

        foreach (JToken nodeJToken in nodesJArray)
        {
            JToken elementNameToken = nodeJToken["elementName"];
            if (elementNameToken == null)
            {
                //Skip non-element types e.g. NodeGroups
                continue;
            }

            Type elementType = StoryElement.InferElementTypeFromName(elementNameToken.ToString());
            if (elementType == typeof(ChoiceElement))
            {
                simpleElement = ConvertChoiceElementToSimple(nodeJToken);
            }
            else if (elementType == typeof(BranchElement))
            {
                simpleElement = ConvertBranchElementToSimple(nodeJToken);
            }
            else
            {
                simpleElement = ConvertStoryElementToSimple(elementType, nodeJToken);
            }

            simpleStoryElements.Add(simpleElement);
        }

        simpleJson.Add("Nodes", simpleStoryElements);
        return simpleJson;
    }

    private JArray ReadSimpleJson([DisallowNull] string filePath)
    {
        string fileContents = File.ReadAllText(filePath);
        JObject rootJObject = JObject.Parse(fileContents);
        return rootJObject["Nodes"] as JArray;
    }

    private JArray GetSimpleJsonNodesOverride([DisallowNull] string filePath, [DisallowNull] JArray nodesJArray)
    {
        JArray nodesJArrayCopy = nodesJArray.DeepClone() as JArray;
        JArray simpleNodesJArray = ReadSimpleJson(filePath);
        var allConnectionChanges = new List<NodeConnectionChange>();

        //N.B. Brittle...
        Dictionary<int, JToken> simpleNodeDict = GetNodeDictionaryById(simpleNodesJArray, (idStr) => idStr.Split('_')[1]);
        Dictionary<int, JToken> fullNodeDict = GetNodeDictionaryById(nodesJArrayCopy);

        //Create list of added (SIMPLE) nodes - aka needs to be generated
        var addedSimpleNodes = simpleNodeDict.Where(kvp => !fullNodeDict.ContainsKey(kvp.Key)).ToList();

        //Generate full nodes from simple where we have additions, these will be default initialized to start
        StoryGraph tempStoryGraph = CreateInstance(typeof(StoryGraph)) as StoryGraph;
        foreach (var simpleAdded in addedSimpleNodes) 
        {
            var idSplit = simpleAdded.Value["$id"].ToString().Split('_');
            string elementName = idSplit[0];
            string nodeIdStr = idSplit[1];
            JObject generatedNode = CreateDefaultNodeJTokenFromElementName(elementName, tempStoryGraph, false);
            generatedNode["$id"] = nodeIdStr;

            //TODO: No current implementation in Simple Json to indicate a connection in Previous/Next node ports.
            //TODO: If generated node is connected to something, place it slightly displaced from the connected node
            // Currently every new node spawns at (0,0) which is quite awkward

            fullNodeDict.Add(int.Parse(nodeIdStr), generatedNode);
        }
        DestroyImmediate(tempStoryGraph);

        //Create list of nodes to remove
        var removedNodes = fullNodeDict.Where(kvp => !simpleNodeDict.ContainsKey(kvp.Key)).ToList();
        var noConnectionRemovedNodes = removedNodes.Where(kvp => kvp.Value["ports"].All(port => (port["connections"] as JArray).Count == 0)).ToList();
        var connectedRemovedNodes = removedNodes.Except(noConnectionRemovedNodes).ToList();

        //Get ConnectionChanges from node removal, add them to all node connection changes
        var nodeDeletionConnectionChanges = GetNodeDeletionConnectionChanges(connectedRemovedNodes.Select(kvp => kvp.Value), nodesJArrayCopy);
        allConnectionChanges.AddRange(nodeDeletionConnectionChanges);

        //The below forloop will handle inserting data into generated nodes and replacing data in existing ones
        foreach (KeyValuePair<int, JToken> entry in simpleNodeDict)
        {
            //Get the corresponding full node
            bool hasValue = fullNodeDict.TryGetValue(entry.Key, out JToken node);
            if (!hasValue)
            {
                throw new InvalidDataException($"{nameof(GetNodeDictionaryById)}: Entry in Simple JSON '{entry.Value["$id"]}' " +
                    $"could not find a corresponding $id value '{entry.Key}' in Full JSON!");
            }

            string nodeElementName = node["elementName"].ToString();

            //Overwrite the values where entry has a fieldname
            foreach (JProperty property in entry.Value)
            {
                if (property.Name.Contains("$id"))
                {
                    continue;
                }

                if (node[property.Name] != null)
                {
                    //Ignore if the value is non-null but semantically equal
                    if (node[property.Name].Equals(property.Value))
                    {
                        continue;
                    }

                    Debug.Log($"{nameof(GetSimpleJsonNodesOverride)}: Overridding node {node["$id"]} property '{property.Name}'. Prev: '{node[property.Name]}', New: '{property.Value}'");
                    node[property.Name] = property.Value;
                }
                //Workaround for Simple Json properties that do not have an easily mappable representation e.g. dynamic ports
                else if (IsCustomSimpleProperty(nodeElementName, property.Name))
                {
                    if (nodeElementName.Equals(nameof(BranchElement)))
                    {
                        allConnectionChanges.AddRange(GetBranchPropertyConnectionChanges(property, node));
                    }
                    else if (nodeElementName.Equals(nameof(ChoiceElement)))
                    {
                        allConnectionChanges.AddRange(GetChoicesPropertyConnectionChanges(property, node));
                    }
                    else
                    {
                        Debug.LogWarning(nameof(GetSimpleJsonNodesOverride) + ": Unhandled custom simple property!");
                    }
                }
                else
                {
                    Debug.LogWarning($"{nameof(GetSimpleJsonNodesOverride)}: Property '{property.Name}' was not found in matching node {node["$id"]}! Property override ignored.");
                }

                //TODO: No current implementation in Simple Json to indicate a connection in Previous/Next node ports. Not planned as of yet.
                //TODO: (If above implemented) DISCOVER CHANGES TO PreviousNode AND NextNode! ADD THEM TO THE CONNECTION CHANGES LIST
            }

            fullNodeDict[entry.Key] = node;
        }

        if (allConnectionChanges != null && allConnectionChanges.Count() > 0)
        {
            ApplyConnectionChangesToNodeJTokens(nodesJArrayCopy, allConnectionChanges);
        }

        foreach (var kvp in removedNodes) 
        {
            fullNodeDict.Remove(kvp.Key);
        }

        JArray overrideNodesJArray = new JArray();
        foreach (var kvp in fullNodeDict)
        {
            overrideNodesJArray.Add(kvp.Value);
        }

        //Return modified nodes
        return overrideNodesJArray;
    }

    private JObject ConvertStoryElementToSimple(Type elementType, JToken nodeJToken)
    {
        //Identify the node type, get the JsonProperty values
        JObject simpleElement = new JObject();
        var jsonProperties = GetAttributeFieldsFromType<JsonPropertyAttribute>(elementType);
        var hiddenJsonProperties = GetAttributeFieldsFromType<DfJsonHideAttribute>(elementType);
        jsonProperties = jsonProperties.Except(hiddenJsonProperties).ToList();
        simpleElement.Add("$id", elementType.Name + "_" + nodeJToken["$id"].ToString());
        foreach (var fieldInfo in jsonProperties)
        {
            simpleElement.Add(fieldInfo.Name, nodeJToken[fieldInfo.Name]);
        }
        return simpleElement;
    }

    private JObject ConvertChoiceElementToSimple(JToken nodeJToken)
    {
        JObject simpleElement = new JObject();
        var jsonProperties = GetAttributeFieldsFromType<JsonPropertyAttribute>(typeof(ChoiceElement));
        var hiddenJsonProperties = GetAttributeFieldsFromType<DfJsonHideAttribute>(typeof(ChoiceElement));
        jsonProperties = jsonProperties.Except(hiddenJsonProperties).ToList();
        simpleElement.Add("$id", typeof(ChoiceElement).Name + "_" + nodeJToken["$id"].ToString());

        //Get the ports with fieldname starting 'Choice'
        JArray ports = nodeJToken["ports"] as JArray;
        List<string> choices = new List<string>();
        List<int?> choiceConnectingNodeIds = new List<int?>();
        foreach (var port in ports)
        {
            string fieldName = port["_fieldName"].ToString();
            if (!fieldName.Contains(ChoiceElement.CHOICE_PORT_NAME))
            {
                continue;
            }

            //We have a choice port, get the target node id
            JArray connections = port["connections"] as JArray;
            if (connections.Count > 0)
            {
                choiceConnectingNodeIds.Add(connections.First["node"]["$ref"].ToObject<int>());
            }
        }

        foreach (var fieldInfo in jsonProperties)
        {
            //N.B. Brittle code...
            if (fieldInfo.Name == "ChoiceKeys")
            {
                //foreach key in choicekeys
                foreach (var choice in nodeJToken["ChoiceKeys"])
                {
                    choices.Add(choice.ToString());
                }

                if (choices.Count > choiceConnectingNodeIds.Count)
                {
                    int delta = choices.Count - choiceConnectingNodeIds.Count;
                    for (int i = 0; i < delta; i++)
                    {
                        choiceConnectingNodeIds.Add(null);
                    }
                }

                var choicesToNodeDict = choices.Zip(choiceConnectingNodeIds, (k, v) => new KeyValuePair<string, int?>(k, v))
                    .ToDictionary(x => x.Key, x => x.Value);
                simpleElement.Add("Choices", JObject.Parse(JsonConvert.SerializeObject(choicesToNodeDict)));
            }
            else
            {
                simpleElement.Add(fieldInfo.Name, nodeJToken[fieldInfo.Name]);
            }
        }

        return simpleElement;
    }

    private JObject ConvertBranchElementToSimple(JToken nodeJToken)
    {
        JObject simpleElement = new JObject();
        var jsonProperties = GetAttributeFieldsFromType<JsonPropertyAttribute>(typeof(BranchElement));
        var hiddenJsonProperties = GetAttributeFieldsFromType<DfJsonHideAttribute>(typeof(BranchElement));
        jsonProperties = jsonProperties.Except(hiddenJsonProperties).ToList();
        simpleElement.Add("$id", typeof(BranchElement).Name + "_" + nodeJToken["$id"].ToString());

        //Get the port with fieldname starting 'Choice'
        JArray ports = nodeJToken["ports"] as JArray;

        //Get 'NextNode' and 'ConditionFails' ports
        //N.B. Brittle code..
        Dictionary<string, int?> branchDict = new Dictionary<string, int?>();
        foreach (var port in ports)
        {
            string fieldName = port["_fieldName"].ToString();
            if (fieldName.Contains("NextNode"))
            {
                JArray connections = port["connections"] as JArray;
                if (connections.Count > 0)
                {
                    branchDict.Add("True", connections.First["node"]["$ref"].ToObject<int>());
                }
                else
                {
                    branchDict.Add("True", null);
                }
            }
            else if (fieldName.Contains("ConditionFails"))
            {
                JArray connections = port["connections"] as JArray;
                if (connections.Count > 0)
                {
                    branchDict.Add("False", connections.First["node"]["$ref"].ToObject<int>());
                }
                else
                {
                    branchDict.Add("False", null);
                }
            }
            else
            {
                continue;
            }
        }

        simpleElement.Add("Branch", JObject.Parse(JsonConvert.SerializeObject(branchDict)));
        foreach (var fieldInfo in jsonProperties)
        {
            simpleElement.Add(fieldInfo.Name, nodeJToken[fieldInfo.Name]);
        }
        return simpleElement;
    }

    private IEnumerable<NodeConnectionChange> GetBranchPropertyConnectionChanges(JProperty simpleBranchProperty, JToken fullNodeJToken)
    {
        const string f = nameof(BranchElement.ConditionFails);
        const string t = nameof(BranchElement.NextNode);

        var connectionChanges = new List<NodeConnectionChange>();
        connectionChanges.Add(new NodeConnectionChange
        {
            SrcNodeId = fullNodeJToken["$id"].ToObject<int>(),
            SrcPortFieldName = f,
            NewConnectionTarget = new KeyValuePair<string, int?>(nameof(StoryElement.PreviousNode), simpleBranchProperty.Value["False"]?.ToObject<int?>())
        });

        connectionChanges.Add(new NodeConnectionChange
        {
            SrcNodeId = fullNodeJToken["$id"].ToObject<int>(),
            SrcPortFieldName = t,
            NewConnectionTarget = new KeyValuePair<string, int?>(nameof(StoryElement.PreviousNode), simpleBranchProperty.Value["True"]?.ToObject<int?>())
        });

        var ports = fullNodeJToken["ports"] as JArray;
        foreach (JToken port in ports)
        {
            string fieldName = port["_fieldName"].ToString();
            if (fieldName.Contains(f))
            {
                JArray connections = port["connections"] as JArray;
                if (connections.Count > 0)
                {
                    connectionChanges[0].PrevConnectionTarget = new KeyValuePair<string, int?>(
                        connections.First["fieldName"].ToString(),
                        connections.First["node"]["$ref"].ToObject<int?>()
                    );
                }
            }
            else if (fieldName.Contains(t))
            {
                JArray connections = port["connections"] as JArray;
                if (connections.Count > 0)
                {
                    connectionChanges[1].PrevConnectionTarget = new KeyValuePair<string, int?>(
                        connections.First["fieldName"].ToString(),
                        connections.First["node"]["$ref"].ToObject<int?>()
                    );
                }
            }
        }

        return connectionChanges.Where(x => x.IsChanged == true);
    }

    private IEnumerable<NodeConnectionChange> GetChoicesPropertyConnectionChanges(JProperty simpleChoicesProperty, JToken fullNodeJToken)
    {
        const string dstPortFieldName = nameof(StoryElement.PreviousNode);
        var connectionChanges = new List<NodeConnectionChange>();
        var choicesKvpList = new List<KeyValuePair<string, int?>>();
        var nodeConnectionsDict = new Dictionary<string, int>();
        int portIndex;

        //Get all simple choices keys/node ids
        foreach (JProperty choice in simpleChoicesProperty.Values()) 
        {
            choicesKvpList.Add(new (choice.Name, choice.Value.ToObject<int?>()));
        }

        //Reduce ChoiceKeys to reflect number of simple json choice entries
        fullNodeJToken["ChoiceKeys"] = new JArray(fullNodeJToken["ChoiceKeys"].Take(choicesKvpList.Count));
        JArray choiceKeys = fullNodeJToken["ChoiceKeys"] as JArray;

        //Iterate over the node token values to get a comparison
        var ports = fullNodeJToken["ports"] as JArray;
        bool removePort = false;
        foreach (JToken port in ports)
        {
            string fieldName = port["_fieldName"].ToString();
            if (!fieldName.StartsWith(ChoiceElement.CHOICE_PORT_NAME)) 
            {
                //Skip non-choice ports
                continue;
            }

            portIndex = int.Parse(fieldName.Substring(ChoiceElement.CHOICE_PORT_NAME.Length));
            removePort = portIndex >= choicesKvpList.Count;

            NodeConnectionChange connectionChange = new NodeConnectionChange
            {
                SrcNodeId = fullNodeJToken["$id"].ToObject<int>(),
                SrcPortFieldName = fieldName
            };

            //Set previous connection value
            JArray connections = port["connections"] as JArray;
            if (connections.Count > 0)
            {
                connectionChange.PrevConnectionTarget = new KeyValuePair<string, int?>(
                    connections.First["fieldName"].ToString(),
                    connections.First["node"]["$ref"].ToObject<int?>()
                );
            }

            //Set new connection and update choice key
            if (!removePort)
            {
                choiceKeys[portIndex] = choicesKvpList[portIndex].Key;
                int? newPortValue = choicesKvpList[portIndex].Value;
                if (choicesKvpList[portIndex].Value != null) 
                {
                    connectionChange.NewConnectionTarget = new KeyValuePair<string, int?>(
                        dstPortFieldName,
                        newPortValue.GetValueOrDefault()
                    );
                }
            }

            connectionChanges.Add(connectionChange);
        }

        //Add any new connections if the number of choices is now greater than the number of choice ports in the node jtoken
        for (int i = connectionChanges.Count; i < choicesKvpList.Count; i++) 
        {
            connectionChanges.Add(new NodeConnectionChange
            {
                SrcNodeId = fullNodeJToken["$id"].ToObject<int>(),
                SrcPortFieldName = ChoiceElement.CHOICE_PORT_NAME + i.ToString(),
                NewConnectionTarget = new KeyValuePair<string, int?>(choicesKvpList[i].Key, choicesKvpList[i].Value)
            });
        }

        //Return comparison result where a change has occurred
        return connectionChanges.Where(x => x.IsChanged == true);
    }

    //N.B. Brittle...
    private bool IsCustomSimpleProperty(string elementName, string propertyName)
    {
        //These elements have a unique conversion to simple that needs reverting
        bool isSpecialElement = elementName == nameof(BranchElement) || elementName == nameof(ChoiceElement);
        bool isSpecialProperty = propertyName == "Branch" || propertyName == "Choices";
        return isSpecialElement && isSpecialProperty;
    }

    private IEnumerable<NodeConnectionChange> GetNodeDeletionConnectionChanges([DisallowNull] IEnumerable<JToken> removedNodes, [DisallowNull] IEnumerable<JToken> nodesData) 
    {
        var nodesDataExcludingRemoved = nodesData.Except(removedNodes);
        var connectionChanges = new List<NodeConnectionChange>();

        foreach (var removed in removedNodes) 
        {
            //Iterate over the ports
            foreach (var port in removed["ports"]) 
            {
                //Get the connections
                foreach (var connection in port["connections"]) 
                {
                    //Then find the equivalent connection on the nodes data
                    JToken connectingNode = nodesDataExcludingRemoved
                        .FirstOrDefault(n => n["$id"].ToString()
                        .Equals(connection["node"]["$ref"].ToString()) 
                        && n["ports"].Any(p => p["_fieldName"].ToString()
                        .Equals(connection["fieldName"].ToString())));

                    //If node exists, create connection change
                    if (connectingNode != null) 
                    {
                        connectionChanges.Add(new NodeConnectionChange 
                        {
                            SrcNodeId = removed["$id"].ToObject<int>(),
                            SrcPortFieldName = port["_fieldName"].ToString(),
                            PrevConnectionTarget = new KeyValuePair<string, int?>(connection["fieldName"].ToString(), connection["node"]["$ref"].ToObject<int?>())
                        });
                    }
                }
            }
        }

        return connectionChanges.Where(x => x.IsChanged == true);
    } 
    #endregion

    #region Helper Functions
    private void CheckForDuplicateElementNames()
    {
        var listOfSubtypes = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(domainAssembly => domainAssembly.GetTypes())
                .Where(type => type.IsSubclassOf(typeof(StoryElement))
                ).ToList();

        var nonDistinctItems = listOfSubtypes.GroupBy(key => key.Name).Where(g => g.Count() > 1).ToList();
        var duplicateNamedMembers = nonDistinctItems.Select(g => g.Key).ToArray();
        bool hasDuplicate = duplicateNamedMembers.Count() > 0;

        if (hasDuplicate)
        {
            Debug.LogWarning($"{nameof(CheckForDuplicateElementNames)}: A duplicate named member(s) '{string.Join(',', duplicateNamedMembers)}' " +
                $"were found when trying to infer a StoryElement type from it's json!");
        }
    }

    private List<FieldInfo> GetAttributeFieldsFromType<TPropertyAttribute>(Type type) where TPropertyAttribute : Attribute
    {
        return type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
            .Where(m => Attribute.IsDefined(m, typeof(TPropertyAttribute)))
            .ToList();
    }

    private List<FieldInfo> GetMultipleAttributeFieldsFromType(IEnumerable<Type> attributeTypes, Type type)
    {
        return type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
            .Where(m => attributeTypes.All(a => Attribute.IsDefined(m, a)))
            .ToList();
    }

    private Dictionary<int, JToken> GetNodeDictionaryById([DisallowNull] JArray nodesJArray, Func<string, string> idParser = null)
    {
        Dictionary<int, JToken> nodeDict = new Dictionary<int, JToken>();
        bool canParse = false;
        int id = -1;
        string nodeIdString = string.Empty;
        foreach (JToken node in nodesJArray)
        {
            if (node["$id"] == null)
            {
                continue;
            }

            nodeIdString = node["$id"].ToString();
            canParse = int.TryParse(idParser != null ? idParser(nodeIdString) : nodeIdString, out id);
            if (!canParse)
            {
                throw new InvalidDataException($"{nameof(GetNodeDictionaryById)}: Could not parse id value from simple node '{nodeIdString}'!");
            }

            nodeDict.Add(id, node);
        }

        return nodeDict;
    }

    private JToken GetNodeToken(IEnumerable<JToken> nodes, int nodeId)
        => nodes.First(t => t["$id"].ToObject<int>() == nodeId);

    private void ApplyConnectionChangesToNodeJTokens(IEnumerable<JToken> nodesToModify, IEnumerable<NodeConnectionChange> nodeConnectionChanges)
    {
        JToken srcNode, prevSrcConnection, prevDstNode, prevDstPort, prevDstConnection, newDstNode, newDstPort;
        JObject newSrcConnection, newSrcConnectionNodeRef, newDstConnection, newDstConnectionNodeRef;
        JArray prevDstPortConnections, newDstPortConnections;
        foreach (var change in nodeConnectionChanges) 
        {
            srcNode = GetNodeToken(nodesToModify, change.SrcNodeId);
            prevSrcConnection = prevDstNode = prevDstPort = prevDstConnection = newDstNode = newDstPort = null;
            newSrcConnection = newSrcConnectionNodeRef = newDstConnection = newDstConnectionNodeRef = null;
            prevDstPortConnections = newDstPortConnections = null;

            //Apply change to source node as well as target nodes
            JToken srcPort = srcNode["ports"].First(p => p["_fieldName"].ToString().Equals(change.SrcPortFieldName));
            JArray srcPortConnections = srcPort["connections"] as JArray;

            if (change.PrevConnectionTarget != null) 
            {
                prevDstNode = GetNodeToken(nodesToModify, change.PrevConnectionTarget.Value.Value.GetValueOrDefault());
                prevSrcConnection = srcPortConnections.First(c => c["fieldName"].ToString().Equals(change.PrevConnectionTarget.Value.Key));
                prevDstPort = prevDstNode["ports"].First(p => p["_fieldName"].ToString().Equals(change.PrevConnectionTarget.Value.Key));
                prevDstPortConnections = prevDstPort["connections"] as JArray;
                prevDstConnection = prevDstPortConnections.First(c => c["fieldName"].ToString().Equals(change.SrcPortFieldName));
            }
            if (change.NewConnectionTarget != null)
            {
                int nodeId = change.NewConnectionTarget.Value.Value.GetValueOrDefault();
                newDstNode = GetNodeToken(nodesToModify, change.NewConnectionTarget.Value.Value.GetValueOrDefault());
                newSrcConnectionNodeRef = new JObject();
                newSrcConnectionNodeRef.Add("$ref", nodeId);

                newDstPort = newDstNode["ports"].First(p => p["_fieldName"].ToString().Equals(change.NewConnectionTarget.Value.Key));
                newDstPortConnections = newDstPort["connections"] as JArray;
                newDstConnectionNodeRef = new JObject();
                newDstConnectionNodeRef.Add("$ref", change.SrcNodeId);

                //Create a token representing the new connection on the src node port, reset reroutes
                newSrcConnection = new JObject();
                newSrcConnection.Add("fieldName", change.NewConnectionTarget.Value.Key);
                newSrcConnection.Add("reroutePoints", new JArray());
                newSrcConnection.Add("node", newSrcConnectionNodeRef);

                //Create a token representing the new connection on the dst node port, reset reroutes
                newDstConnection = new JObject();
                newDstConnection.Add("fieldName", change.SrcPortFieldName);
                newDstConnection.Add("reroutePoints", new JArray());
                newDstConnection.Add("node", newDstConnectionNodeRef);
            }

            if (prevDstNode != null)
            {
                prevDstConnection.Remove();

                //Remove the old connection
                if (newDstNode == null)
                {
                    srcPortConnections.Remove(prevSrcConnection);
                }
                //Replace the old connection
                else 
                {
                    prevSrcConnection.Replace(newSrcConnection);
                    newDstPortConnections.Add(newDstConnection);
                }
            }
            else if (newDstNode != null)
            {
                srcPortConnections.Add(newSrcConnection);
                newDstPortConnections.Add(newDstConnection);
            }
        }
    }

    private JObject CreateDefaultNodeJTokenFromElementName([DisallowNull] string elementName, StoryGraph tempStoryGraph = null, bool destroyGraphOnComplete = true)
    {
        tempStoryGraph = CreateInstance(typeof(StoryGraph)) as StoryGraph;
        Type elementType = StoryElement.InferElementTypeFromName(elementName);
        StoryElement tempElement = tempStoryGraph.AddNode(elementType) as StoryElement;
        string tempNodeJson = JsonConvert.SerializeObject(tempElement, new[] { new StoryElementJsonConverter(null) });

        if (destroyGraphOnComplete) 
        {
            DestroyImmediate(tempStoryGraph);
        }
        
        return JObject.Parse(tempNodeJson);
    }

    //Helper class to facilitate the change of connections between one or more nodes
    private class NodeConnectionChange
    {
        public string SrcPortFieldName;
        public int SrcNodeId;
        public KeyValuePair<string, int?>? PrevConnectionTarget;
        public KeyValuePair<string, int?>? NewConnectionTarget;
        
        public bool IsChanged 
            => PrevConnectionTarget != null ? !PrevConnectionTarget.Equals(NewConnectionTarget) : NewConnectionTarget != null;
    }
    #endregion
}