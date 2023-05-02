using System;
using System.Collections.Generic;
using System.Linq;

namespace SourceGenerator.Analyzers.MetaData
{
    /// <summary>
    /// Attribute 元数据
    /// </summary>
    public sealed class AttributeMetaData
    {
        /// <summary>
        /// Attribute名称
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// 参数值
        /// </summary>
        public Dictionary<string, string> ParamDictionary { get; set; } = new Dictionary<string, string>();
        /// <summary>
        /// 对应的 ClassMetaData
        /// </summary>
        public ClassMetaData ClassMetaData { get; private set; }

        public AttributeMetaData(string name)
        {
            Name = name;
        }

        public void SetClassMetaData(string @namespace, List<string> usingList, List<ClassMetaData> classMetaDataList)
        {
            if (classMetaDataList == null)
                return;

            var newUsing = new string[usingList.Count];
            Array.Copy(usingList.ToArray(), newUsing, usingList.Count);
            newUsing = newUsing.Append(@namespace).ToArray();

            var classList = new List<ClassMetaData>();
            var keys = newUsing.Select(u => $"{u}.{Name.Split('.').Last()}").ToList();
            foreach (var key in keys)
            {
                var exists = classMetaDataList.Where(d => d.Exists(key) || d.Exists($"{key}Attribute")).ToList();

                classList.AddRange(exists);
            }

            classList = classList.Distinct().ToList();
            if (classList.Count > 1)
            {
                throw new Exception("特性关联的类，元数据匹配到个数大于 1 ");
            }

            ClassMetaData = classList.FirstOrDefault();
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
            return ParamDictionary.TryGetValue(key, out string str) && int.TryParse(str, out int v) ? v : null;
        }

        public bool? GetBoolParam(string key)
        {
            return ParamDictionary.TryGetValue(key, out string str) && bool.TryParse(str, out bool v) ? v : null;
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