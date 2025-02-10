#if UNITY_EDITOR
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System;
using System.Linq;
using System.Collections.Generic;
using XNodeEditor;
using XNode.NodeGroups;
using UVNF.Json;
using Newtonsoft.Json.Serialization;
using UVNF.Entities.Containers;
using UVNF.Core.Story;

namespace XNode.Json
{
    public class StoryGraphJsonConverter : JsonConverter<StoryGraph>
    {
        protected StoryGraph target;

        public override bool CanRead => true;
        public override bool CanWrite => false;

        //This JsonConverter overwrites the target graph
        public StoryGraphJsonConverter(StoryGraph target) : base()
        {
            this.target = target;
        }

        public override StoryGraph ReadJson(JsonReader reader, Type objectType, StoryGraph existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            serializer.PreserveReferencesHandling = PreserveReferencesHandling.Objects;
            IReferenceResolver refResolver = serializer.ReferenceResolver;

            JArray nodeJArray = JArray.Load(reader);
            List<Node> addedNodes = new List<Node>();

            foreach (var nodeJToken in nodeJArray) 
            {
                JsonReader nodeReader = nodeJToken.CreateReader();
                JsonConverter nodeConverter = serializer.Converters.First(c => c.CanConvert(InferBaseNodeTypeFromNodeJToken(nodeJToken)) && c.CanRead);
                Node node = nodeConverter.ReadJson(nodeReader, typeof(Node), null, serializer) as Node;
                addedNodes.Add(node);
            }

            foreach (var node in target.nodes)
            {
                //Set default name if not set
                if (node.name == null || node.name.Trim() == "")
                {
                    node.name = NodeEditorUtilities.NodeDefaultName(node.GetType());
                }
            }

            return target;
        }

        public override void WriteJson(JsonWriter writer, StoryGraph value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }

        public static Type InferBaseNodeTypeFromNodeJToken(JToken nodeJToken)
        {
            if (nodeJToken["groupName"] != null)
            {
                return typeof(NodeGroup);
            }
            else
            {
                return typeof(StoryElement);
            }
        }
    }
}
#endif