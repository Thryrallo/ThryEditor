using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace Thry
{
    public class Parsers
    {
        //{text:Test Button,action:{type:url,data:thryrallo.de/megood},hover:hover text,arraytest:[object1,object2],arraytest2:[{type:url,data:thryrallo.de},1]}
        //1
        //string
        public static object Parse(string input)
        {
            if (input.StartsWith("{"))
                 return ParseObject(input);
            else if (input.StartsWith("["))
                return ParseArray(input);
            else
                return ParsePrimitive(input);
        }

        private static Dictionary<string,object> ParseObject(string input)
        {
            input = input.TrimStart(new char[] { '{' });
            int depth = 0;
            int variableStart = 0;
            Dictionary<string, object> variables = new Dictionary<string, object>();
            for(int i = 0; i < input.Length; i++)
            {
                if (input[i] == '{' || input[i] == '[')
                    depth++;
                else if (input[i] == '}' || input[i] == ']')
                    depth--;
                if ((depth==0 && input[i] == ',') || i == input.Length - 1)
                {
                    string[] parts = input.Substring(variableStart, i- variableStart).Split(new char[] { ':' }, 2);
                    string key = parts[0];
                    object value = Parse(parts[1]);
                    variables.Add(key, value);
                    variableStart = i+1;
                }
            }
            return variables;
        }

        private static List<object> ParseArray(string input)
        {
            input = input.TrimStart(new char[] { '[' });
            int depth = 0;
            int variableStart = 0;
            List<object> variables = new List<object>();
            for (int i = 0; i < input.Length; i++)
            {
                if (input[i] == '{' || input[i] == '[')
                    depth++;
                else if (input[i] == '}' || input[i] == ']')
                    depth--;
                if ((depth == 0 && input[i] == ',') || i == input.Length - 1)
                {
                    object value = Parse(input.Substring(variableStart, i - variableStart));
                    variables.Add(value);
                    variableStart = i + 1;
                }
            }
            return variables;
        }

        public static string ToObjectString(object parsed)
        {
            string ret = "";
            if (parsed == null) return ret;
            if (parsed.GetType() == typeof(Dictionary<string, object>))
            {
                ret += "{";
                Dictionary<string, object> dict = ((Dictionary<string, object>)parsed);
                Dictionary<string, object>.Enumerator enumerator = dict.GetEnumerator();
                while (enumerator.MoveNext())
                    ret += enumerator.Current.Key + ":" + ToObjectString(enumerator.Current.Value) + ",";
                ret = ret.TrimEnd(new char[] { ',' }) + "}";
            }
            else if (parsed.GetType() == typeof(List<object>))
            {
                ret += "[";
                foreach (object o in ((List<object>)parsed))
                    ret += ToObjectString(o) + ",";
                ret = ret.TrimEnd(new char[] { ',' }) + "]";
            }
            else
                ret += parsed.ToString();
            return ret;
        }

        private static object ParsePrimitive(string input)
        {
            float floatValue;
            string value = input.TrimStart(new char[] { ' ' });
            if (float.TryParse(value, out floatValue))
            {
                if ((int)floatValue == floatValue)
                    return (int)floatValue;
                return floatValue;
            }
            return value;
        }

        public static type ParseToObject<type>(string input)
        {
            object parsed = Parse(input);
            return (type)ParsedToObject(parsed,typeof(type));
        }

        public static type ConvertParsedToObject<type>(object parsed)
        {
            return (type)ParsedToObject(parsed, typeof(type));
        }

        private static object ParsedToObject(object parsed,Type objtype)
        {
            if (Helper.IsPrimitive(objtype)) return parsed;
            if (parsed.GetType() == typeof(Dictionary<string, object>)) return DictionaryToObject(parsed, objtype);
            if (parsed.GetType() == typeof(List<object>)) return ListToObject(parsed, objtype);
            if (objtype.IsEnum && parsed.GetType() == typeof(string))
            {
                if (Enum.IsDefined(objtype, (string)parsed))
                    return Enum.Parse(objtype, (string)parsed);
                Debug.LogWarning("The specified enum for " + objtype.Name + " does not exist. Existing Values are: " + Helper.ArrayToString(Enum.GetValues(objtype)));
                return Enum.GetValues(objtype).GetValue(0);
            }
            return parsed; 
        }

        private static object DictionaryToObject(object parsed, Type objtype)
        {
            object returnObject = Activator.CreateInstance(objtype);
            Dictionary<string, object> dict = (Dictionary<string, object>)parsed;
            foreach (FieldInfo field in objtype.GetFields())
            {
                if (dict.ContainsKey(field.Name))
                {
                    //Debug.Log(field.Name + "::: " + ParsedToString(Helper.GetValueFromDictionary<string, object>(dict, field.Name)) + ", is prim: " + Helper.IsPrimitive(field.FieldType));
                    field.SetValue(returnObject, ParsedToObject(Helper.GetValueFromDictionary<string, object>(dict, field.Name), field.FieldType));
                }
            }
            return returnObject;
        }

        private static object ListToObject(object parsed, Type objtype)
        {
            //TODO
            return null;
        }

    }
}
