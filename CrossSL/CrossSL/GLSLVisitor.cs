using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.ILAst;
using ICSharpCode.NRefactory;
using ICSharpCode.NRefactory.CSharp;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace CrossSL
{
    internal class GLSLVisitor : ShaderVisitor
    {
        internal GLSLVisitor(BlockStatement methodBody, DecompilerContext decContext)
            : base(methodBody, decContext)
        {
            Result = methodBody.AcceptVisitor(this, 0).ToString();
        }

        /// <summary>
        ///     Translates a block statement, e.g. a method's body.
        /// </summary>
        public override StringBuilder VisitBlockStatement(BlockStatement blockStmt, int data)
        {
            var result = new StringBuilder().NewLine().Open();

            foreach (var statement in blockStmt.Statements)
                result.NewLine().Intend(1).Append(statement.AcceptVisitor(this, data));

            return result.NewLine().Close();
        }

        /// <summary>
        ///     Translates a variable declaration statement, e.g. "float4 x;".
        /// </summary>
        public override StringBuilder VisitVariableDeclarationStatement(
            VariableDeclarationStatement varDeclStmt, int data)
        {
            var result = new StringBuilder();
           // var methodDef = varDeclStmt.
            var type = varDeclStmt.Type.AcceptVisitor(this, data);
            foreach (var varDecl in varDeclStmt.Variables)
                result.Append(type).Space().Append(varDecl.AcceptVisitor(this, data)).Semicolon();

            return result;
        }

        /// <summary>
        ///     Translates a data type, e.g. "float4".
        /// </summary>
        public override StringBuilder VisitSimpleType(SimpleType simpleType, int data)
        {
            var typeRef = simpleType.Annotation<TypeReference>();
            var sysType = xSLHelper.ResolveRef(typeRef);

            if (sysType == null || !xSLDataType.Types.ContainsKey(sysType))
            {
                var parentNode = simpleType.GetParent<Statement>();
                Instruction instrAtOffset = null;

                if (parentNode != null)
                {
                    var ilRange = GetAnnotations<List<ILRange>>(parentNode).First();
                    var instructions = DecContext.CurrentMethod.Body.Instructions;
                    instrAtOffset = instructions.First(il => il.Offset == ilRange.From);
                }

                xSLHelper.WriteToConsole("    => ERROR: Type \"" + simpleType + "\" is unsupported", instrAtOffset);
            }
            else
            {
                var typeTrans = xSLDataType.Types[sysType];
                return new StringBuilder(typeTrans);
            }

            return new StringBuilder(simpleType.ToString());
        }

        /// <summary>
        ///     Translates a variable initializer, e.g. "x" or "x = 5"
        /// </summary>
        public override StringBuilder VisitVariableInitializer(VariableInitializer variableInit, int data)
        {
            var result = new StringBuilder(variableInit.Name);

            if (!variableInit.Initializer.IsNull)
                result.Assign().Append(variableInit.Initializer.AcceptVisitor(this, data));

            return result;
        }

        /// <summary>
        ///     Translates an expression statement, e.g. "x = a + b"
        /// </summary>
        public override StringBuilder VisitExpressionStatement(ExpressionStatement exprStmt, int data)
        {
            return exprStmt.Expression.AcceptVisitor(this, data).Semicolon();
        }

        /// <summary>
        ///     Translates an assignment statement, e.g. "x = a + b"
        /// </summary>
        public override StringBuilder VisitAssignmentExpression(AssignmentExpression assignmentExpr, int data)
        {
            var result = assignmentExpr.Left.AcceptVisitor(this, data);

            // operator assignment type
            var opAssignment = new Dictionary<AssignmentOperatorType, string>
            {
                {AssignmentOperatorType.Assign, ""},
                {AssignmentOperatorType.Add, "+"},
                {AssignmentOperatorType.BitwiseAnd, "&"},
                {AssignmentOperatorType.BitwiseOr, "|"},
                {AssignmentOperatorType.Divide, "/"},
                {AssignmentOperatorType.ExclusiveOr, "^"},
                {AssignmentOperatorType.Modulus, "%"},
                {AssignmentOperatorType.Multiply, "*"},
                {AssignmentOperatorType.ShiftLeft, "<<"},
                {AssignmentOperatorType.ShiftRight, ">>"},
                {AssignmentOperatorType.Subtract, "-"},
            };

            result.Assign(opAssignment[assignmentExpr.Operator]);
            return result.Append(assignmentExpr.Right.AcceptVisitor(this, data));
        }
    }
}