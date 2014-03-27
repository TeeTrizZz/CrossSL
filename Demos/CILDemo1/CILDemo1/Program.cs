using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Win32.SafeHandles;

namespace CILDemo1
{
    public struct float2
    {
        public int x;
        public int y;
    }

    class Program
    {
        static void Main()
        {
string text = Console.ReadLine();

const float factor = 0.5f;
float result = 20 * factor;

Console.WriteLine(text + result);
            var test = new float2() {x = -1, y = 1};
            TestMethod(test);
            Console.WriteLine(test.x + " - " + test.y);
            Console.WriteLine(IsPositive(5));
            Console.ReadLine();
        }

        static float2 TestMethod(float2 val)
        {
            return new float2() { x = Math.Abs(val.x), y = Math.Abs(val.y) };
        }

        static bool IsPositive(int val)
        {
            return val > 0;
        }
    }
}
