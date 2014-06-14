using System.Collections.Generic;
using Mono.Cecil;

namespace CrossSL
{
    internal class GLSLTranslator : ShaderTranslator
    {
        public GLSLTranslator()
        {
            ShaderMapping = new GLSLMapping();
            ShaderVisitor = new GLSLVisitor110();
        }

        internal override IEnumerable<FunctionDesc> Translate(ShaderTarget target, MethodDefinition method)
        {
            method.Name = method.Name.Replace("VertexShader", "main");
            method.Name = method.Name.Replace("FragmentShader", "main");

            return base.Translate(target, method);
        }
    }
}