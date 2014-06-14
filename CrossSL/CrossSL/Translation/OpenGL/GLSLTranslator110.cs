using System.Linq;

namespace CrossSL
{
    internal sealed class GLSLTranslator110 : GLSLTranslator
    {
        public GLSLTranslator110()
        {
            ShaderMapping = new GLSLMapping110();
        }

        protected override void MemberVarCheck()
        {
            base.MemberVarCheck();

            var doubleVars = ShaderDesc.Variables.Where(var => var.DataType == typeof (double));
            foreach (var doubleVar in doubleVars)
            {
                xSLConsole.Warning("'" + doubleVar.Definition.Name + "' is of type 'double' which" +
                                   " is not supported in GLSL 1.1. Type will be changed to 'float'");
            }
        }
    }
}
