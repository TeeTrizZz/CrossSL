using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using CrossSL.Meta;
using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.Ast;
using ICSharpCode.NRefactory.CSharp;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Pdb;
using Mono.Collections.Generic;

namespace CrossSL
{
    internal struct FunctionDesc
    {
        internal MethodDefinition Definion;
        internal StringBuilder Signature;
        internal StringBuilder Body;
        internal Collection<VariableDesc> Variables;
    }

    internal class VariableDesc
    {
        internal IMemberDefinition Definition;
        internal xSLVariableType Attribute;
        internal Type DataType;
        internal object Value;
        internal Instruction Instruction;
        internal bool IsReferenced;

        public override bool Equals(object obj)
        {
            var name = ((VariableDesc) obj).Definition.FullName;
            return name == Definition.FullName;
        }

        public override int GetHashCode()
        {
            // ReSharper disable once NonReadonlyFieldInGetHashCode
            return Definition.FullName.GetHashCode();
        }
    }

    internal struct ShaderTarget
    {
        internal xSLEnvironment Envr;
        internal int VersionID;
        internal int Version;
    }

    internal struct ShaderDesc
    {
        internal string Name;
        internal TypeDefinition Type;
        internal ShaderTarget Target;
        internal xSLDebug DebugFlags;
        internal CustomAttribute[] Precision;
        internal Collection<VariableDesc> Variables;
        internal IEnumerable<FunctionDesc>[] Funcs;
        internal IEnumerable<Instruction> Instructions;
    }

    internal class Program
    {
        private static bool MethodExists(TypeDefinition asmType, string methodName, out MethodDefinition method)
        {
            var methodCount = asmType.Methods.Count(asmMethod => asmMethod.Name == methodName);
            method = asmType.Methods.FirstOrDefault(asmMethod => asmMethod.Name == methodName);

            if (methodCount <= 1 && method != null && method.IsVirtual) return true;

            var instr = (method != null) ? method.Body.Instructions[0] : null;
            Helper.Error("You did not override method '" + methodName + "' properly", instr);

            return false;
        }

        private static void Main(string[] args)
        {
            // update available data types & co.
            xSLTypeMapping.UpdateTypes();
            xSLMethodMapping.UpdateMapping();

            //var inputPath = @"..\..\..\Test\XCompTests.exe";
            const string inputPath =
                @"E:\Dropbox\HS Furtwangen\7. Semester\Thesis\dev\CrossSL\Example\bin\Debug\Example.exe";

            // ReSharper disable once AssignNullToNotNullAttribute
            var metaPath = Path.Combine(Path.GetDirectoryName(inputPath), "CrossSL.Meta.dll");

            // if (args.Length == 0)
            //    return;

            if (!File.Exists(inputPath) || !File.Exists(metaPath))
            {
                Console.WriteLine("File not found.");
                throw new FileNotFoundException();
            }

            // check if there is a .pdb file along with the .exe
            var debugFile = Path.ChangeExtension(inputPath, "pdb");
            Helper.Verbose = File.Exists(debugFile);

            Console.WriteLine(Helper.Verbose
                ? "Found a .pdb file. This allows for better debugging."
                : "No .pdb file found. Extended debugging has been disabled.");

            // read assembly (and symbols) with Mono.Cecil
            var readParams = new ReaderParameters {ReadSymbols = Helper.Verbose};
            var asm = AssemblyDefinition.ReadAssembly(inputPath, readParams);

            // read meta assembly (without symbols) with Mono.Cecil
            var metaParams = new ReaderParameters {ReadSymbols = false};
            var metaAsm = AssemblyDefinition.ReadAssembly(metaPath, metaParams);

            // find all types with xSLShader as base type
            var shaderDescs = new List<ShaderDesc>();

            var asmTypes = asm.Modules.SelectMany(
                asmMod => asmMod.Types.Where(asmType => asmType.BaseType != null));
            var asmShaderTypes = asmTypes.Where(asmType => asmType.BaseType.IsType<xSLShader>());

            foreach (var asmType in asmShaderTypes)
            {
                Helper.Reset();

                if (Helper.Verbose)
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
                        Console.WriteLine("  => Found [xSLDebug] with 'IgnoreShader' flag. Shader skipped.");
                        continue;
                    }
                }

