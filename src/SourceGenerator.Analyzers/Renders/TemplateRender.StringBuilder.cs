using System;
using System.Diagnostics;
using System.Text;

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
            sb.AppendLine($"// 耗时：{elapsedMilliseconds}");
            sb.AppendLine();
        }
    }
}