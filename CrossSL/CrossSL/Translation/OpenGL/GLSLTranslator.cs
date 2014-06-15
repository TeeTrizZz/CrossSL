using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Mono.Cecil;

using xSLEnvironment = CrossSL.Meta.xSLShader.xSLEnvironment;
using xSLPrecision = CrossSL.Meta.xSLShader.xSLPrecision;

namespace CrossSL
{
    internal class GLSLTranslator : ShaderTranslator
    {
        public GLSLTranslator()
        {
            ShaderMapping = new GLSLMapping();
            ShaderVisitor = new GLSLVisitor();
        }

        /// <summary>
        ///     Translates the a given method to the targeted shader language.
        /// </summary>
        /// <param name="target">The targeted shader language.</param>
        /// <param name="methodDef">The method definition.</param>
        /// <returns>A list of <see cref="FunctionDesc" />s for every translated method.</returns>
        internal override IEnumerable<FunctionDesc> Translate(ShaderTarget target, MethodDefinition methodDef)
        {
            methodDef.Name = methodDef.Name.Replace("VertexShader", "main");
            methodDef.Name = methodDef.Name.Replace("FragmentShader", "main");

            return base.Translate(target, methodDef);
        }

        /// <summary>
        ///     Sets the default data type precision definition.
        /// </summary>
        /// <returns>
        ///     A <see cref="StringBuilder" /> for the default precision definition.
        /// </returns>
        protected virtual StringBuilder SetDefaultPrecision()
        {
            // no default needed
            return new StringBuilder();
        }

        /// <summary>
        ///     Adds the data type precision definition to the given shader.
        /// </summary>
        /// <param name="shaderStr">The shader string.</param>
        /// <param name="shaderType">Type of the shader.</param>
        protected override void SetPrecision(ref StringBuilder shaderStr, SLShaderType shaderType)
        {
            var defaultPrec = true;

            if (ShaderDesc.Precision[(int) shaderType] != null)
            {
                var prec = new StringBuilder();
                var precAttr = ShaderDesc.Precision[(int) shaderType];

                var floatPrec = precAttr.Properties.FirstOrDefault(prop => prop.Name == "floatPrecision");
                var intPrec = precAttr.Properties.FirstOrDefault(prop => prop.Name == "intPrecision");

                if (floatPrec.Name != null)
                {
                    var floatPrecVal = ((xSLPrecision) floatPrec.Argument.Value).ToString();
                    prec.Append("precision ").Append(floatPrecVal.ToLower()).Append("p");
                    prec.Append(" float;").NewLine();

                    defaultPrec = false;
                }

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

                shaderStr.Append(prec.Replace("medium", "med").NewLine());
            }

            // default precision
            if (defaultPrec && shaderType == SLShaderType.FragmentShader)
                shaderStr.Append(SetDefaultPrecision());
        }

        /// <summary>
        ///     Tests the given shaders by passing them to the GLSL/HLSL compiler.
        /// </summary>
        /// <param name="vertexShader">The vertex shader.</param>
        /// <param name="fragmentShader">The fragment shader.</param>
        internal override void PreCompile(StringBuilder vertexShader, StringBuilder fragmentShader)
        {
            if (GLSLCompiler.CanCheck(ShaderDesc.Target.Version))
            {
                if (ShaderDesc.Target.Envr == xSLEnvironment.OpenGLES)
                    DebugLog.Warning("Shader will be tested on OpenGL but target is OpenGL ES");

                if (ShaderDesc.Target.Envr == xSLEnvironment.OpenGLMix)
                    DebugLog.Warning("Shader will be tested on OpenGL but target is OpenGL and OpenGL ES");

                var vertTest = GLSLCompiler.CreateShader(vertexShader, SLShaderType.VertexShader);
                vertTest.Length = Math.Max(0, vertTest.Length - 3);
                vertTest = vertTest.Replace("0(", "        => 0(");

                var fragTest = GLSLCompiler.CreateShader(fragmentShader, SLShaderType.FragmentShader);
                fragTest.Length = Math.Max(0, fragTest.Length - 3);
                fragTest = fragTest.Replace("0(", "        => 0(");

                if (vertTest.Length > 0)
                    DebugLog.Error("OpenGL found problems while compiling vertex shader:\n" + vertTest);
                else if (fragTest.Length > 0)
                    DebugLog.Error("OpenGL found problems while compiling fragment shader:\n" + fragTest);
                else
                    Console.WriteLine("        => Test was successful. OpenGL did not find any problems.");
            }
            else
                DebugLog.Warning("Cannot test shader as your graphics card does not support the targeted version.");
        }
    }
}