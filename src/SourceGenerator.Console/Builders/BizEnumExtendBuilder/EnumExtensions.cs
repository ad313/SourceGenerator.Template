using System;
using System.ComponentModel;
using System.Linq;
using System.Reflection;

namespace SourceGenerator.Consoles.Builders.BizEnumExtendBuilder
{
    /// <summary>
    /// 枚举扩展类
    /// </summary>
    public static class EnumExtensions
    {
        /// <summary>
        /// 获取枚举的描述
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string GetEnumDescription(this Enum value)
        {
            return value?.GetType()
                .GetMember(value.ToString())
                .FirstOrDefault()?
                .GetCustomAttribute<DescriptionAttribute>()
                ?.Description;
        }
    }
}
