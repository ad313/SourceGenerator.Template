using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SourceGenerator.Template.MetaData;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using System.Threading;
using SourceGenerator.Template.Generators.Extensions;

namespace SourceGenerator.Template.Generators
{
    /// <summary>
    /// 语法扫描器：从编译中提取全部类型元数据。
    /// 健壮性原则（修复 BUG-01/BUG-02 一类“单点毒化全量”问题）：
    /// 单个类型的解析失败只跳过该类型，并把原因记入 <see cref="Diagnostics"/>，
    /// 由调用方统一报告，绝不让一个坏写法拖垮整个程序集的生成。
    /// </summary>
    sealed class SyntaxReceiver
    {
        private readonly Compilation _compilation;
        private readonly CancellationToken _token;
        private readonly Dictionary<SyntaxTree, SemanticModel> _modelCache = new Dictionary<SyntaxTree, SemanticModel>();
        private readonly List<Diagnostic> _diagnostics = new List<Diagnostic>();

        private readonly List<StructDeclarationSyntax> _structSyntaxList = new List<StructDeclarationSyntax>();
        private readonly List<ClassDeclarationSyntax> _classSyntaxList = new List<ClassDeclarationSyntax>();
        private readonly List<InterfaceDeclarationSyntax> _interfaceSyntaxList = new List<InterfaceDeclarationSyntax>();
        private readonly List<EnumDeclarationSyntax> _enumSyntaxList = new List<EnumDeclarationSyntax>();
        private readonly List<RecordDeclarationSyntax> _recordSyntaxList = new List<RecordDeclarationSyntax>();

        /// <summary>
        /// 扫描过程中收集的诊断（GEN-02），由 IncrementalGenerator 统一 ReportDiagnostic
        /// </summary>
        public IReadOnlyList<Diagnostic> Diagnostics => _diagnostics;

