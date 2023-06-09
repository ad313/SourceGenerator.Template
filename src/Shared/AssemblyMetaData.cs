using System.Collections.Generic;
using System.Linq;

namespace SourceGenerator.Template.MetaData
{
    /// <summary>
    /// 元数据
    /// </summary>
    public sealed class AssemblyMetaData
    {
        /// <summary>
        /// 宿主程序集名称
        /// </summary>
        public string AssemblyName { get; set; }

        /// <summary>
        /// Interface 元数据
        /// </summary>
        public List<InterfaceMetaData> InterfaceMetaDataList { get; private set; }

        /// <summary>
        /// 类元数据
        /// </summary>
        public List<ClassMetaData> ClassMetaDataList { get; private set; }

        /// <summary>
        /// Struct 元数据
        /// </summary>
        public List<StructMetaData> StructMetaDataList { get; private set; }

        /// <summary>
        /// enum 元数据
        /// </summary>
        public List<EnumMetaData> EnumMetaDataList { get; private set; }

        /// <summary>
        /// 所有 的 Attribute
        /// </summary>
        public List<AttributeMetaData> AllAttributeMetaDataList { get; private set; }

        public AssemblyMetaData(List<InterfaceMetaData> interfaceMetaDataList, List<ClassMetaData> classMetaDataList, List<StructMetaData> structMetaDataList, List<EnumMetaData> enumMetaDataList)
        {
            InterfaceMetaDataList = interfaceMetaDataList;
            ClassMetaDataList = classMetaDataList;
            StructMetaDataList = structMetaDataList;
            EnumMetaDataList = enumMetaDataList;
        }

