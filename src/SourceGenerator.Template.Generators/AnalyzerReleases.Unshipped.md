; Analyzer release tracking - unshipped rules
; https://github.com/dotnet/roslyn-analyzers/blob/main/src/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md

### New Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|-------
SGT001 | SourceGenerator.Template | Warning | 元数据解析失败，跳过单个类型
SGT002 | SourceGenerator.Template | Warning | Map 中声明的模板文件未找到
SGT003 | SourceGenerator.Template | Warning | 模板渲染失败
SGT004 | SourceGenerator.Template | Warning | 外部模板程序集加载失败，已跳过
SGT005 | SourceGenerator.Template | Warning | Map.json 配置错误
SGT006 | SourceGenerator.Template | Warning | 枚举成员值超出 Int32 范围，已置空
SGT007 | SourceGenerator.Template | Warning | 特性匹配到多个候选类，取第一个
SGT008 | SourceGenerator.Template | Warning | 生成文件名冲突，已自动改名
