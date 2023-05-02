using System.Threading.Tasks;

namespace SourceGenerator.Consoles.Builders.AopBuilder
{
    /// <summary>
    /// Aop 拦截器
    /// </summary>
    public interface IAopInterceptor
    {
        /// <summary>
        /// 执行前操作，同步方法调用
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        AopContext Before(AopContext context);
        /// <summary>
        /// 执行前操作，异步方法调用
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        ValueTask<AopContext> BeforeAsync(AopContext context);
        /// <summary>
        /// 执行后操作，同步方法调用
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        AopContext After(AopContext context);
        /// <summary>
        /// 执行后操作，异步方法调用
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        ValueTask<AopContext> AfterAsync(AopContext context);
        /// <summary>
        /// 执行方法，同步方法调用
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        AopContext Next(AopContext context);
        /// <summary>
        /// 执行方法，异步方法调用
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        ValueTask<AopContext> NextAsync(AopContext context);
    }
}
