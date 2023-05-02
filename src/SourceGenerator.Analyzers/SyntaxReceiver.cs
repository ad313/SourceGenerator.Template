using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SourceGenerator.Analyzers.Extend;
using SourceGenerator.Analyzers.MetaData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Xml.Linq;

namespace SourceGenerator.Analyzers
{
    /// <summary>
    /// 语法接收器
    /// </summary>
    sealed class SyntaxReceiver : ISyntaxReceiver
    {
        /// <summary>
        /// Struct
        /// </summary>
        private readonly List<StructDeclarationSyntax> _structSyntaxList;
        /// <summary>
        /// Class
        /// </summary>
        private readonly List<ClassDeclarationSyntax> _classSyntaxList;
        /// <summary>
        /// Interface
        /// </summary>
        private readonly List<InterfaceDeclarationSyntax> _interfaceSyntaxList;
        /// <summary>
        /// enum
        /// </summary>
        private readonly List<EnumDeclarationSyntax> _enumSyntaxList;
        /// <summary>
        /// Record
        /// </summary>
        private readonly List<RecordDeclarationSyntax> _recordSyntaxList;

        public SyntaxReceiver(List<ClassDeclarationSyntax> classSyntaxList, List<InterfaceDeclarationSyntax> interfaceSyntaxList)
        {
            _classSyntaxList = classSyntaxList;
            _interfaceSyntaxList = interfaceSyntaxList;
        }

