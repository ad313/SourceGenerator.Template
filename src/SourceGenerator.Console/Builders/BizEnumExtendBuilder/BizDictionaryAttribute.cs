using System;
using System.ComponentModel;

namespace SourceGenerator.Consoles.Builders.BizEnumExtendBuilder
{
    /// <summary>
    /// 字典服务
    /// </summary>
    public class BizDictionaryAttribute : Attribute
    {
        /// <summary>
        /// 指定字典编码
        /// </summary>
        public string Code { get; set; }
        /// <summary>
        /// 字段处理类型
        /// </summary>
        public BizTypeEnum BizType { get; set; } = BizTypeEnum.Dictionary;
        /// <summary>
        /// 是否是多选，用逗号 , 分割
        /// </summary>
        public bool IsMultiple { get; set; }
    }

    /// <summary>
    /// 处理类型
    /// </summary>
    public enum BizTypeEnum
    {
        [Description("字典")]
        Dictionary,
        [Description("部门")]
        Department,
        [Description("用户")]
        User,
        [Description("行政区划")]
        Region,
        [Description("枚举")]
        Enum
    }
}
