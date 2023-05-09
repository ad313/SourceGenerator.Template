using System;
using System.Collections.Generic;
using System.Linq;

namespace SourceGenerator.Analyzers.MetaData
{
    /// <summary>
    /// 枚举元数据
    /// </summary>
    public sealed class EnumMetaData : MetaDataBase, IEquatable<EnumMetaData>
    {
        public string Key => $"{Namespace}.{Name}";

        public EnumMetaData(string @namespace,
            string name,
            List<AttributeMetaData> attributeMetaData,
            List<EnumMemberMetaData> memberMeta,
            List<string> usingList,
            string accessModifier,
            string extModifier)
            : base(name, accessModifier, extModifier, attributeMetaData)
        {
            Namespace = @namespace;
            MemberMeta = memberMeta;
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
        /// 成员集合
        /// </summary>
        public List<EnumMemberMetaData> MemberMeta { get; set; }
        
        /// <summary>
        /// 引用
        /// </summary>
        public List<string> UsingList { get; set; }

        /// <summary>
        /// 引用
        /// </summary>
        public List<string> NewUsingList { get; set; }

        public bool Equals(EnumMetaData other)
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

    /// <summary>
    /// 枚举成员元数据
    /// </summary>
    public class EnumMemberMetaData : MetaDataBase, IEquatable<EnumMemberMetaData>
    {
        public string Key => $"{EnumName}.{Name}";

        public EnumMemberMetaData(string enumName,
            string name,
            int? value,
            List<AttributeMetaData> attributeMetaData)
            : base(name, null, null, attributeMetaData)
        {
            EnumName = enumName;
            Value= value;
        }

        /// <summary>
        /// 枚举名称
        /// </summary>
        public string EnumName { get; set; }

        /// <summary>
        /// 枚举值
        /// </summary>
        public int? Value { get; set; }
        
        public bool Equals(EnumMemberMetaData other)
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