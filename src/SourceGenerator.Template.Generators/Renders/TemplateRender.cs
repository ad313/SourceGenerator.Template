using Microsoft.CodeAnalysis;
using Newtonsoft.Json;
using Scriban.Runtime;
using SourceGenerator.Template.Generators.Extensions;
using SourceGenerator.Template.MetaData;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;

namespace SourceGenerator.Template.Generators.Renders
{
    internal sealed partial class TemplateRender
    {
        /// <summary>
        /// 模板程序集加载缓存（SEC-02 隔离措施之一）：Assembly.LoadFile 加载的程序集在进程内无法卸载，
        /// 按路径缓存避免每次执行都重复加载同一文件
        /// </summary>
        private static readonly ConcurrentDictionary<string, Assembly> TemplateAssemblyCache = new ConcurrentDictionary<string, Assembly>();

        /// <summary>
        /// 执行渲染模板
        /// </summary>
        /// <param name="session">单次执行会话</param>
        /// <param name="additionalTexts">当前程序集参与编译的分析器文件</param>
        /// <param name="meta">当前程序集元数据</param>
        /// <param name="compilation">提供额外的模板程序集</param>
        public static void Build(RenderSession session, ImmutableArray<AdditionalText> additionalTexts, AssemblyMetaData meta, Compilation compilation)
        {
            // 把会话挂到模板函数的作用域内（AsyncLocal 按执行流隔离，执行结束恢复原值），
            // 模板内的 render/json 函数从这里取会话，不再依赖静态全局 Context（GEN-01）
            var previous = FilterFunctions.CurrentSession;
            FilterFunctions.CurrentSession = session;
            try
            {
                //加载外部模板程序集
                var templateAssemblyList = LoadTemplateAssemblies(compilation, session);

                //获取模板
                var maps = GetMaps(session, additionalTexts, templateAssemblyList);

                session.AddSource("TemplateInfo.g.cs", ToTemplateAssemblyStringBuilder(maps, templateAssemblyList).ToString());

                RenderTemplate(session, meta, maps);
            }
            finally
            {
                FilterFunctions.CurrentSession = previous;
            }
        }

        /// <summary>
        /// SEC-02：模板程序集加载。
        /// 说明：模板包的既定分发格式是“以 Sg.Templates.dll 命名的普通程序集引用 + 嵌入资源”，
        /// 嵌入资源没有对应的 Roslyn 标准读取通道（AdditionalFiles 只能来自使用方项目，
        /// 无法由被引用的模板包在编译期注入生成器输入），因此现阶段仍需 Assembly.LoadFile 读取资源。
        /// 隔离措施：逐个 try/catch —— 单个程序集加载失败只跳过并报 SGT004，不再拖垮全部模板
        /// （旧实现一个 LoadFile 异常即整体失败）；按路径缓存，避免重复加载与反复文件锁定。
        /// </summary>
        private static List<Assembly> LoadTemplateAssemblies(Compilation compilation, RenderSession session)
        {
            var result = new List<Assembly>();
            var templateAssemblyDllList = compilation.References
                .Where(d => d.Display != null && d.Display.EndsWith(IncrementalGenerator.TemplateAssemblyName)).ToList();

            foreach (var reference in templateAssemblyDllList)
            {
                try
                {
                    result.Add(TemplateAssemblyCache.GetOrAdd(reference.Display, path => Assembly.LoadFile(path)));
                }
                catch (Exception e)
                {
                    session.Report(Diagnostic.Create(GeneratorDiagnostics.TemplateAssemblyLoadError, Location.None, reference.Display, e.Message));
                    session.AddError("加载模板程序集", e);
                }
            }

            return result;
        }

        private static List<MapModel> GetMaps(RenderSession session, ImmutableArray<AdditionalText> additionalTexts, List<Assembly> templateAssemblyList)
        {
            var list = new List<MapModel>();
            var json = additionalTexts.FirstOrDefault(d => d.Path.Replace("/", "\\").EndsWith($"\\{IncrementalGenerator.MapName}", StringComparison.OrdinalIgnoreCase))?.GetText()?.ToString();
            if (!string.IsNullOrWhiteSpace(json))
            {
                try
                {
                    list = JsonConvert.DeserializeObject<List<MapModel>>(json) ?? new List<MapModel>();
                }
                catch (Exception e)
                {
                    // GEN-02：Map.json 解析失败报诊断，不再静默无生成结果
                    session.Report(Diagnostic.Create(GeneratorDiagnostics.MapConfigError, Location.None, $"Map.json 解析失败：{e.Message}"));
                    list = new List<MapModel>();
                }
            }

            // README 约定 Code 不能重复：重复项保留第一个并报诊断（旧实现从不校验）
            var seenCodes = new HashSet<string>();
            foreach (var model in list)
            {
                if (!string.IsNullOrWhiteSpace(model.Code) && !seenCodes.Add(model.Code))
                {
                    session.Report(Diagnostic.Create(GeneratorDiagnostics.MapConfigError, Location.None, $"Map.json 中 Code '{model.Code}' 重复，已忽略后续重复项"));
                    model.Enable = false;
                }
            }

            foreach (var model in list)
            {
                if (!model.Enable)
                    continue;

                // BUG-10：Templates 缺省为 null 时旧实现 .Any() 空引用使整个渲染失败，现已给默认值并判空
                if (model.Templates != null && model.Templates.Any())
                {
                    foreach (var template in model.Templates)
                    {
                        var file = additionalTexts.FirstOrDefault(d => d.Path.Replace("/", "\\").EndsWith($"\\{template}", StringComparison.OrdinalIgnoreCase));
                        if (file == null)
                        {
                            // GEN-02：模板未找到报 SGT002 并跳过该模板，不再抛异常中断全部渲染
                            session.Report(Diagnostic.Create(GeneratorDiagnostics.TemplateNotFound, Location.None, template));
                            continue;
                        }

                        model.TemplateDictionary ??= new ConcurrentDictionary<string, string>();
                        model.TemplateDictionary.TryAdd(template, file.GetText()?.ToString());
                        model.Path = file.Path;
                    }
                }

                if (!string.IsNullOrWhiteSpace(model.MainTemplate))
                {
                    var mainFile = additionalTexts.FirstOrDefault(d =>
                        d.Path.Replace("/", "\\").EndsWith($"\\{model.MainTemplate}", StringComparison.OrdinalIgnoreCase));
                    if (mainFile == null)
                    {
                        session.Report(Diagnostic.Create(GeneratorDiagnostics.TemplateNotFound, Location.None, model.MainTemplate));
                    }
                    else
                    {
                        model.MainTemplateString = mainFile.GetText()?.ToString();
                    }
                }
            }

            var templateAssemblyMaps = GetMapsFromTemplateAssembly(session, templateAssemblyList);

            return MergeMaps(list, templateAssemblyMaps);
        }

