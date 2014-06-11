using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using CrossSL.Meta;
using ICSharpCode.Decompiler;
using ICSharpCode.NRefactory.CSharp;
using Mono.Cecil;

namespace CrossSL
{
    internal class GLSLVisitor : ShaderVisitor
    {
        internal GLSLVisitor(AstNode methodBody, DecompilerContext decContext)
            : base(methodBody, decContext)
        {
            Result = new StringBuilder().Block(methodBody.AcceptVisitor(this, 0));
        }

        /// <summary>
        ///     Translates a block statement, e.g. a method's body.
        /// </summary>
        /// <remarks>
        ///     If verbose mode is active (i.e. if a .pdb file was found) additional
        ///     line breaks between statements are considered in the output.
        /// </remarks>
        public override StringBuilder VisitBlockStatement(BlockStatement blockStmt, int data)
        {
            var result = new StringBuilder();

            foreach (var stmt in blockStmt.Statements)
                result.Append(stmt.AcceptVisitor(this, data)).NewLine();

            return result;
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
        ///     Translates a simple data type, e.g. "float4".
        /// </summary>
        public override StringBuilder VisitSimpleType(SimpleType simpleType, int data)
        {
            var sysType = simpleType.Annotation<TypeReference>().ToType();
            var mappedType = MapDataTypeIfValid(simpleType, sysType);

            return mappedType == null
                ? new StringBuilder(simpleType.ToString())
                : new StringBuilder(mappedType);
        }

        /// <summary>
        ///     Translates a primitive data type, e.g. "float".
        /// </summary>
        public override StringBuilder VisitPrimitiveType(PrimitiveType primitiveType, int data)
        {
            var typeName = primitiveType.KnownTypeCode;
            var sysType = Type.GetType("System." + typeName);

            var mappedType = MapDataTypeIfValid(primitiveType, sysType);

            return mappedType == null
                ? new StringBuilder(primitiveType.Keyword)
                : new StringBuilder(mappedType);
        }

        /// <summary>
        ///     Translates a variable initializer, e.g. "x" or "x = 5".
        /// </summary>
        public override StringBuilder VisitVariableInitializer(VariableInitializer variableInit, int data)
        {
            var result = new StringBuilder(variableInit.Name);

            if (!variableInit.Initializer.IsNull)
                result.Assign().Append(variableInit.Initializer.AcceptVisitor(this, data));

            return result;
        }

        /// <summary>
        ///     Translates an expression statement, e.g. "x = a + b".
        /// </summary>
        public override StringBuilder VisitExpressionStatement(ExpressionStatement exprStmt, int data)
        {
            return exprStmt.Expression.AcceptVisitor(this, data).Semicolon();
        }

        /// <summary>
        ///     Translates an assignment statement, e.g. "x = a + b".
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
            var rightRes = assignmentExpr.Right.AcceptVisitor(this, data);

            // add value to RefVariables if this is an "constant initialization"
            if (assignmentExpr.Operator == AssignmentOperatorType.Assign)
                if (assignmentExpr.Left.IsType<MemberReferenceExpression>())
                {
                    var left = (MemberReferenceExpression) assignmentExpr.Left;
                    var memberRef = left.Annotation<IMemberDefinition>();
                    var refVar = RefVariables.Last(var => var.Definition == memberRef);

                    if (assignmentExpr.Right.IsType<ObjectCreateExpression>() ||
                        assignmentExpr.Right.IsType<PrimitiveExpression>())
                        RefVariables[RefVariables.IndexOf(refVar)].Value = rightRes;
                    else
                        RefVariables[RefVariables.IndexOf(refVar)].Value = "Exception";
                }

            return result.Append(rightRes);
        }

        /// <summary>
        ///     Translates a binary operator expression, e.g. "a * b".
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
                result.Append(binaryOpExpr.Left.AcceptVisitor(this, data));

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

                result.Space().Append(opAssignment[binaryOpExpr.Operator]).Space();
                result.Append(binaryOpExpr.Right.AcceptVisitor(this, data));
            }

