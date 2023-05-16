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
            List<string> baseInterfaceList,
            List<string> usingList,
            string accessModifier,
            string extModifier,
            string source)
            : base(name, accessModifier, extModifier, attributeMetaData, source)
        {
            Namespace = @namespace;
            PropertyMeta = propertyMeta;
            MethodMetaData = methodMetaData;
            BaseInterfaceList = baseInterfaceList;
            UsingList = usingList;

            var newUsing = new string[UsingList.Count];
            Array.Copy(UsingList.ToArray(), newUsing, UsingList.Count);
            newUsing = newUsing.Append(Namespace).ToArray();
            NewUsingList = newUsing.ToList();
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
        public List<string> BaseInterfaceList { get; set; }

        /// <summary>
        /// 继承的接口
        /// </summary>
        public List<InterfaceMetaData> BaseInterfaceMetaDataList { get; set; }

        /// <summary>
        /// 引用
        /// </summary>
        public List<string> UsingList { get; set; }

        /// <summary>
        /// 引用 添加自身 namespace
        /// </summary>
        public List<string> NewUsingList { get; set; }

        /// <summary>
        /// 是否是最底层的叶子节点
        /// </summary>
        public bool IsLeaf { get; set; }

        /// <summary>
        /// 合并
        /// </summary>
        /// <param name="other"></param>
        public void MergePartial(InterfaceMetaData other)
        {
            if (other == null)
                return;

            base.Append(other);

            if (PropertyMeta != null && other.PropertyMeta != null)
                PropertyMeta.AddRange(other.PropertyMeta);

            if (MethodMetaData != null && other.MethodMetaData != null)
                MethodMetaData.AddRange(other.MethodMetaData);

            if (BaseInterfaceList != null && other.BaseInterfaceList != null)
            {
                BaseInterfaceList.AddRange(other.BaseInterfaceList);
                BaseInterfaceList = BaseInterfaceList.Distinct().ToList();
            }

            if (BaseInterfaceMetaDataList != null && other.BaseInterfaceMetaDataList != null)
                BaseInterfaceMetaDataList.AddRange(other.BaseInterfaceMetaDataList);

            if (UsingList != null && other.UsingList != null)
            {
                UsingList.AddRange(other.UsingList);
                UsingList = UsingList.Distinct().ToList();
            }
        }

        public virtual bool BaseExists(string key)
        {
            return BaseInterfaceList.Contains(key) || BaseInterfaceList.SelectMany(t => NewUsingList.Select(u => $"{u}.{t.Split('.').Last()}")).Contains(key);
        }

        /// <summary>
        /// 合并所有父级数据
        /// </summary>
        public virtual void MergeAllParents()
        {
            foreach (var parent in BaseInterfaceMetaDataList)
            {
                MergeParentItem(this, parent);
            }
        }

        /// <summary>
        /// 合并所有父级数据
        /// </summary>
        protected void MergeParentItem(InterfaceMetaData source, InterfaceMetaData parent)
        {
            if (parent == null)
                return;

            if (parent.BaseInterfaceMetaDataList != null && parent.BaseInterfaceMetaDataList.Any())
            {
                foreach (var interfaceMetaData in parent.BaseInterfaceMetaDataList)
                {
                    MergeParentItem(parent, interfaceMetaData);
                }
            }

            if (parent.UsingList != null)
            {
                source.UsingList.AddRange(parent.UsingList);
                source.UsingList = source.UsingList.Distinct().ToList();
            }

            if (parent.PropertyMeta != null)
            {
                foreach (var metaData in parent.PropertyMeta)
                {
                    var exists = source.PropertyMeta.FirstOrDefault(d => d.Name == metaData.Name);
                    if (exists == null)
                    {
                        source.PropertyMeta.Add(metaData);
                    }
                    else
                    {
                        if (metaData.AttributeMetaData != null)
                        {
                            foreach (var attributeMetaData in metaData.AttributeMetaData)
                            {
                                if (exists.AttributeMetaData.All(d => d.Name != attributeMetaData.Name))
                                {
                                    exists.AttributeMetaData.Add(attributeMetaData);
                                }
                            }
                        }
                    }
                }
            }

            if (parent.MethodMetaData != null)
            {
                foreach (var metaData in parent.MethodMetaData)
                {
                    var exists = source.MethodMetaData.FirstOrDefault(d => d.Key == metaData.Key);
                    if (exists == null)
                    {
                        source.MethodMetaData.Add(metaData);
                    }
                    else
                    {
                        if (metaData.AttributeMetaData != null)
                        {
                            foreach (var attributeMetaData in metaData.AttributeMetaData)
                            {
                                if (exists.AttributeMetaData.All(d => d.Name != attributeMetaData.Name))
                                {
                                    exists.AttributeMetaData.Add(attributeMetaData);
                                }
                            }
                        }
                    }
                }
            }
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