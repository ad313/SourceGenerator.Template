using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using Newtonsoft.Json.Linq;
using SourceGenerator.Template.Generators;
using Xunit;

namespace SourceGenerator.Template.Tests
{
    /// <summary>
    /// 深度分析报告关键修复路径的回归测试：
    /// BUG-01/02（枚举语义解析、单项失败不毒化）、BUG-03（hintName 冲突消歧）、
    /// BUG-04（record 元数据）、GEN-02（单个模板失败隔离）、DET-01（两次生成逐字节一致）
    /// </summary>
    public class GeneratorTests
    {
        private const string SimpleMap = @"[
  { ""Code"": ""Test"", ""Name"": ""Test"", ""MainTemplate"": ""Main.txt"", ""Templates"": [""Sub.txt""], ""Enable"": true }
]";

        private sealed class TestAdditionalText : AdditionalText
        {
            private readonly SourceText _text;

            public TestAdditionalText(string path, string content)
            {
                Path = path;
                _text = SourceText.From(content, Encoding.UTF8);
            }

            public override string Path { get; }

            public override SourceText GetText(CancellationToken cancellationToken = default) => _text;
        }

        private sealed class RunOutput
        {
            public ImmutableArray<Diagnostic> Diagnostics;
            public List<(string HintName, string Text)> Sources;
            public JObject Meta;

            public bool HasDiagnostic(string id) => Diagnostics.Any(d => d.Id == id);
        }

