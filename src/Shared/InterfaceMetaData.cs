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
            List<AttributeMetaData> attributeMetaDataList,
            List<PropertyMetaData> propertyMetaDataList,
            List<MethodMetaData> methodMetaDataList,
            List<string> baseInterfaceList,
            List<string> usingList,
            string accessModifier,
            string extModifier,
            string source)
            : base(name, accessModifier, extModifier, attributeMetaDataList, source)
        {
            Namespace = @namespace;
            PropertyMetaDataList = propertyMetaDataList;
            MethodMetaDataList = methodMetaDataList;
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
        public List<PropertyMetaData> PropertyMetaDataList { get; set; }

        /// <summary>
        /// 方法集合
        /// </summary>
        public List<MethodMetaData> MethodMetaDataList { get; set; }

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

            if (PropertyMetaDataList != null && other.PropertyMetaDataList != null)
                PropertyMetaDataList.AddRange(other.PropertyMetaDataList);

            if (MethodMetaDataList != null && other.MethodMetaDataList != null)
                MethodMetaDataList.AddRange(other.MethodMetaDataList);

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

            if (parent.PropertyMetaDataList != null)
            {
                foreach (var metaData in parent.PropertyMetaDataList)
                {
                    var exists = source.PropertyMetaDataList.FirstOrDefault(d => d.Name == metaData.Name);
                    if (exists == null)
                    {
                        source.PropertyMetaDataList.Add(metaData);
                    }
                    else
                    {
                        if (metaData.AttributeMetaDataList != null)
                        {
                            foreach (var attributeMetaData in metaData.AttributeMetaDataList)
                            {
                                if (exists.AttributeMetaDataList.All(d => d.Name != attributeMetaData.Name))
                                {
                                    exists.AttributeMetaDataList.Add(attributeMetaData);
                                }
                            }
                        }
                    }
                }
            }

            if (parent.MethodMetaDataList != null)
            {
                foreach (var metaData in parent.MethodMetaDataList)
                {
                    var exists = source.MethodMetaDataList.FirstOrDefault(d => d.Key == metaData.Key);
                    if (exists == null)
                    {
                        source.MethodMetaDataList.Add(metaData);
                    }
                    else
                    {
                        if (metaData.AttributeMetaDataList != null)
                        {
                            foreach (var attributeMetaData in metaData.AttributeMetaDataList)
                            {
                                if (exists.AttributeMetaDataList.All(d => d.Name != attributeMetaData.Name))
                                {
                                    exists.AttributeMetaDataList.Add(attributeMetaData);
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