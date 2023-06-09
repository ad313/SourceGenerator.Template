using System.Collections.Generic;
using System.Linq;

namespace SourceGenerator.Analyzers.MetaData
{
    public class MetaDataBase
    {
        /// <summary>
        /// 名称
        /// </summary>
        public string Name { get; private set; }
        /// <summary>
        /// 访问修饰符 public private 等
        /// </summary>
        public string AccessModifier { get; set; }
        /// <summary>
        /// 扩展修饰符 partial,virtual string,int 等
        /// </summary>
        public string ExtModifier { get; set; }
        /// <summary>
        /// 原始字符串
        /// </summary>
        public string Source { get; private set; }
        /// <summary>
        /// Attribute 参数
        /// </summary>
        public List<AttributeMetaData> AttributeMetaDataList { get; private set; }

        public MetaDataBase(string name, string accessModifier, string extModifier, List<AttributeMetaData> attributeMetaDataList, string source)
        {
            Name = name;
            AccessModifier = accessModifier;
            ExtModifier = extModifier;
            AttributeMetaDataList = attributeMetaDataList;
            Source = source;
        }

        /// <summary>
        /// 方法唯一标识符，区分方法重载
        /// </summary>
        /// <param name="name"></param>
        /// <param name="param"></param>
        /// <returns></returns>
        public string GetKey(string name, List<KeyValueModel> param) => $"{name}_{string.Join("_", param.Select(d => $"{d.Key}_{d.Value}"))}";

        /// <summary>
        /// 合并
        /// </summary>
        /// <param name="other"></param>
        public void Append(MetaDataBase other)
        {
            if (other == null)
                return;

            if (AttributeMetaDataList != null && other.AttributeMetaDataList != null)
                AttributeMetaDataList.AddRange(other.AttributeMetaDataList);
        }
    }
}