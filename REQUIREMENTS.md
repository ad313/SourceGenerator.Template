# SourceGenerator.Template 需求规格文档

## 1. 项目背景与目标

### 1.1 项目背景

在现代 C# 开发中，存在大量重复性代码需要编写，如：
- DTO/Entity 到 Proto 的转换
- AOP 拦截器代理类
- 枚举业务扩展方法
- ORM 映射代码
- API 客户端生成

这些代码模式固定，但手动编写容易出错且维护成本高。

### 1.2 核心目标

**SourceGenerator.Template** 旨在提供一个**通用的源代码生成框架**，让开发者能够：
- 通过配置文件和模板文件定义代码生成规则
- 在编译时自动生成代码，无需运行时开销
- 打包和分发模板实现项目间复用
- 支持复杂的代码转换逻辑

---

## 2. 功能需求

### 2.1 元数据提取功能

#### 2.1.1 支持的元数据类型

| 类型 | 支持内容 |
|------|----------|
| **Class** | 名称、命名空间、属性、方法、继承类、接口、构造函数、using、访问修饰符、特性 |
| **Interface** | 名称、命名空间、属性、方法、继承接口、using、访问修饰符、特性 |
| **Struct** | 名称、命名空间、属性、方法、接口、构造函数、using、访问修饰符、特性 |
| **Enum** | 名称、命名空间、成员、using、访问修饰符、特性 |
| **Method** | 名称、返回值、参数、特性、访问修饰符、方法修饰符 |
| **Property** | 名称、类型、访问修饰符、特性、XML 注释 |

#### 2.1.2 继承关系处理

- **分部类合并**：支持 `partial class` 的多个部分合并
- **基类数据合并**：将父类的属性、方法、特性合并到子类
- **接口数据合并**：将接口实现的方法合并到实现类
- **继承链追溯**：支持多层继承的数据合并

#### 2.1.3 特性处理

- 提取特性名称和命名空间
- 解析特性构造参数
- 建立特性与对应类的关联
- 支持特性继承关系判断

### 2.2 模板引擎功能

#### 2.2.1 模板配置管理

- 通过 `Map.json` 定义模板集合
- 支持模板启用/禁用
- 支持模板执行顺序控制
- 支持本地模板覆盖外部模板

#### 2.2.2 Scriban 模板支持

- 使用 Scriban 2.x 语法
- 支持变量、控制流（if/for）、函数调用
- 内置丰富的字符串和集合操作函数

#### 2.2.3 自定义模板函数

| 函数名 | 参数 | 返回值 | 功能说明 |
|--------|------|--------|----------|
| `has_attribute` | (对象, 特性名) | bool | 检查对象是否包含指定特性 |
| `has_attribute_by_full_name` | (对象, 特性全名) | bool | 按全名检查特性 |
| `merge` | (对象) | 对象 | 合并父级数据到子类 |
| `render` | (数据, 模板配置, 模板名, 文件名) | void | 渲染子模板并输出 |
| `json` | (对象, 文件名) | void | 序列化对象为 JSON |
| `list_filter_by_attribute` | (列表, 特性名) | 列表 | 按特性过滤 |
| `list_filter_by_name` | (列表, 名称) | 列表 | 按名称过滤 |
| `list_filter_by_attributes` | (列表, 特性名数组) | 列表 | 按多个特性过滤 |
| `list_filter_by_attribute_key` | (列表, 特性名, 参数Key) | 列表 | 按特性参数过滤 |
| `list_filter_by_attribute_key_value` | (列表, 特性名, 参数Key, 参数值) | 列表 | 按特性参数值过滤 |
| `get_attribute_param_value_from_attribute` | (特性, 参数Key) | string | 获取特性参数值 |
| `attribute_has_parent` | (特性, 父类名) | bool | 判断特性是否继承指定类 |
| `low_first_code` | (字符串) | string | 首字母小写 |
| `now` | 无 | string | 获取当前时间 |

### 2.3 模板分发功能

#### 2.3.1 本地模板

- 支持在项目中直接定义模板
- 模板文件需设置为"分析器其他文件"参与编译
- 本地模板与外部模板合并时优先使用

#### 2.3.2 模板包

- 支持将模板打包为 NuGet 包
- 包命名规范：以 `Sg.Templates.dll` 结尾
- 模板内容放在 `Templates` 文件夹
- 自动合并本地和包中的模板定义

### 2.4 调试与诊断功能

