using System.Collections.Generic;

namespace SourceGenerator.Template.MetaData
{
    /// <summary>
    /// 结构体元数据
    /// </summary>
    public sealed class StructMetaData : InterfaceMetaData
    {
        /// <summary>Initializes a new instance of the <see cref="T:System.Object"></see> class.</summary>
        public StructMetaData(
            string @namespace,
            string name,
            List<AttributeMetaData> attributeMetaData,
            List<PropertyMetaData> propertyMeta,
            List<MethodMetaData> methodMetaDataList,
            List<string> baseInterfaces,
            List<KeyValueModel> constructor,
            List<string> usingList,
            string accessModifier,
            string extModifier = null,
            string source = null)
            : base(@namespace, name, attributeMetaData, propertyMeta, methodMetaDataList, baseInterfaces, usingList, accessModifier, extModifier, source)
        {
            Constructor = constructor;
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        public List<KeyValueModel> Constructor { get; set; }
        
        /// <summary>
        /// 合并
        /// </summary>
        /// <param name="other"></param>
        public void MergePartial(StructMetaData other)
        {
            if (other == null)
                return;

            base.MergePartial(other);
        }
    }
}