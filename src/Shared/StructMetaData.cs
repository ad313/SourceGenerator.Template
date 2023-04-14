using System.Collections.Generic;

namespace SourceGenerator.Analyzers.MetaData
{
    /// <summary>
    /// 类元数据
    /// </summary>
    public class StructMetaData : InterfaceMetaData
    {
        /// <summary>Initializes a new instance of the <see cref="T:System.Object"></see> class.</summary>
        public StructMetaData(
            string @namespace,
            string name,
            List<AttributeMetaData> attributeMetaData,
            List<PropertyMetaData> propertyMeta,
            List<MethodMetaData> methodMetaData,
            List<string> baseInterfaces,
            List<KeyValueModel> constructor,
            List<string> usingList,
            string accessModifier,
            string extModifier = null)
            : base(@namespace, name, attributeMetaData, propertyMeta, methodMetaData, baseInterfaces, usingList, accessModifier, extModifier)
        {
            Constructor = constructor;
        }
        
        /// <summary>
        /// 构造函数参数
        /// </summary>
        public List<KeyValueModel> Constructor { get; set; }

        //public new bool Has(string key)
        //{
        //    var newUsing = new string[Usings.Count];
        //    Array.Copy(Usings.ToArray(), newUsing, Usings.Count);
        //    newUsing = newUsing.Append(Namespace).ToArray();

        //    return BaseStructs.Contains(key) || BaseStructs.SelectMany(t => newUsing.Select(u => $"{u.Replace("using ", "").Replace(";", "")}.{t.Split('.').Last()}")).Contains(key);
        //}

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