        private static RunOutput RunGenerator(string source, params (string Name, string Content)[] additionalFiles)
        {
            var tree = CSharpSyntaxTree.ParseText(source);
            var references = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES"))
                .Split(Path.PathSeparator)
                .Select(p => (MetadataReference)MetadataReference.CreateFromFile(p))
                .ToList();
            var compilation = CSharpCompilation.Create(
                "TestAssembly",
                new[] { tree },
                references,
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

            var texts = additionalFiles
                .Select(f => (AdditionalText)new TestAdditionalText("C:/test/" + f.Name, f.Content))
                .ToImmutableArray();

            var driver = CSharpGeneratorDriver.Create(
                new ISourceGenerator[] { new IncrementalGenerator().AsSourceGenerator() },
                additionalTexts: texts);

            var result = driver.RunGenerators(compilation).GetRunResult().Results.Single();
            var sources = result.GeneratedSources
                .Select(s => (s.HintName, Text: s.SourceText.ToString()))
                .ToList();

            var metaText = sources.First(s => s.HintName.StartsWith("MetaJson")).Text;
            // MetaJson 生成文件以 // 开头（注释包裹的 JSON）
            var meta = JObject.Parse(metaText.Substring(2));

            return new RunOutput { Diagnostics = result.Diagnostics, Sources = sources, Meta = meta };
        }

        [Fact]
        public void EnumValues_VariousLiteralForms_AreParsedFromSemantics()
        {
            var output = RunGenerator(@"
namespace TestNs
{
    public enum Codes
    {
        A = 0x1F,
        B = 1 << 4,
        C = B,
        D = 0b101,
        E
    }
}");

            Assert.False(output.HasDiagnostic("SGT001"));
            Assert.False(output.HasDiagnostic("SGT006"));

            var members = output.Meta["enum_meta_data_list"][0]["member_meta_list"];
            var values = members.ToDictionary(m => (string)m["name"], m => (int?)m["value"]);
            Assert.Equal(31, values["A"]);
            Assert.Equal(16, values["B"]);
            Assert.Equal(16, values["C"]);
            Assert.Equal(5, values["D"]);
            Assert.Equal(6, values["E"]);
        }

        [Fact]
        public void EnumValue_OutOfInt32Range_OnlyThatMemberIsSkipped()
        {
            var output = RunGenerator(@"
namespace TestNs
{
    public enum Big : long
    {
        X = 5000000000,
        Y = 3
    }

    public class Holder
    {
        public int P { get; set; }
    }
}");

            Assert.True(output.HasDiagnostic("SGT006"));
            Assert.False(output.HasDiagnostic("SGT001"));

            var members = output.Meta["enum_meta_data_list"][0]["member_meta_list"];
            Assert.Null((int?)members[0]["value"]);
            Assert.Equal(3, (int?)members[1]["value"]);

            // 同程序集的其他类型不受影响
            var classes = output.Meta["class_meta_data_list"];
            Assert.Contains(classes, c => (string)c["name"] == "Holder");
        }

        [Fact]
        public void AmbiguousAttributeMatch_ReportsDiagnosticWithoutBreaking()
        {
            var output = RunGenerator(@"
using NsA;
using NsB;

namespace NsA
{
    public class TagAttribute : System.Attribute { }
}

namespace NsB
{
    public class TagAttribute : System.Attribute { }
}

namespace App
{
    [Tag]
    public class Widget
    {
        public int P { get; set; }
    }
}");

            Assert.True(output.HasDiagnostic("SGT007"));
            Assert.False(output.HasDiagnostic("SGT001"));
            var classes = output.Meta["class_meta_data_list"];
            Assert.Contains(classes, c => (string)c["name"] == "Widget");
        }

        [Fact]
        public void SameNameClassesInDifferentNamespaces_BothRenderWithDistinctHintNames()
        {
            var output = RunGenerator(@"
public class MarkerAttribute : System.Attribute { }

namespace Ns1
{
    [Marker]
    public class Foo
    {
        public int A { get; set; }
    }
}

namespace Ns2
{
    [Marker]
    public class Foo
    {
        public int B { get; set; }
    }
}",
                ("Map.json", SimpleMap),
                ("Main.txt", "{{ for class in meta_data.class_meta_data_list }}{{ render class template_data 'Sub.txt' '' }}{{ end }}"),
                ("Sub.txt", "// {{ namespace }}.{{ name }}"));

            Assert.True(output.HasDiagnostic("SGT008"));

            var rendered = output.Sources.Where(s => s.Text.StartsWith("// Ns")).ToList();
            Assert.Equal(2, rendered.Count);
            Assert.Contains(rendered, s => s.Text.Contains("Ns1.Foo"));
            Assert.Contains(rendered, s => s.Text.Contains("Ns2.Foo"));
            Assert.Equal(2, rendered.Select(s => s.HintName).Distinct().Count());
        }

        [Fact]
        public void Generation_IsDeterministic_AcrossTwoRuns()
        {
            const string source = @"
public class MarkerAttribute : System.Attribute { }

namespace Ns1
{
    public enum Small
    {
        One = 1,
        Two = 2
    }

    [Marker]
    public class Foo
    {
        public int A { get; set; }
    }
}";
            var files = new (string, string)[]
            {
                ("Map.json", SimpleMap),
                ("Main.txt", "{{ for class in meta_data.class_meta_data_list }}{{ render class template_data 'Sub.txt' '' }}{{ end }}{{ json meta_data.enum_meta_data_list '' }}"),
                ("Sub.txt", "// {{ now() }} class {{ name }}")
            };

            var first = RunGenerator(source, files);
            var second = RunGenerator(source, files);

            Assert.Equal(
                first.Sources.Select(s => s.HintName + "\n" + s.Text),
                second.Sources.Select(s => s.HintName + "\n" + s.Text));
        }

        [Fact]
        public void Records_AppearInMetadataWithPrimaryConstructorProperties()
        {
            var output = RunGenerator(@"
public record Person(string Name, int Age);

public record struct Point(int X, int Y);
");

            Assert.False(output.HasDiagnostic("SGT001"));

            var person = output.Meta["class_meta_data_list"].Single(c => (string)c["name"] == "Person");
            var personProps = person["property_meta_data_list"].Select(p => (string)p["name"]).ToList();
            Assert.Contains("Name", personProps);
            Assert.Contains("Age", personProps);

            var point = output.Meta["struct_meta_data_list"].Single(c => (string)c["name"] == "Point");
            var pointProps = point["property_meta_data_list"].Select(p => (string)p["name"]).ToList();
            Assert.Contains("X", pointProps);
            Assert.Contains("Y", pointProps);
        }

        [Fact]
        public void FailingTemplate_DoesNotPreventOtherMapsFromRendering()
        {
            const string twoMaps = @"[
  { ""Code"": ""Bad"", ""Name"": ""Bad"", ""MainTemplate"": ""Bad.txt"", ""Templates"": [], ""Enable"": true, ""Order"": 1 },
  { ""Code"": ""Good"", ""Name"": ""Good"", ""MainTemplate"": ""Good.txt"", ""Templates"": [""Sub.txt""], ""Enable"": true, ""Order"": 2 }
]";

            var output = RunGenerator(@"
public class MarkerAttribute : System.Attribute { }

namespace Ns1
{
    [Marker]
    public class Foo
    {
        public int A { get; set; }
    }
}",
                ("Map.json", twoMaps),
                ("Bad.txt", "{{ this_function_does_not_exist() }}"),
                ("Good.txt", "{{ for class in meta_data.class_meta_data_list }}{{ render class template_data 'Sub.txt' '' }}{{ end }}"),
                ("Sub.txt", "// good {{ name }}"));

            Assert.True(output.HasDiagnostic("SGT003"));
            Assert.Contains(output.Sources, s => s.Text.Contains("good Foo"));
        }
    }
}
