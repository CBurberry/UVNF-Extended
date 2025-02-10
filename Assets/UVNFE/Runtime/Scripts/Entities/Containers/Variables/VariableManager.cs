using AYellowpaper.SerializedCollections;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UVNF.Entities.Containers.Variables
{
    [CreateAssetMenu(fileName = "NewVariables", menuName = "UVNF/Variable Manager")]
    public class VariableManager : ScriptableObject
    {
        [SerializeField]
        private SerializedDictionary<string, float> numberVariables = new SerializedDictionary<string, float>();

        [SerializeField]
        private SerializedDictionary<string, bool> booleanVariables = new SerializedDictionary<string, bool>();

        [SerializeField]
        private SerializedDictionary<string, string> stringVariables = new SerializedDictionary<string, string>();

        public int VariableCount => numberVariables.Count + stringVariables.Count + booleanVariables.Count;

        public bool HasDuplicateKeys(out string[] duplicateKeys)
        {
            var aggregateKeys = GetAggregateVariableKeys();
            var nonDistinctItems = aggregateKeys.GroupBy(key => key).Where(g => g.Count() > 1).ToList();
            duplicateKeys = nonDistinctItems.Select(g => g.Key).ToArray();
            return duplicateKeys.Count() > 0;
        }

        public void ChangeVariableName(string oldKey, string newKey)
        {
            object value = GetVariable(oldKey);
            RemoveVariable(oldKey);
            AddVariable(newKey, value);
        }

        public void AddVariable(string key, object value)
        {
            switch (value) 
            {
                case string:
                    AddVariable(key, (string)value);
                    break;
                case bool:
                    AddVariable(key, (bool)value);
                    break;
                case float:
                    AddVariable(key, (float)value);
                    break;
                default:
                    throw new InvalidOperationException("Could not find a type matching '" + value.GetType().Name + "'");
            }
        }

        //Has to box to object to be able to resolve type at runtime
        public object GetVariable(string key)
        {
            switch (GetVariableType(key)) 
            {
                case VariableTypes.String:
                    return stringVariables[key];
                case VariableTypes.Number:
                    return numberVariables[key];
                case VariableTypes.Boolean:
                    return booleanVariables[key];
                default:
                    throw new InvalidOperationException("Could not find a key matching '" + key + "'");
            }
        }

        public VariableTypes GetVariableType(string key)
        {
            if (numberVariables.ContainsKey(key))
            {
                return VariableTypes.Number;
            }
            else if (booleanVariables.ContainsKey(key))
            {
                return VariableTypes.Boolean;
            }
            else if (stringVariables.ContainsKey(key)) 
            {
                return VariableTypes.String;
            }

            throw new InvalidOperationException("Could not find a key matching '" + key + "'");
        }

        public void RemoveVariable(string key)
        {
            //SerializedDictionary remove returns TryRemove result, using short circuit bool eval
            _ = numberVariables.Remove(key) && booleanVariables.Remove(key) && stringVariables.Remove(key);
        }

        public void UpdateVariable(string key, object value)
        {
            switch (value) 
            {
                case string:
                    stringVariables[key] = (string)value;
                    break;
                case bool:
                    booleanVariables[key] = (bool)value;
                    break;
                case float:
                    numberVariables[key] = (float)value;
                    break;
                default:
                    throw new InvalidOperationException("Could not find a type matching '" + value.GetType().Name + "'");
            }
        }

        public string[] GetAggregateVariableKeys()
        {
            return numberVariables.Keys.Concat(booleanVariables.Keys.Concat(stringVariables.Keys)).ToArray();
        }
    }
}