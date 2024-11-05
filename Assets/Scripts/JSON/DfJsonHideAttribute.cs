using System;

//
// Summary:
//     Add this property to a StoryElement field that also has JsonPropertyAttribute to hide it from DfJson output
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false)]
public class DfJsonHideAttribute : Attribute
{
}
