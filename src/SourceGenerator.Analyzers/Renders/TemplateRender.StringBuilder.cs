using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Newtonsoft.Json;
using SourceGenerator.Analyzers.MetaData;

namespace SourceGenerator.Analyzers.Renders
{
    internal sealed partial class TemplateRender
    {
        /// <summary>
        /// 生成错误内容
        /// </summary>
        /// <param name="name"></param>
        /// <param name="sb"></param>
        /// <param name="e"></param>
        public static void ToErrorStringBuilder(string name, StringBuilder sb, Exception e)
        {
            sb ??= new StringBuilder();
            sb.AppendLine($"// {name} 异常 => {DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}");
            sb.AppendLine($"// Message：{e.Message?.Replace("\r\n", "\r\n//")}");
            sb.AppendLine($"// InnerException：{e.InnerException?.Message?.Replace("\r\n", "\r\n//")}");
            sb.Append($"// StackTrace：{e.StackTrace?.Replace("\r\n", "\r\n//")}");
            sb.AppendLine();
            sb.Append($"// InnerException.StackTrace：{e.InnerException?.StackTrace?.Replace("\r\n", "\r\n//")}");
            sb.AppendLine();
            sb.AppendLine();
        }

        /// <summary>
        /// 生成时间统计内容
        /// </summary>
        /// <param name="name"></param>
        /// <param name="sb"></param>
        /// <param name="elapsedMilliseconds"></param>
        public static void ToTimeStringBuilder(string name, StringBuilder sb, long elapsedMilliseconds)
        {
            sb ??= new StringBuilder();
            sb.AppendLine($"// {name} =>");
            sb.AppendLine($"// 耗时：{elapsedMilliseconds} ms");
            sb.AppendLine();
        }

        /// <summary>
        /// 生成元数据统计内容
        /// </summary>
        public static StringBuilder ToMetaStringBuilder(AssemblyMetaData meta)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"//interface：{meta.InterfaceMetaDataList.Count} 个");
            sb.AppendLine($"//    class：{meta.ClassMetaDataList.Count} 个");
            sb.AppendLine($"//   struct：{meta.StructMetaDataList.Count} 个");
            sb.AppendLine($"//     enum：{meta.EnumMetaDataList.Count} 个");
            return sb;
        }

        /// <summary>
        /// 生成加载模板与外部模板模板统计情况
        /// </summary>
        public static StringBuilder ToTemplateAssemblyStringBuilder(List<MapModel> maps, List<Assembly> templateAssemblyList)
        {
            var sb = new StringBuilder();
            sb.Append("// 模板程序集：");
            sb.AppendLine(string.Join("、", templateAssemblyList.Select(d => d.FullName)));
            sb.Append("// 模板map：");
            sb.AppendLine(JsonConvert.SerializeObject(maps));
            return sb;
        }
    }
}