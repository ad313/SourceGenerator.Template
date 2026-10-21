using Microsoft.CodeAnalysis;

namespace SourceGenerator.Template.Generators
{
    /// <summary>
    /// 生成器诊断定义（GEN-02）。
    /// 历史上所有失败只写进生成的 Error 注释文件，构建静默成功、代码悄悄不生成；
    /// 现在典型失败点统一通过这里的描述符 ReportDiagnostic，让问题在构建时可见。
    /// </summary>
    internal static class GeneratorDiagnostics
    {
        public const string Category = "SourceGenerator.Template";

        /// <summary>单个类型的元数据解析失败（只跳过该类型，不影响其他类型生成）</summary>
        public static readonly DiagnosticDescriptor MetadataError = new DiagnosticDescriptor(
            "SGT001",
            "元数据解析失败",
            "类型 '{0}' 的元数据解析失败，已跳过该类型：{1}",
            Category,
            DiagnosticSeverity.Warning,
            true);

        /// <summary>Map 中声明的模板文件未找到</summary>
        public static readonly DiagnosticDescriptor TemplateNotFound = new DiagnosticDescriptor(
            "SGT002",
            "模板未找到",
            "未找到模板文件 '{0}'，已跳过该模板",
            Category,
            DiagnosticSeverity.Warning,
            true);

        /// <summary>模板渲染失败</summary>
        public static readonly DiagnosticDescriptor TemplateRenderError = new DiagnosticDescriptor(
            "SGT003",
            "模板渲染失败",
            "模板 '{0}' 渲染失败：{1}",
            Category,
            DiagnosticSeverity.Warning,
            true);

        /// <summary>外部模板程序集加载失败</summary>
        public static readonly DiagnosticDescriptor TemplateAssemblyLoadError = new DiagnosticDescriptor(
            "SGT004",
            "模板程序集加载失败",
            "模板程序集 '{0}' 加载失败，已跳过：{1}",
            Category,
            DiagnosticSeverity.Warning,
            true);

        /// <summary>Map.json 配置错误（解析失败、Code 重复等）</summary>
        public static readonly DiagnosticDescriptor MapConfigError = new DiagnosticDescriptor(
            "SGT005",
            "Map 配置错误",
            "Map 配置错误：{0}",
            Category,
            DiagnosticSeverity.Warning,
            true);

        /// <summary>枚举成员值超出 Int32 范围</summary>
        public static readonly DiagnosticDescriptor EnumValueOutOfRange = new DiagnosticDescriptor(
            "SGT006",
            "枚举值超出范围",
            "枚举成员 '{0}.{1}' 的值超出 Int32 范围，已置空",
            Category,
            DiagnosticSeverity.Warning,
            true);

        /// <summary>特性匹配到多个候选类</summary>
        public static readonly DiagnosticDescriptor AttributeAmbiguous = new DiagnosticDescriptor(
            "SGT007",
            "特性匹配歧义",
            "特性 '{0}' 匹配到多个候选类，已取第一个匹配",
            Category,
            DiagnosticSeverity.Warning,
            true);

        /// <summary>生成文件名冲突，已自动改名</summary>
        public static readonly DiagnosticDescriptor HintNameConflict = new DiagnosticDescriptor(
            "SGT008",
            "生成文件名冲突",
            "生成文件 '{0}' 已存在，已自动改名为 '{1}'",
            Category,
            DiagnosticSeverity.Warning,
            true);
    }
}
