using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.Ast;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Pdb;
using Mono.Collections.Generic;
using Mono.CSharp;
using XCompTests;
using AssemblyDefinition = Mono.Cecil.AssemblyDefinition;
using Enum = System.Enum;
using IMemberDefinition = Mono.Cecil.IMemberDefinition;
using TypeDefinition = Mono.Cecil.TypeDefinition;

namespace CrossSL
{
    internal class Program
    {
        public struct FunctionDesc
        {
            public MethodDefinition Definion;
            public StringBuilder Signature;
            public StringBuilder Body;
            public IEnumerable<IMemberDefinition> Variables;
        }

        public struct VariableDesc
        {
            public IMemberDefinition Definion;
            public xSLVariableType Attribute;
            public Type DataType;
            public object Value;
            public bool IsReferenced;
        }

        public struct ShaderTarget
        {
            public xSLEnvironment Envr;
            public int Version;
        }

        public struct ShaderDesc
        {
            public string Name;
            public TypeDefinition Type;
            public ShaderTarget Target;
            public xSLDebug DebugFlags;
            public bool Aborted;
            public IEnumerable<VariableDesc> Variables;
            public IEnumerable<FunctionDesc>[] Funcs;
            public IEnumerable<Instruction> Instructions;
        }

        private static bool MethodExists(TypeDefinition asmType, string methodName, out MethodDefinition method)
        {
            var methodCount = asmType.Methods.Count(asmMethod => asmMethod.Name == methodName);
            method = asmType.Methods.FirstOrDefault(asmMethod => asmMethod.Name == methodName);

            if (methodCount <= 1 && method != null && method.IsVirtual) return true;

            var instr = (method != null) ? method.Body.Instructions[0] : null;
            xSLHelper.Error("You didn't override method '" + methodName + "' properly", instr);

            return false;
        }

        private static void Main(string[] args)
        {
            // update available data types & co.
            xSLDataType.UpdateTypes();
            xSLMathMapping.UpdateTypes();

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
                ? "Found a .pdb file. This allows for better debugging."
                : "No .pdb file found. Extended debugging has been disabled.");

            // read assembly (and symbols) with Mono.Cecil
            var readParams = new ReaderParameters {ReadSymbols = xSLHelper.Verbose};
            var asm = AssemblyDefinition.ReadAssembly(inputPath, readParams);

            // find all types with xSLShader as base type
            var shaderDescs = new List<ShaderDesc>();

            var asmTypes = asm.Modules.SelectMany(
                asmMod => asmMod.Types.Where(asmType => asmType.BaseType != null));
            var asmShaderTypes = asmTypes.Where(asmType => asmType.BaseType.IsType<xSLShader>());