        public SyntaxReceiver(Compilation compilation, CancellationToken token)
        {
            _compilation = compilation;
            _token = token;

            foreach (var tree in compilation.SyntaxTrees)
            {
                // PERF-01：只收集类型声明节点本身，不再把整棵语法树的全部后代节点物化成大列表
                foreach (var node in tree.GetRoot(token).DescendantNodes())
                {
                    switch (node)
                    {
                        case ClassDeclarationSyntax classDeclaration:
                            _classSyntaxList.Add(classDeclaration);
                            break;
                        case StructDeclarationSyntax structDeclaration:
                            _structSyntaxList.Add(structDeclaration);
                            break;
                        case InterfaceDeclarationSyntax interfaceDeclaration:
                            _interfaceSyntaxList.Add(interfaceDeclaration);
                            break;
                        case EnumDeclarationSyntax enumDeclaration:
                            _enumSyntaxList.Add(enumDeclaration);
                            break;
                        case RecordDeclarationSyntax recordDeclaration:
                            _recordSyntaxList.Add(recordDeclaration);
                            break;
                    }
                }
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

            var interfaceIndex = new Dictionary<string, int>();
            foreach (var declaration in _interfaceSyntaxList)
            {
                _token.ThrowIfCancellationRequested();

                var metaData = TryGetMetaData<InterfaceDeclarationSyntax, InterfaceMetaData>(declaration);
                if (metaData == null)
                    continue;

                AddOrMerge(result.InterfaceMetaDataList, interfaceIndex, GetTypeIdentity(declaration), metaData, (exists, m) => exists.MergePartial(m));
            }

            foreach (var metaData in result.InterfaceMetaDataList)
            {
                metaData.BaseInterfaceMetaDataList = result.InterfaceMetaDataList.Where(d => metaData.BaseExists(d.Key)).ToList();
            }

            #endregion

            #region 处理类（含 record class，BUG-04）

            var classIndex = new Dictionary<string, int>();
            foreach (var declaration in _classSyntaxList)
            {
                _token.ThrowIfCancellationRequested();

                var metaData = TryGetMetaData<ClassDeclarationSyntax, ClassMetaData>(declaration);
                if (metaData == null)
                    continue;

                AddOrMerge(result.ClassMetaDataList, classIndex, GetTypeIdentity(declaration), metaData, (exists, m) => exists.MergePartial(m));
            }

            foreach (var declaration in _recordSyntaxList.Where(d => !d.ClassOrStructKeyword.IsKind(SyntaxKind.StructKeyword)))
            {
                _token.ThrowIfCancellationRequested();

                var metaData = TryGetRecordMetaData<ClassMetaData>(declaration);
                if (metaData == null)
                    continue;

                AddOrMerge(result.ClassMetaDataList, classIndex, GetTypeIdentity(declaration), metaData, (exists, m) => exists.MergePartial(m));
            }

            foreach (var metaData in result.ClassMetaDataList.Where(d => !string.IsNullOrWhiteSpace(d.BaseClass) || d.BaseInterfaceList?.Count > 0))
            {
                metaData.BaseInterfaceMetaDataList = result.InterfaceMetaDataList.Where(d => metaData.ExistsInterface(d.Key, metaData.BaseInterfaceList)).ToList();
                metaData.BaseClassMetaData = result.ClassMetaDataList.FirstOrDefault(d => metaData.BaseExists(d.Key));
            }

            #endregion

            #region 处理 Struct（含 record struct，BUG-04）

            var structIndex = new Dictionary<string, int>();
            foreach (var declaration in _structSyntaxList)
            {
                _token.ThrowIfCancellationRequested();

                var metaData = TryGetMetaData<StructDeclarationSyntax, StructMetaData>(declaration);
                if (metaData == null)
                    continue;

                AddOrMerge(result.StructMetaDataList, structIndex, GetTypeIdentity(declaration), metaData, (exists, m) => exists.MergePartial(m));
            }

            foreach (var declaration in _recordSyntaxList.Where(d => d.ClassOrStructKeyword.IsKind(SyntaxKind.StructKeyword)))
            {
                _token.ThrowIfCancellationRequested();

                var metaData = TryGetRecordMetaData<StructMetaData>(declaration);
                if (metaData == null)
                    continue;

                AddOrMerge(result.StructMetaDataList, structIndex, GetTypeIdentity(declaration), metaData, (exists, m) => exists.MergePartial(m));
            }

            foreach (var metaData in result.StructMetaDataList)
            {
                metaData.BaseInterfaceMetaDataList = result.InterfaceMetaDataList.Where(d => metaData.BaseExists(d.Key)).ToList();
            }

            #endregion

            #region 处理 enum

            foreach (var declaration in _enumSyntaxList)
            {
                _token.ThrowIfCancellationRequested();

                var metaData = TryGetMetaData<EnumDeclarationSyntax, EnumMetaData>(declaration);
                if (metaData == null)
                    continue;

                result.EnumMetaDataList.Add(metaData);
            }

            #endregion

            AssemblyMetaData data;
            try
            {
                data = result.FormatData();
            }
            catch (Exception e)
            {
                // 兜底：格式化阶段异常也不再让全量生成失败
                _diagnostics.Add(Diagnostic.Create(GeneratorDiagnostics.MetadataError, Location.None, "(Assembly)", e.Message));
                data = result;
            }

            // BUG-02：特性多候选匹配已改为不抛异常，这里把歧义项转成诊断上报
            foreach (var name in data.AmbiguousAttributeMatches)
            {
                _diagnostics.Add(Diagnostic.Create(GeneratorDiagnostics.AttributeAmbiguous, Location.None, name));
            }

            return data;
        }

        /// <summary>
        /// 分部类型合并：按类型身份（命名空间 + 包含类型链 + 泛型元数）定位已有元数据（BUG-05），
        /// 不再只靠 Namespace.Name —— 嵌套同名类型、泛型/非泛型同名类型不会被错误合并
        /// </summary>
        private static void AddOrMerge<T>(List<T> list, Dictionary<string, int> index, string identity, T metaData, Action<T, T> merge) where T : class
        {
            if (identity != null && index.TryGetValue(identity, out var i))
            {
                merge(list[i], metaData);
                return;
            }

            if (identity != null)
                index[identity] = list.Count;

            list.Add(metaData);
        }

        private TOut TryGetMetaData<TIn, TOut>(TIn declaration)
            where TIn : BaseTypeDeclarationSyntax
            where TOut : MetaDataBase
        {
            try
            {
                return GetMetaData<TIn, TOut>(declaration);
            }
            catch (Exception e)
            {
                // 单个类型失败只跳过自身（BUG-01 的传播链在此切断）
                _diagnostics.Add(Diagnostic.Create(GeneratorDiagnostics.MetadataError,
                    declaration.Identifier.GetLocation(), declaration.Identifier.Text, e.Message));
                return null;
            }
        }

        private TOut TryGetRecordMetaData<TOut>(RecordDeclarationSyntax declaration) where TOut : MetaDataBase
        {
            try
            {
                return GetRecordMetaData<TOut>(declaration);
            }
            catch (Exception e)
            {
                _diagnostics.Add(Diagnostic.Create(GeneratorDiagnostics.MetadataError,
                    declaration.Identifier.GetLocation(), declaration.Identifier.Text, e.Message));
                return null;
            }
        }

        private TOut GetMetaData<TIn, TOut>(TIn declaration) where TIn : BaseTypeDeclarationSyntax where TOut : MetaDataBase
        {
            var namespaceName = GetNamespace(declaration);
            var className = declaration.Identifier.Text;
            var usingList = GetUsingList(declaration);
            var attributeMetaData = GetAttributeMetaData(declaration);

            // BUG-05：只取类型的直接成员，不再 DescendantNodes 穿透嵌套类型把成员误归到外层
            var typeDeclaration = declaration as TypeDeclarationSyntax;
            var memberList = typeDeclaration?.Members ?? new SyntaxList<MemberDeclarationSyntax>();
            var properties = memberList.OfType<PropertyDeclarationSyntax>().ToList();
            var methodSyntax = memberList.OfType<MethodDeclarationSyntax>().ToList();

            //属性集合
            var props = properties.Select(d => new PropertyMetaData(d.Identifier.Text, GetAttributeMetaData(d), GetPropertyDescription(d), d.Modifiers.ToString(), d.Type?.ToString(), d.ToString())).ToList();
            //方法集合
            var methods = methodSyntax.Select(GetMethodMetaData).ToList();

            //实现的接口集合、继承的类
            GetBaseTypes(typeDeclaration, out var interfaces, out var baseClass, out var isAttribute);

            //构造函数
            var constructorDictionary = GetConstructorParams(typeDeclaration);

            if (typeof(TOut) == typeof(StructMetaData))
            {
                return new StructMetaData(namespaceName,
                    className,
                    attributeMetaData,
                    props,
                    methods,
                    interfaces,
                    constructorDictionary,
                    usingList,
                    declaration.Modifiers.ToString()) as TOut;
            }

            if (typeof(TOut) == typeof(ClassMetaData))
            {
                var classMetaData = new ClassMetaData(namespaceName,
                    className,
                    attributeMetaData,
                    props,
                    methods,
                    interfaces,
                    baseClass,
                    constructorDictionary,
                    usingList,
                    declaration.Modifiers.ToString());

                if (isAttribute.HasValue)
                    classMetaData.IsAttribute = isAttribute.Value;

                return classMetaData as TOut;
            }

            if (typeof(TOut) == typeof(InterfaceMetaData))
            {
                return new InterfaceMetaData(namespaceName,
                    className,
                    attributeMetaData,
                    props,
                    methods,
                    interfaces,
                    usingList,
                    declaration.Modifiers.ToString(), null, null) as TOut;
            }

            if (typeof(TOut) == typeof(EnumMetaData))
            {
                return new EnumMetaData(namespaceName,
                    className,
                    attributeMetaData,
                    GetEnumMembers(declaration as EnumDeclarationSyntax),
                    usingList,
                    declaration.Modifiers.ToString(), null, declaration.ToString()) as TOut;
            }

            return default;
        }

        /// <summary>
        /// BUG-04：record / record struct 的元数据提取。
        /// record class 并入 ClassMetaData 通道，record struct 并入 StructMetaData 通道；
        /// 主构造函数参数合成为属性元数据（位置参数是 record 的主要成员来源）并记录为构造函数参数
        /// </summary>
        private TOut GetRecordMetaData<TOut>(RecordDeclarationSyntax declaration) where TOut : MetaDataBase
        {
            var namespaceName = GetNamespace(declaration);
            var name = declaration.Identifier.Text;
            var usingList = GetUsingList(declaration);
            var attributeMetaData = GetAttributeMetaData(declaration);

            var props = new List<PropertyMetaData>();
            var constructorDictionary = new List<KeyValueModel>();
            if (declaration.ParameterList != null)
            {
                foreach (var parameter in declaration.ParameterList.Parameters)
                {
                    constructorDictionary.Add(new KeyValueModel(parameter.Type?.ToString(), parameter.Identifier.Text));
                    props.Add(new PropertyMetaData(parameter.Identifier.Text,
                        new List<AttributeMetaData>(),
                        new List<string>(),
                        "public",
                        parameter.Type?.ToString(),
                        parameter.ToString()));
                }
            }

            props.AddRange(declaration.Members.OfType<PropertyDeclarationSyntax>()
                .Select(d => new PropertyMetaData(d.Identifier.Text, GetAttributeMetaData(d), GetPropertyDescription(d), d.Modifiers.ToString(), d.Type?.ToString(), d.ToString())));
            var methods = declaration.Members.OfType<MethodDeclarationSyntax>().Select(GetMethodMetaData).ToList();

            GetBaseTypes(declaration, out var interfaces, out var baseClass, out var isAttribute);

            if (typeof(TOut) == typeof(ClassMetaData))
            {
                var classMetaData = new ClassMetaData(namespaceName,
                    name,
                    attributeMetaData,
                    props,
                    methods,
                    interfaces,
                    baseClass,
                    constructorDictionary,
                    usingList,
                    declaration.Modifiers.ToString());

                if (isAttribute.HasValue)
                    classMetaData.IsAttribute = isAttribute.Value;

                return classMetaData as TOut;
            }

            return new StructMetaData(namespaceName,
                name,
                attributeMetaData,
                props,
                methods,
                interfaces,
                constructorDictionary,
                usingList,
                declaration.Modifiers.ToString()) as TOut;
        }

        /// <summary>
        /// BUG-01：枚举成员值改用语义模型的常量值求值，
        /// 0x1F、0b101、1 &lt;&lt; 4、B = A、(int)X 等合法写法都不会再抛异常；
        /// 超出 Int32 范围的值置空并记录诊断，不影响其他成员与类型
        /// </summary>
        private List<EnumMemberMetaData> GetEnumMembers(EnumDeclarationSyntax declaration)
        {
            var result = new List<EnumMemberMetaData>();
            if (declaration == null)
                return result;

            var model = GetSemanticModel(declaration.SyntaxTree);
            foreach (var member in declaration.Members)
            {
                int? value = null;
                IFieldSymbol field = null;
                try
                {
                    field = model.GetDeclaredSymbol(member) as IFieldSymbol;
                }
                catch
                {
                    // 符号解析失败时降级为无值，不中断
                }

                if (field != null && field.HasConstantValue)
                {
                    try
                    {
                        var raw = Convert.ToInt64(field.ConstantValue, CultureInfo.InvariantCulture);
                        if (raw >= int.MinValue && raw <= int.MaxValue)
                        {
                            value = (int)raw;
                        }
                        else
                        {
                            _diagnostics.Add(Diagnostic.Create(GeneratorDiagnostics.EnumValueOutOfRange,
                                member.Identifier.GetLocation(), declaration.Identifier.Text, member.Identifier.Text));
                        }
                    }
                    catch (Exception)
                    {
                        _diagnostics.Add(Diagnostic.Create(GeneratorDiagnostics.EnumValueOutOfRange,
                            member.Identifier.GetLocation(), declaration.Identifier.Text, member.Identifier.Text));
                    }
                }

                result.Add(new EnumMemberMetaData(declaration.Identifier.Text, member.Identifier.Text, value,
                    GetAttributeMetaData(member), member.ToString()));
            }

            return result;
        }

        /// <summary>
        /// BUG-08：基类/接口优先走语义模型（INamedTypeSymbol.BaseType / Interfaces），
        /// 泛型基类不会被逗号拆碎、接口判定不再靠 “I 前缀” 猜测；
        /// 符号不可用时退回语法解析（按 BaseList.Types 逐项，同样不做字符串逗号切分）
        /// </summary>
        private void GetBaseTypes(TypeDeclarationSyntax declaration, out List<string> interfaces, out string baseClass, out bool? isAttribute)
        {
            interfaces = new List<string>();
            baseClass = null;
            isAttribute = null;
            if (declaration == null)
                return;

            INamedTypeSymbol symbol = null;
            try
            {
                symbol = GetSemanticModel(declaration.SyntaxTree).GetDeclaredSymbol(declaration) as INamedTypeSymbol;
            }
            catch
            {
                // 忽略，走语法回退
            }

            if (symbol != null)
            {
                interfaces = symbol.Interfaces.Select(GetTypeKeyName).ToList();

                if (declaration is ClassDeclarationSyntax || declaration is RecordDeclarationSyntax)
                {
                    var baseType = symbol.BaseType;
                    baseClass = baseType == null || baseType.SpecialType == SpecialType.System_Object
                        ? null
                        : GetTypeKeyName(baseType);

                    // IsAttribute 沿继承链找 System.Attribute，间接继承的特性类也能识别
                    isAttribute = false;
                    for (var t = baseType; t != null; t = t.BaseType)
                    {
                        if (t.Name == "Attribute" && t.ContainingNamespace?.ToDisplayString() == "System")
                        {
                            isAttribute = true;
                            break;
                        }
                    }
                }

                return;
            }

            var baseTypes = declaration.BaseList?.Types.Select(t => t.Type.ToString()).ToList() ?? new List<string>();
            interfaces = baseTypes.Where(IsInterfaceName).ToList();
            baseClass = baseTypes.FirstOrDefault(t => !IsInterfaceName(t));
        }

        private static bool IsInterfaceName(string typeName)
        {
            return typeName.Split('.').Last().StartsWith("I");
        }

        /// <summary>
        /// 与元数据 Key（Namespace.Name）同格式的类型名，用于基类/接口关联
        /// </summary>
        private static string GetTypeKeyName(INamedTypeSymbol type)
        {
            var ns = type.ContainingNamespace;
            return ns == null || ns.IsGlobalNamespace ? type.Name : $"{ns.ToDisplayString()}.{type.Name}";
        }

        private static List<KeyValueModel> GetConstructorParams(TypeDeclarationSyntax typeDeclaration)
        {
            if (typeDeclaration == null)
                return new List<KeyValueModel>();

            foreach (var member in typeDeclaration.Members)
            {
                // RS1034：用 IsKind 判断语法种类，不再 Kind().ToString() 比字符串
                if (member.IsKind(SyntaxKind.ConstructorDeclaration))
                {
                    return member.DescendantNodes().OfType<ParameterSyntax>()
                        .Select(d => new KeyValueModel(d.Type?.ToString(), d.Identifier.Text)).ToList();
                }
            }

            return new List<KeyValueModel>();
        }

        /// <summary>
        /// 类型身份：命名空间 + 包含类型链 + 名称（+泛型元数），用于分部合并去重（BUG-05）
        /// </summary>
        private static string GetTypeIdentity(TypeDeclarationSyntax declaration)
        {
            var containing = declaration.Ancestors().OfType<TypeDeclarationSyntax>().Reverse().Select(t => t.Identifier.Text);
            var self = declaration.Identifier.Text +
                       (declaration.TypeParameterList != null ? "`" + declaration.TypeParameterList.Parameters.Count : "");
            var chain = string.Join(".", containing.Concat(new[] { self }));
            var ns = GetNamespace(declaration);
            return ns == null ? chain : ns + "." + chain;
        }

        /// <summary>
        /// BUG-07：向上汇总各层命名空间名称，嵌套命名空间得到完整命名空间，不再强转 CompilationUnitSyntax
        /// </summary>
        private static string GetNamespace(SyntaxNode node)
        {
            var parts = node.Ancestors().OfType<BaseNamespaceDeclarationSyntax>().Reverse().Select(n => n.Name.ToString()).ToList();
            return parts.Count == 0 ? null : string.Join(".", parts);
        }

        /// <summary>
        /// BUG-07：汇总编译单元与沿途各层命名空间内的 using（嵌套命名空间的 using 对内层类型同样有效）
        /// </summary>
        private static List<string> GetUsingList(SyntaxNode node)
        {
            var result = new List<string>();
            foreach (var ancestor in node.Ancestors())
            {
                SyntaxList<UsingDirectiveSyntax> usings;
                switch (ancestor)
                {
                    case CompilationUnitSyntax compilationUnit:
                        usings = compilationUnit.Usings;
                        break;
                    case NamespaceDeclarationSyntax namespaceDeclaration:
                        usings = namespaceDeclaration.Usings;
                        break;
                    case FileScopedNamespaceDeclarationSyntax fileScopedNamespace:
                        usings = fileScopedNamespace.Usings;
                        break;
                    default:
                        continue;
                }

                foreach (var usingDirective in usings)
                {
                    // 别名 using 无法拼出命名空间 Key，跳过
                    if (usingDirective.Alias != null)
                        continue;

                    var name = usingDirective.Name?.ToString();
                    if (!string.IsNullOrWhiteSpace(name) && !result.Contains(name))
                        result.Add(name);
                }
            }

            return result;
        }

        /// <summary>
        /// 提取特性元数据：优先用语义模型（AttributeData）拿真实参数值，
        /// 按 ApplicationSyntaxReference 与语法节点精确对应；符号不可用时退回语法解析。
        /// BUG-06：一个方括号内的多个特性全部提取（旧实现只取第一个）
        /// </summary>
        private List<AttributeMetaData> GetAttributeMetaData(SyntaxNode node)
        {
            var result = new List<AttributeMetaData>();
            if (!(node is MemberDeclarationSyntax memberDeclaration))
                return result;

            Dictionary<AttributeSyntax, AttributeData> dataBySyntax = null;
            try
            {
                var symbol = GetSemanticModel(node.SyntaxTree).GetDeclaredSymbol(node);
                if (symbol != null)
                {
                    dataBySyntax = new Dictionary<AttributeSyntax, AttributeData>();
                    foreach (var data in symbol.GetAttributes())
                    {
                        if (data.ApplicationSyntaxReference?.GetSyntax(_token) is AttributeSyntax syntax)
                            dataBySyntax[syntax] = data;
                    }
                }
            }
            catch
            {
                dataBySyntax = null;
            }

            foreach (var attributeListSyntax in memberDeclaration.AttributeLists)
            {
                foreach (var attributeSyntax in attributeListSyntax.Attributes)
                {
                    var attributeMetaData = new AttributeMetaData(attributeSyntax.Name.ToString(), attributeSyntax.ToString());
                    if (dataBySyntax != null && dataBySyntax.TryGetValue(attributeSyntax, out var data))
                    {
                        FillAttributeParams(attributeMetaData, data);
                    }
                    else
                    {
                        Extensions.Extensions.FillAttributeParamsFromSyntax(attributeMetaData, attributeSyntax);
                    }

                    result.Add(attributeMetaData);
                }
            }

            return result;
        }

        /// <summary>
        /// 用 AttributeData 填充参数：位置参数用构造器形参名作 Key（旧实现用值本身作 Key，模板按名永远取不到），
        /// 命名参数用成员名作 Key；值统一为与源码写法一致的字符串形式
        /// </summary>
        private static void FillAttributeParams(AttributeMetaData meta, AttributeData data)
        {
            var ctorParams = data.AttributeConstructor?.Parameters ?? ImmutableArray<IParameterSymbol>.Empty;
            for (var i = 0; i < data.ConstructorArguments.Length; i++)
            {
                var key = i < ctorParams.Length ? ctorParams[i].Name : i.ToString(CultureInfo.InvariantCulture);
                meta.AddParam(key, TypedConstantToString(data.ConstructorArguments[i]));
            }

            foreach (var kv in data.NamedArguments)
            {
                meta.AddParam(kv.Key, TypedConstantToString(kv.Value));
            }
        }

        private static string TypedConstantToString(TypedConstant constant)
        {
            if (constant.IsNull)
                return null;

            switch (constant.Kind)
            {
                case TypedConstantKind.Primitive:
                    // bool 输出小写 true/false，与源码文本一致（模板有 =='true' 的比较）
                    if (constant.Value is bool b)
                        return b ? "true" : "false";
                    // 字符串/字符输出为 C# 字面量（带引号、已转义）：模板会把参数值直接拼进生成代码
                    // （如 AopExtend 的 LogName 赋值），这也是旧实现命名参数的行为；位置/命名自此一致
                    if (constant.Value is string s)
                        return SymbolDisplay.FormatLiteral(s, true);
                    if (constant.Value is char c)
                        return SymbolDisplay.FormatLiteral(c, true);
                    return Convert.ToString(constant.Value, CultureInfo.InvariantCulture);
                case TypedConstantKind.Enum:
                    // 还原为 TypeName.MemberName，与源码写法一致（如 BizTypeEnum.Enum、EnumType.b）
                    var enumType = constant.Type;
                    var memberName = enumType?.GetMembers().OfType<IFieldSymbol>()
                        .FirstOrDefault(f => f.HasConstantValue && Equals(f.ConstantValue, constant.Value))?.Name;
                    return enumType != null && memberName != null
                        ? $"{enumType.Name}.{memberName}"
                        : Convert.ToString(constant.Value, CultureInfo.InvariantCulture);
                case TypedConstantKind.Type:
                    return (constant.Value as ITypeSymbol)?.ToDisplayString() ?? constant.Value?.ToString();
                case TypedConstantKind.Array:
                    return string.Join(",", constant.Values.Select(TypedConstantToString));
                default:
                    return constant.Value?.ToString();
            }
        }

        private SemanticModel GetSemanticModel(SyntaxTree tree)
        {
            if (!_modelCache.TryGetValue(tree, out var model))
            {
                model = _compilation.GetSemanticModel(tree);
                _modelCache[tree] = model;
            }

            return model;
        }

        private MethodMetaData GetMethodMetaData(MethodDeclarationSyntax methodDeclarationSyntax)
        {
            var param = new List<KeyValueModel>();
            foreach (var parameterSyntax in methodDeclarationSyntax.ParameterList.Parameters)
            {
                var type = parameterSyntax.Type?.ToString();
                var name = parameterSyntax.Identifier.Text;
                if (type != null)
                    param.Add(new KeyValueModel(type, name));
            }

            var returnValue = methodDeclarationSyntax.ReturnType.ToString();

            return new MethodMetaData(methodDeclarationSyntax.Identifier.Text,
                GetAttributeMetaData(methodDeclarationSyntax), returnValue, param, methodDeclarationSyntax.Modifiers.ToString(), methodDeclarationSyntax.Modifiers.ToString(), null);
        }

        /// <summary>
        /// 获取属性上的注释
        /// </summary>
        /// <param name="propertyDeclarationSyntax"></param>
        /// <returns></returns>
        private List<string> GetPropertyDescription(PropertyDeclarationSyntax propertyDeclarationSyntax)
        {
            return propertyDeclarationSyntax.DescendantTokens().OfType<SyntaxToken>()
                .Where(t => t.HasLeadingTrivia && t.LeadingTrivia.Any(l => l.IsKind(SyntaxKind.SingleLineCommentTrivia)))
                .SelectMany(t => t.LeadingTrivia.Where(l => l.IsKind(SyntaxKind.SingleLineCommentTrivia)))
                .Select(t => t.ToString())
                .ToList();
        }
    }
}
