﻿using System;

namespace CrossSL.Meta
{
    // ReSharper disable InconsistentNaming
    // ReSharper disable UnusedParameter.Local

    public class xSLMappingAttribute : Attribute
    {
        public xSLMappingAttribute(string GLSL)
        {
            // dummy implementation            
        }
    }

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property,
        AllowMultiple = false)]
    public class xSLAttributeAttribute : Attribute
    {
        // dummy implementation
    }

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property,
        AllowMultiple = false)]
    public class xSLVaryingAttribute : Attribute
    {
        // dummy implementation
    }

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property,
        AllowMultiple = false)]
    public class xSLUniformAttribute : Attribute
    {
        // dummy implementation
    }

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property,
        AllowMultiple = false)]
    public class xSLConstAttribute : Attribute
    {
        // dummy implementation
    }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public sealed class xSLPrecisionAttribute : Attribute
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

        public xSLTargetAttribute(xSLTarget.GLSLMix version)
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