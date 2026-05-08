# SourceGenerator.Template 项目设计文档

## 1. 项目概述

### 1.1 项目简介

**SourceGenerator.Template** 是一个基于 .NET Roslyn 源代码生成器的模板引擎项目。通过扫描当前项目的元数据（类、接口、结构体、枚举、方法、属性、特性等），结合 Scriban 模板引擎，实现自定义代码生成功能。

### 1.2 核心价值

- **零侵入性**：作为 SourceGenerator 工作，编译时自动执行，无需运行时开销
- **高度可扩展**：通过 JSON 配置文件和模板文件灵活定义生成规则
- **模板共享**：支持将模板打包为 NuGet 包，实现跨项目复用
- **元数据驱动**：基于 Roslyn 语法树分析，提供完整的代码结构信息

---

## 2. 系统架构

### 2.1 整体架构图

```
┌─────────────────────────────────────────────────────────────────────────┐
│                           SourceGenerator.Template                       │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                          │
│  ┌─────────────────┐    ┌──────────────────┐    ┌─────────────────┐   │
│  │ SourceGenerator │───▶│   SyntaxReceiver │───▶│ AssemblyMetaData │   │
│  │  .Template      │    │  (语法树分析器)   │    │  (元数据容器)    │   │
│  │  .Generators    │    └──────────────────┘    └────────┬────────┘   │
│  └─────────────────┘                                      │             │
│         │                                                 ▼             │
│         │                                      ┌─────────────────┐       │
│         │                                      │  TemplateRender │       │
│         │                                      │  (模板渲染器)    │       │
│         │                                      └────────┬────────┘       │
│         │                                               │               │
│         ▼                                               ▼               │
│  ┌─────────────────┐                          ┌─────────────────┐       │
│  │   Map.json      │                          │    Scriban      │       │
│  │ (模板配置)      │                          │   模板引擎      │       │
│  └─────────────────┘                          └────────┬────────┘       │
│                                                        │               │
└────────────────────────────────────────────────────────┼───────────────┘
                                                         ▼
                                               ┌─────────────────┐
                                               │   生成的代码     │
                                               │  (Source Output) │
                                               └─────────────────┘
```

### 2.2 项目模块划分

| 模块名称 | 类型 | 职责 |
|---------|------|------|
| SourceGenerator.Template | 类库 (netstandard2.0) | 核心元数据模型定义，NuGet 包发布 |
| SourceGenerator.Template.Generators | 类库 (netstandard2.0) | 增量生成器实现，模板渲染逻辑 |
| SourceGenerator.Console | 控制台应用 (net6.0) | 示例项目和测试场 |

---

## 3. 核心模块设计

### 3.1 元数据层 (Shared)

#### 3.1.1 元数据类图

```
MetaDataBase (基类)
├── AttributeMetaData          # 特性元数据
├── PropertyMetaData           # 属性元数据
├── MethodMetaData             # 方法元数据
├── InterfaceMetaData          # 接口元数据
│   └── ClassMetaData          # 类元数据 (继承)
├── StructMetaData             # 结构体元数据
├── EnumMetaData               # 枚举元数据
└── AssemblyMetaData           # 程序集元数据 (容器)
```

#### 3.1.2 关键元数据模型

**MetaDataBase** - 所有元数据基类
- `Name`: 名称
- `AccessModifier`: 访问修饰符 (public/private 等)
- `ExtModifier`: 扩展修饰符 (partial、类型名等)
- `Source`: 原始代码字符串
- `AttributeMetaDataList`: 特性列表

**ClassMetaData** - 类元数据扩展
- `BaseClass`: 基类名称
- `BaseClassMetaData`: 基类元数据引用
- `Constructor`: 构造函数参数
- `IsAttribute`: 是否是特性类
- `MergePartial()`: 合并分部类
- `MergeAllParents()`: 合并所有父级数据

**MethodMetaData** - 方法元数据
- `ReturnValue`: 返回值类型
- `ReturnType`: 泛型返回值类型
- `HasReturnValue`: 是否有返回值
- `IsTask`: 是否异步方法
- `CanOverride`: 能否被重写
- `ParamList`: 参数列表
- `Key`: 方法唯一标识符（支持重载区分）

**AttributeMetaData** - 特性元数据
- `Name/FullName/ShortName`: 特性名称
- `ParamDictionary`: 特性参数字典
- `ClassMetaData`: 对应的特性类元数据
- `GetStringParam/GetIntParam/GetBoolParam`: 参数获取方法

### 3.2 语法分析层 (SyntaxReceiver)

**职责**：遍历 Roslyn 语法树，提取元数据信息

**处理流程**：
1. 提取所有 Class、Interface、Struct、Enum、Record 语法节点
2. 递归解析每个类型的属性、方法、特性、继承关系
3. 处理分部类合并
4. 建立继承链关系
5. 格式化输出最终元数据

