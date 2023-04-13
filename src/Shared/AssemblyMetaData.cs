using System.Collections.Generic;
using System.Linq;

namespace SourceGenerator.Analyzers.MetaData
{
    /// <summary>
    /// 扫描接口、类元数据
    /// </summary>
    public class AssemblyMetaData
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

        public AssemblyMetaData(List<InterfaceMetaData> interfaceMetaDataList, List<ClassMetaData> classMetaDataList, List<StructMetaData> structMetaDataList)
        {
            InterfaceMetaDataList = interfaceMetaDataList;
            ClassMetaDataList = classMetaDataList;
            StructMetaDataList = structMetaDataList;
        }

        /// <summary>
        /// 处理数据，类和接口有继承的情况，处理标记数据
        /// </summary>
        public AssemblyMetaData FormatData()
        {
            if (ClassMetaDataList != null)
            {
                var bases = ClassMetaDataList.SelectMany(d => d.BaseClassMetaDataList).Select(d => d.Key).ToList();
                var leaf = ClassMetaDataList.Where(d => !bases.Contains(d.Key)).ToList();
                leaf.ForEach(item => item.IsLeaf = true);

                //foreach (var metaData in ClassMetaDataList.Where(d => d.IsLeaf))
                //{
                //    metaData.MergeAllParent();
                //}
            }

            if (InterfaceMetaDataList != null)
            {
                var bases = InterfaceMetaDataList.SelectMany(d => d.BaseInterfaceMetaDataList).Select(d => d.Key).ToList();
                var leaf = InterfaceMetaDataList.Where(d => !bases.Contains(d.Key)).ToList();
                leaf.ForEach(item => item.IsLeaf = true);

                //foreach (var metaData in InterfaceMetaDataList.Where(d => d.IsLeaf))
                //{
                //    metaData.MergeAllParent();
                //}
            }

            return this;
        }
    }

    public class KeyValueModel
    {
        public string Key { get; set; }

        public string Value { get; set; }

        public KeyValueModel(string key, string value)
        {
            Key = key;
            Value = value;
        }
    }
}