            return result;
        }

        /// <summary>
        ///     Translates a member reference, e.g. a field called "_value".
        /// </summary>
        public override StringBuilder VisitMemberReferenceExpression(MemberReferenceExpression memberRefExpr, int data)
        {
            var result = new StringBuilder();

            if (!(memberRefExpr.Target is ThisReferenceExpression))
                result.Append(memberRefExpr.Target.AcceptVisitor(this, data));

            var memberRef = memberRefExpr.Annotation<IMemberDefinition>();

            if (memberRef != null)
            {
                var instr = GetInstructionFromStmt(memberRefExpr.GetParent<Statement>());
                RefVariables.Add(new VariableDesc {Definition = memberRef, Instruction = instr});
            }

            return result.Append(memberRefExpr.MemberName);
        }

        /// <summary>
        ///     Translates a base reference, i.e. "base.*".
        /// </summary>
        public override StringBuilder VisitBaseReferenceExpression(BaseReferenceExpression baseRefExpr, int data)
        {
            return new StringBuilder();
        }

        /// <summary>
        ///     Translates an object creation, e.g. "new float4(...)".
        /// </summary>
        public override StringBuilder VisitObjectCreateExpression(ObjectCreateExpression objCreateExpr, int data)
        {
            var type = objCreateExpr.Type.AcceptVisitor(this, data);
            var args = JoinArgs(objCreateExpr.Arguments);

            return type.Method(String.Empty, args.ToString());
        }

        /// <summary>
        ///     Translates a primitive type, e.g. "1f".
        /// </summary>
        public override StringBuilder VisitPrimitiveExpression(PrimitiveExpression primitiveExpr, int data)
        {
            var result = new StringBuilder();
            var cultureInfo = CultureInfo.InvariantCulture.NumberFormat;

            if (primitiveExpr.Value is double)
            {
                var dInstr = GetInstructionFromStmt(primitiveExpr.GetParent<Statement>());
                Helper.Warning("Type 'double' is not supported. " +
                                  "Value will be casted to type 'float'.", dInstr);
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

            return result.Append(primitiveExpr.Value);
        }

        /// <summary>
        ///     Translates an unary expression, e.g. "value++".
        /// </summary>
        public override StringBuilder VisitUnaryOperatorExpression(UnaryOperatorExpression unaryOpExpr, int data)
        {
            var result = new StringBuilder();

            var expr = unaryOpExpr.Expression.AcceptVisitor(this, data);

            // unary operator type mapping
            var opPreAsngmt = new Dictionary<UnaryOperatorType, string>
            {
                {UnaryOperatorType.Decrement, "--"},
                {UnaryOperatorType.Increment, "++"},
                {UnaryOperatorType.Minus, "-"},
                {UnaryOperatorType.Plus, "+"},
                {UnaryOperatorType.BitNot, "~"},
                {UnaryOperatorType.Not, "!"},
                {UnaryOperatorType.PostDecrement, "--"},
                {UnaryOperatorType.PostIncrement, "++"},
            };

            var opPostAsngmt = new Dictionary<UnaryOperatorType, string>
            {
                {UnaryOperatorType.PostDecrement, "--"},
                {UnaryOperatorType.PostIncrement, "++"},
            };

            if (opPreAsngmt.ContainsKey(unaryOpExpr.Operator))
                result.Append(opPreAsngmt[unaryOpExpr.Operator]).Append(expr);
            else if (opPostAsngmt.ContainsKey(unaryOpExpr.Operator))
                result.Append(expr).Append(opPostAsngmt[unaryOpExpr.Operator]);
            else
            {
                var dInstr = GetInstructionFromStmt(unaryOpExpr.GetParent<Statement>());
                Helper.Error("Unary operator '" + unaryOpExpr.Operator + "' is not supported", dInstr);
            }

            return result;
        }

        /// <summary>
        ///     Translates a direction expression, e.g. "ref value" or "out value".
        /// </summary>
        public override StringBuilder VisitDirectionExpression(DirectionExpression directionExpr, int data)
        {
            return directionExpr.Expression.AcceptVisitor(this, data);
        }

        /// <summary>
        ///     Translates an identifier, e.g. a variable called "value".
        /// </summary>
        public override StringBuilder VisitIdentifierExpression(IdentifierExpression identifierExpr, int data)
        {
            return new StringBuilder(identifierExpr.Identifier);
        }

        /// <summary>
        ///     Translates an invocation expression, e.g. "Math.Max(10, 5)".
        /// </summary>
        public override StringBuilder VisitInvocationExpression(InvocationExpression invocationExpr, int data)
        {
            var result = new StringBuilder();

            var methodDef = invocationExpr.Annotation<MethodDefinition>() ??
                            invocationExpr.Annotation<MethodReference>().Resolve();
            var declType = methodDef.DeclaringType.ToType();

            var args = JoinArgs(invocationExpr.Arguments).ToString();

            // map method if it's a mathematical method or
            // map method if it's a xSLShader class' method
            if (xSLMathMapping.Types.Contains(declType) || (declType == typeof (xSLShader)))
                if (xSLMathMapping.Methods.ContainsKey(methodDef.Name))
                {
                    var mappedName = xSLMathMapping.Methods[methodDef.Name];
                    return result.Method(mappedName, args);
                }

            // otherwise just call the method
            if (declType != typeof(xSLShader))
                RefMethods.Add(methodDef);

            return result.Method(methodDef.Name, args);
        }

        /// <summary>
        ///     Translates an if/else statement, e.g. "if (...) { ... } else { ... }".
        /// </summary>
        public override StringBuilder VisitIfElseStatement(IfElseStatement ifElseStmt, int data)
        {
            var result = new StringBuilder();
            result.If(ifElseStmt.Condition.AcceptVisitor(this, data));

            var @true = (BlockStatement) ifElseStmt.TrueStatement;
            var trueStmt = @true.AcceptVisitor(this, data);

            if (@true.Statements.Count > 1)
                result.Block(trueStmt);
            else
                result.Intend().Append(trueStmt).Length -= 2;

            if (ifElseStmt.FalseStatement.IsNull)
                return result;

            var @false = (BlockStatement) ifElseStmt.FalseStatement;
            var falseStmt = @false.AcceptVisitor(this, data);

            if (@true.Statements.Count > 1)
                result.Else().Block(falseStmt);
            else
                result.Else().Intend().Append(falseStmt).Length -= 2;

            return result;
        }

        /// <summary>
        ///     Translates an return statement, e.g. "return value".
        /// </summary>
        public override StringBuilder VisitReturnStatement(ReturnStatement returnStmt, int data)
        {
            var expr = returnStmt.Expression.AcceptVisitor(this, data);
            return new StringBuilder("return").Space().Append(expr).Semicolon();
        }
    }
}