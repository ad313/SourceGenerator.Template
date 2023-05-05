using Microsoft.CodeAnalysis;
using Newtonsoft.Json;
using Scriban;
using Scriban.Runtime;
using SourceGenerator.Analyzers.MetaData;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace SourceGenerator.Analyzers.Renders
{
    internal sealed partial class TemplateRender
    {
        private static readonly object Obj = new object();

        /// <summary>
        /// 执行渲染模板
        /// </summary>
        /// <param name="context"></param>
        /// <param name="additionalTexts"></param>
        /// <param name="meta"></param>
        public static void Build(SourceProductionContext context, ImmutableArray<AdditionalText> additionalTexts, AssemblyMetaData meta)
        {
            RenderTemplate(context, meta, GetMaps(additionalTexts));
        }

        private static List<MapModel> SetMaps(List<MapModel> maps)
        {
            var key = "MapsCache";
            var cache = AppDomain.CurrentDomain.GetData(key);
            var cacheData = cache == null ? new List<MapModel>() : (List<MapModel>)cache;

            if (maps == null || !maps.Any())
                return cacheData;

            lock (Obj)
            {
                foreach (var model in cacheData)
                {
                    if (maps.Any(d => d.Code == model.Code))
                        continue;

                    maps.Add(model);
                }

                AppDomain.CurrentDomain.SetData(key, maps);
            }

            return maps;
        }

        private static List<MapModel> GetMaps(ImmutableArray<AdditionalText> additionalTexts)
        {
            var list = new List<MapModel>();
            var json = additionalTexts.FirstOrDefault(d => d.Path.Replace("/", "\\").EndsWith($"\\{IncrementalGenerator.MapName}", StringComparison.OrdinalIgnoreCase))?.GetText()?.ToString();
            if (!string.IsNullOrWhiteSpace(json))
            {
                list = JsonConvert.DeserializeObject<List<MapModel>>(json);
            }

            foreach (var model in list)
            {
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

            return SetMaps(list);
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

                //mapModel.TemplateResult.Clear();
            }
        }
    }
}