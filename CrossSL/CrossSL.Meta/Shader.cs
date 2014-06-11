using System;
using System.Diagnostics;
using Fusee.Math;

namespace CrossSL.Meta
{
    // ReSharper disable InconsistentNaming
    // ReSharper disable UnusedMember.Local
    // ReSharper disable StaticFieldInGenericType
    // ReSharper disable UnusedAutoPropertyAccessor.Local

    public abstract class xSLShader
    {
        private class VertexShaderAttribute : Attribute
        {
            // dummy implementation
        }

        private class FragmentShaderAttribute : Attribute
        {
            // dummy implementation
        }

        private class MandatoryAttribute : Attribute
        {
            // dummy implementation
        }

        #region VERTEX/FRAGMENT SHADER VARIABLES

        // vertex/fragment shader / attribute variables (RO)

        [VertexShader, FragmentShader]
        protected float4 xslColor { get; private set; }

        [VertexShader, FragmentShader]
        protected float4 xslSecondaryColor { get; private set; }

        // vertex/fragment shader / varying output/input (RW)

        [VertexShader, FragmentShader]
        protected float4[] TexCoord { get; set; }

        [VertexShader, FragmentShader]
        protected float xslFogFragCoord { get; set; }

        #endregion

        #region VERTEX SHADER VARIABLES

        // vertex shader / output variables (RW)

        [VertexShader, Mandatory]
        protected float4 xslPosition { get; set; }

        [VertexShader]
        protected float xslPointSize { get; set; }

        [VertexShader]
        protected float4 xslClipVertex { get; set; }

        // vertex shader / attribute variables (RO)

        [VertexShader]
        protected float4 xslVertex { get; private set; }

        [VertexShader]
        protected float3 xslNormal { get; private set; }

        [VertexShader]
        protected float4 xslMultiTexCoord0 { get; private set; }

        [VertexShader]
        protected float4 xslMultiTexCoord1 { get; private set; }

        [VertexShader]
        protected float4 xslMultiTexCoord2 { get; private set; }

        [VertexShader]
        protected float4 xslMultiTexCoord3 { get; private set; }

        [VertexShader]
        protected float4 xslMultiTexCoord4 { get; private set; }

        [VertexShader]
        protected float4 xslMultiTexCoord5 { get; private set; }

        [VertexShader]
        protected float4 xslMultiTexCoord6 { get; private set; }

        [VertexShader]
        protected float4 xslMultiTexCoord7 { get; private set; }

        [VertexShader]
        protected float xslFogCoord { get; private set; }

        // vertex shader / varying output (RW)

        [VertexShader]
        protected float4 xslFrontColor { get; set; }

        [VertexShader]
        protected float4 xslBackColor { get; set; }

        [VertexShader]
        protected float4 xslFrontSecondaryColor { get; set; }

        [VertexShader]
        protected float4 xslBackSecondaryColor { get; set; }

        #endregion

        #region FRAGMENT SHADER VARIABLES

        // fragment shader / output variables (RW)

        [FragmentShader, Mandatory]
        protected float4 xslFragColor { get; set; }

        [FragmentShader]
        protected float4[] xslFragData { get; set; }

        [FragmentShader]
        protected float xslFragDepth { get; set; }

        // fragment shader / input variables (RO)

        [FragmentShader]
        protected float4 xslFragCoord { get; private set; }

        [FragmentShader]
        protected bool xslFrontFacing { get; private set; }

        #endregion

        #region BUILT-IN CONSTANTS

        protected int xslMaxVertexUniformComponents { get; private set; }
        protected int xslMaxFragmentUniformComponents { get; private set; }
        protected int xslMaxVertexAttribs { get; private set; }
        protected int xslMaxVaryingFloats { get; private set; }
        protected int xslMaxDrawBuffers { get; private set; }
        protected int xslMaxTextureCoords { get; private set; }
        protected int xslMaxTextureUnits { get; private set; }
        protected int xslMaxTextureImageUnits { get; private set; }
        protected int xslMaxVertexTextureImageUnits { get; private set; }
        protected int xslMaxCombinedTextureImageUnits { get; private set; }
        protected int xslMaxLights { get; private set; }
        protected int xslMaxClipPlanes { get; private set; }

        #endregion

        #region BUILT-IN UNIFORMS

        protected float4x4 xslModelViewMatrix { get; private set; }
        protected float4x4 xslModelViewProjectionMatrix { get; private set; }
        protected float4x4 xslProjectionMatrix { get; private set; }
        protected float4x4[] xslTextureMatrix { get; private set; }

        protected float4x4 xslModelViewMatrixInverse { get; private set; }
        protected float4x4 xslModelViewProjectionMatrixInverse { get; private set; }
        protected float4x4 xslProjectionMatrixInverse { get; private set; }
        protected float4x4[] xslTextureMatrixInverse { get; private set; }

        protected float4x4 xslModelViewMatrixTranspose { get; private set; }
        protected float4x4 xslModelViewProjectionMatrixTranspose { get; private set; }
        protected float4x4 xslProjectionMatrixTranspose { get; private set; }
        protected float4x4[] xslTextureMatrixTranspose { get; private set; }

