using System;

namespace CrossSL.Meta
{
    // ReSharper disable InconsistentNaming
    // ReSharper disable UnusedParameter.Local

    public abstract partial class xSLShader
    {
        [AttributeUsage(AttributeTargets.Struct | AttributeTargets.Method)]
        private sealed class MappingAttribute : Attribute
        {
            public MappingAttribute(string GLSL)
            {
                // dummy implementation            
            }
        }

        [Mapping("sampler2D")]
        protected struct sampler2D
        {
            // dummy implementation
        }
    }

    // ReSharper restore UnusedParameter.Local
    // ReSharper restore InconsistentNaming
}