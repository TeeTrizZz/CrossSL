using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using CrossSL.Meta;
using Mono.Cecil;
using Mono.Collections.Generic;

namespace CrossSL
{
    internal class ShaderTranslator
    {
        internal ShaderDesc ShaderDesc { get; set; }

        protected ShaderMapping ShaderMapping;
        protected ShaderVisitor ShaderVisitor;

        internal static ShaderTranslator GetTranslator(ShaderTarget target)
        {
            switch (target.Envr)
            {
                case xSLEnvironment.OpenGL:
                    switch ((xSLTarget.GLSL) target.VersionID)
                    {
                        case xSLTarget.GLSL.V110:
                            return new GLSLTranslator110();

                        default:
                            return new GLSLTranslator();
                    }

                case xSLEnvironment.OpenGLES:
                    return new GLSLTranslator110();

                case xSLEnvironment.OpenGLMix:
                    return new GLSLTranslator110();
            }

            return null;
        }

        protected virtual void MemberVarCheck()
        {
            foreach (var memberVar in ShaderDesc.Variables)
            {
                var varName = memberVar.Definition.Name;
                var varType = memberVar.DataType;

                // resolve data type of variable
                if (!ShaderMapping.Types.ContainsKey(varType))
                {
                    var strAdd = (varType != typeof(Object)) ? " type '" + varType.Name + "' " : " a type ";
                    xSLConsole.Error(varType + varName + "' is of" + strAdd + "which is not supported.");
                }
            }
        }

        protected virtual StringBuilder MapReturnType(MethodDefinition method)
        {
            var retType = method.ReturnType.ToType();

            if (!ShaderMapping.Types.ContainsKey(retType))
            {
                var strAdd = (retType != typeof(Object)) ? " '" + retType.Name + "'" : String.Empty;

                var instr = method.Body.Instructions[0];
                xSLConsole.Error("Method has an unsupported return type" + strAdd, instr);

                return null;
            }

            return new StringBuilder(ShaderMapping.Types[retType]);
        }

        protected virtual StringBuilder JoinParams(MethodDefinition method)
        {
            var result = new StringBuilder();

            foreach (var param in method.Parameters)
            {
                var paramType = param.ParameterType.ToType();

                if (!ShaderMapping.Types.ContainsKey(paramType))
                {
                    var strAdd = (paramType != typeof(Object)) ? " '" + paramType.Name + "'" : String.Empty;

                    var instr = method.Body.Instructions[0];
                    xSLConsole.Error("Method has a parameter of the unsupported type" + strAdd, instr);

                    return null;
                }

                var isRef = (param.ParameterType is ByReferenceType);
                var refStr = (isRef) ? "out " : String.Empty;

                var typeMapped = ShaderMapping.Types[paramType];
                var paramName = param.Name;

                result.Append(refStr).Append(typeMapped).Space();
                result.Append(paramName).Append(", ");
            }

            if (result.Length > 0)
                result.Length -= 2;

            return result;
        }

        internal virtual IEnumerable<FunctionDesc> Translate(ShaderTarget target, MethodDefinition methodDef)
        {
            var allFuncs = new List<FunctionDesc>();

            // build function signature
            var retTypeStr = MapReturnType(methodDef);
            var paramStr = JoinParams(methodDef);

            if (retTypeStr == null || paramStr == null)
                return null;

            var sig = retTypeStr.Space().Method(methodDef.Name, paramStr.ToString());

            // create DecompilerContext for given method
            ShaderVisitor.Init(methodDef);
            ShaderVisitor.Translate(ShaderMapping);

            // save information
            var result = new FunctionDesc
            {
                Definion = methodDef,
                Signature = sig,
                Body = ShaderVisitor.Result,
                Variables = ShaderVisitor.RefVariables
            };

            // translate all referenced methods
            foreach (var refMethod in ShaderVisitor.RefMethods)
                if (allFuncs.All(aMethod => aMethod.Definion != refMethod))
                    allFuncs.AddRange(Translate(target, refMethod));

            allFuncs.Add(result);
            return allFuncs;
        }

        internal virtual StringBuilder BuildShader(ref ShaderDesc shaderDescRef, xSLShaderType shaderType)
        {
            var result = new StringBuilder();
            var shaderDesc = shaderDescRef;

            // corresponding functions
            var functions = shaderDesc.Funcs[(int)shaderType];

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
                    xSLConsole.Error("Varying '" + invalidVar.Definition.Name + "' is used in 'FragmentShader()'" +
                                 " but was not set in 'VertexShader()'", invalidVar.Instruction);

                var attrVars = varDescs.Where(var => var.Attribute == xSLVariableType.xSLAttributeAttribute).ToList();

                foreach (var invalidVar in attrVars)
                    xSLConsole.Error("Attribute '" + invalidVar.Definition.Name + "' cannot be " +
                                 "used in in 'FragmentShader()'" + invalidVar.Instruction);
            }
            else
            {
                var fragFunc = shaderDesc.Funcs[(int)xSLShaderType.FragmentShader];
                var mergedVars = fragFunc.SelectMany(func => func.Variables).ToList();

                foreach (var invalidVar in varVars.Where(var => !mergedVars.Contains(var)))
                    xSLConsole.Warning("Varying '" + invalidVar.Definition.Name + "' was set in 'VertexShader()'" +
                                   " but is not used in 'FragmentShader()'", invalidVar.Instruction);
            }

