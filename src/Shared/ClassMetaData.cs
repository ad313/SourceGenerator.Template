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

        public new bool Has(string key)
        {
            var newUsing = new string[UsingList.Count];
            Array.Copy(UsingList.ToArray(), newUsing, UsingList.Count);
            newUsing = newUsing.Append(Namespace).ToArray();

            return BaseClasses.Contains(key) || BaseClasses.SelectMany(t => newUsing.Select(u => $"{u.Replace("using ", "").Replace(";", "")}.{t.Split('.').Last()}")).Contains(key);
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

            if (BaseClasses != null && other.BaseClasses != null)
            {
                BaseClasses.AddRange(other.BaseClasses);
                BaseClasses = BaseClasses.Distinct().ToList();
            }

            if (BaseClassMetaDataList != null && other.BaseClassMetaDataList != null)
                BaseClassMetaDataList.AddRange(other.BaseClassMetaDataList);
        }

        /// <summary>
        /// 合并所有父级数据
        /// </summary>
        public override void MergeAllParent()
        {
            MergeParentItem(this, BaseClassMetaDataList.FirstOrDefault());
        }

        /// <summary>
        /// 合并所有父级数据
        /// </summary>
        private void MergeParentItem(ClassMetaData source, ClassMetaData parent)
        {
            if (parent == null)
                return;

            if (parent.BaseClassMetaDataList != null && parent.BaseClassMetaDataList.Any())
            {
                MergeParentItem(parent, parent.BaseClassMetaDataList.First());
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