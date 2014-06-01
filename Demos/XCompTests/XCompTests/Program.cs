using System;
using System.Diagnostics;

namespace XCompTests
{
    class Program
    {
        static void Main(string[] args)
        {
            Debug.WriteLine("DiffuseShader.cs(18,20): The variable 'foo' is declared but never used");


            Console.WriteLine(xSL<SimpleTexShader>.FragmentShader);
            Console.WriteLine(xSL<SimpleTexShader>.VertexShader);

            //var shader = xSL<MyShader>.ShaderObject;

            //shader.FUSEE_ITMV = new float4x4(/* ... */);
            //shader.FuUV = new float2(0, 0);
            //shader.FragmentShader();

            Console.ReadLine();
        }
    }
}
