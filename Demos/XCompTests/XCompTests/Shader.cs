﻿using System.Diagnostics;
using Fusee.Math;

namespace XCompTests
{
    // ReSharper disable InconsistentNaming
    // ReSharper disable UnusedMember.Local
    // ReSharper disable StaticFieldInGenericType
    // ReSharper disable UnusedAutoPropertyAccessor.Local

    public abstract class xSLShader
    {
        #region VERTEX SHADER VARIABLES

        // vertex shader / output variables (RW)
        protected float4 glPosition { get; set; }  // must be set!
        protected float glPointSize { get; set; }
        protected float4 glClipVertex { get; set; }

        // vertex shader / attribute variables (RO)
        protected float4 glVertex { get; private set; }
        protected float3 glNormal { get; private set; }
        protected float4 glColor { get; private set; }
        protected float4 glSecondaryColor { get; private set; }
        protected float4 glMultiTexCoord0 { get; private set; }
        protected float4 glMultiTexCoord1 { get; private set; }
        protected float4 glMultiTexCoord2 { get; private set; }
        protected float4 glMultiTexCoord3 { get; private set; }
        protected float4 glMultiTexCoord4 { get; private set; }
        protected float4 glMultiTexCoord5 { get; private set; }
        protected float4 glMultiTexCoord6 { get; private set; }
        protected float4 glMultiTexCoord7 { get; private set; }
        protected float glFogCoord { get; private set; }

        // vertex shader / varying output (RW)
        protected float4 glFrontColor { get; set; }
        protected float4 glBackColor { get; set; }
        protected float4 glFrontSecondaryColor { get; set; }
        protected float4 glBackSecondaryColor { get; set; }
        protected float4[] TexCoord { get; set; }
        protected float glFogFragCoord { get; set; }

        #endregion

        #region FRAGMENT SHADER VARIABLES

        // fragment shader / output variables (RW)
        protected float4 glFragColor { get; set; }
        protected float4[] glFragData { get; set; }
        protected float glFragDepth { get; set; }

        // fragment shader / varying input (RO)
        // ---> glColor, glSecondaryColor, glTexCoord[], glFogFragCoord

        // fragment shader / input variables (RO)
        protected float4 glFragCoord { get; private set; }
        protected bool glFrontFacing { get; private set; }

        #endregion

        #region BUILT-IN CONSTANTS

        protected int glMaxVertexUniformComponents { get; private set; }
        protected int glMaxFragmentUniformComponents { get; private set; }
        protected int glMaxVertexAttribs { get; private set; }
        protected int glMaxVaryingFloats { get; private set; }
        protected int glMaxDrawBuffers { get; private set; }
        protected int glMaxTextureCoords { get; private set; }
        protected int glMaxTextureUnits { get; private set; }
        protected int glMaxTextureImageUnits { get; private set; }
        protected int glMaxVertexTextureImageUnits { get; private set; }
        protected int glMaxCombinedTextureImageUnits { get; private set; }
        protected int glMaxLights { get; private set; }
        protected int glMaxClipPlanes { get; private set; }

        #endregion

        #region BUILT-IN UNIFORMS

        protected float4x4 glModelViewMatrix { get; private set; }
        protected float4x4 glModelViewProjectionMatrix { get; private set; }
        protected float4x4 glProjectionMatrix { get; private set; }
        protected float4x4[] glTextureMatrix { get; private set; }

        protected float4x4 glModelViewMatrixInverse { get; private set; }
        protected float4x4 glModelViewProjectionMatrixInverse { get; private set; }
        protected float4x4 glProjectionMatrixInverse { get; private set; }
        protected float4x4[] glTextureMatrixInverse { get; private set; }

        protected float4x4 glModelViewMatrixTranspose { get; private set; }
        protected float4x4 glModelViewProjectionMatrixTranspose { get; private set; }
        protected float4x4 glProjectionMatrixTranspose { get; private set; }
        protected float4x4[] glTextureMatrixTranspose { get; private set; }

        protected float4x4 glModelViewMatrixInverseTranspose { get; private set; }
        protected float4x4 glModelViewProjectionMatrixInverseTranspose { get; private set; }
        protected float4x4 glProjectionMatrixInverseTranspose { get; private set; }
        protected float4x4[] glTextureMatrixInverseTranspose { get; private set; }

