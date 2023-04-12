using System;
using System.Collections.Generic;
using System.Linq;

namespace SourceGenerator.Analyzers.MetaData
{
    /// <summary>
    /// 类元数据
    /// </summary>
    public class ClassMetaData : InterfaceMetaData
    {
        /// <summary>Initializes a new instance of the <see cref="T:System.Object"></see> class.</summary>
        public ClassMetaData(
            string @namespace,
            string name,
            List<AttributeMetaData> attributeMetaData,
            List<PropertyMetaData> propertyMeta,
            List<MethodMetaData> methodMetaData,
            List<string> baseInterfaces,
            List<string> baseClasses,
            List<KeyValueModel> constructor,
            List<string> usings,
            string accessModifier,
            string extModifier = null)
            : base(@namespace, name, attributeMetaData, propertyMeta, methodMetaData, baseInterfaces, usings, accessModifier, extModifier)
        {
            Constructor = constructor;
            BaseInterfaces = baseInterfaces;
            BaseClasses = baseClasses;
        }

        /// <summary>
        /// 继承的类
        /// </summary>
        public List<string> BaseClasses { get; set; }

        /// <summary>
        /// 继承的类
        /// </summary>
        public List<ClassMetaData> BaseClassMetaDataList { get; set; }

        /// <summary>
        /// 构造函数参数
        /// </summary>
        public List<KeyValueModel> Constructor { get; set; }

        public bool HasClass(string key)
        {
            var newUsing = new string[Usings.Count];
            Array.Copy(Usings.ToArray(), newUsing, Usings.Count);
            newUsing = newUsing.Append(Namespace).ToArray();

            return BaseClasses.Contains(key) || BaseClasses.SelectMany(t => newUsing.Select(u => $"{u.Replace("using ", "").Replace(";", "")}.{t.Split('.').Last()}")).Contains(key);
        }

        /// <summary>
        /// 合并
        /// </summary>
        /// <param name="other"></param>
        public void Append(ClassMetaData other)
        {
            if (other == null)
                return;

            base.Append(other);

            if (BaseClasses != null && other.BaseClasses != null)
            {
                BaseClasses.AddRange(other.BaseClasses);
                BaseClasses = BaseClasses.Distinct().ToList();
            }

            if (BaseClassMetaDataList != null && other.BaseClassMetaDataList != null)
                BaseClassMetaDataList.AddRange(other.BaseClassMetaDataList);
        }
    }
}