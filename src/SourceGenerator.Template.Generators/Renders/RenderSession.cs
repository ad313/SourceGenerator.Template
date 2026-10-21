using Microsoft.CodeAnalysis;
using Scriban;
using System;
using System.Collections.Generic;
using System.Text;

namespace SourceGenerator.Template.Generators.Renders
{
    /// <summary>
    /// 单次生成执行的会话状态（GEN-01）。
    /// 旧实现把 SourceProductionContext 放在静态全局字段、错误/耗时放在生成器实例的
    /// StringBuilder 里永不清空，导致跨编译累积与多项目并行构建串扰；
    /// 现在所有可变状态都收敛到本对象：每次执行新建一个，执行结束即释放，生成器本身无状态。
    /// </summary>
    internal sealed class RenderSession
    {
        private readonly SourceProductionContext _context;
        private readonly HashSet<string> _usedHintNames = new HashSet<string>(StringComparer.Ordinal);
        private readonly Dictionary<string, Scriban.Template> _templateCache = new Dictionary<string, Scriban.Template>(StringComparer.Ordinal);
        private readonly StringBuilder _errorBuilder = new StringBuilder();

        public RenderSession(SourceProductionContext context)
        {
            _context = context;
        }

        /// <summary>
        /// 收集到的错误内容（确定性：不含时间戳，DET-01），由调用方统一输出为 Error.g.cs
        /// </summary>
        public string ErrorContent => _errorBuilder.ToString();

        public void Report(Diagnostic diagnostic)
        {
            _context.ReportDiagnostic(diagnostic);
        }

        /// <summary>
        /// 记录错误详情到 Error 输出（保留旧 Error 文件的排障价值，但去掉时间戳保证确定性）
        /// </summary>
        public void AddError(string stage, Exception e)
        {
            _errorBuilder.AppendLine("/*");
            _errorBuilder.AppendLine();
            _errorBuilder.AppendLine($" {stage} 异常");
            _errorBuilder.AppendLine($" Message：{e.Message}");
            _errorBuilder.AppendLine($" InnerException：{e.InnerException?.Message}");
            _errorBuilder.Append($" StackTrace：{e.StackTrace}");
            _errorBuilder.AppendLine();
            _errorBuilder.Append($" InnerException.StackTrace：{e.InnerException?.StackTrace}");
            _errorBuilder.AppendLine();
            _errorBuilder.AppendLine();
            _errorBuilder.AppendLine("*/");
        }

        /// <summary>
        /// 添加生成的源码并做 hintName 去重（BUG-03）：
        /// 文件名冲突时先追加限定名（一般是类型全限定 Key），仍冲突再加确定性序号，并报 SGT008；
        /// 旧实现同名直接抛 ArgumentException，中断后续全部模板
        /// </summary>
        /// <param name="hintName">期望的文件名</param>
        /// <param name="content">源码内容</param>
        /// <param name="qualifier">冲突时的限定名（如类型的 Namespace.Name）</param>
        /// <returns>实际使用的文件名</returns>
        public string AddSource(string hintName, string content, string qualifier = null)
        {
            var candidate = SanitizeHintName(hintName);
            var finalName = candidate;

            if (_usedHintNames.Contains(finalName) && !string.IsNullOrWhiteSpace(qualifier))
            {
                var withQualifier = candidate + "_" + SanitizeHintName(qualifier);
                if (!_usedHintNames.Contains(withQualifier))
                    finalName = withQualifier;
            }

            if (_usedHintNames.Contains(finalName))
            {
                var i = 2;
                string numbered;
                do
                {
                    numbered = candidate + "_" + i;
                    i++;
                } while (_usedHintNames.Contains(numbered));

                finalName = numbered;
            }

            _usedHintNames.Add(finalName);

            if (!string.Equals(finalName, candidate, StringComparison.Ordinal))
            {
                Report(Diagnostic.Create(GeneratorDiagnostics.HintNameConflict, Location.None, candidate, finalName));
            }

            _context.AddSource(finalName, content);
            return finalName;
        }

        /// <summary>
        /// 按模板文本缓存解析结果（PERF-01：旧实现每次 render 都重新 Parse 同一模板，
        /// 一个模板渲染 N 个类型就解析 N 次）；解析失败抛出带 Scriban 错误信息的异常，由调用方转诊断
        /// </summary>
        public Scriban.Template GetOrParseTemplate(string templateText)
        {
            if (_templateCache.TryGetValue(templateText, out var cached))
                return cached;

            var template = Scriban.Template.Parse(templateText);
            if (template.HasErrors)
            {
                throw new InvalidOperationException("模板解析失败：" + string.Join("; ", template.Messages));
            }

            _templateCache[templateText] = template;
            return template;
        }

        private static string SanitizeHintName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return "Generated";

            var sb = new StringBuilder(name.Length);
            foreach (var ch in name)
            {
                sb.Append(char.IsLetterOrDigit(ch) || ch == '_' || ch == '.' || ch == '-' ? ch : '_');
            }

            return sb.ToString();
        }
    }
}
