using System;
using System.Diagnostics;
using CrossSL.Meta;

namespace XCompTests
{
    class Program
    {
        static void Test()
        {
            throw new Exception("Test");
        }

        static void Main(string[] args)
        {
            Debug.WriteLine("DiffuseShader.cs(18,20): The variable 'foo' is declared but never used");



        }
    }
}