        /// <summary>
        /// 处理数据，类和接口有继承的情况，处理标记数据
        /// </summary>
        public AssemblyMetaData FormatData()
        {
            #region 处理继承链的叶子节点

            if (ClassMetaDataList != null)
            {
                var bases = ClassMetaDataList.Where(d => d.BaseClassMetaData != null).Select(d => d.BaseClassMetaData).Select(d => d.Key).ToList();
                var leaf = ClassMetaDataList.Where(d => !bases.Contains(d.Key)).ToList();
                leaf.ForEach(item => item.IsLeaf = true);

                //foreach (var metaData in ClassMetaDataList.Where(d => d.IsLeaf))
                //{
                //    metaData.MergeAllParents();
                //}
            }

            if (InterfaceMetaDataList != null)
            {
                var bases = InterfaceMetaDataList.SelectMany(d => d.BaseInterfaceMetaDataList).Select(d => d.Key).ToList();
                var leaf = InterfaceMetaDataList.Where(d => !bases.Contains(d.Key)).ToList();
                leaf.ForEach(item => item.IsLeaf = true);

                //foreach (var metaData in InterfaceMetaDataList.Where(d => d.IsLeaf))
                //{
                //    metaData.MergeAllParents();
                //}
            }

            #endregion

            #region 处理特性对应的class

            var attrClassMetaDataList = ClassMetaDataList?.Where(d => d.IsAttribute).ToList() ?? new List<ClassMetaData>();

            ClassMetaDataList?.ForEach(item =>
            {
                item.AttributeMetaDataList?.ForEach(att => att.SetClassMetaData(item.NewUsingList, attrClassMetaDataList));
                item.PropertyMetaDataList?.ForEach(prop => prop.AttributeMetaDataList?.ForEach(att => att.SetClassMetaData(item.NewUsingList, attrClassMetaDataList)));
                item.MethodMetaDataList?.ForEach(method => method.AttributeMetaDataList?.ForEach(att => att.SetClassMetaData(item.NewUsingList, attrClassMetaDataList)));
            });

            InterfaceMetaDataList?.ForEach(item =>
            {
                item.AttributeMetaDataList?.ForEach(att => att.SetClassMetaData(item.NewUsingList, attrClassMetaDataList));
                item.PropertyMetaDataList?.ForEach(prop => prop.AttributeMetaDataList?.ForEach(att => att.SetClassMetaData(item.NewUsingList, attrClassMetaDataList)));
                item.MethodMetaDataList?.ForEach(method => method.AttributeMetaDataList?.ForEach(att => att.SetClassMetaData(item.NewUsingList, attrClassMetaDataList)));
            });

            StructMetaDataList?.ForEach(item =>
            {
                item.AttributeMetaDataList?.ForEach(att => att.SetClassMetaData(item.NewUsingList, attrClassMetaDataList));
                item.PropertyMetaDataList?.ForEach(prop => prop.AttributeMetaDataList?.ForEach(att => att.SetClassMetaData(item.NewUsingList, attrClassMetaDataList)));
                item.MethodMetaDataList?.ForEach(method => method.AttributeMetaDataList?.ForEach(att => att.SetClassMetaData(item.NewUsingList, attrClassMetaDataList)));
            });

            EnumMetaDataList?.ForEach(item =>
            {
                item.AttributeMetaDataList?.ForEach(att => att.SetClassMetaData(item.NewUsingList, attrClassMetaDataList));
            });

            #endregion

            #region 提取所有的 Attribute

            AllAttributeMetaDataList = InterfaceMetaDataList?.SelectMany(d => d.AttributeMetaDataList).ToList() ?? new List<AttributeMetaData>();
            AllAttributeMetaDataList.AddRange(InterfaceMetaDataList?.SelectMany(d => d.MethodMetaDataList.SelectMany(m => m.AttributeMetaDataList)).ToList() ?? new List<AttributeMetaData>());
            AllAttributeMetaDataList.AddRange(InterfaceMetaDataList?.SelectMany(d => d.PropertyMetaDataList.SelectMany(m => m.AttributeMetaDataList)).ToList() ?? new List<AttributeMetaData>());

            AllAttributeMetaDataList.AddRange(ClassMetaDataList?.SelectMany(d => d.AttributeMetaDataList).ToList() ?? new List<AttributeMetaData>());
            AllAttributeMetaDataList.AddRange(ClassMetaDataList?.SelectMany(d => d.MethodMetaDataList.SelectMany(m => m.AttributeMetaDataList)).ToList() ?? new List<AttributeMetaData>());
            AllAttributeMetaDataList.AddRange(ClassMetaDataList?.SelectMany(d => d.PropertyMetaDataList.SelectMany(m => m.AttributeMetaDataList)).ToList() ?? new List<AttributeMetaData>());

            AllAttributeMetaDataList.AddRange(StructMetaDataList?.SelectMany(d => d.AttributeMetaDataList).ToList() ?? new List<AttributeMetaData>());
            AllAttributeMetaDataList.AddRange(StructMetaDataList?.SelectMany(d => d.MethodMetaDataList.SelectMany(m => m.AttributeMetaDataList)).ToList() ?? new List<AttributeMetaData>());
            AllAttributeMetaDataList.AddRange(StructMetaDataList?.SelectMany(d => d.PropertyMetaDataList.SelectMany(m => m.AttributeMetaDataList)).ToList() ?? new List<AttributeMetaData>());

            AllAttributeMetaDataList.AddRange(EnumMetaDataList?.SelectMany(d => d.AttributeMetaDataList).ToList() ?? new List<AttributeMetaData>());
            AllAttributeMetaDataList.AddRange(EnumMetaDataList?.SelectMany(d => d.MemberMetaList.SelectMany(m => m.AttributeMetaDataList)).ToList() ?? new List<AttributeMetaData>());

            AllAttributeMetaDataList = AllAttributeMetaDataList.GroupBy(d => d.Key).Select(d =>
            {
                var allParam = d.SelectMany(s => s.ParamDictionary.Keys).Distinct().ToDictionary(t => t, t => t);
                var item = d.FirstOrDefault(c => c.ClassMetaData != null) ?? d.First();
                var temp = new AttributeMetaData(item.Name, item.Source)
                {
                    ClassMetaData = item.ClassMetaData,
                    ParamDictionary = allParam
                };
                return temp;
            }).ToList();

            #endregion

            return this;
        }
    }

    public class KeyValueModel
    {
        public string Key { get; private set; }

        public string Value { get; private set; }

        public KeyValueModel(string key, string value)
        {
            Key = key;
            Value = value;
        }
    }
}