        public SyntaxReceiver(Compilation compilation, CancellationToken token)
        {
            var syntaxNodes = compilation.SyntaxTrees.SelectMany(d => d.GetRoot(token).DescendantNodes()).ToList();
            _classSyntaxList = syntaxNodes.OfType<ClassDeclarationSyntax>().ToList();
            _structSyntaxList = syntaxNodes.OfType<StructDeclarationSyntax>().ToList();
            _interfaceSyntaxList = syntaxNodes.OfType<InterfaceDeclarationSyntax>().ToList();
            _enumSyntaxList = syntaxNodes.OfType<EnumDeclarationSyntax>().ToList();
            _recordSyntaxList = syntaxNodes.OfType<RecordDeclarationSyntax>().ToList();
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
        /// <returns></returns>
        public AssemblyMetaData GetMetaData()
        {
            var result = new AssemblyMetaData(new List<InterfaceMetaData>(), new List<ClassMetaData>(), new List<StructMetaData>(), new List<EnumMetaData>());

            #region 处理接口

            //Debugger.Launch();
            foreach (var declaration in _interfaceSyntaxList)
            {
                var metaData = GetMetaData<InterfaceDeclarationSyntax, InterfaceMetaData>(declaration);
                var exists = result.InterfaceMetaDataList.FirstOrDefault(d => d.Equals(metaData));
                if (exists != null)
                {
                    exists.MergePartial(metaData);
                }
                else
                {
                    result.InterfaceMetaDataList.Add(metaData);
                }
            }

            foreach (var metaData in result.InterfaceMetaDataList)
            {
                metaData.BaseInterfaceMetaDataList = result.InterfaceMetaDataList.Where(d => metaData.BaseExists(d.Key)).ToList();
            }

            #endregion
            
            #region 处理类

            foreach (var declaration in _classSyntaxList)
            {
                var metaData = GetMetaData<ClassDeclarationSyntax, ClassMetaData>(declaration);
                if (metaData == null)
                    continue;

                var exists = result.ClassMetaDataList.FirstOrDefault(d => d.Equals(metaData));
                if (exists != null)
                {
                    exists.MergePartial(metaData);
                }
                else
                {
                    result.ClassMetaDataList.Add(metaData);
                }
            }

            foreach (var metaData in result.ClassMetaDataList)
            {
                metaData.BaseInterfaceMetaDataList = result.InterfaceMetaDataList.Where(d => ((InterfaceMetaData)metaData).BaseExists(d.Key)).ToList();
                metaData.BaseClassMetaData = result.ClassMetaDataList.FirstOrDefault(d => metaData.BaseExists(d.Key));
            }

            #endregion

            #region 处理 Struct

            foreach (var declaration in _structSyntaxList)
            {
                var metaData = GetMetaData<StructDeclarationSyntax, StructMetaData>(declaration);
                if (metaData == null)
                    continue;

                var exists = result.StructMetaDataList.FirstOrDefault(d => d.Equals(metaData));
                if (exists != null)
                {
                    exists.MergePartial(metaData);
                }
                else
                {
                    result.StructMetaDataList.Add(metaData);
                }
            }

            foreach (var metaData in result.StructMetaDataList)
            {
                metaData.BaseInterfaceMetaDataList = result.InterfaceMetaDataList.Where(d => metaData.BaseExists(d.Key)).ToList();
            }

            #endregion

            #region 处理 enum
            
            foreach (var declaration in _enumSyntaxList)
            {
                var metaData = GetMetaData<EnumDeclarationSyntax, EnumMetaData>(declaration);
                if (metaData == null)
                    continue;

                result.EnumMetaDataList.Add(metaData);
            }
            
            #endregion

            return result.FormatData();
        }
        
        private TOut GetMetaData<TIn, TOut>(TIn declaration) where TIn : BaseTypeDeclarationSyntax where TOut : MetaDataBase
        {
            try
            {
                var namespaceName = declaration.FindParent<NamespaceDeclarationSyntax>()?.Name.ToString() ??
                                    declaration.FindParent<FileScopedNamespaceDeclarationSyntax>()?.Name.ToString();
                var className = declaration.Identifier.Text;
                var properties = declaration.DescendantNodes().OfType<PropertyDeclarationSyntax>().ToList();
                var methodSyntax = declaration.DescendantNodes().OfType<MethodDeclarationSyntax>().ToList();

                var enumMembers = new List<EnumMemberMetaData>();
                //属性集合
                var props = properties.Select(d => new PropertyMetaData(d.Identifier.Text, d.GetAttributeMetaData(), GetPropertyDescription(d), d.Modifiers.ToString(), d.Type?.ToString())).ToList();
                //方法集合
                var methods = methodSyntax.Select(GetMethodMetaData).ToList();
                //实现的接口集合、继承的类
                var array = declaration.BaseList?.ToString().Split(':').Last().Trim().Split(',').Select(d => d.Trim()).ToList() ?? new List<string>();
                var interfaces = array.Where(d => d.Split('.').Last().StartsWith("I")).ToList();
                var baseClass = array.FirstOrDefault(d => !d.Split('.').Last().StartsWith("I"));

                //using 引用
                //特殊处理 class中嵌套class
                var parent = declaration.Parent is ClassDeclarationSyntax
                    ? declaration.Parent?.Parent?.Parent
                    : declaration.Parent?.Parent;
                var usingDirectiveSyntax = parent == null ? new SyntaxList<UsingDirectiveSyntax>() : ((CompilationUnitSyntax)parent).Usings;
                var usingList = usingDirectiveSyntax.Select(d => d.ToString().Replace("using", "").Replace(";", "").Trim()).ToList();

                //构造函数
                var constructorDictionary = new List<KeyValueModel>();
                if (declaration is TypeDeclarationSyntax typeDeclarationSyntax)
                {
                    foreach (var memberDeclarationSyntax in typeDeclarationSyntax.Members)
                    {
                        if (memberDeclarationSyntax.Kind().ToString() == "ConstructorDeclaration")
                        {
                            constructorDictionary = memberDeclarationSyntax.DescendantNodes().OfType<ParameterSyntax>().Select(d => new KeyValueModel(d.Type?.ToString(), d.Identifier.Text)).ToList();
                            break;
                        }
                    }
                }
                else if (declaration is BaseTypeDeclarationSyntax baseTypeDeclarationSyntax)
                {
                    //枚举
                    var membersSyntax = declaration.DescendantNodes().OfType<EnumMemberDeclarationSyntax>().ToList();
                    enumMembers = membersSyntax.Select(d =>
                        new EnumMemberMetaData(baseTypeDeclarationSyntax.Identifier.Text, d.Identifier.Text,
                            string.IsNullOrWhiteSpace(d.EqualsValue?.Value.ToString()) ? null : Convert.ToInt32(d.EqualsValue.Value.ToString())
                            , d.GetAttributeMetaData())).ToList();
                }

                if (typeof(TOut) == typeof(StructMetaData))
                {
                    return new StructMetaData(namespaceName,
                        className,
                        declaration.GetAttributeMetaData(),
                        props,
                        methods,
                        interfaces,
                        constructorDictionary,
                        usingList,
                        declaration.Modifiers.ToString()) as TOut;
                }

                if (typeof(TOut) == typeof(ClassMetaData))
                {
                    return new ClassMetaData(namespaceName,
                        className,
                        declaration.GetAttributeMetaData(),
                        props,
                        methods,
                        interfaces,
                        baseClass,
                        constructorDictionary,
                        usingList,
                        declaration.Modifiers.ToString()) as TOut;
                }
                
                if (typeof(TOut) == typeof(InterfaceMetaData))
                {
                    return new InterfaceMetaData(namespaceName,
                        className,
                        declaration.GetAttributeMetaData(),
                        props,
                        methods,
                        interfaces,
                        usingList,
                        declaration.Modifiers.ToString(), null) as TOut;
                }

                if (typeof(TOut) == typeof(EnumMetaData))
                {
                    return new EnumMetaData(namespaceName,
                        className,
                        declaration.GetAttributeMetaData(),
                        enumMembers,
                        usingList,
                        declaration.Modifiers.ToString(), null) as TOut;
                }

                return default;
            }
            catch (Exception e)
            {
                throw new Exception($"class 报错：{declaration.Identifier.Text}", e);
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
                methodDeclarationSyntax.GetAttributeMetaData(), returnValue, param, methodDeclarationSyntax.Modifiers.ToString(), methodDeclarationSyntax.Modifiers.ToString(), null);
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