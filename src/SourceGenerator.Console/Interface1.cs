using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SourceGenerator.Consoles
{
    internal interface Interface1 : Interface2
    {
    }

    public partial interface Interface2
    {
        string a { get; set; }
    }

    public partial interface Interface2 : Interface3
    {
        string b { get; set; }
    }

    public interface Interface3
    {
        Task Get(string name);
    }
}
