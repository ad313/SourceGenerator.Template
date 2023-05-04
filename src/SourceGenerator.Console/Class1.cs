using SourceGenerator.Consoles.Builders.ClassToProtoBuilder;
using System;
using System.Threading.Tasks;

namespace SourceGenerator.Consoles
{
    internal class Class1
    {
        public void Test1()
        {

        }
        
    }
    
    [ClassToProto]
    public partial class Class2
    {
        /// <summary>
        /// aaaaaaaaa
        /// </summary>
        public string a { get; set; }
    }

    public partial class Class2 : Class3, Interface2
    {
        [System.ComponentModel.DataAnnotations.Display(Name = "bbb")]
        public new string b { get; set; }
        public async Task Get(string name)
        {
            throw new NotImplementedException();
        }
    }

    public class Class3 : Class4
    {
        [System.ComponentModel.DataAnnotations.Display(Name = "b")]
        public string b { get; set; }
        public string c { get; set; }
    }

    public class Class4
    {
        public string F { get; set; }

        public async Task Get3(string name)
        {
            throw new NotImplementedException();
        }
    }
}