                // check for [xSLTarget] and save settings
                var targetAttr = asmType.CustomAttributes.FirstOrDefault(
                    attr => attr.AttributeType.IsType<xSLTargetAttribute>());

                if (targetAttr == null)
                {
                    shaderDesc.Target = new ShaderTarget {Envr = xSLEnvironment.OpenGL, Version = 110, VersionID = 0};
                    Console.WriteLine("  => WARNING: Could not find [xSLTarget]. Compiling shader as GLSL 1.1.");
                }
                else
                {
                    var typeName = targetAttr.ConstructorArguments[0].Type.Name;
                    var versionID = (int) targetAttr.ConstructorArguments[0].Value;

                    var shaderTarget = new ShaderTarget {VersionID = versionID};

                    switch (typeName)
                    {
                        case "GLSL":
                            shaderTarget.Envr = xSLEnvironment.OpenGL;
                            break;

                        case "GLSLES":
                            shaderTarget.Envr = xSLEnvironment.OpenGLES;
                            break;

                        case "GLSLMix":
                            shaderTarget.Envr = xSLEnvironment.OpenGLMix;
                            break;
                    }

                    var vStr = xSLVersion.VIDs[(int) shaderTarget.Envr][versionID];

                    shaderTarget.Version = Int32.Parse(vStr);
                    shaderDesc.Target = shaderTarget;

                    if (shaderTarget.Envr == xSLEnvironment.OpenGLMix)
                    {
                        typeName = "GLSL 1.10 & GLSLES";
                        vStr = "100";
                    }

                    vStr = vStr.Insert(1, ".");
                    Console.WriteLine("  => Found [xSLTarget]. Compiling shader as " + typeName + " " + vStr + ".");
                }

