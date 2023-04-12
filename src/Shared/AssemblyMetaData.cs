using System.Collections.Generic;

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

        public AssemblyMetaData(List<InterfaceMetaData> interfaceMetaDataList, List<ClassMetaData> classMetaDataList)
        {
            InterfaceMetaDataList = interfaceMetaDataList;
            ClassMetaDataList = classMetaDataList;
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