            // check if constants have been set
            var constVars = varDescs.Where(var => var.Attribute == xSLVariableType.xSLConstAttribute).ToList();

            foreach (var constVar in refVars.Where(constVars.Contains).Where(con => con.Value != null))
                xSLConsole.Error("Constant '" + constVar.Definition.Name + "' cannot be initialized " +
                             "in 'VertexShader()'", constVar.Instruction);

            foreach (var constVar in constVars.Where(var => var.Value == null))
            {
                constVar.Value = shaderDesc.Variables.First(var => var.Definition == constVar.Definition).Value;

                if (constVar.Value == null)
                    xSLConsole.Error("Constant '" + constVar.Definition.Name + "' was not initialized",
                        constVar.Instruction);
            }

            // check if invalid variables are set
            var nestedTypes = typeof(xSLShader).GetNestedTypes(BindingFlags.NonPublic);

            var attrType = nestedTypes.FirstOrDefault(type => type.Name == shaderType + "Attribute");
            var mandType = nestedTypes.FirstOrDefault(type => type.Name == "MandatoryAttribute");

            var allProps = typeof(xSLShader).GetProperties(BindingFlags.Instance | BindingFlags.NonPublic);
            var validProps = allProps.Where(prop => prop.CustomAttributes.Any(attr => attr.AttributeType == attrType));
            var validNames = validProps.Select(prop => prop.Name).ToList();

            var globalVars = refVars.Where(def => def.Definition.DeclaringType.IsType<xSLShader>()).ToList();
            var globalNames = globalVars.Select(var => var.Definition.Name).ToList();

            foreach (var memberVar in globalNames.Where(var => !validNames.Contains(var)))
            {
                var instr = globalVars.First(var => var.Definition.Name == memberVar).Instruction;
                xSLConsole.Error("'" + memberVar + "' cannot be used in '" + shaderType + "()'", instr);
            }

            // check if necessary variables are set
            var mandVars = allProps.Where(prop => prop.CustomAttributes.Any(attr => attr.AttributeType == mandType));

            foreach (var mandVar in mandVars)
            {
                var mandVarName = mandVar.Name;

                if (validNames.Contains(mandVarName) && !globalNames.Contains(mandVarName))
                    xSLConsole.Error("'" + mandVarName + "' has to be set in '" + shaderType + "()'");

                if (globalNames.Count(var => var == mandVarName) > 1)
                {
                    var instr = globalVars.Last(var => var.Definition.Name == mandVarName).Instruction;
                    xSLConsole.Warning("'" + mandVarName + "' has been set more than" +
                                   " once in '" + shaderType + "()'", instr);
                }
            }

            if (xSLConsole.Abort) return null;

            // add precision to output
            var defPrec = String.Empty;

            if (shaderDescRef.Precision[(int)shaderType] != null)
            {
                var prec = new StringBuilder();
                var precAttr = shaderDescRef.Precision[(int)shaderType];

                var floatPrec = precAttr.Properties.FirstOrDefault(prop => prop.Name == "floatPrecision");
                var intPrec = precAttr.Properties.FirstOrDefault(prop => prop.Name == "intPrecision");

                if (floatPrec.Name == null && intPrec.Name == null)
                    defPrec = "Found [xSLPrecision] for '" + shaderType + "()' but no precision was set";
                else
                {
                    if (floatPrec.Name != null)
                    {
                        var floatPrecVal = ((xSLPrecision)floatPrec.Argument.Value).ToString();
                        prec.Append("precision ").Append(floatPrecVal.ToLower()).Append("p");
                        prec.Append(" float;").NewLine();
                    }
                    else if (shaderDesc.Target.Envr == xSLEnvironment.OpenGLES)
                        if (shaderType == xSLShaderType.FragmentShader)
                            defPrec = "Target GLSLES requires to set float precision for 'FragmentShader()'";

                    if (intPrec.Name != null)
                    {
                        var intPrecVal = ((xSLPrecision)intPrec.Argument.Value).ToString();
                        prec.Append("precision ").Append(intPrecVal.ToLower()).Append("p");
                        prec.Append(" int;").NewLine();
                    }

                    if (precAttr.ConstructorArguments.Count > 0)
                    {
                        var condition = (xSLEnvironment)precAttr.ConstructorArguments[0].Value;
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
            if (defPrec.Length > 0)
            {
                xSLConsole.Warning(defPrec + ". Using high precision for float as default");
                result.Append("precision highp float;");
            }

            // add variables to shader output
            foreach (var varDesc in varDescs.Distinct().OrderBy(var => var.Attribute))
            {
                var dataType = ShaderMapping.Types[varDesc.DataType];

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
            foreach (var func in shaderDesc.Funcs[(int)shaderType])
                result.NewLine(2).Append(func.Signature).NewLine().Append(func.Body);

            shaderDescRef = shaderDesc;
            return result;
        }
    }
}