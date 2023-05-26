using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SourceGenerator.Consoles.Builders.AopBuilder
{
    //[Log(Enable = true, LogName = "this is bbb", Type = EnumType.b)]
    [Log2(Enable = true, LogName = "this is bbb2", Type = EnumType.b)]
    //[IgnoreAop]
    public interface IAopClass1
    {
        void Test1(string aaa, string bbb);
        void Test2();

        //[Log]
        //[IgnoreAop]
        Task<int> Test3();
    }


    //[Log]
    //[IgnoreAop]
    public class AopClass1 : IAopClass1
    {
        public AopClass1(string a, int b)
        {

        }

        //[Display]
        [Log(Enable = true, LogName = "this is aaa", Type = EnumType.a)]
        [Log2(Enable = true, LogName = "this is aaa2", Type = EnumType.a)]
        public virtual void Test1(string aaa, string bbb)
        {

        }

        [IgnoreAop]
        public void Test2()
        {

        }

        public virtual async Task<int> Test3()
        {
            return 1;
        }
    }

    //public class AopClass12 : AopClass1
    //{
    //    public AopClass12(string a, int b) : base(a, b)
    //    {
    //    }
    //}


}