                // save debug settings
                if (debugAttr == null)
                {
                    shaderDesc.DebugFlags = xSLDebug.None;
                    Console.WriteLine("  => WARNING: Could not find [xSLDebug]. Debugging has been disabled.");
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

                // get their precission attributes
                var vertPrecAttr = vertexMain.CustomAttributes.FirstOrDefault(
                    attrType => attrType.AttributeType.IsType<xSLPrecisionAttribute>());

                var fragPrecAttr = fragmentMain.CustomAttributes.FirstOrDefault(
                    attrType => attrType.AttributeType.IsType<xSLPrecisionAttribute>());

                shaderDesc.Precision = new CustomAttribute[2];
                shaderDesc.Precision[(int) xSLShaderType.VertexShader] = vertPrecAttr;
                shaderDesc.Precision[(int) xSLShaderType.FragmentShader] = fragPrecAttr;

                // check if there are additional constructors for field/property initialization
                var ctorMethods = asmType.Methods.Where(asmMethod => asmMethod.IsConstructor);
                var customCtors = new Collection<MethodDefinition>();

                foreach (var ctorMethod in ctorMethods.Where(ctor => ctor.Body.CodeSize > 7))
                {
                    // see if there are field initializations (as for "constants")
                    var varInits = ctorMethod.Body.Instructions.Any(instr => instr.OpCode == OpCodes.Stfld);

                    // or property setter calls (as for "constants")
                    var funcCalls = ctorMethod.Body.Instructions.Where(instr => instr.OpCode == OpCodes.Call);
                    var propInits = funcCalls.Any(instr => ((MethodReference) instr.Operand).Resolve().IsSetter);

                    if (varInits || propInits)
                        customCtors.Add(ctorMethod);
                    else
                    {
                        var instr = ctorMethod.Body.Instructions[0];
                        Helper.Warning("Found a constructor with no valid content", instr);
                    }
                }

                // analyze variables used in shader
                Console.WriteLine("\n  2. Collecting information about fields and properties.");

                var variables = new Collection<VariableDesc>();
                var varTypes = Enum.GetNames(typeof (xSLVariableType));

                // read and gather fields and backing fields
                foreach (var asmField in asmType.Fields)
                {
                    var varDesc = new VariableDesc {Definition = asmField};

                    var fdName = asmField.Name;
                    var fdType = asmField.FieldType.ToType();

                    var attrs = asmField.CustomAttributes;
                    var attrCt = attrs.Count(attr => varTypes.Contains(attr.AttributeType.Name));

                    var isProp = asmField.Name.Contains("<");

                    if (isProp)
                    {
                        // ReSharper disable once StringIndexOfIsCultureSpecific.1
                        fdName = fdName.Remove(0, 1).Remove(fdName.IndexOf(">") - 1);

                        var asmProp = asmType.Properties.First(prop => prop.Name == fdName);
                        varDesc.Definition = asmProp;

                        attrs = asmProp.CustomAttributes;
                        fdType = asmProp.PropertyType.ToType();
                    }

                    var validFd = asmField.HasConstant ^ attrCt == 1 ^ isProp;
                    var varType = (isProp) ? "Property '" : "Field '";

                    if (asmField.IsStatic)
                        Helper.Error(varType + fdName + "' cannot be static");
                    else if (validFd)
                    {
                        var fdAttr = xSLVariableType.xSLConstAttribute;

                        if (!asmField.HasConstant)
                        {
                            var fdAttrName = attrs.First(attr => varTypes.Contains(attr.AttributeType.Name));
                            fdAttr = (xSLVariableType) Array.IndexOf(varTypes, fdAttrName.AttributeType.Name);
                        }
                        else
                            varDesc.Value = asmField.Constant;

                        // resolve data type of variable
                        if (!xSLTypeMapping.Types.ContainsKey(fdType))
                        {
                            var strAdd = (fdType != typeof (Object)) ? " type '" + fdType.Name + "' " : " a type ";
                            Helper.Error(varType + asmField.Name + "' is of" + strAdd + "which is not supported.");
                        }

                        varDesc.DataType = fdType;
                        varDesc.Attribute = fdAttr;

                        variables.Add(varDesc);
                    }
                    else
                        Helper.Error(varType + asmField.Name + "' is neither a constant nor has valid attributes");
                }

                shaderDesc.Variables = variables;

                // translate main, depending methods and constructors
                Console.WriteLine("\n  3. Translating shader from C# to " + shaderDesc.Target.Envr + ".");

                shaderDesc.Funcs = new IEnumerable<FunctionDesc>[2];

                var vertexFuncs = Translate(shaderDesc.Target, vertexMain);
                shaderDesc.Funcs[(int) xSLShaderType.VertexShader] = vertexFuncs;

                var fragmentFuncs = Translate(shaderDesc.Target, fragmentMain);
                shaderDesc.Funcs[(int) xSLShaderType.FragmentShader] = fragmentFuncs;

                // check correct use of constants
                foreach (var ctor in customCtors)
                {
                    var funcs = Translate(shaderDesc.Target, ctor);

                    var allVars = funcs.SelectMany(func => func.Variables).ToList();
                    var allGlobVars = allVars.Where(variables.Contains).ToList();
                    var illegalVars = allVars.Except(allGlobVars).ToList();

                    foreach (var illegalVar in illegalVars)
                    {
                        var name = illegalVar.Definition.Name;
                        var instr = illegalVar.Instruction;

                        Helper.Error("Illegal use of '" + name + "' in a constructor", instr);
                    }

                    foreach (var constVar in allGlobVars)
                    {
                        var globVar = shaderDesc.Variables.First(var => var.Equals(constVar));
                        var index = shaderDesc.Variables.IndexOf(globVar);

                        var name = constVar.Definition.Name;
                        var instr = constVar.Instruction;

                        if (globVar.Attribute != xSLVariableType.xSLConstAttribute)
                            Helper.Error("Variable '" + name + "' is used as a constant but not marked as such'", instr);
                        else if (globVar.Value != null && constVar.Value != null)
                            Helper.Error("Constant '" + name + "' cannot be set more than once", instr);
                        else if (constVar.Value is String)
                            Helper.Error("Constant '" + name + "' was initialized with an invalid value", instr);
                        else
                            shaderDesc.Variables[index].Value = constVar.Value;
                    }
                }

                // build both shaders
                var vertexResult = BuildShader(ref shaderDesc, xSLShaderType.VertexShader);
                var fragmentResult = BuildShader(ref shaderDesc, xSLShaderType.FragmentShader);

                // see if there are unused fields/properties
                var unusedVars = shaderDesc.Variables.Where(var => !var.IsReferenced);

                foreach (var unsedVar in unusedVars)
                    Helper.Warning("Variable '" + unsedVar.Definition.Name + "' was declared but is not used");

                if (!Helper.Abort)
                {
                    Console.WriteLine("\n  4. Building vertex and fragment shader.");

                    // debugging: precompile first then save to file
                    Console.WriteLine("\n  5. Applying debugging flags if any.");

                    if (!Helper.Abort && (xSLDebug.PreCompile & shaderDesc.DebugFlags) != 0)
                    {
                        if (GLSLCompiler.CanCheck(shaderDesc.Target.Version))
                        {
                            if (shaderDesc.Target.Envr == xSLEnvironment.OpenGLES)
                                Helper.Warning("Shader will be tested on OpenGL but target is OpenGL ES");

                            if (shaderDesc.Target.Envr == xSLEnvironment.OpenGLMix)
                                Helper.Warning("Shader will only be tested on OpenGL but target is OpenGL and OpenGL ES");

                            var vertTest = GLSLCompiler.CreateShader(vertexResult, xSLShaderType.VertexShader);
                            vertTest.Length = Math.Max(0, vertTest.Length - 3);
                            vertTest = vertTest.Replace("0(", "        => 0(");

                            var fragTest = GLSLCompiler.CreateShader(fragmentResult, xSLShaderType.FragmentShader);
                            fragTest.Length = Math.Max(0, fragTest.Length - 3);
                            fragTest = fragTest.Replace("0(", "        => 0(");

                            if (vertTest.ToString() != String.Empty)
                                Helper.Error("OpenGL found problems while compiling vertex shader:\n" + vertTest);
                            else if (fragTest.ToString() != String.Empty)
                                Helper.Error("OpenGL found problems while compiling fragment shader:\n" + fragTest);
                            else
                                Console.WriteLine("        => Test was successful. OpenGL did not find any problems.");
                        }
                        else
                            Helper.Warning("Cannot test shader as your graphics card does not support GLSL.");
                    }

                    if (!Helper.Abort && (xSLDebug.SaveToFile & shaderDesc.DebugFlags) != 0)
                    {
                        var directory = Path.GetDirectoryName(inputPath);

                        if (directory != null)
                        {
                            var combined = new StringBuilder("---- VertexShader ----").NewLine(2);
                            combined.Append(vertexResult).NewLine(3).Append("---- FragmentShader ----");
                            combined.NewLine(2).Append(fragmentResult);

                            var filePath = Path.Combine(directory, shaderDesc.Name + ".txt");
                            File.WriteAllText(filePath, combined.ToString());

                            Console.WriteLine("    => Saved shader to: '" + filePath + "'");
                        }
                    }
                }
                else
                {
                    Console.WriteLine("\n  4. Shader will not be built due to critical errors.");

                    if ((xSLDebug.ThrowException & shaderDesc.DebugFlags) != 0)
                    {
                        Console.WriteLine("\n  5. Applying debugging flags if any.");
                        Console.WriteLine("    => Errors will be thrown when using the shader.");
                        Console.WriteLine("\n  6. Preparing to update meta assembly for this shader.");
                    }
                }

                // save shaders or errors into the assembly
                var genShader = metaAsm.MainModule.Types.First(type => type.ToType() == typeof (xSL<>));
                var instShader = new GenericInstanceType(genShader);

                var asmTypeImport = metaAsm.MainModule.Import(asmType);
                instShader.GenericArguments.Add(asmTypeImport);

                var instrList = new List<Instruction>();

                if (!Helper.Abort)
                {
                    Console.WriteLine("\n  6. Preparing to update meta assembly for this shader.");

                    var vertField = GenericFieldReference(genShader, instShader, "_vertex");
                    var fragField = GenericFieldReference(genShader, instShader, "_fragment");
                    var transField = GenericFieldReference(genShader, instShader, "_translated");

                    metaAsm.MainModule.Import(vertField);
                    metaAsm.MainModule.Import(fragField);
                    metaAsm.MainModule.Import(transField);

                    instrList.Add(Instruction.Create(OpCodes.Ldstr, vertexResult.ToString()));
                    instrList.Add(Instruction.Create(OpCodes.Stsfld, vertField));

                    instrList.Add(Instruction.Create(OpCodes.Ldstr, fragmentResult.ToString()));
                    instrList.Add(Instruction.Create(OpCodes.Stsfld, fragField));

                    instrList.Add(Instruction.Create(OpCodes.Ldc_I4_1));
                    instrList.Add(Instruction.Create(OpCodes.Stsfld, transField));
                }

                // apply debug mode ThrowException
                if (Helper.Abort && (xSLDebug.ThrowException & shaderDesc.DebugFlags) != 0)
                {
                    var errors = Helper.Errors.ToString().Replace("    =>", "=>");
                    var errField = GenericFieldReference(genShader, instShader, "_error");

                    instrList.Add(Instruction.Create(OpCodes.Ldstr, errors));
                    instrList.Add(Instruction.Create(OpCodes.Stsfld, errField));
                }

                shaderDesc.Instructions = instrList;

                Console.WriteLine(Helper.Abort
                    ? "\n  ---- Translation failed ----"
                    : "\n  ---- Translation succeeded ----");

                shaderDescs.Add(shaderDesc);
            }

