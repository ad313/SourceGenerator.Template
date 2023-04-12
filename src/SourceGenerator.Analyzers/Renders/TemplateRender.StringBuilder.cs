using System;
using System.Diagnostics;
using System.Text;

namespace SourceGenerator.Analyzers.Renders
{
    internal partial class TemplateRender
    {
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

        public static void ToTimeStringBuilder(string name, StringBuilder sb, Stopwatch watch)
        {
            sb ??= new StringBuilder();
            sb.AppendLine($"// {name} =>");
            sb.AppendLine($"// 耗时：{watch.ElapsedMilliseconds}");
            watch.Restart();
            sb.AppendLine();
            sb.AppendLine();
        }

        //public static void ToTraceStringBuilder(StringBuilder sb, AopCodeBuilder builder)
        //{
        //    sb ??= new StringBuilder();
        //    sb.AppendLine($"// -------------------------------------------------");
        //    sb.AppendLine($"//命名空间：{builder.Namespace}");
        //    sb.AppendLine($"//文件名称：{builder.SourceCodeName}");
        //    sb.AppendLine($"//接口：{string.Join("、", builder._metaData.InterfaceMetaData.Select(d => d.Key))}");
        //    sb.AppendLine($"//类名：{builder.ClassName}");

        //    foreach (var methodData in builder._metaData.MethodMetaDatas)
        //    {
        //        sb.AppendLine();
        //        sb.AppendLine($"//方法名称：{methodData.Name}");
        //        sb.AppendLine($"//Key名称：{methodData.Key}");
        //        sb.AppendLine($"//Aop属性：{string.Join("、", methodData.AttributeMetaData.Select(d => d.Name))}");
        //    }

        //    sb.AppendLine();
        //    sb.AppendLine();
        //}

        //public static StringBuilder ToRegisterStringBuilder(StringBuilder sb, List<AopCodeBuilder> builders, AssemblyMetaData mateData)
        //{
        //    sb ??= new StringBuilder();
        //    sb.AppendLine("namespace Microsoft.Extensions.DependencyInjection");
        //    sb.AppendLine("{");
        //    sb.AppendLine($"\tinternal static class AopClassExtensions");
        //    sb.AppendLine("\t{");
        //    sb.AppendLine($"\t\tpublic static IServiceCollection RegisterInjectionFor{mateData.AssemblyName.Replace(".","")}(this IServiceCollection services)");
        //    sb.AppendLine("\t\t{");

        //    foreach (var aopAttribute in mateData.AopAttributeMetaDataList)
        //    {
        //        sb.AppendLine($"\t\t\tservices.AddTransient<{aopAttribute.Key}>();");
        //    }

        //    sb.AppendLine();

        //    foreach (var builder in builders)
        //    {
        //        if (builder._metaData.InterfaceMetaData.Any())
        //        {
        //            sb.AppendLine($"\t\t\tservices.AddScoped<{builder._metaData.InterfaceMetaData.First().Key}, {builder._metaData.NameSpace}.{builder.ClassName}>();");
        //        }
        //        else
        //        {
        //            sb.AppendLine($"\t\t\tservices.AddScoped<{builder._metaData.NameSpace}.{builder._metaData.Name}, {builder._metaData.NameSpace}.{builder.ClassName}>();");
        //        }
        //    }

        //    sb.AppendLine("\t\t\treturn services;");
        //    sb.AppendLine("\t\t}");
        //    sb.AppendLine("\t}");
        //    sb.AppendLine("}");

        //    return sb;
        //}
    }
}