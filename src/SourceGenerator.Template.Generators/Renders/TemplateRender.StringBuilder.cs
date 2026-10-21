using Newtonsoft.Json;
using SourceGenerator.Template.MetaData;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace SourceGenerator.Template.Generators.Renders
{
    internal sealed partial class TemplateRender
    {
        // 注：旧的 ToErrorStringBuilder（含时间戳）/ ToTimeStringBuilder（耗时统计）已移除（DET-01），
        // 错误收集改由 RenderSession.AddError 负责（确定性内容），耗时输出整体取消

        /// <summary>
        /// 生成加载模板与外部模板模板统计情况
        /// </summary>
        public static StringBuilder ToTemplateAssemblyStringBuilder(List<MapModel> maps, List<Assembly> templateAssemblyList)
        {
            var sb = new StringBuilder();
            sb.AppendLine("/*");
            sb.Append("1、模板程序集：");
            sb.AppendLine(string.Join("、", templateAssemblyList.Select(d => d.FullName)));
            sb.AppendLine("");
            sb.AppendLine("2、模板 Map：");
            sb.AppendLine(JsonConvert.SerializeObject(maps, Formatting.Indented));
            sb.AppendLine();
            sb.AppendLine("*/");
            return sb;
        }
    }
}
