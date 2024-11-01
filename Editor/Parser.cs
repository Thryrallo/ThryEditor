// Material/Shader Inspector for Unity 2017/2018
// Copyright (C) 2019 Thryrallo

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Thry
{
    public class Parser
    {

        public static string Serialize(object o, bool prettyPrint = false)
        {
            return Serialize(o, prettyPrint, 0);
        }

        [System.Obsolete("Use Deserialize<T> instead")]
        public static string ObjectToString(object obj)
        {
            return Serialize(obj, false, 0);
        }

        public static T Deserialize<T>(string s)
        {
            return DeserializeInternal<T>(s);
        }

        public static object Deserialize(string s, Type t)
        {
            return DeserializeInternal(s, t);
        }

        private static string Serialize(object obj, bool prettyPrint, int indent)
        {
            if (obj == null) return "null";
            if (Helper.IsPrimitive(obj.GetType())) return SerializePrimitive(obj);
            if (obj is IList) return SerializeList(obj, prettyPrint, indent);
            if (obj.GetType().IsGenericType && obj.GetType().GetGenericTypeDefinition() == typeof(Dictionary<,>)) return SerializeDictionary(obj, prettyPrint, indent);
            if (obj.GetType().IsArray) return SerializeList(obj, prettyPrint, indent);
            if (obj.GetType().IsEnum) return obj.ToString();
            if (obj.GetType().IsClass) return SerializeClass(obj, prettyPrint, indent);
            if (obj.GetType().IsValueType && !obj.GetType().IsEnum) return SerializeClass(obj, prettyPrint, indent);
            return "";
        }

        private static T DeserializeInternal<T>(string s)
        {
            object parsed = ParseJson(s);
            object ret = null;
            try
            {
                ret = (T)ParsedToObject(parsed, typeof(T));
            }
            catch (Exception e)
            {
                Debug.LogWarning(e.ToString());
                Debug.LogWarning(s + " cannot be parsed to object of type " + typeof(T).ToString());
                ret = Activator.CreateInstance(typeof(T));
            }
            return (T)ret;
        }

        private static object DeserializeInternal(string s, Type t)
        {
            object parsed = ParseJson(s);
            object ret = null;
            try
            {
                ret = ParsedToObject(parsed, t);
            }
            catch (Exception e)
            {
                Debug.LogWarning(e.ToString());
                Debug.LogWarning(s + " cannot be parsed to object of type " + t.ToString());
                ret = Activator.CreateInstance(t);
            }
            return ret;
        }

#region Json to Object Parser
        public static object ParseJson(string input)
        {
            return ParseJsonPart(input, 0, input.Length);
        }

        private static object ParseJsonPart(string input, int start, int end)
        {
            int rawStart = start;
            int rawEnd = end;

            while (start < end && (input[start] == ' ' || input[start] == '\t' || input[start] == '\n' || input[start] == '\r'))
                start++;
            if (start == end)
                return input; // empty string
            if (input[start] == '{')
            {
                start++;
                end--;
                while (end > start && (input[end] == ' ' || input[end] == '\t' || input[end] == '\n' || input[end] == '\r'))
                    end--;
                if (input[end] == '}')
                {
                    return ParseObject(input, start, end);
                }else
                {
                    Debug.LogWarning("Invalid json object: " + input.Substring(rawStart, rawEnd - rawStart));
                    return null;
                }
            }
            if (input[start] == '[')
            {
                start++;
                end--;
                while (end > start && (input[end] == ' ' || input[end] == '\t' || input[end] == '\n' || input[end] == '\r'))
                    end--;
                if (input[end] == ']')
                {
                    return ParseArray(input, start, end);
                }
                else
                {
                    Debug.LogWarning("Invalid json array: " + input);
                    return null;
                }
            }
            return ParsePrimitive(input.Substring(start, end - start));
        }

        private static Dictionary<object, object> ParseObject(string input, int start, int end)
        {
            // Debug.Log("Parse Object: "+ input.Substring(start, end - start));
            int depth = 0;
            int variableStart = start;
            bool isString = false;
            Dictionary<object, object> variables = new Dictionary<object, object>();
            for (int i = start; i < end; i++)
            {
                bool escaped = i != 0 && input[i - 1] == '\\';
                if (input[i] == '\"' && !escaped)
                    isString = !isString;
                if (!isString)
                {
                    if ((depth == 0 && input[i] == ',' && !escaped) || (!escaped && depth == 0 && input[i] == '}'))
                    {
                        int seperatorIndex = input.IndexOf(':', variableStart, i - variableStart);
                        if (seperatorIndex == -1)
                            break;
                        string key = "" + ParseJsonPart(input, variableStart, seperatorIndex);
                        object value = ParseJsonPart(input, seperatorIndex + 1, i);
                        variables.Add(key, value);
                        variableStart = i + 1;
                    }else if(i == end - 1)
                    {
                        int seperatorIndex = input.IndexOf(':', variableStart, i - variableStart);
                        if (seperatorIndex == -1)
                            break;
                        string key = "" + ParseJsonPart(input, variableStart, seperatorIndex);
                        object value = ParseJsonPart(input, seperatorIndex + 1, i + 1);
                        variables.Add(key, value);
                    }
                    else if ((input[i] == '{' || input[i] == '[') && !escaped)
                        depth++;
                    else if ((input[i] == '}' || input[i] == ']') && !escaped)
                        depth--;
                }

            }
            return variables;
        }

        private static List<object> ParseArray(string input, int start, int end)
        {
            // Debug.Log("Parse Array: " + input.Substring(start, end - start));
            int depth = 0;
            int variableStart = start;
            List<object> variables = new List<object>();
            for (int i = start; i < end; i++)
            {
                if(depth == 0 && input[i] == ',' && (i == 0 || input[i - 1] != '\\'))
                {
                    variables.Add(ParseJsonPart(input, variableStart, i));
                    variableStart = i + 1;
                }else if(i == end - 1)
                {
                    variables.Add(ParseJsonPart(input, variableStart, i + 1));
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
            // Debug.Log("Parse Primitive: " + input);
            // string
            if (input.StartsWith("\"", StringComparison.Ordinal))
            {
                input = input.Trim(new char[] { '\r', '\n', ' ','\t' });
                return input.Trim(new char[] { '"' });
            }
                

            // boolean
            // StartsWith ordinal, because it's faster than toLower and trim (in case of spaces after)
            if (input.StartsWith("true", StringComparison.OrdinalIgnoreCase))
                return true;
            if (input.StartsWith("false", StringComparison.OrdinalIgnoreCase))
                return false;
            // null
            if (input == "null" || input == "NULL" || input == "Null")
                return null;

            // number
            float floatValue;
            // parse float invariant
            if(float.TryParse(input, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out floatValue))
            {
                if ((int)floatValue == floatValue)
                    return (int)floatValue;
                return floatValue;
            }

            return input;
        }

#endregion
#region Converters
        public static float ParseFloat(string s, float defaultF = 0)
        {
            float f;
            if(float.TryParse(s, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out f))
            {
                return f;
            }
            return defaultF;
        }

        public static type ConvertParsedToObject<type>(object parsed)
        {
            return (type)ParsedToObject(parsed, typeof(type));
        }

        private static object ParsedToObject(object parsed,Type objtype)
        {
            if (parsed == null) return null;
            if (Helper.IsPrimitive(objtype)) return ConvertToPrimitive(parsed, objtype);
            if (objtype.IsGenericType && objtype.GetInterfaces().Contains(typeof(IList))) return ConvertToList(parsed, objtype);
            if (objtype.IsGenericType && objtype.GetGenericTypeDefinition() == typeof(Dictionary<,>)) return ConvertToDictionary(parsed,objtype);
            if (objtype.IsArray) return ConvertToArray(parsed, objtype);
            if (objtype.IsEnum) return ConvertToEnum(parsed, objtype);
            if (objtype.IsClass) return ConvertToObject(parsed, objtype);
            if (objtype.IsValueType && !objtype.IsEnum) return ConvertToObject(parsed, objtype);
            return null;
        }

        private static object ConvertToDictionary(object parsed, Type objtype)
        {
            var returnObject = (dynamic)Activator.CreateInstance(objtype);
            Dictionary<object, object> dict = (Dictionary<object, object>)parsed;
            foreach (KeyValuePair<object, object> keyvalue in dict)
            {
                dynamic key = ParsedToObject(keyvalue.Key, objtype.GetGenericArguments()[0]);
                dynamic value = ParsedToObject(keyvalue.Value, objtype.GetGenericArguments()[1]);
                returnObject.Add(key , value );
            }
            return returnObject;
        }

        private static Dictionary<Type,FieldInfo[]> fieldCache = new Dictionary<Type, FieldInfo[]>();
        private static Dictionary<Type, PropertyInfo[]> propertyCache = new Dictionary<Type, PropertyInfo[]>();

        private static Dictionary<Type, MethodInfo> thryObjectMethodCache = new Dictionary<Type, MethodInfo>();
        private static bool TryThryParser(object parsed, Type objtype, out object returnObject)
        {
            returnObject = null;
            if(Helper.IsPrimitive(parsed.GetType()) == false) return false;
            MethodInfo method = null;
            if (!thryObjectMethodCache.TryGetValue(objtype, out method))
            {
                method = objtype.GetMethod("ParseForThryParser", BindingFlags.Static | BindingFlags.NonPublic);
                thryObjectMethodCache.Add(objtype, method);
            }
            if (method == null) return false;
            returnObject = method.Invoke(null, new object[] { parsed.ToString() });
            return true;
        }

        private static object ConvertToObject(object parsed, Type objtype)
        {
            object returnObject;
            if (TryThryParser(parsed, objtype, out returnObject))
                return returnObject;
            if (parsed.GetType() != typeof(Dictionary<object, object>)) return null;
            returnObject = Activator.CreateInstance(objtype);
            Dictionary<object, object> dict = (Dictionary<object, object>)parsed;
            FieldInfo[] fields;
            if (!fieldCache.TryGetValue(objtype, out fields))
            {
                fields = objtype.GetFields();
                fieldCache.Add(objtype, fields);
            }
            foreach (FieldInfo field in fields)
            {
                if(dict.TryGetValue(field.Name, out object value))
                {
                    field.SetValue(returnObject, ParsedToObject(value, field.FieldType));
                }
            }
            PropertyInfo[] properties;
            if (!propertyCache.TryGetValue(objtype, out properties))
            {
                properties = objtype.GetProperties().Where(p => p.CanWrite && p.CanRead && p.GetIndexParameters().Length == 0).ToArray();
                propertyCache.Add(objtype, properties);
            }
            foreach (PropertyInfo property in properties)
            {
                if(dict.TryGetValue(property.Name, out object value))
                {
                    property.SetValue(returnObject, ParsedToObject(value, property.PropertyType), null);
                }
            }
            return returnObject;
        }

        private static object ConvertToList(object parsed, Type objtype)
        {
            Type list_obj_type = objtype.GetGenericArguments()[0];
            List<object> list_strings = (List<object>)parsed;
            IList return_list = (IList)Activator.CreateInstance(objtype);
            foreach (object s in list_strings)
                return_list.Add(ParsedToObject(s, list_obj_type));
            return return_list;
        }

        private static Dictionary<Type, MethodInfo> thryArrayMethodCache = new Dictionary<Type, MethodInfo>();
        private static bool TryThryArrayParser(object parsed, Type objtype, out object returnObject)
        {
            returnObject = null;
            if (objtype.BaseType != typeof(System.Array)) return false;
            if (parsed.GetType() != typeof(string)) return false;
            MethodInfo method = null;
            if (!thryArrayMethodCache.TryGetValue(objtype, out method))
            {
                method = objtype.GetMethod("ParseToArrayForThryParser", BindingFlags.Static | BindingFlags.NonPublic);
                thryArrayMethodCache.Add(objtype, method);
            }
            if (method == null) return false;
            returnObject = method.Invoke(null, new object[] { parsed.ToString() });
            return true;
        }

        private static object ConvertToArray(object parsed, Type objtype)
        {
            if(TryThryArrayParser(parsed, objtype, out object returnObject))
                return returnObject;
            if (parsed == null || (parsed is string && (string)parsed == ""))
                return null;
            Type array_obj_type = objtype.GetElementType();
            List<object> list_strings = (List<object>)parsed;
            IList return_list = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(array_obj_type));
            foreach (object s in list_strings)
            {
                object o = ParsedToObject(s, array_obj_type);
                if(o!=null)
                    return_list.Add(o);
            }
            object return_array = Activator.CreateInstance(objtype, return_list.Count);
            return_list.CopyTo(return_array as Array, 0);
            return return_array;
        }

        private static object ConvertToEnum(object parsed, Type objtype)
        {
            if (Enum.IsDefined(objtype, (string)parsed))
                return Enum.Parse(objtype, (string)parsed);
            Debug.LogWarning("The specified enum for " + objtype.Name + " does not exist. Existing Values are: " + Converter.ArrayToString(Enum.GetValues(objtype)));
            return Enum.GetValues(objtype).GetValue(0);
        }

        private static object ConvertToPrimitive(object parsed, Type objtype)
        {
            if (typeof(String) == objtype)
                return parsed!=null?parsed.ToString():null;
            if (typeof(char) == objtype)
                return ((string)parsed)[0];
            return parsed;
        }
#endregion
#region Serializer
        //Serilizer
        private static string PrintIndent(int indent) => new string(' ', indent * 4);
        private static string SerializeDictionary(object obj, bool prettyPrint = false, int indent = 0)
        {
            indent += 1;
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append("{");
            foreach (KeyValuePair<object,object> item in (dynamic)obj)
            {
                if (prettyPrint)
                {
                    stringBuilder.Append("\n");
                    stringBuilder.Append(PrintIndent(indent));
                }
                stringBuilder.Append(Serialize(item.Key, prettyPrint, indent) + ": " + Serialize(item.Value, prettyPrint, indent) + ",");
            }
            if (stringBuilder.Length > 1)
                stringBuilder.Remove(stringBuilder.Length - 1, 1);
            if (prettyPrint)
            {
                stringBuilder.Append("\n");
                stringBuilder.Append(PrintIndent(indent-1));
            }
            stringBuilder.Append("}");
            return stringBuilder.ToString();
        }

        private static string SerializeClass(object obj, bool prettyPrint = false, int indent = 0)
        {
            indent += 1;
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append("{");
            foreach(FieldInfo field in obj.GetType().GetFields())
            {
                if(prettyPrint)
                {
                    stringBuilder.Append("\n");
                    stringBuilder.Append(PrintIndent(indent));
                }
                if(field.IsPublic)
                    stringBuilder.Append("\""+field.Name + "\"" + ": " + Serialize(field.GetValue(obj), prettyPrint, indent) + ",");
            }
            foreach (PropertyInfo property in obj.GetType().GetProperties())
            {
                if (prettyPrint)
                {
                    stringBuilder.Append("\n");
                    stringBuilder.Append(PrintIndent(indent));
                }
                if(property.CanWrite && property.CanRead && property.GetIndexParameters().Length==0)
                    stringBuilder.Append("\""+ property.Name + "\"" + ": " + Serialize(property.GetValue(obj,null), prettyPrint, indent) + ",");
            }
            if (stringBuilder.Length > 1)
                stringBuilder.Remove(stringBuilder.Length - 1, 1);
            if (prettyPrint)
            {
                stringBuilder.Append("\n");
                stringBuilder.Append(PrintIndent(indent-1));
            }
            stringBuilder.Append("}");
            return stringBuilder.ToString();
        }

        private static string SerializeList(object obj, bool prettyPrint = false, int indent = 0)
        {
            indent += 1;
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append("[");
            foreach (object o in obj as IEnumerable)
            {
                if(prettyPrint)
                {
                    stringBuilder.Append("\n");
                    stringBuilder.Append(PrintIndent(indent));
                }
                stringBuilder.Append(Serialize(o, prettyPrint, indent));
                stringBuilder.Append(",");
            }
            if(stringBuilder.Length > 1)
                stringBuilder.Remove(stringBuilder.Length - 1, 1);
            if (prettyPrint)
            {
                stringBuilder.Append("\n");
                stringBuilder.Append(PrintIndent(indent-1));
            }
            stringBuilder.Append("]");
            return stringBuilder.ToString();
        }

        private static string SerializePrimitive(object obj)
        {
            if (obj.GetType() == typeof(string))
                return "\"" + obj + "\"";
            return obj.ToString().Replace(",", "."); ;
        }
#endregion
    }

#region Animation Parser
    public class AnimationParser
    {
        public class Animation
        {
            public PPtrCurve[] pPtrCurves;
        }

        public class PPtrCurve
        {
            public PPtrType curveType;
            public PPtrKeyframe[] keyframes;
        }

        public enum PPtrType
        {
            None,Material
        }

        public class PPtrKeyframe
        {
            public float time;
            public string guid;
            public int type;
        }

        public static Animation Parse(AnimationClip clip)
        {
            return Parse(AssetDatabase.GetAssetPath(clip));
        }

        public static Animation Parse(string path)
        {
            string data = FileHelper.ReadFileIntoString(path);

            List<PPtrCurve> pPtrCurves = new List<PPtrCurve>();
            int pptrIndex;
            int lastIndex = 0;
            while ((pptrIndex = data.IndexOf("m_PPtrCurves", lastIndex)) != -1)
            {
                lastIndex = pptrIndex + 1;
                int pptrEndIndex = data.IndexOf("  m_", pptrIndex);

                int curveIndex;
                int lastCurveIndex = pptrIndex;
                //find all curves
                while((curveIndex = data.IndexOf("  - curve:", lastCurveIndex, pptrEndIndex- lastCurveIndex)) != -1)
                {
                    lastCurveIndex = curveIndex + 1;
                    int curveEndIndex = data.IndexOf("    script: ", curveIndex);

                    PPtrCurve curve = new PPtrCurve();
                    List<PPtrKeyframe> keyframes = new List<PPtrKeyframe>();

                    int keyFrameIndex;
                    int lastKeyFrameIndex = curveIndex;
                    while((keyFrameIndex = data.IndexOf("    - time:", lastKeyFrameIndex, curveEndIndex - lastKeyFrameIndex)) != -1)
                    {
                        lastKeyFrameIndex = keyFrameIndex + 1;
                        int keyFrameEndIndex = data.IndexOf("}", keyFrameIndex);

                        PPtrKeyframe keyframe = new PPtrKeyframe();
                        keyframe.time = float.Parse(data.Substring(keyFrameIndex, data.IndexOf("\n", keyFrameIndex, keyFrameEndIndex)));
                        keyframes.Add(keyframe);
                    }

                    curve.curveType = data.IndexOf("    attribute: m_Materials", lastKeyFrameIndex, curveEndIndex - lastKeyFrameIndex) != -1 ? PPtrType.Material : PPtrType.None;
                    curve.keyframes = keyframes.ToArray();
                    pPtrCurves.Add(curve);
                }
            }
            Animation animation = new Animation();
            animation.pPtrCurves = pPtrCurves.ToArray();
            Debug.Log(Parser.Serialize(animation));
            return animation;
        }
    }
#endregion
}