#### 2.4.1 编译时诊断

生成以下诊断信息文件：
- **Error**: 错误信息汇总
- **Time**: 各阶段耗时统计
- **MetaJson**: 扫描到的元数据 JSON 序列化
- **TemplateInfo**: 生效的模板信息

#### 2.4.2 错误处理

- 全局异常捕获
- 分阶段错误收集
- 详细的错误位置和原因报告

---

## 3. 非功能需求

### 3.1 性能需求

- 元数据提取应在编译过程中高效完成
- 增量编译时仅处理变更的源文件
- 支持 CancellationToken 以响应编译取消

### 3.2 兼容性需求

- 目标框架：.NET Standard 2.0
- 支持 .NET 6+ 项目引用
- 兼容 Visual Studio 2022 及更高版本

### 3.3 可扩展性需求

- 支持添加新的模板函数
- 支持自定义元数据处理逻辑
- 支持模板间的数据共享

---

## 4. 用例分析

### 4.1 基础用例：元数据查看

**场景**：开发者想了解项目中有哪些类、接口、枚举

**步骤**：
1. 在项目中添加 `Map.json` 和模板
2. 定义遍历所有元数据的模板
3. 编译项目
4. 查看生成的元数据文件

**输出示例**：
```json
{
  "class_meta_data_list": [...],
  "interface_meta_data_list": [...],
  "struct_meta_data_list": [...],
  "enum_meta_data_list": [...]
}
```

### 4.2 进阶用例：AOP 代理生成

**场景**：为服务类方法自动生成 AOP 拦截代理

**步骤**：
1. 定义 AOP 拦截器基类 `AopInterceptor`
2. 创建自定义拦截特性（如 `[Log]`）
3. 在业务类/方法上添加拦截特性
4. 模板自动生成 `{ClassName}_g` 代理类

**输入示例**：
```csharp
[Log]
public interface IUserService
{
    [Log]
    Task<User> GetUser(int id);
}
```

**输出示例**：
```csharp
public sealed class UserService_g : UserService
{
    public override async Task<User> GetUser(int id)
    {
        var context = new AopContext(...);
        // Before: LogInterceptor.BeforeAsync
        // Next: 调用实际方法
        // After: LogInterceptor.AfterAsync
        return context.ReturnValue;
    }
}
```

### 4.3 高级用例：Proto 模型生成

**场景**：将 DTO 类转换为 gRPC Proto 消息

**步骤**：
1. 在 DTO 类上添加 `[ToProto]` 特性
2. 定义 Proto 模板
3. 编译时自动生成 `.proto` 文件

**输入示例**：
```csharp
[ToProto]
public class UserDto
{
    public int Id { get; set; }
    public string Name { get; set; }
    public DateTime CreateTime { get; set; }
}
```

**输出示例**：
```protobuf
message UserDtoProto {
    int32 id = 1;
    google.protobuf.StringValue name = 2;
    google.protobuf.StringValue create_time = 3;
}
```

---

## 5. 使用限制与约束

### 5.1 技术限制

1. **仅扫描当前程序集**：SourceGenerator 只能分析当前编译的程序集元数据
2. **不支持跨程序集继承**：继承自外部程序集的类无法获取父类元数据
3. **模板文件类型**：仅支持 `.txt` 和 `.json` 文件

### 5.2 最佳实践

1. 合理使用分部类组织代码
2. 为重要的类和方法添加 XML 文档注释
3. 特性参数尽量使用命名参数提高可读性
4. 模板文件命名应清晰表达用途

---

## 6. 术语表

| 术语 | 定义 |
|------|------|
| **SourceGenerator** | .NET 编译器 API，可在编译时生成代码 |
| **IncrementalGenerator** | 增量源生成器，优化编译性能 |
| **Scriban** | 轻量级文本模板引擎 |
| **Roslyn** | .NET 编译器平台，提供代码分析 API |
| **语法树 (SyntaxTree)** | 代码的结构化表示 |
| **分部类 (Partial Class)** | 跨多个文件定义的类 |
| **特性 (Attribute)** | C# 代码注解机制 |

---

## 7. 参考文献

- [Roslyn Source Generators](https://docs.microsoft.com/zh-cn/dotnet/csharp/roslyn-sdk/source-generators-overview)
- [Scriban Template Language](https://github.com/scriban/scriban/blob/master/doc/builtins.md)
- [SourceGenerator.Template GitHub](https://github.com/ad313/SourceGenerator.Template)
