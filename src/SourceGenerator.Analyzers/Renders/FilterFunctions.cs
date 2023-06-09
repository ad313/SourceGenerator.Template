using Newtonsoft.Json;
using Scriban;
using Scriban.Runtime;
using SourceGenerator.Analyzers.Extend;
using SourceGenerator.Analyzers.MetaData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace SourceGenerator.Analyzers.Renders
{
    /// <summary>
    /// Scriban 自定义函数
    /// </summary>
    public class FilterFunctions : ScriptObject
    {
        /// <summary>
        /// 通过空格分割字符串，返回特定 index 的数据
        /// split_string_by_whitespace
        /// </summary>
        /// <param name="text"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        public static string SplitStringByWhitespace(string text, int index)
        {
            if (string.IsNullOrWhiteSpace(text))
                return null;

            var arr = Regex.Split(text, "\\s+");
            return arr.Length >= index ? arr[index] : null;
        }

        /// <summary>
        /// 判断 是否有指定的特性
        /// has_attribute
        /// </summary>
        /// <param name="obj">属性、方法、接口、类、特性集合</param>
        /// <param name="attributeName"></param>
        /// <returns></returns>
        public static bool HasAttribute(object obj, string attributeName)
        {
            if (obj is MetaDataBase data)
            {
                return data.AttributeMetaDataList.HasAttribute(attributeName);
            }

            if (obj is List<AttributeMetaData> attrs)
            {
                return attrs.HasAttribute(attributeName);
            }

            return false;
        }

        /// <summary>
        /// 判断 是否有指定的特性
        /// has_attribute
        /// </summary>
        /// <param name="obj">属性、方法、接口、类、特性集合</param>
        /// <param name="attributeName"></param>
        /// <returns></returns>
        public static bool HasAttributeByFullName(object obj, string attributeName)
        {
            if (obj is MetaDataBase data)
            {
                return data.AttributeMetaDataList.HasAttributeByFullName(attributeName);
            }

            if (obj is List<AttributeMetaData> attrs)
            {
                return attrs.HasAttributeByFullName(attributeName);
            }

            return false;
        }

        /// <summary>
        /// 判断特性父级
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="parent"></param>
        /// <returns></returns>
        public static bool AttributeHasParent(object obj, string parent)
        {
            if (obj is MetaDataBase data)
            {
                return data.AttributeMetaDataList.Any(d => d.ClassMetaData != null && d.ClassMetaData.BaseExists(parent));
            }

            if (obj is List<AttributeMetaData> attrs)
            {
                return attrs.Any(d => d.ClassMetaData != null && d.ClassMetaData.BaseExists(parent));
            }

            if (obj is AttributeMetaData attr)
            {
                return attr.ClassMetaData != null && attr.ClassMetaData.BaseExists(parent);
            }

            return false;
        }

        /// <summary>
        /// 合并父级，把所有父级的东西合并到子类，针对 class 和 interface
        /// merge
        /// </summary>
        /// <param name="obj">接口、类</param>
        /// <returns></returns>
        public static object Merge(object obj)
        {
            if (obj is ClassMetaData classMetaData)
            {
                classMetaData.MergeAllParents();
            }

            if (obj is InterfaceMetaData interfaceMetaData)
            {
                interfaceMetaData.MergeAllParents();
            }

            return obj;
        }

        /// <summary>
        /// 根据 特性名称 和 特性参数Key，获取对应的值
        /// get_attribute_param_value_from_attribute_list
        /// </summary>
        /// <param name="attributeMetaDataList">特性集合</param>
        /// <param name="attributeName"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        public static string GetAttributeParamValueFromAttributeList(List<AttributeMetaData> attributeMetaDataList, string attributeName, string key)
        {
            var attr = attributeMetaDataList.FirstOrDefault(d => d.EqualsByName(attributeName));
            if (attr == null)
                return null;

            return GetAttributeParamValueFromAttribute(attr, key);
        }

        /// <summary>
        /// 根据 特性名称 和 特性参数Key，获取对应的值
        /// get_attribute_param_value_from_attribute
        /// </summary>
        /// <param name="attributeMetaData">特性</param>
        /// <param name="key"></param>
        /// <returns></returns>
        public static string GetAttributeParamValueFromAttribute(AttributeMetaData attributeMetaData, string key)
        {
            return attributeMetaData.ParamDictionary.TryGetValue(key, out string v) ? v : null;
        }

        #region Filter

        /// <summary>
        /// 根据特性名称 过滤（属性、方法、类、接口、结构体、枚举、特性）列表
        /// list_filter_by_attribute
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="attributeName"></param>
        /// <returns></returns>
        public static object ListFilterByAttribute(object obj, string attributeName)
        {
            if (obj is List<PropertyMetaData> propertyMeta)
            {
                return ListFilterByAttributeInternal(propertyMeta, attributeName);
            }

            if (obj is List<MethodMetaData> methodMeta)
            {
                return ListFilterByAttributeInternal(methodMeta, attributeName);
            }

            if (obj is List<ClassMetaData> classMeta)
            {
                return ListFilterByAttributeInternal(classMeta, attributeName);
            }

            if (obj is List<InterfaceMetaData> interfaceMeta)
            {
                return ListFilterByAttributeInternal(interfaceMeta, attributeName);
            }

            if (obj is List<StructMetaData> structMeta)
            {
                return ListFilterByAttributeInternal(structMeta, attributeName);
            }

            if (obj is List<EnumMetaData> enumMeta)
            {
                return ListFilterByAttributeInternal(enumMeta, attributeName);
            }

            if (obj is List<AttributeMetaData> attributeMeta)
            {
                return attributeMeta.Where(t => t.EqualsByName(attributeName)).ToList();
            }

            return obj;
        }

        /// <summary>
        /// 根据名称 过滤（属性、方法、类、接口、结构体、枚举、特性）列表
        /// list_filter_by_name
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public static object ListFilterByName(object obj, string name)
        {
            if (obj is List<PropertyMetaData> propertyMeta)
            {
                return propertyMeta.Where(d => d.Name == name).ToList();
            }

            if (obj is List<MethodMetaData> methodMeta)
            {
                return methodMeta.Where(d => d.Key == name).ToList();
            }

            if (obj is List<ClassMetaData> classMeta)
            {
                return classMeta.Where(d => d.Name == name).ToList();
            }

            if (obj is List<InterfaceMetaData> interfaceMeta)
            {
                return interfaceMeta.Where(d => d.Name == name).ToList();
            }

            if (obj is List<StructMetaData> structMeta)
            {
                return structMeta.Where(d => d.Name == name).ToList();
            }

            if (obj is List<EnumMetaData> enumMeta)
            {
                return enumMeta.Where(d => d.Name == name).ToList();
            }

            if (obj is List<AttributeMetaData> attributeMeta)
            {
                return attributeMeta.Where(d => d.Name == name).ToList();
            }

            return obj;
        }

        /// <summary>
        /// 根据特性名称 过滤（属性、方法、类、接口、结构体、枚举、特性）列表
        /// list_filter_by_attribute
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="attributeNameObject"></param>
        /// <returns></returns>
        public static object ListFilterByAttributes(object obj, object attributeNameObject)
        {
            if (attributeNameObject is ScriptArray attributeNames)
            {
                if (obj is List<PropertyMetaData> propertyMeta)
                {
                    return ListFilterByAttributeInternal(propertyMeta, attributeNames);
                }

                if (obj is List<MethodMetaData> methodMeta)
                {
                    return ListFilterByAttributeInternal(methodMeta, attributeNames);
                }

                if (obj is List<ClassMetaData> classMeta)
                {
                    return ListFilterByAttributeInternal(classMeta, attributeNames);
                }

                if (obj is List<InterfaceMetaData> interfaceMeta)
                {
                    return ListFilterByAttributeInternal(interfaceMeta, attributeNames);
                }

                if (obj is List<StructMetaData> structMeta)
                {
                    return ListFilterByAttributeInternal(structMeta, attributeNames);
                }

                if (obj is List<EnumMetaData> enumMeta)
                {
                    return ListFilterByAttributeInternal(enumMeta, attributeNames);
                }

                if (obj is List<AttributeMetaData> attributeMeta)
                {
                    return attributeNames.SelectMany(attributeName =>
                        attributeMeta.Where(t => t.EqualsByName(attributeName.ToString()))).ToList();
                }
            }
            
            return obj;
        }

        private static List<T> ListFilterByAttributeInternal<T>(List<T> data, string attributeName) where T : MetaDataBase
        {
            return data.Where(d => d.AttributeMetaDataList.HasAttribute(attributeName)).ToList();
        }

        private static List<T> ListFilterByAttributeInternal<T>(List<T> data, ScriptArray attributeNames) where T : MetaDataBase
        {
            return attributeNames.SelectMany(attributeName =>
                data.Where(d => d.AttributeMetaDataList.HasAttribute(attributeName.ToString()))).ToList();
        }

        /// <summary>
        /// 根据特性名称、特性参数Key 过滤（属性、方法、类、接口、结构体、枚举、特性）列表
        /// list_filter_by_attribute_key
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="attributeName"></param>
        /// <param name="key">特性参数Key</param>
        /// <returns></returns>
        public static object ListFilterByAttributeKey(object obj, string attributeName, string key)
        {
            if (obj is List<PropertyMetaData> propertyMeta)
            {
                return ListFilterByAttributeKeyInternal(propertyMeta, attributeName, key);
            }

            if (obj is List<MethodMetaData> methodMeta)
            {
                return ListFilterByAttributeKeyInternal(methodMeta, attributeName, key);
            }

            if (obj is List<ClassMetaData> classMeta)
            {
                return ListFilterByAttributeKeyInternal(classMeta, attributeName, key);
            }

            if (obj is List<InterfaceMetaData> interfaceMeta)
            {
                return ListFilterByAttributeKeyInternal(interfaceMeta, attributeName, key);
            }

            if (obj is List<StructMetaData> structMeta)
            {
                return ListFilterByAttributeKeyInternal(structMeta, attributeName, key);
            }

            if (obj is List<EnumMetaData> enumMeta)
            {
                return ListFilterByAttributeKeyInternal(enumMeta, attributeName, key);
            }

            if (obj is List<AttributeMetaData> attributeMeta)
            {
                return attributeMeta.Where(t => t.EqualsByName(attributeName) && t.ParamDictionary.Any(dic => dic.Key == key)).ToList();
            }

            return obj;
        }

        private static List<T> ListFilterByAttributeKeyInternal<T>(List<T> data, string attributeName, string key) where T : MetaDataBase
        {
            return data.Where(d => d.AttributeMetaDataList.Any(t => t.EqualsByName(attributeName) && t.ParamDictionary.Any(dic => dic.Key == key))).ToList();
        }

        /// <summary>
        /// 根据特性名称、特性参数Key、特性参数值 过滤（属性、方法、类、接口、结构体、枚举、特性）列表
        /// list_filter_by_attribute_key_value
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="attributeName"></param>
        /// <param name="key">特性参数Key</param>
        /// <param name="value">特性参数值</param>
        /// <returns></returns>
        public static object ListFilterByAttributeKeyValue(object obj, string attributeName, string key, string value)
        {
            if (obj is List<PropertyMetaData> propertyMeta)
            {
                return ListFilterByAttributeKeyValueInternal(propertyMeta, attributeName, key, value);
            }

            if (obj is List<MethodMetaData> methodMeta)
            {
                return ListFilterByAttributeKeyValueInternal(methodMeta, attributeName, key, value);
            }

            if (obj is List<ClassMetaData> classMeta)
            {
                return ListFilterByAttributeKeyValueInternal(classMeta, attributeName, key, value);
            }

            if (obj is List<InterfaceMetaData> interfaceMeta)
            {
                return ListFilterByAttributeKeyValueInternal(interfaceMeta, attributeName, key, value);
            }

            if (obj is List<StructMetaData> structMeta)
            {
                return ListFilterByAttributeKeyValueInternal(structMeta, attributeName, key, value);
            }

            if (obj is List<EnumMetaData> enumMeta)
            {
                return ListFilterByAttributeKeyValueInternal(enumMeta, attributeName, key, value);
            }

            if (obj is List<AttributeMetaData> attributeMeta)
            {
                return attributeMeta.Where(t => t.EqualsByName(attributeName) && t.ParamDictionary.Any(dic => dic.Key == key && dic.Value == value)).ToList();
            }

            return obj;
        }

        private static List<T> ListFilterByAttributeKeyValueInternal<T>(List<T> data, string attributeName, string key, string value) where T : MetaDataBase
        {
            return data.Where(d => d.AttributeMetaDataList.Any(t => t.EqualsByName(attributeName) && t.ParamDictionary.Any(dic => dic.Key == key && dic.Value == value))).ToList();
        }
        

        #endregion

        /// <summary>
        /// 获取传入的字符串中 第一个不为空的数据
        /// get_first_not_null_value
        /// </summary>
        /// <param name="first"></param>
        /// <param name="second"></param>
        /// <returns></returns>
        public static string GetFirstNotNullValue(string first, string second)
        {
            return !string.IsNullOrWhiteSpace(first) ? first.Trim('"') : second?.Trim('"');
        }

        /// <summary>
        /// 获取当前时间
        /// now
        /// </summary>
        /// <returns></returns>
        public static string Now()
        {
            return DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        }

        /// <summary>
        /// 让字符串首字母小写
        /// low_first_code
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static string LowFirstCode(string str)
        {
            if (string.IsNullOrWhiteSpace(str))
                return str;

            return str.Substring(0, 1).ToLower() + str.Substring(1);
        }

        /// <summary>
        /// 渲染模板
        /// render
        /// </summary>
        /// <param name="data">传入数据</param>
        /// <param name="templateModel">模板定义数据，主要获取模板内容</param>
        /// <param name="templateName">模板名称</param>
        /// <param name="fileName">保存的文件名，为空则自动生成</param>
        /// <exception cref="ArgumentNullException"></exception>
        public static void Render(dynamic data, MapModel templateModel, string templateName, string fileName)
        {
            var templateString = templateModel?.GetTemplate(templateName);
            if (string.IsNullOrWhiteSpace(templateString))
                throw new ArgumentNullException($"未找到 {templateName} 对应的模板");

            var scriptObject1 = new FilterFunctions();
            scriptObject1.Import((object)data);
            var scContext = new TemplateContext();
            scContext.PushGlobal(scriptObject1);

            var template = Template.Parse(templateString);
            var result = template.Render(scContext);

            if (string.IsNullOrWhiteSpace(fileName))
                fileName = $"{templateName.Replace(".txt", "")}_{data.Name}_g";

            IncrementalGenerator.Context.AddSource(fileName, result);
        }

        /// <summary>
        /// 转换成json，并存为对应的文件名
        /// json
        /// </summary>
        /// <param name="data"></param>
        /// <param name="name"></param>
        public static void Json(dynamic data, string name)
        {
            IncrementalGenerator.Context.AddSource(string.IsNullOrWhiteSpace(name) ? Guid.NewGuid().ToString() : name, JsonConvert.SerializeObject(data));
        }
    }
}
