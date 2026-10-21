using Microsoft.CodeAnalysis;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using SourceGenerator.Template.Generators.Renders;
using SourceGenerator.Template.MetaData;
using System;
using System.Collections.Immutable;

namespace SourceGenerator.Template.Generators
{
    /// <summary>
    /// 代码生成器
    /// GEN-01：生成器本身保持无状态——旧实现把错误/耗时 StringBuilder 放在实例字段（永不清空）、
    /// 把 SourceProductionContext 放在静态全局字段，Roslyn 复用/并发执行时会跨编译累积与串扰；
    /// 现在全部状态收敛到单次执行的 <see cref="RenderSession"/>。
    /// DET-01：不再输出 Time 耗时文件，Error 内容去掉时间戳，保证相同输入产物逐字节一致。
    /// </summary>
    [Generator]
    public class IncrementalGenerator : IIncrementalGenerator
    {
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
                Execute(context, compilation.Left, compilation.Right);
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
            var session = new RenderSession(context);

            AssemblyMetaData meta = null;

            #region 1、获取元数据

            try
            {
                if (context.CancellationToken.IsCancellationRequested)
                    return;

                var receiver = new SyntaxReceiver(compilation, context.CancellationToken);
                meta = receiver.GetMetaData();
                meta.AssemblyName = compilation.AssemblyName;

                // GEN-02：元数据阶段收集的诊断统一上报，失败不再静默
                foreach (var diagnostic in receiver.Diagnostics)
                {
                    context.ReportDiagnostic(diagnostic);
                }
            }
            catch (Exception e)
            {
                context.ReportDiagnostic(Diagnostic.Create(GeneratorDiagnostics.MetadataError, Location.None, "(Assembly)", e.Message));
                session.AddError("1、获取元数据", e);
            }
            finally
            {
                //输出元数据json
                context.AddSource("MetaJson.g.cs", "//" + JsonConvert.SerializeObject(meta, new JsonSerializerSettings()
                {
                    ContractResolver = new DefaultContractResolver() { NamingStrategy = new SnakeCaseNamingStrategy() }
                }));
            }

            #endregion

            #region 2、渲染模板

            try
            {
                if (meta != null && !context.CancellationToken.IsCancellationRequested)
                {
                    TemplateRender.Build(session, additionalTexts, meta, compilation);
                }
            }
            catch (Exception e)
            {
                context.ReportDiagnostic(Diagnostic.Create(GeneratorDiagnostics.TemplateRenderError, Location.None, "(global)", e.Message));
                session.AddError("2、渲染模板", e);
            }

            #endregion

            context.AddSource("Error.g.cs", session.ErrorContent);
        }
    }
}
