using System;
using System.Threading.Tasks;

namespace SourceGenerator.Consoles.Builders.AopBuilder
{
    public class AopInterceptor : Attribute, IAopInterceptor//, IDisposable
    {
        //#region IServiceProvider IServiceScope
        //private IServiceScope Scope { get; set; }

        //protected IServiceScope CreateServiceScope(IServiceProvider serviceProvider)
        //{
        //    Scope = serviceProvider.CreateScope();
        //    return Scope;
        //}

        //protected T GetService<T>() where T : class
        //{
        //    return Scope?.ServiceProvider.GetService<T>();
        //} 
        //#endregion

        /// <summary>
        /// 是否执行 Before
        /// </summary>
        public bool HasBefore { get; set; }
        /// <summary>
        /// 是否执行 After
        /// </summary>
        public bool HasAfter { get; set; }
        /// <summary>
        /// 是否执行 Aop 的 Next
        /// </summary>
        public bool HasAopNext { get; set; }
        /// <summary>
        /// 是否执行实际的方法
        /// </summary>
        public bool HasActualNext { get; set; }

        /// <summary>
        /// 启用
        /// </summary>
        public bool AopTag { get; set; }

        public AopInterceptor()
        {
            HasBefore = true;
            HasAopNext = true;
            HasActualNext = true;
            HasAfter = true;
        }

        public virtual AopContext Before(AopContext context) => context;

        public virtual async ValueTask<AopContext> BeforeAsync(AopContext context)
        {
            await ValueTask.CompletedTask;
            return context;
        }

        public virtual AopContext After(AopContext context)
        {
            return context.Exception != null ? throw context.Exception : context;
        }

        public virtual async ValueTask<AopContext> AfterAsync(AopContext context)
        {
            if (context.Exception != null)
                throw context.Exception;

            await ValueTask.CompletedTask;
            return context;
        }

        public virtual AopContext Next(AopContext context)
        {
            try
            {
                context.Invoke();
            }
            catch (Exception e)
            {
                context.Exception = e;
            }
            return context;
        }

        public virtual async ValueTask<AopContext> NextAsync(AopContext context)
        {
            try
            {
                context = await context.InvokeAsync();
            }
            catch (Exception e)
            {
                context.Exception = e;
            }

            return context;
        }

        public virtual void Clear()
        {

        }

        //public void Dispose()
        //{
        //    //Scope?.Dispose();
        //}
    }

    public class DisplayAttribute : Attribute
    {

    }
}
