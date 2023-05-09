using Microsoft.CodeAnalysis;
using Newtonsoft.Json;
using Scriban;
using Scriban.Runtime;
using SourceGenerator.Analyzers.Extend;
using SourceGenerator.Analyzers.MetaData;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Text;

namespace SourceGenerator.Analyzers.Renders
{
    internal sealed partial class TemplateRender
    {
        /// <summary>
        /// 执行渲染模板
        /// </summary>
        /// <param name="context">上下文</param>
        /// <param name="additionalTexts">当前程序集参与编译的分析器文件</param>
        /// <param name="meta">当前程序集元数据</param>
        /// <param name="compilation">提供额外的模板程序集</param>
        public static void Build(SourceProductionContext context, ImmutableArray<AdditionalText> additionalTexts, AssemblyMetaData meta, Compilation compilation)
        {
            //加载外部模板程序集
            var templateAssemblyDllList = compilation.References.Where(d => d.Display != null && d.Display.EndsWith(IncrementalGenerator.TemplateAssemblyName)).ToList();
            var templateAssemblyList = templateAssemblyDllList.Select(d => d.Display == null ? null : Assembly.LoadFile(d.Display)).Where(d => d != null).ToList();

            //获取模板
            var maps = GetMaps(additionalTexts, templateAssemblyList);

            context.AddSource("TemplateInfo", ToTemplateAssemblyStringBuilder(maps, templateAssemblyList).ToString());

            RenderTemplate(context, meta, maps);
        }

        private static List<MapModel> GetMaps(ImmutableArray<AdditionalText> additionalTexts, List<Assembly> templateAssemblyList)
        {
            var list = new List<MapModel>();
            var json = additionalTexts.FirstOrDefault(d => d.Path.Replace("/", "\\").EndsWith($"\\{IncrementalGenerator.MapName}", StringComparison.OrdinalIgnoreCase))?.GetText()?.ToString();
            if (!string.IsNullOrWhiteSpace(json))
            {
                list = JsonConvert.DeserializeObject<List<MapModel>>(json) ?? new List<MapModel>();
            }
            
            foreach (var model in list)
            {
                if (!model.Enable)
                    continue;

                if (model.Templates.Any())
                {
                    foreach (var template in model.Templates)
                    {
                        var file = additionalTexts.FirstOrDefault(d => d.Path.Replace("/", "\\").EndsWith($"\\{template}", StringComparison.OrdinalIgnoreCase));
                        if (file == null)
                            throw new ArgumentNullException($"未找到模板 {nameof(template)}");

                        model.TemplateDictionary ??= new ConcurrentDictionary<string, string>();
                        model.TemplateDictionary.TryAdd(template, file.GetText()?.ToString());
                        model.Path = file.Path;
                    }
                }

                if (!string.IsNullOrWhiteSpace(model.MainTemplate))
                {
                    model.MainTemplateString = additionalTexts.FirstOrDefault(d =>
                            d.Path.Replace("/", "\\").EndsWith($"\\{model.MainTemplate}", StringComparison.OrdinalIgnoreCase))?.GetText()
                        ?.ToString();
                }
            }

            var templateAssemblyMaps = GetMapsFromTemplateAssembly(templateAssemblyList);

            return MergeMaps(list, templateAssemblyMaps);
        }

        private static List<MapModel> GetMapsFromTemplateAssembly(List<Assembly> templateAssemblyList)
        {
            return templateAssemblyList.SelectMany(templateAssembly =>
            {
                var mapJson = templateAssembly?.GetResourceString($"{IncrementalGenerator.AssemblySourceBase}.{IncrementalGenerator.MapName}");
                if (string.IsNullOrWhiteSpace(mapJson))
                    return new List<MapModel>();

                //处理编码，防止反序列化失败
                var bytes = Encoding.Convert(Encoding.Default, Encoding.UTF8, Encoding.Default.GetBytes(mapJson));
                var json = Encoding.UTF8.GetString(bytes);
                if (json.StartsWith("?"))
                    json = json.Substring(1);

                var maps = JsonConvert.DeserializeObject<List<MapModel>>(json)?.Where(d => d.Enable).ToList() ?? new List<MapModel>();
                foreach (var model in maps)
                {
                    if (model.Templates.Any())
                    {
                        foreach (var template in model.Templates)
                        {
                            var path = $"{IncrementalGenerator.AssemblySourceBase}.{template}";
                            var file = templateAssembly.GetResourceString(path);
                            if (string.IsNullOrWhiteSpace(file))
                                throw new ArgumentNullException($"未找到模板 {nameof(template)}");

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

        private static void RenderTemplate(SourceProductionContext context, AssemblyMetaData meta, List<MapModel> mapModels)
        {
            foreach (var mapModel in mapModels)
            {
                if (context.CancellationToken.IsCancellationRequested)
                    return;

                if (string.IsNullOrWhiteSpace(mapModel.MainTemplateString))
                    continue;

                var scriptObject1 = new FilterFunctions();
                scriptObject1.Import(new { meta_data = meta, template_data = mapModel });

                var scContext = new TemplateContext();
                scContext.PushGlobal(scriptObject1);

                Template.Parse(mapModel.MainTemplateString).Render(scContext);
            }
        }
    }
}