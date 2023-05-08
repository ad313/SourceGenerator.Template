using System.Collections.Concurrent;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace SourceGenerator.Analyzers.Renders
{
    /// <summary>
    /// 模板数据模型
    /// </summary>
    public class MapModel
    {
        /// <summary>
        /// 模板名称
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// 模板编码
        /// </summary>
        public string Code { get; set; }
        /// <summary>
        /// 是否有效
        /// </summary>
        public bool Enable { get; set; } = true;
        /// <summary>
        /// 主模板
        /// </summary>
        public string MainTemplate { get; set; }
        /// <summary>
        /// 其他模板
        /// </summary>
        public List<string> Templates { get; set; }
        /// <summary>
        /// 模板内容
        /// </summary>
        [JsonIgnore]
        public ConcurrentDictionary<string, string> TemplateDictionary { get; set; } = new ConcurrentDictionary<string, string>();
        /// <summary>
        /// 主模板内容
        /// </summary>
        [JsonIgnore]
        public string MainTemplateString { get; set; }
        /// <summary>
        /// 模板文件路径
        /// </summary>
        public string Path { get; set; }

        /// <summary>
        /// 获取模板内容
        /// </summary>
        /// <param name="templateName"></param>
        /// <returns></returns>
        public string GetTemplate(string templateName)
        {
            return TemplateDictionary.TryGetValue(templateName, out string v) ? v : string.Empty;
        }
    }
}