            foreach (var asmType in asmShaderTypes)
            {
                xSLHelper.Abort = false;

                if (xSLHelper.Verbose)
                {
                    // load symbols from pdb file
                    var asmModule = asmType.Module;
                    var symbReader = new PdbReaderProvider().GetSymbolReader(asmModule, debugFile);

                    asmModule.ReadSymbols(symbReader);
                }

                Console.WriteLine("\n\nFound a shader called '" + asmType.Name + "':");

                var shaderDesc = new ShaderDesc {Name = asmType.Name, Type = asmType};

                // check for [xSLDebug] first in case the shader should be ignored
                var debugAttr = asmType.CustomAttributes.FirstOrDefault(
                    attrType => attrType.AttributeType.IsType<xSLDebugAttribute>());

                if (debugAttr != null)
                {
                    shaderDesc.DebugFlags = (xSLDebug) debugAttr.ConstructorArguments[0].Value;

                    if ((shaderDesc.DebugFlags & xSLDebug.IgnoreShader) != 0)
                    {
                        Console.WriteLine("  => Found [xSLDebug] with 'IgnoreShader' flag. Shader has been ignored.");
                        continue;
                    }
                }

                // check for [xSLTarget] and save settings
                var targetAttr = asmType.CustomAttributes.FirstOrDefault(
                    attr => attr.AttributeType.IsType<xSLTargetAttribute>());

                if (targetAttr == null)
                {
                    shaderDesc.Target = new ShaderTarget
                    {Envr = xSLEnvironment.GLSL, Version = 0};

                    Console.WriteLine("  => WARNING: Couldn't find [xSLTarget]. Compiling shader as GLSL 1.1.");
                }
                else
                {
                    var typeName = targetAttr.ConstructorArguments[0].Type.Name;
                    var versionID = (int) targetAttr.ConstructorArguments[0].Value;

                    var shaderTarget = new ShaderTarget {Version = versionID};

                    switch (typeName)
                    {
                        case "GLSL":
                            shaderTarget.Envr = xSLEnvironment.GLSL;
                            break;

                        case "GLSLES":
                            shaderTarget.Envr = xSLEnvironment.GLSLES;
                            break;
                    }

                    shaderDesc.Target = shaderTarget;

                    var vStr = xSLVersion.VIDs[(int)shaderTarget.Envr][versionID];
                    Console.WriteLine("  => Found [xSLTarget]. Compiling shader as " + typeName + " " + vStr + ".");
                }

                // save debug settings
                if (debugAttr == null)
                {
                    shaderDesc.DebugFlags = xSLDebug.None;
                    Console.WriteLine("  => WARNING: Couldn't find [xSLDebug]. Debugging has been disabled.");
                }
                else
                {
                    if ((shaderDesc.DebugFlags & xSLDebug.None) != 0)
                        Console.WriteLine("  => Found [xSLDebug] with 'None' flag. Debugging has been disabled.");
                    else
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
                            attr => attr.AttributeType.IsType<xSLConstAttribute>());

                        if (declType != asmType) continue;

                        if (!isConst)
                            xSLHelper.Warning("Field '" + fdDecl.Name + "' is initialized" +
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

                        if (declType.IsType<xSLShader>())
                            xSLHelper.Error("Illegal use of '" + propName + "' in a constructor", varInit);
                        else if (declType == asmType)
                        {
                            var propDecl = asmType.Properties.First(prop => prop.Name == propName);
                            var isConst = propDecl.CustomAttributes.Any(
                                attr => attr.AttributeType.IsType<xSLConstAttribute>());

                            if (!isConst)
                                xSLHelper.Warning("Property '" + propDecl.Name + "' is" +
                                                  "initialized but not marked as const or [xSLConstant]", varInit);

                            foundValidContent = true;
                        }
                    }

                    if (!foundValidContent)
                    {
                        var instr = ctorMethod.Body.Instructions[0];
                        xSLHelper.Warning("Found a constructor with no valid content", instr);
                    }
                }

                // analyze variables used in shader
                Console.WriteLine("\n  2. Collecting information about fields and properties.");

                var variables = new Collection<VariableDesc>();
                var varTypes = Enum.GetNames(typeof (xSLVariableType));

                // read and gather fields
                foreach (var asmField in asmType.Fields)
                {
                    var varDesc = new VariableDesc {Definion = asmField};

                    var attrCt = asmField.CustomAttributes.Count(attr => varTypes.Contains(attr.AttributeType.Name));
                    var validFd = asmField.HasConstant ^ attrCt == 1;

                    if (validFd)
                    {
                        var fdAttr = xSLVariableType.xSLConstAttribute;

                        if (!asmField.HasConstant)
                        {
                            var fdAttrName =
                                asmField.CustomAttributes.First(attr => varTypes.Contains(attr.AttributeType.Name));
                            fdAttr = (xSLVariableType) Array.IndexOf(varTypes, fdAttrName.AttributeType.Name);
                        }
                        else
                            varDesc.Value = asmField.Constant;

                        // resolve data type of variable
                        var fdType = asmField.FieldType.ToType();

                        if (!xSLDataType.Types.ContainsKey(fdType))
                        {
                            var strAdd = (fdType != typeof (Object)) ? " type '" + fdType.Name + "' " : " a type ";
                            xSLHelper.Error("Field '" + asmField.Name + "' is of" + strAdd + "which is not supported.");
                        }

                        varDesc.DataType = fdType;
                        varDesc.Attribute = fdAttr;

                        variables.Add(varDesc);
                    }
                    else
                        xSLHelper.Error("Field '" + asmField.Name + "' is neither a constant nor has valid attributes");
                }

                shaderDesc.Variables = variables;

