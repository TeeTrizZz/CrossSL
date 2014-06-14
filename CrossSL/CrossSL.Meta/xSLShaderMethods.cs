using System;
using Fusee.Math;

namespace CrossSL.Meta
{
    // ReSharper disable InconsistentNaming

    public abstract partial class xSLShader
    {
        // SHADER MAIN
        protected abstract void VertexShader();
        protected abstract void FragmentShader();

        // BUILT-IN FUNCTIONS
        [Mapping("texture2D")]
        protected float4 Texture2D(sampler2D sampler, float2 coord)
        {
            return new float4(1, 1, 1, 1);
        }

        [Mapping("texture2D")]
        protected float4 Texture2D(sampler2D sampler, float2 coord, float bias)
        {
            return new float4(1, 1, 1, 1);
        }
    }

    // ReSharper restore InconsistentNaming
}