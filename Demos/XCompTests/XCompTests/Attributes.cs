using System;

namespace XCompTests
{
    // ReSharper disable InconsistentNaming
    // ReSharper disable UnusedParameter.Local

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property,
        AllowMultiple = true)]
    public class xSLAttributeAttribute : Attribute
    {
        // dummy implementation
    }

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property,
        AllowMultiple = true)]
    public class xSLVaryingAttribute : Attribute
    {
        // dummy implementation
    }

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property,
        AllowMultiple = true)]
    public class xSLUniformAttribute : Attribute
    {
        // dummy implementation
    }

    public enum xSLEnvironment
    {
        OpenGL,
        OpenGLES
    }

    public class xSLTarget
    {
        public enum GLSL
        {
            V110,
            V120,
            V130,
            V140,
            V150,
            V330,
            V400,
            V420,
            V430,
            V440,
        }

        public enum GLSLES
        {
            V100
        }
    }

    [Flags]
    public enum xSLDebug
    {
        PreCompile,
        SaveToFile,
        ThrowException
    }

    public enum xSLPrecision
    {
        Low,
        Medium,
        High
    }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    internal sealed class xSLPrecisionAttribute : Attribute
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
    public sealed class xSLTargetAttribute : Attribute
    {
        public xSLTargetAttribute(xSLTarget.GLSL version)
        {
            // dummy constructor
        }

        public xSLTargetAttribute(xSLTarget.GLSLES version)
        {
            // dummy constructor
        }
    }

    [AttributeUsage(AttributeTargets.Class)]
    public sealed class xSLDebugAttribute : Attribute
    {
        public xSLDebugAttribute(xSLDebug setting)
        {
            // dummy constructor
        }  
    }

    // ReSharper restore InconsistentNaming
    // ReSharper restore UnusedParameter.Local
}