                // read and gather properties
                foreach (var asmProp in asmType.Properties)
                {
                    var attrCt = asmProp.CustomAttributes.Count(attr => varTypes.Contains(attr.AttributeType.Name));

                    if (attrCt == 1)
                    {
                        var prAttrName =
                            asmProp.CustomAttributes.First(attr => varTypes.Contains(attr.AttributeType.Name));
                        var prAttr = (xSLVariableType) Array.IndexOf(varTypes, prAttrName.AttributeType.Name);

                        // resolve data type of variable
                        var prType = asmProp.PropertyType.ToType();

                        if (!xSLDataType.Types.ContainsKey(prType))
                        {
                            var strAdd = (prType != typeof (Object)) ? " type '" + prType.Name + "' " : " a type ";
                            xSLHelper.Error("Property '" + asmProp.Name + "' is of" + strAdd + "which is not supported.");
                        }

                        var varDesc = new VariableDesc {Definion = asmProp, DataType = prType, Attribute = prAttr};
                        variables.Add(varDesc);
                    }
                    else
                        xSLHelper.Error("Property '" + asmProp.Name + "' is neither a constant nor has valid attributes");
                }

                // translate main and depending methods
                Console.WriteLine("\n  3. Translating shader from C# to " + shaderDesc.Target.Envr + ".");

                shaderDesc.Funcs = new IEnumerable<FunctionDesc>[2];

                var vertexFuncs = Translate(vertexMain);
                if (vertexFuncs == null) continue;
                shaderDesc.Funcs[(int) xSLShaderType.VertexShader] = vertexFuncs;

                var fragmentFuncs = Translate(fragmentMain);
                if (fragmentFuncs == null) continue;
                shaderDesc.Funcs[(int) xSLShaderType.FragmentShader] = fragmentFuncs;

                // build both shaders
                Console.WriteLine("\n  4. Building vertex and fragment shader.");

                var vertexResult = BuildShader(ref shaderDesc, xSLShaderType.VertexShader);
                var fragmentResult = BuildShader(ref shaderDesc, xSLShaderType.FragmentShader);

                // see if there are unused fields/properties
                var unusedVars = shaderDesc.Variables.Where(var => !var.IsReferenced);

                foreach (var unsedVar in unusedVars)
                    xSLHelper.Warning("'" + unsedVar.Definion.Name + "' was declared but is not used");

                // debugging: precompile first then save to file
                Console.WriteLine("\n  5. Applying debugging flags if any.");

                if ((xSLDebug.PreCompile & shaderDesc.DebugFlags) != 0)
                {
                    // test test
                }

                if ((xSLDebug.SaveToFile & shaderDesc.DebugFlags) != 0)
                {
                    var directiory = Path.GetDirectoryName(inputPath);

                    if (directiory != null)
                    {
                        var combined = new StringBuilder("---- VertexShader ----").NewLine(2);
                        combined.Append(vertexResult).NewLine(3).Append("---- FragmentShader ----");
                        combined.NewLine(2).Append(fragmentResult);

                        var filePath = Path.Combine(directiory, shaderDesc.Name + ".txt");
                        File.WriteAllText(filePath, combined.ToString());

                        Console.WriteLine("    => Saved shader to: '" + filePath + "'");
                    }
                }

                // save shaders into the assembly
                Console.WriteLine("\n  6. Preparing to write shaders into the assembly.");

                var genShader = asmType.Module.Types.First(type => type.ToType() == typeof (xSL<>));
                var instShader = new GenericInstanceType(genShader);
                instShader.GenericArguments.Add(asmType);

                var genVField = genShader.Fields.First(field => field.Name == "_vertex");
                var vField = new FieldReference(genVField.Name, genVField.FieldType)
                {
                    DeclaringType = instShader
                };

                var genFField = genShader.Fields.First(field => field.Name == "_fragment");
                var fField = new FieldReference(genFField.Name, genFField.FieldType)
                {
                    DeclaringType = instShader
                };

                var genTField = genShader.Fields.First(field => field.Name == "_translated");
                var tField = new FieldReference(genTField.Name, genTField.FieldType)
                {
                    DeclaringType = instShader
                };

                var ldVStr = Instruction.Create(OpCodes.Ldstr, vertexResult.ToString());
                var stVStr = Instruction.Create(OpCodes.Stsfld, vField);

