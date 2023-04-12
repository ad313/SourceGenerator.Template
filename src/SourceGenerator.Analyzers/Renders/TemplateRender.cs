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
using System.Text;

namespace SourceGenerator.Analyzers.Renders
{
    internal partial class TemplateRender
    {
        /// <summary>
        /// 构建扩展代码
        /// </summary>
        /// <param name="context"></param>
        /// <param name="additionalTexts"></param>
        /// <param name="meta"></param>
        public static void BuildExtend(SourceProductionContext context, ImmutableArray<AdditionalText> additionalTexts, AssemblyMetaData meta, StringBuilder sb)
        {
            var extendMapModels = GetExtendMapModels(additionalTexts);
            RenderExtendMain(context, meta, extendMapModels, sb);
        }

        private static List<ExtendTemplateModel> GetExtendMapModels(ImmutableArray<AdditionalText> additionalTexts)
        {
            var list = new List<ExtendTemplateModel>();
            var json = additionalTexts.FirstOrDefault(d => d.Path.Replace("/", "\\").EndsWith("\\Map.txt", StringComparison.OrdinalIgnoreCase))?.GetText()?.ToString();
            if (!string.IsNullOrWhiteSpace(json))
            {
                list = JsonConvert.DeserializeObject<List<ExtendTemplateModel>>(json);
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

            return list;
        }

        private static void RenderExtendMain(SourceProductionContext context, AssemblyMetaData meta, List<ExtendTemplateModel> extendMapModels, StringBuilder sb)
        {
            foreach (var extendMapModel in extendMapModels)
            {
                if (context.CancellationToken.IsCancellationRequested)
                    return;

                if (string.IsNullOrWhiteSpace(extendMapModel.MainTemplateString))
                    continue;

                var scriptObject1 = new FilterFunctions();
                scriptObject1.Import(new { meta_data = meta, template_data = extendMapModel });
                var scContext = new TemplateContext();
                scContext.PushGlobal(scriptObject1);

                var template = Template.Parse(extendMapModel.MainTemplateString);
                template.Render(scContext);

                extendMapModel.TemplateResult.Clear();
            }
        }
    }
}