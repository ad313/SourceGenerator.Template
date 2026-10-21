using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SourceGenerator.Template.MetaData;

namespace SourceGenerator.Template.Generators.Extensions
{
    public static class Extensions
    {
        public static bool HasAttribute(this List<AttributeMetaData> attributes, string name)
        {
            return attributes.Any(a => a.EqualsByName(name));
        }

        public static bool HasAttributeByFullName(this List<AttributeMetaData> attributes, string name)
        {
            return attributes.Any(a => a.EqualsByFullName(name));
        }

        public static T FindParent<T>(this SyntaxNode node) where T : class
        {
            var current = node;
            while (true)
            {
                current = current.Parent;
                if (current == null || current is T)
                    return current as T;
            }
        }

        /// <summary>
        /// 获取 Attribute 参数（纯语法版本，语义模型不可用时的回退实现；
        /// 生成器内部优先走 SyntaxReceiver 的语义模型路径）
        /// </summary>
        /// <param name="property"></param>
        /// <returns></returns>
        public static List<AttributeMetaData> GetAttributeMetaData(this MemberDeclarationSyntax property)
        {
            var list = new List<AttributeMetaData>();

            foreach (var attributeListSyntax in property.AttributeLists)
            {
                // BUG-06：一个方括号里写多个特性 [A, B] 时全部提取，旧实现只取第一个，B 静默丢失
                foreach (var attribute in attributeListSyntax.Attributes)
                {
                    var attributeMetaData = new AttributeMetaData(attribute.Name.ToString(), attribute.ToString());
                    FillAttributeParamsFromSyntax(attributeMetaData, attribute);
                    list.Add(attributeMetaData);
                }
            }

            return list;
        }

        /// <summary>
        /// 纯语法填充特性参数：
        /// 命名参数以参数名为 Key；位置参数以序号为 Key；
        /// 含 '=' 的字符串值不再被 Split('=') 截断（BUG-06）
        /// </summary>
        internal static void FillAttributeParamsFromSyntax(AttributeMetaData meta, AttributeSyntax attributeSyntax)
        {
            if (attributeSyntax?.ArgumentList == null)
                return;

            var index = 0;
            foreach (var argument in attributeSyntax.ArgumentList.Arguments)
            {
                string key;
                if (argument.NameEquals != null)
                {
                    key = argument.NameEquals.Name.Identifier.Text;
                }
                else if (argument.NameColon != null)
                {
                    key = argument.NameColon.Name.Identifier.Text;
                }
                else
                {
                    key = index.ToString(CultureInfo.InvariantCulture);
                }

                index++;
                meta.AddParam(key, ExpressionToString(argument.Expression));
            }
        }

        internal static string ExpressionToString(ExpressionSyntax expression)
        {
            if (expression is LiteralExpressionSyntax literal)
            {
                // 字符串/字符保留源码字面量形式（带引号），与语义模型路径的输出保持一致：
                // 模板会把参数值直接拼进生成代码；其余字面量取求值后的文本
                if (literal.IsKind(Microsoft.CodeAnalysis.CSharp.SyntaxKind.StringLiteralExpression) ||
                    literal.IsKind(Microsoft.CodeAnalysis.CSharp.SyntaxKind.CharacterLiteralExpression))
                    return literal.Token.Text;

                return literal.Token.ValueText;
            }

            return expression.ToString();
        }

        public static string GetStringParam(this List<AttributeMetaData> attributeMetaData, string attributeName, string key)
        {
            if (!attributeMetaData.Any()) return null;
            return attributeMetaData.FirstOrDefault(d => d.Name == attributeName)?.GetStringParam(key);
        }

        public static int? GetIntParam(this List<AttributeMetaData> attributeMetaData, string attributeName, string key)
        {
            if (!attributeMetaData.Any()) return null;
            return attributeMetaData.FirstOrDefault(d => d.Name == attributeName)?.GetIntParam(key);
        }

        public static bool? GetBoolParam(this List<AttributeMetaData> attributeMetaData, string attributeName, string key)
        {
            if (!attributeMetaData.Any()) return null;
            return attributeMetaData.FirstOrDefault(d => d.Name == attributeName)?.GetBoolParam(key);
        }

        public static bool HasIgnore(this List<AttributeMetaData> attributeMetaDatas, string ignoreAttribute)
        {
            return attributeMetaDatas.Any(d => d.Name == ignoreAttribute || d.Name + "Attribute" == ignoreAttribute);
        }

        public static bool HasIgnore(this ClassMetaData classMetaData, string ignoreAttribute)
        {
            return classMetaData.AttributeMetaDataList.HasIgnore(ignoreAttribute);
        }

        public static bool HasIgnore(this InterfaceMetaData interfaceMetaData, string ignoreAttribute)
        {
            return interfaceMetaData.AttributeMetaDataList.HasIgnore(ignoreAttribute);
        }

        public static AttributeMetaData GetAopAttribute(this List<AttributeMetaData> attributeMetaDatas, List<string> aopAttributeList)
        {
            return attributeMetaDatas.FirstOrDefault(d => aopAttributeList.Contains(d.Name) || aopAttributeList.Contains(d.Name + "Attribute"));
        }

        public static List<AttributeMetaData> GetAopAttributes(this List<AttributeMetaData> attributeMetaDatas, List<string> aopAttributeList)
        {
            return attributeMetaDatas.Where(d => aopAttributeList.Contains(d.Name.Replace("Attribute", "") + "Attribute")).ToList();
        }
    }
}
