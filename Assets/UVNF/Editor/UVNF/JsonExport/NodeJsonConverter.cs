using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using UnityEngine;

namespace XNode.Json
{
    //Generic override for StoryElements or other XNode derives to use base implementation
    public abstract class NodeJsonConverter<T> : NodeJsonConverter where T : Node
    {
        public abstract T ReadJson(JsonReader reader, Type objectType, T existingValue, bool hasExistingValue, JsonSerializer serializer);
        public abstract void WriteJson(JsonWriter writer, T value, JsonSerializer serializer);

        //Seal the node implementations so it isn't mistakenly called via inference
        public sealed override Node ReadJson(JsonReader reader, Type objectType, Node existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            bool flag = existingValue == null;
            if (!flag && !(existingValue is T))
            {
                throw new JsonSerializationException($"Converter cannot read JSON with the specified existing value. {typeof(T).Name} is required.");
            }

            return ReadJson(reader, objectType, flag ? default : (T)existingValue, !flag, serializer);
        }

        //Seal the node implementations so it isn't mistakenly called via inference
        public sealed override void WriteJson(JsonWriter writer, Node value, JsonSerializer serializer)
        {
            if (value == null || value is not Node)
            {
                throw new JsonSerializationException($"Converter cannot write specified value to JSON. {typeof(T).Name} is required.");
            }

            WriteJson(writer, (T)value, serializer);
        }

        public sealed override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
            => ReadJson(reader, objectType, existingValue as Node, existingValue != null, serializer);

        public sealed override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
            => WriteJson(writer, value as Node, serializer);
    }