                var ldFStr = Instruction.Create(OpCodes.Ldstr, fragmentResult.ToString());
                var stFStr = Instruction.Create(OpCodes.Stsfld, fField);

                var ldTInt = Instruction.Create(OpCodes.Ldc_I4_1);
                var stTInt = Instruction.Create(OpCodes.Stsfld, tField);

                var instrList = new List<Instruction> { ldVStr, stVStr, ldFStr, stFStr, ldTInt, stTInt };
                shaderDesc.Instructions = instrList;

                Console.WriteLine(xSLHelper.Abort
                    ? "\n  ---- Translation failed ----"
                    : "\n  ---- Translation succeeded ----");

                shaderDesc.Aborted = xSLHelper.Abort;
                shaderDescs.Add(shaderDesc);
            }

            // write shaders into assembly
            var validCt = shaderDescs.Count(shader => shader.Aborted);

            if (validCt == shaderDescs.Count)
                Console.WriteLine("\nAssembly will not be updated as no shader was translated successfully.");
            else
            {
                Console.WriteLine(validCt > 0
                    ? "\n\nAdding all successfully translated shader to the assembly:"
                    : "\n\nAdding all translated shaders to the assembly:");

                var filtDescs = shaderDescs.Where(shader => !shader.Aborted).ToList();
                var moduleList = filtDescs.Select(shader => shader.Type.Module).Distinct();

                foreach (var module in moduleList)
                {
                    var descs = filtDescs.Where(shader => shader.Type.Module == module).ToList();
                    var instrs = descs.SelectMany(shaderDesc => shaderDesc.Instructions);

                    var asmModule = descs.First().Type.Module;
                    var genShader = asmModule.Types.First(type => type.ToType() == typeof(xSL<>));

                    var xSLInit = genShader.Methods.First(method => method.Name == "Init");
                    var ilProc = xSLInit.Body.GetILProcessor();

                    xSLInit.Body.Instructions.Clear();

                    foreach (var instr in instrs)
                        ilProc.Append(instr);

                    var ret = Instruction.Create(OpCodes.Ret);
                    ilProc.Append(ret);

                    try
                    {
                        asmModule.Write(inputPath);

                        foreach (var shaderDesc in descs)
                            Console.WriteLine("  => Sucessfully added '" + shaderDesc.Name + "' to assembly.");

                    }
                    catch (IOException)
                    {
                        foreach (var shaderDesc in descs)
                            Console.WriteLine("  => Cannot write shader '" + shaderDesc.Name +
                                            "' into assembly. File might be read-only or in use.");
                    }
                }
            }

            Console.WriteLine("\n\nDone.");

