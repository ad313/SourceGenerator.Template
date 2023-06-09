using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SourceGenerator.Consoles.Builders.AopBuilder
{
    /// <summary>
    /// Aop 上下文
    /// </summary>
    public struct AopContext
    {
        /// <summary>
        /// 是否是异步
        /// </summary>
        public bool IsTask { get; private set; }
        /// <summary>
        /// 是否有返回值
        /// </summary>
        public bool HasReturnValue { get; private set; }
        /// <summary>
        /// 方法输入参数
        /// </summary>
        public Dictionary<string, dynamic> MethodInputParam { get; private set; }

        /// <summary>
        /// 实际方法执行结果，可能是 Task
        /// </summary>
        public Func<dynamic> ActualMethod { get; set; }
        /// <summary>
        /// 返回值，具体的值
        /// </summary>
        public dynamic ReturnValue { get; set; }
        /// <summary>
        /// 返回值类型
        /// </summary>
        public Type ReturnType { get; set; }
        /// <summary>
        /// 异常信息
        /// </summary>
        public Exception Exception { get; set; }
        /// <summary>
        /// IServiceProvider
        /// </summary>
        public IServiceProvider ServiceProvider { get; private set; }

        /// <summary>
        /// 初始化
        /// </summary>
        /// <param name="serviceProvider"></param>
        /// <param name="methodInputParam"></param>
        /// <param name="isTask"></param>
        /// <param name="hasReturnValue"></param>
        /// <param name="actualMethod"></param>
        public AopContext(IServiceProvider serviceProvider, Dictionary<string, dynamic> methodInputParam, bool isTask, bool hasReturnValue, Func<dynamic> actualMethod, Type returnType) : this()
        {
            ServiceProvider = serviceProvider;
            MethodInputParam = methodInputParam;
            IsTask = isTask;
            HasReturnValue = hasReturnValue;
            ActualMethod = actualMethod;
            ReturnType = returnType;
        }

        /// <summary>
        /// 执行实际方法 异步
        /// </summary>
        /// <returns></returns>
        public async ValueTask<AopContext> InvokeAsync()
        {
            if (ActualMethod == null)
                return this;

            if (HasReturnValue)
            {
                ReturnValue = IsTask ? await ActualMethod() : ActualMethod();
                return this;
            }

            if (IsTask)
                await ActualMethod();
            else
                ActualMethod();

            return this;
        }

        /// <summary>
        /// 执行实际方法 同步
        /// </summary>
        /// <returns></returns>
        public void Invoke()
        {
            if (ActualMethod == null)
                return;

            //特殊处理 同步且没有返回值，用 Task.Run 包装
            if (!IsTask && !HasReturnValue)
                ActualMethod.Invoke().GetAwaiter().GetResult();
            else
                ReturnValue = ActualMethod.Invoke();
        }
    }
}