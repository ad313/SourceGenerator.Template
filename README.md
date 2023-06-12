# SourceGenerator.Template
通过扫描当前项目的元数据，定义 Map.Json 和 *.txt 模板文件，使用 scriban 模板引擎，实时生成内容。局限性：只能扫描当前程序集的内容。
## 扫描元数据类型
```
class
interface
struct
enum
method
property
attribute
```
##  怎么使用？
### 1、引入包
    //搜索 SourceGenerator.Template 包，安装最新版
    <PackageReference Include="SourceGenerator.Template" Version="1.0.9" />
    
    此时在项目的 依赖项-分析器 - [SourceGenerator.Template.Generators] 下面可以看到扫描出来的元数据信息
    Error：报错信息
    MetaJson：元数据序列化后的json字符串
    TemplateInfo：生效的相关模板信息
    Time：扫描出来的元数据类别以及耗时
    
### 2、定义模板。内部引入了 scriban https://github.com/scriban/scriban。
#### 1、添加 Map.Json 注册模板。比如添加一个 class 转 grpc proto message 模型的模板
```
[
  {
    "Code": "ToProtoBuilder",
    "Name": "Class To Proto 扩展",
    "MainTemplate": "ToProto_Main.txt",
    "Templates": [ "ToProto.txt" ],
    "Enable": true,
    "Order": 1
  }
]

Code：唯一值不能重复
MainTemplate：模板入口，类似于 Program 的 Main。这里组织数据，做一些逻辑组装，比如过滤class，或者过滤包含某个tag的method或attribute等。
Templates：可以定义多个模板文件，这里定义了只在当前扩展范围内生效。
Enable：是否启用
Order：数字越大越晚执行
数据组装好后，调用 render 方法，生成代码
```
#### 2、编写 scriban 模板，固定是.txt 文件。 具体见 项目中的 SourceGenerator.Consoles 示例项目：https://github.com/ad313/SourceGenerator.Template/tree/main/src/SourceGenerator.Console/tmp
scriban 模板语法参考：https://github.com/scriban/scriban/blob/master/doc/builtins.md
#### 3、本地项目使用，需要把 Map.Json 和 所有模板设置为 “C#分析器其他文件”。这样才能参与编译。
#### 4、如果要把模板打包后让其他项目使用，则需要满足下列规则：
      
      1、打包后的dll以 Sg.Templates.dll 结尾
      2、项目根目录新建一个文件夹，名称为 Templates
      3、把模板放入Templates，并且设置所有的模板为 "嵌入的资源"
#### 5、模板覆盖原则：当引入了外部 *Sg.Templates.dll 模板，同时本地也定义了 Map.Json 及模板，此时会合并两部分模板。当模板Code重复，以本地的为准，本地的覆盖外部的。
##### 局限性：SourceGenerator 只能扫描每个程序集的内容，因此你本地每个程序集可能都需要定义 Map.Json 和 相关模板， 当然你可以把模板封装成包，并以 Sg.Templates 结尾，这样引用这个包就可以了，达到共享的目的。
