using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SourceGenerator.Analyzers.Extend;
using SourceGenerator.Analyzers.MetaData;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace SourceGenerator.Analyzers
{
    /// <summary>
    /// Aop 语法接收器
    /// </summary>
    sealed class SyntaxReceiver : ISyntaxReceiver
    {
        /// <summary>
        /// 类列表
        /// </summary>
        private readonly List<ClassDeclarationSyntax> _classSyntaxList;
        /// <summary>
        /// 接口列表
        /// </summary>
        private readonly List<InterfaceDeclarationSyntax> _interfaceSyntaxList;

        public SyntaxReceiver(List<ClassDeclarationSyntax> classSyntaxList, List<InterfaceDeclarationSyntax> interfaceSyntaxList)
        {
            _classSyntaxList = classSyntaxList;
            _interfaceSyntaxList = interfaceSyntaxList;
        }

        /// <summary>
        /// 访问语法树 
        /// </summary>
        /// <param name="syntaxNode"></param>
        void ISyntaxReceiver.OnVisitSyntaxNode(SyntaxNode syntaxNode)
        {
            if (syntaxNode is InterfaceDeclarationSyntax interfaceSyntax)
            {
                this._interfaceSyntaxList.Add(interfaceSyntax);
            }

            if (syntaxNode is ClassDeclarationSyntax classSyntax)
            {
                this._classSyntaxList.Add(classSyntax);
            }
        }
        
        /// <summary>
        /// 获取所有接口和类
        /// </summary>
        /// <param name="compilation"></param>
        /// <returns></returns>
        public AssemblyMetaData GetMetaData(Compilation compilation)
        {
            var result = new AssemblyMetaData(new List<InterfaceMetaData>(), new List<ClassMetaData>());

            //处理接口
            foreach (var interfaceDeclaration in _interfaceSyntaxList)
            {
                var interfaceMetaData = GetInterfaceMetaData(interfaceDeclaration);
                var exists = result.InterfaceMetaDataList.FirstOrDefault(d => d.Equals(interfaceMetaData));
                if (exists != null)
                {
                    exists.Append(interfaceMetaData);
                }
                else
                {
                    result.InterfaceMetaDataList.Add(interfaceMetaData);
                }
            }

            foreach (var interfaceMetaData in result.InterfaceMetaDataList)
            {
                interfaceMetaData.BaseInterfaceMetaDataList = result.InterfaceMetaDataList.Where(d => interfaceMetaData.HasInterface(d.Key)).ToList();
            }
            
            //处理类
            foreach (var classDeclaration in _classSyntaxList)
            {
                var classMetaData = GetClassMetaData(classDeclaration);
                if (classMetaData == null)
                    continue;
                
                var exists = result.ClassMetaDataList.FirstOrDefault(d => d.Equals(classMetaData));
                if (exists != null)
                {
                    exists.Append(classMetaData);
                }
                else
                {
                    result.ClassMetaDataList.Add(classMetaData);
                }
            }

            foreach (var classMetaData in result.ClassMetaDataList)
            {
                classMetaData.BaseInterfaceMetaDataList = result.InterfaceMetaDataList.Where(d => classMetaData.HasInterface(d.Key)).ToList();
                classMetaData.BaseClassMetaDataList = result.ClassMetaDataList.Where(d => classMetaData.HasClass(d.Key)).ToList();
            }
            //Debugger.Launch();
            return result;
        }

        private InterfaceMetaData GetInterfaceMetaData(InterfaceDeclarationSyntax classDeclaration)
        {
            try
            {
                var namespaceName = classDeclaration.FindParent<NamespaceDeclarationSyntax>()?.Name.ToString();
                var className = classDeclaration.Identifier.Text;
                var properties = classDeclaration.DescendantNodes().OfType<PropertyDeclarationSyntax>().ToList();
                var methodSyntax = classDeclaration.DescendantNodes().OfType<MethodDeclarationSyntax>().ToList();

                //属性集合
                var props = properties.Select(d => new PropertyMetaData(d.Identifier.Text, d.GetAttributeMetaData(), GetPropertyDescription(d), d.Modifiers.ToString(), d.Type?.ToString())).ToList();
                //方法集合
                var methods = methodSyntax.Select(GetMethodMetaData).ToList();
                //实现的接口集合
                var interfaces = classDeclaration.BaseList?.ToString().Split(':').Last().Trim().Split(',').Where(d => d.Trim().Split('.').Last().StartsWith("I")).Select(d => d.Trim()).ToList() ?? new List<string>();
                //using 引用
                //特殊处理 class中嵌套class
                var parent = classDeclaration.Parent is ClassDeclarationSyntax
                    ? classDeclaration.Parent?.Parent?.Parent
                    : classDeclaration.Parent?.Parent;
                var usingDirectiveSyntax = parent == null ? new SyntaxList<UsingDirectiveSyntax>() : ((CompilationUnitSyntax)parent).Usings;
                var usingList = usingDirectiveSyntax.Select(d => d.ToString()).ToList();
                
                return new InterfaceMetaData(namespaceName,
                    className, 
                    classDeclaration.GetAttributeMetaData(),
                    props, 
                    methods,
                    interfaces, 
                    usingList,
                    classDeclaration.Modifiers.ToString(), null);
            }
            catch (Exception e)
            {
                throw new Exception($"class 报错：{classDeclaration.Identifier.Text}", e);
            }
        }

        private ClassMetaData GetClassMetaData(ClassDeclarationSyntax classDeclaration)
        {
            try
            {
                var namespaceName = classDeclaration.FindParent<NamespaceDeclarationSyntax>()?.Name.ToString();
                var className = classDeclaration.Identifier.Text;
                var properties = classDeclaration.DescendantNodes().OfType<PropertyDeclarationSyntax>().ToList();
                var methodSyntax = classDeclaration.DescendantNodes().OfType<MethodDeclarationSyntax>().ToList();

                //属性集合
                var props = properties.Select(d => new PropertyMetaData(d.Identifier.Text, d.GetAttributeMetaData(), GetPropertyDescription(d), d.Modifiers.ToString(), d.Type?.ToString())).ToList();
                //方法集合
                var methods = methodSyntax.Select(GetMethodMetaData).ToList();
                //实现的接口集合、继承的类
                var array = classDeclaration.BaseList?.ToString().Split(':').Last().Trim().Split(',').Select(d => d.Trim()).ToList() ?? new List<string>();
                var interfaces = array.Where(d => d.Split('.').Last().StartsWith("I")).ToList();
                var classes = array.Where(d => !d.Split('.').Last().StartsWith("I")).ToList();
                //using 引用
                //特殊处理 class中嵌套class
                var parent = classDeclaration.Parent is ClassDeclarationSyntax
                    ? classDeclaration.Parent?.Parent?.Parent
                    : classDeclaration.Parent?.Parent;
                var usingDirectiveSyntax = parent == null ? new SyntaxList<UsingDirectiveSyntax>() : ((CompilationUnitSyntax)parent).Usings;
                var usingList = usingDirectiveSyntax.Select(d => d.ToString()).ToList();

                //构造函数
                var constructorDictionary = new List<KeyValueModel>();
                foreach (var memberDeclarationSyntax in classDeclaration.Members)
                {
                    if (memberDeclarationSyntax.Kind().ToString() == "ConstructorDeclaration")
                    {
                        constructorDictionary = memberDeclarationSyntax.DescendantNodes().OfType<ParameterSyntax>().Select(d => new KeyValueModel(d.Type?.ToString(), d.Identifier.Text)).ToList();
                        break;
                    }
                }

                return new ClassMetaData(namespaceName, className, classDeclaration.GetAttributeMetaData(), props, methods, interfaces, classes, constructorDictionary, usingList, classDeclaration.Modifiers.ToString());
            }
            catch (Exception e)
            {
                throw new Exception($"class 报错：{classDeclaration.Identifier.Text}", e);
            }
        }

        private MethodMetaData GetMethodMetaData(MethodDeclarationSyntax methodDeclarationSyntax)
        {
            var param = new List<KeyValueModel>();
            var properties = methodDeclarationSyntax.DescendantNodes().OfType<ParameterListSyntax>().FirstOrDefault()?.DescendantNodes().OfType<ParameterSyntax>().ToList() ?? new List<ParameterSyntax>();
            foreach (var parameterSyntax in properties)
            {
                var type = parameterSyntax?.Type?.ToString();
                var name = parameterSyntax?.Identifier.Text;
                if (type != null && name != null)
                    param.Add(new KeyValueModel(type, name));
            }

            var returnValue = methodDeclarationSyntax.ReturnType.ToString();

            return new MethodMetaData(methodDeclarationSyntax.Identifier.Text,
                methodDeclarationSyntax.GetAttributeMetaData(), returnValue, param, methodDeclarationSyntax.Modifiers.ToString(), methodDeclarationSyntax.Modifiers.ToString(),null);
        }

        /// <summary>
        /// 获取属性上的注释
        /// </summary>
        /// <param name="propertyDeclarationSyntax"></param>
        /// <returns></returns>
        private List<string> GetPropertyDescription(PropertyDeclarationSyntax propertyDeclarationSyntax)
        {
            return propertyDeclarationSyntax.DescendantTokens().OfType<SyntaxToken>()
                .Where(t => t.HasLeadingTrivia && t.LeadingTrivia.Any(l => l != null && l.Kind() == SyntaxKind.SingleLineCommentTrivia))
                .SelectMany(t => t.LeadingTrivia.Where(l => l != null && l.Kind() == SyntaxKind.SingleLineCommentTrivia))
                .Select(t => t.ToString())
                .ToList();
        }
    }
}