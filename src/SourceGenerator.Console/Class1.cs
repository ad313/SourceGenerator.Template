using SourceGenerator.Consoles.Builders.ClassToProtoBuilder;
using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using SourceGenerator.Consoles.Builders.BizEnumExtendBuilder;

namespace SourceGenerator.Consoles
{
    internal class Class1
    {

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
        [Display(Name = "bbb")]
        public new string b { get; set; }
        public async Task Get(string name)
        {
            throw new NotImplementedException();
        }
    }

    public class Class3 : Class4
    {
        [Display(Name = "b")]
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