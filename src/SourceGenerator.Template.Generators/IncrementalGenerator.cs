using Microsoft.CodeAnalysis;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using SourceGenerator.Template.Generators.Renders;
using SourceGenerator.Template.MetaData;
using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Text;

namespace SourceGenerator.Template.Generators
{
    /// <summary>
    /// 代码生成器
    /// </summary>
    [Generator]
    public class IncrementalGenerator : IIncrementalGenerator
    {
        private readonly StringBuilder _errorBuilder = new StringBuilder();
        private readonly StringBuilder _timeBuilder = new StringBuilder();

        public static SourceProductionContext Context;
        /// <summary>
        /// Map 文件名称
        /// </summary>
        public const string MapName = "Map.json";
        /// <summary>
        /// 额外提供的程序集的资源目录
        /// </summary>
        public const string AssemblySourceBase = "Templates";
        /// <summary>
        /// 额外提供的程序集的名称 包含关系
        /// </summary>
        public const string TemplateAssemblyName = "Sg.Templates.dll";

        /// <summary>
        /// 初始化
        /// </summary>
        /// <param name="initializationContextContext"></param>
        public void Initialize(IncrementalGeneratorInitializationContext initializationContextContext)
        {
            //Debugger.Launch();

            var textFiles = initializationContextContext.AdditionalTextsProvider.Where(file =>
                    file.Path.EndsWith(".txt", StringComparison.OrdinalIgnoreCase) ||
                    file.Path.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                .Collect();

            var compilations = initializationContextContext.CompilationProvider.Select((compilation, cancellationToken) => compilation);

            initializationContextContext.RegisterSourceOutput(compilations.Combine(textFiles), (context, compilation) =>
            {
                try
                {
                    //if (context.CancellationToken.IsCancellationRequested)
                    //    return;

                    Context = context;
                    Execute(context, compilation.Left, compilation.Right);
                }
                catch (Exception e)
                {
                    TemplateRender.ToErrorStringBuilder("全局异常", _errorBuilder, e);
                }
                finally
                {
                    context.AddSource("Error", _errorBuilder.ToString());
                    context.AddSource("Time", _timeBuilder.ToString());
                }
            });
        }

        /// <summary>
        /// 执行
        /// </summary>
        /// <param name="context"></param>
        /// <param name="compilation"></param>
        /// <param name="additionalTexts">附加文本文件</param>
        public void Execute(SourceProductionContext context, Compilation compilation, ImmutableArray<AdditionalText> additionalTexts)
        {
            var watch = Stopwatch.StartNew();
            watch.Start();

            AssemblyMetaData meta = null;

            #region 1、获取元数据

            try
            {
                meta = new SyntaxReceiver(compilation, context.CancellationToken).GetMetaData(out StringBuilder sb);
                meta.AssemblyName = compilation.AssemblyName;

                TemplateRender.ToTimeStringBuilder("1、获取元数据", _timeBuilder, watch.ElapsedMilliseconds);

               _timeBuilder.AppendLine(TemplateRender.ToMetaStringBuilder(meta).ToString());
               _timeBuilder.AppendLine(sb.ToString());
            }
            catch (Exception e)
            {
                TemplateRender.ToErrorStringBuilder("1、获取元数据", _errorBuilder, e);
                return;
            }
            finally
            {
                //输出元数据json
                context.AddSource("MetaJson", "//" + JsonConvert.SerializeObject(meta, new JsonSerializerSettings()
                {
                    ContractResolver = new DefaultContractResolver() { NamingStrategy = new SnakeCaseNamingStrategy() }
                }));

                watch.Restart();
            }

            #endregion

            #region 2、渲染模板
            try
            {
                //if (context.CancellationToken.IsCancellationRequested)
                //    return;
                
                TemplateRender.Build(context, additionalTexts, meta, compilation);
                TemplateRender.ToTimeStringBuilder("2、渲染模板", _timeBuilder, watch.ElapsedMilliseconds);
            }
            catch (Exception e)
            {
                TemplateRender.ToErrorStringBuilder("2、渲染模板", _errorBuilder, e);
            }
            finally
            {
                watch.Stop();
            }
            #endregion
        }
    }
}
