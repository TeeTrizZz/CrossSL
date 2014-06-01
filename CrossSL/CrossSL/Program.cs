using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;

namespace CrossSL
{
    internal class Program
    {
        /*
        private static List<int> GetShaderTypes()
        {

        }
         */

        public struct ShaderDesc
        {
            public TypeDefinition TypeDef;
            public xSLTarget Target;
            public string Version;
            public xSLDebug DebugFlag;
        }

        private static List<ShaderDesc> _shaderDescs;

        private static void Main(string[] args)
        {
            var inputPath = @"..\..\..\Test\XCompTests.exe";

            // if (args.Length == 0)
            //    return;

            // Read Assembly by Reflection
            var asmRes = new DefaultAssemblyResolver();
            var readParams = new ReaderParameters {AssemblyResolver = asmRes};
            var asm = AssemblyDefinition.ReadAssembly(inputPath, readParams);

            // Find all Types with xSLShader as Base Type
            _shaderDescs = new List<ShaderDesc>();

            var asmTypes = asm.Modules.SelectMany(
                asmMod => asmMod.Types.Where(asmType => asmType.BaseType != null));
            var asmShaderTypes = asmTypes.Where(
                asmType => asmType.BaseType.Name == "xSLShader");

            foreach (var asmType in asmShaderTypes)
            {
                Console.WriteLine("Found a Shader called \"" + asmType.Name + "\":");

                var desc = new ShaderDesc {TypeDef = asmType};

                // Check for [xSLTarget] and gather Settings
                var targetAttr =
                    asmType.CustomAttributes.FirstOrDefault(attr => attr.AttributeType.Name == "xSLTargetAttribute");

                if (targetAttr == null)
                {
                    desc.Target = xSLTarget.GLSL;
                    desc.Version = xSLVersion.GLSL[0];

                    Console.WriteLine("  => Warning: Couldn't find [xSLTarget]. Compiling Shader as GLSL 1.1.");
                }
                else
                {
                    var typeName = targetAttr.ConstructorArguments[0].Type.Name;
                    var versionID = (int) targetAttr.ConstructorArguments[0].Value;

                    switch (typeName)
                    {
                        case "GLSL":
                            desc.Target = xSLTarget.GLSL;
                            desc.Version = xSLVersion.GLSL[versionID];

                            break;

                        case "GLSLES":
                            desc.Target = xSLTarget.GLSLES;
                            desc.Version = xSLVersion.GLSLES[versionID];

                            break;
                    }

                    Console.WriteLine("  => Found [xSLTarget]. Compiling Shader as " + typeName + " " + desc.Version + ".");
                }

                // Check for [xSLDebug] and gather Settings
                var debugAttr =
                    asmType.CustomAttributes.FirstOrDefault(attr => attr.AttributeType.Name == "xSLDebugAttribute");

                if (debugAttr == null)
                {
                    desc.DebugFlag = xSLDebug.None;

                    Console.WriteLine("  => Warning: Couldn't find [xSLDebug]. Debugging has been disabled.");
                }
                else
                {
                    desc.DebugFlag = (xSLDebug) debugAttr.ConstructorArguments[0].Value;

                    Console.WriteLine("  => Found [xSLDebug]. Debugging with Flags: " + desc.DebugFlag);
                }

                // Save this Description
                _shaderDescs.Add(desc);

                Console.Write("\n");
            }

            Console.ReadLine();
        }
    }
}