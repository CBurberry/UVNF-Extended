using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UVNF.Core.Story;

namespace UVNF.Json
{
    public static class StoryElementJsonExtensions
    {
        public static List<FieldInfo> GetJsonPropertyFields(this StoryElement element)
        {
            return element.GetType().GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .Where(m => Attribute.IsDefined(m, typeof(JsonPropertyAttribute)))
                .ToList();
        }

        public static List<PropertyInfo> GetJsonProperties(this StoryElement element)
        {
            return element.GetType().GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .Where(m => Attribute.IsDefined(m, typeof(JsonPropertyAttribute)))
                .ToList();
        }

        public static List<MemberInfo> GetJsonPropertyMembers(this StoryElement element)
        {
            return element.GetType().GetMembers(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .Where(m => Attribute.IsDefined(m, typeof(JsonPropertyAttribute)))
                .ToList();
        }
    }
}