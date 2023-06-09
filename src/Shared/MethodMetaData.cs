using System;
using System.Collections.Generic;
using System.Linq;

namespace SourceGenerator.Analyzers.MetaData
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
                    var arr = returnValue?.Replace("Task<", "#").Replace("ValueTask<", "#").Replace("ITask<", "#")
                        .Split('#').ToList() ?? new List<string>();

                    if (arr.Count == 2)
                    {
                        ReturnType = arr[1].Split('>')[0];
                    }
                }
                else
                {
                    ReturnType = ReturnValue;
                }
            }

            CanOverride = !string.IsNullOrWhiteSpace(perfix) && perfix.Contains(" ") && new List<string>() { "virtual", "override" }.Contains(perfix
                .Replace("public", "").Trim()
                .Split(' ').First());

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