        protected float4x4 xslModelViewMatrixInverseTranspose { get; private set; }
        protected float4x4 xslModelViewProjectionMatrixInverseTranspose { get; private set; }
        protected float4x4 xslProjectionMatrixInverseTranspose { get; private set; }
        protected float4x4[] xslTextureMatrixInverseTranspose { get; private set; }

        protected float4x4 xslNormalMatrix { get; private set; }
        protected float4x4 xslNormalScale { get; private set; }

        protected struct xslDepthRangeParameters
        {
            public float Near;
            public float Far;
            public float Diff;
        }

        protected xslDepthRangeParameters xslDepthRange { get; private set; }

        protected struct xslFogParameters
        {
            public float4 Color;
            public float Density;
            public float Start;
            public float End;
            public float Scale;
        }

        protected xslFogParameters xslFog { get; private set; }

        protected struct xslLightSourceParameters
        {
            public float4 Ambient;
            public float4 Diffuse;
            public float4 Specular;
            public float4 Position;
            public float4 HalfVector;
            public float3 SpotDirection;
            public float SpotExponent;
            public float SpotCutoff;
            public float SpotCosCutoff;
            public float ConstantAttenuation;
            public float LinearAttentuation;
            public float QuadraticAttenuation;
        }

        protected xslLightSourceParameters[] xslLightSource { get; private set; }

        protected struct xslLightModelParameters
        {
            public float4 Ambient;
        }

        protected xslLightModelParameters xslLightModel { get; private set; }

        protected struct xslLightModelProducts
        {
            public float4 SceneColor;
        }

        protected xslLightModelProducts xslFrontLightModelProduct { get; private set; }
        protected xslLightModelProducts xslBackLightModelProduct { get; private set; }

        protected struct xslLightProducts
        {
            public float4 Ambient;
            public float4 Diffuse;
            public float4 Specular;
        }

        protected xslLightProducts[] xslFrontLightProduct { get; private set; }
        protected xslLightProducts[] xslBackLightProduct { get; private set; }

        protected struct xslMaterialParameters
        {
            public float4 Emission;
            public float4 Ambient;
            public float4 Diffuse;
            public float4 Specular;
            public float Shininess;
        }

        protected xslMaterialParameters xslFrontMaterial { get; private set; }
        protected xslMaterialParameters xslBackMaterial { get; private set; }

        protected struct xslPointParameters
        {
            public float Size;
            public float SizeMin;
            public float SizeMax;
            public float FadeThresholdSize;
            public float DistanceConstantAttenuation;
            public float DistanceLinearAttenuation;
            public float DistanceQuadraticAttenuation;
        }

        protected xslPointParameters xslPoint { get; private set; }

        protected float4[] xslTextureEnvColor { get; private set; }

        protected float4[] xslClipPlane { get; private set; }

        protected float4[] xslEyePlaneS { get; private set; }
        protected float4[] xslEyePlaneT { get; private set; }
        protected float4[] xslEyePlaneR { get; private set; }
        protected float4[] xslEyePlaneQ { get; private set; }

        protected float4[] xslObjectPlaneS { get; private set; }
        protected float4[] xslObjectPlaneT { get; private set; }
        protected float4[] xslObjectPlaneR { get; private set; }
        protected float4[] xslObjectPlaneQ { get; private set; }

        #endregion

        // SHADER MAIN
        protected abstract void VertexShader();
        protected abstract void FragmentShader();

        // BUILT-IN FUNCTIONS
        protected float4 Texture2D(sampler2D sampler, float2 coord)
        {
            return new float4(1, 1, 1, 1);
        }

        protected float4 Texture2D(sampler2D sampler, float2 coord, float bias)
        {
            return new float4(1, 1, 1, 1);
        }

        // datatypes
        [xSLDataType("sampler2D")]
        protected struct sampler2D
        {
        }
    }

    public sealed class xSL<TShader> where TShader : xSLShader, new()
    {
        // ReSharper disable FieldCanBeMadeReadOnly.Local
        private static bool _translated;
        private static string _error;
        // ReSharper restore FieldCanBeMadeReadOnly.Local

        private static string _vertex;
        private static string _fragment;

        private static TShader _instance;

        static xSL()
        {
            _translated = false;
            _error = String.Empty;

            _vertex = "... default ...";
            _fragment = "... default ...";

            Init();

            _instance = new TShader();
        }

        // to be modified by xSL
        private static void Init()
        {
            // dummy implementation
        }

        // to be modified by xSL
        private static void ShaderInfo()
        {
            if (!_translated)
                Debug.WriteLine("xSL: Shader '" + typeof (TShader).Name +
                    "' has not been translated.");

            if (_error != String.Empty)
                throw new ApplicationException(_error);
        }

        public static string VertexShader
        {
            get
            {
                ShaderInfo();
                return _vertex;
            }

            private set { _vertex = value; }
        }

        public static string FragmentShader
        {
            get
            {
                ShaderInfo();
                return _fragment;
            }

            private set { _fragment = value; }
        }

        public static TShader ShaderObject
        {
            get
            {
                ShaderInfo();
                return _instance;
            }

            private set { _instance = value; }
        }
    }

    // ReSharper restore UnusedAutoPropertyAccessor.Local
    // ReSharper restore InconsistentNaming
    // ReSharper restore UnusedMember.Local
    // ReSharper restore StaticFieldInGenericType
}