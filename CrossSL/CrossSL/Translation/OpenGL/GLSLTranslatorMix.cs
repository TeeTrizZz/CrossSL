using System.Text;

namespace CrossSL
{
    internal sealed class GLSLTranslatorMix : GLSLTranslator110
    {
        public GLSLTranslatorMix()
        {
            ShaderMapping = new GLSLMapping110();
            ShaderVisitor = new GLSLVisitorMix();
        }

        protected override StringBuilder SetDefaultPrecision()
        {
            xSLConsole.Warning("Target GLSLES requires [xSLPrecision] at 'FragmentShader()' to set " +
                               "the precision of data type 'float'. Using high precision as default");

            var result = new StringBuilder("#ifdef GL_ES").NewLine().Intend();
            return result.Append("precision highp float;").NewLine().Append("#endif").NewLine(2);
        }
    }
}