using System;
using System.Collections.Generic;
using System.Linq;

namespace SourceGenerator.Analyzers.MetaData
{
    /// <summary>
    /// 类元数据
    /// </summary>
    public sealed class ClassMetaData : InterfaceMetaData
    {
        /// <summary>Initializes a new instance of the <see cref="T:System.Object"></see> class.</summary>
        public ClassMetaData(
            string @namespace,
            string name,
            List<AttributeMetaData> attributeMetaData,
            List<PropertyMetaData> propertyMeta,
            List<MethodMetaData> methodMetaData,
            List<string> baseInterfaceList,
            string baseClass,
            List<KeyValueModel> constructor,
            List<string> usingList,
            string accessModifier,
            string extModifier = null,
            string source = null)
            : base(@namespace, name, attributeMetaData, propertyMeta, methodMetaData, baseInterfaceList, usingList, accessModifier, extModifier, source)
        {
            Constructor = constructor;
            BaseInterfaceList = baseInterfaceList;
            BaseClass = baseClass;

            IsAttribute = BaseClass != null && BaseClass.Split('.').Last() == "Attribute";
        }

        /// <summary>
        /// 基类
        /// </summary>
        public string BaseClass { get; set; }

        /// <summary>
        /// 基类
        /// </summary>
        public ClassMetaData BaseClassMetaData { get; set; }

        /// <summary>
        /// 构造函数
        /// </summary>
        public List<KeyValueModel> Constructor { get; set; }

        /// <summary>
        /// 是否是 Attribute
        /// </summary>
        public bool IsAttribute { get; set; }

        public override bool BaseExists(string key)
        {
            if (string.IsNullOrWhiteSpace(BaseClass))
                return false;

            return BaseClass.Equals(key, StringComparison.OrdinalIgnoreCase) ||
                   NewUsingList.Select(u => $"{u}.{BaseClass.Split('.').Last()}").Contains(key);
        }

        /// <summary>
        /// 加上命名空间 判断是否存在
        /// </summary>
        /// <param name="key"></param>
        /// <param name="names"></param>
        /// <returns></returns>
        public bool ExistsInterface(string key, List<string> names)
        {
            if (string.IsNullOrWhiteSpace(key))
                return false;

            return names.SelectMany(name => NewUsingList.Select(u => $"{u}.{name.Split('.').Last()}")).Contains(key);
        }

        /// <summary>
        /// 合并分部
        /// </summary>
        /// <param name="other"></param>
        public void MergePartial(ClassMetaData other)
        {
            if (other == null)
                return;

            base.MergePartial(other);

            if (!string.IsNullOrWhiteSpace(other.BaseClass))
                BaseClass = other.BaseClass;

            if (other.BaseClassMetaData != null)
                BaseClassMetaData = other.BaseClassMetaData;
        }

        /// <summary>
        /// 合并所有父级数据
        /// </summary>
        public override void MergeAllParents()
        {
            MergeParentItem(this, BaseClassMetaData);
        }

        /// <summary>
        /// 合并所有父级数据
        /// </summary>
        private void MergeParentItem(ClassMetaData source, ClassMetaData parent)
        {
            if (parent == null)
                return;

            if (parent.BaseClassMetaData != null)
            {
                MergeParentItem(parent, parent.BaseClassMetaData);
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
    }
}