    //Override base JsonConverter instead of JsonConverter<T> to have access to the base abstract methods (non-sealed)
    public abstract class NodeJsonConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType) 
            => objectType.IsSubclassOf(typeof(Node));

        public abstract Node ReadJson(JsonReader reader, Type objectType, Node existingValue, bool hasExistingValue, JsonSerializer serializer);
        public abstract void WriteJson(JsonWriter writer, Node value, JsonSerializer serializer);

        //Connections that we've encountered but could not immediately deserialize
        private List<PortConnectionData> connectionCache = new List<PortConnectionData>();
        private JsonSerializer serializer;

        public virtual void ResolvePortConnections()
        {
            Node fromNode = null;
            Node toNode = null;
            NodePort fromPort = null;
            NodePort toPort = null;
            bool isAlreadyConnected = false;
            foreach (var connection in connectionCache)
            {
                fromNode = serializer.ReferenceResolver.ResolveReference(serializer, connection.source.nodeId.ToString()) as Node;
                toNode = serializer.ReferenceResolver.ResolveReference(serializer, connection.destination.nodeId.ToString()) as Node;
                fromPort = fromNode.GetPort(connection.source.fieldName);
                toPort = toNode.GetPort(connection.destination.fieldName);
                isAlreadyConnected = fromPort.IsConnectedTo(toPort);

                if (!isAlreadyConnected) 
                {
                    fromPort.Connect(toPort);
                }

                List<Vector2> reroutePoints = fromPort.GetReroutePoints(fromPort.GetConnectionIndex(toPort));
                reroutePoints.AddRange(connection.reroutePoints);
            }
        }

        protected virtual void ReadAllBaseNodeData(JObject jObject, Node target, JsonSerializer serializer)
        {
            this.serializer ??= serializer;
            string id = (string)jObject["$id"];
            serializer.ReferenceResolver.AddReference(serializer, id, target);
            target.position = new Vector2(jObject["position"]["x"].ToObject<float>(), jObject["position"]["y"].ToObject<float>());
            ReadAllNodePortData(jObject, target, serializer);
        }

        protected virtual void WriteAllBaseNodeData(JsonWriter writer, Node value, JsonSerializer serializer)
        {
            this.serializer ??= serializer;
            writer.WritePropertyName("$id");
            writer.WriteValue(serializer.ReferenceResolver.GetReference(serializer, value));
            writer.WritePropertyName("position");
            writer.WriteStartObject();
            writer.WritePropertyName("x");
            writer.WriteValue(value.position.x);
            writer.WritePropertyName("y");
            writer.WriteValue(value.position.y);
            writer.WriteEndObject();
            WriteAllNodePortData(writer, value, serializer);
        }

        protected virtual void ReadAllNodePortData(JObject jObject, Node target, JsonSerializer serializer)
        {
            this.serializer ??= serializer;
            JArray portsJArray = jObject["ports"] as JArray;
            foreach (var portJToken in portsJArray) 
            {
                string fieldName = portJToken["_fieldName"].ToString();
                NodePort.IO direction = portJToken["_direction"].ToObject<NodePort.IO>();
                Node.TypeConstraint typeConstraint = portJToken["_typeConstraint"].ToObject<Node.TypeConstraint>();
                Node.ConnectionType connectionType = portJToken["_connectionType"].ToObject<Node.ConnectionType>();
                string typeName = portJToken["_typeQualifiedName"].ToString();
                bool isDynamic = portJToken["_dynamic"].ToObject<bool>();
                Type portType = Type.GetType(typeName, true);
                NodePort port = null;

                //Dynamic ports are added in the graph (such as extra parameters)
                //N.B. Static ports are part of the class definition e.g. Exec pins so we just add connection data to match the named port.
                switch (direction)
                {
                    case NodePort.IO.Input:
                        port = isDynamic ? target.AddDynamicInput(portType, connectionType, typeConstraint, fieldName) : target.GetInputPort(fieldName);
                        break;
                    case NodePort.IO.Output:
                        port = isDynamic ? target.AddDynamicOutput(portType, connectionType, typeConstraint, fieldName) : target.GetOutputPort(fieldName);
                        break;
                    default:
                        throw new Exception($"{nameof(NodeJsonConverter)}.{nameof(ReadJson)}: support for {direction} not implemented!");
                }

                JArray connections = portJToken["connections"] as JArray;
                foreach (var connection in connections) 
                {
                    //Add them to the connection data list to be resolved later
                    JToken connectionNodeJToken = connection["node"];
                    string connectedPortName = connection["fieldName"].ToString();
                    JArray reroutePointsJArray = connection["reroutePoints"] as JArray;

                    PortConnectionData portConnectionData = new PortConnectionData();
                    portConnectionData.source.nodeId = int.Parse(serializer.ReferenceResolver.GetReference(serializer, target));
                    portConnectionData.source.fieldName = fieldName;
                    portConnectionData.destination.nodeId = connectionNodeJToken["$ref"].ToObject<int>();
                    portConnectionData.destination.fieldName = connectedPortName;
                    portConnectionData.reroutePoints = GetReroutePointsFromJArray(reroutePointsJArray);
                    connectionCache.Add(portConnectionData);
                }
            }
        }

        protected virtual void WriteAllNodePortData(JsonWriter writer, Node value, JsonSerializer serializer)
        {
            this.serializer ??= serializer;
            writer.WritePropertyName("ports");
            writer.WriteStartArray();
            foreach (var port in value.Ports)
            {
                //Note: for purposes of serialization, parent node reference is ignored (circular)
                writer.WriteStartObject();
                writer.WritePropertyName("_fieldName");
                writer.WriteValue(port.fieldName);
                writer.WritePropertyName("_dynamic");
                writer.WriteValue(port.IsDynamic);
                writer.WritePropertyName("_direction");
                writer.WriteValue(port.direction);
                writer.WritePropertyName("_connectionType");
                writer.WriteValue(port.connectionType);
                writer.WritePropertyName("_typeConstraint");
                writer.WriteValue(port.typeConstraint);
                writer.WritePropertyName("_typeQualifiedName");
                writer.WriteValue(port.ValueType.AssemblyQualifiedName);

                //Connections list (rerouting positions and target port, node belonging to target port)
                writer.WritePropertyName("connections");
                writer.WriteStartArray();
                for (int i = 0; i < port.ConnectionCount; i++)
                {
                    writer.WriteStartObject();
                    var connectedPort = port.GetConnection(i);
                    writer.WritePropertyName("fieldName");
                    writer.WriteValue(connectedPort.fieldName);
                    //Save an identifier to the node rather than the node itself
                    writer.WritePropertyName("node");
                    //serializer.Serialize(writer, connectedPort.node);
                    writer.WriteStartObject();
                    writer.WritePropertyName("$ref");
                    string connectedNodeReference = serializer.ReferenceResolver.GetReference(serializer, connectedPort.node);
                    writer.WriteValue(connectedNodeReference);
                    writer.WriteEndObject();
                    var points = port.GetReroutePoints(i);
                    writer.WritePropertyName("reroutePoints");
                    writer.WriteStartArray();
                    foreach (var point in points)
                    {
                        writer.WriteStartObject();
                        writer.WritePropertyName("x");
                        writer.WriteValue(point.x);
                        writer.WritePropertyName("y");
                        writer.WriteValue(point.y);
                        writer.WriteEndObject();
                    }
                    writer.WriteEndArray();
                    writer.WriteEndObject();
                }
                writer.WriteEndArray();
                writer.WriteEndObject();
            }
            writer.WriteEndArray();
        }

        //Helper function to parse list of reroute points
        private List<Vector2> GetReroutePointsFromJArray([DisallowNull] JArray reroutePointsJArray)
        {
            List<Vector2> parsedPoints = new List<Vector2>();
            foreach (var token in reroutePointsJArray) 
            {
                parsedPoints.Add(new Vector2(token["x"].ToObject<float>(), token["y"].ToObject<float>()));
            }
            return parsedPoints;
        }

        //Caching class for each connection we discover but don't deserialize immediately
        private class PortConnectionData
        {
            public class NodePortInfo
            {
                //Get node reference from ReferenceResolver
                public int nodeId;
                public string fieldName;
            };

            public NodePortInfo source = new NodePortInfo();
            public NodePortInfo destination = new NodePortInfo();
            public List<Vector2> reroutePoints = new List<Vector2>();

            public bool IsSameConnection([DisallowNull] PortConnectionData other)
            {
                return source.nodeId == other.source.nodeId 
                    && source.fieldName == other.source.fieldName 
                    && destination.nodeId == other.destination.nodeId 
                    && destination.fieldName == other.destination.fieldName;
            }

            public bool IsReverseConnection([DisallowNull] PortConnectionData other)
            {
                return source.nodeId == other.destination.nodeId
                    && source.fieldName == other.destination.fieldName
                    && destination.nodeId == other.source.nodeId
                    && destination.fieldName == other.source.fieldName;
            }
        };
    }
}