            // write shaders into assembly
            var invalidCt = shaderDescs.Count(shader => !shader.Instructions.Any());

            if (invalidCt == shaderDescs.Count)
                Console.WriteLine("\n\nAssembly will not be updated as no shader was translated successfully.");
            else
            {
                Console.WriteLine("\n\nUpdating CrossSL meta assembly:");

                var asmModule = metaAsm.MainModule;
                var genShader = asmModule.Types.First(type => type.ToType() == typeof (xSL<>));

                var xSLInit = genShader.Methods.First(method => method.Name == "Init");
                var ilProc = xSLInit.Body.GetILProcessor();

                xSLInit.Body.Instructions.Clear();

                foreach (var shaderDesc in shaderDescs)
                    foreach (var instr in shaderDesc.Instructions)
                        ilProc.Append(instr);

                var ret = Instruction.Create(OpCodes.Ret);
                ilProc.Append(ret);

                try
                {
                    var writeParams = new WriterParameters {WriteSymbols = false};
                    asmModule.Write(metaPath, writeParams);

                    foreach (var shaderDesc in shaderDescs)
                        if (shaderDesc.Instructions.Count() > 2)
                            Console.WriteLine("  => Added shader '" + shaderDesc.Name + "' to assembly.");
                        else
                            Console.WriteLine("  => [ThrowException] mode was applied for shader '" +
                                              shaderDesc.Name + "'.");

                    Console.WriteLine("\n  => Saved assembly as '" + metaPath + "'");
                }
                catch (IOException)
                {
                    Console.WriteLine("  => Cannot update assembly. File might be missing, read-only or in use.");
                }
            }

