using System.Text;

namespace CrossSL
{
    internal sealed class GLSLTranslator100 : GLSLTranslator
    {
        internal GLSLTranslator100()
        {
            ShaderMapping = new GLSLMapping110();
            ShaderVisitor = new GLSLVisitor100();
        }

        protected override StringBuilder SetDefaultPrecision()
        {
            DebugLog.Warning("Target GLSLES requires [xSLPrecision] at 'FragmentShader()' to set " +
                               "the precision of data type 'float'. Using high precision as default");

            return new StringBuilder("precision highp float;").NewLine(2);
        }
    }
}