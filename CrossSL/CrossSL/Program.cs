using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.Ast;
using ICSharpCode.NRefactory.CSharp;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Pdb;
using XCompTests;

namespace CrossSL
{
    internal class Program
    {
        public struct VariableDesc
        {
            public FieldDefinition FieldDef;
            public PropertyDefinition PropDef;
            public xSLVariableAttr Attribute;
            public Type DataType;
            public object Value;
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

        private static bool MethodExists(TypeDefinition asmType, string methodName, out MethodDefinition method)
        {
            var methodCount = asmType.Methods.Count(asmMethod => asmMethod.Name == methodName);
            method = asmType.Methods.FirstOrDefault(asmMethod => asmMethod.Name == methodName);

            if (methodCount <= 1 && method != null && method.IsVirtual) return true;

            var instr = (method != null) ? method.Body.Instructions[0] : null;
            xSLHelper.WriteToConsole("    => ERROR: You didn't override method \"" + methodName + "\" properly", instr);

            return false;
        }

        private static void Main(string[] args)
        {
            xSLDataType.UpdateTypes();

            //var inputPath = @"..\..\..\Test\XCompTests.exe";
            const string inputPath =
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
            xSLHelper.Verbose = File.Exists(debugFile);

            Console.WriteLine(xSLHelper.Verbose
                ? "Found a .pdb file. This allows for better debugging.\n"
                : "No .pdb file found. Extended debugging has been disabled.\n");

            // read assembly (and symbols) with Mono.Cecil
            var readParams = new ReaderParameters { ReadSymbols = xSLHelper.Verbose };
            var asm = AssemblyDefinition.ReadAssembly(inputPath, readParams);

            // find all types with xSLShader as base type
            _shaderDescs = new List<ShaderDesc>();

            var asmTypes = asm.Modules.SelectMany(
                asmMod => asmMod.Types.Where(asmType => asmType.BaseType != null));
            var asmShaderTypes = asmTypes.Where(
                asmType => xSLHelper.ResolveRef(asmType.BaseType) == typeof (xSLShader));

            foreach (var asmType in asmShaderTypes)
            {
                if (xSLHelper.Verbose)
                {
                    // load symbols from pdb file
                    var asmModule = asmType.Module;
                    var symbReader = new PdbReaderProvider().GetSymbolReader(asmModule, debugFile);

                    asmModule.ReadSymbols(symbReader);
                }

                Console.WriteLine("\nFound a shader called \"" + asmType.Name + "\":");

                var shaderDesc = new ShaderDesc {TypeDef = asmType};

                // check for [xSLTarget] and gather settings
                var targetAttr =
                    asmType.CustomAttributes.FirstOrDefault(
                        attrType => xSLHelper.ResolveRef(attrType.AttributeType) == typeof (xSLTargetAttribute));

                if (targetAttr == null)
                {
                    shaderDesc.Target = new xSLShaderTarget
                    {Target = xSLTarget.GLSL, Version = 0};

                    Console.WriteLine("  => WARNING: Couldn't find [xSLTarget]. Compiling shader as GLSL 1.1.");
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
                    asmType.CustomAttributes.FirstOrDefault(
                        attrType => xSLHelper.ResolveRef(attrType.AttributeType) == typeof (xSLDebugAttribute));

                if (debugAttr == null)
                {
                    shaderDesc.DebugFlags = xSLDebug.None;
                    Console.WriteLine("  => WARNING: Couldn't find [xSLDebug]. Debugging has been disabled.");
                }
                else
                {
                    shaderDesc.DebugFlags = (xSLDebug) debugAttr.ConstructorArguments[0].Value;
                    Console.WriteLine("  => Found [xSLDebug]. Debugging with flags: " + shaderDesc.DebugFlags);
                }

                // check for common mistakes
                Console.WriteLine("\n  1. Checking shader for obvious mistakes.");

                // check if vertex or fragment method is missing
                MethodDefinition vertexMain;
                if (!MethodExists(asmType, "VertexShader", out vertexMain)) continue;

                MethodDefinition fragmentMain;
                if (!MethodExists(asmType, "FragmentShader", out fragmentMain)) continue;

                // check if there are additional constructors for field/property initialization
                var ctorMethods = asmType.Methods.Where(asmMethod => asmMethod.IsConstructor);

                foreach (var ctorMethod in ctorMethods.Where(ctor => ctor.Body.CodeSize > 7))
                {
                    var foundValidContent = false;

                    // see if there are field initializations (as for "constants")
                    var varInits = ctorMethod.Body.Instructions.Where(instr => instr.OpCode == OpCodes.Stfld);

                    foreach (var varInit in varInits)
                    {
                        var fdDecl = (FieldDefinition) varInit.Operand;
                        var declType = fdDecl.DeclaringType;
                        var isConst = fdDecl.CustomAttributes.Any(
                            attrType => xSLHelper.ResolveRef(attrType.AttributeType) == typeof (xSLConstantAttribute));

                        if (declType != asmType) continue;

                        if (!isConst)
                            xSLHelper.WriteToConsole("    => WARNING: Field \"" + fdDecl.Name + "\" is initialized" +
                                                     " but not marked as const or [xSLConstant]", varInit);

                        foundValidContent = true;
                    }

                    // see if there are property setter calls (as for "constants")
                    varInits = ctorMethod.Body.Instructions.Where(instr => instr.OpCode == OpCodes.Call);

                    foreach (var varInit in varInits.Where(instr => ((MethodDefinition) instr.Operand).IsSetter))
                    {
                        var methDecl = (MethodDefinition) varInit.Operand;
                        var declType = methDecl.DeclaringType;
                        var propName = methDecl.Name.Remove(0, 4);

                        if (xSLHelper.ResolveRef(declType) == typeof (xSLShader))
                            xSLHelper.WriteToConsole(
                                "    => ERROR: Illegal use of \"" + propName + "\" in a constructor", varInit);
                        else if (declType == asmType)
                        {
                            var propDecl = asmType.Properties.First(prop => prop.Name == propName);
                            var isConst = propDecl.CustomAttributes.Any(
                                attrType =>
                                    xSLHelper.ResolveRef(attrType.AttributeType) == typeof (xSLConstantAttribute));

                            if (!isConst)
                                xSLHelper.WriteToConsole("    => WARNING: Property \"" + propDecl.Name + "\" is" +
                                                         "initialized but not marked as const or [xSLConstant]", varInit);

                            foundValidContent = true;
                        }
                    }

                    if (!foundValidContent)
                    {
                        var instr = ctorMethod.Body.Instructions[0];
                        xSLHelper.WriteToConsole("    => WARNING: Found a constructor with no valid content", instr);
                    }
                }

                // analyze variables used in shader
                Console.WriteLine("\n  2. Collecting information about fields and properties.");

                shaderDesc.Variables = new List<VariableDesc>();
                var varTypes = Enum.GetNames(typeof (xSLVariableAttr));

                // read and gather fields
                foreach (var asmField in asmType.Fields)
                {
                    var varDesc = new VariableDesc {FieldDef = asmField};

                    var attrCt = asmField.CustomAttributes.Count(attr => varTypes.Contains(attr.AttributeType.Name));
                    var validFd = asmField.HasConstant ^ attrCt == 1;

                    if (validFd)
                    {
                        // resolve type of variable
                        var fdAttr = xSLVariableAttr.xSLConstantAttribute;

                        if (!asmField.HasConstant)
                        {
                            var fdAttrName =
                                asmField.CustomAttributes.First(attr => varTypes.Contains(attr.AttributeType.Name));
                            fdAttr = (xSLVariableAttr) Array.IndexOf(varTypes, fdAttrName.AttributeType.Name);
                        }
                        else
                            varDesc.Value = asmField.Constant;

                        // resolve data type of variable
                        var fdType = xSLHelper.ResolveRef(asmField.FieldType);

                        if (fdType == null || !xSLDataType.Types.ContainsKey(fdType))
                        {
                            var strAdd = (fdType != null)
                                ? " type \"" + fdType.Name + "\" "
                                : " a type ";

                            Console.WriteLine("    => ERROR: Field \"" + asmField.Name +
                                              "\" uses" + strAdd + "which is not supported.");
                        }

                        varDesc.DataType = fdType;
                        varDesc.Attribute = fdAttr;

                        shaderDesc.Variables.Add(varDesc);
                    }
                    else
                        Console.WriteLine("    => WARNING: Field \"" + asmField.Name +
                                          "\" is neither a constant nor has a valid attribute.");
                }

                // read and gather properties
                foreach (var asmProp in asmType.Properties)
                {
                    var attrCt = asmProp.CustomAttributes.Count(attr => varTypes.Contains(attr.AttributeType.Name));

                    if (attrCt == 1)
                    {
                        // resolve type of variable
                        var prAttrName =
                            asmProp.CustomAttributes.First(attr => varTypes.Contains(attr.AttributeType.Name));
                        var prAttr = (xSLVariableAttr) Array.IndexOf(varTypes, prAttrName.AttributeType.Name);

                        // resolve data type of variable
                        var prType = xSLHelper.ResolveRef(asmProp.PropertyType);

                        if (prType == null || !xSLDataType.Types.ContainsKey(prType))
                        {
                            var strAdd = (prType != null)
                                ? " type \"" + prType.Name + "\" "
                                : " a type ";

                            Console.WriteLine("    => ERROR: Field \"" + asmProp.Name +
                                              "\" uses" + strAdd + "which is not supported.");
                        }

                        var varDesc = new VariableDesc {PropDef = asmProp, DataType = prType, Attribute = prAttr};
                        shaderDesc.Variables.Add(varDesc);
                    }
                    else
                        Console.WriteLine("    => WARNING: Property \"" + asmProp.Name +
                                          "\" is neither a constant nor has a valid attribute.");
                }

                // translate both main methods
                Console.WriteLine("\n  3. Translating...");

                Translate(vertexMain);
                Translate(fragmentMain);

                Console.Write("\n");
            }

            Console.ReadLine();
        }

        private static void Translate(MethodDefinition method)
        {
            // create DecompilerContext for given method
            var type = method.DeclaringType;
            var module = method.DeclaringType.Module;

            var decContext = new DecompilerContext(module)
            {
                CurrentType = type,
                CurrentMethod = method
            };

            // create AST for method and start traversing
            var methodBody = AstMethodBodyBuilder.CreateMethodBody(method, decContext);
            var translator = new GLSLVisitor(methodBody, decContext);

            Console.WriteLine(translator.Result);
        }
    }
}