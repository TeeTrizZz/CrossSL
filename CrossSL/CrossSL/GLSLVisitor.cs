using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.ILAst;
using ICSharpCode.NRefactory.CSharp;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.CompilerServices.SymbolWriter;

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
            var result = new StringBuilder().NewLine().OBraces();

            foreach (var statement in blockStmt.Statements)
                result.NewLine().Intend().Append(statement.AcceptVisitor(this, data));

            return result.NewLine().CBraces();
        }

        /// <summary>
        ///     Translates a variable declaration statement, e.g. "float4 x;".
        /// </summary>
        public override StringBuilder VisitVariableDeclarationStatement(
            VariableDeclarationStatement varDeclStmt, int data)
        {
            var result = new StringBuilder();

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
                var instr = GetInstructionFromStmt(simpleType.GetParent<Statement>());
                xSLHelper.Error("Type \"" + simpleType + "\" is not supported", instr);
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

            // assignment operator type mapping
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

        /// <summary>
        ///     Translates a binary operator expression, e.g. "a * b"
        /// </summary>
        public override StringBuilder VisitBinaryOperatorExpression(BinaryOperatorExpression binaryOpExpr, int data)
        {
            var result = new StringBuilder();

            if (binaryOpExpr.Operator == BinaryOperatorType.Modulus)
            {
                var leftOp = binaryOpExpr.Left.AcceptVisitor(this, data).ToString();
                var rightOp = binaryOpExpr.Right.AcceptVisitor(this, data).ToString();

                result.Method("mod", leftOp, rightOp);
            }
            else
            {
                result.Append(binaryOpExpr.Left.AcceptVisitor(this, data)).OParent();

                // binary operator type mapping
                var opAssignment = new Dictionary<BinaryOperatorType, string>
                {
                    {BinaryOperatorType.Add, "+"},
                    {BinaryOperatorType.BitwiseAnd, "&"},
                    {BinaryOperatorType.BitwiseOr, "|"},
                    {BinaryOperatorType.ConditionalAnd, "&&"},
                    {BinaryOperatorType.ConditionalOr, "||"},
                    {BinaryOperatorType.Divide, "/"},
                    {BinaryOperatorType.Equality, "=="},
                    {BinaryOperatorType.ExclusiveOr, "^"},
                    {BinaryOperatorType.GreaterThan, ">"},
                    {BinaryOperatorType.GreaterThanOrEqual, ">="},
                    {BinaryOperatorType.InEquality, "!="},
                    {BinaryOperatorType.LessThan, "<"},
                    {BinaryOperatorType.LessThanOrEqual, "<="},
                    {BinaryOperatorType.Multiply, "*"},
                    {BinaryOperatorType.NullCoalescing, "??"},
                    {BinaryOperatorType.ShiftLeft, "<<"},
                    {BinaryOperatorType.ShiftRight, ">>"},
                    {BinaryOperatorType.Subtract, "-"}
                };

                result.Assign(opAssignment[binaryOpExpr.Operator]);
                result.Append(binaryOpExpr.Right.AcceptVisitor(this, data)).CParent();
            }

            return result;
        }

        /// <summary>
        ///     Translates a member reference, e.g. "_field"
        /// </summary>
        public override StringBuilder VisitMemberReferenceExpression(MemberReferenceExpression memberRefExpr, int data)
        {
            var result = new StringBuilder();

            if (!(memberRefExpr.Target is ThisReferenceExpression))
                result.Append(memberRefExpr.Target.AcceptVisitor(this, data)).Dot();

            return result.Append(memberRefExpr.MemberName);
        }

        /// <summary>
        ///     Translates an object creation expression, e.g. "new float4(...)"
        /// </summary>
        public override StringBuilder VisitObjectCreateExpression(ObjectCreateExpression objCreateExpr, int data)
        {
            var type = objCreateExpr.Type.AcceptVisitor(this, data);
            var args = ArgJoin(objCreateExpr.Arguments);

            return type.OParent().Append(args).CParent();
        }

        /// <summary>
        ///     Translates a primitive type, e.g. "1f"
        /// </summary>
        public override StringBuilder VisitPrimitiveExpression(PrimitiveExpression primitiveExpr, int data)
        {
            var result = new StringBuilder();
            var cultureInfo = CultureInfo.InvariantCulture.NumberFormat;

            if (primitiveExpr.Value is double)
            {
                var dInstr = GetInstructionFromStmt(primitiveExpr.GetParent<Statement>());
                xSLHelper.Warning("Type \"double\" is not supported. " +
                                         "Value will be casted to type \"float\".", dInstr);
            }

            if (primitiveExpr.Value is float || primitiveExpr.Value is double)
            {
                var value = ((float) primitiveExpr.Value).ToString(cultureInfo);
                if (!value.Contains(".")) value += ".0";

                return result.Append(value).Append("f");
            }

            if (primitiveExpr.Value is uint)
            {
                var value = ((uint) primitiveExpr.Value).ToString(cultureInfo);
                return result.Append(value).Append("u");
            }

            if (primitiveExpr.Value is bool)
            {
                var value = primitiveExpr.Value.ToString().ToLower();
                return result.Append(value);
            }

            var instr = GetInstructionFromStmt(primitiveExpr.GetParent<Statement>());
            xSLHelper.Error("Type \"double\" is not supported.", instr);

            return result;
        }
    }
}