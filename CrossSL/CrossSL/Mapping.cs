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
        /// Initializes the <see cref="xSLTypeMapping"/> class.
        /// </summary>
        static xSLTypeMapping()
        {
            UpdateTypes();
        }

        /// <summary>
        ///     Some data types have to be resolved by reflection at runtime, as they are
        ///     protected and nested into the <see cref="xSLShader" /> class. They are
        ///     marked with the <see cref="xSLShader.MappingAttribute" /> attribute, which
        ///     also contains their GLSL equivalent as the constructor argument.
        /// </summary>
        private static void UpdateTypes()
        {
            var nestedTypes = typeof(xSLShader).GetNestedTypes(BindingFlags.NonPublic);
            var mappingAttr = nestedTypes.FirstOrDefault(type => type.Name == "MappingAttribute");
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
        /// <summary>
        /// All types whose methods need to be mapped by CrossSL 
        /// </summary>
        internal static HashSet<Type> Types = new HashSet<Type>
        {
            typeof (xSLShader),
            typeof (Math),
            typeof (float2),
            typeof (float3),
            typeof (float4),
            typeof (float3x3),
            typeof (float4x4)
        };

        /// <summary>
        /// A lookup table for mapping methods from C# to GLSL
        /// </summary>
        internal static Dictionary<string, string> Methods = new Dictionary<string, string>
        {
            {"Normalize", "normalize"},
            {"Dot", "dot"},
            {"Max", "max"},
            {"Min", "min"},
        };

        /// <summary>
        /// Initializes the <see cref="xSLMethodMapping"/> class.
        /// </summary>
        static xSLMethodMapping()
        {
            UpdateMapping();
        }

        /// <summary>
        ///     Resolves all <see cref="xSLShader" /> methods by reflection at runtime,
        ///     so that they can change and new methods can be added without the need to
        ///     update this class' <see cref="Methods" /> field.
        /// </summary>
        private static void UpdateMapping()
        {
            var nestedTypes = typeof(xSLShader).GetNestedTypes(BindingFlags.NonPublic);
            var mappingAttr = nestedTypes.FirstOrDefault(type => type.Name == "MappingAttribute");

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