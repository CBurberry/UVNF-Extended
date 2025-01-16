using Newtonsoft.Json;
using System;
using UVNF.Core.Story;
using UVNF.Entities.Containers;
using XNode.Json;
using UnityEngine;
using System.Linq;
using System.Reflection;
using System.Collections;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Converters;

namespace UVNF.Json
{    
    public class StoryElementJsonConverter : NodeJsonConverter<StoryElement>
    {
        protected StoryGraph storyGraph;

        public override bool CanRead => true;
        public override bool CanWrite => true;

        public StoryElementJsonConverter(StoryGraph storyGraph) : base()
        {
            this.storyGraph = storyGraph;
        }

        public override StoryElement ReadJson(JsonReader reader, Type objectType, StoryElement existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            //Read the property values into the element (includes base node data)
            JObject o = JObject.Load(reader);
            string id = (string)o["$id"];
            if (id != null)
            {
                //Get StoryElement type from elementName property in the Json
                // N.B. Brittle (has to be else-if'ed) since the string can be serialized but cannot be read from the class definition as it is a auto-property value.
                string elementName = (string)o["elementName"];
                Type elementType = StoryElement.InferElementTypeFromName(elementName);
                StoryElement storyElement = storyGraph.AddNode(elementType) as StoryElement;

                //Reading all json manually since serializer.populate does not seem to recognize the object for reference counting
                reader = ReadAllJsonPropertyData(o, storyElement, serializer);
                ReadAllBaseNodeData(o, storyElement, serializer);
                return storyElement;
            }
            else
            {
                string reference = (string)o["$ref"];
                object resolvedObject = serializer.ReferenceResolver.ResolveReference(serializer, reference);
                return resolvedObject as StoryElement;
            }
        }

        public override void WriteJson(JsonWriter writer, StoryElement value, JsonSerializer serializer)
        {
            writer.WriteStartObject();
            WriteAllJsonPropertyData(writer, value, serializer);
            WriteAllBaseStoryElementData(writer, value);
            WriteAllBaseNodeData(writer, value, serializer);
            writer.WriteEndObject();
        }

        private JsonReader ReadAllJsonPropertyData(JObject jObject, StoryElement target, JsonSerializer serializer)
        {
            var fieldInfo = target.GetJsonPropertyFields();

            foreach (var member in fieldInfo)
            {
                JsonConverter attributedConverter = null;
                if (Attribute.IsDefined(member, typeof(JsonConverterAttribute)))
                {
                    var attribute = member.GetCustomAttribute<JsonConverterAttribute>();
                    Type converterType = attribute.ConverterType;
                    object[] args = attribute.ConverterParameters;
                    attributedConverter = (JsonConverter)Activator.CreateInstance(converterType, args);
                }

                if (jObject[member.Name] != null)
                {
                    if (attributedConverter != null)
                    {
                        //Call the relevant JsonConverter to handle this member
                        var tokenReader = jObject[member.Name].CreateReader();

                        //WORKAROUND: https://github.com/JamesNK/Newtonsoft.Json/issues/2988
                        if (typeof(StringEnumConverter).IsAssignableFrom(attributedConverter.GetType())) 
                        {
                            //Read once to set this to a JsonReader.TokenType.String
                            tokenReader.ReadAsString();
                        }

                        object convertedObject = attributedConverter.ReadJson(tokenReader, member.FieldType, member.GetValue(target), serializer);
                        member.SetValue(target, convertedObject);
                    }
                    else 
                    {
                        member.SetValue(target, jObject[member.Name].ToObject(member.FieldType));
                    }
                }
            }

            return jObject.CreateReader();
        }

        protected virtual void WriteAllBaseStoryElementData(JsonWriter writer, StoryElement value)
        {
            writer.WritePropertyName("elementName");
            writer.WriteValue(value.ElementName);
            writer.WritePropertyName("_displayColor");
            writer.WriteValue(ColorUtility.ToHtmlStringRGBA(value.DisplayColor));   
        }

        private void WriteAllJsonPropertyData(JsonWriter writer, StoryElement value, JsonSerializer serializer)
        {
            var memberInfo = value.GetJsonPropertyMembers();

            foreach (var member in memberInfo) 
            {
                JsonConverter attributedConverter = null;
                if (Attribute.IsDefined(member, typeof(JsonConverterAttribute))) 
                {
                    var attribute = member.GetCustomAttribute<JsonConverterAttribute>();
                    Type converterType = attribute.ConverterType;
                    object[] args = attribute.ConverterParameters;
                    attributedConverter = (JsonConverter)Activator.CreateInstance(converterType, args);
                }

                writer.WritePropertyName(member.Name);
                bool isFieldOrProperty = member.MemberType == MemberTypes.Field || member.MemberType == MemberTypes.Property;
                object fieldValue = member is FieldInfo ? (member as FieldInfo).GetValue(value) : (member as PropertyInfo).GetValue(value);
                Type fieldType = member is FieldInfo ? (member as FieldInfo).FieldType : (member as PropertyInfo).PropertyType;
                if (attributedConverter == null)
                {
                    if (!isFieldOrProperty)
                    {
                        Debug.LogError($"{nameof(StoryElementJsonConverter)}.{nameof(WriteAllJsonPropertyData)}: " +
                            $"'{value.GetType().Name}.{member.Name}' is not a field or property but was marked with a custom [JsonProperty, JsonConverter] attribute. Unsupported operation!");
                    }

                    //Collections should be treated different but strings are IEnumerables
                    if (typeof(IEnumerable).IsAssignableFrom(fieldType) && fieldType != typeof(string))
                    {
                        //Handle collections
                        IEnumerable collection = fieldValue as IEnumerable;
                        writer.WriteStartArray();
                        foreach (var item in collection)
                        {
                            writer.WriteValue(item);
                        }
                        writer.WriteEndArray();
                    }
                    else
                    {
                        //Use this for pod types or otherwise simple classes
                        writer.WriteValue(fieldValue);
                    }
                }
                else
                {
                    //If the class defined it's own JsonConverter for a member, use it.
                    attributedConverter.WriteJson(writer, fieldValue, serializer);
                }
            }
        }
    }
}