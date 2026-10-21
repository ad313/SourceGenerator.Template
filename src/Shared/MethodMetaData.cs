using System;
using System.Collections.Generic;
using System.Linq;

namespace SourceGenerator.Template.MetaData
{
    /// <summary>
    /// 方法元数据
    /// </summary>
    public sealed class MethodMetaData : MetaDataBase
    {
        /// <summary>
        /// 初始化
        /// </summary>
        /// <param name="name">名称</param>
        /// <param name="attributeMetaData">attribute</param>
        /// <param name="returnValue">返回值</param>
        /// <param name="paramList">输入参数</param>
        /// <param name="perfix">方法修饰符</param>
        /// <param name="accessModifier"></param>
        /// <param name="extModifier"></param>
        /// <param name="source"></param>
        public MethodMetaData(string name, List<AttributeMetaData> attributeMetaData, string returnValue, List<KeyValueModel> paramList, string perfix, string accessModifier, string extModifier, string source = null) : base(name, accessModifier, extModifier, attributeMetaData, source)
        {
            ReturnValue = returnValue;
            ParamList = paramList;

            HasReturnValue = !string.IsNullOrWhiteSpace(returnValue) && returnValue != "void" && returnValue != "Task";
            IsTask = returnValue?.StartsWith("Task") == true || returnValue?.StartsWith("ValueTask") == true || returnValue?.StartsWith("ITask") == true;

            if (HasReturnValue)
            {
                if (IsTask)
                {
                    // BUG-12：按首个 '<' 与末尾 '>' 截取泛型实参，
                    // Task<Dictionary<string, int>> 不再被 Split('>') 截断成 "Dictionary<string"
                    var start = returnValue.IndexOf('<');
                    var end = returnValue.LastIndexOf('>');
                    if (start >= 0 && end > start)
                    {
                        ReturnType = returnValue.Substring(start + 1, end - start - 1);
                    }
                }
                else
                {
                    ReturnType = ReturnValue;
                }
            }

            // BUG-12：修饰符逐词判断，protected virtual 等组合不再误判为不可重写
            CanOverride = !string.IsNullOrWhiteSpace(perfix) && perfix
                .Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
                .Any(t => t == "virtual" || t == "override");

            Key = GetKey(name, paramList);
        }

        /// <summary>
        /// 方法唯一标识符，区分方法重载
        /// </summary>
        public string Key { get; private set; }
        /// <summary>
        /// 返回值
        /// </summary>
        public string ReturnValue { get; private set; }
        /// <summary>
        /// 返回值类型
        /// </summary>
        public string ReturnType { get; private set; }
        /// <summary>
        /// 是否有返回值
        /// </summary>
        public bool HasReturnValue { get; private set; }
        /// <summary>
        /// 是否是异步
        /// </summary>
        public bool IsTask { get; private set; }
        /// <summary>
        /// 能否被重写
        /// </summary>
        public bool CanOverride { get; private set; }
        /// <summary>
        /// 输入参数
        /// </summary>
        public List<KeyValueModel> ParamList { get; private set; }
    }
}