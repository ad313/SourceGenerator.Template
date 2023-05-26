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
        /// 唯一值
        /// </summary>
        public string Key => ClassMetaData?.Key ?? Name;
        /// <summary>
        /// 包含 Attribute
        /// </summary>
        public string FullName { get; set; }
        /// <summary>
        /// Attribute名称
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// 原始字符串
        /// </summary>
        public string Source { get; private set; }
        /// <summary>
        /// Attribute名称
        /// </summary>
        public string ShortName { get; set; }
        /// <summary>
        /// 参数值
        /// </summary>
        public Dictionary<string, string> ParamDictionary { get; set; } = new Dictionary<string, string>();
        /// <summary>
        /// 对应的 ClassMetaData
        /// </summary>
        public ClassMetaData ClassMetaData { get; set; }

        public AttributeMetaData(string name, string source)
        {
            Name = name;
            ShortName = name.IndexOf('.') > -1 ? name.Split('.').Last() : name;
            FullName = ShortName.EndsWith("Attribute") ? ShortName : $"{ShortName}Attribute";
            Source = source;
        }

        public void SetClassMetaData(List<string> newUsing, List<ClassMetaData> classMetaDataList)
        {
            if (classMetaDataList == null)
                return;

            var classList = new List<ClassMetaData>();
            var keys = newUsing.Select(u => $"{u}.{ShortName}").ToList();
            foreach (var key in keys)
            {
                var exists = classMetaDataList.Where(d => d.Key == key || d.Key == $"{key}Attribute").ToList();
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

        public bool Equals(AttributeMetaData other)
        {
            return Key == other?.Key;
        }

        /// <summary>
        /// 获取哈希
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            return this.Key.GetHashCode();
        }

        public bool EqualsByName(string name)
        {
            return IsEquals(name, Name);
        }

        public bool EqualsByFullName(string name)
        {
            return IsEquals(name, Key);
        }

        private bool IsEquals(string first, string second)
        {
            string fullname, shortname;
            var attrLen = "Attribute".Length;
            if (first.EndsWith("Attribute"))
            {
                fullname = first;
                shortname = first.Remove(first.Length - attrLen, attrLen);
            }
            else
            {
                fullname = first + "Attribute";
                shortname = first;
            }

            return second == fullname || second == shortname;
        }
    }
}