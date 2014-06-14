using System;
using System.Linq;
using System.Text;
using Fusee.Math;
using ICSharpCode.Decompiler;
using ICSharpCode.NRefactory.CSharp;
using Mono.Cecil;

namespace CrossSL
{
    class GLSLVisitor110 : GLSLVisitor
    {
        internal GLSLVisitor110(AstNode methodBody, DecompilerContext decContext)
            : base(methodBody, decContext)
        {
            // base
        }

        /// <summary>
        ///     Translates a primitive type, e.g. "1f".
        /// </summary>
        /// <remarks>
        ///     GLSL 1.1 does not support type suffix.
        /// </remarks>
        public override StringBuilder VisitPrimitiveExpression(PrimitiveExpression primitiveExpr)
        {
            var result = base.VisitPrimitiveExpression(primitiveExpr);
            return result.Replace("f", String.Empty);
        }

        /// <summary>
        ///     Translates an object creation, e.g. "new float4(...)".
        /// </summary>
        /// <remarks>
        ///     GLSL 1.1 does not support matrix casts.
        /// </remarks>
        public override StringBuilder VisitObjectCreateExpression(ObjectCreateExpression objCreateExpr)
        {
            var result = base.VisitObjectCreateExpression(objCreateExpr);

            if (!(objCreateExpr.GetType() == typeof (SimpleType)))
                return result;

            var simpleType = (SimpleType) objCreateExpr.Type;
            var dataType = simpleType.Annotation<TypeReference>().ToType();

            if (dataType == typeof (float3x3))
            {
                var methodRef = (MethodReference) objCreateExpr.Annotations.First();
                var methodDef = methodRef.Resolve();
                var methodParam = methodDef.Parameters.FirstOrDefault();

                if (methodParam != null && methodParam.ParameterType.IsType<float4x4>())
                {
                    var instr = GetInstructionFromStmt(objCreateExpr.GetParent<Statement>());
                    Helper.Error("Matrix casting (float4x4 to float3x3) is not supported in GLSL 1.0", instr);
                }
            }

            return new StringBuilder();
        }
    }
}
