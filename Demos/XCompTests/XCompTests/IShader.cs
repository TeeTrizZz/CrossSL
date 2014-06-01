using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XCompTests
{
    interface IShader
    {
        dynamic glPosition { get; set; }
        void FragmentShader();
    }

    interface IShader2
    {
        void FragmentShader();        
    }
}
