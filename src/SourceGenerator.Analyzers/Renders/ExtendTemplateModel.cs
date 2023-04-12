using System.Collections.Concurrent;
using System.Collections.Generic;

namespace SourceGenerator.Analyzers.Renders
{
    /// <summary>
    /// 扩展模板数据模型
    /// </summary>
    public class ExtendTemplateModel
    {
        public string Name { get; set; }

        public string Code { get; set; }

        public string MainTemplate { get; set; }

        public List<string> Templates { get; set; }

        public ConcurrentDictionary<string, string> TemplateDictionary { get; set; } = new ConcurrentDictionary<string, string>();

        public ConcurrentDictionary<string, string> TemplateResult { get; set; } = new ConcurrentDictionary<string, string>();

        public string MainTemplateString { get; set; }

        public string Path { get; set; }

        public string GetTemplate(string templateName)
        {
            return TemplateDictionary.TryGetValue(templateName, out string v) ? v : string.Empty;
        }
    }
}
