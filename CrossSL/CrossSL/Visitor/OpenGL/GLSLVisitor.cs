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
            // base
        }

        /// <summary>
        ///     Translates a block statement, e.g. a method's body.
        /// </summary>
        /// <remarks>
        ///     If verbose mode is active (i.e. if a .pdb file was found) additional
        ///     line breaks between statements are considered in the output.
        /// </remarks>
        public override StringBuilder VisitBlockStatement(BlockStatement blockStmt)
        {
            var result = new StringBuilder();

            foreach (var stmt in blockStmt.Statements)
                result.Append(stmt.AcceptVisitor(this)).NewLine();

            return result;
        }

        /// <summary>
        ///     Translates a variable declaration statement, e.g. "float4 x;".
        /// </summary>
        public override StringBuilder VisitVariableDeclarationStatement(
            VariableDeclarationStatement varDeclStmt)
        {
            var result = new StringBuilder();

            var type = varDeclStmt.Type.AcceptVisitor(this);
            foreach (var varDecl in varDeclStmt.Variables)
                result.Append(type).Space().Append(varDecl.AcceptVisitor(this)).Semicolon();

            return result;
        }

        /// <summary>
        ///     Translates a simple data type, e.g. "float4".
        /// </summary>
        public override StringBuilder VisitSimpleType(SimpleType simpleType)
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
        public override StringBuilder VisitPrimitiveType(PrimitiveType primitiveType)
        {
            var typeName = primitiveType.KnownTypeCode.ToString();
            var sysType = Type.GetType("System." + typeName);

            var mappedType = MapDataTypeIfValid(primitiveType, sysType);

            return mappedType == null
                ? new StringBuilder(primitiveType.Keyword)
                : new StringBuilder(mappedType);
        }

        /// <summary>
        ///     Translates a variable initializer, e.g. "x" or "x = 5".
        /// </summary>
        public override StringBuilder VisitVariableInitializer(VariableInitializer variableInit)
        {
            var result = new StringBuilder(variableInit.Name);

            if (!variableInit.Initializer.IsNull)
                result.Assign().Append(variableInit.Initializer.AcceptVisitor(this));

            return result;
        }

        /// <summary>
        ///     Translates an expression statement, e.g. "x = a + b".
        /// </summary>
        public override StringBuilder VisitExpressionStatement(ExpressionStatement exprStmt)
        {
            return exprStmt.Expression.AcceptVisitor(this).Semicolon();
        }

        /// <summary>
        ///     Translates an assignment statement, e.g. "x = a + b".
        /// </summary>
        public override StringBuilder VisitAssignmentExpression(AssignmentExpression assignmentExpr)
        {
            var result = assignmentExpr.Left.AcceptVisitor(this);

            // assignment operator type mapping
            var opAssignment = new Dictionary<AssignmentOperatorType, string>
            {
                {AssignmentOperatorType.Assign, String.Empty},
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
            var rightRes = assignmentExpr.Right.AcceptVisitor(this);

            // add value to RefVariables if this is an "constant initialization"
            if (assignmentExpr.Operator == AssignmentOperatorType.Assign)
                if (assignmentExpr.Left.IsType<MemberReferenceExpression>())
                {
                    var left = (MemberReferenceExpression) assignmentExpr.Left;
                    var memberRef = left.Annotation<IMemberDefinition>();

                    if (RefVariables.Any(var => var.Definition == memberRef))
                    {
                        var refVar = RefVariables.Last(var => var.Definition == memberRef);

                        if (assignmentExpr.Right.IsType<ObjectCreateExpression>() ||
                            assignmentExpr.Right.IsType<PrimitiveExpression>())
                            RefVariables[RefVariables.IndexOf(refVar)].Value = rightRes;
                        else
                            RefVariables[RefVariables.IndexOf(refVar)].Value = "Exception";
                    }
                }

            return result.Append(rightRes);
        }

        /// <summary>
        ///     Translates a binary operator expression, e.g. "a * b".
        /// </summary>
        public override StringBuilder VisitBinaryOperatorExpression(BinaryOperatorExpression binaryOpExpr)
        {
            var result = new StringBuilder();

            if (binaryOpExpr.Operator == BinaryOperatorType.Modulus)
            {
                var leftOp = binaryOpExpr.Left.AcceptVisitor(this).ToString();
                var rightOp = binaryOpExpr.Right.AcceptVisitor(this).ToString();

                result.Method("mod", leftOp, rightOp);
            }
            else
            {
                result.Append(binaryOpExpr.Left.AcceptVisitor(this));

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
                result.Append(binaryOpExpr.Right.AcceptVisitor(this));
            }

            return result;
        }

        /// <summary>
        ///     Translates a member reference, e.g. a field called "_value".
        /// </summary>
        public override StringBuilder VisitMemberReferenceExpression(MemberReferenceExpression memberRefExpr)
        {
            var result = new StringBuilder();

            if (!(memberRefExpr.Target is ThisReferenceExpression))
            {
                result = memberRefExpr.Target.AcceptVisitor(this);
                if (result != null && result.Length > 0) result.Dot();
            }

            var memberName = memberRefExpr.MemberName;
            if (memberRefExpr.Target is BaseReferenceExpression)
                memberName = memberName.Replace("xsl", "gl_");

            // save member reference
            var memberRef = memberRefExpr.Annotation<IMemberDefinition>();

            if (result != null && memberRef != null)
            {
                var instr = GetInstructionFromStmt(memberRefExpr.GetParent<Statement>());
                RefVariables.Add(new VariableDesc {Definition = memberRef, Instruction = instr});
            }

            return result != null ? result.Append(memberName) : new StringBuilder();
        }

        /// <summary>
        ///     Translates a base reference, i.e. "base.*".
        /// </summary>
        public override StringBuilder VisitBaseReferenceExpression(BaseReferenceExpression baseRefExpr)
        {
            return new StringBuilder();
        }

        /// <summary>
        ///     Translates an object creation, e.g. "new float4(...)".
        /// </summary>
        public override StringBuilder VisitObjectCreateExpression(ObjectCreateExpression objCreateExpr)
        {
            var type = objCreateExpr.Type.AcceptVisitor(this);
            var args = JoinArgs(objCreateExpr.Arguments);

            return type.Method(String.Empty, args.ToString());
        }

        /// <summary>
        ///     Translates a primitive type, e.g. "1f".
        /// </summary>
        public override StringBuilder VisitPrimitiveExpression(PrimitiveExpression primitiveExpr)
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
        ///     Translates a type reference, e.g. "OtherClass.".
        /// </summary>
        public override StringBuilder VisitTypeReferenceExpression(TypeReferenceExpression typeRefExpr)
        {
            var memberRef = typeRefExpr.GetParent<MemberReferenceExpression>();
            if (memberRef == null) return new StringBuilder();

            var instr = GetInstructionFromStmt(typeRefExpr.GetParent<Statement>());
            var name = memberRef.MemberName;

            Helper.Error("Static member '" + name + "' of class '" + typeRefExpr.Type + "' cannot be used", instr);

            return null;
        }

        /// <summary>
        ///     Translates an unary expression, e.g. "value++".
        /// </summary>
        public override StringBuilder VisitUnaryOperatorExpression(UnaryOperatorExpression unaryOpExpr)
        {
            var result = new StringBuilder();

            var expr = unaryOpExpr.Expression.AcceptVisitor(this);

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
        public override StringBuilder VisitDirectionExpression(DirectionExpression directionExpr)
        {
            return directionExpr.Expression.AcceptVisitor(this);
        }

        /// <summary>
        ///     Translates an identifier, e.g. a variable called "value".
        /// </summary>
        public override StringBuilder VisitIdentifierExpression(IdentifierExpression identifierExpr)
        {
            return new StringBuilder(identifierExpr.Identifier);
        }

        /// <summary>
        ///     Translates an invocation expression, e.g. "Math.Max(10, 5)".
        /// </summary>
        public override StringBuilder VisitInvocationExpression(InvocationExpression invocationExpr)
        {
            var result = new StringBuilder();

            var methodDef = invocationExpr.Annotation<MethodDefinition>() ??
                            invocationExpr.Annotation<MethodReference>().Resolve();

            var methodName = methodDef.Name;
            var declType = methodDef.DeclaringType.ToType();

            var args = JoinArgs(invocationExpr.Arguments).ToString();

            // map method if it's a mathematical method or
            // map method if it's a xSLShader class' method
            if (xSLMethodMapping.Types.Contains(declType))
                if (xSLMethodMapping.Methods.ContainsKey(methodName))
                {
                    var mappedName = xSLMethodMapping.Methods[methodName];
                    return result.Method(mappedName, args);
                }

            // otherwise just call the method
            if (declType != typeof (xSLShader))
                RefMethods.Add(methodDef);

            return result.Method(methodDef.Name, args);
        }

        /// <summary>
        ///     Translates an if/else statement, e.g. "if (...) { ... } else { ... }".
        /// </summary>
        public override StringBuilder VisitIfElseStatement(IfElseStatement ifElseStmt)
        {
            var result = new StringBuilder();
            result.If(ifElseStmt.Condition.AcceptVisitor(this));

            var @true = (BlockStatement) ifElseStmt.TrueStatement;
            var trueStmt = @true.AcceptVisitor(this);

            if (@true.Statements.Count > 1)
                result.Block(trueStmt);
            else
                result.Intend().Append(trueStmt).Length -= 2;

            if (ifElseStmt.FalseStatement.IsNull)
                return result;

            var @false = (BlockStatement) ifElseStmt.FalseStatement;
            var falseStmt = @false.AcceptVisitor(this);

            if (@true.Statements.Count > 1)
                result.Else().Block(falseStmt);
            else
                result.Else().Intend().Append(falseStmt).Length -= 2;

            return result;
        }

        /// <summary>
        ///     Translates an return statement, e.g. "return value".
        /// </summary>
        public override StringBuilder VisitReturnStatement(ReturnStatement returnStmt)
        {
            var expr = returnStmt.Expression.AcceptVisitor(this);
            return new StringBuilder("return").Space().Append(expr).Semicolon();
        }
    }
}