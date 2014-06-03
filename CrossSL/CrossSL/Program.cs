using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Mono.Cecil;
using Mono.Cecil.Cil;
using XCompTests;

namespace CrossSL
{
    internal class Program
    {
        private static bool _verbose;

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
            public bool CustomCtor;
        }

        private static List<ShaderDesc> _shaderDescs;

        /// <summary>
        ///     Resolves the type of a given TypeReference via Mono.Cecil and System.Reflection.
        /// </summary>
        /// <param name="typeRef">The type reference to resolve.</param>
        /// <returns>The resolved type.</returns>
        private static Type ResolveReference(TypeReference typeRef)
        {
            var typeDef = typeRef.Resolve();
            var typeName = Assembly.CreateQualifiedName(typeDef.Module.Assembly.FullName, typeDef.FullName);
            return Type.GetType(typeName.Replace('/', '+'));
        }

        private static void WriteToConsole(string msg, Instruction instr)
        {
            Console.Write(msg);

            if (instr != null && _verbose)
            {
                var findSeq = instr;

                while (findSeq.SequencePoint == null && findSeq.Previous != null)
                    findSeq = findSeq.Previous;

                if (findSeq.SequencePoint != null)
                {
                    var doc = findSeq.SequencePoint.Document.Url;
                    var line = findSeq.SequencePoint.StartLine;

                    Console.Write(" (" + Path.GetFileName(doc) + ":" + line + ")");
                }
            }

            Console.WriteLine(".");            
        }

