using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Thry
{
    public class Parser
    {

        public static object ParseJson(string input)
        {
            input = Regex.Replace(input, @"^\s+|\s+$","");
            if (input.StartsWith("{"))
                 return ParseObject(input);
            else if (input.StartsWith("["))
                return ParseArray(input);
            else
                return ParsePrimitive(input);
        }

        private static Dictionary<string,object> ParseObject(string input)
        {
            input = Regex.Replace(input, @"^\s+|\s+$", "");
            input = input.TrimStart(new char[] { '{' });
            int depth = 0;
            int variableStart = 0;
            bool isString = false;
            Dictionary<string, object> variables = new Dictionary<string, object>();
            for(int i = 0; i < input.Length; i++)
            {
                bool escaped = i != 0 && input[i - 1] == '\\';
                if (input[i] == '\"' && !escaped)
                    isString = !isString;
                if (!isString)
                {
                    if (i == input.Length - 1 || (depth == 0 && input[i] == ',' && !escaped))
                    {
                        string[] parts = input.Substring(variableStart, i - variableStart).Split(new char[] { ':' }, 2);
                        string key = ""+ParsePrimitive(parts[0]);
                        object value = ParseJson(parts[1]);
                        variables.Add(key, value);
                        variableStart = i + 1;
                    }
                    else if ((input[i] == '{' || input[i] == '[') && !escaped)
                        depth++;
                    else if ((input[i] == '}' || input[i] == ']') && !escaped)
                        depth--;
                }
                
            }
            return variables;
        }

        private static List<object> ParseArray(string input)
        {
            input = input.Trim(new char[] { ' ' });
            input = input.TrimStart(new char[] { '[' });
            int depth = 0;
            int variableStart = 0;
            List<object> variables = new List<object>();
            for (int i = 0; i < input.Length; i++)
            {
                if (i == input.Length-1 || (depth == 0 && input[i] == ',' && (i == 0 || input[i - 1] != '\\')))
                {
                    variables.Add(ParseJson(input.Substring(variableStart, i - variableStart)));
                    variableStart = i + 1;
                }
                else if (input[i] == '{' || input[i] == '[')
                    depth++;
                else if (input[i] == '}' || input[i] == ']')
                    depth--;
            }
            return variables;
        }

        private static object ParsePrimitive(string input)
        {
            input = Regex.Replace(input, @"^\s+|\s+$", "");
            input = input.Replace("\\n", "\n");
            if (input.StartsWith("\""))
                return Regex.Replace(input, "^\\\"|\\\"$", "");
            else if (input.ToLower() == "true")
                return true;
            else if (input.ToLower() == "false")
                return false;
            else
            {
                float floatValue;
                if (float.TryParse(input, out floatValue))
                {
                    if ((int)floatValue == floatValue)
                        return (int)floatValue;
                    return floatValue;
                }
            }
            return input;
        }

        public static type ParseToObject<type>(string input)
        {
            object parsed = ParseJson(input);
            object ret = null;
            try
            {
                ret = (type)ParsedToObject(parsed, typeof(type));
            }
            catch (Exception e)
            {
                Debug.LogError(input + " cannot be parsed to object of type " + typeof(type).ToString());
            }
            return (type)ret;
        }

        public static type ConvertParsedToObject<type>(object parsed)
        {
            return (type)ParsedToObject(parsed, typeof(type));
        }

        private static object ParsedToObject(object parsed,Type objtype)
        {
            if (Helper.IsPrimitive(objtype)) return PrimitiveToObject(parsed,objtype);
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
                    field.SetValue(returnObject, ParsedToObject(Helper.GetValueFromDictionary<string, object>(dict, field.Name), field.FieldType));
                }
            }
            return returnObject;
        }

        private static object ListToObject(object parsed, Type objtype)
        {
            Type list_obj_type = objtype.GetGenericArguments()[0];
            List<object> list_strings = (List<object>)parsed;
            IList return_list = (IList)Activator.CreateInstance(objtype);
            foreach (object s in list_strings)
                return_list.Add(ParsedToObject(s, list_obj_type));
            return return_list;
        }

        private static object PrimitiveToObject(object parsed, Type objtype)
        {
            if (typeof(String) == objtype)
                return parsed.ToString();
            if (typeof(char) == objtype)
                return ((string)parsed)[0];
            return parsed;
        }

        public static string ObjectToString(object obj)
        {
            if (obj == null) return "null";
            if (Helper.IsPrimitive(obj.GetType())) return PrimitiveToString(obj);
            if (obj.GetType() == typeof(List<object>)) return ListToString(obj);
            if (obj.GetType().IsEnum)
            {
                return obj.ToString();
            }
            return ClassObjectToString(obj);
        }

        private static string ClassObjectToString(object obj)
        {
            string ret = "{";
            foreach(FieldInfo field in obj.GetType().GetFields())
            {
                ret += "\""+field.Name + "\"" + ":" + ObjectToString(field.GetValue(obj)) + ",";
            }
            ret = ret.TrimEnd(new char[] { ',' });
            ret += "}";
            return ret;
        }

        private static string ListToString(object obj)
        {
            string ret = "[";
            foreach (object o in (List<object>)obj)
            {
                ret += ObjectToString(o) + ",";
            }
            ret = ret.TrimEnd(new char[] { ',' });
            ret += "]";
            return ret;
        }

        private static string PrimitiveToString(object obj)
        {
            if (obj.GetType() == typeof(string))
                return "\"" + obj + "\"";
            return obj.ToString();
        }
    }
}