        protected float4x4 glNormalMatrix { get; private set; }
        protected float4x4 glNormalScale { get; private set; }

        protected struct glDepthRangeParameters
        {
            public float Near;
            public float Far;
            public float Diff;
        }

        protected glDepthRangeParameters glDepthRange { get; private set; }

        protected struct glFogParameters
        {
            public float4 Color;
            public float Density;
            public float Start;
            public float End;
            public float Scale;
        }

        protected glFogParameters glFog { get; private set; }

        protected struct glLightSourceParameters
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

        protected glLightSourceParameters[] glLightSource { get; private set; }

        protected struct glLightModelParameters
        {
            public float4 Ambient;
        }

        protected glLightModelParameters glLightModel { get; private set; }

        protected struct glLightModelProducts
        {
            public float4 SceneColor;
        }

        protected glLightModelProducts glFrontLightModelProduct { get; private set; }
        protected glLightModelProducts glBackLightModelProduct { get; private set; }

        protected struct glLightProducts
        {
            public float4 Ambient;
            public float4 Diffuse;
            public float4 Specular;
        }

        protected glLightProducts[] glFrontLightProduct { get; private set; }
        protected glLightProducts[] glBackLightProduct { get; private set; }

        protected struct glMaterialParameters
        {
            public float4 Emission;
            public float4 Ambient;
            public float4 Diffuse;
            public float4 Specular;
            public float Shininess;
        }

        protected glMaterialParameters glFrontMaterial { get; private set; }
        protected glMaterialParameters glBackMaterial { get; private set; }

        protected struct glPointParameters
        {
            public float Size;
            public float SizeMin;
            public float SizeMax;
            public float FadeThresholdSize;
            public float DistanceConstantAttenuation;
            public float DistanceLinearAttenuation;
            public float DistanceQuadraticAttenuation;
        }

        protected glPointParameters glPoint { get; private set; }

        protected float4[] glTextureEnvColor { get; private set; }

        protected float4[] glClipPlane { get; private set; }

        protected float4[] glEyePlaneS { get; private set; }
        protected float4[] glEyePlaneT { get; private set; }
        protected float4[] glEyePlaneR { get; private set; }
        protected float4[] glEyePlaneQ { get; private set; }

        protected float4[] glObjectPlaneS { get; private set; }
        protected float4[] glObjectPlaneT { get; private set; }
        protected float4[] glObjectPlaneR { get; private set; }
        protected float4[] glObjectPlaneQ { get; private set; }

        #endregion

        // SHADER MAIN
        internal abstract void VertexShader();
        internal abstract void FragmentShader();

        // BUILT-IN FUNCTIONS
        protected float4 texture2D(sampler2D sampler, float2 coord)
        {
            return new float4(1, 1, 1, 1);
        }

        protected float4 texture2D(sampler2D sampler, float2 coord, float bias)
        {
            return new float4(1, 1, 1, 1);
        }

        // datatypes
        protected struct sampler2D { }
    }

    public sealed class xSL<TShader>
        where TShader : xSLShader, new()
    {
        private static string _vertex = "... default ...";
        private static string _fragment = "... default ...";
        private static TShader _instance;

        static xSL()
        {
            // to be filled by xSL

            _instance = new TShader();
        }

        private static void VertexInfo()
        {
            // to be modified by xSL
            Debug.WriteLine("xSL: Vertex shader of " +
                typeof (TShader).Name + " has not been translated.");
        }

        private static void FragmentInfo()
        {
            // to be modified by xSL
            Debug.WriteLine("xSL: Fragment shader of " +
                typeof (TShader).Name + " has not been translated.");
        }

        private static void ShaderObjInfo()
        {
            // to be modified by xSL
            Debug.WriteLine("xSL: Shader " +
                typeof(TShader).Name + " has not been translated.");
        }

        public static string VertexShader
        {
            get
            {
                VertexInfo();
                return _vertex;
            }

            private set { _vertex = value; }
        }

        public static string FragmentShader
        {
            get
            {
                FragmentInfo();
                return _fragment;
            }

            private set { _fragment = value; }
        }

        public static TShader ShaderObject
        {
            get
            {
                ShaderObjInfo();
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