using System;
using System.Collections.Generic;
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
        internal static Dictionary<Type, string> Types = new Dictionary<Type, string>
        {
            {typeof (int), "int"},
            {typeof (float), "float"},
            {typeof (float2), "vec2"},
            {typeof (float3), "vec3"},
            {typeof (float4), "vec4"},
            {typeof (float4x4), "mat4"},
        };

        /// <summary>
        ///     Some data types have to be resolved by reflection at runtime, as they are
        ///     protected and nested into the <see cref="xSLShader" /> class. They are
        ///     marked with the <see cref="XCompTests.xSLDataTypeAttribute" /> attribute,
        ///     which also contains their GLSL equivalent as the constructor argument.
        /// </summary>
        internal static void UpdateTypes()
        {
            var typeAttr = typeof (xSLDataTypeAttribute);
            var nestedTypes = typeof (xSLShader).GetNestedTypes(BindingFlags.NonPublic);
            var dataTypes = nestedTypes.Where(type => type.GetCustomAttribute(typeAttr) != null);

            foreach (var type in dataTypes)
            {
                var attrData = CustomAttributeData.GetCustomAttributes(type);
                var typeData = attrData.First(attr => attr.AttributeType == typeAttr);
                Types.Add(type, typeData.ConstructorArguments[0].Value.ToString());
            }
        }
    }

    // ReSharper restore InconsistentNaming
    // ReSharper restore UnusedParameter.Local
}