using System.ComponentModel.DataAnnotations;

namespace SourceGenerator.Consoles.Builders.BizEnumExtendBuilder
{
    [BizDictionary]
    public partial class BizDictionaryClass
    {
        [Display(Name = "枚举枚举")]
        [BizDictionary(BizType = BizTypeEnum.Enum)]
        public BizTypeEnum? EnumType211 { get; set; }

        /// <summary>
        /// 单个字典，单选
        /// </summary>
        [Display(Name = "单个字典")]
        [BizDictionary(BizType = BizTypeEnum.Dictionary, Code = "aaaaa")]
        public string Name1111 { get; set; }

        /// <summary>
        /// 单个字典，多选
        /// </summary>
        [Display(Name = "多个字典")]
        [BizDictionary(BizType = BizTypeEnum.Dictionary, IsMultiple = true)]
        public string Name1122 { get; set; }

        /// <summary>
        /// 这是行政区划
        /// </summary>
        [Display(Name = "单个行政区划")]
        [BizDictionary(BizType = BizTypeEnum.Region)]
        public string Region11 { get; set; }

        /// <summary>
        /// 这是行政区划
        /// </summary>
        [Display(Name = "多个行政区划")]
        [BizDictionary(BizType = BizTypeEnum.Region, IsMultiple = true)]
        public string Region112 { get; set; }

        /// <summary>
        /// 这是部门
        /// </summary>
        [Display(Name = "单个部门")]
        [BizDictionary(BizType = BizTypeEnum.Department)]
        public string Department11 { get; set; }

        /// <summary>
        /// 这是部门
        /// </summary>
        [Display(Name = "多个部门")]
        [BizDictionary(BizType = BizTypeEnum.Department, IsMultiple = true)]
        public string Department112 { get; set; }


        /// <summary>
        /// 这是用户
        /// </summary>
        [Display(Name = "单个用户")]
        [BizDictionary(BizType = BizTypeEnum.User)]
        public string User1 { get; set; }

        /// <summary>
        /// 这是用户
        /// </summary>
        [Display(Name = "多个用户")]
        [BizDictionary(BizType = BizTypeEnum.User, IsMultiple = true)]
        public string User2 { get; set; }
    }
}
