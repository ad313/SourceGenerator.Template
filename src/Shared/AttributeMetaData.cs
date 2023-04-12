using System;
using System.Collections.Generic;

namespace SourceGenerator.Analyzers.MetaData
{
    public class AttributeMetaData
    {
        /// <summary>
        /// Attribute名称
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// 参数值
        /// </summary>
        public Dictionary<string, string> ParamDictionary { get; set; } = new Dictionary<string, string>();

        public AttributeMetaData(string name)
        {
            Name = name;
        }

        public void AddParam(string name, string value)
        {
            if (!ParamDictionary.ContainsKey(name))
                ParamDictionary.Add(name, value);
        }

        public string GetStringParam(string key)
        {
            return ParamDictionary.TryGetValue(key, out string str) ? str : null;
        }

        public int? GetIntParam(string key)
        {
            if (ParamDictionary.TryGetValue(key, out string str) && int.TryParse(str, out int v))
                return v;
            return null;
        }

        public bool? GetBoolParam(string key)
        {
            if (ParamDictionary.TryGetValue(key, out string str) && bool.TryParse(str, out bool v))
                return v;
            return null;
        }

        public T GetParam<T>(string key)
        {
            if (ParamDictionary.TryGetValue(key, out string str))
            {
                var d = Convert.ChangeType(str, typeof(T));
                if (d != null)
                    return (T)d;
            }

            return default!;
        }
    }
}