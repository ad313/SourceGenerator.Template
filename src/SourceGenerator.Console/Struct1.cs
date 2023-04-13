using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SourceGenerator.Consoles
{
    internal partial struct Struct1: Interface1
    {
        public int Type { get; set; }

        public void ss(){}
        public async Task Get(string name)
        {
            throw new NotImplementedException();
        }

        public string b { get; set; }
        public string a { get; set; }
    }

    internal partial struct Struct1 
    {
        public string c { get; set; }
    }

    internal struct Struct2 
    {
        public string? b1 { get; set; }
        public string? a1 { get; set; }

        public Struct2(string a,string b)
        {
            a1 = a;
            b1 = b;
        }
    }
}
