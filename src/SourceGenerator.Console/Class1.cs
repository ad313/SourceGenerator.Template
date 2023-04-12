using System;
using System.Threading.Tasks;

namespace SourceGenerator.Consoles
{
    internal class Class1
    {

    }

    public partial class Class2
    {
        public string a { get; set; }
    }

    public partial class Class2 : Class3, Interface2
    {
        public string b { get; set; }
        public async Task Get(string name)
        {
            throw new NotImplementedException();
        }
    }

    public  class Class3
    {
        public string c { get; set; }
    }
}
