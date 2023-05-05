using System;
using System.Threading.Tasks;

namespace SourceGenerator.Consoles.Builders.AopBuilder
{
    /// <summary>
    /// 日志
    /// </summary>
    public class LogAttribute : AopInterceptor
    {
        public string LogName { get; set; }

        public EnumType Type { get; set; }

        /// <summary>
        /// 日志服务，只有 实际方法、After
        /// </summary>
        public LogAttribute()
        {
            HasBefore = false;
            HasAopNext = false;
        }

        public override AopContext After(AopContext context)
        {
            Console.WriteLine("log trace sync");
            return context;
        }
        
        /// <summary>执行后操作，异步方法调用</summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public override ValueTask<AopContext> AfterAsync(AopContext context)
        {
            Console.WriteLine("log trace async");
            return base.AfterAsync(context);
        }
    }

    /// <summary>
    /// 日志
    /// </summary>
    public class Log2Attribute : AopInterceptor
    {
        public string LogName { get; set; }

        public EnumType Type { get; set; }

        /// <summary>
        /// 日志服务，只有 实际方法、After
        /// </summary>
        public Log2Attribute()
        {
            HasBefore = false;
            HasAopNext = false;
        }

        public override AopContext After(AopContext context)
        {
            Console.WriteLine("log trace sync");
            return context;
        }

        /// <summary>执行后操作，异步方法调用</summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public override ValueTask<AopContext> AfterAsync(AopContext context)
        {
            Console.WriteLine("log trace async");
            return base.AfterAsync(context);
        }
    }
}
