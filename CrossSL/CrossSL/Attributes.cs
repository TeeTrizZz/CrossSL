using System;

namespace CrossSL
{
    // ReSharper disable InconsistentNaming
    // ReSharper disable UnusedParameter.Local

    internal enum xSLTarget
    {
        GLSL,
        GLSLES
    }

    internal class xSLVersion
    {
        internal static string[] GLSL =
        {
            "110",
            "120",
            "130",
            "140",
            "150",
            "330",
            "400",
            "420",
            "430",
            "440"
        };

        internal static string[] GLSLES =
        {
            "100"
        };
    }

    [Flags]
    internal enum xSLDebug
    {
        None,
        PreCompile,
        SaveToFile,
        ThrowException
    }

    internal enum xSLPrecision
    {
        Low,
        Medium,
        High
    }

    // ReSharper restore InconsistentNaming
    // ReSharper restore UnusedParameter.Local
}