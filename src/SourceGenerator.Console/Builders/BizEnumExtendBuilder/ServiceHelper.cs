using System;

namespace SourceGenerator.Consoles.Builders.BizEnumExtendBuilder
{
    public class ServiceHelper
    {
        public static Func<Type, object> GetServiceFunc;

        public static T GetService<T>()
        {
            return (T)GetServiceFunc(typeof(T));
        }
    }
}
