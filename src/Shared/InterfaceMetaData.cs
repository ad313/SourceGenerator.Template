using System;
using System.Collections.Generic;
using System.Linq;

namespace SourceGenerator.Analyzers.MetaData
{
    /// <summary>
    /// 接口元数据
    /// </summary>
    public class InterfaceMetaData : MetaDataBase, IEquatable<InterfaceMetaData>
    {
        public string Key => $"{Namespace}.{Name}";

        /// <summary>Initializes a new instance of the <see cref="T:System.Object"></see> class.</summary>
        public InterfaceMetaData(string @namespace,
            string name, 
            List<AttributeMetaData> attributeMetaData,
            List<PropertyMetaData> propertyMeta,
            List<MethodMetaData> methodMetaData,
            List<string> baseInterfaces,
            List<string> usings,
            string accessModifier,
            string extModifier) 
            : base(name, accessModifier, extModifier, attributeMetaData)
        {
            Namespace = @namespace;
            PropertyMeta = propertyMeta;
            MethodMetaData = methodMetaData;
            BaseInterfaces = baseInterfaces;
            Usings = usings;
        }

        /// <summary>
        /// 命名空间
        /// </summary>
        public string Namespace { get; set; }

        /// <summary>
        /// 属性集合
        /// </summary>
        public List<PropertyMetaData> PropertyMeta { get; set; }

        /// <summary>
        /// 方法集合
        /// </summary>
        public List<MethodMetaData> MethodMetaData { get; set; }

        /// <summary>
        /// 继承的接口
        /// </summary>
        public List<string> BaseInterfaces { get; set; }

        /// <summary>
        /// 继承的接口
        /// </summary>
        public List<InterfaceMetaData> BaseInterfaceMetaDataList { get; set; }

        /// <summary>
        /// 引用
        /// </summary>
        public List<string> Usings { get; set; }

        /// <summary>
        /// 合并
        /// </summary>
        /// <param name="other"></param>
        public void Append(InterfaceMetaData other)
        {
            if (other == null)
                return;

            base.Append(other);

            if (PropertyMeta != null && other.PropertyMeta != null)
                PropertyMeta.AddRange(other.PropertyMeta);

            if (MethodMetaData != null && other.MethodMetaData != null)
                MethodMetaData.AddRange(other.MethodMetaData);

            if (BaseInterfaces != null && other.BaseInterfaces != null)
            {
                BaseInterfaces.AddRange(other.BaseInterfaces);
                BaseInterfaces = BaseInterfaces.Distinct().ToList();
            }

            if (BaseInterfaceMetaDataList != null && other.BaseInterfaceMetaDataList != null)
                BaseInterfaceMetaDataList.AddRange(other.BaseInterfaceMetaDataList);

            if (Usings != null && other.Usings != null)
            {
                Usings.AddRange(other.Usings);
                Usings = Usings.Distinct().ToList();
            }
        }

        public bool HasInterface(string key)
        {
            var newUsing = new string[Usings.Count];
            Array.Copy(Usings.ToArray(), newUsing, Usings.Count);
            newUsing = newUsing.Append(Namespace).ToArray();

            return BaseInterfaces.Contains(key) || BaseInterfaces.SelectMany(t => newUsing.Select(u => $"{u.Replace("using ", "").Replace(";", "")}.{t.Split('.').Last()}")).Contains(key);
        }

        public bool Equals(InterfaceMetaData other)
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
    }
}