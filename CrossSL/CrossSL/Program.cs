using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace CrossSL
{
    internal class Program
    {
        public struct VariableDesc
        {
            public FieldDefinition FieldDef;
            public PropertyDefinition PropDef;
            public xSLVariableType Type;
        }

        public struct xSLShaderTarget
        {
            public xSLTarget Target;
            public int Version;
        }

        public struct ShaderDesc
        {
            public TypeDefinition TypeDef;
            public xSLShaderTarget Target;
            public xSLDebug DebugFlags;
            public List<VariableDesc> Variables;
        }

        private static List<ShaderDesc> _shaderDescs;

        private static void Main(string[] args)
        {
            //var inputPath = @"..\..\..\Test\XCompTests.exe";
            var inputPath =
                @"E:\Dropbox\HS Furtwangen\7. Semester\Thesis\dev\Demos\XCompTests\XCompTests\bin\x86\Debug\XCompTests.exe";

            // if (args.Length == 0)
            //    return;

            if (!File.Exists(inputPath))
            {
                Console.WriteLine("File not found.");
                throw new FileNotFoundException();
            }

            // check if there is a .pdb file along with the .exe
            var debugFile = Path.ChangeExtension(inputPath, "pdb");
            var readSymbols = File.Exists(debugFile);

            Console.WriteLine(readSymbols
                ? "Found a .pdb file. This allows for better debugging.\n"
                : "No .pdb file found. Extended debugging has been disabled.\n");

            // read assembly (and symbols) with Mono.Cecil
            var readParams = new ReaderParameters {ReadSymbols = readSymbols};
            var asm = AssemblyDefinition.ReadAssembly(inputPath, readParams);

            // find all types with xSLShader as base type
            _shaderDescs = new List<ShaderDesc>();

            var asmTypes = asm.Modules.SelectMany(
                asmMod => asmMod.Types.Where(asmType => asmType.BaseType != null));
            var asmShaderTypes = asmTypes.Where(
                asmType => asmType.BaseType.Name == "xSLShader");

            foreach (var asmType in asmShaderTypes)
            {
                Console.WriteLine("\nFound a shader called \"" + asmType.Name + "\":");

                var shaderDesc = new ShaderDesc {TypeDef = asmType};

                // check for [xSLTarget] and gather settings
                var targetAttr =
                    asmType.CustomAttributes.FirstOrDefault(attr => attr.AttributeType.Name == "xSLTargetAttribute");

                if (targetAttr == null)
                {
                    shaderDesc.Target = new xSLShaderTarget
                    {Target = xSLTarget.GLSL, Version = 0};

                    Console.WriteLine("  => Warning: Couldn't find [xSLTarget]. Compiling shader as GLSL 1.1.");
                }
                else
                {
                    var typeName = targetAttr.ConstructorArguments[0].Type.Name;
                    var versionID = (int) targetAttr.ConstructorArguments[0].Value;

                    var shaderTarget = new xSLShaderTarget {Version = versionID};

                    switch (typeName)
                    {
                        case "GLSL":
                            shaderTarget.Target = xSLTarget.GLSL;
                            break;

                        case "GLSLES":
                            shaderTarget.Target = xSLTarget.GLSLES;
                            break;
                    }

                    shaderDesc.Target = shaderTarget;

                    var vStr = xSLVersion.VIDs[(int) shaderTarget.Target][versionID];
                    Console.WriteLine("  => Found [xSLTarget]. Compiling shader as " + typeName + " " + vStr + ".");
                }

                // check for [xSLDebug] and gather settings
                var debugAttr =
                    asmType.CustomAttributes.FirstOrDefault(attr => attr.AttributeType.Name == "xSLDebugAttribute");

                if (debugAttr == null)
                {
                    shaderDesc.DebugFlags = xSLDebug.None;
                    Console.WriteLine("  => Warning: Couldn't find [xSLDebug]. Debugging has been disabled.");
                }
                else
                {
                    shaderDesc.DebugFlags = (xSLDebug) debugAttr.ConstructorArguments[0].Value;
                    Console.WriteLine("  => Found [xSLDebug]. Debugging with flags: " + shaderDesc.DebugFlags);
                }

                // check for common mistakes
                Console.WriteLine("\n  1. Checking shader for obvious mistakes.");

                // check if any but non-default constructor
                var ctorCt = asmType.Methods.Count(asmMethod => asmMethod.IsConstructor);
                var ctor = asmType.Methods.First(asmMethod => asmMethod.IsConstructor);

                if (ctorCt > 1 || ctor.Body.CodeSize > 7)
                {
                    Console.WriteLine("    => Warning: You are using a constructor in your shader.");

                    // see if there is an unnecessary field initialization
                    var stFld = ctor.Body.Instructions.FirstOrDefault(instr => instr.OpCode == OpCodes.Stfld);

                    if (stFld != null)
                        Console.Write("       Do not initialize field \"" +
                                      ((FieldDefinition) stFld.Operand).Name + "\"");
                    else
                        Console.Write("       Constructor will be ignored as it is not supported");

                    if (readSymbols)
                    {
                        var doc = ctor.Body.Instructions[0].SequencePoint.Document.Url;
                        var line = ctor.Body.Instructions[0].SequencePoint.StartLine;

                        Console.Write(" (" + Path.GetFileName(doc) + ":" + line + ").");
                    }

                    Console.Write(".\n");
                }

                // check if vertex or fragment method is missing, etc.
                var vertexMainCt =  asmType.Methods.Count(asmMethod => asmMethod.Name == "VertexShader");
                var vertexMain = asmType.Methods.FirstOrDefault(asmMethod => asmMethod.Name == "VertexShader");

                if (vertexMainCt > 1 || vertexMain == null || !vertexMain.IsVirtual)
                {
                    Console.Write("    => Error: You didn't override method \"FragmentShader\" properly");

                    if (readSymbols && vertexMain != null)
                    {
                        var doc = vertexMain.Body.Instructions[0].SequencePoint.Document.Url;
                        var line = vertexMain.Body.Instructions[0].SequencePoint.StartLine;

                        Console.Write(" (" + Path.GetFileName(doc) + ":" + line + ").");
                    }

                    Console.Write(".\n");

                    continue;
                }

                var fragMainCt = asmType.Methods.Count(asmMethod => asmMethod.Name == "FragmentShader");
                var fragMain = asmType.Methods.FirstOrDefault(asmMethod => asmMethod.Name == "FragmentShader");

                if (fragMainCt > 1 || fragMain == null || !fragMain.IsVirtual)
                {
                    Console.Write("    => Error: You didn't override method \"FragmentShader\" properly");

                    if (readSymbols && fragMain != null)
                    {
                        var doc = fragMain.Body.Instructions[0].SequencePoint.Document.Url;
                        var line = fragMain.Body.Instructions[0].SequencePoint.StartLine;

                        Console.Write(" (" + Path.GetFileName(doc) + ":" + line + ").");
                    }

                    Console.Write(".\n");

                    continue;
                }

                // analyze variables used in shader
                Console.WriteLine("\n  2. Collecting information about fields and properties.");

                shaderDesc.Variables = new List<VariableDesc>();
                var varTypes = Enum.GetNames(typeof (xSLVariableType));

                // read and gather fields
                foreach (var asmField in asmType.Fields)
                {
                    var attrCt = asmField.CustomAttributes.Count(attr => varTypes.Contains(attr.AttributeType.Name));
                    var validFd = asmField.HasConstant ^ attrCt == 1;

                    if (!validFd)
                    {
                        Console.WriteLine("    => Warning: Field \"" + asmField.Name +
                                          "\" is neither a constant nor has a valid attribute.");
                        continue;
                    }

                    var fdAttr = xSLVariableType.xSLConstAttribute;

                    if (!asmField.HasConstant)
                    {
                        var fdAttrName = asmField.CustomAttributes.First(attr => varTypes.Contains(attr.AttributeType.Name));
                        fdAttr = (xSLVariableType)Array.IndexOf(varTypes, fdAttrName.AttributeType.Name);                       
                    }

                    var varDesc = new VariableDesc {FieldDef = asmField, Type = fdAttr};
                    shaderDesc.Variables.Add(varDesc);
                }

                // read and gather properties
                foreach (var asmProp in asmType.Properties)
                {
                    var attrCt = asmProp.CustomAttributes.Count(attr => varTypes.Contains(attr.AttributeType.Name));

                    if (attrCt != 1)
                    {
                        Console.WriteLine("    => Warning: Property \"" + asmProp.Name +
                                          "\" is neither a constant nor has a valid attribute.");
                        continue;
                    }

                    var prAttrName = asmProp.CustomAttributes.First(attr => varTypes.Contains(attr.AttributeType.Name));
                    var prAttr = (xSLVariableType)Array.IndexOf(varTypes, prAttrName.AttributeType.Name);

                    var varDesc = new VariableDesc { PropDef = asmProp, Type = prAttr };
                    shaderDesc.Variables.Add(varDesc);
                }

                Console.Write("\n");
            }

            Console.ReadLine();
        }
    }
}