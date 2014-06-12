using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CrossSL.Meta;
using Fusee.Math;

namespace CrossSL
{
    // ReSharper disable InconsistentNaming
    // ReSharper disable UnusedParameter.Local

    internal enum xSLShaderType
    {
        VertexShader,
        FragmentShader
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

    internal static class xSLTypeMapping
    {
        internal static Dictionary<Type, string> Types = new Dictionary<Type, string>
        {
            {typeof (void), "void"},
            {typeof (int), "int"},
            {typeof (float), "float"},
            {typeof (float2), "vec2"},
            {typeof (float3), "vec3"},
            {typeof (float4), "vec4"},
            {typeof (float3x3), "mat3"},
            {typeof (float4x4), "mat4"}
        };

        /// <summary>
        ///     Some data types have to be resolved by reflection at runtime, as they are
        ///     protected and nested into the <see cref="xSLShader" /> class. They are
        ///     marked with the <see cref="CrossSL.Meta.xSLDataTypeAttribute" /> attribute,
        ///     which also contains their GLSL equivalent as the constructor argument.
        /// </summary>
        internal static void UpdateTypes()
        {
            var mappingAttr = typeof (xSLMappingAttribute);
            var nestedTypes = typeof (xSLShader).GetNestedTypes(BindingFlags.NonPublic);
            var dataTypes = nestedTypes.Where(type => type.GetCustomAttribute(mappingAttr) != null);

            foreach (var type in dataTypes)
            {
                var attrData = CustomAttributeData.GetCustomAttributes(type);
                var typeData = attrData.First(attr => attr.AttributeType == mappingAttr);
                Types.Add(type, typeData.ConstructorArguments[0].Value.ToString());
            }
        }
    }

    internal static class xSLMethodMapping
    {
        internal static HashSet<Type> Types = new HashSet<Type>
        {
            typeof (xSLShader),
            typeof (Math)
        };

        internal static Dictionary<string, string> Methods = new Dictionary<string, string>
        {
            {"Normalize", "normalize"},
            {"Dot", "dot"},
            {"Max", "max"},
            {"Min", "min"},
        };

        /// <summary>
        ///     Resolves all <see cref="Fusee.Math" /> types and all <see cref="xSLShader" />
        ///     methods by reflection at runtime, so that they can change and new types can be
        ///     added without the need to update this class' <see cref="Types" /> field.
        /// </summary>
        internal static void UpdateMapping()
        {
            // update types
            var lookUpType = typeof (float4);
            var lookUpNS = lookUpType.Namespace;
            var lookUpAssembly = lookUpType.Assembly;

            Types.UnionWith(lookUpAssembly.GetTypes().Where(type => type.Namespace == lookUpNS));

            // update methods
            var mappingAttr = typeof (xSLMappingAttribute);
            var allmethods = typeof (xSLShader).GetMethods(BindingFlags.NonPublic | BindingFlags.Instance);
            var shMethods = allmethods.Where(type => type.GetCustomAttribute(mappingAttr) != null).ToList();

            foreach (var method in shMethods)
            {
                var attrData = CustomAttributeData.GetCustomAttributes(method);
                var methodData = attrData.First(attr => attr.AttributeType == mappingAttr);

                if (!Methods.ContainsKey(method.Name))
                    Methods.Add(method.Name, methodData.ConstructorArguments[0].Value.ToString());
            }
        }
    }

    // ReSharper restore InconsistentNaming
    // ReSharper restore UnusedParameter.Local
}