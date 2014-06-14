namespace CrossSL
{
    // ReSharper disable InconsistentNaming

    internal enum xSLShaderType
    {
        VertexShader,
        FragmentShader
    }

    internal static class xSLVersion
    {
        internal static string[][] VIDs =
        {
            new[]
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
            },
            new[]
            {
                "100"
            },
            new[]
            {
                "110"
            }
        };
    }

    internal enum xSLVariableType
    {
        xSLUnknown,
        xSLAttributeAttribute,
        xSLVaryingAttribute,
        xSLUniformAttribute,
        xSLConstAttribute
    }

    // ReSharper restore InconsistentNaming
}