        private static void Main(string[] args)
        {
            xSLDataType.UpdateTypes();

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
            _verbose = File.Exists(debugFile);

            Console.WriteLine(_verbose
                ? "Found a .pdb file. This allows for better debugging.\n"
                : "No .pdb file found. Extended debugging has been disabled.\n");

            // read assembly (and symbols) with Mono.Cecil
            var readParams = new ReaderParameters { ReadSymbols = _verbose };
            var asm = AssemblyDefinition.ReadAssembly(inputPath, readParams);

            // find all types with xSLShader as base type
            _shaderDescs = new List<ShaderDesc>();

            var asmTypes = asm.Modules.SelectMany(
                asmMod => asmMod.Types.Where(asmType => asmType.BaseType != null));
            var asmShaderTypes = asmTypes.Where(
                asmType => ResolveReference(asmType.BaseType) == typeof (xSLShader));

            foreach (var asmType in asmShaderTypes)
            {
                Console.WriteLine("\nFound a shader called \"" + asmType.Name + "\":");
                
                var shaderDesc = new ShaderDesc {TypeDef = asmType};

                // check for [xSLTarget] and gather settings
                var targetAttr =
                    asmType.CustomAttributes.FirstOrDefault(
                        attrType => ResolveReference(attrType.AttributeType) == typeof (xSLTargetAttribute));

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
                    asmType.CustomAttributes.FirstOrDefault(
                        attrType => ResolveReference(attrType.AttributeType) == typeof (xSLDebugAttribute));

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

                // check if there are additional constructors for field/property initialization
                shaderDesc.CustomCtor = false;

                var ctorMethods = asmType.Methods.Where(asmMethod => asmMethod.IsConstructor);
                foreach (var ctorMethod in ctorMethods.Where(ctor => ctor.Body.CodeSize > 7))
                {
                    // see if there are field initializations (as for "constants")
                    var varInits = ctorMethod.Body.Instructions.Where(instr => instr.OpCode == OpCodes.Stfld);

                    foreach (var varInit in varInits)
                    {
                        var fdDecl = (FieldDefinition) varInit.Operand;
                        var declType = fdDecl.DeclaringType;
                        var isConst = fdDecl.CustomAttributes.Any(
                            attrType => ResolveReference(attrType.AttributeType) == typeof (xSLConstantAttribute));

                        if (declType != asmType) continue;

                        if (!isConst)
                            WriteToConsole("    => Warning: Field \"" + fdDecl.Name +
                                           "\" is initialized but not marked as const or [xSLConstant]", varInit);

                        shaderDesc.CustomCtor = true;
                    }

                    // see if there are property setter calls (as for "constants")
                    varInits = ctorMethod.Body.Instructions.Where(instr => instr.OpCode == OpCodes.Call);

                    foreach (var varInit in varInits.Where(instr => ((MethodDefinition) instr.Operand).IsSetter))
                    {
                        var methDecl = (MethodDefinition) varInit.Operand;
                        var declType = methDecl.DeclaringType;
                        var propName = methDecl.Name.Remove(0, 4);

                        if (ResolveReference(declType) == typeof (xSLShader))
                            WriteToConsole("    => Error: Illegal use of \"" + propName + "\" in a constructor", varInit);
                        else if (declType == asmType)
                        {
                            var propDecl = asmType.Properties.First(prop => prop.Name == propName);
                            var isConst = propDecl.CustomAttributes.Any(
                                attrType => ResolveReference(attrType.AttributeType) == typeof (xSLConstantAttribute));

                            if (!isConst)
                                WriteToConsole("    => Warning: Property \"" + propDecl.Name +
                                               "\" is initialized but not marked as const or [xSLConstant]", varInit);

                            shaderDesc.CustomCtor = true;
                        }
                    }

                    if (!shaderDesc.CustomCtor)
                    {
                        var instr = ctorMethod.Body.Instructions[0];   
                        WriteToConsole("    => Warning: There is an unsupported constructor", instr);
                    }
                }

                // check if vertex or fragment method is missing, etc.
                var vertexMainCt = asmType.Methods.Count(asmMethod => asmMethod.Name == "VertexShader");
                var vertexMain = asmType.Methods.FirstOrDefault(asmMethod => asmMethod.Name == "VertexShader");

                if (vertexMainCt > 1 || vertexMain == null || !vertexMain.IsVirtual) {
                    if (vertexMain != null)
                    {
                        var instr = vertexMain.Body.Instructions[0];
                        WriteToConsole("    => Error: You didn't override method \"FragmentShader\" properly", instr);
                    }

                    continue;
                }

                var fragMainCt = asmType.Methods.Count(asmMethod => asmMethod.Name == "FragmentShader");
                var fragMain = asmType.Methods.FirstOrDefault(asmMethod => asmMethod.Name == "FragmentShader");

                if (fragMainCt > 1 || fragMain == null || !fragMain.IsVirtual)
                {
                    if (fragMain != null)
                    {
                        var instr = fragMain.Body.Instructions[0];
                        WriteToConsole("    => Error: You didn't override method \"FragmentShader\" properly", instr);
                    }

                    continue;
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

                    if (!validFd)
                    {
                        Console.WriteLine("    => Warning: Field \"" + asmField.Name +
                                          "\" is neither a constant nor has a valid attribute.");
                        continue;
                    }

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
                    var fdType = ResolveReference(asmField.FieldType);

                    if (fdType == null || !xSLDataType.Types.Contains(fdType))
                    {
                        var strAdd = (fdType != null)
                            ? " type \"" + fdType.Name + "\" " : " a type ";

                        Console.WriteLine("    => Error: Field \"" + asmField.Name +
                                          "\" uses" + strAdd + "which is not supported.");
                    }

                    varDesc.DataType = fdType;
                    varDesc.Attribute = fdAttr;
                    
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

                    // resolve type of variable
                    var prAttrName = asmProp.CustomAttributes.First(attr => varTypes.Contains(attr.AttributeType.Name));
                    var prAttr = (xSLVariableAttr) Array.IndexOf(varTypes, prAttrName.AttributeType.Name);

                    // resolve data type of variable
                    var prType = ResolveReference(asmProp.PropertyType);

                    if (prType == null || !xSLDataType.Types.Contains(prType))
                    {
                        var strAdd = (prType != null)
                            ? " type \"" + prType.Name + "\" " : " a type ";

                        Console.WriteLine("    => Error: Field \"" + asmProp.Name +
                                          "\" uses" + strAdd + "which is not supported.");
                    }

                    var varDesc = new VariableDesc {PropDef = asmProp, DataType = prType, Attribute = prAttr};
                    shaderDesc.Variables.Add(varDesc);
                }

                Console.Write("\n");
            }

            Console.ReadLine();
        }
    }
}