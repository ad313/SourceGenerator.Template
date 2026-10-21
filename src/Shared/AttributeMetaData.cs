using System;
using System.Collections.Generic;
using System.Linq;

namespace SourceGenerator.Template.MetaData
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

        /// <summary>
        /// 是否匹配到多个候选类（歧义匹配）
        /// </summary>
        public bool IsAmbiguousMatch { get; private set; }

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
            // BUG-02：多候选时不再抛异常（旧实现会让 FormatData 整体失败、全量不生成），
            // 改为取第一个并标记歧义，由生成器侧统一报告诊断
            IsAmbiguousMatch = classList.Count > 1;
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
        /// QLT-02：补齐 Equals(object)，与 GetHashCode 的按 Key 语义保持一致
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(object obj)
        {
            return Equals(obj as AttributeMetaData);
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