using Microsoft.CodeAnalysis;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using SourceGenerator.Analyzers.MetaData;
using SourceGenerator.Analyzers.Renders;
using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace SourceGenerator.Analyzers
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
        /// 初始化
        /// </summary>
        /// <param name="initializationContextContext"></param>
        public void Initialize(IncrementalGeneratorInitializationContext initializationContextContext)
        {
            //Debugger.Launch();

            var textFiles = initializationContextContext.AdditionalTextsProvider.Where(file => file.Path.EndsWith(".txt", StringComparison.OrdinalIgnoreCase)).Collect();
            var compilations = initializationContextContext.CompilationProvider.Select((compilation, cancellationToken) => compilation);

            initializationContextContext.RegisterSourceOutput(compilations.Combine(textFiles), (context, compilation) =>
            {
                try
                {
                    if (context.CancellationToken.IsCancellationRequested) 
                        return;

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
                    context.AddSource("Times", _timeBuilder.ToString());
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

            var receiver = new SyntaxReceiver(compilation, context.CancellationToken);
            AssemblyMetaData meta = null;

            #region 1、获取元数据

            try
            {
                meta = receiver.GetMetaData(compilation);
                meta.AssemblyName = compilation.AssemblyName;
                
                TemplateRender.ToTimeStringBuilder("1、获取元数据", _timeBuilder, watch);
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

            #region 5、自定义模板生成代码
            try
            {
                if (context.CancellationToken.IsCancellationRequested)
                    return;

                TemplateRender.BuildExtend(context, additionalTexts, meta, _errorBuilder);
                TemplateRender.ToTimeStringBuilder("2、自定义模板生成代码", _timeBuilder, watch);
            }
            catch (Exception e)
            {
                TemplateRender.ToErrorStringBuilder("2、自定义模板生成代码", _errorBuilder, e);
                return;
            }
            finally
            {
                watch.Stop();
            } 
            #endregion
        }
    }
}
