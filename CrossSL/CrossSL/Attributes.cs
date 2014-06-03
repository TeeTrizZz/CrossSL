using System;
using System.Linq;
using System.Reflection;
using Fusee.Math;
using XCompTests;

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
            }
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

    internal enum xSLVariableAttr
    {
        xSLAttributeAttribute,
        xSLVaryingAttribute,
        xSLUniformAttribute,
        xSLConstantAttribute
    }

    internal static class xSLDataType
    {
        internal static Type[] Types =
        {
            typeof (int),
            typeof (float),
            typeof (float2),
            typeof (float3),
            typeof (float4),
            typeof (float4x4)
        };

        /// <summary>
        ///     Some data types have to be resolved by reflection at runtime, as they are
        ///     protected and nested into the <see cref="xSLShader" /> class. They are
        ///     marked with the <see cref="XCompTests.xSLDataTypeAttribute" /> attribute.
        /// </summary>
        internal static void UpdateTypes()
        {
            var typeList = Types.ToList();

            var typeAttr = typeof (xSLDataTypeAttribute);
            var nestedTypes = typeof (xSLShader).GetNestedTypes(BindingFlags.NonPublic);
            var dataTypes = nestedTypes.Where(type => Attribute.GetCustomAttribute(type, typeAttr) != null);

            typeList.AddRange(dataTypes);
            Types = typeList.ToArray();            
        }
    }

    // ReSharper restore InconsistentNaming
    // ReSharper restore UnusedParameter.Local
}