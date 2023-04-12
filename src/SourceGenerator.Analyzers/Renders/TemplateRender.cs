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
                //list = JsonSerializer.ToObject<List<ExtendTemplateModel>>(json);
                list =JsonConvert.DeserializeObject<List<ExtendTemplateModel>>(json);
            }

            foreach (var model in list)
            {
                if (model.Templates.Any())
                {
                    foreach (var template in model.Templates)
                    {
                        var file = additionalTexts.FirstOrDefault(d => d.Path.Replace("/","\\").EndsWith($"\\{template}", StringComparison.OrdinalIgnoreCase));
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

            var assembly = Assembly.GetExecutingAssembly();

            //默认附加 枚举业务扩展
            var bizDicModel = list.FirstOrDefault(d => d.Code.Equals("BizEnumExtendBuilder", StringComparison.OrdinalIgnoreCase));
            if (bizDicModel == null)
            {
                bizDicModel = new ExtendTemplateModel()
                {
                    Code = "BizEnumExtendBuilder",
                    Name = "枚举业务扩展",
                    MainTemplate = "BizEnumExtend_Main.txt",
                    Templates = new List<string>() { "BizEnumExtend.txt" },
                };

                foreach (var template in bizDicModel.Templates)
                {
                    var file = assembly.GetResourceString($"Templates.{template}");
                    bizDicModel.TemplateDictionary ??= new ConcurrentDictionary<string, string>();
                    bizDicModel.TemplateDictionary.TryAdd(template, file);
                }

                bizDicModel.MainTemplateString = assembly.GetResourceString($"Templates.{bizDicModel.MainTemplate}");

                list.Add(bizDicModel);
            }

            //默认附加 Quartz 扩展
            var qzModel = list.FirstOrDefault(d => d.Code.Equals("QuartzToolBuilder", StringComparison.OrdinalIgnoreCase));
            if (qzModel == null)
            {
                qzModel = new ExtendTemplateModel()
                {
                    Code = "QuartzToolBuilder",
                    Name = "Quartz 扩展",
                    MainTemplate = "QuartzToolExtend_Main.txt",
                    Templates = new List<string>() { "QuartzToolExtend.txt" },
                };

                foreach (var template in qzModel.Templates)
                {
                    var file = assembly.GetResourceString($"Templates.{template}");
                    qzModel.TemplateDictionary ??= new ConcurrentDictionary<string, string>();
                    qzModel.TemplateDictionary.TryAdd(template, file);
                }

                qzModel.MainTemplateString = assembly.GetResourceString($"Templates.{qzModel.MainTemplate}");

                list.Add(qzModel);
            }

            //默认附加 ClassToProto 扩展
            var classToProtoModel = list.FirstOrDefault(d => d.Code.Equals("ClassToProtoBuilder", StringComparison.OrdinalIgnoreCase));
            if (classToProtoModel == null)
            {
                classToProtoModel = new ExtendTemplateModel()
                {
                    Code = "ClassToProtoBuilder",
                    Name = "ClassToProto 扩展",
                    MainTemplate = "ClassToProtoExtend_Main.txt",
                    Templates = new List<string>() { "ClassToProtoExtend.txt" },
                };

                foreach (var template in classToProtoModel.Templates)
                {
                    var file = assembly.GetResourceString($"Templates.{template}");
                    classToProtoModel.TemplateDictionary ??= new ConcurrentDictionary<string, string>();
                    classToProtoModel.TemplateDictionary.TryAdd(template, file);
                }

                classToProtoModel.MainTemplateString = assembly.GetResourceString($"Templates.{classToProtoModel.MainTemplate}");

                list.Add(classToProtoModel);
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