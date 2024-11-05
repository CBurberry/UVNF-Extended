using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using UnityEngine;
using XNode.NodeGroups;

namespace XNode.Json
{
    public class NodeGroupJsonConverter : JsonConverter<NodeGroup>
    {
        protected NodeGraph nodeGraph;

        public override bool CanRead => true;
        public override bool CanWrite => true;

        public NodeGroupJsonConverter(NodeGraph nodeGraph)
        {
            this.nodeGraph = nodeGraph;
        }

        public override NodeGroup ReadJson(JsonReader reader, Type objectType, NodeGroup existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            NodeGroup target = nodeGraph.AddNode<NodeGroup>();
            JObject nodeJObject = JObject.Load(reader);
            target.groupName = nodeJObject["groupName"].ToString();
            target.position = new Vector2(nodeJObject["position"]["x"].ToObject<float>(), nodeJObject["position"]["y"].ToObject<float>());
            target.width = nodeJObject["width"].ToObject<int>();
            target.height = nodeJObject["height"].ToObject<int>();
            ColorUtility.TryParseHtmlString("#" + nodeJObject["color"].ToString(), out target.color);
            return target;
        }

        public override void WriteJson(JsonWriter writer, NodeGroup value, JsonSerializer serializer)
        {
            writer.WriteStartObject();
            writer.WritePropertyName("position");
            writer.WriteStartObject();
            writer.WritePropertyName("x");
            writer.WriteValue(value.position.x);
            writer.WritePropertyName("y");
            writer.WriteValue(value.position.y);
            writer.WriteEndObject();
            writer.WritePropertyName("groupName");
            writer.WriteValue(value.groupName);
            writer.WritePropertyName("width");
            writer.WriteValue(value.width);
            writer.WritePropertyName("height");
            writer.WriteValue(value.height);
            writer.WritePropertyName("color");
            writer.WriteValue(ColorUtility.ToHtmlStringRGBA(value.color));
            writer.WriteEndObject();
        }
    }
}