            Console.WriteLine("\n\nDone.");
            Console.ReadLine();
        }

        private static FieldReference GenericFieldReference(TypeDefinition gen, TypeReference inst, string name)
        {
            var genTField = gen.Fields.First(field => field.Name == name);
            var tField = new FieldReference(genTField.Name, genTField.FieldType)
            {
                DeclaringType = inst
            };
            return tField;
        }

        private static StringBuilder BuildShader(ref ShaderDesc shaderDescRef, xSLShaderType shaderType)
        {
            var result = new StringBuilder();
            var shaderDesc = shaderDescRef;

            // corresponding functions
            var functions = shaderDesc.Funcs[(int) shaderType];

            // collect all referenced variables
            var refVars = new List<VariableDesc>();

            foreach (var func in functions.Where(func => func.Variables != null))
                refVars.AddRange(func.Variables);

            var varDescs = new Collection<VariableDesc>();
            var shaderDescType = shaderDesc.Type.ToType();
            var memberVars = refVars.Where(var => var.Definition.DeclaringType.ToType() == shaderDescType);

            foreach (var memberVar in memberVars)
            {
                var globVar = shaderDesc.Variables.First(var => var.Definition == memberVar.Definition);
                var globIndex = shaderDesc.Variables.IndexOf(globVar);

                shaderDesc.Variables[globIndex].IsReferenced = true;

                memberVar.Attribute = globVar.Attribute;
                memberVar.DataType = globVar.DataType;
                memberVar.IsReferenced = true;

                varDescs.Add(memberVar);
            }

            // check if varyings and attributes are used properly
            var varVars = varDescs.Where(var => var.Attribute == xSLVariableType.xSLVaryingAttribute);

            if (shaderType == xSLShaderType.FragmentShader)
            {
                foreach (var invalidVar in varVars.Where(var => !var.IsReferenced))
                    Helper.Error("Varying '" + invalidVar.Definition.Name + "' is used in 'FragmentShader()'" +
                                 " but was not set in 'VertexShader()'", invalidVar.Instruction);

                var attrVars = varDescs.Where(var => var.Attribute == xSLVariableType.xSLAttributeAttribute).ToList();

                foreach (var invalidVar in attrVars)
                    Helper.Error("Attribute '" + invalidVar.Definition.Name + "' cannot be " +
                                 "used in in 'FragmentShader()'" + invalidVar.Instruction);
            }
            else
            {
                var fragFunc = shaderDesc.Funcs[(int) xSLShaderType.FragmentShader];
                var mergedVars = fragFunc.SelectMany(func => func.Variables).ToList();

                foreach (var invalidVar in varVars.Where(var => !mergedVars.Contains(var)))
                    Helper.Warning("Varying '" + invalidVar.Definition.Name + "' was set in 'VertexShader()'" +
                                   " but is not used in 'FragmentShader()'", invalidVar.Instruction);
            }

            // check if constants have been set
            var constVars = varDescs.Where(var => var.Attribute == xSLVariableType.xSLConstAttribute).ToList();

            foreach (var constVar in refVars.Where(constVars.Contains).Where(con => con.Value != null))
                Helper.Error("Constant '" + constVar.Definition.Name + "' cannot be initialized " +
                             "in 'VertexShader()'", constVar.Instruction);

            foreach (var constVar in constVars.Where(var => var.Value == null))
            {
                constVar.Value = shaderDesc.Variables.First(var => var.Definition == constVar.Definition).Value;

                if (constVar.Value == null)
                    Helper.Error("Constant '" + constVar.Definition.Name + "' was not initialized",
                        constVar.Instruction);
            }

            // check if invalid variables are set
            var nestedTypes = typeof (xSLShader).GetNestedTypes(BindingFlags.NonPublic);

            var attrType = nestedTypes.FirstOrDefault(type => type.Name == shaderType + "Attribute");
            var mandType = nestedTypes.FirstOrDefault(type => type.Name == "MandatoryAttribute");

            var allProps = typeof (xSLShader).GetProperties(BindingFlags.Instance | BindingFlags.NonPublic);
            var validProps = allProps.Where(prop => prop.CustomAttributes.Any(attr => attr.AttributeType == attrType));
            var validNames = validProps.Select(prop => prop.Name).ToList();

            var globalVars = refVars.Where(def => def.Definition.DeclaringType.IsType<xSLShader>()).ToList();
            var globalNames = globalVars.Select(var => var.Definition.Name).ToList();

            foreach (var memberVar in globalNames.Where(var => !validNames.Contains(var)))
            {
                var instr = globalVars.First(var => var.Definition.Name == memberVar).Instruction;
                Helper.Error("'" + memberVar + "' cannot be used in '" + shaderType + "()'", instr);
            }

            // check if necessary variables are set
            var mandVars = allProps.Where(prop => prop.CustomAttributes.Any(attr => attr.AttributeType == mandType));

            foreach (var mandVar in mandVars)
            {
                if (validNames.Contains(mandVar.Name) && !globalNames.Contains(mandVar.Name))
                    Helper.Error("'" + mandVar.Name + "' has to be set in '" + shaderType + "()'");

                if (globalNames.Count(var => var == mandVar.Name) > 1)
                {
                    var instr = globalVars.Last(var => var.Definition.Name == mandVar.Name).Instruction;
                    Helper.Warning("'" + mandVar.Name + "' has been set more than" +
                                   " once in '" + shaderType + "()'", instr);
                }
            }

            if (Helper.Abort) return null;

            // add precision to output
            var defPrec = String.Empty;

            if (shaderDescRef.Precision[(int) shaderType] != null)
            {
                var prec = new StringBuilder();
                var precAttr = shaderDescRef.Precision[(int) shaderType];

                var floatPrec = precAttr.Properties.FirstOrDefault(prop => prop.Name == "floatPrecision");
                var intPrec = precAttr.Properties.FirstOrDefault(prop => prop.Name == "intPrecision");

                if (floatPrec.Name == null && intPrec.Name == null)
                    defPrec = "Found [xSLPrecision] for '" + shaderType + "()' but no precision was set";
                else
                {
                    if (floatPrec.Name != null)
                    {
                        var floatPrecVal = ((xSLPrecision) floatPrec.Argument.Value).ToString();
                        prec.Append("precision ").Append(floatPrecVal.ToLower()).Append("p");
                        prec.Append(" float;").NewLine();
                    }
                    else if (shaderDesc.Target.Envr == xSLEnvironment.OpenGLES)
                        if (shaderType == xSLShaderType.FragmentShader)
                            defPrec = "Target GLSLES requires to set float precision for 'FragmentShader()'";

                    if (intPrec.Name != null)
                    {
                        var intPrecVal = ((xSLPrecision) intPrec.Argument.Value).ToString();
                        prec.Append("precision ").Append(intPrecVal.ToLower()).Append("p");
                        prec.Append(" int;").NewLine();
                    }

                    if (precAttr.ConstructorArguments.Count > 0)
                    {
                        var condition = (xSLEnvironment) precAttr.ConstructorArguments[0].Value;
                        var ifdef = (condition == xSLEnvironment.OpenGL) ? "#ifndef" : "#ifdef";

                        prec.Replace(Environment.NewLine, Environment.NewLine + "\t").Length--;
                        prec = new StringBuilder(ifdef).Append(" GL_ES").NewLine().Intend().Append(prec);
                        prec.Append("#endif").NewLine();
                    }

                    result.Append(prec.Replace("medium", "med").NewLine());
                }
            }
            else if (shaderDesc.Target.Envr == xSLEnvironment.OpenGLES)
                if (shaderType == xSLShaderType.FragmentShader)
                    defPrec = "Target GLSLES requires [xSLPrecision] to set float precision for 'FragmentShader()'";

            // default precision
            if (defPrec != String.Empty)
            {
                Helper.Warning(defPrec + ". Using high precision for float as default");
                result.Append("precision highp float;");
            }

            // add variables to shader output
            foreach (var varDesc in varDescs.Distinct().OrderBy(var => var.Attribute))
            {
                var dataType = xSLTypeMapping.Types[varDesc.DataType];

                var varType = varDesc.Attribute.ToString().ToLower();
                varType = varType.Remove(0, 3).Remove(varType.Length - 12);

                result.Append(varType).Space().Append(dataType).Space();
                result.Append(varDesc.Definition.Name);

                if (varDesc.Attribute == xSLVariableType.xSLConstAttribute)
                    result.Assign().Append(varDesc.Value);

                result.Semicolon().NewLine();
            }

            result.Length -= 2;

            // add all functions to shader output
            foreach (var func in shaderDesc.Funcs[(int) shaderType])
                result.NewLine(2).Append(func.Signature).NewLine().Append(func.Body);

            shaderDescRef = shaderDesc;
            return result;
        }