        private static List<MapModel> GetMapsFromTemplateAssembly(RenderSession session, List<Assembly> templateAssemblyList)
        {
            return templateAssemblyList.SelectMany(templateAssembly =>
            {
                var mapJson = templateAssembly?.GetResourceString($"{IncrementalGenerator.AssemblySourceBase}.{IncrementalGenerator.MapName}");
                if (string.IsNullOrWhiteSpace(mapJson))
                    return new List<MapModel>();

                // ENC-01：资源在 AssemblyResourceExtension 中已按 UTF-8 解码，这里只剥 BOM，
                // 不再做 Encoding.Default 往返转码（旧实现结果依赖构建机区域设置，中文/英文系统行为不一致）
                var json = mapJson.TrimStart('\uFEFF'); // 剥 BOM（U+FEFF）

                List<MapModel> maps;
                try
                {
                    maps = JsonConvert.DeserializeObject<List<MapModel>>(json)?.Where(d => d.Enable).ToList() ?? new List<MapModel>();
                }
                catch (Exception e)
                {
                    session.Report(Diagnostic.Create(GeneratorDiagnostics.MapConfigError, Location.None, $"模板程序集 {templateAssembly.GetName().Name} 的 Map.json 解析失败：{e.Message}"));
                    return new List<MapModel>();
                }

                foreach (var model in maps)
                {
                    if (model.Templates != null && model.Templates.Any())
                    {
                        foreach (var template in model.Templates)
                        {
                            var path = $"{IncrementalGenerator.AssemblySourceBase}.{template}";
                            var file = templateAssembly.GetResourceString(path);
                            if (string.IsNullOrWhiteSpace(file))
                            {
                                session.Report(Diagnostic.Create(GeneratorDiagnostics.TemplateNotFound, Location.None, template));
                                continue;
                            }

                            model.TemplateDictionary ??= new ConcurrentDictionary<string, string>();
                            model.TemplateDictionary.TryAdd(template, file);
                            model.Path = path;
                        }
                    }

                    if (!string.IsNullOrWhiteSpace(model.MainTemplate))
                    {
                        model.MainTemplateString =
                            templateAssembly.GetResourceString(
                                $"{IncrementalGenerator.AssemblySourceBase}.{model.MainTemplate}");
                    }
                }

                return maps;
            }).ToList();
        }

        private static List<MapModel> MergeMaps(List<MapModel> source, List<MapModel> templateAssemblyMaps)
        {
            source ??= new List<MapModel>();

            if (templateAssemblyMaps == null || !templateAssemblyMaps.Any())
                return source;

            foreach (var map in templateAssemblyMaps)
            {
                if (source.Any(d => d.Code == map.Code))
                    continue;

                source.Add(map);
            }

            return source.Where(d => d.Enable).ToList();
        }

        private static void RenderTemplate(RenderSession session, AssemblyMetaData meta, List<MapModel> mapModels)
        {
            foreach (var mapModel in mapModels.OrderBy(d => d.Order))
            {
                if (string.IsNullOrWhiteSpace(mapModel.MainTemplateString))
                    continue;

                try
                {
                    var scriptObject1 = new FilterFunctions();
                    scriptObject1.Import(new { meta_data = meta, template_data = mapModel });

                    var scContext = FilterFunctions.CreateTemplateContext();
                    scContext.PushGlobal(scriptObject1);

                    // PERF-01：主模板解析结果按文本缓存，同一次执行内不重复 Parse
                    session.GetOrParseTemplate(mapModel.MainTemplateString).Render(scContext);
                }
                catch (Exception e)
                {
                    // 单个 Map 渲染失败只跳过自身并报 SGT003，不再中断后续 Map（旧实现整体中断且静默）
                    var templateName = !string.IsNullOrWhiteSpace(mapModel.MainTemplate) ? mapModel.MainTemplate
                        : !string.IsNullOrWhiteSpace(mapModel.Code) ? mapModel.Code
                        : mapModel.Name;
                    session.Report(Diagnostic.Create(GeneratorDiagnostics.TemplateRenderError, Location.None, templateName, e.Message));
                    session.AddError("2、渲染模板", e);
                }
            }
        }
    }
}