**关键技术点**：
- 使用 `CancellationToken` 支持编译取消
- 分部类自动合并 (partial class)
- 命名空间和 using 指令解析
- 属性注释提取 (///\<summary\>)

### 3.3 模板渲染层 (TemplateRender + FilterFunctions)

#### 3.3.1 模板配置模型 (MapModel)

```json
{
  "Code": "ToProtoBuilder",           // 唯一标识
  "Name": "Class To Proto 扩展",       // 显示名称
  "MainTemplate": "ToProto_Main.txt", // 主模板入口
  "Templates": ["ToProto.txt"],       // 模板文件列表
  "Enable": true,                     // 是否启用
  "Order": 1                           // 执行顺序
}
```

#### 3.3.2 Scriban 自定义函数

| 函数名 | 用途 |
|--------|------|
| `has_attribute` | 检查是否包含指定特性 |
| `merge` | 合并父级数据到子类 |
| `render` | 渲染子模板并输出 |
| `json` | 序列化对象为 JSON |
| `list_filter_by_attribute` | 按特性过滤列表 |
| `list_filter_by_name` | 按名称过滤列表 |
| `get_attribute_param_value_from_attribute` | 获取特性参数值 |
| `low_first_code` | 首字母小写 |
| `now` | 获取当前时间 |

---

## 4. 工作流程

### 4.1 代码生成流程

```
┌─────────────┐     ┌──────────────┐     ┌──────────────┐     ┌─────────────┐
│   编译触发   │────▶│ 增量编译器   │────▶│ 语法树分析   │────▶│ 元数据提取  │
└─────────────┘     └──────────────┘     └──────────────┘     └──────┬──────┘
                                                                        │
                        ┌──────────────────────────────────────────────┘
                        ▼
┌─────────────┐     ┌──────────────┐     ┌──────────────┐     ┌─────────────┐
│  生成代码   │◀────│ Scriban渲染  │◀────│ 模板合并     │◀────│ 读取模板   │
└─────────────┘     └──────────────┘     └──────────────┘     └─────────────┘
```

### 4.2 模板解析流程

1. **加载配置**：读取 `Map.json` 获取模板定义
2. **加载模板文件**：从 AdditionalFiles 加载 `.txt` 模板
3. **解析主模板**：执行 MainTemplate 中的逻辑
4. **调用子模板**：通过 `render` 函数渲染具体模板
5. **输出代码**：通过 `IncrementalGenerator.Context.AddSource` 输出

### 4.3 模板覆盖机制

- 本地模板优先于外部模板包
- 当 Code 相同时，本地定义覆盖外部定义
- 外部模板需打包为 `*Sg.Templates.dll`

---

## 5. 示例功能实现

### 5.1 AOP 拦截器生成

**功能**：为标记了 AOP 特性的方法自动生成代理类

**模板处理逻辑**：
1. 扫描所有继承自 `AopInterceptor` 的特性
2. 遍历所有类和接口方法
3. 按特性继承链向上查找（方法 → 类 → 接口）
4. 生成继承原始类的代理类
5. 在代理方法中插入 Before/Next/After 管道

**生成代码结构**：
```csharp
public sealed class {OriginalClass}_g : {OriginalClass}
{
    private readonly IServiceProvider _serviceProvider0;
    public {ClassName}(...) : base(...) { }

    public override {ReturnType} {MethodName}(...)
    {
        var aopContext = new AopContext(...);
        // Before 拦截
        if (interceptor.HasBefore) aopContext = await interceptor.BeforeAsync(aopContext);
        // Next 执行
        if (interceptor.HasAopNext) aopContext = await interceptor.NextAsync(aopContext);
        // After 拦截
        if (interceptor.HasAfter) aopContext = await interceptor.AfterAsync(aopContext);
        return aopContext.ReturnValue;
    }
}
```

### 5.2 Proto 模型生成

**功能**：将 C# 类转换为 gRPC Proto 消息定义

**类型映射**：
| C# 类型 | Proto 类型 |
|---------|-----------|
| string/DateTime | google.protobuf.StringValue |
| int/int? | int32 / google.protobuf.Int32Value |
| bool/bool? | bool / google.protobuf.BoolValue |
| double/float | double |
| decimal | google.protobuf.DoubleValue |

---

## 6. 部署与发布

### 6.1 NuGet 包结构

```
SourceGenerator.Template.{version}.nupkg
├── lib/netstandard2.0/
│   └── SourceGenerator.Template.dll
├── analyzers/dotnet/cs/
│   └── SourceGenerator.Template.Generators.dll
└── tools/
    ├── install.ps1
    └── uninstall.ps1
```

### 6.2 模板包发布规范

打包模板扩展包需满足：
1. DLL 名称以 `Sg.Templates.dll` 结尾
2. 根目录包含 `Templates` 文件夹
3. Templates 文件夹内包含 `Map.json` 和模板文件
4. 所有模板文件设置为"嵌入的资源"

---

## 7. 技术选型

| 技术 | 版本 | 用途 |
|------|------|------|
| .NET Standard | 2.0 | 跨框架兼容性 |
| Microsoft.CodeAnalysis.CSharp | 4.3.1 | 语法树分析 |
| Scriban | 5.7.0 | 模板渲染引擎 |
| Newtonsoft.Json | 13.0.3 | JSON 序列化 |

---

## 8. 扩展设计

### 8.1 自定义模板开发步骤

1. **定义 Map.json**：注册模板配置
2. **编写 MainTemplate**：定义数据处理逻辑
3. **编写子模板**：定义具体输出格式
4. **设置文件类型**：将模板文件设为"分析器其他文件"
5. **测试验证**：编译项目查看生成的代码

### 8.2 模板函数扩展点

可通过继承 `FilterFunctions` 添加自定义 Scriban 函数：
```csharp
public class CustomFunctions : FilterFunctions
{
    public static string CustomFilter(string input) { ... }
}
```