        private static StringBuilder MapReturnType(MethodDefinition method)
        {
            var retType = method.ReturnType.ToType();

            if (!xSLTypeMapping.Types.ContainsKey(retType))
            {
                var strAdd = (retType != typeof (Object)) ? " '" + retType.Name + "'" : String.Empty;

                var instr = method.Body.Instructions[0];
                Helper.Error("Method has an unsupported return type" + strAdd, instr);

                return null;
            }

            return new StringBuilder(xSLTypeMapping.Types[retType]);
        }

        private static StringBuilder JoinParams(MethodDefinition method)
        {
            var result = new StringBuilder();

            foreach (var param in method.Parameters)
            {
                var paramType = param.ParameterType.ToType();

                if (!xSLTypeMapping.Types.ContainsKey(paramType))
                {
                    var strAdd = (paramType != typeof (Object)) ? " '" + paramType.Name + "'" : String.Empty;

                    var instr = method.Body.Instructions[0];
                    Helper.Error("Method has a parameter of the unsupported type" + strAdd, instr);

                    return null;
                }

                var isRef = (param.ParameterType is ByReferenceType);
                var refStr = (isRef) ? "out " : String.Empty;

                var typeMapped = xSLTypeMapping.Types[paramType];
                var paramName = param.Name;

                result.Append(refStr).Append(typeMapped).Space();
                result.Append(paramName).Append(", ");
            }

            if (result.Length > 0)
                result.Length -= 2;

            return result;
        }

