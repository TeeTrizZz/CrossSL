using System;

namespace CrossSL.Meta
{
    // ReSharper disable InconsistentNaming
    // ReSharper disable UnusedParameter.Local
    // ReSharper disable UnusedMember.Local

    public abstract partial class xSLShader
    {
        [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property,
            AllowMultiple = false)]
        public sealed class xSLAttributeAttribute : Attribute
        {
            // dummy implementation
        }

        [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property,
            AllowMultiple = false)]
        public sealed class xSLVaryingAttribute : Attribute
        {
            // dummy implementation
        }

        [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property,
            AllowMultiple = false)]
        public sealed class xSLUniformAttribute : Attribute
        {
            // dummy implementation
        }

        [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property,
            AllowMultiple = false)]
        public sealed class xSLConstAttribute : Attribute
        {
            // dummy implementation
        }

        [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
        protected sealed class xSLPrecisionAttribute : Attribute
        {
            public xSLPrecisionAttribute()
            {
                // dummy constructor
            }

            public xSLPrecisionAttribute(xSLEnvironment condition)
            {
                // dummy constructor
            }

            public xSLPrecision floatPrecision { get; set; }
            public xSLPrecision intPrecision { get; set; }
        }

        [AttributeUsage(AttributeTargets.Class)]
        protected sealed class xSLTargetAttribute : Attribute
        {
            public xSLTargetAttribute(xSLTarget.GLSL version)
            {
                // dummy constructor
            }

            public xSLTargetAttribute(xSLTarget.GLSLES version)
            {
                // dummy constructor
            }

            public xSLTargetAttribute(xSLTarget.GLSLMix version)
            {
                // dummy constructor
            }
        }

        [AttributeUsage(AttributeTargets.Class)]
        protected sealed class xSLDebugAttribute : Attribute
        {
            public xSLDebugAttribute(xSLDebug setting)
            {
                // dummy constructor
            }
        }

        [AttributeUsage(AttributeTargets.Property)]
        private sealed class VertexShaderAttribute : Attribute
        {
            // dummy implementation
        }

        [AttributeUsage(AttributeTargets.Property)]
        private sealed class FragmentShaderAttribute : Attribute
        {
            // dummy implementation
        }

        [AttributeUsage(AttributeTargets.Property)]
        private sealed class MandatoryAttribute : Attribute
        {
            // dummy implementation
        }

        [AttributeUsage(AttributeTargets.Struct | AttributeTargets.Method)]
        private sealed class MappingAttribute : Attribute
        {
            public MappingAttribute(string GLSL)
            {
                // dummy implementation            
            }
        }
    }

    // ReSharper restore UnusedMember.Local
    // ReSharper restore UnusedParameter.Local
    // ReSharper restore InconsistentNaming
}