            Console.ReadLine();
        }

        private static StringBuilder BuildShader(ref ShaderDesc shaderDescRef, xSLShaderType shaderType)
        {
            var result = new StringBuilder();
            var shaderDesc = shaderDescRef;

            // corresponding functions
            var functions = shaderDesc.Funcs[(int) shaderType];

            // collect all referenced variables
            var refVars = new HashSet<IMemberDefinition>();

            foreach (var func in functions.Where(func => func.Variables != null))
                refVars.UnionWith(func.Variables);

            var allVars = shaderDesc.Variables.ToList();
            var memberVars = refVars.Where(def => def.DeclaringType.ToType() == shaderDesc.Type.ToType());
            var varDescs = new Collection<VariableDesc>();

            foreach (var memberVar in memberVars)
            {
                var globIndex = allVars.FindIndex(var => var.Definion == memberVar);
                var globVar = allVars.First(var => var.Definion == memberVar);

                varDescs.Add(globVar);

                globVar.IsReferenced = true;
                allVars[globIndex] = globVar;
            }

            shaderDesc.Variables = allVars;

            // add variables to shader output
            foreach (var varDesc in varDescs.OrderBy(var => var.Attribute))
            {
                var dataType = xSLDataType.Types[varDesc.DataType];

                var varType = varDesc.Attribute.ToString().ToLower();
                varType = varType.Remove(0, 3).Remove(varType.Length - 12);

                result.Append(varType).Space().Append(dataType).Space();
                result.Append(varDesc.Definion.Name).Semicolon().NewLine();
            }

            result.Length -= 2;

            // check if invalid variables are set
            var nestedTypes = typeof (xSLShader).GetNestedTypes(BindingFlags.NonPublic);

            var attrType = nestedTypes.FirstOrDefault(type => type.Name == shaderType + "Attribute");
            var mandType = nestedTypes.FirstOrDefault(type => type.Name == "MandatoryAttribute");

            var allProps = typeof (xSLShader).GetProperties(BindingFlags.Instance | BindingFlags.NonPublic);
            var validProps = allProps.Where(prop => prop.CustomAttributes.Any(attr => attr.AttributeType == attrType));
            var validNames = validProps.Select(prop => prop.Name).ToList();

            var globalVars = refVars.Where(def => def.DeclaringType.ToType() == typeof(xSLShader));
            var globalNames = globalVars.Select(var => var.Name).ToList();

            var invalidVars = globalNames.Where(var => !validNames.Contains(var));

            foreach (var memberVar in invalidVars)
                xSLHelper.Error("'" + memberVar + "' cannot be used in '" + shaderType + "()'");

            // check if necessary variables are set
            var mandVars = allProps.Where(prop => prop.CustomAttributes.Any(attr => attr.AttributeType == mandType));

            foreach (var mandVar in mandVars)
                if (validNames.Contains(mandVar.Name) && !globalNames.Contains(mandVar.Name))
                    xSLHelper.Error("'" + mandVar.Name + "' has to be set in '" + shaderType + "'");

            // add all functions to shader output
            foreach (var func in shaderDesc.Funcs[(int)shaderType])
                result.NewLine(2).Append(func.Signature).NewLine().Append(func.Body);

            shaderDescRef = shaderDesc;
            return result;
        }

        private static StringBuilder MapReturnType(MethodDefinition method)
        {
            var retType = method.ReturnType.ToType();

            if (!xSLDataType.Types.ContainsKey(retType))
            {
                var strAdd = (retType != typeof (Object)) ? " '" + retType.Name + "'" : String.Empty;

                var instr = method.Body.Instructions[0];
                xSLHelper.Error("Method has an unsupported return type" + strAdd, instr);

                return null;
            }

            return new StringBuilder(xSLDataType.Types[retType]);
        }

        private static StringBuilder JoinParams(MethodDefinition method)
        {
            var result = new StringBuilder();

            foreach (var param in method.Parameters)
            {
                var paramType = param.ParameterType.ToType();

                if (!xSLDataType.Types.ContainsKey(paramType))
                {
                    var strAdd = (paramType != typeof (Object)) ? " '" + paramType.Name + "'" : String.Empty;

                    var instr = method.Body.Instructions[0];
                    xSLHelper.Error("Method has a parameter of the unsupported type" + strAdd, instr);

                    return null;
                }

                var isRef = (param.ParameterType is ByReferenceType);
                var refStr = (isRef) ? "out " : String.Empty;

                var typeMapped = xSLDataType.Types[paramType];
                var paramName = param.Name;

                result.Append(refStr).Append(typeMapped).Space();
                result.Append(paramName).Append(", ");
            }

            if (result.Length > 0)
                result.Length -= 2;

            return result;
        }

        private static IEnumerable<FunctionDesc> Translate(MethodDefinition method)
        {
            var allFuncs = new List<FunctionDesc>();

            // build function signature
            var retTypeStr = MapReturnType(method);
            var paramStr = JoinParams(method);

            if (retTypeStr == null || paramStr == null)
                return null;

            var methodName = method.Name;
            methodName = methodName.Replace("VertexShader", "main");
            methodName = methodName.Replace("FragmentShader", "main");

            var sig = retTypeStr.Space().Method(methodName, paramStr.ToString());

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

            // save information
            var result = new FunctionDesc
            {
                Definion = method,
                Signature = sig,
                Body = translator.Result,
                Variables = translator.RefVariables
            };

            // translate all referenced methods
            foreach (var refMethod in translator.RefMethods)
                if (allFuncs.All(aMethod => aMethod.Definion != refMethod))
                    allFuncs.AddRange(Translate(refMethod));

            allFuncs.Add(result);
            return allFuncs;
        }
    }
}