        private static IEnumerable<FunctionDesc> Translate(ShaderTarget target, MethodDefinition method)
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

            var transVisitor = GetTranslator(target, methodBody, decContext);
            transVisitor.Translate();

            // save information
            var result = new FunctionDesc
            {
                Definion = method,
                Signature = sig,
                Body = transVisitor.Result,
                Variables = transVisitor.RefVariables
            };

            // translate all referenced methods
            foreach (var refMethod in transVisitor.RefMethods)
                if (allFuncs.All(aMethod => aMethod.Definion != refMethod))
                    allFuncs.AddRange(Translate(target, refMethod));

            allFuncs.Add(result);
            return allFuncs;
        }

        private static ShaderVisitor GetTranslator(ShaderTarget target, AstNode methodBody, DecompilerContext decContext)
        {
            switch (target.Envr)
            {
                case xSLEnvironment.OpenGL:
                    switch ((xSLTarget.GLSL) target.VersionID)
                    {
                        case xSLTarget.GLSL.V110:
                            return new GLSLVisitor110(methodBody, decContext);

                        default:
                            return new GLSLVisitor(methodBody, decContext);
                    }

                case xSLEnvironment.OpenGLES:
                    return new GLSLVisitor110(methodBody, decContext);

                case xSLEnvironment.OpenGLMix:
                    return new GLSLVisitor110(methodBody, decContext);
            }

            return null;
        }
    }
}